#Requires -Version 5.1
# Utilidades compartidas por las pruebas de los scripts de setup\common.
#
# Cada prueba arma su propio repositorio de juguete en %TEMP% y ejecuta ahí la COPIA del
# script bajo prueba: así ejercitan el script real sin tocar el repositorio ni el git de
# quien las corre. Sin red, sin dotnet y sin npm.

$script:Utf8ConBom = New-Object System.Text.UTF8Encoding $true
$script:Utf8SinBom = New-Object System.Text.UTF8Encoding $false

$script:RepoReal  = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$script:CommonReal = Join-Path $script:RepoReal 'setup\common'

function Write-ArchivoBom {
    param([string]$Path, [string]$Texto)
    $dir = Split-Path -Parent $Path
    if (-not (Test-Path -LiteralPath $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
    [System.IO.File]::WriteAllText($Path, $Texto, $script:Utf8ConBom)
}

function Write-ArchivoSinBom {
    param([string]$Path, [string]$Texto)
    $dir = Split-Path -Parent $Path
    if (-not (Test-Path -LiteralPath $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
    [System.IO.File]::WriteAllText($Path, $Texto, $script:Utf8SinBom)
}

function Get-Sha256Archivo {
    param([string]$Path)
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToUpperInvariant()
}

function New-DirectorioTemporal {
    $ruta = Join-Path ([System.IO.Path]::GetTempPath()) ("edgeguard-test-" + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $ruta -Force | Out-Null
    return $ruta
}

function Remove-DirectorioTemporal {
    param([string]$Path)
    if ($Path -and (Test-Path -LiteralPath $Path)) {
        Remove-Item -LiteralPath $Path -Recurse -Force -ErrorAction SilentlyContinue
    }
}

<#
.SYNOPSIS
    Ejecuta un script y devuelve el mensaje de su error, o $null si terminó bien.
.DESCRIPTION
    Pester 3.4 (el que trae Windows) y Pester 5 no comparten la sintaxis de
    'Should Throw'. Devolver el texto permite comprobarlo con 'Should Match' en ambos.
#>
function Invoke-ScriptCapturandoError {
    param([string]$Script, [hashtable]$Parametros = @{})

    try {
        # 6>$null: los scripts hablan por Write-Host y llenarían el informe de pruebas
        $null = & $Script @Parametros 6>$null
        return $null
    }
    catch {
        return $_.Exception.Message
    }
}

function Get-EntradasZip {
    param([string]$Zip)

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $z = [System.IO.Compression.ZipFile]::OpenRead($Zip)
    try { return @($z.Entries | ForEach-Object { $_.FullName.Replace('\', '/') }) }
    finally { $z.Dispose() }
}

function Read-EntradaZip {
    param([string]$Zip, [string]$Entrada)

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $z = [System.IO.Compression.ZipFile]::OpenRead($Zip)
    try {
        $e = $z.Entries | Where-Object { $_.FullName.Replace('\', '/') -eq $Entrada } | Select-Object -First 1
        if (-not $e) { return $null }
        $r = New-Object System.IO.StreamReader($e.Open())
        try { return $r.ReadToEnd() } finally { $r.Dispose() }
    }
    finally { $z.Dispose() }
}

<#
.SYNOPSIS
    Un repositorio de juguete con git, para probar Seal-Version.ps1.
#>
function New-RepoSellado {
    $raiz = New-DirectorioTemporal

    Write-ArchivoSinBom (Join-Path $raiz 'Directory.Build.props') "<Project><PropertyGroup><Version>1.0.0</Version></PropertyGroup></Project>`n"
    New-Item -ItemType Directory -Path (Join-Path $raiz 'setup\common') -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $script:CommonReal 'Seal-Version.ps1') -Destination (Join-Path $raiz 'setup\common')

    # Un repositorio por prueba, con identidad local: no depende de la configuración
    # global de quien corre las pruebas ni la modifica.
    & git -C $raiz init -q 2>&1 | Out-Null
    & git -C $raiz config user.name 'Prueba' | Out-Null
    & git -C $raiz config user.email 'prueba@example.invalid' | Out-Null
    & git -C $raiz config core.autocrlf false | Out-Null
    & git -C $raiz add -A | Out-Null
    & git -C $raiz commit -q -m 'base' | Out-Null

    return $raiz
}

<#
.SYNOPSIS
    Un repositorio de juguete con un nodo ya sellado, para probar New-DeliveryPackage.ps1.
.PARAMETER ExtraEnPaquete
    Nombre de un archivo a meter dentro del paquete sellado (para probar la auditoría).
#>
function New-RepoConNodoSellado {
    param([string]$Version = '9.9.9', [string]$ExtraEnPaquete)

    $raiz = New-DirectorioTemporal
    $setup = Join-Path $raiz 'setup'
    $nodo = Join-Path $setup 'node'

    Write-ArchivoSinBom (Join-Path $raiz 'Directory.Build.props') "<Project><PropertyGroup><Version>$Version</Version></PropertyGroup></Project>`n"

    # Módulo común y su copia en bin\ (idénticos, como los deja Seal-Version.ps1)
    $modulo = "# modulo comun de prueba`n"
    Write-ArchivoBom (Join-Path $setup 'common\EdgeGuard.Setup.psm1') $modulo
    Write-ArchivoBom (Join-Path $nodo 'bin\EdgeGuard.Setup.psm1') $modulo
    Copy-Item -LiteralPath (Join-Path $script:CommonReal 'New-DeliveryPackage.ps1') -Destination (Join-Path $setup 'common')

    # Instalador
    Write-ArchivoBom (Join-Path $nodo 'install.ps1') "# instalador`n"
    Write-ArchivoBom (Join-Path $nodo 'bin\01-Paso.ps1') "# paso 01 — con guion largo`n"
    Write-ArchivoSinBom (Join-Path $nodo 'bin\notas.txt') "no debe viajar`n"
    Write-ArchivoSinBom (Join-Path $nodo 'README.md') "# README`n"
    Write-ArchivoBom (Join-Path $nodo 'node-install.example.psd1') "@{ NodeName = 'EJEMPLO' }`n"

    # Configuración REAL de una sede: el caso que motivó el script
    Write-ArchivoBom (Join-Path $nodo 'node-install.psd1') "@{ BootstrapToken = 'SECRETO-DE-SEDE' }`n"

    # Paquete sellado: el .exe y el appsettings.Production.json en la raíz del zip
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $publicado = Join-Path $raiz 'publicado'
    New-Item -ItemType Directory -Path $publicado -Force | Out-Null
    Write-ArchivoSinBom (Join-Path $publicado 'Dicom.Edge.Node.exe') "exe`n"
    Write-ArchivoSinBom (Join-Path $publicado 'appsettings.Production.json') "{}`n"
    if ($ExtraEnPaquete) { Write-ArchivoSinBom (Join-Path $publicado $ExtraEnPaquete) "x`n" }

    $data = Join-Path $nodo 'data'
    New-Item -ItemType Directory -Path $data -Force | Out-Null
    $zip = Join-Path $data "edgeguard-node-$Version.zip"
    [System.IO.Compression.ZipFile]::CreateFromDirectory($publicado, $zip)
    Remove-Item -LiteralPath $publicado -Recurse -Force

    # Una versión anterior que sigue en data\ y NO debe viajar
    $zipViejo = Join-Path $data 'edgeguard-node-0.0.1.zip'
    Copy-Item -LiteralPath $zip -Destination $zipViejo
    Write-ArchivoSinBom (Join-Path $data 'README.md') "# data`n"

    $hash = Get-Sha256Archivo $zip
    $hashViejo = Get-Sha256Archivo $zipViejo
    Write-ArchivoSinBom (Join-Path $data 'checksums.sha256') ("# firmado`n$hashViejo  edgeguard-node-0.0.1.zip`n$hash  edgeguard-node-$Version.zip`n")

    Write-ArchivoSinBom (Join-Path $nodo 'manuales\Nodo-Manual.pdf') "%PDF-1.4 prueba`n"

    $manifiesto = [ordered]@{
        version     = $Version
        commit      = 'abc123'
        arbolLimpio = $true
        sellado     = '2026-01-01T00:00:00.0000000+00:00'
        maquina     = 'PRUEBA'
        paquetes    = [ordered]@{
            Node = [ordered]@{
                zip    = "edgeguard-node-$Version.zip"
                hashes = [ordered]@{ "edgeguard-node-$Version.zip" = $hash }
            }
        }
    }
    Write-ArchivoSinBom (Join-Path $setup 'common\version.json') ($manifiesto | ConvertTo-Json -Depth 6)

    return $raiz
}

function Set-Manifiesto {
    param([string]$Raiz, [scriptblock]$Cambio)

    $ruta = Join-Path $Raiz 'setup\common\version.json'
    $m = Get-Content -LiteralPath $ruta -Raw | ConvertFrom-Json
    & $Cambio $m
    Write-ArchivoSinBom $ruta ($m | ConvertTo-Json -Depth 6)
}
