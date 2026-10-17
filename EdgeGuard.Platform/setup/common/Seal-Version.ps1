#Requires -Version 5.1
<#
.SYNOPSIS
    Sella una versión de EdgeGuard: estampa el número, compila, empaqueta y firma
    los artefactos con su SHA-256.

.DESCRIPTION
    Una versión sellada es un conjunto de artefactos del que se puede decir, más
    tarde y sin ambigüedad, de qué código salió. Este script produce ese conjunto
    y deja constancia de cómo se produjo.

    Hace cinco cosas, en este orden:

      1. Estampa el número en el único sitio donde vive (Directory.Build.props) y
         en el package.json del SPA, que el SDK de .NET no toca.
      2. Compila y publica lo que cada instalador despliega.
      3. Comprime con la versión en el nombre, que es como el paso 01 de cada
         instalador elige el paquete más nuevo cuando hay varios.
      4. Calcula los SHA-256 y escribe el checksums.sha256 que ese mismo paso 01
         verifica antes de tocar nada.
      5. Escribe version.json con el commit, la fecha y los hashes.

    El paso 5 es el que convierte esto en un sellado y no en un empaquetado: sin
    el commit, un .zip de hace tres meses no dice de qué código salió, y cotejar
    el binario contra el repositorio deja de ser posible.

.PARAMETER Version
    Número a sellar, en formato MAJOR.MINOR.PATCH. Sin él, se relee el actual de
    Directory.Build.props y se reempaqueta sin cambiar el número — útil para
    rehacer un paquete corrupto sin inventar una versión.

.PARAMETER Target
    Qué empaquetar: Hub, Node o All (por defecto).

.PARAMETER Tag
    Crea además la etiqueta de git 'v<versión>'. Exige, desde el inicio, un
    árbol limpio y el número ya estampado y confirmado: una etiqueta sobre
    cambios sin confirmar apunta a un commit que no contiene lo que se empaquetó.
    Por eso se sella primero sin -Tag, se confirma el número por PR, y se etiqueta
    después desde el commit integrado.

.PARAMETER DryRun
    Recorre todo sin escribir: dice qué número estamparía, qué compilaría y con
    qué nombre quedaría cada paquete.

.EXAMPLE
    .\Seal-Version.ps1 -Version 1.5.0 -Tag
    Sellado completo de una versión nueva.

.EXAMPLE
    .\Seal-Version.ps1 -Target Node
    Reempaqueta solo el nodo con la versión que ya está estampada.
#>
[CmdletBinding()]
param(
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version,

    [ValidateSet('All', 'Hub', 'Node')]
    [string]$Target = 'All',

    [switch]$Tag,
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$CommonPath = $PSScriptRoot
$SetupPath  = Split-Path -Parent $CommonPath
$RepoPath   = Split-Path -Parent $SetupPath

$BuildProps  = Join-Path $RepoPath 'Directory.Build.props'
$SpaPackage  = Join-Path $RepoPath 'src\frontend\dicomedge-ui\package.json'
$ModulePath  = Join-Path $CommonPath 'EdgeGuard.Setup.psm1'

# ─────────────────────────────────────────────────────────────────────────────
# Definición de los paquetes
#
# Un renglón por instalador. Todo lo que distingue al Hub del nodo cabe aquí:
# qué proyecto se publica, con qué runtime y qué se comprueba dentro del zip.
# ─────────────────────────────────────────────────────────────────────────────
$Paquetes = [ordered]@{
    Hub  = @{
        Proyecto     = 'src\backend\Dicom.Edge.Hub.Api'
        Nombre       = 'edgeguard-hub'
        Destino      = Join-Path $SetupPath 'hub\data'
        BinInstalador = Join-Path $SetupPath 'hub\bin'
        SelfContained = $false
        # El SPA se compila ANTES de publicar: angular.json escribe directamente en
        # wwwroot\ del proyecto de la API y el .csproj no dispara el build del front.
        # Publicar primero produce un paquete sin front que solo se nota al abrir el
        # navegador — por eso el paso 01 del instalador exige este archivo.
        Spa          = 'src\frontend\dicomedge-ui'
        Requiere     = @('wwwroot/index.html', 'Dicom.Edge.Hub.Api.dll')
    }
    Node = @{
        Proyecto     = 'src\edge\Dicom.Edge.Node'
        Nombre       = 'edgeguard-node'
        Destino      = Join-Path $SetupPath 'node\data'
        BinInstalador = Join-Path $SetupPath 'node\bin'
        # Self-contained: un nodo vive en el servidor de una sede, sin administrador
        # a mano. Llevarse el runtime dentro cuesta ~55 MB más de zip y elimina la
        # clase entera de fallos "la versión de .NET del servidor no es la que toca".
        SelfContained = $true
        Spa          = $null
        Requiere     = @('Dicom.Edge.Node.exe', 'appsettings.Production.json')
    }
}

# ─────────────────────────────────────────────────────────────────────────────
# Utilidades
# ─────────────────────────────────────────────────────────────────────────────

function Write-Paso {
    param([string]$Texto, [string]$Color = 'Cyan')
    Write-Host ""
    Write-Host "  $Texto" -ForegroundColor $Color
}

function Write-Detalle {
    param([string]$Texto)
    Write-Host "    $Texto" -ForegroundColor DarkGray
}

function Invoke-Accion {
    param([string]$Descripcion, [scriptblock]$Accion)

    if ($DryRun) {
        Write-Detalle "[dry-run] $Descripcion"
        return $null
    }
    Write-Detalle $Descripcion
    return (& $Accion)
}

<#
.SYNOPSIS
    Lee el <Version> vigente de Directory.Build.props.
#>
function Get-VersionActual {
    $contenido = Get-Content -LiteralPath $BuildProps -Raw
    if ($contenido -notmatch '<Version>(\d+\.\d+\.\d+)</Version>') {
        throw "No se encontró <Version> en $BuildProps. Es el único sitio donde vive el número; sin él no hay nada que sellar."
    }
    return $Matches[1]
}

<#
.SYNOPSIS
    Estampa el número en Directory.Build.props y en el package.json del SPA.
.DESCRIPTION
    Son los dos sitios que no se derivan de ningún otro. Todo lo demás —los
    ensamblados, el /api/info del Hub, la versión que el nodo reporta al
    registrarse— sale de <Version> por el SDK, y por eso no se toca aquí.
#>
function Set-VersionEnFuentes {
    param([string]$Nueva)

    # Lectura en UTF-8 explícito: Get-Content de PS 5.1 toma un archivo sin BOM como
    # ANSI, y al reescribirlo en UTF-8 cada tilde de los comentarios queda corrompida.
    $utf8 = New-Object System.Text.UTF8Encoding $false

    $props = [System.IO.File]::ReadAllText($BuildProps, $utf8)
    $actualizado = $props -replace '<Version>\d+\.\d+\.\d+</Version>', "<Version>$Nueva</Version>"

    Invoke-Accion "Directory.Build.props → <Version>$Nueva</Version>" {
        # Sin BOM: MSBuild lo tolera, pero el diff de un archivo que gana BOM
        # aparece entero cambiado y esconde la única línea que sí cambió.
        [System.IO.File]::WriteAllText($BuildProps, $actualizado, (New-Object System.Text.UTF8Encoding $false))
    } | Out-Null

    if (Test-Path -LiteralPath $SpaPackage) {
        $pkg = [System.IO.File]::ReadAllText($SpaPackage, $utf8)
        # Solo la primera aparición: es la del propio paquete. Las de 'dependencies'
        # son versiones de terceros y pisarlas rompería el npm ci.
        $pkgActualizado = [regex]::Replace(
            $pkg, '"version"\s*:\s*"[^"]*"', """version"": ""$Nueva""", 1)

        Invoke-Accion "package.json del SPA → $Nueva" {
            [System.IO.File]::WriteAllText($SpaPackage, $pkgActualizado, (New-Object System.Text.UTF8Encoding $false))
        } | Out-Null
    }
}

<#
.SYNOPSIS
    Publica un proyecto y devuelve el directorio con el resultado.
#>
function Publish-Proyecto {
    param([hashtable]$Paquete, [string]$Version)

    $proyecto = Join-Path $RepoPath $Paquete.Proyecto
    $salida   = Join-Path $RepoPath "artifacts\publish\$($Paquete.Nombre)-$Version"

    if ($Paquete.Spa) {
        $spa = Join-Path $RepoPath $Paquete.Spa
        Invoke-Accion "npm ci + build del SPA en $($Paquete.Spa)" {
            Push-Location $spa
            try {
                & npm ci
                if ($LASTEXITCODE -ne 0) { throw "npm ci falló" }
                & npm run build
                if ($LASTEXITCODE -ne 0) { throw "npm run build falló" }
            }
            finally { Pop-Location }
        } | Out-Null
    }

    $argumentos = @(
        'publish', $proyecto
        '--configuration', 'Release'
        '--runtime', 'win-x64'
        "--self-contained", $(if ($Paquete.SelfContained) { 'true' } else { 'false' })
        '--output', $salida
        '--nologo'
    )

    Invoke-Accion "dotnet publish $($Paquete.Proyecto) (self-contained=$($Paquete.SelfContained))" {
        if (Test-Path -LiteralPath $salida) { Remove-Item -LiteralPath $salida -Recurse -Force }
        & dotnet @argumentos | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "dotnet publish falló para $($Paquete.Proyecto)" }
    } | Out-Null

    return $salida
}

<#
.SYNOPSIS
    Comprueba que el publicado traiga lo que el instalador va a buscar.
.DESCRIPTION
    Se verifica aquí y no solo en el instalador porque aquí todavía se puede
    arreglar: un paquete al que le falta el SPA se detecta en segundos, no
    después de haberlo mandado a una sede.
#>
function Assert-Contenido {
    param([string]$Publicado, [string[]]$Requeridos, [string]$Nombre)

    if ($DryRun) { return }

    $faltantes = @($Requeridos | Where-Object {
        -not (Test-Path -LiteralPath (Join-Path $Publicado ($_ -replace '/', '\')))
    })

    if ($faltantes.Count -gt 0) {
        throw "El publicado de $Nombre no tiene: $($faltantes -join ', '). El paquete saldría incompleto."
    }
}

<#
.SYNOPSIS
    Comprime el publicado con la versión en el nombre.
#>
function New-Paquete {
    param([hashtable]$Paquete, [string]$Publicado, [string]$Version)

    $zip = Join-Path $Paquete.Destino "$($Paquete.Nombre)-$Version.zip"

    Invoke-Accion "comprimir → $(Split-Path -Leaf $zip)" {
        if (-not (Test-Path -LiteralPath $Paquete.Destino)) {
            New-Item -ItemType Directory -Path $Paquete.Destino -Force | Out-Null
        }
        if (Test-Path -LiteralPath $zip) { Remove-Item -LiteralPath $zip -Force }

        # Compress-Archive de PS 5.1 no comprime bien árboles grandes y se atraganta
        # con rutas largas; ZipFile es el que usa el propio SDK.
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        [System.IO.Compression.ZipFile]::CreateFromDirectory(
            $Publicado, $zip,
            [System.IO.Compression.CompressionLevel]::Optimal,
            $false)
    } | Out-Null

    return $zip
}

<#
.SYNOPSIS
    Copia el módulo compartido al bin\ del instalador.
.DESCRIPTION
    Cada instalador se distribuye como un zip autónomo y necesita su copia. La
    fuente es setup\common\: dos copias editables acabarían divergiendo, y con
    ellas el significado de -DryRun en cada instalador.
#>
function Copy-ModuloComun {
    param([hashtable]$Paquete)

    if (-not (Test-Path -LiteralPath $Paquete.BinInstalador)) { return }

    Invoke-Accion "copiar EdgeGuard.Setup.psm1 → $($Paquete.BinInstalador)" {
        Copy-Item -LiteralPath $ModulePath -Destination $Paquete.BinInstalador -Force
    } | Out-Null
}

<#
.SYNOPSIS
    Escribe el checksums.sha256 de una carpeta data\.
.DESCRIPTION
    Formato "<HASH>  <nombre>", el que lee el paso 01 de cada instalador. Se
    recalcula sobre TODO lo que haya en data\, no solo sobre el zip recién hecho:
    el Hosting Bundle que acompaña al Hub también se verifica, y un archivo
    declarado de una versión anterior haría fallar la instalación siguiente.
#>
function New-Checksums {
    param([hashtable]$Paquete, [string]$Version)

    $archivo = Join-Path $Paquete.Destino 'checksums.sha256'

    if ($DryRun) {
        Write-Detalle "[dry-run] checksums.sha256 de $($Paquete.Destino)"
        return @{}
    }

    $hashes = [ordered]@{}
    $lineas = New-Object System.Collections.Generic.List[string]

    $lineas.Add("# EdgeGuard $($Paquete.Nombre) $Version")
    $lineas.Add("# Generado por setup\common\Seal-Version.ps1 el $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')")
    $lineas.Add('#')

    # Fuera el propio checksums, la documentación y los marcadores de git: declarar un
    # .gitkeep vacío no verifica nada y ensucia el archivo que el operador lee cuando una
    # instalación aborta por integridad.
    # Solo se firma lo que el instalador despliega. Un .psd1 es configuración de
    # sede, no artefacto: declararlo haría fallar la verificación de integridad en
    # cuanto alguien lo editara — que es precisamente para lo que está ahí.
    $artefactos = @(Get-ChildItem -LiteralPath $Paquete.Destino -File |
        Where-Object {
            $_.Name -ne 'checksums.sha256' -and
            $_.Extension -notin @('.md', '.psd1') -and
            -not $_.Name.StartsWith('.')
        } |
        Sort-Object Name)

    foreach ($artefacto in $artefactos) {
        $hash = (Get-FileHash -LiteralPath $artefacto.FullName -Algorithm SHA256).Hash.ToUpperInvariant()
        $lineas.Add("$hash  $($artefacto.Name)")
        $hashes[$artefacto.Name] = $hash
        Write-Detalle "$($artefacto.Name) — $([math]::Round($artefacto.Length / 1MB, 1)) MB"
    }

    [System.IO.File]::WriteAllLines($archivo, $lineas, (New-Object System.Text.UTF8Encoding $false))
    Write-Detalle "checksums.sha256 — $($artefactos.Count) artefacto(s)"

    return $hashes
}

<#
.SYNOPSIS
    El manifiesto del sellado: qué versión, de qué commit, cuándo y con qué hashes.
#>
function New-VersionManifest {
    param([string]$Version, [hashtable]$PorPaquete, [hashtable]$Origen)

    # El estado es el de ANTES de estampar: el propio sellado reescribe
    # Directory.Build.props y version.json, así que medirlo aquí dejaba
    # arbolLimpio en false en cualquier sellado que cambiara el número.
    $commit = $Origen.Commit
    $sucio  = $Origen.Sucio

    $manifiesto = [ordered]@{
        version    = $Version
        commit     = $commit
        # Un sellado desde un árbol con cambios sin confirmar no es reproducible:
        # queda anotado en vez de silenciado, para que se sepa al investigarlo.
        arbolLimpio = -not $sucio
        sellado    = (Get-Date).ToString('o')
        maquina    = $env:COMPUTERNAME
        paquetes   = $PorPaquete
    }

    $destino = Join-Path $SetupPath 'common\version.json'

    if ($DryRun) {
        Write-Detalle "[dry-run] version.json"
        return
    }

    $json = $manifiesto | ConvertTo-Json -Depth 6
    [System.IO.File]::WriteAllText($destino, $json, (New-Object System.Text.UTF8Encoding $false))
    Write-Detalle "version.json"

    if ($sucio) {
        Write-Host "    AVISO  El árbol tiene cambios sin confirmar: este sellado no es reproducible." -ForegroundColor Yellow
    }
}

<#
.SYNOPSIS
    Comprueba, ANTES de compilar nada, que se pueda etiquetar este sellado.
.DESCRIPTION
    Una etiqueta garantiza que el commit al que apunta contiene lo que se empaquetó.
    Eso se cumple solo si (1) el árbol estaba limpio al empezar, y (2) el número ya
    estaba estampado y confirmado: si el sellado lo estampa, el commit etiquetado
    tiene el número anterior.

    Se comprueba al inicio y no al final porque el sellado reescribe version.json
    (versionado) antes de terminar: medir el árbol al final lo da siempre por sucio,
    y -Tag fallaba después de haber compilado y empaquetado todo.
#>
function Assert-EtiquetaPosible {
    param([string]$Version, [string]$VersionActual, [hashtable]$Origen)

    if ($Origen.Sucio) {
        throw ("El árbol tiene cambios sin confirmar. Una etiqueta sobre un árbol sucio apunta a " +
               "un commit que NO contiene lo que se empaquetó, que es justo lo que la etiqueta " +
               "debería garantizar. Confirma los cambios y vuelve a ejecutar con -Tag.")
    }

    if ($Version -ne $VersionActual) {
        throw ("-Tag exige que el número ya esté estampado y confirmado ($VersionActual en " +
               "Directory.Build.props, se pidió $Version). Estampa y confirma primero (sellado " +
               "sin -Tag), y etiqueta después desde el commit ya integrado.")
    }

    $etiqueta = "v$Version"
    if (& git -C $RepoPath tag --list $etiqueta) {
        throw "La etiqueta '$etiqueta' ya existe. Sellar dos veces la misma versión deja dos paquetes distintos con el mismo número."
    }
}

<#
.SYNOPSIS
    Crea la etiqueta de git del sellado.
.DESCRIPTION
    Las precondiciones ya se comprobaron al inicio (Assert-EtiquetaPosible). HEAD no
    se movió durante el sellado, así que la etiqueta apunta al commit que se empaquetó.
#>
function New-EtiquetaGit {
    param([string]$Version)

    $etiqueta = "v$Version"

    Invoke-Accion "git tag $etiqueta" {
        & git -C $RepoPath tag -a $etiqueta -m "EdgeGuard $Version"
        if ($LASTEXITCODE -ne 0) { throw "git tag falló" }
    } | Out-Null

    Write-Host ""
    Write-Host "  Etiqueta $etiqueta creada en local. Publícala con: git push origin $etiqueta" -ForegroundColor Yellow
    Write-Host "  version.json quedó modificado: es el registro de este sellado; confírmalo en un cambio aparte." -ForegroundColor DarkGray
}

# ─────────────────────────────────────────────────────────────────────────────
# Principal
# ─────────────────────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "  ─────────────────────────────────────────────────────────────" -ForegroundColor Cyan
Write-Host "   EdgeGuard — sellado de versión" -ForegroundColor Cyan
Write-Host "  ─────────────────────────────────────────────────────────────" -ForegroundColor Cyan

$versionActual = Get-VersionActual

if ($Version) {
    Write-Host ""
    Write-Host ("   {0,-16} {1} → {2}" -f 'Versión', $versionActual, $Version)
}
else {
    $Version = $versionActual
    Write-Host ""
    Write-Host ("   {0,-16} {1}  (sin cambio: reempaquetado)" -f 'Versión', $Version)
}

Write-Host ("   {0,-16} {1}" -f 'Objetivo', $Target)
if ($DryRun) {
    Write-Host ("   {0,-16} {1}" -f 'Modo', 'DRY-RUN — no se escribe nada')
}

$seleccion = if ($Target -eq 'All') { $Paquetes.Keys } else { @($Target) }

# Estado de git ANTES de tocar nada. Lo que el sellado escribe después (el número,
# version.json) no cuenta como "cambios sin confirmar" del árbol que se empaqueta.
$origenCommit = try { (& git -C $RepoPath rev-parse HEAD).Trim() } catch { $null }
$origenSucio  = try { [bool](& git -C $RepoPath status --porcelain) } catch { $false }
$origen = @{ Commit = $origenCommit; Sucio = $origenSucio }

if ($Tag) {
    Assert-EtiquetaPosible -Version $Version -VersionActual $versionActual -Origen $origen
}

# ── 1. Estampar ──────────────────────────────────────────────────────────────
if ($Version -ne $versionActual) {
    Write-Paso '1 · Estampar el número'
    Set-VersionEnFuentes -Nueva $Version
}
else {
    Write-Paso '1 · Estampar el número'
    Write-Detalle "sin cambio ($Version)"
}

# ── 2-4. Publicar, empaquetar, firmar ────────────────────────────────────────
$resumen = [ordered]@{}

foreach ($clave in $seleccion) {
    $paquete = $Paquetes[$clave]

    Write-Paso "2 · $clave — publicar"
    $publicado = Publish-Proyecto -Paquete $paquete -Version $Version
    Assert-Contenido -Publicado $publicado -Requeridos $paquete.Requiere -Nombre $clave

    Write-Paso "3 · $clave — empaquetar"
    $zip = New-Paquete -Paquete $paquete -Publicado $publicado -Version $Version
    Copy-ModuloComun -Paquete $paquete

    Write-Paso "4 · $clave — firmar"
    $hashes = New-Checksums -Paquete $paquete -Version $Version

    $resumen[$clave] = [ordered]@{
        zip    = Split-Path -Leaf $zip
        hashes = $hashes
    }
}

# ── 5. Manifiesto ────────────────────────────────────────────────────────────
Write-Paso '5 · Manifiesto del sellado'
New-VersionManifest -Version $Version -PorPaquete $resumen -Origen $origen

# ── Etiqueta ─────────────────────────────────────────────────────────────────
if ($Tag) {
    Write-Paso '6 · Etiqueta de git'
    if ($DryRun) {
        Write-Detalle "[dry-run] git tag v$Version"
    }
    else {
        New-EtiquetaGit -Version $Version
    }
}

Write-Host ""
if ($DryRun) {
    Write-Host "  DRY-RUN completado. Nada se escribió." -ForegroundColor Yellow
}
else {
    Write-Host "  Versión $Version sellada." -ForegroundColor Green
    foreach ($clave in $seleccion) {
        Write-Host "    $clave → $((Get-Item -LiteralPath (Join-Path $Paquetes[$clave].Destino $resumen[$clave].zip)).FullName)" -ForegroundColor DarkGray
    }
}
Write-Host ""
