#Requires -Version 5.1
<#
.SYNOPSIS
    Paso 05 — Despliegue del paquete

.DESCRIPTION
    Detiene el app pool, respalda la instalación previa, descomprime el paquete
    y restaura los directorios de datos. Escribe installed.json como manifiesto.

    Dos cosas que este paso NO puede hacer mal, porque no tienen vuelta atrás:

      · workspace\ guarda los PDF de reportes que Study.ReportPdfPath
        referencia. Reemplazar el directorio a lo bruto deja los estudios
        existentes apuntando a archivos que ya no están.
      · logs\ es la única copia del bootstrap token de la instalación previa.

    Ambos se preservan siempre. El respaldo completo queda en State.Backup para
    que el maestro pueda revertir si un paso posterior falla.
#>

# Directorios que sobreviven a una actualización.
$script:PreservedDirectories = @('workspace', 'logs')

function Stop-HubAppPool {
    [CmdletBinding()]
    param([string]$AppPoolName)

    if (-not (Get-Module -ListAvailable -Name WebAdministration)) { return $false }
    Import-Module WebAdministration -ErrorAction SilentlyContinue

    $poolPath = "IIS:\AppPools\$AppPoolName"
    if (-not (Test-Path $poolPath)) { return $false }

    $state = (Get-ItemProperty $poolPath -Name state -ErrorAction SilentlyContinue).Value
    if ($state -ne 'Started') { return $false }

    Invoke-SetupAction -Description "detener el app pool $AppPoolName" -Action {
        Stop-WebAppPool -Name $AppPoolName -ErrorAction Stop
        # w3wp tarda en soltar los DLL; sin esto la descompresión falla por
        # archivos bloqueados, y el error resultante no señala la causa.
        $deadline = (Get-Date).AddSeconds(30)
        while ((Get-WebAppPoolState -Name $AppPoolName).Value -ne 'Stopped' -and (Get-Date) -lt $deadline) {
            Start-Sleep -Milliseconds 500
        }
    } | Out-Null

    return $true
}

<#
.SYNOPSIS
    Copia la instalación previa a un directorio de respaldo con marca de tiempo.
#>
function Backup-HubInstallation {
    [CmdletBinding()]
    param([string]$InstallPath)

    if (-not (Test-Path -LiteralPath $InstallPath)) { return $null }

    $items = @(Get-ChildItem -LiteralPath $InstallPath -Force -ErrorAction SilentlyContinue)
    if ($items.Count -eq 0) { return $null }

    $backup = "$InstallPath.backup-$(Get-Date -Format 'yyyyMMdd-HHmmss')"

    Invoke-SetupAction -Description "respaldar la instalación previa en $backup" -Action {
        Copy-Item -LiteralPath $InstallPath -Destination $backup -Recurse -Force -ErrorAction Stop
    } | Out-Null

    return $backup
}

<#
.SYNOPSIS
    Vacía el directorio de instalación conservando los directorios de datos.
#>
function Clear-HubInstallation {
    [CmdletBinding()]
    param([string]$InstallPath)

    if (-not (Test-Path -LiteralPath $InstallPath)) { return }

    foreach ($item in Get-ChildItem -LiteralPath $InstallPath -Force) {
        if ($item.PSIsContainer -and $script:PreservedDirectories -contains $item.Name) {
            Write-SetupLog "se conserva: $($item.Name)\" -Level Detail
            continue
        }

        Invoke-SetupAction -Description "eliminar $($item.Name)" -Action {
            Remove-Item -LiteralPath $item.FullName -Recurse -Force -ErrorAction Stop
        } | Out-Null
    }
}

function Expand-HubPackage {
    [CmdletBinding()]
    param([string]$PackagePath, [string]$InstallPath)

    Invoke-SetupAction -Description "descomprimir $(Split-Path $PackagePath -Leaf) en $InstallPath" -Action {
        if (-not (Test-Path -LiteralPath $InstallPath)) {
            New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null
        }
        # -Force para que el paquete pise los binarios; los directorios
        # preservados ya se excluyeron al vaciar.
        Expand-Archive -LiteralPath $PackagePath -DestinationPath $InstallPath -Force -ErrorAction Stop
    } | Out-Null
}

<#
.SYNOPSIS
    Escribe el manifiesto que 'Auto' leerá en la próxima ejecución.
#>
function Write-HubManifest {
    [CmdletBinding()]
    param([string]$InstallPath, [hashtable]$State, [string]$Mode)

    $manifest = [ordered]@{
        Version     = $State.PackageVersion
        PackageFile = Split-Path $State.Package -Leaf
        PackageHash = $State.PackageHash
        InstalledAt = (Get-Date).ToString('o')
        InstalledBy = "$env:USERDOMAIN\$env:USERNAME"
        Mode        = $Mode
        Machine     = $env:COMPUTERNAME
    }

    Invoke-SetupAction -Description "escribir installed.json" -Action {
        $json = $manifest | ConvertTo-Json
        [System.IO.File]::WriteAllText(
            (Join-Path $InstallPath 'installed.json'), $json, (New-Object System.Text.UTF8Encoding($false)))
    } | Out-Null
}

function Step-DeployPackage {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][hashtable]$Config,
        [Parameter(Mandatory)][hashtable]$State
    )

    $installPath = $Config.InstallPath

    $stopped = Stop-HubAppPool -AppPoolName $Config.AppPoolName
    if ($stopped) { Write-SetupLog "App pool detenido para liberar los binarios" -Level Detail }

    $backup = Backup-HubInstallation -InstallPath $installPath
    if ($backup) {
        $State['Backup'] = $backup
        Write-SetupLog "Respaldo: $backup" -Level Detail
    }
    else {
        Write-SetupLog "Sin instalación previa que respaldar" -Level Detail
    }

    Clear-HubInstallation -InstallPath $installPath
    Expand-HubPackage -PackagePath $State.Package -InstallPath $installPath
    Write-HubManifest -InstallPath $installPath -State $State -Mode $State.ResolvedMode

    if (Test-SetupDryRun) { return }

    # ── Postcondición ────────────────────────────────────────────────────────
    # Se verifica el SPA además del binario: es lo que distingue un despliegue
    # completo de uno que servirá solo la API.
    foreach ($required in @('Dicom.Edge.Hub.Api.dll', 'web.config', 'wwwroot\index.html')) {
        $path = Join-Path $installPath $required
        if (-not (Test-Path -LiteralPath $path)) {
            throw "Tras descomprimir falta '$required' en $installPath. El despliegue está incompleto."
        }
    }

    Write-SetupLog "Desplegado $($State.PackageVersion) en $installPath" -Level Detail
}
