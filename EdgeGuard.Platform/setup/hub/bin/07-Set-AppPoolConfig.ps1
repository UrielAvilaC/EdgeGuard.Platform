#Requires -Version 5.1
<#
.SYNOPSIS
    Paso 07 — Variables de entorno del app pool

.DESCRIPTION
    Escribe la configuración como variables de entorno del app pool. appsettings.Production.json queda vacío a propósito, de modo que el estado vive en IIS y una actualización nunca tiene que fusionar archivos de configuración. Genera Jwt__SecretKey con RNG si no existe, y lo conserva si ya lo hay: rotarlo invalidaría todas las sesiones activas.

.NOTES
    Cuerpo pendiente: se implementa en la Fase 4.
    El contrato ya es el definitivo — Config y State no cambiarán.
#>
function Step-SetAppPoolConfig {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][hashtable]$Config,
        [Parameter(Mandatory)][hashtable]$State
    )

    Write-SetupLog "Sin implementar todavía (Fase 4)." -Level Warn
}
