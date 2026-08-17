#Requires -Version 5.1
<#
.SYNOPSIS
    Paso 05 — Despliegue del paquete

.DESCRIPTION
    Respalda la versión previa, descomprime el paquete en InstallPath y preserva workspace\ y logs\: ahí viven los PDF que Study.ReportPdfPath referencia, y reemplazar la carpeta a lo bruto deja los estudios existentes apuntando a archivos que ya no están. Escribe el manifiesto installed.json con versión, timestamp y hash del paquete.

.NOTES
    Cuerpo pendiente: se implementa en la Fase 4.
    El contrato ya es el definitivo — Config y State no cambiarán.
#>
function Step-DeployPackage {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][hashtable]$Config,
        [Parameter(Mandatory)][hashtable]$State
    )

    Write-SetupLog "Sin implementar todavía (Fase 4)." -Level Warn
}
