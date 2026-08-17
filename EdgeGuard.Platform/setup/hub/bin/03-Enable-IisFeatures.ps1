#Requires -Version 5.1
<#
.SYNOPSIS
    Paso 03 — Características de IIS

.DESCRIPTION
    Habilita IIS y los módulos que el Hub necesita. WebSockets no es opcional:
    SignalR no negocia sin él y el monitoreo en tiempo real del SPA queda mudo,
    con la particularidad de que el resto del sitio funciona — un fallo que se
    diagnostica tarde y mal.

    En Windows Server usa Install-WindowsFeature; en sistemas cliente ese cmdlet
    no existe y se recurre a Enable-WindowsOptionalFeature, que nombra las
    mismas características de otra forma.
#>

# Nombre en Windows Server → nombre en sistemas cliente (DISM).
$script:FeatureMap = [ordered]@{
    'Web-Server'             = 'IIS-WebServer'
    'Web-WebSockets'         = 'IIS-WebSockets'
    'Web-Stat-Compression'   = 'IIS-HttpCompressionStatic'
    'Web-Dyn-Compression'    = 'IIS-HttpCompressionDynamic'
    'Web-Http-Logging'       = 'IIS-HttpLogging'
    'Web-Mgmt-Console'       = 'IIS-ManagementConsole'
}

# Sin esta, SignalR no funciona. Se distingue del resto para poder fallar
# explícitamente en vez de dejar un aviso que nadie lee.
$script:CriticalFeature = 'Web-WebSockets'

function Test-IsWindowsServer {
    return ((Get-CimInstance Win32_OperatingSystem).ProductType -ne 1)
}

function Enable-FeatureOnServer {
    [CmdletBinding()]
    param([string]$Name)

    $feature = Get-WindowsFeature -Name $Name -ErrorAction SilentlyContinue
    if (-not $feature) {
        Write-SetupLog "Característica desconocida en este servidor: $Name" -Level Warn
        return $false
    }
    if ($feature.Installed) {
        Write-SetupLog "ya presente: $Name" -Level Detail
        return $true
    }

    $result = Invoke-SetupAction -Description "habilitar $Name" -Action {
        Install-WindowsFeature -Name $Name -IncludeManagementTools -ErrorAction Stop
    }

    if (Test-SetupDryRun) { return $true }

    if ($result.RestartNeeded -eq 'Yes') {
        Write-SetupLog "$Name habilitada; el servidor requiere reinicio." -Level Warn
    }
    return $result.Success
}

function Enable-FeatureOnClient {
    [CmdletBinding()]
    param([string]$DismName)

    $feature = Get-WindowsOptionalFeature -Online -FeatureName $DismName -ErrorAction SilentlyContinue
    if (-not $feature) {
        Write-SetupLog "Característica desconocida en este sistema: $DismName" -Level Warn
        return $false
    }
    if ($feature.State -eq 'Enabled') {
        Write-SetupLog "ya presente: $DismName" -Level Detail
        return $true
    }

    Invoke-SetupAction -Description "habilitar $DismName" -Action {
        Enable-WindowsOptionalFeature -Online -FeatureName $DismName -All -NoRestart -ErrorAction Stop
    } | Out-Null

    return $true
}

function Step-EnableIisFeatures {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][hashtable]$Config,
        [Parameter(Mandatory)][hashtable]$State
    )

    $isServer = Test-IsWindowsServer
    Write-SetupLog $(if ($isServer) { 'Windows Server: se usará Install-WindowsFeature' }
                     else { 'Sistema cliente: se usará Enable-WindowsOptionalFeature' }) -Level Detail

    $failed = @()

    foreach ($serverName in $script:FeatureMap.Keys) {
        $ok =
            if ($isServer) { Enable-FeatureOnServer -Name $serverName }
            else           { Enable-FeatureOnClient -DismName $script:FeatureMap[$serverName] }

        if (-not $ok) { $failed += $serverName }
    }

    if ($failed -contains $script:CriticalFeature) {
        throw ("No se pudo habilitar $($script:CriticalFeature). SignalR no negocia sin WebSockets " +
               "y el monitoreo en tiempo real quedaría mudo mientras el resto del sitio parece sano.")
    }

    if ($failed.Count -gt 0) {
        Write-SetupLog "Características no habilitadas: $($failed -join ', ')" -Level Warn
    }

    # Habilitar la característica a nivel de servidor no basta: el sitio también
    # debe permitir WebSockets. Eso se aplica en el paso 06, al crearlo.
    $State['WebSocketsPending'] = $true
}
