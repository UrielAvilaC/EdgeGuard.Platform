#Requires -Version 5.1
<#
.SYNOPSIS
    Paso 04 — Conectividad y permisos de PostgreSQL

.DESCRIPTION
    El rol y la base ya existen: este paso solo verifica. Comprueba el puerto, autentica de verdad cargando Npgsql.dll del propio paquete (así no hace falta psql ni cliente alguno en el servidor) y confirma que el rol tenga CREATE en el esquema, porque el Hub aplica las migraciones de EF al arrancar. Reporta si la base está vacía o ya migrada.

.NOTES
    Cuerpo pendiente: se implementa en la Fase 3.
    El contrato ya es el definitivo — Config y State no cambiarán.
#>
function Step-TestDatabase {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][hashtable]$Config,
        [Parameter(Mandatory)][hashtable]$State
    )

    Write-SetupLog "Sin implementar todavía (Fase 3)." -Level Warn
}
