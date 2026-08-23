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
      · El bootstrap token, si lo hubo, se muestra UNA vez y no se persiste.
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
    Extrae el bootstrap token del log de arranque.
.DESCRIPTION
    Aparece una sola vez, en el primer arranque contra una base vacía. No se
    escribe al log del instalador: se muestra en consola y ahí termina su rastro
    en este proceso.
#>
function Find-BootstrapToken {
    [CmdletBinding()]
    param([string[]]$LogLines)

    foreach ($line in $LogLines) {
        if ($line -match '(eyJ[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]+)') {
            return $Matches[1]
        }
    }
    return $null
}

function Step-TestInstallation {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][hashtable]$Config,
        [Parameter(Mandatory)][hashtable]$State
    )

    Start-HubSite -SiteName $Config.SiteName -AppPoolName $Config.AppPoolName

    if (Test-SetupDryRun) {
        Write-SetupLog "haría: esperar /health y revisar el log de arranque" -Level Detail
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

    # ── Bootstrap token ──────────────────────────────────────────────────────
    $token = Find-BootstrapToken -LogLines $logLines
    if ($token) {
        Write-SetupLog "Bootstrap token emitido (se muestra una sola vez, no queda en el log del instalador)" -Level Info

        Write-Host ""
        Write-Host "  ─────────────────────────────────────────────────────────────" -ForegroundColor Yellow
        Write-Host "   BOOTSTRAP TOKEN — de un solo uso, no se repite" -ForegroundColor Yellow
        Write-Host "  ─────────────────────────────────────────────────────────────" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "   $token"
        Write-Host ""
        Write-Host "   Crea la cuenta de administrador inicial:" -ForegroundColor Gray
        Write-Host "     POST http://$($Config.HostHeader):$($Config.Port)/api/auth/bootstrap" -ForegroundColor Gray
        Write-Host "     Authorization: Bearer <token>" -ForegroundColor Gray
        Write-Host "     { `"username`": `"admin`", `"password`": `"...`", `"email`": `"...`" }" -ForegroundColor Gray
        Write-Host ""
    }
    else {
        Write-SetupLog "Sin bootstrap token: la base ya estaba inicializada." -Level Detail
    }
}
