#Requires -Version 5.1
<#
.SYNOPSIS
    Paso 10 — Verificación post-instalación

.DESCRIPTION
    Arranca el sitio, espera a que /health responda y revisa el log de arranque.

    Este paso es la única prueba real de que la instalación funciona: hasta aquí
    todo fue configuración que "parece" correcta. Tres cosas se comprueban y
    ninguna es decorativa:

      · /health responde. Ejercita la conexión a la base y las migraciones, de
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
    Consulta /health hasta que responda o venza el plazo.
.DESCRIPTION
    Se usa HttpWebRequest en lugar de Invoke-WebRequest para poder fijar la
    cabecera Host: el sitio está enlazado a un host header y el servidor no
    resolvería la petición sin ella.
#>
function Wait-HubHealth {
    [CmdletBinding()]
    param([int]$Port, [string]$HostHeader)

    $url      = "http://localhost:$Port/health"
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
        }
        catch [System.Net.WebException] {
            $resp = $_.Exception.Response
            $last = if ($resp) { "HTTP $([int]$resp.StatusCode)" } else { $_.Exception.Message }
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

    Escribir la colección recicla el app pool. Es aceptable aquí: /health y el
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
        Write-SetupLog "haría: esperar /health, revisar el log y verificar la cuenta de administrador" -Level Detail
        return
    }

    # ── /health ──────────────────────────────────────────────────────────────
    $health = Wait-HubHealth -Port $Config.Port -HostHeader $Config.HostHeader

    if (-not $health.Ok) {
        $hint = "Revisa $(Join-Path $Config.InstallPath 'logs') para el detalle."
        throw "El Hub no respondió en /health tras $($script:HealthTimeoutSeconds)s ($($health.Error)). $hint"
    }

    Write-SetupLog "/health respondió HTTP $($health.StatusCode): $($health.Body)" -Level Ok

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
