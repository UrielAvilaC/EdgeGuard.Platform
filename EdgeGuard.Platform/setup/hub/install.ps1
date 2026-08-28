#Requires -Version 5.1
<#
.SYNOPSIS
    Instalador del Hub EdgeGuard sobre Windows Server + IIS.

.DESCRIPTION
    Despliega un paquete ya compilado (backend + SPA) desde data\, configura el
    sitio y el app pool de IIS, y verifica que el Hub arranque. No compila nada,
    no instala PostgreSQL y no emite certificados.

    Solo HTTP. Si se requiere HTTPS, el binding se añade a mano en IIS Manager
    después de instalar.

.PARAMETER ConfigFile
    Ruta al archivo de configuración. Si se omite, se busca 'hub-install.psd1'
    junto a este script.

.PARAMETER Mode
    Install | Update | Repair | Auto (por defecto). Auto detecta el estado
    del servidor y elige.

.PARAMETER NonInteractive
    No pregunta nada. Requiere que toda la configuración obligatoria venga del
    archivo o de parámetros; si falta algo, aborta listándolo todo de una vez.

.PARAMETER DryRun
    Simula el recorrido completo sin escribir nada. Valida prerrequisitos,
    verifica checksums y prueba la conexión a PostgreSQL de verdad.

.EXAMPLE
    .\install.ps1 -DryRun
    Pase de validación recomendado antes de tocar producción.

.EXAMPLE
    .\install.ps1 -NonInteractive
    Instalación desatendida tomando hub-install.psd1.
#>
[CmdletBinding()]
param(
    [string]$ConfigFile,

    [ValidateSet('Auto', 'Install', 'Update', 'Repair')]
    [string]$Mode = 'Auto',

    [switch]$NonInteractive,
    [switch]$DryRun,
    [switch]$AllowHostingBundleDownload,

    # ── Sobreescrituras puntuales (ganan sobre el archivo) ───────────────────
    [string]$PackagePath,
    [string]$InstallPath,
    [string]$SiteName,
    [string]$AppPoolName,
    [string]$HostHeader,
    [int]$Port,
    [string]$DbHost,
    [int]$DbPort,
    [string]$DbName,
    [string]$DbUser,
    [System.Management.Automation.PSCredential]$DbCredential
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RootPath = $PSScriptRoot
$BinPath  = Join-Path $RootPath 'bin'
$DataPath = Join-Path $RootPath 'data'
$LogDir   = Join-Path $RootPath 'log'

Import-Module (Join-Path $BinPath 'EdgeGuard.Setup.psm1') -Force

# ─────────────────────────────────────────────────────────────────────────────
# Esquema de configuración
#
# Fuente única de verdad: define el default, el tipo, si es obligatorio y cómo
# se valida. La validación se deriva de aquí, de modo que añadir una clave no
# exige tocar tres sitios.
# ─────────────────────────────────────────────────────────────────────────────
$Schema = [ordered]@{
    PackagePath                = @{ Type = 'string'; Default = $null;    Prompt = 'Ruta del paquete';        Required = $false }
    InstallPath                = @{ Type = 'string'; Default = 'C:\inetpub\EdgeGuard\Hub'; Prompt = 'Directorio de instalación'; Required = $true }
    SiteName                   = @{ Type = 'string'; Default = 'EdgeGuard.Hub';  Prompt = 'Nombre del sitio IIS'; Required = $true }
    AppPoolName                = @{ Type = 'string'; Default = 'EdgeGuardHub';   Prompt = 'Nombre del app pool';  Required = $true }
    HostHeader                 = @{ Type = 'string'; Default = 'hub.local';      Prompt = 'Host header';          Required = $true }
    Port                       = @{ Type = 'int';    Default = 80;               Prompt = 'Puerto HTTP';          Required = $true; Validate = { param($v) $v -ge 1 -and $v -le 65535 }; ValidateMessage = 'debe estar entre 1 y 65535' }
    AppPoolIdentity            = @{ Type = 'string'; Default = 'LocalSystem';    Required = $true; Validate = { param($v) $v -in @('LocalSystem','ApplicationPoolIdentity','NetworkService','LocalService') }; ValidateMessage = "debe ser LocalSystem, ApplicationPoolIdentity, NetworkService o LocalService" }

    DbHost                     = @{ Type = 'string'; Default = 'localhost';      Prompt = 'PostgreSQL — host';    Required = $true }
    DbPort                     = @{ Type = 'int';    Default = 5432;             Prompt = 'PostgreSQL — puerto';  Required = $true; Validate = { param($v) $v -ge 1 -and $v -le 65535 }; ValidateMessage = 'debe estar entre 1 y 65535' }
    DbName                     = @{ Type = 'string'; Default = 'edgeguard_hub';  Prompt = 'PostgreSQL — base';    Required = $true }
    DbUser                     = @{ Type = 'string'; Default = 'edgeguard';      Prompt = 'PostgreSQL — usuario'; Required = $true }
    DbPassword                 = @{ Type = 'string'; Default = $null;            Prompt = 'PostgreSQL — contraseña'; Required = $true; Secret = $true }

    AdminUsername              = @{ Type = 'string'; Default = 'admin';          Prompt = 'Administrador — usuario'; Required = $false }
    AdminPassword              = @{ Type = 'string'; Default = $null;            Prompt = 'Administrador — contraseña'; Required = $false; Secret = $true }

    Hl7Enabled                 = @{ Type = 'bool';   Default = $true;            Prompt = 'Habilitar listener MLLP'; Required = $true }
    Hl7Port                    = @{ Type = 'int';    Default = 8001;             Prompt = 'HL7 — puerto MLLP';    Required = $true; Validate = { param($v) $v -ge 1 -and $v -le 65535 }; ValidateMessage = 'debe estar entre 1 y 65535' }
    Hl7RemoteAddress           = @{ Type = 'string'; Default = 'Any';            Prompt = 'HL7 — origen permitido'; Required = $true }

    DataProtectionKeyPath      = @{ Type = 'string'; Default = 'C:\inetpub\edgeguard\dp-keys'; Required = $true }
    InstanceId                 = @{ Type = 'string'; Default = 'HUB-001';        Required = $true }
    RedactionMode              = @{ Type = 'string'; Default = 'Strict';         Required = $true; Validate = { param($v) $v -in @('Strict','Relaxed') }; ValidateMessage = "debe ser 'Strict' o 'Relaxed'" }
    CorsAllowedOrigins         = @{ Type = 'array';  Default = @();              Required = $false }
    NodeAuthEnforce            = @{ Type = 'bool';   Default = $false;           Required = $true }
    Hl7ValidateBeforeAck       = @{ Type = 'bool';   Default = $false;           Required = $true }
    AllowHostingBundleDownload = @{ Type = 'bool';   Default = $false;           Required = $true }
}

# ─────────────────────────────────────────────────────────────────────────────
# Definición de pasos
#
# Modes indica en qué modos corre cada paso. Repair reaplica configuración sin
# arriesgar binarios ni base: por eso omite 02, 04 y 05.
# ─────────────────────────────────────────────────────────────────────────────
$StepDefinitions = @(
    @{ Id = '01'; File = '01-Test-Prerequisites.ps1';   Function = 'Step-TestPrerequisites';   Description = 'Prerrequisitos y validación del paquete'; Modes = @('Install','Update','Repair') }
    @{ Id = '02'; File = '02-Install-HostingBundle.ps1';Function = 'Step-InstallHostingBundle';Description = 'ASP.NET Core Hosting Bundle';             Modes = @('Install','Update') }
    @{ Id = '03'; File = '03-Enable-IisFeatures.ps1';   Function = 'Step-EnableIisFeatures';   Description = 'Características de IIS';                  Modes = @('Install','Update','Repair') }
    @{ Id = '04'; File = '04-Test-Database.ps1';        Function = 'Step-TestDatabase';        Description = 'Conectividad y permisos de PostgreSQL';   Modes = @('Install','Update') }
    @{ Id = '05'; File = '05-Deploy-Package.ps1';       Function = 'Step-DeployPackage';       Description = 'Despliegue del paquete';                  Modes = @('Install','Update') }
    @{ Id = '06'; File = '06-New-IisSite.ps1';          Function = 'Step-NewIisSite';          Description = 'App pool y sitio IIS';                    Modes = @('Install','Update','Repair') }
    @{ Id = '07'; File = '07-Set-AppPoolConfig.ps1';    Function = 'Step-SetAppPoolConfig';    Description = 'Variables de entorno del app pool';       Modes = @('Install','Update','Repair') }
    @{ Id = '08'; File = '08-New-Directories.ps1';      Function = 'Step-NewDirectories';      Description = 'Directorios de datos';                    Modes = @('Install','Update','Repair') }
    @{ Id = '09'; File = '09-New-FirewallRule.ps1';     Function = 'Step-NewFirewallRule';     Description = 'Regla de firewall HL7 MLLP';              Modes = @('Install','Update','Repair') }
    @{ Id = '10'; File = '10-Test-Installation.ps1';    Function = 'Step-TestInstallation';    Description = 'Verificación post-instalación';           Modes = @('Install','Update','Repair') }
)

# ─────────────────────────────────────────────────────────────────────────────
# Carga de configuración
# ─────────────────────────────────────────────────────────────────────────────

function Resolve-ConfigFile {
    param([string]$Explicit)

    if ($Explicit) {
        if (-not (Test-Path -LiteralPath $Explicit)) {
            throw "El archivo de configuración '$Explicit' no existe."
        }
        return (Resolve-Path -LiteralPath $Explicit).Path
    }

    $default = Join-Path $RootPath 'hub-install.psd1'
    if (Test-Path -LiteralPath $default) { return $default }
    return $null
}

function Import-SetupConfig {
    param([string]$Path)

    try   { return Import-PowerShellDataFile -LiteralPath $Path }
    catch { throw "No se pudo leer '$Path': $($_.Exception.Message)" }
}

<#
.SYNOPSIS
    Combina defaults, archivo y parámetros de línea de comandos.
.DESCRIPTION
    Precedencia: CLI > archivo > default. Se registra el origen de cada valor
    para mostrarlo en el resumen — saber de dónde salió un valor es la mitad
    del trabajo cuando una instalación no queda como se esperaba.
#>
function Merge-SetupConfig {
    param(
        [hashtable]$FromFile,
        [hashtable]$FromCli
    )

    $config = @{}
    $origin = @{}

    foreach ($key in $Schema.Keys) {
        $config[$key] = $Schema[$key].Default
        $origin[$key] = 'default'
    }

    if ($FromFile) {
        foreach ($key in $FromFile.Keys) {
            if ($Schema.Contains($key)) {
                $config[$key] = $FromFile[$key]
                $origin[$key] = 'archivo'
            }
        }
    }

    foreach ($key in $FromCli.Keys) {
        if ($Schema.Contains($key)) {
            $config[$key] = $FromCli[$key]
            $origin[$key] = 'parámetro'
        }
    }

    return @{ Config = $config; Origin = $origin }
}

<#
.SYNOPSIS
    Valida la configuración completa, acumulando TODOS los errores.
.DESCRIPTION
    Acumular en vez de abortar en el primero es deliberado: en modo no
    interactivo, fallar de uno en uno obliga a tantas ejecuciones como errores
    haya, y cada una contra un servidor de producción.
#>
function Test-SetupConfig {
    param(
        [hashtable]$Config,
        [hashtable]$FromFile
    )

    $errors = New-Object System.Collections.Generic.List[string]

    # Claves desconocidas: un 'DbHostt' mal escrito caería silenciosamente al
    # default y dejaría al operador depurando una conexión a la base equivocada.
    if ($FromFile) {
        foreach ($key in $FromFile.Keys) {
            if (-not $Schema.Contains($key)) {
                $errors.Add("Clave desconocida en el archivo de configuración: '$key'")
            }
        }
    }

    foreach ($key in $Schema.Keys) {
        $spec  = $Schema[$key]
        $value = $Config[$key]

        $isEmpty = ($null -eq $value) -or
                   (($spec.Type -eq 'string') -and [string]::IsNullOrWhiteSpace([string]$value))

        if ($spec.Required -and $isEmpty) {
            $errors.Add("Falta un valor obligatorio: '$key'")
            continue
        }
        if ($isEmpty) { continue }

        # Sin 'continue' dentro del switch: en PowerShell su efecto sobre el
        # foreach que lo envuelve no es fiable. Se usa una bandera explícita.
        $typeOk = $true
        switch ($spec.Type) {
            'int'   { if ($value -isnot [int])   { $errors.Add("'$key' debe ser un número entero."); $typeOk = $false } }
            'bool'  { if ($value -isnot [bool])  { $errors.Add("'$key' debe ser `$true o `$false."); $typeOk = $false } }
            'array' { if ($value -isnot [array]) { $errors.Add("'$key' debe ser un arreglo.");       $typeOk = $false } }
        }
        if (-not $typeOk) { continue }

        if ($spec.Contains('Validate') -and -not (& $spec.Validate $value)) {
            $errors.Add("'$key' $($spec.ValidateMessage)")
        }
    }

    # ── Reglas cruzadas ──────────────────────────────────────────────────────

    if ($Config.DbPassword -eq 'CAMBIAR_ANTES_DE_INSTALAR') {
        $errors.Add("'DbPassword' sigue con el valor de ejemplo. Sustitúyelo por la contraseña real.")
    }

    # La política se replica de AdminUserSeed.ValidatePasswordPolicy. Duplicarla
    # aquí es deliberado: el Hub, ante una contraseña que no la cumple, se limita
    # a un LogWarning y NO crea la cuenta. El arranque parece correcto, /health
    # responde y el operador solo descubre el problema al intentar entrar al SPA.
    # Fallar aquí cuesta una corrección en el .psd1; fallar allá cuesta una
    # sesión de diagnóstico.
    $adminPassword = [string]$Config.AdminPassword
    if (-not [string]::IsNullOrWhiteSpace($adminPassword)) {
        $policy = @(
            @{ Ok = ($adminPassword.Length -ge 8);                                  Message = 'al menos 8 caracteres' }
            @{ Ok = ($adminPassword -cmatch '[A-Z]');                               Message = 'al menos una mayúscula' }
            @{ Ok = ($adminPassword -cmatch '[a-z]');                               Message = 'al menos una minúscula' }
            @{ Ok = ($adminPassword -match '\d');                                   Message = 'al menos un dígito' }
            # El @() no es cosmético: bajo StrictMode, .Count sobre un pipeline
            # que no devolvió nada lanza PropertyNotFoundStrict, y la validación
            # entera moriría justo con las contraseñas que debe rechazar.
            @{ Ok = (@($adminPassword.ToCharArray() | Where-Object { -not [char]::IsLetterOrDigit($_) }).Count -gt 0)
               Message = 'al menos un carácter especial' }
        )
        $unmet = @($policy | Where-Object { -not $_.Ok } | ForEach-Object { $_.Message })
        if ($unmet.Count -gt 0) {
            $errors.Add("'AdminPassword' no cumple la política del Hub: falta $($unmet -join ', ').")
        }
    }

    if ([string]::IsNullOrWhiteSpace([string]$Config.AdminUsername) -and
        -not [string]::IsNullOrWhiteSpace($adminPassword)) {
        $errors.Add("'AdminUsername' no puede estar vacío cuando se indica 'AdminPassword'.")
    }

    $dpPath = [string]$Config.DataProtectionKeyPath
    if (-not [string]::IsNullOrWhiteSpace($dpPath)) {
        if (-not [System.IO.Path]::IsPathRooted($dpPath)) {
            $errors.Add("'DataProtectionKeyPath' debe ser una ruta absoluta.")
        }
        else {
            $install = [string]$Config.InstallPath
            if (-not [string]::IsNullOrWhiteSpace($install)) {
                $dpFull      = [System.IO.Path]::GetFullPath($dpPath).TrimEnd('\') + '\'
                $installFull = [System.IO.Path]::GetFullPath($install).TrimEnd('\') + '\'
                if ($dpFull.StartsWith($installFull, [StringComparison]::OrdinalIgnoreCase)) {
                    $errors.Add(
                        "'DataProtectionKeyPath' no puede estar dentro de 'InstallPath'. " +
                        "Cada actualización reemplaza ese directorio, y perder el key ring vuelve " +
                        "indescifrables los SigningSecret de todos los nodos.")
                }
            }
        }
    }

    # Coma delante: sin ella PowerShell desenvuelve la lista al retornar, y con
    # cero o un elemento el llamador recibiría $null o un string suelto.
    return ,$errors
}

# ─────────────────────────────────────────────────────────────────────────────
# Interacción
# ─────────────────────────────────────────────────────────────────────────────

function Read-SetupValue {
    param(
        [string]$Key,
        [hashtable]$Spec,
        $CurrentValue
    )

    $label = if ($Spec.Contains('Prompt')) { $Spec.Prompt } else { $Key }

    if ($Spec.Contains('Secret') -and $Spec.Secret) {
        $secure = Read-Host -Prompt "  $label" -AsSecureString
        if ($secure.Length -eq 0) { return $CurrentValue }
        return (ConvertFrom-SetupSecureString $secure)
    }

    $shown  = if ($null -eq $CurrentValue) { '' } else { [string]$CurrentValue }
    $suffix = if ($shown) { " [$shown]" } else { '' }
    $answer = Read-Host -Prompt "  $label$suffix"

    if ([string]::IsNullOrWhiteSpace($answer)) { return $CurrentValue }

    switch ($Spec.Type) {
        'int'  {
            $parsed = 0
            if ([int]::TryParse($answer, [ref]$parsed)) { return $parsed }
            Write-Host "     Valor no numérico, se conserva '$shown'." -ForegroundColor Yellow
            return $CurrentValue
        }
        'bool' { return ($answer -match '^(s|si|sí|y|yes|true|1)$') }
        default { return $answer }
    }
}

function Invoke-SetupInterview {
    param([hashtable]$Config, [hashtable]$Origin)

    Write-Host ""
    Write-Host "  Configuración del Hub — Enter conserva el valor entre corchetes" -ForegroundColor Cyan
    Write-Host ""

    foreach ($key in $Schema.Keys) {
        $spec = $Schema[$key]
        if (-not $spec.Contains('Prompt')) { continue }

        $Config[$key] = Read-SetupValue -Key $key -Spec $spec -CurrentValue $Config[$key]
        $Origin[$key] = 'interactivo'
    }

    return $Config
}

function Show-SetupSummary {
    param([hashtable]$Config, [hashtable]$Origin, [string]$ResolvedMode, [string]$Package)

    Write-Host ""
    Write-Host "  ─────────────────────────────────────────────────────────────" -ForegroundColor Cyan
    Write-Host "   Resumen de la instalación" -ForegroundColor Cyan
    Write-Host "  ─────────────────────────────────────────────────────────────" -ForegroundColor Cyan
    Write-Host ""
    Write-Host ("   {0,-28} {1}" -f 'Modo', $ResolvedMode)
    Write-Host ("   {0,-28} {1}" -f 'Paquete', $(if ($Package) { $Package } else { '(sin resolver)' }))

    foreach ($key in $Schema.Keys) {
        $spec  = $Schema[$key]
        $value = $Config[$key]

        $display =
            if ($spec.Contains('Secret') -and $spec.Secret) { Format-SetupMasked ([string]$value) }
            elseif ($value -is [array]) { if ($value.Count -eq 0) { '(vacío)' } else { $value -join ', ' } }
            elseif ($null -eq $value)   { '(vacío)' }
            else { [string]$value }

        Write-Host ("   {0,-28} {1,-42} {2}" -f $key, $display, "($($Origin[$key]))")
    }

    if ($Config.Hl7Enabled -and $Config.Hl7RemoteAddress -eq 'Any') {
        Write-Host ""
        Write-Host "   AVISO  El listener MLLP quedará accesible desde cualquier origen." -ForegroundColor Yellow
        Write-Host "          Es TCP crudo, sin TLS ni autenticación, y transporta PHI." -ForegroundColor Yellow
    }

    Write-Host ""
}

# ─────────────────────────────────────────────────────────────────────────────
# Principal
# ─────────────────────────────────────────────────────────────────────────────

$logFile = Start-SetupLog -Directory $LogDir -DryRun:$DryRun
Write-SetupLog "Registro en $logFile"

# Declaradas fuera del try: el manejador de errores las consulta para decidir si
# hay que revertir, y bajo StrictMode referenciar una variable sin asignar lanza
# excepción — el propio manejador fallaría justo cuando más se le necesita.
$config = $null
$state  = $null

try {
    # ── Configuración ────────────────────────────────────────────────────────
    $configPath = Resolve-ConfigFile -Explicit $ConfigFile
    $fromFile   = $null

    if ($configPath) {
        Write-SetupLog "Archivo de configuración: $configPath"
        $fromFile = Import-SetupConfig -Path $configPath
    }
    else {
        Write-SetupLog "Sin archivo de configuración; se usarán defaults, parámetros y preguntas." -Level Warn
    }

    $fromCli = @{}
    foreach ($name in $PSBoundParameters.Keys) {
        if ($Schema.Contains($name)) { $fromCli[$name] = $PSBoundParameters[$name] }
    }
    if ($PSBoundParameters.ContainsKey('AllowHostingBundleDownload')) {
        $fromCli['AllowHostingBundleDownload'] = [bool]$AllowHostingBundleDownload
    }

    $merged = Merge-SetupConfig -FromFile $fromFile -FromCli $fromCli
    $config = $merged.Config
    $origin = $merged.Origin

    # ── Contraseña: precedencia explícita ────────────────────────────────────
    if ($DbCredential) {
        $config.DbPassword = $DbCredential.GetNetworkCredential().Password
        $origin.DbPassword = 'parámetro'
    }
    elseif ($env:EDGEGUARD_SETUP_DBPASSWORD) {
        $config.DbPassword = $env:EDGEGUARD_SETUP_DBPASSWORD
        $origin.DbPassword = 'variable de entorno'
    }

    # ── Entrevista ───────────────────────────────────────────────────────────
    # Con archivo de configuración no se pregunta: solo se valida y se confirma.
    if (-not $NonInteractive -and -not $configPath) {
        $config = Invoke-SetupInterview -Config $config -Origin $origin
    }
    elseif (-not $NonInteractive -and [string]::IsNullOrWhiteSpace([string]$config.DbPassword)) {
        $secure = Read-Host -Prompt '  PostgreSQL — contraseña' -AsSecureString
        $config.DbPassword = ConvertFrom-SetupSecureString $secure
        $origin.DbPassword = 'interactivo'
    }

    Register-SetupSecret ([string]$config.DbPassword)
    if (-not [string]::IsNullOrWhiteSpace([string]$config.AdminPassword)) {
        Register-SetupSecret ([string]$config.AdminPassword)
    }

    # ── Validación ───────────────────────────────────────────────────────────
    $errors = Test-SetupConfig -Config $config -FromFile $fromFile
    if ($errors.Count -gt 0) {
        Write-SetupLog "La configuración tiene $($errors.Count) problema(s):" -Level Error
        foreach ($e in $errors) { Write-SetupLog $e -Level Detail }
        throw "Configuración inválida. Corrige lo anterior y vuelve a ejecutar."
    }
    Write-SetupLog "Configuración validada" -Level Ok

    # ── Estado compartido entre pasos ────────────────────────────────────────
    $state = @{
        RootPath     = $RootPath
        BinPath      = $BinPath
        DataPath     = $DataPath
        LogDir       = $LogDir
        ResolvedMode = $Mode
        Package      = $null
        Backup       = $null
    }

    # La resolución real del paquete y del modo vive en el paso 01 (Fase 3).
    if ($config.PackagePath) { $state.Package = $config.PackagePath }

    # ── Resumen y confirmación ───────────────────────────────────────────────
    Show-SetupSummary -Config $config -Origin $origin -ResolvedMode $state.ResolvedMode -Package $state.Package

    if (-not $NonInteractive) {
        $answer = Read-Host "  ¿Continuar? (s/N)"
        if ($answer -notmatch '^(s|si|sí|y|yes)$') {
            Write-SetupLog "Cancelado por el operador." -Level Warn
            exit 2
        }
    }

    if ($DryRun) {
        Write-SetupLog "DRY-RUN activo: ningún paso escribirá cambios." -Level Warn
    }

    # ── Ejecución ────────────────────────────────────────────────────────────
    foreach ($def in $StepDefinitions) {
        # El modo se relee en cada iteración a propósito: el paso 01 resuelve
        # 'Auto' leyendo installed.json, y los pasos siguientes deben respetar
        # esa decisión. Hasta entonces se asume Install, que es el superconjunto.
        $modeForSteps = if ($state.ResolvedMode -eq 'Auto') { 'Install' } else { $state.ResolvedMode }

        if ($def.Modes -notcontains $modeForSteps) {
            Write-SetupLog "$($def.Id) omitido en modo $modeForSteps" -Level Detail
            continue
        }

        . (Join-Path $BinPath $def.File)

        Invoke-SetupStep -Id $def.Id -Description $def.Description -Body {
            & $def.Function -Config $config -State $state
        } | Out-Null
    }

    Write-SetupLog ""
    Write-SetupLog "Instalación completada. Registro: $logFile" -Level Ok
    exit 0
}
catch {
    Write-SetupLog ""
    Write-SetupLog "INSTALACIÓN ABORTADA: $($_.Exception.Message)" -Level Error

    # ── Reversión ────────────────────────────────────────────────────────────
    # Solo hay algo que revertir si el paso 05 llegó a respaldar. Antes de eso
    # nada se tocó, y después el respaldo es una copia íntegra de lo que había.
    if ($state -and $state.ContainsKey('Backup') -and $state.Backup -and -not $DryRun) {
        Write-SetupLog "Revirtiendo al estado anterior desde $($state.Backup)" -Level Warn
        try {
            Restore-SetupBackup -BackupPath $state.Backup -InstallPath $config.InstallPath

            # El sitio anterior vuelve a quedar en pie; si no arranca, el
            # operador lo verá en el log de IIS y no en un directorio a medias.
            if (Get-Module -ListAvailable -Name WebAdministration) {
                Import-Module WebAdministration -ErrorAction SilentlyContinue
                if (Test-Path "IIS:\AppPools\$($config.AppPoolName)") {
                    Start-WebAppPool -Name $config.AppPoolName -ErrorAction SilentlyContinue
                }
            }

            Write-SetupLog "Reversión completada. La instalación anterior quedó restaurada." -Level Ok
            Write-SetupLog "El respaldo se conserva en $($state.Backup)" -Level Detail
        }
        catch {
            Write-SetupLog "LA REVERSIÓN FALLÓ: $($_.Exception.Message)" -Level Error
            Write-SetupLog "El respaldo íntegro sigue en $($state.Backup) — restáuralo a mano." -Level Error
        }
    }
    elseif ($state -and $state.ContainsKey('Backup') -and $state.Backup) {
        Write-SetupLog "DRY-RUN: no hay nada que revertir." -Level Detail
    }
    else {
        Write-SetupLog "No se modificó la instalación: no hay nada que revertir." -Level Detail
    }

    Write-SetupLog "Registro completo: $logFile" -Level Detail
    exit 1
}
