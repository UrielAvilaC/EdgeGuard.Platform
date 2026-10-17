#Requires -Version 5.1
<#
.SYNOPSIS
    Arma el paquete de entrega al cliente: instalador + paquete sellado + checksums +
    manuales, en un solo .zip.

.DESCRIPTION
    Seal-Version.ps1 deja el paquete sellado en setup\<hub|node>\data\. Lo que se
    entrega al cliente es más que eso: la carpeta del instalador, el paquete, los
    checksums y los manuales. Armarlo a mano llevó a entregar un install.zip que
    arrastraba un hub-install.psd1 real, con la configuración de una sede.

    Este script lo arma con LISTA BLANCA: solo viaja lo que está nombrado aquí. Un
    archivo nuevo que alguien deje en setup\ no entra por accidente, y un .psd1 de
    sede no puede entrar porque nunca se nombra.

    Antes de empaquetar exige que lo sellado sea de fiar:

      · version.json es del número pedido, de un árbol limpio, y declara el hash del
        paquete que hay en data\.
      · El paquete en data\ coincide con ese hash y con su checksums.sha256.
      · La copia del módulo común en bin\ es idéntica a la fuente de setup\common\.
      · Todo script lleva BOM y parsea.

    Y al final audita lo que va a viajar: ni configuración de sede, ni certificados,
    ni bases de datos, ni el appsettings.Development.json del Hub, tampoco dentro del
    paquete sellado.

    Escribe también <paquete>.sha256 con el hash del zip de entrega, para comunicarlo
    al cliente por un canal distinto del propio zip: los checksums que viajan DENTRO
    detectan un zip corrupto, no uno alterado.

.PARAMETER Target
    Qué entregar: Hub o Node.

.PARAMETER Version
    Número a entregar. Sin él, el vigente en Directory.Build.props.

.PARAMETER OutputPath
    Carpeta de salida. Por defecto, artifacts\delivery\ en la raíz del repositorio.

.PARAMETER AllowUnsealed
    Permite entregar sin un version.json limpio y coincidente. Es para ensayar; el
    paquete queda marcado en pantalla como NO sellado y no debe llegar a un cliente.

.PARAMETER DryRun
    Recorre y valida todo, pero no escribe el zip de entrega.

.EXAMPLE
    .\New-DeliveryPackage.ps1 -Target Node
    Entrega del nodo en la versión vigente.

.EXAMPLE
    .\New-DeliveryPackage.ps1 -Target Hub -Version 1.5.0 -OutputPath D:\entregas
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('Hub', 'Node')]
    [string]$Target,

    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version,

    [string]$OutputPath,

    [switch]$AllowUnsealed,
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$CommonPath = $PSScriptRoot
$SetupPath  = Split-Path -Parent $CommonPath
$RepoPath   = Split-Path -Parent $SetupPath

$BuildProps = Join-Path $RepoPath 'Directory.Build.props'
$ModulePath = Join-Path $CommonPath 'EdgeGuard.Setup.psm1'
$Manifiesto = Join-Path $CommonPath 'version.json'

# ─────────────────────────────────────────────────────────────────────────────
# Qué viaja en cada entrega
#
# Una sola tabla: lo que distingue la entrega del Hub de la del nodo cabe aquí.
# Nada que no esté nombrado se copia.
# ─────────────────────────────────────────────────────────────────────────────
$Entregables = @{
    Hub = @{
        Carpeta   = 'hub'
        Nombre    = 'edgeguard-hub'
        Ejemplo   = 'hub-install.example.psd1'
        Manuales  = Join-Path $SetupPath 'manuales'
        # Archivos opcionales de data\ que también se firman y se entregan
        Anexos    = @('dotnet-hosting-*.exe')
        # Dentro del paquete sellado: motivo para abortar
        ProhibidoEnPaquete = @('appsettings.Development.json')
        AvisoEnPaquete     = @()
    }
    Node = @{
        Carpeta   = 'node'
        Nombre    = 'edgeguard-node'
        Ejemplo   = 'node-install.example.psd1'
        Manuales  = Join-Path $SetupPath 'node\manuales'
        Anexos    = @()
        ProhibidoEnPaquete = @()
        # Sin secretos (se revisó), pero un entorno Development no tiene sitio en una sede
        AvisoEnPaquete     = @('appsettings.Development.json')
    }
}

# Nunca en la entrega, en ningún nivel: configuración de sede, material criptográfico
# y datos. Defensa en profundidad: la lista blanca ya los deja fuera.
$PatronesProhibidos = @(
    '*-install.psd1'      # excepto *.example.psd1, que se resuelve abajo
    '*.pfx', '*.key', '*.pem'
    '*.db', '*.sqlite'
    '.env', '*.env'
    '*.log'
)

# ─────────────────────────────────────────────────────────────────────────────
# Utilidades
# ─────────────────────────────────────────────────────────────────────────────

function Write-Paso {
    param([string]$Texto)
    Write-Host ""
    Write-Host "  $Texto" -ForegroundColor Cyan
}

function Write-Detalle {
    param([string]$Texto)
    Write-Host "    $Texto" -ForegroundColor DarkGray
}

function Write-Aviso {
    param([string]$Texto)
    Write-Host "    AVISO  $Texto" -ForegroundColor Yellow
}

function Get-VersionActual {
    $contenido = [System.IO.File]::ReadAllText($BuildProps, (New-Object System.Text.UTF8Encoding $false))
    if ($contenido -notmatch '<Version>(\d+\.\d+\.\d+)</Version>') {
        throw "No se encontró <Version> en $BuildProps."
    }
    return $Matches[1]
}

function Get-Sha256 {
    param([string]$Path)
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToUpperInvariant()
}

<#
.SYNOPSIS
    Lee un checksums.sha256 como tabla nombre → hash.
#>
function Read-Checksums {
    param([string]$Path)

    $tabla = @{}
    foreach ($linea in [System.IO.File]::ReadAllLines($Path)) {
        $t = $linea.Trim()
        if (-not $t -or $t.StartsWith('#')) { continue }
        if ($t -notmatch '^([0-9A-Fa-f]{64})\s+(.+)$') {
            throw "Línea con formato inválido en ${Path}: '$t'"
        }
        $tabla[$Matches[2]] = $Matches[1].ToUpperInvariant()
    }
    return $tabla
}

# ─────────────────────────────────────────────────────────────────────────────
# Validación de lo sellado
# ─────────────────────────────────────────────────────────────────────────────

<#
.SYNOPSIS
    Resuelve y valida las fuentes. Devuelve un objeto con todo lo que se va a copiar.
#>
function Resolve-Fuentes {
    param([hashtable]$Def, [string]$Version)

    $raiz = Join-Path $SetupPath $Def.Carpeta
    $data = Join-Path $raiz 'data'

    $requeridos = @(
        @{ Etiqueta = 'install.ps1';           Ruta = Join-Path $raiz 'install.ps1' }
        @{ Etiqueta = 'README.md';             Ruta = Join-Path $raiz 'README.md' }
        @{ Etiqueta = $Def.Ejemplo;            Ruta = Join-Path $raiz $Def.Ejemplo }
        @{ Etiqueta = 'bin\';                  Ruta = Join-Path $raiz 'bin' }
        @{ Etiqueta = 'data\checksums.sha256'; Ruta = Join-Path $data 'checksums.sha256' }
    )
    foreach ($r in $requeridos) {
        if (-not (Test-Path -LiteralPath $r.Ruta)) {
            throw "Falta $($r.Etiqueta) en $raiz. ¿Se ejecutó Seal-Version.ps1?"
        }
    }

    $nombreZip = "$($Def.Nombre)-$Version.zip"
    $zip = Join-Path $data $nombreZip
    if (-not (Test-Path -LiteralPath $zip)) {
        throw ("No existe $nombreZip en $data. Sella esa versión con " +
               "Seal-Version.ps1 -Target $Target antes de entregarla.")
    }

    $anexos = New-Object System.Collections.Generic.List[string]
    foreach ($patron in $Def.Anexos) {
        $hallado = @(Get-ChildItem -LiteralPath $data -File -Filter $patron | Sort-Object Name)
        # El más nuevo por nombre: un instalador antiguo del mismo producto no debe viajar
        if ($hallado.Count -gt 0) { $anexos.Add($hallado[-1].FullName) }
    }

    $manuales = @()
    if (Test-Path -LiteralPath $Def.Manuales) {
        $manuales = @(Get-ChildItem -LiteralPath $Def.Manuales -File -Filter '*.pdf')
    }
    if ($manuales.Count -eq 0) {
        Write-Aviso "No hay PDF en $($Def.Manuales): la entrega saldrá sin manuales."
    }

    return [pscustomobject]@{
        Raiz      = $raiz
        Data      = $data
        Zip       = $zip
        NombreZip = $nombreZip
        Anexos    = @($anexos)
        Manuales  = $manuales
        Checksums = Join-Path $data 'checksums.sha256'
    }
}

<#
.SYNOPSIS
    El paquete de data\ coincide con lo que Seal-Version firmó.
#>
function Assert-PaqueteSellado {
    param($Fuentes)

    $declarados = Read-Checksums -Path $Fuentes.Checksums

    foreach ($archivo in @($Fuentes.Zip) + $Fuentes.Anexos) {
        $nombre = Split-Path -Leaf $archivo
        if (-not $declarados.ContainsKey($nombre)) {
            throw "$nombre no está declarado en data\checksums.sha256. Vuelve a sellar."
        }
        if ((Get-Sha256 -Path $archivo) -ne $declarados[$nombre]) {
            throw ("El hash de $nombre no coincide con data\checksums.sha256: el archivo cambió " +
                   "después del sellado, o se copió a medias.")
        }
    }
    Write-Detalle "paquete y anexos coinciden con data\checksums.sha256"
}

<#
.SYNOPSIS
    version.json declara este número, de un árbol limpio, con este mismo paquete.
.DESCRIPTION
    Es lo que convierte la entrega en trazable: de un zip de hace tres meses se puede
    decir de qué commit salió. Sin esto, se entregaría un paquete del que no se sabe
    nada.
#>
function Assert-Manifiesto {
    param($Fuentes, [string]$Version)

    $problemas = New-Object System.Collections.Generic.List[string]

    if (-not (Test-Path -LiteralPath $Manifiesto)) {
        $problemas.Add('no existe setup\common\version.json')
    }
    else {
        $m = Get-Content -LiteralPath $Manifiesto -Raw | ConvertFrom-Json

        if ($m.version -ne $Version) {
            $problemas.Add("version.json es de la versión $($m.version), no de $Version")
        }
        if (-not $m.arbolLimpio) {
            $problemas.Add('version.json se selló desde un árbol con cambios sin confirmar (no reproducible)')
        }

        # Los nombres de propiedad se sacan a una lista: sobre un objeto sin propiedades,
        # '.Name' falla bajo StrictMode en vez de dar una lista vacía.
        $paquete = $null
        $sellados = @($m.paquetes.PSObject.Properties | ForEach-Object { $_.Name })
        if ($sellados -contains $Target) {
            $paquete = $m.paquetes.$Target
        }
        if (-not $paquete) {
            $problemas.Add("version.json no incluye el paquete $Target (¿se selló con otro -Target? Sella con All)")
        }
        elseif (@($paquete.hashes.PSObject.Properties | ForEach-Object { $_.Name }) -notcontains $Fuentes.NombreZip) {
            $problemas.Add("version.json no declara $($Fuentes.NombreZip)")
        }
        elseif ($paquete.hashes.($Fuentes.NombreZip) -ne (Get-Sha256 -Path $Fuentes.Zip)) {
            $problemas.Add("el hash de $($Fuentes.NombreZip) no es el que version.json registró al sellar")
        }
    }

    if ($problemas.Count -eq 0) {
        Write-Detalle "version.json: sellado limpio de $Version, con el hash de este paquete"
        return $true
    }

    $texto = ($problemas | ForEach-Object { "      · $_" }) -join "`n"
    if ($AllowUnsealed) {
        Write-Aviso "Entrega NO sellada (-AllowUnsealed). No debe llegar a un cliente:`n$texto"
        return $false
    }

    throw ("El sellado no es de fiar:`n$texto`n    Vuelve a sellar desde un árbol limpio " +
           "(Seal-Version.ps1) o usa -AllowUnsealed para ensayar.")
}

function Assert-ModuloComun {
    param($Fuentes)

    $copia = Join-Path $Fuentes.Raiz 'bin\EdgeGuard.Setup.psm1'
    if (-not (Test-Path -LiteralPath $copia)) {
        throw "Falta bin\EdgeGuard.Setup.psm1 en $($Fuentes.Raiz): Seal-Version.ps1 lo copia al empaquetar."
    }
    if ((Get-Sha256 -Path $copia) -ne (Get-Sha256 -Path $ModulePath)) {
        throw ("bin\EdgeGuard.Setup.psm1 no es idéntico a setup\common\EdgeGuard.Setup.psm1: " +
               "es una copia desfasada. Vuelve a sellar.")
    }
}

<#
.SYNOPSIS
    Todo script que viaja lleva BOM y parsea.
.DESCRIPTION
    PS 5.1 lee un .ps1 sin BOM como ANSI: un guion largo cierra una cadena y el
    error aparece mucho más abajo, en el servidor del cliente.
#>
function Assert-Scripts {
    param([string]$Carpeta)

    # Filtro por extensión y no -Include: con -LiteralPath, PS 5.1 lo ignora y el
    # control acabaría exigiendo BOM a un README.
    $scripts = @(Get-ChildItem -LiteralPath $Carpeta -Recurse -File |
        Where-Object { $_.Extension -in @('.ps1', '.psm1', '.psd1') })
    foreach ($s in $scripts) {
        $b = [System.IO.File]::ReadAllBytes($s.FullName)
        $conBom = ($b.Length -ge 3 -and $b[0] -eq 239 -and $b[1] -eq 187 -and $b[2] -eq 191)
        if (-not $conBom) {
            throw "$($s.Name) no está en UTF-8 con BOM. Windows PowerShell 5.1 lo leería como ANSI."
        }

        $errores = $null
        $tokens  = $null
        [void][System.Management.Automation.Language.Parser]::ParseFile($s.FullName, [ref]$tokens, [ref]$errores)
        if ($errores -and $errores.Count -gt 0) {
            throw "$($s.Name) no parsea: $($errores[0].Message) (línea $($errores[0].Extent.StartLineNumber))"
        }
    }
    Write-Detalle "$($scripts.Count) script(s) con BOM y sin errores de sintaxis"
}

# ─────────────────────────────────────────────────────────────────────────────
# Montaje
# ─────────────────────────────────────────────────────────────────────────────

function Copy-Entrega {
    param($Fuentes, [hashtable]$Def, [string]$Destino)

    New-Item -ItemType Directory -Path $Destino, "$Destino\data", "$Destino\manuales", "$Destino\log" -Force | Out-Null

    # Raíz: solo lo nombrado
    foreach ($nombre in @('install.ps1', 'README.md', $Def.Ejemplo)) {
        Copy-Item -LiteralPath (Join-Path $Fuentes.Raiz $nombre) -Destination $Destino
    }

    # bin\: solo scripts y el módulo. Cualquier otra cosa que alguien deje ahí no viaja.
    New-Item -ItemType Directory -Path "$Destino\bin" -Force | Out-Null
    Get-ChildItem -LiteralPath (Join-Path $Fuentes.Raiz 'bin') -File |
        Where-Object { $_.Extension -in @('.ps1', '.psm1') } |
        ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination "$Destino\bin" }

    # data\: el paquete de ESTA versión, sus anexos y el README. Versiones anteriores, no.
    foreach ($archivo in @($Fuentes.Zip) + $Fuentes.Anexos) {
        Copy-Item -LiteralPath $archivo -Destination "$Destino\data"
    }
    $readmeData = Join-Path $Fuentes.Data 'README.md'
    if (Test-Path -LiteralPath $readmeData) {
        Copy-Item -LiteralPath $readmeData -Destination "$Destino\data"
    }

    foreach ($pdf in $Fuentes.Manuales) {
        Copy-Item -LiteralPath $pdf.FullName -Destination "$Destino\manuales"
    }
}

<#
.SYNOPSIS
    Reescribe data\checksums.sha256 con lo que de verdad viaja.
.DESCRIPTION
    El de origen declara también versiones anteriores que aquí no van; un archivo
    declarado pero ausente se omite con aviso en el instalador, y un aviso que
    siempre sale deja de leerse.
#>
function Write-ChecksumsEntrega {
    param([string]$Destino, [hashtable]$Def, [string]$Version)

    $data = Join-Path $Destino 'data'
    $lineas = New-Object System.Collections.Generic.List[string]
    $lineas.Add("# EdgeGuard $($Def.Nombre) $Version")
    $lineas.Add("# Paquete de entrega generado por setup\common\New-DeliveryPackage.ps1 el $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')")
    $lineas.Add('#')

    $artefactos = @(Get-ChildItem -LiteralPath $data -File |
        Where-Object { $_.Name -ne 'checksums.sha256' -and $_.Extension -ne '.md' -and -not $_.Name.StartsWith('.') } |
        Sort-Object Name)

    foreach ($a in $artefactos) {
        $lineas.Add("$(Get-Sha256 -Path $a.FullName)  $($a.Name)")
    }

    [System.IO.File]::WriteAllLines((Join-Path $data 'checksums.sha256'), $lineas, (New-Object System.Text.UTF8Encoding $false))
    Write-Detalle "data\checksums.sha256 — $($artefactos.Count) artefacto(s)"
}

# ─────────────────────────────────────────────────────────────────────────────
# Auditoría de lo que va a viajar
# ─────────────────────────────────────────────────────────────────────────────

function Test-NombreProhibido {
    param([string]$Nombre)

    foreach ($patron in $PatronesProhibidos) {
        if ($Nombre -like $patron) {
            # El ejemplo es plantilla, no configuración de sede
            if ($Nombre -like '*.example.psd1') { return $false }
            return $true
        }
    }
    return $false
}

function Assert-Auditoria {
    param([string]$Destino, [hashtable]$Def, $Fuentes)

    $hallazgos = New-Object System.Collections.Generic.List[string]

    foreach ($f in Get-ChildItem -LiteralPath $Destino -Recurse -File) {
        $relativo = $f.FullName.Substring($Destino.Length).TrimStart('\')
        if (Test-NombreProhibido -Nombre $f.Name) {
            $hallazgos.Add("$relativo (nombre prohibido en una entrega)")
        }
    }

    # Y dentro del paquete sellado: es lo que se despliega en el servidor del cliente
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [System.IO.Compression.ZipFile]::OpenRead($Fuentes.Zip)
    try {
        foreach ($e in $zip.Entries) {
            $nombre = ($e.FullName -split '/')[-1]
            if (-not $nombre) { continue }

            if ($Def.ProhibidoEnPaquete -contains $nombre -or (Test-NombreProhibido -Nombre $nombre)) {
                $hallazgos.Add("$($Fuentes.NombreZip) → $($e.FullName)")
            }
            elseif ($Def.AvisoEnPaquete -contains $nombre) {
                Write-Aviso "$($Fuentes.NombreZip) lleva $($e.FullName): sin secretos, pero no hace falta en una sede."
            }
        }
    }
    finally { $zip.Dispose() }

    if ($hallazgos.Count -gt 0) {
        $texto = ($hallazgos | ForEach-Object { "      · $_" }) -join "`n"
        throw "La auditoría encontró archivos que no deben llegar a un cliente:`n$texto"
    }
    Write-Detalle "auditoría: sin configuración de sede, certificados, bases de datos ni registros"
}

# ─────────────────────────────────────────────────────────────────────────────
# Principal
# ─────────────────────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "  ─────────────────────────────────────────────────────────────" -ForegroundColor Cyan
Write-Host "   EdgeGuard — paquete de entrega" -ForegroundColor Cyan
Write-Host "  ─────────────────────────────────────────────────────────────" -ForegroundColor Cyan

if (-not $Version) { $Version = Get-VersionActual }
if (-not $OutputPath) { $OutputPath = Join-Path $RepoPath 'artifacts\delivery' }

$def = $Entregables[$Target]

Write-Host ""
Write-Host ("   {0,-16} {1}" -f 'Destino', $Target)
Write-Host ("   {0,-16} {1}" -f 'Versión', $Version)
if ($DryRun) { Write-Host ("   {0,-16} {1}" -f 'Modo', 'DRY-RUN — no se escribe el zip') }

Write-Paso '1 · Validar lo sellado'
$fuentes = Resolve-Fuentes -Def $def -Version $Version
Assert-PaqueteSellado -Fuentes $fuentes
Assert-ModuloComun -Fuentes $fuentes
$sellado = Assert-Manifiesto -Fuentes $fuentes -Version $Version

$staging = Join-Path ([System.IO.Path]::GetTempPath()) ("edgeguard-entrega-" + [guid]::NewGuid().ToString('N'))
try {
    Write-Paso '2 · Montar la entrega (lista blanca)'
    Copy-Entrega -Fuentes $fuentes -Def $def -Destino $staging
    Write-ChecksumsEntrega -Destino $staging -Def $def -Version $Version

    # version.json viaja solo si es el de este sellado: es el rastro de qué código es
    if ($sellado) {
        Copy-Item -LiteralPath $Manifiesto -Destination $staging
        Write-Detalle 'version.json incluido (commit, fecha y hashes del sellado)'
    }

    Write-Paso '3 · Comprobar los scripts que viajan'
    Assert-Scripts -Carpeta $staging

    Write-Paso '4 · Auditar'
    Assert-Auditoria -Destino $staging -Def $def -Fuentes $fuentes

    Write-Paso '5 · Comprimir'
    $nombreSalida = "$($def.Nombre)-$Version-instalador.zip"
    $salida = Join-Path $OutputPath $nombreSalida

    if ($DryRun) {
        Write-Detalle "[dry-run] $salida"
    }
    else {
        if (-not (Test-Path -LiteralPath $OutputPath)) {
            New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
        }
        if (Test-Path -LiteralPath $salida) { Remove-Item -LiteralPath $salida -Force }

        Add-Type -AssemblyName System.IO.Compression.FileSystem
        [System.IO.Compression.ZipFile]::CreateFromDirectory(
            $staging, $salida,
            [System.IO.Compression.CompressionLevel]::Optimal,
            $false)

        $hash = Get-Sha256 -Path $salida
        [System.IO.File]::WriteAllText("$salida.sha256", "$hash  $nombreSalida`n", (New-Object System.Text.UTF8Encoding $false))

        Write-Detalle "$nombreSalida — $([math]::Round((Get-Item -LiteralPath $salida).Length / 1MB, 1)) MB"
        Write-Detalle "$nombreSalida.sha256"
    }
}
finally {
    if (Test-Path -LiteralPath $staging) { Remove-Item -LiteralPath $staging -Recurse -Force -ErrorAction SilentlyContinue }
}

Write-Host ""
if ($DryRun) {
    Write-Host "  DRY-RUN completado. No se escribió el zip." -ForegroundColor Yellow
}
else {
    Write-Host "  Entrega lista: $salida" -ForegroundColor Green
    Write-Host "  SHA-256 del paquete (comunícalo al cliente por otro canal):" -ForegroundColor Green
    Write-Host "    $hash" -ForegroundColor Green
    if (-not $sellado) {
        Write-Host "  ATENCIÓN: entrega NO sellada. No la envíes a un cliente." -ForegroundColor Red
    }
}
Write-Host ""
