#Requires -Version 5.1
<#
.SYNOPSIS
    Paso 03 — Características de IIS

.DESCRIPTION
    Habilita Web-Server, Web-WebSockets, compresión estática y dinámica, y logging HTTP. SignalR no arranca sin WebSockets, de modo que la ausencia de esa característica es un fallo, no un aviso.

.NOTES
    Cuerpo pendiente: se implementa en la Fase 3.
    El contrato ya es el definitivo — Config y State no cambiarán.
#>
function Step-EnableIisFeatures {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][hashtable]$Config,
        [Parameter(Mandatory)][hashtable]$State
    )

    Write-SetupLog "Sin implementar todavía (Fase 3)." -Level Warn
}
