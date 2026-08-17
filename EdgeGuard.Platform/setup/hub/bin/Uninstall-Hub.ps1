#Requires -Version 5.1
<#
.SYNOPSIS
    Desinstala el Hub EdgeGuard de este servidor.

.DESCRIPTION
    Detiene y elimina el sitio y el app pool de IIS, retira la regla de firewall
    y borra los binarios desplegados.

    Por defecto PRESERVA los datos que no se pueden regenerar:

      · el key ring de Data Protection — sin él dejan de ser descifrables los
        SigningSecret de todos los nodos registrados;
      · workspace\reports\ — los PDF que Study.ReportPdfPath referencia;
      · logs\.

    -Purge los elimina también. Nunca toca la base de datos.

.NOTES
    Cuerpo pendiente: se implementa en la Fase 5.
#>
[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
param(
    [string]$SiteName    = 'EdgeGuard.Hub',
    [string]$AppPoolName = 'EdgeGuardHub',
    [string]$InstallPath = 'C:\inetpub\EdgeGuard\Hub',
    [switch]$Purge,
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot 'EdgeGuard.Setup.psm1') -Force

$logDir = Join-Path (Split-Path $PSScriptRoot -Parent) 'log'
Start-SetupLog -Directory $logDir -Prefix 'uninstall' -DryRun:$DryRun | Out-Null

Write-SetupLog "Sin implementar todavía (Fase 5)." -Level Warn
