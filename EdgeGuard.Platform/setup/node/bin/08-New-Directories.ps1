#Requires -Version 5.1
<#
.SYNOPSIS
    Paso 08 — Directorios de datos

.DESCRIPTION
    Crea los tres directorios que el nodo escribe en tiempo de ejecución y, si el
    servicio no corre como LocalSystem, les concede permiso de escritura.

    Con LocalSystem no hay trabajo de ACL: esa identidad ya tiene acceso total al
    sistema de archivos local. Lo que sigue importando es que los directorios
    EXISTAN antes del primer arranque, y sobre todo que la base y los estudios
    estén separados.
#>

<#
.SYNOPSIS
    Reafirma que la base no queda bajo la carpeta de estudios.
.DESCRIPTION
    Redundante con la validación del maestro, y a propósito: es la condición cuya
    violación no produce ningún error visible, sino borrados de estudios meses
    después. El recorrido que mide el espacio sumaría el archivo SQLite como
    carga DICOM purgable, y la limpieza de emergencia borraría estudios para
    aliviar una presión que en parte no puede aliviar, porque SQLite no devuelve
    páginas al sistema operativo sin un VACUUM.

    El propio nodo la comprueba al arrancar (StoragePathGuard) y en Producción se
    niega a iniciar. Aquí se comprueba antes de crear los directorios, para no
    dejar montado un layout que el nodo va a rechazar.
#>
function Assert-DatabaseOutsideStorage {
    [CmdletBinding()]
    param([string]$DatabasePath, [string]$StorageRootPath)

    $dbDir = [System.IO.Path]::GetDirectoryName([System.IO.Path]::GetFullPath($DatabasePath)).TrimEnd('\') + '\'
    $root  = [System.IO.Path]::GetFullPath($StorageRootPath).TrimEnd('\') + '\'

    if ($dbDir.StartsWith($root, [StringComparison]::OrdinalIgnoreCase)) {
        throw ("La base de datos ($DatabasePath) quedaría dentro de la carpeta de estudios " +
               "($StorageRootPath). El nodo se niega a arrancar así: la medición del espacio " +
               "contaría la base como carga DICOM y la limpieza de emergencia borraría estudios " +
               "para aliviar una presión que no puede aliviar.")
    }
}

function New-NodeDirectory {
    [CmdletBinding()]
    param([string]$Path, [string]$Proposito)

    if (Test-Path -LiteralPath $Path) {
        Write-SetupLog "ya existe: $Path" -Level Detail
        return
    }

    Invoke-SetupAction -Description "crear $Path ($Proposito)" -Action {
        New-Item -ItemType Directory -Path $Path -Force -ErrorAction Stop | Out-Null
    } | Out-Null
}

<#
.SYNOPSIS
    Concede modificación sobre un directorio a la cuenta del servicio.
.DESCRIPTION
    Solo para cuentas distintas de LocalSystem. Sin esto, el servicio arranca,
    falla al crear su base y entra en el ciclo de reinicio de la política de
    recuperación: un servicio "en marcha" que no recibe nada.
#>
function Grant-ServiceAccess {
    [CmdletBinding()]
    param([string]$Path, [string]$Account)

    Invoke-SetupAction -Description "conceder modificación sobre $Path a $Account" -Action {
        $acl = Get-Acl -LiteralPath $Path
        $regla = New-Object System.Security.AccessControl.FileSystemAccessRule(
            $Account,
            [System.Security.AccessControl.FileSystemRights]::Modify,
            @([System.Security.AccessControl.InheritanceFlags]::ContainerInherit,
              [System.Security.AccessControl.InheritanceFlags]::ObjectInherit),
            [System.Security.AccessControl.PropagationFlags]::None,
            [System.Security.AccessControl.AccessControlType]::Allow)

        $acl.AddAccessRule($regla)
        Set-Acl -LiteralPath $Path -AclObject $acl -ErrorAction Stop
    } | Out-Null
}

# ─────────────────────────────────────────────────────────────────────────────

function Step-NewDirectories {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][hashtable]$Config,
        [Parameter(Mandatory)][hashtable]$State
    )

    Assert-DatabaseOutsideStorage `
        -DatabasePath $Config.DatabasePath `
        -StorageRootPath $Config.StorageRootPath

    $directorios = [ordered]@{
        $Config.DataRootPath    = 'raíz de datos del nodo'
        ([System.IO.Path]::GetDirectoryName($Config.DatabasePath)) = 'base de datos del nodo'
        $Config.StorageRootPath = 'estudios DICOM en tránsito'
        $Config.LogPath         = 'registro rotativo del nodo'
    }

    foreach ($ruta in $directorios.Keys) {
        New-NodeDirectory -Path $ruta -Proposito $directorios[$ruta]
    }

    if (Test-SetupDryRun) { return }

    foreach ($ruta in $directorios.Keys) {
        if (-not (Test-Path -LiteralPath $ruta)) {
            throw "No se pudo crear el directorio $ruta."
        }
    }

    # ── Permisos ─────────────────────────────────────────────────────────────
    if ($Config.ServiceAccount -eq 'LocalSystem') {
        Write-SetupLog "cuenta LocalSystem: sin trabajo de ACL" -Level Detail
    }
    else {
        foreach ($ruta in $directorios.Keys) {
            Grant-ServiceAccess -Path $ruta -Account $Config.ServiceAccount
        }
    }

    Write-SetupLog "$($directorios.Count) directorios verificados" -Level Detail
}
