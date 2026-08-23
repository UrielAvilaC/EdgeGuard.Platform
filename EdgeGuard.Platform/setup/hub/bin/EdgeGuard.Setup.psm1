#Requires -Version 5.1
<#
.SYNOPSIS
    Infraestructura compartida del instalador del Hub EdgeGuard.

.DESCRIPTION
    Registro con redacción de secretos, ejecución uniforme de pasos y utilidades
    de validación. No contiene lógica de instalación: cada paso vive en su propio
    archivo bajo bin\.
#>

Set-StrictMode -Version Latest

# ── Estado del módulo ────────────────────────────────────────────────────────
$script:LogPath   = $null
$script:DryRun    = $false
$script:Secrets   = New-Object System.Collections.Generic.List[string]
$script:StepIndex = 0

# ─────────────────────────────────────────────────────────────────────────────
# Redacción
# ─────────────────────────────────────────────────────────────────────────────

<#
.SYNOPSIS
    Registra un valor que nunca debe aparecer en el log.
.DESCRIPTION
    La redacción es central a propósito: si cada paso tuviera que acordarse de
    enmascarar sus propios secretos, bastaría un olvido para filtrar una
    contraseña al transcript. Registrando el valor una vez, toda escritura
    posterior queda cubierta, venga del paso que venga.
#>
function Register-SetupSecret {
    [CmdletBinding()]
    param([Parameter(Mandatory)][AllowEmptyString()][string]$Value)

    if (-not [string]::IsNullOrWhiteSpace($Value) -and -not $script:Secrets.Contains($Value)) {
        $script:Secrets.Add($Value)
    }
}

function Protect-SetupSecret {
    [CmdletBinding()]
    param([Parameter(Mandatory)][AllowEmptyString()][string]$Text)

    $result = $Text
    foreach ($secret in $script:Secrets) {
        if ($secret) { $result = $result.Replace($secret, '[REDACTADO]') }
    }
    return $result
}

<#
.SYNOPSIS
    Enmascara un valor para mostrarlo en el resumen (no para el log).
#>
function Format-SetupMasked {
    [CmdletBinding()]
    param([AllowEmptyString()][AllowNull()][string]$Value)

    if ([string]::IsNullOrEmpty($Value)) { return '(vacío)' }
    return ('*' * [Math]::Min($Value.Length, 12))
}

# ─────────────────────────────────────────────────────────────────────────────
# Registro
# ─────────────────────────────────────────────────────────────────────────────

function Start-SetupLog {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Directory,
        [string]$Prefix = 'install',
        [switch]$DryRun
    )

    if (-not (Test-Path $Directory)) {
        New-Item -ItemType Directory -Path $Directory -Force | Out-Null
    }

    $script:DryRun  = [bool]$DryRun
    $script:LogPath = Join-Path $Directory ("{0}-{1}.log" -f $Prefix, (Get-Date -Format 'yyyyMMdd-HHmmss'))

    "# EdgeGuard Hub — instalador"                        | Out-File $script:LogPath -Encoding utf8
    "# Inicio      : $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" | Out-File $script:LogPath -Encoding utf8 -Append
    "# Equipo      : $env:COMPUTERNAME"                   | Out-File $script:LogPath -Encoding utf8 -Append
    "# Usuario     : $env:USERDOMAIN\$env:USERNAME"       | Out-File $script:LogPath -Encoding utf8 -Append
    "# PowerShell  : $($PSVersionTable.PSVersion)"        | Out-File $script:LogPath -Encoding utf8 -Append
    if ($script:DryRun) {
        "# MODO        : DRY-RUN — no se escribe ningún cambio" | Out-File $script:LogPath -Encoding utf8 -Append
    }
    "" | Out-File $script:LogPath -Encoding utf8 -Append

    return $script:LogPath
}

function Get-SetupLogPath { return $script:LogPath }

function Write-SetupLog {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, Position = 0)][AllowEmptyString()][string]$Message,
        [ValidateSet('Info', 'Ok', 'Warn', 'Error', 'Step', 'Detail')][string]$Level = 'Info'
    )

    $safe   = Protect-SetupSecret $Message
    $stamp  = Get-Date -Format 'HH:mm:ss'
    $prefix = if ($script:DryRun) { '[DRY-RUN] ' } else { '' }

    $tag = switch ($Level) {
        'Ok'     { 'OK  ' }
        'Warn'   { 'AVIS' }
        'Error'  { 'ERR ' }
        'Step'   { 'PASO' }
        'Detail' { '    ' }
        default  { 'INFO' }
    }

    if ($script:LogPath) {
        "$stamp [$tag] $prefix$safe" | Out-File $script:LogPath -Encoding utf8 -Append
    }

    $color = switch ($Level) {
        'Ok'     { 'Green' }
        'Warn'   { 'Yellow' }
        'Error'  { 'Red' }
        'Step'   { 'Cyan' }
        'Detail' { 'DarkGray' }
        default  { 'Gray' }
    }

    $console = switch ($Level) {
        'Step'   { "`n=== $prefix$safe" }
        'Detail' { "     $safe" }
        'Ok'     { "  OK  $prefix$safe" }
        'Warn'   { "  !   $prefix$safe" }
        'Error'  { "  X   $prefix$safe" }
        default  { "      $prefix$safe" }
    }

    Write-Host $console -ForegroundColor $color
}

# ─────────────────────────────────────────────────────────────────────────────
# Ejecución de pasos
# ─────────────────────────────────────────────────────────────────────────────

<#
.SYNOPSIS
    Ejecuta un paso bajo un contrato uniforme.
.DESCRIPTION
    Todo paso se comporta igual: se anuncia, corre, y o cumple su postcondición
    o aborta. Ningún paso degrada en silencio — esa es la diferencia entre un
    instalador que falla donde está el problema y uno que falla tres pasos
    después, cuando ya es caro diagnosticarlo.

    En modo DryRun el cuerpo NO se ejecuta; el paso reporta lo que haría.
#>
function Invoke-SetupStep {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Id,
        [Parameter(Mandatory)][string]$Description,
        [Parameter(Mandatory)][scriptblock]$Body,
        [scriptblock]$Postcondition,
        [string]$PostconditionMessage = 'La postcondición del paso no se cumplió.'
    )

    $script:StepIndex++
    Write-SetupLog "$Id — $Description" -Level Step

    try {
        & $Body

        if ($Postcondition -and -not $script:DryRun) {
            if (-not (& $Postcondition)) {
                throw $PostconditionMessage
            }
        }

        Write-SetupLog "$Id completado" -Level Ok
        return $true
    }
    catch {
        Write-SetupLog "$Id falló: $($_.Exception.Message)" -Level Error
        throw
    }
}

<#
.SYNOPSIS
    Envuelve una acción con efectos secundarios para que DryRun la omita.
#>
function Invoke-SetupAction {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Description,
        [Parameter(Mandatory)][scriptblock]$Action
    )

    if ($script:DryRun) {
        Write-SetupLog "haría: $Description" -Level Detail
        return $null
    }

    Write-SetupLog $Description -Level Detail
    return (& $Action)
}

function Test-SetupDryRun { return $script:DryRun }

# ─────────────────────────────────────────────────────────────────────────────
# Utilidades
# ─────────────────────────────────────────────────────────────────────────────

function Test-SetupAdministrator {
    $identity  = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

<#
.SYNOPSIS
    Convierte un SecureString a texto plano.
.DESCRIPTION
    Necesario para construir la cadena de conexión. Se libera el BSTR de
    inmediato para no dejar la contraseña en memoria no administrada más
    tiempo del imprescindible.
#>
function ConvertFrom-SetupSecureString {
    [CmdletBinding()]
    param([Parameter(Mandatory)][System.Security.SecureString]$SecureString)

    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecureString)
    try   { return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) }
}

<#
.SYNOPSIS
    Restaura una instalación desde su respaldo.
.DESCRIPTION
    Vive en el módulo, y no en línea dentro del catch del maestro, para que se
    pueda probar por separado: es el código que solo se ejecuta cuando algo ya
    salió mal, que es justo cuando no puede fallar a su vez.
#>
function Restore-SetupBackup {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$BackupPath,
        [Parameter(Mandatory)][string]$InstallPath
    )

    if (-not (Test-Path -LiteralPath $BackupPath)) {
        throw "El respaldo $BackupPath no existe; no hay nada que restaurar."
    }

    if (Test-Path -LiteralPath $InstallPath) {
        Remove-Item -LiteralPath $InstallPath -Recurse -Force -ErrorAction Stop
    }

    Copy-Item -LiteralPath $BackupPath -Destination $InstallPath -Recurse -Force -ErrorAction Stop

    if (-not (Test-Path -LiteralPath $InstallPath)) {
        throw "La restauración no dejó nada en $InstallPath."
    }
}

function Get-SetupFileHashSafe {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) { return $null }
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToUpperInvariant()
}

Export-ModuleMember -Function @(
    'Register-SetupSecret'
    'Protect-SetupSecret'
    'Format-SetupMasked'
    'Start-SetupLog'
    'Get-SetupLogPath'
    'Write-SetupLog'
    'Invoke-SetupStep'
    'Invoke-SetupAction'
    'Test-SetupDryRun'
    'Test-SetupAdministrator'
    'ConvertFrom-SetupSecureString'
    'Restore-SetupBackup'
    'Get-SetupFileHashSafe'
)
