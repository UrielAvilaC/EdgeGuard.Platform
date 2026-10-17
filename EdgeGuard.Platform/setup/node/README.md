# Instalador del Nodo EdgeGuard

Instala el nodo como servicio de Windows en el servidor de una sede. Despliega un
paquete ya compilado, registra el servicio, escribe su configuración, abre los
puertos y verifica que arranque y se registre con el Hub.

No compila nada y **no hace falta instalar .NET**: el paquete es self-contained.

Misma arquitectura que el instalador del Hub (`setup/hub/`) — esquema declarativo,
validación acumulativa, `-DryRun` real, respaldo con reversión — cambiando IIS por
el servicio de Windows.

---

## Antes de empezar

| Requisito | Detalle |
|---|---|
| **Permisos** | Sesión de PowerShell **como Administrador**. Registra un servicio y abre puertos. |
| **Paquete** | Un `edgeguard-node-<versión>.zip` y su `checksums.sha256` en `data\`. Los genera `setup\common\Seal-Version.ps1 -Target Node`. |
| **Configuración** | Copia `node-install.example.psd1` como `node-install.psd1` y rellénalo. |
| **Hub** | Debe estar en marcha y accesible desde el servidor, salvo que uses `-SkipHubCheck`. |

---

## Uso

```powershell
# 1. Validación completa sin escribir nada. Hazlo siempre primero.
.\install.ps1 -DryRun

# 2. Instalación, pidiendo confirmación tras el resumen
.\install.ps1

# 3. Desatendida (varias sedes, un .psd1 por sede)
.\install.ps1 -NonInteractive -ConfigFile .\sedes\norte.psd1
```

`-DryRun` **no es una simulación aparte**: recorre la misma ruta de código que la
instalación real y solo omite los efectos. Valida prerrequisitos, verifica los
checksums y prueba la conectividad con el Hub de verdad.

### Modos

| Modo | Qué hace | Pasos |
|---|---|---|
| `Install` | Instalación nueva | Todos |
| `Update` | Reemplaza binarios y reaplica configuración | Todos |
| `Repair` | Reaplica configuración sin tocar binarios | Omite el 05 |
| `Auto` (default) | Elige según `installed.json` | — |

### Opciones

| Parámetro | Para qué |
|---|---|
| `-SkipHubCheck` | Instalar antes de que el Hub exista o antes de que el enlace esté tendido |
| `-PackagePath` | Forzar un `.zip` concreto de `data\` |
| `-ConfigFile` | Un `.psd1` distinto del de al lado |

---

## Los pasos

Se conservan los identificadores del instalador del Hub aunque falten el 02 y el
03 (Hosting Bundle y características de IIS, que aquí no aplican): quien conoce un
instalador reconoce el otro.

| # | Qué hace |
|---|---|
| **01** | Privilegios, sistema, espacio en disco, **puertos libres**, paquete, checksums, contenido del zip. Resuelve el modo |
| **04** | **Conectividad con el Hub**: DNS, certificado, que responda y que sea un Hub, que esté sano |
| **05** | Detiene el servicio, respalda, despliega, escribe `installed.json` |
| **06** | Registra el servicio y su política de recuperación. Lo deja **detenido** |
| **07** | Genera `appsettings.Production.json` desde el `.psd1` |
| **08** | Crea los directorios de datos y, si hace falta, concede permisos |
| **09** | Dos reglas de firewall: DICOM y API |
| **10** | Arranca, espera `/health/ready`, comprueba DICOM y el registro con el Hub |

---

## Las tres capas de configuración

Lo menos evidente del nodo, y lo que hay que entender antes de tocar el `.psd1`.
No todas las claves pesan igual.

**Capa A — `appsettings` manda siempre.** `ConnectionStrings:NodeDatabase`,
`Diagnostics:*`, `HubConnection:Enabled`. La base nunca las sombrea.

**Capa B — `appsettings` siembra la base en el primer arranque; después manda la
base.** Todo `HubConnection`: `NodeSettingsHubHydrator` las copia a las filas
`hub.*` tras migrar.

**Capa C — la base las sombrea desde el arranque.** `DicomServer:AeTitle`,
`DicomServer:Port`, `NodeApi:Port` y `NodeStorage:RootPath`. El proveedor de
configuración de SQLite se registra después de `appsettings` y tiene la
precedencia más alta, así que la fila gana. Escribirlas en el archivo funciona
porque existe `NodeSettingsDeploymentHydrator`, que las copia a sus filas al
arrancar.

### La consecuencia que sorprende

En un nodo **ya registrado** (tiene `hub.api_key`), los dos hidratadores solo
rellenan filas vacías. Eso significa que **cambiar `HubBaseUrl`, el AE title o los
puertos en el `.psd1` y reinstalar NO los cambia**: el operador vería los valores
correctos en el archivo y el nodo comportándose como antes.

Es deliberado — así sobreviven al reinicio las ediciones que el operador haga
desde el Hub — y el paso 07 lo avisa cuando detecta una base previa.

- **Para cambiarlos:** desde la consola del Hub.
- **Para mover el nodo a OTRO Hub:** detén el servicio y elimina la base. Se
  pierde el historial de qué estudios se entregaron; los estudios no.

---

## El AE title es la identidad del nodo

Con él se anuncia al Hub, con él lo llaman las modalidades y con él sale como SCU
hacia el PACS. Los tres se derivan de **una sola** fila (`dicom.ae_title`), de modo
que no pueden desalinearse entre sí — pero escribirla mal los desalinea a los tres
a la vez respecto del resto de la red.

Máximo 16 caracteres, sin espacios. Lo valida el instalador y lo vuelve a validar
el nodo al arrancar.

---

## Si algo falla

El registro completo de cada ejecución queda en `log\install-<fecha>.log`.

**Reversión automática.** Si un paso falla después del despliegue, el instalador
restaura la instalación anterior desde el respaldo que hizo el paso 05 y vuelve a
arrancar el servicio. El respaldo se conserva.

**El instalador no borra datos.** La base, los estudios y los registros viven
fuera de `InstallPath` —el instalador rechaza una configuración que los ponga
dentro— y sobreviven a cualquier actualización.

---

## Desinstalar

```powershell
# Quita servicio, reglas de firewall y binarios. CONSERVA los datos.
.\bin\Uninstall-Node.ps1

# Retiro definitivo: borra también base, estudios y registros.
.\bin\Uninstall-Node.ps1 -PurgeData
```

Conservar los datos por defecto es deliberado: la desinstalación más frecuente no
es un retiro, es un paso de una reinstalación o de una migración de servidor.
`-PurgeData` pide escribir el nombre del servicio para confirmar — una
confirmación de una letra se contesta por reflejo.

---

## Manuales del cliente

En `manuales\`. Se entregan con el instalador y están escritos para quien está a
cargo del equipo de imagen en la sede, sin formación técnica. Ver
`manuales/README.md`.
