#Requires -Version 5.1
<#
.SYNOPSIS
    Paso 01 — Prerrequisitos y validación del paquete

.DESCRIPTION
    Verifica privilegios, sistema operativo, IIS y espacio en disco; resuelve el
    paquete dentro de data\, comprueba los checksums y valida el contenido del
    zip. Resuelve además el modo efectivo cuando -Mode es Auto.

    Corre completo también en -DryRun: es solo lectura, y detectar aquí un
    paquete corrupto o incompleto es justo donde sale barato.
#>

# Espacio mínimo libre en el volumen de instalación.
$script:MinFreeDiskGb = 5

<#
.SYNOPSIS
    Localiza el paquete: el indicado por configuración o el de mayor versión en data\.
#>
function Resolve-HubPackage {
    [CmdletBinding()]
    param([string]$Explicit, [string]$DataPath)

    if (-not [string]::IsNullOrWhiteSpace($Explicit)) {
        if (-not (Test-Path -LiteralPath $Explicit)) {
            throw "El paquete indicado no existe: $Explicit"
        }
        return (Resolve-Path -LiteralPath $Explicit).Path
    }

    if (-not (Test-Path -LiteralPath $DataPath)) {
        throw "No existe la carpeta data\. Deposita ahí el paquete .zip del Hub."
    }

    $candidates = @(Get-ChildItem -LiteralPath $DataPath -Filter '*.zip' -File -ErrorAction SilentlyContinue)
    if ($candidates.Count -eq 0) {
        throw "No hay ningún .zip en $DataPath. Consulta data\README.md para armar el paquete."
    }

    if ($candidates.Count -eq 1) { return $candidates[0].FullName }

    # Varios candidatos: se toma el de versión mayor y se deja constancia, para
    # que nadie descubra por accidente que instaló una versión distinta.
    $ranked = $candidates |
        ForEach-Object {
            $v = if ($_.Name -match '(\d+\.\d+(\.\d+)?(\.\d+)?)') { [version]$Matches[1] } else { [version]'0.0' }
            [pscustomobject]@{ File = $_; Version = $v }
        } |
        Sort-Object Version -Descending

    Write-SetupLog "Hay $($candidates.Count) paquetes en data\; se usará el de versión mayor:" -Level Warn
    foreach ($r in $ranked) { Write-SetupLog "$($r.File.Name)  ($($r.Version))" -Level Detail }

    return $ranked[0].File.FullName
}

<#
.SYNOPSIS
    Verifica los checksums declarados en data\checksums.sha256.
.DESCRIPTION
    Formato: "<HASH>  <nombre de archivo>", una línea por artefacto. Se verifica
    todo lo declarado que exista; un hash que no cuadra aborta la instalación.
#>
function Test-HubChecksums {
    [CmdletBinding()]
    param([string]$DataPath)

    $file = Join-Path $DataPath 'checksums.sha256'
    if (-not (Test-Path -LiteralPath $file)) {
        throw "Falta data\checksums.sha256. Sin él no se puede verificar la integridad del paquete."
    }

    $checked = 0
    foreach ($line in (Get-Content -LiteralPath $file)) {
        $trimmed = $line.Trim()
        if (-not $trimmed -or $trimmed.StartsWith('#')) { continue }

        if ($trimmed -notmatch '^([0-9A-Fa-f]{64})\s+(.+)$') {
            throw "Línea con formato inválido en checksums.sha256: '$trimmed'"
        }

        $expected = $Matches[1].ToUpperInvariant()
        $name     = $Matches[2].Trim()
        $target   = Join-Path $DataPath $name

        if (-not (Test-Path -LiteralPath $target)) {
            Write-SetupLog "Declarado en checksums pero ausente (se omite): $name" -Level Detail
            continue
        }

        $actual = Get-SetupFileHashSafe -Path $target
        if ($actual -ne $expected) {
            throw "Checksum incorrecto en '$name'. Esperado $expected, obtenido $actual. El paquete está corrupto o no es el que se declaró."
        }

        Write-SetupLog "checksum correcto: $name" -Level Detail
        $checked++
    }

    if ($checked -eq 0) {
        throw "checksums.sha256 no verificó ningún archivo. Revisa que los nombres coincidan con los de data\."
    }

    return $checked
}

<#
.SYNOPSIS
    Valida que el zip sea realmente un paquete del Hub y esté completo.
.DESCRIPTION
    La comprobación de wwwroot/index.html no es un extra: angular.json escribe
    su salida directamente en Hub.Api\wwwroot y el .csproj no tiene ningún
    target que dispare el build del SPA. Si dotnet publish corrió antes que
    npm run build, el paquete sale sin front y el sitio publica solo la API —
    algo que de otro modo solo se descubre al abrir el navegador.
#>
function Test-HubPackageContent {
    [CmdletBinding()]
    param([string]$PackagePath)

    Add-Type -AssemblyName System.IO.Compression.FileSystem -ErrorAction SilentlyContinue

    $required = @(
        'Dicom.Edge.Hub.Api.dll'
        'web.config'
        'wwwroot/index.html'
    )

    $zip = $null
    try {
        $zip = [System.IO.Compression.ZipFile]::OpenRead($PackagePath)
        $entries = @($zip.Entries | ForEach-Object { $_.FullName.Replace('\', '/') })

        $missing = @()
        foreach ($item in $required) {
            if (-not ($entries -contains $item)) { $missing += $item }
        }

        if ($missing -contains 'wwwroot/index.html') {
            throw ("El paquete no contiene wwwroot/index.html: se publicó sin el SPA. " +
                   "Ejecuta 'npm run build' en src\frontend\dicomedge-ui ANTES de 'dotnet publish' " +
                   "y vuelve a armar el zip (ver data\README.md).")
        }
        if ($missing.Count -gt 0) {
            throw "El paquete no parece un despliegue del Hub. Falta: $($missing -join ', ')"
        }

        $npgsql = $entries | Where-Object { $_ -eq 'Npgsql.dll' }
        if (-not $npgsql) {
            Write-SetupLog "El paquete no trae Npgsql.dll; el paso 04 sólo podrá comprobar el puerto." -Level Warn
        }

        return $entries.Count
    }
    finally {
        if ($zip) { $zip.Dispose() }
    }
}

<#
.SYNOPSIS
    Decide el modo efectivo cuando -Mode es Auto.
.DESCRIPTION
    La versión no se puede deducir del ensamblado: el .csproj no declara
    <Version>, así que Dicom.Edge.Hub.Api.dll siempre reporta 1.0.0.0. Por eso
    el paso 05 escribe installed.json y aquí se lee.
#>
function Resolve-HubMode {
    [CmdletBinding()]
    param([string]$Requested, [string]$InstallPath, [string]$PackagePath)

    if ($Requested -ne 'Auto') { return $Requested }

    $manifest = Join-Path $InstallPath 'installed.json'
    if (-not (Test-Path -LiteralPath $manifest)) {
        Write-SetupLog "No hay instalación previa (sin installed.json) → Install" -Level Detail
        return 'Install'
    }

    try {
        $installed = Get-Content -LiteralPath $manifest -Raw | ConvertFrom-Json
    }
    catch {
        Write-SetupLog "installed.json ilegible; se tratará como instalación nueva → Install" -Level Warn
        return 'Install'
    }

    $packageHash = Get-SetupFileHashSafe -Path $PackagePath

    if ($installed.PSObject.Properties.Name -contains 'PackageHash' -and
        $installed.PackageHash -eq $packageHash) {
        Write-SetupLog "El paquete instalado es idéntico al de data\ → Repair" -Level Detail
        return 'Repair'
    }

    $prev = if ($installed.PSObject.Properties.Name -contains 'Version') { $installed.Version } else { 'desconocida' }
    Write-SetupLog "Instalación previa (versión $prev) distinta del paquete → Update" -Level Detail
    return 'Update'
}

function Step-TestPrerequisites {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][hashtable]$Config,
        [Parameter(Mandatory)][hashtable]$State
    )

    # ── Privilegios ──────────────────────────────────────────────────────────
    if (-not (Test-SetupAdministrator)) {
        throw "Este instalador requiere una consola elevada (Ejecutar como administrador)."
    }
    Write-SetupLog "Sesión elevada" -Level Detail

    # ── Sistema operativo ────────────────────────────────────────────────────
    $os = Get-CimInstance Win32_OperatingSystem
    Write-SetupLog "$($os.Caption) ($($os.Version))" -Level Detail

    # ProductType: 1 = estación de trabajo, 2 = controlador de dominio, 3 = servidor
    if ($os.ProductType -eq 1) {
        Write-SetupLog ("Sistema operativo cliente, no Windows Server. La instalación puede continuar, " +
                        "pero Install-WindowsFeature no existe aquí y el paso 03 usará el mecanismo " +
                        "alternativo de características opcionales.") -Level Warn
    }

    # ── IIS ──────────────────────────────────────────────────────────────────
    $inetStp = 'HKLM:\SOFTWARE\Microsoft\InetStp'
    if (Test-Path $inetStp) {
        $iis = Get-ItemProperty $inetStp
        Write-SetupLog "IIS $($iis.MajorVersion).$($iis.MinorVersion) presente" -Level Detail
    }
    else {
        Write-SetupLog "IIS no está instalado; el paso 03 intentará habilitarlo." -Level Warn
    }

    # ── Disco ────────────────────────────────────────────────────────────────
    $qualifier = Split-Path -Qualifier $Config.InstallPath
    $drive     = Get-PSDrive -Name $qualifier.TrimEnd(':') -ErrorAction SilentlyContinue
    if ($drive) {
        $freeGb = [math]::Round($drive.Free / 1GB, 1)
        Write-SetupLog "Espacio libre en $qualifier $freeGb GB" -Level Detail
        if ($freeGb -lt $script:MinFreeDiskGb) {
            throw "Espacio insuficiente en $qualifier ($freeGb GB libres, se requieren $($script:MinFreeDiskGb) GB)."
        }
    }
    else {
        Write-SetupLog "No se pudo determinar el espacio libre de $qualifier" -Level Warn
    }

    # ── Checksums ────────────────────────────────────────────────────────────
    $verified = Test-HubChecksums -DataPath $State.DataPath
    Write-SetupLog "$verified artefacto(s) con checksum verificado" -Level Detail

    # ── Paquete ──────────────────────────────────────────────────────────────
    $package = Resolve-HubPackage -Explicit $Config.PackagePath -DataPath $State.DataPath
    $State['Package'] = $package
    Write-SetupLog "Paquete: $(Split-Path $package -Leaf)" -Level Detail

    $entryCount = Test-HubPackageContent -PackagePath $package
    Write-SetupLog "Contenido del paquete válido ($entryCount entradas, SPA incluido)" -Level Detail

    $State['PackageHash'] = Get-SetupFileHashSafe -Path $package
    $State['PackageVersion'] =
        if ((Split-Path $package -Leaf) -match '(\d+\.\d+(\.\d+)?(\.\d+)?)') { $Matches[1] } else { 'desconocida' }

    # ── Modo efectivo ────────────────────────────────────────────────────────
    $resolved = Resolve-HubMode -Requested $State.ResolvedMode -InstallPath $Config.InstallPath -PackagePath $package
    if ($resolved -ne $State.ResolvedMode) {
        Write-SetupLog "Modo resuelto: $($State.ResolvedMode) → $resolved" -Level Info
    }
    $State['ResolvedMode'] = $resolved
}
