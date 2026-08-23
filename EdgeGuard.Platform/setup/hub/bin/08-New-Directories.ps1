#Requires -Version 5.1
<#
.SYNOPSIS
    Paso 08 — Directorios de datos

.DESCRIPTION
    Crea los directorios que el Hub escribe en tiempo de ejecución.

    Con el app pool en LocalSystem no hay trabajo de ACL: esa identidad ya tiene
    acceso total al sistema de archivos local. Lo que sigue importando es que
    los directorios EXISTAN antes del primer arranque, y sobre todo dónde está
    el de Data Protection.
#>

<#
.SYNOPSIS
    Comprueba que el key ring quede fuera del directorio de despliegue.
.DESCRIPTION
    Redundante con la validación del maestro, y a propósito: es la condición
    cuya violación no se nota hasta la primera actualización, cuando el paso 05
    reemplaza el directorio y se lleva por delante el key ring. A partir de ahí
    los SigningSecret de todos los nodos quedan sin poder descifrarse y el Hub
    solo emite un LogError por nodo al intentar firmar.
#>
function Assert-KeyPathOutsideInstall {
    [CmdletBinding()]
    param([string]$KeyPath, [string]$InstallPath)

    $key     = [System.IO.Path]::GetFullPath($KeyPath).TrimEnd('\') + '\'
    $install = [System.IO.Path]::GetFullPath($InstallPath).TrimEnd('\') + '\'

    if ($key.StartsWith($install, [StringComparison]::OrdinalIgnoreCase)) {
        throw ("El directorio de Data Protection ($KeyPath) está dentro del directorio de " +
               "instalación. La próxima actualización lo borraría junto con los binarios y " +
               "los SigningSecret de todos los nodos dejarían de ser descifrables.")
    }
}

function New-HubDirectory {
    [CmdletBinding()]
    param([string]$Path, [string]$Purpose)

    if (Test-Path -LiteralPath $Path) {
        Write-SetupLog "ya existe: $Path" -Level Detail
        return
    }

    Invoke-SetupAction -Description "crear $Path ($Purpose)" -Action {
        New-Item -ItemType Directory -Path $Path -Force -ErrorAction Stop | Out-Null
    } | Out-Null
}

function Step-NewDirectories {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][hashtable]$Config,
        [Parameter(Mandatory)][hashtable]$State
    )

    Assert-KeyPathOutsideInstall -KeyPath $Config.DataProtectionKeyPath -InstallPath $Config.InstallPath

    $directories = [ordered]@{
        (Join-Path $Config.InstallPath 'logs')              = 'registro rotativo del Hub'
        (Join-Path $Config.InstallPath 'workspace')         = 'espacio de trabajo'
        (Join-Path $Config.InstallPath 'workspace\reports') = 'PDF de reportes referenciados por Study.ReportPdfPath'
        $Config.DataProtectionKeyPath                       = 'key ring de Data Protection'
    }

    foreach ($path in $directories.Keys) {
        New-HubDirectory -Path $path -Purpose $directories[$path]
    }

    if (Test-SetupDryRun) { return }

    foreach ($path in $directories.Keys) {
        if (-not (Test-Path -LiteralPath $path)) {
            throw "No se pudo crear el directorio $path."
        }
    }

    # El runtime crearía el directorio de llaves por su cuenta, pero lo hace con
    # la identidad del app pool y falla si esa cuenta no puede escribir en el
    # padre. Crearlo aquí, desde una sesión elevada, elimina ese modo de fallo.
    Write-SetupLog "$($directories.Count) directorios verificados" -Level Detail
}
