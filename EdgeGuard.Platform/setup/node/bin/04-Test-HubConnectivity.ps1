#Requires -Version 5.1
<#
.SYNOPSIS
    Paso 04 — Conectividad con el Hub

.DESCRIPTION
    El equivalente del 04-Test-Database del Hub, y con el mismo propósito:
    fallar aquí cuesta una corrección del .psd1; fallar después cuesta una
    visita al servidor.

    Un nodo que no alcanza su Hub arranca sin quejarse, responde /health/live y
    el único síntoma es que nunca aparece en la consola. Es un fallo silencioso
    y lo que lo produce, casi siempre, es una dirección mal escrita.

    Corre completo también en -DryRun: es solo lectura.

    Lo que este paso NO hace es pedir un token de arranque a POST /api/edge/token.
    Ese endpoint es anónimo y emitiría un token real, de un solo uso, que nadie
    va a consumir — basura en la tabla del Hub por cada verificación. El nodo
    pide el suyo al arrancar.
#>

<#
.SYNOPSIS
    Resuelve el host del Hub y devuelve las direcciones a las que apunta.
.DESCRIPTION
    Se registran: un nombre que resuelve a la IP equivocada produce exactamente
    el mismo error que uno inalcanzable, y sin este dato los dos casos se
    diagnostican igual durante media hora.
#>
function Resolve-HubHost {
    [CmdletBinding()]
    param([Uri]$HubUri)

    if ($HubUri.HostNameType -in @([UriHostNameType]::IPv4, [UriHostNameType]::IPv6)) {
        Write-SetupLog "host: $($HubUri.Host) (dirección literal, sin DNS)" -Level Detail
        return @($HubUri.Host)
    }

    try {
        $direcciones = @([System.Net.Dns]::GetHostAddresses($HubUri.Host) |
            ForEach-Object { $_.IPAddressToString })
    }
    catch {
        throw ("El nombre '$($HubUri.Host)' no resuelve en este servidor: $($_.Exception.Message). " +
               "Revisa el DNS de la sede o usa la IP del Hub en 'HubBaseUrl'.")
    }

    if ($direcciones.Count -eq 0) {
        throw "El nombre '$($HubUri.Host)' no resolvió a ninguna dirección."
    }

    Write-SetupLog "host: $($HubUri.Host) → $($direcciones -join ', ')" -Level Detail
    return $direcciones
}

<#
.SYNOPSIS
    Llama a un endpoint del Hub y devuelve la respuesta, o el motivo del fallo.
#>
function Invoke-HubEndpoint {
    [CmdletBinding()]
    param([string]$Url, [int]$TimeoutSeconds)

    try {
        $respuesta = Invoke-RestMethod -Uri $Url -Method Get -TimeoutSec $TimeoutSeconds -ErrorAction Stop
        return @{ Ok = $true; Body = $respuesta; Error = $null }
    }
    catch {
        return @{ Ok = $false; Body = $null; Error = $_.Exception.Message }
    }
}

<#
.SYNOPSIS
    Comprueba la cadena del certificado cuando la URL es https.
.DESCRIPTION
    El nodo no acepta certificados que el servidor no valide. Un autofirmado
    deja al nodo sin poder conectar, y el síntoma —sin esta verificación— es un
    nodo instalado que nunca se registra, con un error de TLS enterrado en su
    registro diario.
#>
function Test-HubCertificate {
    [CmdletBinding()]
    param([Uri]$HubUri, [int]$TimeoutSeconds)

    if ($HubUri.Scheme -ne 'https') { return }

    $cliente = New-Object System.Net.Sockets.TcpClient
    try {
        $tarea = $cliente.ConnectAsync($HubUri.Host, $HubUri.Port)
        if (-not $tarea.Wait($TimeoutSeconds * 1000)) {
            throw "sin respuesta en $TimeoutSeconds s"
        }

        $validado = $true
        $ssl = New-Object System.Net.Security.SslStream(
            $cliente.GetStream(), $false,
            [System.Net.Security.RemoteCertificateValidationCallback]{
                param($remitente, $certificado, $cadena, $erroresSsl)
                $script:ErroresTls = $erroresSsl
                return $true   # se acepta para poder inspeccionar; el veredicto es nuestro
            })

        $ssl.AuthenticateAsClient($HubUri.Host)
        $cert = $ssl.RemoteCertificate

        if ($script:ErroresTls -ne [System.Net.Security.SslPolicyErrors]::None) {
            $validado = $false
            Write-SetupLog ("AVISO  El certificado del Hub no valida: $($script:ErroresTls). " +
                            "El nodo NO podrá conectar por https — instala un certificado de " +
                            "confianza en el Hub, o usa http si la red entre sede y Hub es privada.") -Level Warn
        }

        if ($cert) {
            $x509 = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($cert)
            Write-SetupLog "certificado: $($x509.Subject) — vence $($x509.NotAfter.ToString('yyyy-MM-dd'))" -Level Detail
        }

        $ssl.Dispose()
        return $validado
    }
    catch {
        Write-SetupLog "no se pudo inspeccionar el certificado: $($_.Exception.Message)" -Level Detail
        return $null
    }
    finally { $cliente.Dispose() }
}

# ─────────────────────────────────────────────────────────────────────────────

function Step-TestHubConnectivity {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][hashtable]$Config,
        [Parameter(Mandatory)][hashtable]$State
    )

    $hubUri  = [Uri]$Config.HubBaseUrl
    $timeout = [int]$Config.TimeoutSeconds

    $fallos = New-Object System.Collections.Generic.List[string]

    # ── DNS ──────────────────────────────────────────────────────────────────
    try   { Resolve-HubHost -HubUri $hubUri | Out-Null }
    catch { $fallos.Add($_.Exception.Message) }

    # ── TLS ──────────────────────────────────────────────────────────────────
    if ($fallos.Count -eq 0) {
        Test-HubCertificate -HubUri $hubUri -TimeoutSeconds $timeout | Out-Null
    }

    # ── ¿Alcanzable, y es un Hub? ────────────────────────────────────────────
    # /api/info es anónimo y devuelve servicio, versión y capacidades. Que
    # conteste 200 no basta: un portal cautivo o un proxy también lo hacen. Lo
    # que prueba que hay un Hub detrás es el contenido.
    if ($fallos.Count -eq 0) {
        $info = Invoke-HubEndpoint -Url "$($hubUri.AbsoluteUri.TrimEnd('/'))/api/info" -TimeoutSeconds $timeout

        if (-not $info.Ok) {
            $fallos.Add("El Hub no respondió a /api/info: $($info.Error)")
        }
        elseif (-not $info.Body -or -not $info.Body.PSObject.Properties.Name.Contains('service')) {
            $fallos.Add(("La dirección respondió, pero no como un Hub EdgeGuard: /api/info no devolvió " +
                         "el campo 'service'. Revisa que 'HubBaseUrl' apunte al Hub y no a un proxy " +
                         "o a un portal de la red."))
        }
        else {
            $State.HubInfo = $info.Body
            $version = if ($info.Body.PSObject.Properties.Name.Contains('version')) { $info.Body.version } else { '(sin versión)' }
            Write-SetupLog "Hub alcanzable: $($info.Body.service) $version" -Level Detail

            # ── Versiones ────────────────────────────────────────────────────
            # Aviso, no error: nodo y Hub se despliegan por separado y una
            # diferencia de patch es normal. Una de major merece una mirada
            # antes de instalar, no después de que algo no cuadre.
            # Del nombre del archivo, no de la ruta: un directorio con números
            # (C:\Instaladores\v2.1.0\…) daría una versión que no es la del paquete.
            $nombrePaquete  = Split-Path -Leaf $State.Package
            $paqueteVersion = if ($nombrePaquete -match '(\d+\.\d+\.\d+)') { $Matches[1] } else { $null }
            if ($paqueteVersion -and $version -match '^\d+\.') {
                $majorHub    = ([version]$version).Major
                $majorNodo   = ([version]$paqueteVersion).Major
                if ($majorHub -ne $majorNodo) {
                    # ${} obligatorio: sin las llaves, PowerShell lee '$paqueteVersion:'
                    # como un ámbito de variable y el script no parsea.
                    Write-SetupLog ("AVISO  El Hub es $version y el paquete del nodo ${paqueteVersion}: " +
                                    "difieren en versión mayor. Revisa que sean compatibles antes de continuar.") -Level Warn
                }
            }
        }
    }

    # ── ¿El Hub está sano? ───────────────────────────────────────────────────
    # Distingue "IIS sirve" de "el Hub arrancó": un Hub con su base caída
    # responde lo primero y no lo segundo.
    if ($fallos.Count -eq 0) {
        $salud = Invoke-HubEndpoint -Url "$($hubUri.AbsoluteUri.TrimEnd('/'))/health/live" -TimeoutSeconds $timeout
        if (-not $salud.Ok) {
            Write-SetupLog ("AVISO  /api/info respondió pero /health/live no: $($salud.Error). " +
                            "El Hub puede estar arrancando o con su base caída.") -Level Warn
        }
        else {
            Write-SetupLog "Hub sano (/health/live)" -Level Detail
        }
    }

    # ── Token de arranque ────────────────────────────────────────────────────
    # Solo formato. Validarlo contra el Hub lo consumiría: es de un solo uso y
    # el nodo se quedaría sin él justo al arrancar.
    $token = [string]$Config.BootstrapToken
    if (-not [string]::IsNullOrWhiteSpace($token)) {
        if ($token.Length -lt 16) {
            $fallos.Add("'BootstrapToken' es demasiado corto para ser un token emitido por el Hub. Déjalo vacío para que el nodo pida el suyo.")
        }
        else {
            Write-SetupLog "token de arranque presente: $(Format-SetupMasked $token) (no se valida: es de un solo uso)" -Level Detail
        }
    }

    # ── Veredicto ────────────────────────────────────────────────────────────
    if ($fallos.Count -eq 0) { return }

    foreach ($f in $fallos) { Write-SetupLog $f -Level Detail }

    if ($State.SkipHubCheck) {
        Write-SetupLog ("El Hub no responde, pero -SkipHubCheck está activo: se continúa. El nodo " +
                        "se registrará cuando el Hub esté disponible.") -Level Warn
        return
    }

    throw ("No se pudo verificar el Hub en '$($Config.HubBaseUrl)'. Corrige 'HubBaseUrl' en el " +
           ".psd1, o vuelve a ejecutar con -SkipHubCheck si el Hub todavía no existe y quieres " +
           "instalar igualmente.")
}
