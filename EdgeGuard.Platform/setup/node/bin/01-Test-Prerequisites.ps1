#Requires -Version 5.1
<#
.SYNOPSIS
    Paso 01 — Prerrequisitos y validación del paquete

.DESCRIPTION
    Verifica privilegios, sistema operativo, espacio en disco y puertos libres;
    resuelve el paquete dentro de data\, comprueba los checksums y valida el
    contenido del zip. Resuelve además el modo efectivo cuando -Mode es Auto.

    Corre completo también en -DryRun: es solo lectura, y detectar aquí un
    paquete corrupto o un puerto ocupado es justo donde sale barato.

    No verifica el runtime de .NET: el paquete del nodo es self-contained y se
    lleva el suyo dentro. Un nodo vive en el servidor de una sede sin
    administrador a mano, y esa decisión elimina la clase entera de fallos
    "la versión de .NET del servidor no es la que toca".
#>

# Espacio mínimo libre en el volumen de instalación. El paquete son ~53 MB, pero
# el despliegue mantiene el respaldo de la versión anterior mientras copia.
$script:MinFreeDiskGb = 3

<#
.SYNOPSIS
    Localiza el paquete: el indicado por configuración o el de mayor versión en data\.
#>
function Resolve-NodePackage {
    [CmdletBinding()]
    param([string]$Explicit, [string]$DataPath)

    if (-not [string]::IsNullOrWhiteSpace($Explicit)) {
        if (-not (Test-Path -LiteralPath $Explicit)) {
            throw "El paquete indicado no existe: $Explicit"
        }
        return (Resolve-Path -LiteralPath $Explicit).Path
    }

    if (-not (Test-Path -LiteralPath $DataPath)) {
        throw "No existe la carpeta data\. Deposita ahí el paquete .zip del nodo."
    }

    $candidates = @(Get-ChildItem -LiteralPath $DataPath -Filter '*.zip' -File -ErrorAction SilentlyContinue)
    if ($candidates.Count -eq 0) {
        throw ("No hay ningún .zip en $DataPath. Genéralo con " +
               "setup\common\Seal-Version.ps1 -Target Node, o consulta data\README.md.")
    }

    if ($candidates.Count -eq 1) { return $candidates[0].FullName }

    # Varios candidatos: se toma el de versión mayor y se deja constancia, para
    # que nadie descubra por accidente que instaló una versión distinta.
    $ranked = $candidates |
        ForEach-Object {
            $v = if ($_.Name -match '(\d+\.\d+(\.\d+)?(\.\d+)?)') { [version]$Matches[1] } else { [version]'0.0' }
            [pscustomobject]@{ File = $_; Version = $v }
        } |
        Sort-Object Version -Descending

    Write-SetupLog "Hay $($candidates.Count) paquetes en data\; se usará el de versión mayor:" -Level Warn
    foreach ($r in $ranked) { Write-SetupLog "$($r.File.Name)  ($($r.Version))" -Level Detail }

    return $ranked[0].File.FullName
}

<#
.SYNOPSIS
    Verifica los checksums declarados en data\checksums.sha256.
.DESCRIPTION
    Formato: "<HASH>  <nombre de archivo>", una línea por artefacto. Un hash que
    no cuadra aborta: un zip a medio copiar produce un despliegue que parece
    correcto y falla al arrancar, con un error que no señala la causa.
#>
function Test-NodeChecksums {
    [CmdletBinding()]
    param([string]$DataPath)

    $file = Join-Path $DataPath 'checksums.sha256'
    if (-not (Test-Path -LiteralPath $file)) {
        throw ("Falta data\checksums.sha256. Sin él no se puede verificar la integridad del " +
               "paquete. Lo genera setup\common\Seal-Version.ps1 al sellar la versión.")
    }

    $checked = 0
    foreach ($line in (Get-Content -LiteralPath $file)) {
        $trimmed = $line.Trim()
        if (-not $trimmed -or $trimmed.StartsWith('#')) { continue }

        if ($trimmed -notmatch '^([0-9A-Fa-f]{64})\s+(.+)$') {
            throw "Línea con formato inválido en checksums.sha256: '$trimmed'"
        }

        $expected = $Matches[1].ToUpperInvariant()
        $name     = $Matches[2].Trim()
        $target   = Join-Path $DataPath $name

        if (-not (Test-Path -LiteralPath $target)) {
            Write-SetupLog "Declarado en checksums pero ausente (se omite): $name" -Level Detail
            continue
        }

        $actual = (Get-SetupFileHashSafe -Path $target).ToUpperInvariant()
        if ($actual -ne $expected) {
            throw ("El hash de '$name' no coincide con el declarado. El archivo está corrupto o " +
                   "no es el que se selló. Vuelve a copiarlo desde el paquete original.")
        }

        Write-SetupLog "integridad verificada: $name" -Level Detail
        $checked++
    }

    if ($checked -eq 0) {
        throw "checksums.sha256 no declaró ningún archivo presente. No se verificó nada."
    }

    return $checked
}

<#
.SYNOPSIS
    Comprueba que el zip traiga lo que el despliegue va a necesitar.
#>
function Test-NodePackageContent {
    [CmdletBinding()]
    param([string]$ZipPath)

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [System.IO.Compression.ZipFile]::OpenRead($ZipPath)
    try {
        $nombres = @($zip.Entries | ForEach-Object { $_.FullName })
    }
    finally { $zip.Dispose() }

    $requeridos = @('Dicom.Edge.Node.exe', 'appsettings.Production.json')
    $faltantes  = @($requeridos | Where-Object { $nombres -notcontains $_ })

    if ($faltantes.Count -gt 0) {
        throw ("El paquete no contiene: $($faltantes -join ', '). No es un paquete del nodo, o " +
               "se comprimió desde el directorio equivocado (el .exe debe quedar en la raíz del zip).")
    }

    # El runtime dentro del zip es lo que hace que no haga falta instalar .NET.
    # Si falta, el servicio se registra bien y falla al arrancar con un error del
    # cargador que no menciona en ningún momento la palabra "runtime".
    if ($nombres -notcontains 'hostfxr.dll') {
        throw ("El paquete no trae el runtime (falta hostfxr.dll): se publicó framework-dependent. " +
               "El nodo se despliega self-contained — vuelve a sellar con Seal-Version.ps1.")
    }

    Write-SetupLog "paquete válido: $($nombres.Count) archivos" -Level Detail
}

<#
.SYNOPSIS
    ¿Hay algo escuchando en este puerto?
.DESCRIPTION
    Se comprueba antes de instalar porque el síntoma posterior es engañoso: el
    servicio arranca, falla al abrir el socket y se reinicia en bucle por la
    política de recuperación, dejando un servicio "en marcha" que no recibe nada.
#>
function Test-PortFree {
    [CmdletBinding()]
    param([int]$Port, [string]$Proposito, [string]$ServiceName)

    $enUso = @(Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue)
    if ($enUso.Count -eq 0) {
        Write-SetupLog "puerto $Port libre ($Proposito)" -Level Detail
        return
    }

    # Si el ocupante es el propio nodo de una instalación anterior, no es un
    # conflicto: el paso 05 lo detiene antes de desplegar.
    $propio = $false
    foreach ($conexion in $enUso) {
        $proceso = Get-Process -Id $conexion.OwningProcess -ErrorAction SilentlyContinue
        if ($proceso -and $proceso.Name -eq 'Dicom.Edge.Node') { $propio = $true; break }
    }

    if ($propio) {
        Write-SetupLog "puerto $Port en uso por el nodo instalado ($Proposito) — el paso 05 lo detiene" -Level Detail
        return
    }

    $quien = ($enUso | ForEach-Object {
        $p = Get-Process -Id $_.OwningProcess -ErrorAction SilentlyContinue
        if ($p) { "$($p.Name) (PID $($p.Id))" } else { "PID $($_.OwningProcess)" }
    }) -join ', '

    throw ("El puerto $Port ($Proposito) ya lo escucha: $quien. Libéralo o asigna otro puerto " +
           "en el .psd1 — si el nodo no puede abrirlo, arranca y se reinicia en bucle sin recibir nada.")
}

<#
.SYNOPSIS
    Resuelve -Mode Auto leyendo installed.json.
#>
function Resolve-InstallMode {
    [CmdletBinding()]
    param([string]$Declarado, [string]$InstallPath, [string]$ServiceName)

    if ($Declarado -ne 'Auto') { return $Declarado }

    $manifiesto = Join-Path $InstallPath 'installed.json'
    $servicio   = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

    if (Test-Path -LiteralPath $manifiesto) {
        Write-SetupLog "Auto → Update (hay installed.json en $InstallPath)" -Level Detail
        return 'Update'
    }

    if ($servicio) {
        # Servicio sin manifiesto: instalación anterior a este instalador, o un
        # despliegue a medias. Update reinstala los binarios y reescribe todo,
        # que es lo que hace falta en ambos casos.
        Write-SetupLog "Auto → Update (el servicio '$ServiceName' existe pero no hay installed.json)" -Level Warn
        return 'Update'
    }

    Write-SetupLog "Auto → Install (no hay instalación previa)" -Level Detail
    return 'Install'
}

# ─────────────────────────────────────────────────────────────────────────────

function Step-TestPrerequisites {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][hashtable]$Config,
        [Parameter(Mandatory)][hashtable]$State
    )

    # ── Privilegios ──────────────────────────────────────────────────────────
    if (-not (Test-SetupAdministrator)) {
        throw "Este instalador debe ejecutarse como Administrador: registra un servicio de Windows y abre puertos en el firewall."
    }
    Write-SetupLog "sesión elevada" -Level Detail

    # ── Sistema operativo ────────────────────────────────────────────────────
    $os = Get-CimInstance Win32_OperatingSystem
    Write-SetupLog "$($os.Caption) — $($os.Version)" -Level Detail

    # ── Espacio en disco ─────────────────────────────────────────────────────
    foreach ($ruta in @($Config.InstallPath, $Config.DataRootPath)) {
        $unidad = [System.IO.Path]::GetPathRoot([System.IO.Path]::GetFullPath($ruta))
        $drive  = Get-PSDrive -Name $unidad.TrimEnd(':\') -ErrorAction SilentlyContinue

        if (-not $drive) {
            throw "La unidad $unidad (de '$ruta') no existe o no es accesible."
        }

        $libreGb = [math]::Round($drive.Free / 1GB, 1)
        if ($libreGb -lt $script:MinFreeDiskGb) {
            throw "La unidad $unidad tiene $libreGb GB libres y hacen falta al menos $($script:MinFreeDiskGb) GB."
        }
        Write-SetupLog "unidad $unidad — $libreGb GB libres" -Level Detail
    }

    # ── Puertos ──────────────────────────────────────────────────────────────
    Test-PortFree -Port $Config.DicomPort   -Proposito 'DICOM SCP'    -ServiceName $Config.ServiceName
    Test-PortFree -Port $Config.NodeApiPort -Proposito 'API del nodo' -ServiceName $Config.ServiceName

    # ── Paquete ──────────────────────────────────────────────────────────────
    $paquete = Resolve-NodePackage -Explicit $State.Package -DataPath $State.DataPath
    $State.Package = $paquete
    Write-SetupLog "paquete: $(Split-Path -Leaf $paquete)" -Level Detail

    $verificados = Test-NodeChecksums -DataPath $State.DataPath
    Write-SetupLog "$verificados artefacto(s) con integridad verificada" -Level Detail

    Test-NodePackageContent -ZipPath $paquete

    # ── Modo ─────────────────────────────────────────────────────────────────
    $State.ResolvedMode = Resolve-InstallMode `
        -Declarado $State.ResolvedMode `
        -InstallPath $Config.InstallPath `
        -ServiceName $Config.ServiceName

    Write-SetupLog "modo efectivo: $($State.ResolvedMode)" -Level Detail
}
