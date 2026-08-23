#Requires -Version 5.1
<#
.SYNOPSIS
    Desinstala el Hub EdgeGuard de este servidor.

.DESCRIPTION
    Detiene y elimina el sitio y el app pool de IIS, retira la regla de firewall
    del listener MLLP y borra los binarios desplegados.

    Por defecto PRESERVA lo que no se puede regenerar:

      · el key ring de Data Protection — sin él dejan de ser descifrables los
        SigningSecret de todos los nodos registrados, y cada uno tendría que
        volver a autenticarse contra el Hub para que se le recomponga;
      · workspace\reports\ — los PDF que Study.ReportPdfPath referencia;
      · logs\.

    -Purge los elimina también. Nunca toca la base de datos: los datos clínicos
    no son del instalador.

.PARAMETER Purge
    Elimina también los directorios de datos. Irreversible.

.EXAMPLE
    .\Uninstall-Hub.ps1 -DryRun
    Muestra qué se eliminaría, sin tocar nada.
#>
[CmdletBinding()]
param(
    [string]$SiteName              = 'EdgeGuard.Hub',
    [string]$AppPoolName           = 'EdgeGuardHub',
    [string]$InstallPath           = 'C:\inetpub\EdgeGuard\Hub',
    [string]$DataProtectionKeyPath = 'C:\inetpub\edgeguard\dp-keys',
    [switch]$Purge,
    [switch]$NonInteractive,
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot 'EdgeGuard.Setup.psm1') -Force

$logDir  = Join-Path (Split-Path $PSScriptRoot -Parent) 'log'
$logFile = Start-SetupLog -Directory $logDir -Prefix 'uninstall' -DryRun:$DryRun

$FirewallRuleName    = 'EdgeGuard Hub - HL7 MLLP'
$PreservedDirectories = @('workspace', 'logs')

try {
    if (-not (Test-SetupAdministrator)) {
        throw "La desinstalación requiere una consola elevada (Ejecutar como administrador)."
    }

    # ── Resumen y confirmación ───────────────────────────────────────────────
    Write-Host ""
    Write-Host "  Se va a desinstalar el Hub EdgeGuard" -ForegroundColor Yellow
    Write-Host ""
    Write-Host ("   {0,-26} {1}" -f 'Sitio IIS',    $SiteName)
    Write-Host ("   {0,-26} {1}" -f 'App pool',     $AppPoolName)
    Write-Host ("   {0,-26} {1}" -f 'Directorio',   $InstallPath)
    Write-Host ("   {0,-26} {1}" -f 'Regla firewall', $FirewallRuleName)
    Write-Host ""

    if ($Purge) {
        Write-Host "   -Purge ACTIVO — se eliminarán también:" -ForegroundColor Red
        Write-Host "     · $DataProtectionKeyPath" -ForegroundColor Red
        Write-Host "       Los SigningSecret de todos los nodos dejarán de ser descifrables." -ForegroundColor Red
        Write-Host "     · $InstallPath\workspace  (PDF de reportes)" -ForegroundColor Red
        Write-Host "     · $InstallPath\logs" -ForegroundColor Red
    }
    else {
        Write-Host "   Se conservan el key ring, workspace\ y logs\." -ForegroundColor Gray
    }
    Write-Host ""
    Write-Host "   La base de datos NO se toca." -ForegroundColor Gray
    Write-Host ""

    if (-not $NonInteractive -and -not $DryRun) {
        $answer = Read-Host "  Escribe DESINSTALAR para confirmar"
        if ($answer -cne 'DESINSTALAR') {
            Write-SetupLog "Cancelado por el operador." -Level Warn
            exit 2
        }
    }

    Import-Module WebAdministration -ErrorAction SilentlyContinue

    # ── Sitio ────────────────────────────────────────────────────────────────
    Invoke-SetupStep -Id '01' -Description 'Detener y eliminar el sitio' -Body {
        if (Test-Path "IIS:\Sites\$SiteName") {
            Invoke-SetupAction -Description "eliminar el sitio $SiteName" -Action {
                if ((Get-WebsiteState -Name $SiteName).Value -eq 'Started') {
                    Stop-Website -Name $SiteName -ErrorAction SilentlyContinue
                }
                Remove-Website -Name $SiteName -ErrorAction Stop
            } | Out-Null
        }
        else { Write-SetupLog "el sitio $SiteName no existe" -Level Detail }
    } | Out-Null

    # ── App pool ─────────────────────────────────────────────────────────────
    Invoke-SetupStep -Id '02' -Description 'Detener y eliminar el app pool' -Body {
        if (Test-Path "IIS:\AppPools\$AppPoolName") {
            Invoke-SetupAction -Description "eliminar el app pool $AppPoolName" -Action {
                if ((Get-WebAppPoolState -Name $AppPoolName).Value -eq 'Started') {
                    Stop-WebAppPool -Name $AppPoolName -ErrorAction SilentlyContinue
                    $deadline = (Get-Date).AddSeconds(30)
                    while ((Get-WebAppPoolState -Name $AppPoolName).Value -ne 'Stopped' -and (Get-Date) -lt $deadline) {
                        Start-Sleep -Milliseconds 500
                    }
                }
                Remove-WebAppPool -Name $AppPoolName -ErrorAction Stop
            } | Out-Null
        }
        else { Write-SetupLog "el app pool $AppPoolName no existe" -Level Detail }
    } | Out-Null

    # ── Firewall ─────────────────────────────────────────────────────────────
    Invoke-SetupStep -Id '03' -Description 'Retirar la regla de firewall' -Body {
        $rule = Get-NetFirewallRule -DisplayName $FirewallRuleName -ErrorAction SilentlyContinue
        if ($rule) {
            Invoke-SetupAction -Description "eliminar la regla '$FirewallRuleName'" -Action {
                Remove-NetFirewallRule -DisplayName $FirewallRuleName -ErrorAction Stop
            } | Out-Null
        }
        else { Write-SetupLog "la regla no existe" -Level Detail }
    } | Out-Null

    # ── Archivos ─────────────────────────────────────────────────────────────
    Invoke-SetupStep -Id '04' -Description 'Eliminar los binarios desplegados' -Body {
        if (-not (Test-Path -LiteralPath $InstallPath)) {
            Write-SetupLog "$InstallPath no existe" -Level Detail
            return
        }

        foreach ($item in Get-ChildItem -LiteralPath $InstallPath -Force) {
            if (-not $Purge -and $item.PSIsContainer -and $PreservedDirectories -contains $item.Name) {
                Write-SetupLog "se conserva: $($item.Name)\" -Level Detail
                continue
            }
            Invoke-SetupAction -Description "eliminar $($item.Name)" -Action {
                Remove-Item -LiteralPath $item.FullName -Recurse -Force -ErrorAction Stop
            } | Out-Null
        }
    } | Out-Null

    # ── Key ring ─────────────────────────────────────────────────────────────
    Invoke-SetupStep -Id '05' -Description 'Key ring de Data Protection' -Body {
        if (-not $Purge) {
            Write-SetupLog "se conserva $DataProtectionKeyPath (los nodos dependen de él)" -Level Detail
            return
        }
        if (Test-Path -LiteralPath $DataProtectionKeyPath) {
            Invoke-SetupAction -Description "eliminar $DataProtectionKeyPath" -Action {
                Remove-Item -LiteralPath $DataProtectionKeyPath -Recurse -Force -ErrorAction Stop
            } | Out-Null
        }
    } | Out-Null

    Write-SetupLog ""
    Write-SetupLog "Desinstalación completada. Registro: $logFile" -Level Ok

    if (-not $Purge) {
        Write-SetupLog "Se conservaron el key ring, workspace\ y logs\. Usa -Purge para eliminarlos." -Level Detail
    }
    Write-SetupLog "La base de datos no se modificó." -Level Detail

    exit 0
}
catch {
    Write-SetupLog ""
    Write-SetupLog "DESINSTALACIÓN ABORTADA: $($_.Exception.Message)" -Level Error
    Write-SetupLog "Registro completo: $logFile" -Level Detail
    exit 1
}
