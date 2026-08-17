# Guía de instalación — `install-service.ps1`

Script de PowerShell que instala el **EdgeGuard DICOM Node** como servicio de Windows. Registra el ejecutable, crea las carpetas de datos y logs, configura la recuperación automática ante fallos y reinstala limpio si el servicio ya existía.

> Ubicación: `src/edge/Dicom.Edge.Node/install-service.ps1`

---

## Requisitos previos

| Requisito | Detalle |
|-----------|---------|
| **Permisos** | El script **debe ejecutarse como Administrador**. Si no, aborta con error. |
| **Paquete de binarios (.zip)** | Los binarios ya vienen compilados en un `.zip`. Solo hay que **extraerlos** a la carpeta deseada (ver [Extraer los binarios](#1-extraer-los-binarios)). No es necesario compilar ni instalar .NET (el paquete es *self-contained*). |
| **Política de ejecución** | PowerShell debe permitir correr scripts (ver [nota de ExecutionPolicy](#nota-executionpolicy)). |

---

## Parámetros

El script acepta estos parámetros (todos con valor por defecto):

| Parámetro | Valor por defecto | Descripción |
|-----------|-------------------|-------------|
| `-ServiceName` | `EdgeGuardNode` | Nombre interno del servicio (el que usas con `Start-Service`, `sc.exe`, etc.). |
| `-DisplayName` | `EdgeGuard DICOM Node` | Nombre visible en `services.msc`. |
| `-Description` | `EdgeGuard Platform - DICOM edge node service (C-STORE SCP, routing, worklist)` | Descripción del servicio. |
| `-ExePath` | `<carpeta del script>\Dicom.Edge.Node.exe` | Ruta al ejecutable. Por defecto lo busca **en la misma carpeta que el script** (`install-service.ps1`), es decir, la carpeta donde extrajiste el `.zip`. |
| `-StartupType` | `Automatic` | Tipo de arranque: `Automatic`, `Manual` o `Disabled`. |

---

## Qué hace el script (paso a paso)

1. **Verifica privilegios de administrador.** Si no los tiene, muestra error y sale con código `1`.
2. **Crea el directorio de datos** `C:\ProgramData\EdgeGuard\Node` y su subcarpeta `logs\` (si no existen).
3. **Detecta un servicio previo** con el mismo nombre. Si existe:
   - Lo detiene (`Stop-Service -Force`) si está corriendo.
   - Lo elimina (`sc.exe delete`).
4. **Instala el servicio** con `New-Service`, ejecutando el `.exe` con el argumento `--environment Production`.
5. **Configura la recuperación ante fallos**: reinicia el servicio tras el 1.º (5 s), 2.º (10 s) y siguientes fallos (30 s); el contador se resetea cada 24 h.
6. Muestra confirmación e indica cómo arrancarlo.

> **Nota:** el argumento `--environment Production` hace que el nodo cargue `appsettings.Production.json`, que apunta a `C:\ProgramData\EdgeGuard\Node` para base de datos (`data\edge-node.db`), logs y almacenamiento.

---

## Uso

### 1. Extraer los binarios

Los binarios ya vienen compilados dentro de un `.zip`. Extráelos a la carpeta donde quieras que resida el servicio (por ejemplo `C:\EdgeGuard\Node`):

```powershell
Expand-Archive -Path ".\EdgeGuardNode.zip" -DestinationPath "C:\EdgeGuard\Node" -Force
```

Dentro de la carpeta extraída, el script `install-service.ps1` y el ejecutable `Dicom.Edge.Node.exe` quedan **en la misma carpeta**. La ruta por defecto del script (`-ExePath`) apunta al `.exe` que está **junto a él**, así que no necesitas parámetros extra si conservas el contenido del `.zip` en una sola carpeta. El servicio se registra apuntando exactamente a ese `.exe`.

> Extrae con clic derecho → **Extraer todo…** también funciona; solo asegúrate de mantener la estructura de subcarpetas del paquete.

### 2. Ejecutar el instalador

Abre **PowerShell como Administrador**, ve a la carpeta donde extrajiste el `.zip` y ejecuta:

```powershell
cd "C:\EdgeGuard\Node"
.\install-service.ps1
```

### 3. Arrancar el servicio

```powershell
Start-Service -Name EdgeGuardNode
```

---

## Ejemplos

**Instalación estándar (valores por defecto):**

```powershell
.\install-service.ps1
```

**Ejecutable en otra ruta** (si extrajiste el `.zip` en otro sitio o cambiaste la estructura):

```powershell
.\install-service.ps1 -ExePath "D:\EdgeGuard\Node\Dicom.Edge.Node.exe"
```

**Nombre de servicio personalizado y arranque manual** (útil para instalar varios nodos en la misma máquina):

```powershell
.\install-service.ps1 -ServiceName "EdgeGuardNode-RIS" -DisplayName "EdgeGuard Node RIS" -StartupType Manual
```

---

## Verificación

```powershell
# Estado del servicio
Get-Service -Name EdgeGuardNode

# Ver detalles (ruta del binario, tipo de arranque)
sc.exe qc EdgeGuardNode

# Revisar logs del nodo
Get-Content "C:\ProgramData\EdgeGuard\Node\logs\node-*.log" -Tail 50
```

---

## Gestión del servicio

```powershell
# Iniciar / detener / reiniciar
Start-Service   -Name EdgeGuardNode
Stop-Service    -Name EdgeGuardNode
Restart-Service -Name EdgeGuardNode
```

### Desinstalar

El script no incluye un modo de desinstalación; hazlo manualmente:

```powershell
Stop-Service -Name EdgeGuardNode -Force
sc.exe delete EdgeGuardNode
```

> Los datos en `C:\ProgramData\EdgeGuard\Node` (base de datos, logs, estudios) **no se borran** al eliminar el servicio. Bórralos manualmente solo si estás seguro.

---

## Parametrización (`appsettings.Production.json`)

Toda la configuración del nodo vive en el archivo **`appsettings.Production.json`**, que se encuentra **junto al ejecutable** (en la carpeta que extrajiste del `.zip`). El servicio arranca con `--environment Production`, por lo que **este es el archivo que realmente se usa** en la instalación (no `appsettings.json` ni `appsettings.Development.json`).

> **Importante:** edita este archivo **antes de arrancar el servicio por primera vez** (o detén el servicio, edita y reinícialo). Los cambios solo se aplican al reiniciar: `Restart-Service -Name EdgeGuardNode`.

### Conexión con el Hub — sección `HubConnection`

Es la sección que casi siempre necesitas ajustar en cada instalación. Define **a qué Hub se conecta el nodo y cómo se identifica**.

```json
"HubConnection": {
  "Enabled": true,
  "HubBaseUrl": "https://localhost:7228",
  "NodeName": "RIS_SISTEMA",
  "IpAddress": "127.0.0.1",
  "Port": 11112,
  "Location": "RIS SISTEMA",
  "FacilityName": "RIS_SISTEMA",
  "Version": "1.0.0",
  "TimeoutSeconds": 30,
  "HeartbeatIntervalSeconds": 60,
  "ConfigPullIntervalSeconds": 900,
  "RegisterOnStartup": true,
  "MaxReconnectAttempts": 10,
  "ReconnectDelaySeconds": 30
}
```

| Campo | Qué configurar | Ejemplo |
|-------|----------------|---------|
| `Enabled` | Activa/desactiva la conexión con el Hub. Déjalo en `true` en producción. | `true` |
| `HubBaseUrl` | **URL del Hub** al que se registra y reporta el nodo. **Cámbialo** por la dirección real del Hub. | `https://hub.miclinica.com` |
| `NodeName` | **Nombre único del nodo** con el que se identifica en el Hub. | `RIS_HOSPITAL_NORTE` |
| `IpAddress` | IP con la que el nodo se anuncia (dónde escucha DICOM). | `192.168.1.50` |
| `Port` | **Puerto DICOM** donde el nodo recibe C-STORE (SCP). | `11112` |
| `Location` | Ubicación descriptiva del nodo (texto libre). | `Sala de RX - Piso 2` |
| `FacilityName` | **Nombre de la institución/sede**. | `Hospital Norte` |
| `Version` | Versión reportada del nodo (informativa). | `1.0.0` |
| `TimeoutSeconds` | Timeout de las llamadas HTTP al Hub. | `30` |
| `HeartbeatIntervalSeconds` | Cada cuántos segundos envía "latido" al Hub. | `60` |
| `ConfigPullIntervalSeconds` | Cada cuántos segundos consulta su configuración en el Hub. | `900` (15 min) |
| `RegisterOnStartup` | Si se registra automáticamente al arrancar. | `true` |
| `MaxReconnectAttempts` | Reintentos de reconexión ante caída del Hub. | `10` |
| `ReconnectDelaySeconds` | Espera entre reintentos de reconexión. | `30` |

> **Token de arranque (bootstrap):** si no defines `BootstrapToken`, el nodo lo solicita automáticamente al Hub mediante `POST /edge/token` (válido 5 minutos, de un solo uso). Solo configúralo manualmente si necesitas un token admin pre-emitido vía `POST /api/nodes/bootstrap-tokens`.

### Rutas de datos — `ConnectionStrings` y `NodeStorage`

Definen dónde guarda el nodo su base de datos y los estudios. Por defecto todo cuelga de `C:\ProgramData\EdgeGuard\Node` (carpeta que crea el instalador).

```json
"ConnectionStrings": {
  "NodeDatabase": "Data Source=C:/ProgramData/EdgeGuard/Node/data/edge-node.db"
},
"NodeStorage": {
  "RootPath": "C:/ProgramData/EdgeGuard/Node/data",
  "ArchivePath": "C:/ProgramData/EdgeGuard/Node/workspace"
}
```

| Campo | Descripción |
|-------|-------------|
| `ConnectionStrings:NodeDatabase` | Ruta del archivo SQLite del nodo. |
| `NodeStorage:RootPath` | Carpeta donde se almacenan los estudios recibidos. |
| `NodeStorage:ArchivePath` | Carpeta de trabajo/archivo temporal. |

> Si cambias estas rutas a otra unidad o carpeta, asegúrate de que **la cuenta del servicio tenga permisos de escritura** sobre ellas.

### Logs y diagnóstico — sección `Diagnostics`

```json
"Diagnostics": {
  "File": {
    "Path": "C:/ProgramData/EdgeGuard/Node/logs",
    "RetainDays": 90,
    "MaxFileSizeMb": 200,
    "RollingInterval": "Day"
  },
  "HealthChecks": {
    "StorageMinAvailableMb": 500,
    "QueueMaxPendingItems": 1000
  }
}
```

| Campo | Descripción |
|-------|-------------|
| `File:Path` | Carpeta de logs del nodo. |
| `File:RetainDays` / `MaxFileSizeMb` | Retención y tamaño máximo por archivo de log. |
| `HealthChecks:StorageMinAvailableMb` | Espacio libre mínimo antes de alertar. |
| `HealthChecks:QueueMaxPendingItems` | Máximo de ítems pendientes en cola antes de alertar. |

> Las integraciones `Seq`, `HttpSink` y `OpenTelemetry` vienen **deshabilitadas** (`"Enabled": false`) por defecto; actívalas solo si tu entorno cuenta con esos servicios.

### Ejemplo mínimo a personalizar

En la mayoría de instalaciones basta con ajustar estos cuatro valores dentro de `HubConnection`:

```jsonc
"HubBaseUrl":   "https://hub.miclinica.com",   // URL real del Hub
"NodeName":     "RIS_HOSPITAL_NORTE",          // nombre único del nodo
"FacilityName": "Hospital Norte",              // institución/sede
"Port":         11112                          // puerto DICOM del nodo
```

Tras editar, aplica los cambios:

```powershell
Restart-Service -Name EdgeGuardNode
```

---

## Reinstalación / actualización

Para actualizar a una nueva versión: **detén el servicio**, extrae el nuevo `.zip` sobre la misma carpeta (sobrescribiendo los binarios) y ejecuta de nuevo `.\install-service.ps1`. El script detecta el servicio existente, lo detiene, lo elimina y lo reinstala apuntando al ejecutable actualizado. La carpeta de datos (`C:\ProgramData\EdgeGuard\Node`) se conserva.

```powershell
Stop-Service -Name EdgeGuardNode -Force
Expand-Archive -Path ".\EdgeGuardNode.zip" -DestinationPath "C:\EdgeGuard\Node" -Force
.\install-service.ps1
```

---

## Solución de problemas

| Síntoma | Causa probable / solución |
|---------|---------------------------|
| `This script must be run as Administrator.` | Abre PowerShell con **Ejecutar como administrador**. |
| `New-Service` falla por ruta inexistente | El `.exe` no está en `-ExePath`. Verifica que extrajiste el `.zip` con su estructura de carpetas o pasa la ruta correcta con `-ExePath`. |
| `... cannot be loaded because running scripts is disabled` | Ver [nota de ExecutionPolicy](#nota-executionpolicy). |
| El servicio arranca y se detiene enseguida | Revisa los logs en `C:\ProgramData\EdgeGuard\Node\logs` y verifica `appsettings.Production.json` (Hub accesible, puerto libre). |
| Puerto DICOM ocupado | Otro proceso usa el `Port` configurado. Cámbialo en `appsettings.Production.json` o libera el puerto. |

### Nota: ExecutionPolicy

Si PowerShell bloquea la ejecución del script:

```powershell
# Solo para esta sesión
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
```

---

## Referencia rápida

```powershell
# Extraer binarios
Expand-Archive -Path ".\EdgeGuardNode.zip" -DestinationPath "C:\EdgeGuard\Node" -Force

# Instalar (como Administrador, desde la carpeta extraída)
cd "C:\EdgeGuard\Node"
.\install-service.ps1

# Arrancar
Start-Service -Name EdgeGuardNode

# Estado / logs
Get-Service -Name EdgeGuardNode
Get-Content "C:\ProgramData\EdgeGuard\Node\logs\node-*.log" -Tail 50

# Desinstalar
Stop-Service -Name EdgeGuardNode -Force; sc.exe delete EdgeGuardNode
```
