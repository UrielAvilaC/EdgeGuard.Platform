#Requires -Version 5.1
<#
.SYNOPSIS
    Paso 09 — Regla de firewall HL7 MLLP

.DESCRIPTION
    Abre el puerto del listener MLLP para el origen configurado.

    El puerto HTTP del sitio NO se abre aquí: IIS registra sus propias reglas al
    crear el sitio, y duplicarlas sólo genera confusión al auditar el firewall.

    El listener MLLP, en cambio, es un socket TCP crudo que vive dentro del
    proceso del Hub y que IIS no conoce, así que necesita su regla explícita.
#>

$script:RuleName = 'EdgeGuard Hub - HL7 MLLP'

function Step-NewFirewallRule {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][hashtable]$Config,
        [Parameter(Mandatory)][hashtable]$State
    )

    if (-not $Config.Hl7Enabled) {
        Write-SetupLog "Listener MLLP deshabilitado; no se crea regla de firewall." -Level Detail

        # Si quedó una regla de una instalación anterior, deja el puerto abierto
        # sin nada escuchando. Se retira.
        if (-not (Test-SetupDryRun)) {
            $stale = Get-NetFirewallRule -DisplayName $script:RuleName -ErrorAction SilentlyContinue
            if ($stale) {
                Invoke-SetupAction -Description "eliminar la regla obsoleta '$($script:RuleName)'" -Action {
                    Remove-NetFirewallRule -DisplayName $script:RuleName -ErrorAction Stop
                } | Out-Null
            }
        }
        return
    }

    $remote = $Config.Hl7RemoteAddress

    if ($remote -eq 'Any') {
        Write-SetupLog ("El puerto MLLP $($Config.Hl7Port) quedará abierto a CUALQUIER origen. " +
                        "Es TCP crudo, sin TLS ni autenticación, y transporta PHI. " +
                        "Acótalo a la subred del HIS/RIS en cuanto sea posible.") -Level Warn
    }

    $existing = if (Test-SetupDryRun) { $null }
                else { Get-NetFirewallRule -DisplayName $script:RuleName -ErrorAction SilentlyContinue }

    if ($existing) {
        # Se actualiza en lugar de recrear, para no perder personalizaciones que
        # el administrador haya hecho sobre la regla (perfiles, interfaces).
        Invoke-SetupAction -Description "actualizar la regla '$($script:RuleName)' → puerto $($Config.Hl7Port), origen $remote" -Action {
            Set-NetFirewallRule -DisplayName $script:RuleName -Enabled True -ErrorAction Stop
            Get-NetFirewallRule -DisplayName $script:RuleName |
                Set-NetFirewallRule -RemoteAddress $remote -ErrorAction Stop
            Get-NetFirewallRule -DisplayName $script:RuleName |
                Get-NetFirewallPortFilter |
                Set-NetFirewallPortFilter -LocalPort $Config.Hl7Port -Protocol TCP -ErrorAction Stop
        } | Out-Null
    }
    else {
        Invoke-SetupAction -Description "crear la regla '$($script:RuleName)' → puerto $($Config.Hl7Port), origen $remote" -Action {
            New-NetFirewallRule -DisplayName $script:RuleName `
                                -Direction Inbound -Protocol TCP `
                                -LocalPort $Config.Hl7Port -RemoteAddress $remote `
                                -Action Allow -Enabled True `
                                -Description 'Listener MLLP del Hub EdgeGuard (HL7 v2 sobre TCP crudo)' `
                                -ErrorAction Stop | Out-Null
        } | Out-Null
    }

    if (Test-SetupDryRun) { return }

    $rule = Get-NetFirewallRule -DisplayName $script:RuleName -ErrorAction SilentlyContinue
    if (-not $rule) { throw "La regla de firewall '$($script:RuleName)' no existe tras crearla." }

    $port = $rule | Get-NetFirewallPortFilter
    Write-SetupLog "Regla activa: TCP $($port.LocalPort), origen $remote" -Level Detail
}
