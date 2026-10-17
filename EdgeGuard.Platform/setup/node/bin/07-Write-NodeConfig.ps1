#Requires -Version 5.1
<#
.SYNOPSIS
    Paso 07 — Configuración del nodo

.DESCRIPTION
    Genera appsettings.Production.json completo a partir del .psd1. El
    equivalente del 07-Set-AppPoolConfig del Hub, que escribe variables de
    entorno en el app pool; aquí el archivo ES la salida generada.

    La diferencia con el Hub no es caprichosa: el nodo ya tiene un archivo de
    configuración que el runtime lee, y añadirle una segunda fuente por
    variables de entorno crearía dos sitios donde mirar cuando algo no cuadra.
    Con el archivo generado hay una sola fuente que el operador edita (el .psd1)
    y un archivo derivado que nadie toca a mano, y la actualización no fusiona
    nada porque reescribe de forma determinista.

    LAS TRES CAPAS. No todas las claves pesan igual, y conviene saberlo antes de
    editar este paso:

      A · appsettings manda siempre — la base nunca las sombrea.
          ConnectionStrings:NodeDatabase, Diagnostics:*, HubConnection:Enabled.

      B · appsettings siembra la base en el primer arranque; después manda la
          base. Todo HubConnection: NodeSettingsHubHydrator las copia a las filas
          hub.* tras migrar.

      C · la base las sombrea desde el arranque, y NodeSettingsDeploymentHydrator
          es quien las copia: DicomServer:AeTitle, DicomServer:Port, NodeApi:Port
          y NodeStorage:RootPath. Escribirlas aquí sirve porque ese hidratador
          existe; sin él, no harían nada.

    En un nodo YA REGISTRADO las capas B y C dejan de aplicarse: manda la base,
    para que las ediciones del operador desde el Hub sobrevivan al reinicio. Mover
    el nodo a otro Hub exige detener el servicio y eliminar la base.
#>

<#
.SYNOPSIS
    Construye el objeto de configuración que se serializa a JSON.
#>
function Build-NodeConfiguration {
    [CmdletBinding()]
    param([hashtable]$Config)

    $conexion = "Data Source=$($Config.DatabasePath -replace '\\', '/')"

    $configuracion = [ordered]@{
        Logging = [ordered]@{
            LogLevel = [ordered]@{
                'Default'                     = 'Warning'
                'Microsoft.Hosting.Lifetime'  = 'Information'
                'Microsoft.EntityFrameworkCore' = 'Error'
                'Microsoft.AspNetCore'        = 'Warning'
            }
        }

        ConnectionStrings = [ordered]@{
            NodeDatabase = $conexion
        }

        Diagnostics = [ordered]@{
            Environment = 'Production'
            InstanceId  = $Config.InstanceId

            File = [ordered]@{
                Path                   = ($Config.LogPath -replace '\\', '/')
                FileNameTemplate       = 'node-.log'
                MaxFileSizeMb          = $Config.LogMaxFileSizeMb
                RetainDays             = $Config.RetainDays
                RetainedFileCountLimit = $Config.RetainDays
                RollingInterval        = 'Day'
                UseCompactJson         = $true
            }

            Seq           = [ordered]@{ Enabled = $false }
            HttpSink      = [ordered]@{ Enabled = $false }
            OpenTelemetry = [ordered]@{ Enabled = $false }

            HealthChecks = [ordered]@{
                StorageMinAvailableMb      = 500
                QueueMaxPendingItems       = 1000
                ConnectivityTimeoutSeconds = 10
            }
        }

        # ── Capa C ───────────────────────────────────────────────────────────
        DicomServer = [ordered]@{
            AeTitle = $Config.AeTitle
            Port    = $Config.DicomPort
        }

        NodeApi = [ordered]@{
            Port = $Config.NodeApiPort
        }

        NodeStorage = [ordered]@{
            RootPath = ($Config.StorageRootPath -replace '\\', '/')
        }

        # ── Capas A y B ──────────────────────────────────────────────────────
        HubConnection = [ordered]@{
            Enabled                   = $true
            HubBaseUrl                = $Config.HubBaseUrl
            NodeName                  = $Config.NodeName
            FacilityName              = [string]$Config.FacilityName
            Location                  = [string]$Config.Location
            IpAddress                 = [string]$Config.IpAddress
            Port                      = $Config.DicomPort
            Version                   = ''
            TimeoutSeconds            = $Config.TimeoutSeconds
            HeartbeatIntervalSeconds  = $Config.HeartbeatIntervalSeconds
            ConfigPullIntervalSeconds = $Config.ConfigPullIntervalSeconds
            RegisterOnStartup         = $Config.RegisterOnStartup
            MaxReconnectAttempts      = $Config.MaxReconnectAttempts
            ReconnectDelaySeconds     = $Config.ReconnectDelaySeconds
        }
    }

    # La versión se toma del paquete, no se inventa: es la que el nodo reporta al
    # Hub y la que aparece en la consola junto al nombre de la sede.
    return $configuracion
}

<#
.SYNOPSIS
    Escribe el archivo y vuelve a leerlo para comprobar que quedó como se quería.
.DESCRIPTION
    Releer no es ceremonia: un JSON mal formado deja al nodo sin arrancar con un
    error del cargador de configuración que no nombra el archivo, y el operador
    acaba buscando el problema en el servicio.
#>
function Write-NodeAppSettings {
    [CmdletBinding()]
    param([hashtable]$Config, [hashtable]$Configuracion)

    $destino = Join-Path $Config.InstallPath 'appsettings.Production.json'

    $encabezado = @(
        '// ─────────────────────────────────────────────────────────────────────────'
        '// GENERADO POR EL INSTALADOR — NO EDITAR A MANO'
        '//'
        '// Este archivo lo reescribe setup\node\install.ps1 en cada ejecución a partir'
        '// de node-install.psd1. Cualquier cambio hecho aquí se pierde en la siguiente'
        '// actualización. Lo que se edita es el .psd1.'
        '//'
        "// Generado: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
        "// Nodo:     $($Config.NodeName)  ·  AE $($Config.AeTitle)"
        '// ─────────────────────────────────────────────────────────────────────────'
        ''
    ) -join "`r`n"

    $json = $Configuracion | ConvertTo-Json -Depth 8

    Invoke-SetupAction -Description "escribir appsettings.Production.json" -Action {
        # Con BOM: el lector de configuración de .NET lo admite, y sin él cualquier
        # editor del servidor abre el archivo como ANSI y el encabezado —que es
        # justo lo que debe leer quien lo abra— sale en mojibake.
        [System.IO.File]::WriteAllText($destino, $encabezado + $json, (New-Object System.Text.UTF8Encoding $true))
    } | Out-Null

    if (Test-SetupDryRun) { return }

    # Releer con el mismo criterio que usará el nodo. ConvertFrom-Json de PS 5.1
    # no admite comentarios, así que se quitan antes: el runtime de .NET sí los
    # admite, y el encabezado es demasiado útil para renunciar a él.
    $contenido = Get-Content -LiteralPath $destino -Raw
    $sinComentarios = ($contenido -split "`r?`n" | Where-Object { $_.TrimStart() -notlike '//*' }) -join "`n"

    try   { $releido = $sinComentarios | ConvertFrom-Json }
    catch { throw "El appsettings generado no es JSON válido: $($_.Exception.Message)" }

    if ($releido.HubConnection.HubBaseUrl -ne $Config.HubBaseUrl) {
        throw "El appsettings generado no tiene la dirección del Hub esperada."
    }
    if ($releido.DicomServer.AeTitle -ne $Config.AeTitle) {
        throw "El appsettings generado no tiene el AE title esperado."
    }

    Write-SetupLog "appsettings.Production.json verificado tras escribirlo" -Level Detail
}

<#
.SYNOPSIS
    Escribe el token de arranque, si el .psd1 trae uno.
.DESCRIPTION
    Va en un archivo aparte y no en el appsettings generado: es un secreto de un
    solo uso y no tiene por qué quedar en un archivo que se regenera, se copia a
    los respaldos y se lee para diagnosticar.
#>
function Write-NodeBootstrapToken {
    [CmdletBinding()]
    param([hashtable]$Config)

    $token = [string]$Config.BootstrapToken
    if ([string]::IsNullOrWhiteSpace($token)) {
        Write-SetupLog "sin token de arranque: el nodo pedirá el suyo al registrarse" -Level Detail
        return
    }

    $destino = Join-Path $Config.InstallPath 'appsettings.bootstrap.json'

    Invoke-SetupAction -Description "escribir el token de arranque" -Action {
        $contenido = [ordered]@{ HubConnection = [ordered]@{ BootstrapToken = $token } } |
            ConvertTo-Json -Depth 4
        [System.IO.File]::WriteAllText($destino, $contenido, (New-Object System.Text.UTF8Encoding $false))
    } | Out-Null

    Write-SetupLog "token de arranque escrito: $(Format-SetupMasked $token)" -Level Detail
}

<#
.SYNOPSIS
    Avisa cuando lo que se acaba de escribir ya no es lo que manda.
.DESCRIPTION
    Es la trampa de las capas B y C, y merece un aviso explícito porque no
    produce ningún error: en un nodo YA REGISTRADO los hidratadores solo rellenan
    filas vacías, así que cambiar HubBaseUrl, el AE o los puertos en el .psd1 y
    reinstalar NO los cambia. El operador vería los valores correctos en el
    archivo y el nodo comportándose como antes.

    Mover de Hub un nodo registrado exige limpiar sus filas hub.*, incluida la
    credencial, y eso significa escribir en la base con el servicio parado. Sin
    un cliente de SQLite en el servidor —el nodo es self-contained y no instala
    uno— hace falta herramienta propia, así que no se hace desde aquí. Los dos
    caminos que sí existen están en el mensaje.
#>
function Assert-DeployedConfigStillApplies {
    [CmdletBinding()]
    param([hashtable]$Config)

    if (-not (Test-Path -LiteralPath $Config.DatabasePath)) {
        Write-SetupLog "nodo sin base previa: la configuración escrita manda entera" -Level Detail
        return
    }

    Write-SetupLog ("Este nodo YA tiene base de datos. Si además está registrado con un Hub, la " +
                    "dirección del Hub, el AE title, los puertos y la carpeta de estudios los manda " +
                    "la base y NO este archivo — lo que se acaba de escribir no los cambiará.") -Level Warn
    Write-SetupLog ("Para cambiarlos: desde la consola del Hub actual. Para moverlo a OTRO Hub: " +
                    "detén el servicio y elimina la base ($($Config.DatabasePath)) — se pierde el " +
                    "historial de qué estudios se entregaron, no los estudios.") -Level Detail
}

# ─────────────────────────────────────────────────────────────────────────────

function Step-WriteNodeConfig {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][hashtable]$Config,
        [Parameter(Mandatory)][hashtable]$State
    )

    $configuracion = Build-NodeConfiguration -Config $Config
    Write-NodeAppSettings -Config $Config -Configuracion $configuracion
    Write-NodeBootstrapToken -Config $Config
    Assert-DeployedConfigStillApplies -Config $Config
}
