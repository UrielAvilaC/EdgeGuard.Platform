#Requires -Version 5.1
<#
.SYNOPSIS
    Paso 09 — Reglas de firewall

.DESCRIPTION
    Abre los dos puertos que el nodo escucha: el DICOM, por el que reciben las
    modalidades, y el de su API, por el que entra el Hub.

    Aquí van DOS reglas donde el Hub tiene una. En el Hub, IIS registra la suya
    al crear el sitio y solo queda por abrir el listener MLLP, que vive dentro
    del proceso y que IIS no conoce. En el nodo no hay IIS: los dos sockets son
    del proceso y ninguno se abre solo.

    Ambas reglas se recrean en cada ejecución. Cambiar el puerto en el .psd1 y
    dejar la regla vieja apuntando al anterior deja el puerto nuevo cerrado y el
    viejo abierto sin nada detrás: los dos errores a la vez.
#>

$script:RuleDicom  = 'EdgeGuard Node - DICOM SCP'
$script:RuleNodeApi = 'EdgeGuard Node - API'

<#
.SYNOPSIS
    Crea (o recrea) una regla de entrada para un puerto TCP.
#>
function Set-NodeFirewallRule {
    [CmdletBinding()]
    param(
        [string]$DisplayName,
        [int]$Port,
        [string]$RemoteAddress,
        [string]$Descripcion
    )

    Invoke-SetupAction -Description "regla '$DisplayName' — TCP $Port desde $RemoteAddress" -Action {
        $previa = Get-NetFirewallRule -DisplayName $DisplayName -ErrorAction SilentlyContinue
        if ($previa) {
            Remove-NetFirewallRule -DisplayName $DisplayName -ErrorAction Stop
        }

        $parametros = @{
            DisplayName = $DisplayName
            Description = $Descripcion
            Direction   = 'Inbound'
            Action      = 'Allow'
            Protocol    = 'TCP'
            LocalPort   = $Port
            Profile     = 'Any'
            ErrorAction = 'Stop'
        }

        # 'Any' se omite en lugar de pasarse: New-NetFirewallRule sin
        # -RemoteAddress ya significa cualquier origen, y pasar la cadena 'Any'
        # funciona pero deja la regla mostrando un valor literal en la consola
        # de firewall, donde se lee como si fuera una restricción.
        if ($RemoteAddress -and $RemoteAddress -ne 'Any') {
            $parametros.RemoteAddress = ($RemoteAddress -split '\s*,\s*')
        }

        New-NetFirewallRule @parametros | Out-Null
    } | Out-Null
}

# ─────────────────────────────────────────────────────────────────────────────

function Step-NewFirewallRule {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][hashtable]$Config,
        [Parameter(Mandatory)][hashtable]$State
    )

    if ($Config.DicomRemoteAddress -eq 'Any') {
        Write-SetupLog ("El puerto DICOM $($Config.DicomPort) quedará abierto a CUALQUIER origen. " +
                        "DICOM es TCP sin TLS ni autenticación fuerte y transporta PHI: acótalo a " +
                        "las modalidades y al PACS con 'DicomRemoteAddress' en el .psd1.") -Level Warn
    }

    Set-NodeFirewallRule `
        -DisplayName $script:RuleDicom `
        -Port $Config.DicomPort `
        -RemoteAddress $Config.DicomRemoteAddress `
        -Descripcion 'Recepcion DICOM del nodo EdgeGuard (C-STORE, C-ECHO, MWL, Q/R)'

    if ($Config.NodeApiRemoteAddress -eq 'Any') {
        Write-SetupLog ("El puerto de la API $($Config.NodeApiPort) quedará abierto a cualquier " +
                        "origen. Solo lo consume el Hub: acotarlo a su dirección con " +
                        "'NodeApiRemoteAddress' es lo razonable.") -Level Detail
    }

    Set-NodeFirewallRule `
        -DisplayName $script:RuleNodeApi `
        -Port $Config.NodeApiPort `
        -RemoteAddress $Config.NodeApiRemoteAddress `
        -Descripcion 'API del nodo EdgeGuard (configuracion empujada por el Hub y health checks)'

    if (Test-SetupDryRun) { return }

    foreach ($nombre in @($script:RuleDicom, $script:RuleNodeApi)) {
        if (-not (Get-NetFirewallRule -DisplayName $nombre -ErrorAction SilentlyContinue)) {
            throw "La regla de firewall '$nombre' no quedó creada."
        }
    }

    Write-SetupLog "2 reglas de firewall verificadas" -Level Detail
}
