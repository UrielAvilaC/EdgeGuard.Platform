#Requires -Version 5.1
<#
.SYNOPSIS
    Paso 06 — Registro del servicio de Windows

.DESCRIPTION
    Registra —o reconfigura— el servicio y su política de recuperación ante
    fallos. El equivalente del 06-New-IisSite del Hub: aquí el SCM hace lo que
    allá hace IIS.

    El servicio se deja DETENIDO. Lo arranca el paso 10, después de que el 07
    haya escrito la configuración y el 08 creado los directorios: arrancarlo
    antes produce un ciclo de fallo-reinicio contra una configuración a medias,
    y la política de recuperación lo reintentaría tres veces antes de rendirse,
    llenando el registro de errores que no son el problema real.
#>

<#
.SYNOPSIS
    Elimina un servicio previo, esperando a que el SCM lo suelte.
#>
function Remove-NodeService {
    [CmdletBinding()]
    param([string]$ServiceName)

    $servicio = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if (-not $servicio) { return }

    Invoke-SetupAction -Description "eliminar el servicio previo $ServiceName" -Action {
        if ($servicio.Status -ne 'Stopped') {
            Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
        }

        & sc.exe delete $ServiceName | Out-Null

        # sc.exe delete marca el servicio para borrado; el registro tarda en
        # limpiarse. Crear el nuevo antes de que desaparezca falla con
        # "el servicio está marcado para eliminación", que no dice qué hacer.
        $limite = (Get-Date).AddSeconds(20)
        while ((Get-Date) -lt $limite) {
            if (-not (Get-Service -Name $ServiceName -ErrorAction SilentlyContinue)) { break }
            Start-Sleep -Milliseconds 500
        }

        if (Get-Service -Name $ServiceName -ErrorAction SilentlyContinue) {
            throw ("El servicio '$ServiceName' sigue registrado tras pedir su eliminación. " +
                   "Suele ser services.msc o el Visor de eventos abiertos sobre él: ciérralos y repite.")
        }
    } | Out-Null
}

<#
.SYNOPSIS
    Registra el servicio apuntando al ejecutable desplegado.
#>
function New-NodeService {
    [CmdletBinding()]
    param([hashtable]$Config)

    $exe = Join-Path $Config.InstallPath 'Dicom.Edge.Node.exe'

    if (-not (Test-SetupDryRun) -and -not (Test-Path -LiteralPath $exe)) {
        throw "No existe $exe. El paso 05 no dejó el ejecutable donde debía."
    }

    # --environment Production es lo que hace que el nodo cargue
    # appsettings.Production.json, que es el archivo que escribe el paso 07.
    $binaryPath = "`"$exe`" --environment Production"

    Invoke-SetupAction -Description "registrar el servicio $($Config.ServiceName)" -Action {
        $parametros = @{
            Name           = $Config.ServiceName
            BinaryPathName = $binaryPath
            DisplayName    = $Config.DisplayName
            Description    = 'EdgeGuard Platform - nodo DICOM de sede (C-STORE SCP, ruteo, worklist)'
            StartupType    = $Config.StartupType
            ErrorAction    = 'Stop'
        }

        # LocalSystem es el default y no se pasa: New-Service lo usa cuando no se
        # indica credencial. Una cuenta dedicada necesita contraseña, que este
        # instalador no maneja — se configura después en services.msc, y el paso
        # 08 ya deja los permisos de los directorios preparados para ella.
        New-Service @parametros | Out-Null
    } | Out-Null

    if ($Config.ServiceAccount -ne 'LocalSystem') {
        Write-SetupLog ("El servicio quedó registrado como LocalSystem. Para usar " +
                        "'$($Config.ServiceAccount)' asígnala en services.msc → Propiedades → " +
                        "Iniciar sesión: la contraseña no pasa por este instalador.") -Level Warn
    }
}

<#
.SYNOPSIS
    Configura la recuperación automática ante fallos.
.DESCRIPTION
    Reinicia tras el 1.º (5 s), el 2.º (10 s) y los siguientes fallos (30 s), con
    el contador reseteándose cada 24 h.

    Las esperas crecientes importan: un nodo que falla por algo transitorio —el
    disco lleno un instante, la red caída— vuelve solo en segundos; uno que falla
    por configuración entraría en bucle cerrado y llenaría el disco de registros,
    que es el modo de fallo que convierte un problema en dos.
#>
function Set-NodeServiceRecovery {
    [CmdletBinding()]
    param([string]$ServiceName)

    Invoke-SetupAction -Description "configurar la recuperación ante fallos" -Action {
        & sc.exe failure $ServiceName reset= 86400 actions= restart/5000/restart/10000/restart/30000 | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw "sc.exe failure devolvió $LASTEXITCODE al configurar la recuperación."
        }

        # Sin esto, la política solo se aplica cuando el servicio termina con un
        # código de error. Un nodo que aborta el arranque por configuración
        # inválida termina limpiamente desde el punto de vista del SCM, y no se
        # reiniciaría nunca — justo el caso en el que el reintento ayuda, porque
        # da tiempo a que alguien corrija el archivo.
        & sc.exe failureflag $ServiceName 1 | Out-Null
    } | Out-Null
}

# ─────────────────────────────────────────────────────────────────────────────

function Step-NewService {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][hashtable]$Config,
        [Parameter(Mandatory)][hashtable]$State
    )

    Remove-NodeService -ServiceName $Config.ServiceName
    New-NodeService -Config $Config
    Set-NodeServiceRecovery -ServiceName $Config.ServiceName

    if (Test-SetupDryRun) { return }

    $servicio = Get-Service -Name $Config.ServiceName -ErrorAction SilentlyContinue
    if (-not $servicio) {
        throw "El servicio '$($Config.ServiceName)' no quedó registrado."
    }

    Write-SetupLog "servicio registrado — arranque $($Config.StartupType), estado $($servicio.Status)" -Level Detail
    Write-SetupLog "se arrancará en el paso 10, con la configuración ya escrita" -Level Detail
}
