#Requires -Version 5.1
<#
.SYNOPSIS
    Paso 09 — Regla de firewall HL7 MLLP

.DESCRIPTION
    Abre el puerto MLLP para el origen configurado. Con Any queda accesible desde cualquier red que alcance al servidor; el resumen previo a la confirmación lo advierte en cada instalación.

.NOTES
    Cuerpo pendiente: se implementa en la Fase 4.
    El contrato ya es el definitivo — Config y State no cambiarán.
#>
function Step-NewFirewallRule {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][hashtable]$Config,
        [Parameter(Mandatory)][hashtable]$State
    )

    Write-SetupLog "Sin implementar todavía (Fase 4)." -Level Warn
}
