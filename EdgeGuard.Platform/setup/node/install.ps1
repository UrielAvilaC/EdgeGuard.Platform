#Requires -Version 5.1
<#
.SYNOPSIS
    Instalador del Nodo EdgeGuard como servicio de Windows.

.DESCRIPTION
    Despliega un paquete ya compilado desde data\, registra el servicio, escribe
    su configuración, abre los puertos y verifica que el nodo arranque y se
    registre con el Hub. No compila nada.

    El paquete es self-contained: no hace falta instalar .NET en el servidor.

.PARAMETER ConfigFile
    Ruta al archivo de configuración. Si se omite, se busca 'node-install.psd1'
    junto a este script.

.PARAMETER Mode
    Install | Update | Repair | Auto (por defecto). Auto detecta el estado del
    servidor y elige.

.PARAMETER NonInteractive
    No pregunta nada. Requiere que toda la configuración obligatoria venga del
    archivo o de parámetros; si falta algo, aborta listándolo todo de una vez.

.PARAMETER DryRun
    Simula el recorrido completo sin escribir nada. Valida prerrequisitos,
    verifica checksums y prueba la conectividad con el Hub de verdad.

.PARAMETER SkipHubCheck
    Continúa aunque el Hub no responda. Para instalar antes de que el Hub exista
    o antes de que el enlace esté tendido. Por defecto, no responder aborta: el
    caso común no es una instalación adelantada, es una URL mal escrita.

.EXAMPLE
    .\install.ps1 -DryRun
    Pase de validación recomendado antes de tocar producción.

.EXAMPLE
    .\install.ps1 -NonInteractive
    Instalación desatendida tomando node-install.psd1.
#>
[CmdletBinding()]
param(
    [string]$ConfigFile,

    [ValidateSet('Auto', 'Install', 'Update', 'Repair')]
    [string]$Mode = 'Auto',

    [switch]$NonInteractive,
    [switch]$DryRun,
    [switch]$SkipHubCheck,

    # ── Sobreescrituras puntuales (ganan sobre el archivo) ───────────────────
    [string]$PackagePath,
    [string]$InstallPath,
    [string]$ServiceName,
    [string]$AeTitle,
    [int]$DicomPort,
    [int]$NodeApiPort,
    [string]$HubBaseUrl,
    [string]$NodeName,
    [string]$IpAddress
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
    PackagePath      = @{ Type = 'string'; Default = $null;                  Prompt = 'Ruta del paquete';          Required = $false }
    InstallPath      = @{ Type = 'string'; Default = 'C:\EdgeGuard\Node';    Prompt = 'Directorio de binarios';    Required = $true }

    ServiceName      = @{ Type = 'string'; Default = 'EdgeGuardNode';        Prompt = 'Nombre del servicio';       Required = $true }
    DisplayName      = @{ Type = 'string'; Default = 'EdgeGuard DICOM Node'; Required = $true }
    StartupType      = @{ Type = 'string'; Default = 'Automatic';            Required = $true; Validate = { param($v) $v -in @('Automatic','Manual','Disabled') }; ValidateMessage = 'debe ser Automatic, Manual o Disabled' }
    ServiceAccount   = @{ Type = 'string'; Default = 'LocalSystem';          Required = $true }

    DataRootPath     = @{ Type = 'string'; Default = 'C:\ProgramData\EdgeGuard\Node';                              Prompt = 'Carpeta de datos'; Required = $true }
    DatabasePath     = @{ Type = 'string'; Default = 'C:\ProgramData\EdgeGuard\Node\persistence\edge-node.db';     Required = $true }
    StorageRootPath  = @{ Type = 'string'; Default = 'C:\ProgramData\EdgeGuard\Node\data';                         Prompt = 'Carpeta de estudios'; Required = $true }
    LogPath          = @{ Type = 'string'; Default = 'C:\ProgramData\EdgeGuard\Node\logs';                         Required = $true }

    AeTitle          = @{ Type = 'string'; Default = 'EDGE_NODE'; Prompt = 'AE title del nodo'; Required = $true; Validate = { param($v) $v.Length -le 16 -and $v -notmatch '\s' }; ValidateMessage = 'debe tener 16 caracteres o menos y no llevar espacios (estándar DICOM)' }
    DicomPort        = @{ Type = 'int';    Default = 11112;       Prompt = 'Puerto DICOM';      Required = $true; Validate = { param($v) $v -ge 1 -and $v -le 65535 }; ValidateMessage = 'debe estar entre 1 y 65535' }
    NodeApiPort      = @{ Type = 'int';    Default = 5120;        Prompt = 'Puerto de la API';  Required = $true; Validate = { param($v) $v -ge 1 -and $v -le 65535 }; ValidateMessage = 'debe estar entre 1 y 65535' }

    HubBaseUrl       = @{ Type = 'string'; Default = $null; Prompt = 'URL del Hub';             Required = $true }
    NodeName         = @{ Type = 'string'; Default = $null; Prompt = 'Nombre del nodo';         Required = $true }
    FacilityName     = @{ Type = 'string'; Default = '';    Prompt = 'Institución / sede';      Required = $false }
    Location         = @{ Type = 'string'; Default = '';    Prompt = 'Ubicación';               Required = $false }
    IpAddress        = @{ Type = 'string'; Default = '';    Prompt = 'IP con la que se anuncia'; Required = $false }
    BootstrapToken   = @{ Type = 'string'; Default = $null; Required = $false; Secret = $true }

    TimeoutSeconds            = @{ Type = 'int';  Default = 30;   Required = $true }
    HeartbeatIntervalSeconds  = @{ Type = 'int';  Default = 60;   Required = $true }
    ConfigPullIntervalSeconds = @{ Type = 'int';  Default = 900;  Required = $true }
    RegisterOnStartup         = @{ Type = 'bool'; Default = $true; Required = $true }
    MaxReconnectAttempts      = @{ Type = 'int';  Default = 10;   Required = $true }
    ReconnectDelaySeconds     = @{ Type = 'int';  Default = 30;   Required = $true }

    InstanceId       = @{ Type = 'string'; Default = 'NODE-001'; Required = $true }
    RetainDays       = @{ Type = 'int';    Default = 90;         Required = $true; Validate = { param($v) $v -ge 1 }; ValidateMessage = 'debe ser 1 o más' }
    LogMaxFileSizeMb = @{ Type = 'int';    Default = 200;        Required = $true; Validate = { param($v) $v -ge 1 }; ValidateMessage = 'debe ser 1 o más' }

    DicomRemoteAddress   = @{ Type = 'string'; Default = 'Any'; Required = $true }
    NodeApiRemoteAddress = @{ Type = 'string'; Default = 'Any'; Required = $true }
}

# ─────────────────────────────────────────────────────────────────────────────
# Definición de pasos
#
# Se conservan los identificadores del instalador del Hub aunque falten el 02 y
# el 03 (Hosting Bundle y características de IIS, que aquí no aplican): un
# operador que conoce uno reconoce el otro, y renumerar rompería esa lectura.
#
# Repair reaplica configuración sin arriesgar binarios: por eso omite el 05.
# ─────────────────────────────────────────────────────────────────────────────
$StepDefinitions = @(
    @{ Id = '01'; File = '01-Test-Prerequisites.ps1';  Function = 'Step-TestPrerequisites';  Description = 'Prerrequisitos y validación del paquete'; Modes = @('Install','Update','Repair') }
    @{ Id = '04'; File = '04-Test-HubConnectivity.ps1';Function = 'Step-TestHubConnectivity';Description = 'Conectividad con el Hub';                 Modes = @('Install','Update','Repair') }
    @{ Id = '05'; File = '05-Deploy-Package.ps1';      Function = 'Step-DeployPackage';      Description = 'Despliegue del paquete';                  Modes = @('Install','Update') }
    @{ Id = '06'; File = '06-New-Service.ps1';         Function = 'Step-NewService';         Description = 'Registro del servicio de Windows';        Modes = @('Install','Update','Repair') }
    @{ Id = '07'; File = '07-Write-NodeConfig.ps1';    Function = 'Step-WriteNodeConfig';    Description = 'Configuración del nodo';                  Modes = @('Install','Update','Repair') }
    @{ Id = '08'; File = '08-New-Directories.ps1';     Function = 'Step-NewDirectories';     Description = 'Directorios de datos';                    Modes = @('Install','Update','Repair') }
    @{ Id = '09'; File = '09-New-FirewallRule.ps1';    Function = 'Step-NewFirewallRule';    Description = 'Reglas de firewall';                      Modes = @('Install','Update','Repair') }
    @{ Id = '10'; File = '10-Test-Installation.ps1';   Function = 'Step-TestInstallation';   Description = 'Verificación post-instalación';           Modes = @('Install','Update','Repair') }
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

    $default = Join-Path $RootPath 'node-install.psd1'
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
    param([hashtable]$FromFile, [hashtable]$FromCli)

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
    ¿Está 'Child' por debajo de 'Parent'?
.DESCRIPTION
    El separador final evita que "C:\data-backup" cuente como dentro de
    "C:\data". Mismo criterio que StoragePathGuard aplica en el nodo.
#>
function Test-PathInside {
    param([string]$Child, [string]$Parent)

    if ([string]::IsNullOrWhiteSpace($Child) -or [string]::IsNullOrWhiteSpace($Parent)) { return $false }

    try {
        $c = [System.IO.Path]::GetFullPath($Child).TrimEnd('\') + '\'
        $p = [System.IO.Path]::GetFullPath($Parent).TrimEnd('\') + '\'
    }
    catch { return $false }

    return $c.StartsWith($p, [StringComparison]::OrdinalIgnoreCase)
}

<#
.SYNOPSIS
    Valida la configuración completa, acumulando TODOS los errores.
.DESCRIPTION
    Acumular en vez de abortar en el primero es deliberado: en modo no
    interactivo, fallar de uno en uno obliga a tantas ejecuciones como errores
    haya, y un nodo se instala una vez por sede.
#>
function Test-SetupConfig {
    param([hashtable]$Config, [hashtable]$FromFile)

    $errors = New-Object System.Collections.Generic.List[string]

    # Claves desconocidas: un 'AeTitel' mal escrito caería silenciosamente al
    # default y dejaría al operador buscando por qué el nodo se anuncia con otro AE.
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
            'array' { if ($value -isnot [array] -and $value -isnot [string]) { $errors.Add("'$key' debe ser un arreglo o una cadena."); $typeOk = $false } }
        }
        if (-not $typeOk) { continue }

        if ($spec.Contains('Validate') -and -not (& $spec.Validate $value)) {
            $errors.Add("'$key' $($spec.ValidateMessage)")
        }
    }

    # ── Reglas cruzadas ──────────────────────────────────────────────────────

    if ($Config.HubBaseUrl -eq 'http://hub.miclinica.com') {
        $errors.Add("'HubBaseUrl' sigue con el valor de ejemplo. Sustitúyelo por la dirección real del Hub.")
    }
    if ($Config.NodeName -eq 'RIS_HOSPITAL_NORTE') {
        $errors.Add("'NodeName' sigue con el valor de ejemplo. Debe ser único entre los nodos del Hub.")
    }

    $hubUrl = [string]$Config.HubBaseUrl
    if (-not [string]::IsNullOrWhiteSpace($hubUrl)) {
        $uri = $null
        if (-not [Uri]::TryCreate($hubUrl, [UriKind]::Absolute, [ref]$uri) -or
            $uri.Scheme -notin @('http', 'https')) {
            $errors.Add("'HubBaseUrl' ('$hubUrl') no es una URL absoluta http/https. Ejemplo: http://10.10.10.32:80")
        }
    }

    if ($Config.DicomPort -is [int] -and $Config.NodeApiPort -is [int] -and
        $Config.DicomPort -eq $Config.NodeApiPort) {
        $errors.Add("'DicomPort' y 'NodeApiPort' no pueden ser el mismo puerto ($($Config.DicomPort)).")
    }

    # Rutas absolutas: una relativa se resolvería contra el directorio del
    # ejecutable del servicio, no contra donde el operador cree que están los datos.
    foreach ($key in @('InstallPath', 'DataRootPath', 'DatabasePath', 'StorageRootPath', 'LogPath')) {
        $ruta = [string]$Config[$key]
        if (-not [string]::IsNullOrWhiteSpace($ruta) -and -not [System.IO.Path]::IsPathRooted($ruta)) {
            $errors.Add("'$key' debe ser una ruta absoluta.")
        }
    }

    # La base NO puede vivir bajo la carpeta de estudios. Es la misma invariante que
    # StoragePathGuard comprueba al arrancar, comprobada aquí porque corregir el .psd1
    # cuesta un minuto y descubrirlo en el arranque cuesta una visita al servidor:
    # la medición del espacio contaría el archivo SQLite como carga DICOM y la limpieza
    # de emergencia borraría estudios para aliviar una presión que no puede aliviar.
    $dbDir = try { [System.IO.Path]::GetDirectoryName([System.IO.Path]::GetFullPath([string]$Config.DatabasePath)) } catch { $null }
    if ($dbDir -and (Test-PathInside -Child $dbDir -Parent ([string]$Config.StorageRootPath))) {
        $errors.Add(
            "'DatabasePath' está dentro de 'StorageRootPath'. Deben ser directorios separados: " +
            "de lo contrario la medición del espacio cuenta la base como carga DICOM y la limpieza " +
            "de emergencia borra estudios para aliviar una presión que no puede aliviar. " +
            "La convención es una carpeta 'persistence' hermana de 'data'.")
    }
    if ((Test-PathInside -Child ([string]$Config.StorageRootPath) -Parent $dbDir)) {
        $errors.Add("'StorageRootPath' está dentro de la carpeta de la base de datos. Deben ser directorios separados.")
    }

    # Nada que deba sobrevivir puede vivir bajo InstallPath: el paso 05 lo reemplaza entero.
    foreach ($key in @('DataRootPath', 'DatabasePath', 'StorageRootPath', 'LogPath')) {
        if (Test-PathInside -Child ([string]$Config[$key]) -Parent ([string]$Config.InstallPath)) {
            $errors.Add(
                "'$key' no puede estar dentro de 'InstallPath'. Cada actualización reemplaza ese " +
                "directorio completo y se llevaría por delante los datos del nodo.")
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
    param([string]$Key, [hashtable]$Spec, $CurrentValue)

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
    Write-Host "  Configuración del nodo — Enter conserva el valor entre corchetes" -ForegroundColor Cyan
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

        Write-Host ("   {0,-28} {1,-46} {2}" -f $key, $display, "($($Origin[$key]))")
    }

    if ($Config.DicomRemoteAddress -eq 'Any') {
        Write-Host ""
        Write-Host "   AVISO  El puerto DICOM quedará accesible desde cualquier origen." -ForegroundColor Yellow
        Write-Host "          Es TCP sin TLS ni autenticación fuerte, y transporta PHI." -ForegroundColor Yellow
        Write-Host "          Acótalo a las modalidades y al PACS con DicomRemoteAddress." -ForegroundColor Yellow
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

    $merged = Merge-SetupConfig -FromFile $fromFile -FromCli $fromCli
    $config = $merged.Config
    $origin = $merged.Origin

    # ── Entrevista ───────────────────────────────────────────────────────────
    # Con archivo de configuración no se pregunta: solo se valida y se confirma.
    if (-not $NonInteractive -and -not $configPath) {
        $config = Invoke-SetupInterview -Config $config -Origin $origin
    }

    if (-not [string]::IsNullOrWhiteSpace([string]$config.BootstrapToken)) {
        Register-SetupSecret ([string]$config.BootstrapToken)
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
        RootPath        = $RootPath
        BinPath         = $BinPath
        DataPath        = $DataPath
        LogDir          = $LogDir
        ResolvedMode    = $Mode
        Package         = $null
        Backup          = $null
        SkipHubCheck    = [bool]$SkipHubCheck
        HubInfo         = $null
    }

    # La resolución real del paquete y del modo vive en el paso 01.
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

            # El servicio anterior vuelve a quedar en pie; si no arranca, el
            # operador lo verá en el registro del nodo y no en un directorio a medias.
            $svc = Get-Service -Name $config.ServiceName -ErrorAction SilentlyContinue
            if ($svc -and $svc.Status -ne 'Running') {
                Start-Service -Name $config.ServiceName -ErrorAction SilentlyContinue
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
