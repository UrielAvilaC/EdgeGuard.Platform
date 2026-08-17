#Requires -Version 5.1
<#
.SYNOPSIS
    Paso 02 — ASP.NET Core Hosting Bundle

.DESCRIPTION
    Si ya está instalado no hace nada. Si falta y está en data\, verifica el checksum e instala en silencio, tratando el codigo de salida 3010 como reinicio pendiente. Si falta y no está en data\, descarga de la URL oficial fijada en el script y verifica el hash antes de ejecutar nada; requiere AllowHostingBundleDownload.

.NOTES
    Cuerpo pendiente: se implementa en la Fase 3.
    El contrato ya es el definitivo — Config y State no cambiarán.
#>
function Step-InstallHostingBundle {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][hashtable]$Config,
        [Parameter(Mandatory)][hashtable]$State
    )

    Write-SetupLog "Sin implementar todavía (Fase 3)." -Level Warn
}
