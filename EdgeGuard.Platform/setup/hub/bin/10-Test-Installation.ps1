#Requires -Version 5.1
<#
.SYNOPSIS
    Paso 10 — Verificación post-instalación

.DESCRIPTION
    Arranca el sitio, espera a que /health/live responda y revisa el log de arranque.

    Este paso es la única prueba real de que la instalación funciona: hasta aquí
    todo fue configuración que "parece" correcta. Tres cosas se comprueban y
    ninguna es decorativa:

      · /health/live responde. Ejercita la conexión a la base y las migraciones, de
        modo que una contraseña incorrecta se manifiesta aquí aunque el paso 04
        no haya podido validarla.
      · Data Protection persiste llaves. Si no, degrada a llaves efímeras con
        solo un LogWarning, y en el siguiente reciclaje del app pool los
        SigningSecret de todos los nodos dejan de ser descifrables.
      · La cuenta de administrador inicial existe y puede iniciar sesión. El
        sembrado del Hub omite la cuenta en silencio —solo un LogWarning— si la
        contraseña no llegó o no cumple su política, así que el único modo
        fiable de saberlo es autenticarse de verdad. Confirmada la cuenta, las
        dos variables de sembrado se retiran del app pool antes de terminar.
#>

$script:HealthTimeoutSeconds = 90
$script:HealthPollSeconds    = 3

function Start-HubSite {
    [CmdletBinding()]
    param([string]$SiteName, [string]$AppPoolName)

    Import-Module WebAdministration -ErrorAction Stop

    Invoke-SetupAction -Description "arrancar el app pool y el sitio" -Action {
        if ((Get-WebAppPoolState -Name $AppPoolName).Value -ne 'Started') {
            Start-WebAppPool -Name $AppPoolName -ErrorAction Stop
        }
        if ((Get-WebsiteState -Name $SiteName).Value -ne 'Started') {
            Start-Website -Name $SiteName -ErrorAction Stop
        }
    } | Out-Null
}

<#
.SYNOPSIS
    Consulta /health/live hasta que responda o venza el plazo.
.DESCRIPTION
    La ruta es /health/live, no /health/live: la aplicación solo mapea /health/live y
    /health/ready (HealthCheckConstants), y /health/live está en la lista de prefijos
    que el fallback de la SPA devuelve como 404. Sondear /health agotaba el plazo
    entero contra una ruta que nunca existió.

    Se sondea la sonda de liveness y no la de readiness a propósito. Las
    migraciones se aplican en Program.cs ANTES de que Kestrel acepte conexiones,
    así que cualquier respuesta HTTP ya demuestra que la base conectó y migró
    —que es lo que este paso necesita probar—. /health/ready agrega además los
    checks de disco y del listener MLLP, y Hl7ListenerHealthCheck devuelve
    Unhealthy (no Degraded) cuando el listener no escucha: con Hl7Enabled = $false,
    una configuración legítima, el agregado da 503 y este paso reprobaría una
    instalación correcta. El estado de readiness se consulta después, aparte, y
    solo se informa.

    Se usa HttpWebRequest en lugar de Invoke-WebRequest para poder fijar la
    cabecera Host: el sitio está enlazado a un host header y el servidor no
    resolvería la petición sin ella.
#>
function Wait-HubHealth {
    [CmdletBinding()]
    param([int]$Port, [string]$HostHeader)

    $url      = "http://localhost:$Port/health/live"
    $deadline = (Get-Date).AddSeconds($script:HealthTimeoutSeconds)
    $last     = 'sin respuesta'

    Write-SetupLog "Esperando $url (Host: $HostHeader), hasta $($script:HealthTimeoutSeconds)s" -Level Detail

    while ((Get-Date) -lt $deadline) {
        try {
            $request = [System.Net.HttpWebRequest]::Create($url)
            $request.Host    = $HostHeader
            $request.Timeout = 10000
            $request.Method  = 'GET'

            $response = $request.GetResponse()
            try {
                $reader = New-Object System.IO.StreamReader($response.GetResponseStream())
                $body   = $reader.ReadToEnd()
                $code   = [int]$response.StatusCode
            }
            finally { $response.Close() }

            if ($code -ge 200 -and $code -lt 300) {
                return @{ Ok = $true; StatusCode = $code; Body = $body.Trim() }
            }
            $last = "HTTP $code"
            if ($code -eq 404) { return @{ Ok = $false; Error = $last; Fatal = $true } }
        }
        catch [System.Net.WebException] {
            $resp = $_.Exception.Response
            if ($resp) {
                $code = [int]$resp.StatusCode
                $last = "HTTP $code"

                # Un 404 no es un estado transitorio: el proceso respondió, y
                # respondió que la ruta no existe. Reintentarlo 90s no cambia el
                # resultado y desplaza la sospecha hacia el arranque, que es
                # justo donde no está el problema. Se corta aquí.
                if ($code -eq 404) { return @{ Ok = $false; Error = $last; Fatal = $true } }
            }
            else {
                $last = $_.Exception.Message
            }
        }
        catch {
            $last = $_.Exception.Message
        }

        Start-Sleep -Seconds $script:HealthPollSeconds
    }

    return @{ Ok = $false; Error = $last }
}

<#
.SYNOPSIS
    Consulta /health/ready una vez y vuelca el resultado al log.
.DESCRIPTION
    Puramente informativo. Un 503 aquí no reprueba la instalación: significa que
    alguno de los checks con tag 'ready' —base, disco, listener MLLP— no está
    verde, y eso puede deberse a decisiones legítimas del despliegue o a
    condiciones ajenas al instalador. Se muestra para que el operador lo vea
    ahora y no en la primera incidencia.
#>
function Write-HubReadiness {
    [CmdletBinding()]
    param([int]$Port, [string]$HostHeader)

    $url = "http://localhost:$Port/health/ready"

    try {
        $request = [System.Net.HttpWebRequest]::Create($url)
        $request.Host    = $HostHeader
        $request.Timeout = 15000
        $request.Method  = 'GET'

        $response = $request.GetResponse()
        try {
            $reader = New-Object System.IO.StreamReader($response.GetResponseStream())
            $body   = $reader.ReadToEnd().Trim()
            $code   = [int]$response.StatusCode
        }
        finally { $response.Close() }

        Write-SetupLog "/health/ready HTTP $code : $body" -Level Detail
    }
    catch [System.Net.WebException] {
        # 503 = algún check no está verde. El cuerpo dice cuál.
        $resp = $_.Exception.Response
        if ($resp) {
            $code = [int]$resp.StatusCode
            $body = ''
            try {
                $reader = New-Object System.IO.StreamReader($resp.GetResponseStream())
                $body   = $reader.ReadToEnd().Trim()
            }
            catch { }
            finally { $resp.Close() }

            Write-SetupLog ("/health/ready HTTP $code : $body — informativo, no reprueba la " +
                            "instalación. Revísalo antes de poner el Hub en servicio.") -Level Warn
        }
        else {
            Write-SetupLog "No se pudo consultar /health/ready: $($_.Exception.Message)" -Level Detail
        }
    }
    catch {
        Write-SetupLog "No se pudo consultar /health/ready: $($_.Exception.Message)" -Level Detail
    }
}

<#
.SYNOPSIS
    Devuelve las líneas escritas recientemente en los logs del Hub.
#>
function Get-RecentHubLog {
    [CmdletBinding()]
    param([string]$InstallPath, [int]$MinutesBack = 15)

    $logDir = Join-Path $InstallPath 'logs'
    if (-not (Test-Path -LiteralPath $logDir)) { return @() }

    $cutoff = (Get-Date).AddMinutes(-$MinutesBack)
    $lines  = @()

    foreach ($file in Get-ChildItem -LiteralPath $logDir -Filter '*.log' -File |
                      Where-Object { $_.LastWriteTime -ge $cutoff }) {
        try   { $lines += Get-Content -LiteralPath $file.FullName -ErrorAction Stop }
        catch { Write-SetupLog "No se pudo leer $($file.Name): $($_.Exception.Message)" -Level Detail }
    }

    return $lines
}

<#
.SYNOPSIS
    Confirma que Data Protection persiste llaves en disco.
#>
function Test-DataProtectionPersisted {
    [CmdletBinding()]
    param([string[]]$LogLines, [string]$KeyPath)

    $ephemeral = @($LogLines | Where-Object { $_ -match 'KeyPath not configured|keys are ephemeral' })
    if ($ephemeral.Count -gt 0) {
        throw ("El Hub arrancó con llaves de Data Protection EFÍMERAS. La variable " +
               "DataProtection__KeyPath no llegó al proceso. En el siguiente reciclaje del app " +
               "pool los SigningSecret de todos los nodos dejarán de ser descifrables. " +
               "Línea del log: $($ephemeral[0])")
    }

    $persisted = @($LogLines | Where-Object { $_ -match 'DataProtection keys persisted to' })
    if ($persisted.Count -gt 0) {
        Write-SetupLog "Data Protection persiste llaves en $KeyPath" -Level Detail
        return $true
    }

    # Ni confirmación ni aviso: el nivel de log puede estar por encima de
    # Information. Se comprueba el disco, que es la evidencia directa.
    $keyFiles = @(Get-ChildItem -LiteralPath $KeyPath -Filter 'key-*.xml' -File -ErrorAction SilentlyContinue)
    if ($keyFiles.Count -gt 0) {
        Write-SetupLog "Data Protection: $($keyFiles.Count) llave(s) en disco en $KeyPath" -Level Detail
        return $true
    }

    Write-SetupLog ("No se pudo confirmar que Data Protection persista llaves: no hay línea en el log " +
                    "ni archivos key-*.xml en $KeyPath. Verifícalo antes de registrar nodos.") -Level Warn
    return $false
}

<#
.SYNOPSIS
    Comprueba que la cuenta de administrador funciona, autenticándose de verdad.
.DESCRIPTION
    AdminUserSeed corre al arrancar el Hub y siembra la cuenta leyendo
    EDGEGUARD_ADMIN_USERNAME y EDGEGUARD_ADMIN_PASSWORD del entorno. Es
    idempotente y, cuando algo no le cuadra —la variable ausente, la contraseña
    fuera de política—, se limita a un LogWarning y NO crea la cuenta.

    Por eso no basta con leer el log: se hace un inicio de sesión real contra
    /api/auth/login. Es la única prueba de que el operador podrá entrar al SPA,
    y distingue los tres desenlaces que de otro modo se parecen entre sí:
    cuenta recién creada, cuenta que ya existía y cuenta que nunca se sembró.

    Como en Wait-HubHealth, se usa HttpWebRequest para poder fijar la cabecera
    Host: el sitio está enlazado a un host header.
#>
function Test-HubAdminLogin {
    [CmdletBinding()]
    param([int]$Port, [string]$HostHeader, [string]$Username, [string]$Password)

    $url  = "http://localhost:$Port/api/auth/login"
    $body = @{ username = $Username; password = $Password } | ConvertTo-Json -Compress
    $data = [System.Text.Encoding]::UTF8.GetBytes($body)

    $request = [System.Net.HttpWebRequest]::Create($url)
    $request.Host        = $HostHeader
    $request.Method      = 'POST'
    $request.ContentType = 'application/json'
    $request.Timeout     = 20000
    $request.ContentLength = $data.Length

    try {
        $stream = $request.GetRequestStream()
        try   { $stream.Write($data, 0, $data.Length) }
        finally { $stream.Dispose() }

        $response = $request.GetResponse()
        try     { return @{ Ok = $true; StatusCode = [int]$response.StatusCode } }
        finally { $response.Close() }
    }
    catch [System.Net.WebException] {
        $resp = $_.Exception.Response
        $code = if ($resp) { [int]$resp.StatusCode } else { 0 }
        return @{ Ok = $false; StatusCode = $code; Error = $_.Exception.Message }
    }
}

<#
.SYNOPSIS
    Retira del app pool las variables de sembrado del administrador.
.DESCRIPTION
    Se llama sólo después de comprobar que la cuenta existe y funciona: las dos
    variables ya cumplieron su único cometido. El sembrado es idempotente y en
    los siguientes arranques se saltaría de todos modos porque el usuario ya
    existe, así que no aportan nada y sí cuestan.

    EDGEGUARD_ADMIN_PASSWORD dejaría la contraseña del administrador en claro
    dentro de applicationHost.config de forma permanente. EDGEGUARD_ADMIN_USERNAME
    no es secreto, pero se retira con ella: dejar sola la mitad que nombra la
    cuenta privilegiada no aporta nada operativo —con qué cuenta se sembró queda
    en el log del instalador— y una variable huérfana invita a "completarla"
    volviendo a poner la contraseña al lado.

    Escribir la colección recicla el app pool. Es aceptable aquí: /health/live y el
    inicio de sesión ya se verificaron, y el reciclaje no vuelve a sembrar nada.
#>
function Remove-AdminSeedVariables {
    [CmdletBinding()]
    param([string]$AppPoolName)

    $names = @('EDGEGUARD_ADMIN_PASSWORD', 'EDGEGUARD_ADMIN_USERNAME')

    Import-Module WebAdministration -ErrorAction Stop
    $poolPath = "IIS:\AppPools\$AppPoolName"

    $current = @{}
    $collection = (Get-ItemProperty $poolPath -Name environmentVariables -ErrorAction SilentlyContinue)
    if ($collection -and $collection.Collection) {
        foreach ($entry in $collection.Collection) { $current[$entry.name] = $entry.value }
    }

    $present = @($names | Where-Object { $current.ContainsKey($_) })
    if ($present.Count -eq 0) { return }

    foreach ($name in $present) { $current.Remove($name) }

    Invoke-SetupAction -Description "retirar del app pool $($present -join ' y ')" -Action {
        $rebuilt = @()
        foreach ($name in $current.Keys) {
            $rebuilt += @{ name = $name; value = [string]$current[$name] }
        }
        Set-ItemProperty $poolPath -Name environmentVariables -Value $rebuilt -ErrorAction Stop
    } | Out-Null

    if (Test-SetupDryRun) { return }

    $after = (Get-ItemProperty $poolPath -Name environmentVariables -ErrorAction SilentlyContinue)
    $still = @($after.Collection | Where-Object { $names -contains $_.name } | ForEach-Object { $_.name })

    if ($still -contains 'EDGEGUARD_ADMIN_PASSWORD') {
        Write-SetupLog ("No se pudo retirar EDGEGUARD_ADMIN_PASSWORD del app pool. Queda la " +
                        "contraseña del administrador en claro en applicationHost.config: " +
                        "elimínala a mano desde IIS Manager antes de dar por cerrada la instalación.") -Level Warn
        return
    }
    if ($still.Count -gt 0) {
        Write-SetupLog "No se pudo retirar $($still -join ', ') del app pool; elimínala a mano." -Level Warn
        return
    }

    Write-SetupLog "$($present -join ' y ') retiradas del app pool tras confirmar la cuenta" -Level Detail
}

<#
.SYNOPSIS
    Siembra y verifica la cuenta de administrador inicial.
#>
function Confirm-HubAdminAccount {
    [CmdletBinding()]
    param([hashtable]$Config, [string[]]$LogLines)

    $username = ([string]$Config.AdminUsername).Trim().ToLowerInvariant()
    $password = [string]$Config.AdminPassword

    if ([string]::IsNullOrWhiteSpace($password)) {
        Write-SetupLog ("Sin AdminPassword en la configuración: no se sembró ninguna cuenta. " +
                        "Si esta es una instalación nueva, NO habrá con qué iniciar sesión en el " +
                        "SPA; rellena AdminPassword y ejecuta -Mode Repair, o da de alta la cuenta " +
                        "con scripts\seed-admin.sql.") -Level Warn
        return
    }

    $login = Test-HubAdminLogin -Port $Config.Port -HostHeader $Config.HostHeader `
                                -Username $username -Password $password

    if (-not $login.Ok) {
        # El log del Hub dice por qué se omitió el sembrado; es más útil que el 401.
        $skipped = @($LogLines | Where-Object { $_ -match 'Admin seed skipped' })
        $detail  = if ($skipped.Count -gt 0) { " El Hub reportó: $($skipped[-1])" } else { '' }

        throw ("La cuenta de administrador '$username' no pudo iniciar sesión " +
               "(HTTP $($login.StatusCode)).$detail Revisa AdminUsername y AdminPassword; " +
               "si la cuenta ya existía con otra contraseña, el instalador no la cambia.")
    }

    $created = @($LogLines | Where-Object { $_ -match 'Default super-administrator created' })
    Write-SetupLog $(if ($created.Count -gt 0) {
                        "Cuenta de administrador '$username' creada y verificada por inicio de sesión."
                     } else {
                        "La cuenta '$username' ya existía; verificada por inicio de sesión."
                     }) -Level Ok

    Remove-AdminSeedVariables -AppPoolName $Config.AppPoolName

    Write-Host ""
    Write-Host "  ─────────────────────────────────────────────────────────────" -ForegroundColor Cyan
    Write-Host "   ADMINISTRADOR INICIAL" -ForegroundColor Cyan
    Write-Host "  ─────────────────────────────────────────────────────────────" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "   Usuario   $username"
    Write-Host "   Acceso    http://$($Config.HostHeader):$($Config.Port)/"
    Write-Host ""
    Write-Host "   Cambia la contraseña desde el SPA tras el primer acceso, y borra" -ForegroundColor Gray
    Write-Host "   hub-install.psd1 del servidor: contiene ambas contraseñas en claro." -ForegroundColor Gray
    Write-Host ""
}

function Step-TestInstallation {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][hashtable]$Config,
        [Parameter(Mandatory)][hashtable]$State
    )

    Start-HubSite -SiteName $Config.SiteName -AppPoolName $Config.AppPoolName

    if (Test-SetupDryRun) {
        Write-SetupLog "haría: esperar /health/live, revisar el log y verificar la cuenta de administrador" -Level Detail
        return
    }

    # ── /health/live ─────────────────────────────────────────────────────────
    $health = Wait-HubHealth -Port $Config.Port -HostHeader $Config.HostHeader

    if (-not $health.Ok) {
        if ($health.ContainsKey('Fatal') -and $health.Fatal) {
            throw ("El Hub respondió 404 en /health/live. El proceso está en pie: lo que falta " +
                   "es la ruta. Confirma que el paquete desplegado registra los endpoints de " +
                   "diagnóstico (MapDiagnosticsEndpoints) y que no es una versión anterior a " +
                   "este instalador.")
        }

        $hint = "Revisa $(Join-Path $Config.InstallPath 'logs') para el detalle."
        throw "El Hub no respondió en /health/live tras $($script:HealthTimeoutSeconds)s ($($health.Error)). $hint"
    }

    Write-SetupLog "/health/live respondió HTTP $($health.StatusCode): $($health.Body)" -Level Ok

    # Readiness es informativo: da el estado de base, disco y listener MLLP sin
    # que ninguno de los tres pueda reprobar una instalación correcta.
    Write-HubReadiness -Port $Config.Port -HostHeader $Config.HostHeader

    # ── Log de arranque ──────────────────────────────────────────────────────
    $logLines = Get-RecentHubLog -InstallPath $Config.InstallPath
    Write-SetupLog "$($logLines.Count) líneas de log de arranque revisadas" -Level Detail

    Test-DataProtectionPersisted -LogLines $logLines -KeyPath $Config.DataProtectionKeyPath | Out-Null

    if ($State.ContainsKey('RebootRequired') -and $State.RebootRequired) {
        Write-SetupLog "El Hosting Bundle pidió reinicio: prográmalo antes de poner el Hub en servicio." -Level Warn
    }

    # ── Administrador inicial ────────────────────────────────────────────────
    Confirm-HubAdminAccount -Config $Config -LogLines $logLines
}
