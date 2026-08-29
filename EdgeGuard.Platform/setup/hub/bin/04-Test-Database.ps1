#Requires -Version 5.1
<#
.SYNOPSIS
    Paso 04 — Conectividad y permisos de PostgreSQL

.DESCRIPTION
    El rol y la base ya existen: este paso solo verifica, nunca crea.

    Comprueba en tres niveles, del más barato al más informativo, y deja
    constancia de hasta dónde llegó:

      1. Alcance TCP del puerto.
      2. Sondeo del protocolo PostgreSQL: confirma que al otro lado hay un
         PostgreSQL de verdad —no otro servicio ocupando el puerto— y reporta
         qué método de autenticación exige.
      3. Verificación completa con psql, si está disponible: autenticación real,
         permiso CREATE en el esquema y estado de las migraciones.

    Sobre el alcance real del nivel 2: PostgreSQL valida el rol y la base
    DESPUÉS de completar la autenticación, así que un rol o una base
    inexistentes NO se detectan ahí en un servidor que pide contraseña. Solo
    llegan temprano los rechazos de pg_hba.conf y, con autenticación trust, los
    errores de base inexistente. Cuando eso ocurre se reporta el SQLSTATE.

.NOTES
    No se usa Npgsql: el ensamblado del paquete es net10.0 y PowerShell 5.1
    corre sobre .NET Framework, que no puede cargarlo. Cuando psql no está
    presente y el servidor exige SCRAM, la validación de credenciales queda
    diferida al paso 10, que arranca el Hub y espera /health/live — y ahí una
    contraseña incorrecta se manifiesta de inmediato.
#>

$script:ConnectTimeoutMs = 5000

<#
.SYNOPSIS
    Nivel 1 — ¿responde el puerto?
#>
function Test-PostgresPort {
    [CmdletBinding()]
    param([string]$DbHost, [int]$Port)

    $client = New-Object System.Net.Sockets.TcpClient
    try {
        $async = $client.BeginConnect($DbHost, $Port, $null, $null)
        if (-not $async.AsyncWaitHandle.WaitOne($script:ConnectTimeoutMs)) {
            throw "Sin respuesta de ${DbHost}:${Port} en $($script:ConnectTimeoutMs) ms."
        }
        $client.EndConnect($async)
        return $true
    }
    catch {
        throw "No se puede alcanzar PostgreSQL en ${DbHost}:${Port} — $($_.Exception.Message)"
    }
    finally {
        $client.Close()
    }
}

<#
.SYNOPSIS
    Nivel 2 — sondeo del protocolo PostgreSQL 3.0.
.DESCRIPTION
    Envía un StartupMessage y lee la primera respuesta del servidor:

      'R' → petición de autenticación. Prueba que hay un PostgreSQL real
            hablando el protocolo 3.0 e indica el método exigido. NO prueba que
            el rol o la base existan: eso se valida tras autenticar.
      'E' → ErrorResponse antes de autenticar, típicamente un rechazo de
            pg_hba.conf. Trae SQLSTATE y se reporta tal cual.

    Nada de esto requiere la contraseña, así que funciona incluso cuando la
    verificación completa no es posible.
#>
function Test-PostgresProtocol {
    [CmdletBinding()]
    param([string]$DbHost, [int]$Port, [string]$User, [string]$Database)

    $client = New-Object System.Net.Sockets.TcpClient
    try {
        $client.Connect($DbHost, $Port)
        $stream = $client.GetStream()
        $stream.ReadTimeout = $script:ConnectTimeoutMs

        # StartupMessage: Int32 longitud, Int32 protocolo (3.0), pares clave\0valor\0, \0 final
        $payload = New-Object System.IO.MemoryStream
        $writer  = New-Object System.IO.BinaryWriter($payload)

        function Write-CString {
            param($BinaryWriter, [string]$Text)
            $BinaryWriter.Write([System.Text.Encoding]::UTF8.GetBytes($Text))
            $BinaryWriter.Write([byte]0)
        }

        # Big-endian: .NET escribe little-endian, así que se invierte a mano.
        $protocol = [BitConverter]::GetBytes([int]196608)
        [Array]::Reverse($protocol)
        $writer.Write($protocol)

        Write-CString $writer 'user';     Write-CString $writer $User
        Write-CString $writer 'database'; Write-CString $writer $Database
        $writer.Write([byte]0)
        $writer.Flush()

        $body   = $payload.ToArray()
        $length = [BitConverter]::GetBytes([int]($body.Length + 4))
        [Array]::Reverse($length)

        $stream.Write($length, 0, 4)
        $stream.Write($body, 0, $body.Length)
        $stream.Flush()

        # Respuesta: Byte1 tipo, Int32 longitud, cuerpo
        $header = New-Object byte[] 5
        $read   = $stream.Read($header, 0, 5)
        if ($read -lt 5) {
            throw "El servidor cerró la conexión sin responder. ¿Es realmente PostgreSQL?"
        }

        $type    = [char]$header[0]
        $lenRaw  = $header[1..4]
        [Array]::Reverse($lenRaw)
        $bodyLen = [BitConverter]::ToInt32($lenRaw, 0) - 4

        $buffer = New-Object byte[] ([Math]::Max($bodyLen, 0))
        if ($bodyLen -gt 0) {
            $offset = 0
            while ($offset -lt $bodyLen) {
                $n = $stream.Read($buffer, $offset, $bodyLen - $offset)
                if ($n -le 0) { break }
                $offset += $n
            }
        }

        switch ($type) {
            'R' {
                $authRaw = $buffer[0..3]
                [Array]::Reverse($authRaw)
                $authType = [BitConverter]::ToInt32($authRaw, 0)

                $method = switch ($authType) {
                    0  { 'sin contraseña (trust)' }
                    3  { 'contraseña en claro' }
                    5  { 'MD5' }
                    10 { 'SCRAM-SHA-256' }
                    default { "tipo $authType" }
                }
                return @{ Ok = $true; AuthMethod = $method; AuthType = $authType }
            }
            'E' {
                # Campos: Byte1 código, cadena terminada en \0; termina en \0
                $fields = @{}
                $i = 0
                while ($i -lt $buffer.Length -and $buffer[$i] -ne 0) {
                    $code = [char]$buffer[$i]; $i++
                    $start = $i
                    while ($i -lt $buffer.Length -and $buffer[$i] -ne 0) { $i++ }
                    $fields[$code] = [System.Text.Encoding]::UTF8.GetString($buffer, $start, $i - $start)
                    $i++
                }
                return @{
                    Ok       = $false
                    SqlState = $(if ($fields.ContainsKey('C')) { $fields['C'] } else { '?????' })
                    Message  = $(if ($fields.ContainsKey('M')) { $fields['M'] } else { 'sin descripción' })
                }
            }
            default {
                throw "Respuesta inesperada del servidor (tipo '$type'). ¿Es realmente PostgreSQL?"
            }
        }
    }
    finally {
        $client.Close()
    }
}

function Find-Psql {
    $cmd = Get-Command psql.exe -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }

    $roots = @("$env:ProgramFiles\PostgreSQL", "${env:ProgramFiles(x86)}\PostgreSQL")
    foreach ($root in $roots) {
        if (-not (Test-Path -LiteralPath $root)) { continue }
        $found = Get-ChildItem -LiteralPath $root -Filter 'psql.exe' -Recurse -ErrorAction SilentlyContinue |
                 Sort-Object FullName -Descending | Select-Object -First 1
        if ($found) { return $found.FullName }
    }
    return $null
}

<#
.SYNOPSIS
    Nivel 3 — verificación completa con psql.
#>
function Test-PostgresWithPsql {
    [CmdletBinding()]
    param([string]$Psql, [hashtable]$Config)

    $query = @"
SELECT
  has_schema_privilege(current_user, 'public', 'CREATE') AS can_create,
  (SELECT count(*) FROM information_schema.tables
    WHERE table_schema = 'public' AND table_name = '__EFMigrationsHistory') AS has_history;
"@

    $previous = $env:PGPASSWORD
    try {
        # PGPASSWORD evita el prompt interactivo; se limpia en el finally para no
        # dejar la contraseña en el entorno del proceso más de lo necesario.
        $env:PGPASSWORD = $Config.DbPassword

        $output = & $Psql `
            --host=$($Config.DbHost) --port=$($Config.DbPort) `
            --username=$($Config.DbUser) --dbname=$($Config.DbName) `
            --no-password --tuples-only --no-align --field-separator='|' `
            --command=$query 2>&1

        if ($LASTEXITCODE -ne 0) {
            throw "psql falló: $($output -join ' ')"
        }

        $row = ($output | Where-Object { $_ -match '\|' } | Select-Object -First 1)
        if (-not $row) { throw "psql no devolvió resultados legibles: $($output -join ' ')" }

        $parts = $row.Split('|')
        return @{
            CanCreate  = ($parts[0].Trim() -eq 't')
            HasHistory = ([int]$parts[1].Trim() -gt 0)
        }
    }
    finally {
        $env:PGPASSWORD = $previous
    }
}

function Step-TestDatabase {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][hashtable]$Config,
        [Parameter(Mandatory)][hashtable]$State
    )

    # ── Nivel 1 ──────────────────────────────────────────────────────────────
    Test-PostgresPort -DbHost $Config.DbHost -Port $Config.DbPort | Out-Null
    Write-SetupLog "Puerto $($Config.DbHost):$($Config.DbPort) alcanzable" -Level Detail

    # ── Nivel 2 ──────────────────────────────────────────────────────────────
    $probe = Test-PostgresProtocol -DbHost $Config.DbHost -Port $Config.DbPort `
                                   -User $Config.DbUser -Database $Config.DbName

    if (-not $probe.Ok) {
        $hint = switch ($probe.SqlState) {
            '3D000' { "La base '$($Config.DbName)' no existe. El instalador no la crea: créala antes." }
            '28000' { "El rol '$($Config.DbUser)' no existe o pg_hba.conf rechaza la conexión desde este servidor." }
            '28P01' { "Contraseña incorrecta para el rol '$($Config.DbUser)'." }
            default { '' }
        }
        throw ("PostgreSQL rechazó la conexión [$($probe.SqlState)]: $($probe.Message). $hint").Trim()
    }

    Write-SetupLog "Servidor PostgreSQL confirmado; autenticación por $($probe.AuthMethod)" -Level Detail

    # ── Nivel 3 ──────────────────────────────────────────────────────────────
    $psql = Find-Psql
    if (-not $psql) {
        Write-SetupLog ("psql no está disponible en este servidor: no se puede verificar aquí la " +
                        "contraseña ni el permiso CREATE en el esquema. Ambos se comprobarán al " +
                        "arrancar el Hub en el paso 10, donde un fallo es igual de visible.") -Level Warn
        $State['DbVerification'] = 'protocolo'
        return
    }

    Write-SetupLog "psql: $psql" -Level Detail
    $result = Test-PostgresWithPsql -Psql $psql -Config $Config

    if (-not $result.CanCreate) {
        throw ("El rol '$($Config.DbUser)' no tiene permiso CREATE en el esquema public. " +
               "El Hub aplica las migraciones de EF al arrancar y fallaría entonces, " +
               "cuando el diagnóstico ya es más caro. Concede: " +
               "GRANT CREATE ON SCHEMA public TO $($Config.DbUser);")
    }
    Write-SetupLog "Autenticación correcta y permiso CREATE en el esquema public" -Level Detail

    $State['DbVerification'] = 'completa'
    $State['DbMigrated']     = $result.HasHistory

    Write-SetupLog $(if ($result.HasHistory) {
                        'La base ya tiene historial de migraciones: el Hub aplicará solo las pendientes.'
                     } else {
                        'Base vacía: el Hub creará el esquema y emitirá un bootstrap token en el primer arranque.'
                     }) -Level Detail
}
