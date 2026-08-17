#Requires -Version 5.1
<#
.SYNOPSIS
    Paso 06 — App pool y sitio IIS

.DESCRIPTION
    Crea el app pool en No Managed Code, startMode AlwaysRunning, idleTimeout en cero y la identidad configurada, y el sitio con binding HTTP. Sin TLS por diseño: si se requiere HTTPS se añade el binding a mano en IIS Manager.

.NOTES
    Cuerpo pendiente: se implementa en la Fase 4.
    El contrato ya es el definitivo — Config y State no cambiarán.
#>
function Step-NewIisSite {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][hashtable]$Config,
        [Parameter(Mandatory)][hashtable]$State
    )

    Write-SetupLog "Sin implementar todavía (Fase 4)." -Level Warn
}
