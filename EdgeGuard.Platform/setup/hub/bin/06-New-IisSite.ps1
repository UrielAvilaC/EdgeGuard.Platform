#Requires -Version 5.1
<#
.SYNOPSIS
    Paso 06 — App pool y sitio IIS

.DESCRIPTION
    Crea o reconfigura el app pool y el sitio. Idempotente: si ya existen, se
    ajustan sus propiedades en lugar de recrearlos, de modo que una reinstalación
    sobre una instalación sana no interrumpe el servicio más de lo necesario.

    Solo HTTP por diseño. Si se requiere HTTPS, el binding se añade después en
    IIS Manager; este paso no lo toca ni lo elimina.
#>

function Import-WebAdministrationModule {
    if (-not (Get-Module -ListAvailable -Name WebAdministration)) {
        throw ("El módulo WebAdministration no está disponible. Falta la consola de administración " +
               "de IIS (Web-Mgmt-Console); revisa el resultado del paso 03.")
    }
    Import-Module WebAdministration -ErrorAction Stop
}

<#
.SYNOPSIS
    Crea o reconfigura el app pool.
.DESCRIPTION
    managedRuntimeVersion vacío significa "No Managed Code": ASP.NET Core no se
    hospeda en el CLR de IIS, sino a través de AspNetCoreModuleV2.

    startMode AlwaysRunning e idleTimeout en cero no son afinación: el listener
    MLLP de HL7 vive DENTRO del proceso del Hub. Con la configuración por
    defecto, IIS apaga el proceso tras 20 minutos sin peticiones HTTP y el
    listener deja de aceptar mensajes del HIS sin que nadie se entere.
#>
function Set-HubAppPool {
    [CmdletBinding()]
    param([string]$Name, [string]$Identity)

    $path = "IIS:\AppPools\$Name"

    if (-not (Test-Path $path)) {
        Invoke-SetupAction -Description "crear el app pool $Name" -Action {
            New-WebAppPool -Name $Name -Force | Out-Null
        } | Out-Null
    }
    else {
        Write-SetupLog "el app pool $Name ya existe; se ajustan sus propiedades" -Level Detail
    }

    if (Test-SetupDryRun) { return }

    Set-ItemProperty $path -Name managedRuntimeVersion     -Value ''
    Set-ItemProperty $path -Name startMode                 -Value 'AlwaysRunning'
    Set-ItemProperty $path -Name processModel.idleTimeout  -Value ([TimeSpan]::Zero)
    Set-ItemProperty $path -Name recycling.periodicRestart.time -Value ([TimeSpan]::Zero)

    # identityType: 0 LocalSystem, 1 LocalService, 2 NetworkService, 4 ApplicationPoolIdentity
    $identityType = switch ($Identity) {
        'LocalSystem'              { 0 }
        'LocalService'             { 1 }
        'NetworkService'           { 2 }
        'ApplicationPoolIdentity'  { 4 }
        default { throw "Identidad de app pool no soportada: $Identity" }
    }
    Set-ItemProperty $path -Name processModel.identityType -Value $identityType

    Write-SetupLog "App pool: No Managed Code, AlwaysRunning, sin reciclaje por inactividad, identidad $Identity" -Level Detail
}

<#
.SYNOPSIS
    Crea o reconfigura el sitio y su binding HTTP.
.DESCRIPTION
    El binding es *:Puerto sin host header: IIS atiende por cualquier dirección
    IP del servidor y por cualquier nombre con el que se le llame. No hay que
    registrar ningún nombre en DNS ni en el archivo hosts de cada equipo, y el
    acceso por IP —lo primero que prueba el operador— funciona desde el minuto
    uno.

    A cambio, este sitio se queda con todo el puerto 80 del servidor: si más
    adelante conviven otros sitios en la misma máquina, habrá que darle un
    puerto propio o volver a introducir host headers a mano.
#>
function Set-HubSite {
    [CmdletBinding()]
    param([string]$Name, [string]$PhysicalPath, [string]$AppPoolName, [int]$Port)

    $path = "IIS:\Sites\$Name"

    if (-not (Test-Path $path)) {
        Invoke-SetupAction -Description "crear el sitio $Name en *:$Port" -Action {
            New-WebSite -Name $Name -PhysicalPath $PhysicalPath -ApplicationPool $AppPoolName `
                        -Port $Port -Force | Out-Null
        } | Out-Null
    }
    else {
        Write-SetupLog "el sitio $Name ya existe; se ajustan sus propiedades" -Level Detail

        if (-not (Test-SetupDryRun)) {
            Set-ItemProperty $path -Name physicalPath     -Value $PhysicalPath
            Set-ItemProperty $path -Name applicationPool  -Value $AppPoolName

            $existing = @(Get-WebBinding -Name $Name -Protocol http |
                          Where-Object { $_.bindingInformation -eq "*:${Port}:" })
            if ($existing.Count -eq 0) {
                Write-SetupLog "se añade el binding http *:${Port}:" -Level Detail
                New-WebBinding -Name $Name -Protocol http -Port $Port -HostHeader ''
            }
        }
    }

    if (Test-SetupDryRun) { return }

    # WebSockets a nivel de sitio. Habilitar la característica de Windows en el
    # paso 03 no basta: si el sitio la tiene deshabilitada, SignalR cae a long
    # polling o falla, y el resto del sitio sigue funcionando con normalidad.
    Set-WebConfigurationProperty -PSPath $path -Filter 'system.webServer/webSocket' `
                                 -Name 'enabled' -Value $true -ErrorAction SilentlyContinue

    # Precarga: sin esto el proceso no arranca hasta la primera petición HTTP, y
    # el listener MLLP no existe hasta entonces.
    Set-ItemProperty $path -Name applicationDefaults.preloadEnabled -Value $true -ErrorAction SilentlyContinue

    Write-SetupLog "Sitio $Name → $PhysicalPath, http://*:$Port (sin host header), WebSockets y precarga activos" -Level Detail
}

function Step-NewIisSite {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][hashtable]$Config,
        [Parameter(Mandatory)][hashtable]$State
    )

    Import-WebAdministrationModule

    Set-HubAppPool -Name $Config.AppPoolName -Identity $Config.AppPoolIdentity

    Set-HubSite -Name $Config.SiteName `
                -PhysicalPath $Config.InstallPath `
                -AppPoolName $Config.AppPoolName `
                -Port $Config.Port

    if (Test-SetupDryRun) { return }

    if (-not (Test-Path "IIS:\AppPools\$($Config.AppPoolName)")) {
        throw "El app pool $($Config.AppPoolName) no existe tras la configuración."
    }
    if (-not (Test-Path "IIS:\Sites\$($Config.SiteName)")) {
        throw "El sitio $($Config.SiteName) no existe tras la configuración."
    }

    Write-SetupLog ("Recordatorio: el sitio queda en HTTP. Para HTTPS, añade el binding y el " +
                    "certificado en IIS Manager; el instalador no los toca.") -Level Warn
}
