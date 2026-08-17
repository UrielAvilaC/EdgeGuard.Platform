#Requires -Version 5.1
<#
.SYNOPSIS
    Paso 01 — Prerrequisitos y validación del paquete

.DESCRIPTION
    Verifica privilegios de administrador, versión del sistema operativo, presencia de IIS y espacio en disco. Valida los checksums de data\ y que el paquete contenga Dicom.Edge.Hub.Api.dll y wwwroot\index.html: un zip publicado antes de compilar el SPA sale sin front, y sin esta comprobación solo se detecta al abrir el navegador. Resuelve además el paquete y el modo efectivo cuando -Mode es Auto.

.NOTES
    Cuerpo pendiente: se implementa en la Fase 3.
    El contrato ya es el definitivo — Config y State no cambiarán.
#>
function Step-TestPrerequisites {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][hashtable]$Config,
        [Parameter(Mandatory)][hashtable]$State
    )

    Write-SetupLog "Sin implementar todavía (Fase 3)." -Level Warn
}
