#Requires -Version 5.1
<#
.SYNOPSIS
    Desinstala el nodo EdgeGuard de este servidor.

.DESCRIPTION
    Detiene y elimina el servicio, retira las reglas de firewall y borra los
    binarios. Los datos del nodo —base, estudios y registros— se CONSERVAN salvo
    que se pida lo contrario.

    Conservarlos por defecto es deliberado. La desinstalación más frecuente no es
    un retiro: es un paso de una reinstalación o de una migración de servidor, y
    en la base está el historial de qué estudios se entregaron al PACS. Borrarla
    sin pedirlo convertiría una reinstalación rutinaria en una pérdida.

.PARAMETER ServiceName
    Nombre del servicio. Por defecto 'EdgeGuardNode'.

.PARAMETER InstallPath
    Directorio de binarios a borrar. Por defecto se lee de installed.json si se
    encuentra junto a este script; si no, 'C:\EdgeGuard\Node'.

.PARAMETER PurgeData
    Borra también la base, los estudios y los registros. Pide confirmación
    escrita salvo con -Force.

.PARAMETER Force
    No pregunta nada. Con -PurgeData, borra los datos sin confirmar.

.PARAMETER DryRun
    Dice qué haría sin tocar nada.

.EXAMPLE
    .\Uninstall-Node.ps1 -DryRun

.EXAMPLE
    .\Uninstall-Node.ps1 -PurgeData
    Retiro definitivo del nodo de esta sede.
#>
[CmdletBinding()]
param(
    [string]$ServiceName = 'EdgeGuardNode',
    [string]$InstallPath,
    [switch]$PurgeData,
    [switch]$Force,
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$BinPath = $PSScriptRoot
$RootPath = Split-Path -Parent $BinPath
$LogDir   = Join-Path $RootPath 'log'

Import-Module (Join-Path $BinPath 'EdgeGuard.Setup.psm1') -Force

$RuleDicom  = 'EdgeGuard Node - DICOM SCP'
$RuleNodeApi = 'EdgeGuard Node - API'

<#
.SYNOPSIS
    Lee installed.json para saber qué se instaló y dónde.
.DESCRIPTION
    Es lo que permite desinstalar sin que el operador tenga que recordar rutas.
    Si no está, se cae a los valores por defecto y se avisa: borrar el directorio
    equivocado es el error que no se puede deshacer.
#>
function Read-InstalledManifest {
    [CmdletBinding()]
    param([string]$InstallPath)

    $ruta = Join-Path $InstallPath 'installed.json'
    if (-not (Test-Path -LiteralPath $ruta)) { return $null }

    try   { return (Get-Content -LiteralPath $ruta -Raw | ConvertFrom-Json) }
    catch {
        Write-SetupLog "installed.json existe pero no se pudo leer: $($_.Exception.Message)" -Level Warn
        return $null
    }
}

function Remove-NodeServiceCompletely {
    [CmdletBinding()]
    param([string]$ServiceName)

    $servicio = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if (-not $servicio) {
        Write-SetupLog "el servicio '$ServiceName' no existe" -Level Detail
        return
    }

    Invoke-SetupAction -Description "detener y eliminar el servicio $ServiceName" -Action {
        if ($servicio.Status -ne 'Stopped') {
            Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue

            # Se espera al proceso, no al estado: el SCM marca 'Stopped' al
            # aceptar la señal, y borrar el directorio con el proceso vivo deja
            # archivos bloqueados y una desinstalación a medias.
            $limite = (Get-Date).AddSeconds(45)
            while ((Get-Date) -lt $limite) {
                if (-not (Get-Process -Name 'Dicom.Edge.Node' -ErrorAction SilentlyContinue)) { break }
                Start-Sleep -Milliseconds 500
            }
        }

        & sc.exe delete $ServiceName | Out-Null

        $limite = (Get-Date).AddSeconds(20)
        while ((Get-Date) -lt $limite) {
            if (-not (Get-Service -Name $ServiceName -ErrorAction SilentlyContinue)) { break }
            Start-Sleep -Milliseconds 500
        }
    } | Out-Null
}

function Remove-NodeFirewallRules {
    [CmdletBinding()]
    param()

    foreach ($nombre in @($RuleDicom, $RuleNodeApi)) {
        $regla = Get-NetFirewallRule -DisplayName $nombre -ErrorAction SilentlyContinue
        if (-not $regla) {
            Write-SetupLog "la regla '$nombre' no existe" -Level Detail
            continue
        }

        Invoke-SetupAction -Description "eliminar la regla '$nombre'" -Action {
            Remove-NetFirewallRule -DisplayName $nombre -ErrorAction Stop
        } | Out-Null
    }
}

# ─────────────────────────────────────────────────────────────────────────────

$logFile = Start-SetupLog -Directory $LogDir -DryRun:$DryRun
Write-SetupLog "Registro en $logFile"

try {
    if (-not (Test-SetupAdministrator)) {
        throw "Esta desinstalación debe ejecutarse como Administrador."
    }

    if (-not $InstallPath) { $InstallPath = 'C:\EdgeGuard\Node' }

    $manifiesto = Read-InstalledManifest -InstallPath $InstallPath
    if ($manifiesto) {
        Write-SetupLog "instalación encontrada: versión $($manifiesto.version), servicio $($manifiesto.servicio)" -Level Detail
        if (-not $PSBoundParameters.ContainsKey('ServiceName')) { $ServiceName = $manifiesto.servicio }
        $rutasDatos = @(
            $manifiesto.rutas.base
            $manifiesto.rutas.estudios
            $manifiesto.rutas.registros
        )
    }
    else {
        Write-SetupLog ("No se encontró installed.json en $InstallPath. Se usarán los valores por " +
                        "defecto: confirma que son los correctos antes de continuar.") -Level Warn
        $rutasDatos = @(
            'C:\ProgramData\EdgeGuard\Node\persistence\edge-node.db'
            'C:\ProgramData\EdgeGuard\Node\data'
            'C:\ProgramData\EdgeGuard\Node\logs'
        )
    }

    # ── Resumen ──────────────────────────────────────────────────────────────
    Write-Host ""
    Write-Host "  Se va a desinstalar:" -ForegroundColor Cyan
    Write-Host "    Servicio    $ServiceName"
    Write-Host "    Binarios    $InstallPath"
    Write-Host "    Firewall    $RuleDicom, $RuleNodeApi"
    Write-Host ""
    if ($PurgeData) {
        Write-Host "  Y SE BORRARÁN LOS DATOS:" -ForegroundColor Yellow
        foreach ($r in $rutasDatos) { Write-Host "    $r" -ForegroundColor Yellow }
    }
    else {
        Write-Host "  Los datos se conservan:" -ForegroundColor Green
        foreach ($r in $rutasDatos) { Write-Host "    $r" -ForegroundColor DarkGray }
    }
    Write-Host ""

    if (-not $Force -and -not $DryRun) {
        if ($PurgeData) {
            # Escribir el nombre completo, no 's': borrar la base del nodo no se
            # deshace, y una confirmación de una letra se contesta por reflejo.
            $respuesta = Read-Host "  Escribe el nombre del servicio ($ServiceName) para confirmar el borrado de datos"
            if ($respuesta -ne $ServiceName) {
                Write-SetupLog "Cancelado: la confirmación no coincide." -Level Warn
                exit 2
            }
        }
        else {
            $respuesta = Read-Host "  ¿Continuar? (s/N)"
            if ($respuesta -notmatch '^(s|si|sí|y|yes)$') {
                Write-SetupLog "Cancelado por el operador." -Level Warn
                exit 2
            }
        }
    }

    # ── Ejecución ────────────────────────────────────────────────────────────
    Invoke-SetupStep -Id '01' -Description 'Servicio' -Body {
        Remove-NodeServiceCompletely -ServiceName $ServiceName
    } | Out-Null

    Invoke-SetupStep -Id '02' -Description 'Reglas de firewall' -Body {
        Remove-NodeFirewallRules
    } | Out-Null

    Invoke-SetupStep -Id '03' -Description 'Binarios' -Body {
        if (-not (Test-Path -LiteralPath $InstallPath)) {
            Write-SetupLog "no existe $InstallPath" -Level Detail
            return
        }
        Invoke-SetupAction -Description "borrar $InstallPath" -Action {
            Remove-Item -LiteralPath $InstallPath -Recurse -Force -ErrorAction Stop
        } | Out-Null
    } | Out-Null

    if ($PurgeData) {
        Invoke-SetupStep -Id '04' -Description 'Datos del nodo' -Body {
            foreach ($ruta in $rutasDatos) {
                if (-not (Test-Path -LiteralPath $ruta)) {
                    Write-SetupLog "no existe $ruta" -Level Detail
                    continue
                }
                Invoke-SetupAction -Description "borrar $ruta" -Action {
                    Remove-Item -LiteralPath $ruta -Recurse -Force -ErrorAction Stop
                } | Out-Null
            }
        } | Out-Null
    }

    Write-SetupLog ""
    if ($PurgeData) {
        Write-SetupLog "Nodo desinstalado y datos borrados." -Level Ok
    }
    else {
        Write-SetupLog "Nodo desinstalado. Los datos siguen en disco." -Level Ok
        Write-SetupLog "Para retirarlos: vuelve a ejecutar con -PurgeData." -Level Detail
    }
    exit 0
}
catch {
    Write-SetupLog ""
    Write-SetupLog "DESINSTALACIÓN ABORTADA: $($_.Exception.Message)" -Level Error
    Write-SetupLog "Registro completo: $logFile" -Level Detail
    exit 1
}
