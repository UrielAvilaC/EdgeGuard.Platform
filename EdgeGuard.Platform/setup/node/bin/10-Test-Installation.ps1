#Requires -Version 5.1
<#
.SYNOPSIS
    Paso 10 — Verificación post-instalación

.DESCRIPTION
    Arranca el servicio y comprueba que el nodo haga las cuatro cosas por las que
    se instaló: arrancar, responder, escuchar DICOM y registrarse con el Hub.

    Una instalación que termina sin verificar no es una instalación: es un
    despliegue de archivos. El caso que este paso existe para atrapar es el nodo
    que arranca, se queda en pie y nunca aparece en la consola del Hub — que sin
    esta comprobación se descubre días después, cuando alguien pregunta por los
    estudios de esa sede.
#>

# Cuánto se espera a que el nodo responda tras arrancar. Migrar la base en una
# instalación nueva y sembrar los ajustes toma unos segundos.
$script:ReadyTimeoutSeconds = 90

# Cuánto se espera al registro con el Hub después de que el nodo responda.
$script:RegisterTimeoutSeconds = 60

<#
.SYNOPSIS
    Arranca el servicio y espera a que el SCM lo dé por iniciado.
#>
function Start-NodeService {
    [CmdletBinding()]
    param([string]$ServiceName)

    Invoke-SetupAction -Description "arrancar el servicio $ServiceName" -Action {
        Start-Service -Name $ServiceName -ErrorAction Stop

        $limite = (Get-Date).AddSeconds(30)
        while ((Get-Date) -lt $limite) {
            $svc = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
            if ($svc -and $svc.Status -eq 'Running') { return }
            Start-Sleep -Milliseconds 500
        }

        throw "El servicio no llegó a 'Running' en 30 s."
    } | Out-Null
}

<#
.SYNOPSIS
    Espera a que /health/ready conteste y devuelve la respuesta.
.DESCRIPTION
    Se sondea /health/ready y no /health/live porque lo segundo solo dice que el
    proceso vive. Lo que interesa saber es si el nodo terminó de migrar su base,
    sembrar sus ajustes y abrir sus puertos.
#>
function Wait-NodeReady {
    [CmdletBinding()]
    param([int]$Port, [int]$TimeoutSeconds)

    $url    = "http://localhost:$Port/health/ready"
    $limite = (Get-Date).AddSeconds($TimeoutSeconds)
    $ultimo = $null

    while ((Get-Date) -lt $limite) {
        try {
            # /health/ready responde 503 cuando algo no está sano, y en PS 5.1 eso
            # es una excepción. Interesa el cuerpo en ambos casos: dice cuál de las
            # tres revisiones falla, que es justo lo que hay que reportar.
            return Invoke-RestMethod -Uri $url -TimeoutSec 10 -ErrorAction Stop
        }
        catch {
            $ultimo = $_
            $respuesta = $_.Exception.Response
            if ($respuesta) {
                try {
                    $lector = New-Object System.IO.StreamReader($respuesta.GetResponseStream())
                    $cuerpo = $lector.ReadToEnd()
                    $lector.Dispose()
                    if ($cuerpo) { return ($cuerpo | ConvertFrom-Json) }
                }
                catch { }
            }
            Start-Sleep -Seconds 2
        }
    }

    throw ("El nodo no respondió en http://localhost:$Port/health/ready tras $TimeoutSeconds s. " +
           "Último error: $($ultimo.Exception.Message). Revisa el registro de arranque en " +
           "logs\node-bootstrap-*.log dentro del directorio de binarios.")
}

<#
.SYNOPSIS
    Informa el resultado de cada revisión de salud.
#>
function Write-NodeReadiness {
    [CmdletBinding()]
    param($Salud)

    if (-not $Salud) { return 'desconocido' }

    $estado = if ($Salud.PSObject.Properties.Name -contains 'status') { $Salud.status } else { 'desconocido' }

    if ($Salud.PSObject.Properties.Name -contains 'checks') {
        foreach ($check in $Salud.checks) {
            $nivel = if ($check.status -eq 'Healthy') { 'Detail' } else { 'Warn' }
            $texto = "$($check.name): $($check.status)"
            if ($check.description) { $texto += " — $($check.description)" }
            Write-SetupLog $texto -Level $nivel
        }
    }

    return $estado
}

<#
.SYNOPSIS
    ¿Está el nodo escuchando DICOM?
#>
function Test-NodeDicomListening {
    [CmdletBinding()]
    param([int]$Port)

    $escucha = @(Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue)
    if ($escucha.Count -eq 0) {
        throw ("El nodo arrancó pero NO está escuchando DICOM en el puerto $Port. Las modalidades " +
               "no podrán enviarle nada. Suele ser el puerto ocupado por otro proceso — revisa el " +
               "registro del nodo del día de hoy.")
    }

    Write-SetupLog "escuchando DICOM en el puerto $Port" -Level Detail
}

<#
.SYNOPSIS
    Confirma que el nodo aparece registrado en el Hub.
.DESCRIPTION
    Es la comprobación que justifica el paso entero. El nodo pide su token, se
    registra y guarda la credencial; hasta que eso ocurre no existe para el Hub,
    y nada en el servidor de la sede lo delata.
#>
function Wait-NodeRegistered {
    [CmdletBinding()]
    param([hashtable]$Config, [int]$TimeoutSeconds)

    $limite = (Get-Date).AddSeconds($TimeoutSeconds)

    while ((Get-Date) -lt $limite) {
        try {
            $salud = Invoke-RestMethod -Uri "http://localhost:$($Config.NodeApiPort)/health/ready" -TimeoutSec 10 -ErrorAction SilentlyContinue
        }
        catch { $salud = $null }

        # El nodo escribe su credencial en la base al registrarse. Leerla desde
        # aquí exigiría un cliente de SQLite que el servidor no tiene, así que se
        # comprueba por el efecto observable: el registro del día lo dice.
        $hoy = Join-Path $Config.LogPath "node-$(Get-Date -Format yyyyMMdd).log"
        if (Test-Path -LiteralPath $hoy) {
            $lineas = Get-Content -LiteralPath $hoy -Tail 400 -ErrorAction SilentlyContinue
            $registrado = $lineas | Where-Object {
                $_ -match 'registration|registered|NodeId' -and $_ -notmatch 'skipping'
            }
            if ($registrado) {
                Write-SetupLog "el nodo reporta registro con el Hub" -Level Detail
                return $true
            }
        }

        Start-Sleep -Seconds 3
    }

    return $false
}

# ─────────────────────────────────────────────────────────────────────────────

function Step-TestInstallation {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][hashtable]$Config,
        [Parameter(Mandatory)][hashtable]$State
    )

    if (Test-SetupDryRun) {
        Write-SetupLog "haría: arrancar el servicio y verificar salud, DICOM y registro" -Level Detail
        return
    }

    # ── 1. Arrancar ──────────────────────────────────────────────────────────
    Start-NodeService -ServiceName $Config.ServiceName
    Write-SetupLog "servicio en marcha" -Level Detail

    # ── 2. Responder ─────────────────────────────────────────────────────────
    $salud  = Wait-NodeReady -Port $Config.NodeApiPort -TimeoutSeconds $script:ReadyTimeoutSeconds
    $estado = Write-NodeReadiness -Salud $salud

    if ($estado -eq 'Unhealthy') {
        throw ("El nodo arrancó pero reporta 'Unhealthy'. Revisa arriba cuál de las revisiones " +
               "falla: 'storage' es espacio en disco, 'database' es la base del nodo y 'pacs' es " +
               "el PACS destino, que todavía puede no estar configurado.")
    }

    if ($estado -eq 'Degraded') {
        # Degradado no aborta: en una instalación nueva lo normal es que 'pacs'
        # esté degradado porque nadie ha configurado el PACS destino todavía. Eso
        # se hace desde la consola del Hub, después de instalar.
        Write-SetupLog ("El nodo responde 'Degraded'. En una instalación nueva suele ser el PACS " +
                        "sin configurar: se asigna desde la consola del Hub.") -Level Warn
    }
    else {
        Write-SetupLog "salud: $estado" -Level Ok
    }

    # ── 3. Escuchar DICOM ────────────────────────────────────────────────────
    Test-NodeDicomListening -Port $Config.DicomPort

    # ── 4. Registrarse con el Hub ────────────────────────────────────────────
    if ($State.SkipHubCheck) {
        Write-SetupLog ("-SkipHubCheck activo: no se espera el registro con el Hub. El nodo lo " +
                        "intentará solo cuando el Hub esté disponible.") -Level Warn
    }
    else {
        $registrado = Wait-NodeRegistered -Config $Config -TimeoutSeconds $script:RegisterTimeoutSeconds
        if (-not $registrado) {
            Write-SetupLog ("El nodo no reportó registro con el Hub en $($script:RegisterTimeoutSeconds) s. " +
                            "El servicio está en marcha y reintentará solo, pero conviene confirmarlo " +
                            "en la consola del Hub antes de dar la sede por instalada: busca " +
                            "'$($Config.NodeName)' en la lista de nodos.") -Level Warn
        }
    }

    # ── Resumen ──────────────────────────────────────────────────────────────
    Write-SetupLog "" -Level Detail
    Write-SetupLog "Nodo '$($Config.NodeName)' instalado y en marcha." -Level Ok
    Write-SetupLog "AE title       $($Config.AeTitle)" -Level Detail
    Write-SetupLog "DICOM          puerto $($Config.DicomPort) — configúralo así en las modalidades" -Level Detail
    Write-SetupLog "Salud          http://localhost:$($Config.NodeApiPort)/health/ready" -Level Detail
    Write-SetupLog "Hub            $($Config.HubBaseUrl)" -Level Detail
    Write-SetupLog "Registro       $($Config.LogPath)" -Level Detail
    Write-SetupLog "" -Level Detail
    Write-SetupLog "Queda por hacer desde la consola del Hub: asignar el PACS destino y las reglas de ruteo." -Level Detail
}
