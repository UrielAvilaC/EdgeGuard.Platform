#Requires -Version 5.1
<#
.SYNOPSIS
    Paso 10 — Verificación post-instalación

.DESCRIPTION
    Arranca el sitio, espera a que /health responda, extrae el bootstrap token del log y lo muestra una sola vez. Verifica además que el log de arranque confirme que Data Protection persiste llaves: si aparece el aviso de llaves efímeras el paso falla, porque esa degradación solo deja un LogWarning y rompe los SigningSecret de todos los nodos en el siguiente reciclaje del app pool.

.NOTES
    Cuerpo pendiente: se implementa en la Fase 5.
    El contrato ya es el definitivo — Config y State no cambiarán.
#>
function Step-TestInstallation {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][hashtable]$Config,
        [Parameter(Mandatory)][hashtable]$State
    )

    Write-SetupLog "Sin implementar todavía (Fase 5)." -Level Warn
}
