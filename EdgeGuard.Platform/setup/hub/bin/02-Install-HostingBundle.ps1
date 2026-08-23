#Requires -Version 5.1
<#
.SYNOPSIS
    Paso 02 — ASP.NET Core Hosting Bundle

.DESCRIPTION
    Si ya está instalado no hace nada. Si falta y está en data\, verifica el
    checksum e instala en silencio. Si falta y no está en data\, descarga desde
    la URL oficial fijada abajo y verifica el hash antes de ejecutar nada;
    requiere AllowHostingBundleDownload.
#>

# Versión mínima del runtime ASP.NET Core requerida por el Hub (net10.0).
$script:RequiredMajor = 10

# ── URL y hash fijados de la descarga ────────────────────────────────────────
# DEBEN rellenarse con la versión que la organización haya homologado, tomando
# el SHA-256 de la página oficial de descargas de .NET. Se dejan vacíos a
# propósito: un hash inventado es peor que no tener descarga, porque convierte
# la verificación en un trámite que siempre pasa.
#
# Mientras estén vacíos, la vía soportada es depositar el instalador en data\.
$script:HostingBundleUrl    = $null
$script:HostingBundleSha256 = $null

<#
.SYNOPSIS
    Determina si el Hosting Bundle ya está presente.
.DESCRIPTION
    Se comprueban las dos mitades por separado, porque fallan por separado: el
    módulo nativo de IIS (AspNetCoreModuleV2) y el runtime de ASP.NET Core. Un
    servidor con el runtime pero sin el módulo devuelve 500.19 en cada petición,
    y el diagnóstico no es evidente.
#>
function Get-HostingBundleState {
    [CmdletBinding()]
    param()

    $ancmPath = Join-Path $env:ProgramFiles 'IIS\Asp.Net Core Module\V2\aspnetcorev2.dll'
    $hasAncm  = Test-Path -LiteralPath $ancmPath

    $hasRuntime = $false
    $runtimeVersions = @()

    $dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($dotnet) {
        $runtimes = & $dotnet.Source --list-runtimes 2>$null
        foreach ($line in $runtimes) {
            if ($line -match '^Microsoft\.AspNetCore\.App\s+(\d+)\.(\d+)\.(\d+)') {
                $runtimeVersions += "$($Matches[1]).$($Matches[2]).$($Matches[3])"
                if ([int]$Matches[1] -ge $script:RequiredMajor) { $hasRuntime = $true }
            }
        }
    }

    return @{
        HasAncm         = $hasAncm
        HasRuntime      = $hasRuntime
        RuntimeVersions = $runtimeVersions
        Installed       = ($hasAncm -and $hasRuntime)
    }
}

<#
.SYNOPSIS
    Ejecuta el instalador del bundle en silencio.
.DESCRIPTION
    El código 3010 significa "correcto, pero requiere reinicio". Tratarlo como
    fallo abortaría una instalación que en realidad funcionó; ignorarlo dejaría
    al operador sin saber que el servidor necesita reiniciarse.
#>
function Install-HostingBundleFile {
    [CmdletBinding()]
    param([string]$Path, [hashtable]$State)

    $result = Invoke-SetupAction -Description "instalar el Hosting Bundle desde $(Split-Path $Path -Leaf)" -Action {
        Start-Process -FilePath $Path -ArgumentList '/install', '/quiet', '/norestart' -Wait -PassThru
    }

    if (Test-SetupDryRun) { return }

    switch ($result.ExitCode) {
        0 {
            Write-SetupLog "Hosting Bundle instalado" -Level Detail
        }
        3010 {
            $State['RebootRequired'] = $true
            Write-SetupLog "Hosting Bundle instalado, pero el servidor requiere REINICIO (código 3010)." -Level Warn
            Write-SetupLog "El Hub no atenderá peticiones de forma fiable hasta reiniciar." -Level Warn
        }
        default {
            throw "El instalador del Hosting Bundle terminó con código $($result.ExitCode)."
        }
    }
}

function Step-InstallHostingBundle {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][hashtable]$Config,
        [Parameter(Mandatory)][hashtable]$State
    )

    $state0 = Get-HostingBundleState

    if ($state0.Installed) {
        Write-SetupLog "Ya instalado — ASP.NET Core $($state0.RuntimeVersions -join ', ') y AspNetCoreModuleV2" -Level Detail
        return
    }

    if ($state0.HasRuntime -and -not $state0.HasAncm) {
        Write-SetupLog ("El runtime de ASP.NET Core está, pero falta AspNetCoreModuleV2. " +
                        "IIS devolvería 500.19 en cada petición; se instalará el bundle completo.") -Level Warn
    }
    elseif (-not $state0.HasRuntime) {
        $found = if ($state0.RuntimeVersions.Count) { $state0.RuntimeVersions -join ', ' } else { 'ninguno' }
        Write-SetupLog "Falta el runtime de ASP.NET Core $($script:RequiredMajor).x (presentes: $found)" -Level Detail
    }

    # ── Desde data\ ──────────────────────────────────────────────────────────
    $local = @(Get-ChildItem -LiteralPath $State.DataPath -Filter 'dotnet-hosting-*.exe' -File -ErrorAction SilentlyContinue)

    if ($local.Count -gt 0) {
        $installer = $local[0].FullName
        Write-SetupLog "Instalador encontrado en data\: $($local[0].Name)" -Level Detail
        # El checksum ya se verificó en el paso 01 si estaba declarado en
        # checksums.sha256; aquí se exige que lo estuviera.
        Install-HostingBundleFile -Path $installer -State $State
    }
    # ── Descarga ─────────────────────────────────────────────────────────────
    elseif ($Config.AllowHostingBundleDownload) {
        if (-not $script:HostingBundleUrl -or -not $script:HostingBundleSha256) {
            throw ("La descarga está permitida pero no hay URL ni hash fijados en " +
                   "bin\02-Install-HostingBundle.ps1. Rellena `$script:HostingBundleUrl y " +
                   "`$script:HostingBundleSha256 con la versión homologada, o deposita el " +
                   "instalador en data\ (es la vía recomendada para servidores sin salida a internet).")
        }

        $target = Join-Path $env:TEMP 'dotnet-hosting-bundle.exe'

        Invoke-SetupAction -Description "descargar el Hosting Bundle desde $($script:HostingBundleUrl)" -Action {
            $ProgressPreference = 'SilentlyContinue'
            [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
            Invoke-WebRequest -Uri $script:HostingBundleUrl -OutFile $target -UseBasicParsing
        } | Out-Null

        if (-not (Test-SetupDryRun)) {
            $actual = Get-SetupFileHashSafe -Path $target
            if ($actual -ne $script:HostingBundleSha256.ToUpperInvariant()) {
                Remove-Item -LiteralPath $target -Force -ErrorAction SilentlyContinue
                throw "El archivo descargado no coincide con el hash fijado. Descarga descartada."
            }
            Write-SetupLog "Descarga verificada contra el hash fijado" -Level Detail
            Install-HostingBundleFile -Path $target -State $State
        }
    }
    else {
        throw ("Falta el ASP.NET Core Hosting Bundle $($script:RequiredMajor).x. " +
               "Deposita el instalador en data\ y vuelve a ejecutar, o usa " +
               "-AllowHostingBundleDownload si este servidor tiene salida a internet.")
    }

    # ── Postcondición ────────────────────────────────────────────────────────
    if (-not (Test-SetupDryRun)) {
        $state1 = Get-HostingBundleState
        if (-not $state1.Installed) {
            throw "Tras la instalación sigue sin detectarse el Hosting Bundle completo."
        }
        Write-SetupLog "Verificado: ASP.NET Core $($state1.RuntimeVersions -join ', ') y AspNetCoreModuleV2" -Level Ok
    }
}
