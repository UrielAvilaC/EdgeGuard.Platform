#Requires -Version 5.1
<#
.SYNOPSIS
    Paso 07 — Variables de entorno del app pool

.DESCRIPTION
    Escribe TODA la configuración específica del despliegue como variables de
    entorno del app pool. appsettings.Production.json queda vacío a propósito,
    de modo que el estado vive en IIS y una actualización nunca tiene que
    fusionar archivos de configuración.

    Los nombres de variable están tomados del código, no de la guía de
    despliegue, que tenía dos mal:

      EDGEGUARD_HUB_CONNECTIONSTRING  (no HUB_DB_CONNECTION_STRING)
      Jwt__SecretKey                  (no Jwt__Secret)
#>

<#
.SYNOPSIS
    Genera un secreto de firma criptográficamente aleatorio.
.DESCRIPTION
    64 caracteres del alfabeto base64url. JwtTokenService exige 32 como mínimo y
    aborta el arranque por debajo de eso.
#>
function New-JwtSecret {
    $bytes = New-Object byte[] 48
    $rng   = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try { $rng.GetBytes($bytes) } finally { $rng.Dispose() }

    return ([Convert]::ToBase64String($bytes) -replace '\+', '-' -replace '/', '_' -replace '=', '').Substring(0, 64)
}

<#
.SYNOPSIS
    Lee las variables de entorno actuales del app pool.
#>
function Get-AppPoolEnvironment {
    [CmdletBinding()]
    param([string]$AppPoolName)

    $result = @{}
    $collection = (Get-ItemProperty "IIS:\AppPools\$AppPoolName" -Name environmentVariables -ErrorAction SilentlyContinue)

    if ($collection -and $collection.Collection) {
        foreach ($entry in $collection.Collection) {
            $result[$entry.name] = $entry.value
        }
    }
    return $result
}

<#
.SYNOPSIS
    Construye el conjunto completo de variables a partir de la configuración.
#>
function Build-HubEnvironment {
    [CmdletBinding()]
    param([hashtable]$Config, [string]$JwtSecret)

    $connectionString =
        "Host=$($Config.DbHost);Port=$($Config.DbPort);Database=$($Config.DbName);" +
        "Username=$($Config.DbUser);Password=$($Config.DbPassword)"

    $env = [ordered]@{
        'ASPNETCORE_ENVIRONMENT'          = 'Production'
        'EDGEGUARD_HUB_CONNECTIONSTRING'  = $connectionString
        'Jwt__SecretKey'                  = $JwtSecret
        'Jwt__Issuer'                     = "http://$($Config.HostHeader)"
        'DataProtection__KeyPath'         = $Config.DataProtectionKeyPath
        'Diagnostics__InstanceId'         = $Config.InstanceId
        'Diagnostics__Redaction__Mode'    = $Config.RedactionMode
        'Hl7Listener__Enabled'            = $Config.Hl7Enabled.ToString().ToLowerInvariant()
        'Hl7Listener__Port'               = $Config.Hl7Port.ToString()
        'Hl7Listener__ValidateBeforeAck'  = $Config.Hl7ValidateBeforeAck.ToString().ToLowerInvariant()
        'NodeAuth__Enforce'               = $Config.NodeAuthEnforce.ToString().ToLowerInvariant()
    }

    # ── Administrador inicial ────────────────────────────────────────────────
    # AdminUserSeed las lee EXCLUSIVAMENTE del entorno —nunca de archivos de
    # configuración— y es idempotente: si la cuenta ya existe, no hace nada.
    #
    # Ambas se retiran del app pool en el paso 10, en cuanto se confirma que la
    # cuenta quedó creada y puede iniciar sesión. Dejar la contraseña aquí de
    # forma permanente la pondría en claro dentro de applicationHost.config,
    # legible por cualquiera que pueda leer ese archivo, para siempre.
    if (-not [string]::IsNullOrWhiteSpace([string]$Config.AdminPassword)) {
        $env['EDGEGUARD_ADMIN_USERNAME'] = ([string]$Config.AdminUsername).Trim().ToLowerInvariant()
        $env['EDGEGUARD_ADMIN_PASSWORD'] = [string]$Config.AdminPassword
    }

    # Los arreglos de configuración se indexan: Cors__AllowedOrigins__0, __1, ...
    $i = 0
    foreach ($origin in @($Config.CorsAllowedOrigins)) {
        if ([string]::IsNullOrWhiteSpace($origin)) { continue }
        $env["Cors__AllowedOrigins__$i"] = $origin
        $i++
    }

    return $env
}

function Step-SetAppPoolConfig {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][hashtable]$Config,
        [Parameter(Mandatory)][hashtable]$State
    )

    Import-Module WebAdministration -ErrorAction Stop
    $poolPath = "IIS:\AppPools\$($Config.AppPoolName)"

    # ── Secreto de firma ─────────────────────────────────────────────────────
    # Se conserva el existente. Rotarlo en una actualización invalidaría todas
    # las sesiones activas y sacaría a los usuarios de la SPA sin explicación.
    $existing  = if (Test-SetupDryRun) { @{} } else { Get-AppPoolEnvironment -AppPoolName $Config.AppPoolName }
    $jwtSecret = $null

    if ($existing.ContainsKey('Jwt__SecretKey') -and
        -not [string]::IsNullOrWhiteSpace($existing['Jwt__SecretKey'])) {
        $jwtSecret = $existing['Jwt__SecretKey']
        Write-SetupLog "Se conserva el Jwt__SecretKey existente (rotarlo cerraría todas las sesiones)" -Level Detail
    }
    else {
        $jwtSecret = New-JwtSecret
        Write-SetupLog "Jwt__SecretKey generado ($($jwtSecret.Length) caracteres)" -Level Detail
    }

    Register-SetupSecret $jwtSecret

    # ── Composición ──────────────────────────────────────────────────────────
    $environment = Build-HubEnvironment -Config $Config -JwtSecret $jwtSecret
    Register-SetupSecret $environment['EDGEGUARD_HUB_CONNECTIONSTRING']
    if ($environment.Contains('EDGEGUARD_ADMIN_PASSWORD')) {
        Register-SetupSecret $environment['EDGEGUARD_ADMIN_PASSWORD']
    }

    foreach ($name in $environment.Keys) {
        Write-SetupLog "$name = $($environment[$name])" -Level Detail
    }

    # ── Escritura ────────────────────────────────────────────────────────────
    # Se reemplaza la colección completa en vez de fusionar: así una variable
    # retirada de la configuración desaparece de verdad, en lugar de quedar
    # viva de una instalación anterior.
    Invoke-SetupAction -Description "escribir $($environment.Count) variables de entorno en el app pool" -Action {
        $collection = @()
        foreach ($name in $environment.Keys) {
            $collection += @{ name = $name; value = [string]$environment[$name] }
        }
        Set-ItemProperty $poolPath -Name environmentVariables -Value $collection -ErrorAction Stop
    } | Out-Null

    if (Test-SetupDryRun) { return }

    # ── Postcondición ────────────────────────────────────────────────────────
    $written  = Get-AppPoolEnvironment -AppPoolName $Config.AppPoolName
    $required = @('EDGEGUARD_HUB_CONNECTIONSTRING', 'Jwt__SecretKey', 'DataProtection__KeyPath')

    # Si se configuró contraseña de administrador, que llegara al app pool es
    # tan obligatorio como el resto: es la única vía por la que AdminUserSeed
    # la recibe, y si falta el Hub se limita a un LogWarning y no crea la cuenta.
    if ($environment.Contains('EDGEGUARD_ADMIN_PASSWORD')) {
        $required += 'EDGEGUARD_ADMIN_PASSWORD'
    }

    foreach ($name in $required) {
        if (-not $written.ContainsKey($name) -or [string]::IsNullOrWhiteSpace($written[$name])) {
            throw "La variable $name no quedó escrita en el app pool."
        }
    }

    Write-SetupLog "$($written.Count) variables verificadas en el app pool" -Level Detail
}
