#Requires -Version 5.1
<#
.SYNOPSIS
    Paso 05 — Despliegue del paquete

.DESCRIPTION
    Detiene el servicio, respalda la instalación previa, descompone el paquete y
    escribe installed.json como manifiesto. El respaldo completo queda en
    State.Backup para que el maestro pueda revertir si un paso posterior falla.

    Detener el nodo no es como detener un app pool. IIS drena peticiones que
    duran milisegundos; aquí puede haber un estudio a medio recibir y una cola de
    envíos pendiente. Dos cosas se hacen por eso:

      · Se informa el tamaño de la cola ANTES de detener. Los elementos no se
        pierden —viven en SQLite y sobreviven al reinicio—, pero el operador
        merece saber que hay trabajo en vuelo antes de cortar.
      · Se espera a que el proceso suelte los archivos. Sin esa espera, la
        descompresión falla por archivos bloqueados y el error resultante no
        señala la causa.

    Los datos del nodo NO viven bajo InstallPath —el instalador lo valida antes
    de empezar—, así que reemplazar este directorio es seguro por construcción.
#>

# Cuánto se espera a que el servicio se detenga del todo antes de desistir.
$script:StopTimeoutSeconds = 45

<#
.SYNOPSIS
    Cuenta los elementos pendientes en la cola del nodo.
.DESCRIPTION
    Lectura directa de SQLite, en solo lectura: el servicio puede estar corriendo
    y no se le va a quitar el archivo de debajo. Un fallo aquí no detiene nada —
    es información para el operador, no una condición de la instalación.
#>
function Get-NodeQueueDepth {
    [CmdletBinding()]
    param([string]$DatabasePath)

    if (-not (Test-Path -LiteralPath $DatabasePath)) { return $null }

    # No hay cliente de SQLite garantizado en un Windows limpio, y el nodo es
    # self-contained: no deja uno instalado. Se aproxima por el tamaño del -wal,
    # que es lo único que se puede leer sin dependencias. Un -wal grande significa
    # escrituras recientes sin checkpoint, es decir, trabajo en vuelo.
    $wal = "$DatabasePath-wal"
    if (-not (Test-Path -LiteralPath $wal)) { return 0 }

    $mb = [math]::Round((Get-Item -LiteralPath $wal).Length / 1MB, 1)
    return $mb
}

<#
.SYNOPSIS
    Detiene el servicio y espera a que suelte los archivos.
#>
function Stop-NodeService {
    [CmdletBinding()]
    param([string]$ServiceName)

    $servicio = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if (-not $servicio) { return $false }
    if ($servicio.Status -eq 'Stopped') { return $false }

    Invoke-SetupAction -Description "detener el servicio $ServiceName" -Action {
        Stop-Service -Name $ServiceName -Force -ErrorAction Stop

        # Status 'Stopped' llega antes de que el proceso muera: el SCM lo marca al
        # aceptar la señal. Lo que bloquea los DLL es el proceso, así que se espera
        # a que desaparezca, no a que cambie el estado.
        $limite = (Get-Date).AddSeconds($script:StopTimeoutSeconds)
        while ((Get-Date) -lt $limite) {
            $proceso = Get-Process -Name 'Dicom.Edge.Node' -ErrorAction SilentlyContinue
            if (-not $proceso) { break }
            Start-Sleep -Milliseconds 500
        }

        if (Get-Process -Name 'Dicom.Edge.Node' -ErrorAction SilentlyContinue) {
            throw ("El proceso del nodo sigue vivo tras $($script:StopTimeoutSeconds) s. " +
                   "Detenlo a mano y vuelve a ejecutar: descomprimir sobre archivos bloqueados " +
                   "deja la instalación a medias.")
        }
    } | Out-Null

    return $true
}

<#
.SYNOPSIS
    Copia la instalación previa a un directorio de respaldo con marca de tiempo.
#>
function Backup-NodeInstallation {
    [CmdletBinding()]
    param([string]$InstallPath)

    if (-not (Test-Path -LiteralPath $InstallPath)) { return $null }

    $items = @(Get-ChildItem -LiteralPath $InstallPath -Force -ErrorAction SilentlyContinue)
    if ($items.Count -eq 0) { return $null }

    $destino = "$($InstallPath.TrimEnd('\')).backup-$(Get-Date -Format 'yyyyMMdd-HHmmss')"

    Invoke-SetupAction -Description "respaldar la instalación previa en $destino" -Action {
        Copy-Item -LiteralPath $InstallPath -Destination $destino -Recurse -Force -ErrorAction Stop
    } | Out-Null

    if (Test-SetupDryRun) { return $null }
    return $destino
}

<#
.SYNOPSIS
    Descomprime el paquete sobre el directorio de instalación.
#>
function Expand-NodePackage {
    [CmdletBinding()]
    param([string]$ZipPath, [string]$InstallPath)

    Invoke-SetupAction -Description "desplegar $(Split-Path -Leaf $ZipPath) en $InstallPath" -Action {
        if (Test-Path -LiteralPath $InstallPath) {
            Remove-Item -LiteralPath $InstallPath -Recurse -Force -ErrorAction Stop
        }
        New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null

        Add-Type -AssemblyName System.IO.Compression.FileSystem
        [System.IO.Compression.ZipFile]::ExtractToDirectory($ZipPath, $InstallPath)
    } | Out-Null
}

<#
.SYNOPSIS
    Escribe installed.json: qué se instaló, cuándo y desde qué paquete.
.DESCRIPTION
    Es lo que el paso 01 lee para resolver -Mode Auto, y lo que contesta "¿qué
    versión tiene esta sede?" sin entrar a mirar binarios.
#>
function Write-InstalledManifest {
    [CmdletBinding()]
    param([hashtable]$Config, [hashtable]$State)

    $paquete = $State.Package

    # Sobre el NOMBRE, no sobre la ruta completa: un directorio como
    # C:\Instaladores\v2.1.0\node\data\ haría que la versión registrada fuera la
    # de la carpeta y no la del paquete, y installed.json es lo que contesta
    # "¿qué versión tiene esta sede?".
    $version = if ((Split-Path -Leaf $paquete) -match '(\d+\.\d+\.\d+)') { $Matches[1] } else { 'desconocida' }

    $manifiesto = [ordered]@{
        version     = $version
        paquete     = Split-Path -Leaf $paquete
        sha256      = Get-SetupFileHashSafe -Path $paquete
        instalado   = (Get-Date).ToString('o')
        modo        = $State.ResolvedMode
        servicio    = $Config.ServiceName
        rutas       = [ordered]@{
            binarios   = $Config.InstallPath
            base       = $Config.DatabasePath
            estudios   = $Config.StorageRootPath
            registros  = $Config.LogPath
        }
        nodo        = [ordered]@{
            nombre    = $Config.NodeName
            aeTitle   = $Config.AeTitle
            dicomPort = $Config.DicomPort
            apiPort   = $Config.NodeApiPort
            hub       = $Config.HubBaseUrl
        }
    }

    Invoke-SetupAction -Description "escribir installed.json" -Action {
        $destino = Join-Path $Config.InstallPath 'installed.json'
        $json = $manifiesto | ConvertTo-Json -Depth 6
        [System.IO.File]::WriteAllText($destino, $json, (New-Object System.Text.UTF8Encoding $false))
    } | Out-Null

    return $version
}

# ─────────────────────────────────────────────────────────────────────────────

function Step-DeployPackage {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][hashtable]$Config,
        [Parameter(Mandatory)][hashtable]$State
    )

    # ── Trabajo en vuelo ─────────────────────────────────────────────────────
    $walMb = Get-NodeQueueDepth -DatabasePath $Config.DatabasePath
    if ($null -ne $walMb -and $walMb -gt 1) {
        Write-SetupLog ("El nodo tiene escrituras recientes sin consolidar ($walMb MB de diario). " +
                        "Nada se pierde al detenerlo —la cola vive en la base y sobrevive al " +
                        "reinicio—, pero hay trabajo en curso: si puedes, despliega fuera del " +
                        "horario de estudios.") -Level Warn
    }

    # ── Detener ──────────────────────────────────────────────────────────────
    $estaba = Stop-NodeService -ServiceName $Config.ServiceName
    if ($estaba) { Write-SetupLog "servicio detenido" -Level Detail }
    else         { Write-SetupLog "el servicio no estaba corriendo" -Level Detail }

    # ── Respaldar ────────────────────────────────────────────────────────────
    $respaldo = Backup-NodeInstallation -InstallPath $Config.InstallPath
    if ($respaldo) {
        $State.Backup = $respaldo
        Write-SetupLog "respaldo en $respaldo" -Level Detail
    }

    # ── Desplegar ────────────────────────────────────────────────────────────
    Expand-NodePackage -ZipPath $State.Package -InstallPath $Config.InstallPath

    if (-not (Test-SetupDryRun)) {
        $exe = Join-Path $Config.InstallPath 'Dicom.Edge.Node.exe'
        if (-not (Test-Path -LiteralPath $exe)) {
            throw "El despliegue no dejó Dicom.Edge.Node.exe en $($Config.InstallPath)."
        }
    }

    # ── Manifiesto ───────────────────────────────────────────────────────────
    $version = Write-InstalledManifest -Config $Config -State $State
    Write-SetupLog "versión $version desplegada" -Level Detail
}
