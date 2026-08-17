#Requires -Version 5.1
<#
.SYNOPSIS
    Paso 08 — Directorios de datos

.DESCRIPTION
    Crea logs\, workspace\reports\ y el directorio de Data Protection. Con el app pool en LocalSystem no hay trabajo de ACL, pero el directorio de llaves debe existir antes del primer arranque: el runtime lo crearía por su cuenta, y ese es justo el punto donde una identidad sin permiso sobre el padre falla.

.NOTES
    Cuerpo pendiente: se implementa en la Fase 4.
    El contrato ya es el definitivo — Config y State no cambiarán.
#>
function Step-NewDirectories {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][hashtable]$Config,
        [Parameter(Mandatory)][hashtable]$State
    )

    Write-SetupLog "Sin implementar todavía (Fase 4)." -Level Warn
}
