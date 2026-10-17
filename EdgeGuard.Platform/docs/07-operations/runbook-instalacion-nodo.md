# Runbook de instalación — Nodo EdgeGuard

Procedimiento completo para instalar un nodo EdgeGuard en el servidor de una
sede, desde la preparación hasta la verificación funcional.

Lo ejecuta personal técnico con consola elevada en el servidor. Para la operación
diaria una vez instalado, el documento es otro: **Nodo: operación, diagnóstico y
soporte**, que se entrega en este mismo paquete.

---

## 1. Alcance y límites

### Qué hace este procedimiento

Despliega un paquete **ya compilado** y self-contained, registra el nodo como
servicio de Windows, escribe su configuración, crea los directorios de datos, abre
los dos puertos en el firewall y verifica que el nodo arranque, escuche DICOM y se
registre con el Hub.

### Qué NO hace — y por lo tanto debe estar resuelto antes o después

| Fuera de alcance | Responsable | Cuándo |
|---|---|---|
| Instalar el Hub | Procedimiento aparte | Antes |
| Dar de alta el PACS destino del nodo | Responsable de la aplicación, desde la consola del Hub | T+1 |
| Definir las reglas de ruteo | Responsable de la aplicación, desde la consola del Hub | T+1 |
| Configurar las modalidades para que envíen al nodo | Proveedor de cada modalidad | T+1 |
| Abrir puertos entre sede y Hub en la red corporativa | Administrador de red | T-7 |
| Respaldos de la base del nodo | Operaciones de la sede | Continuo |

> **El instalador no instala .NET.** El paquete es self-contained: se lleva el
> runtime dentro. Un servidor de sede sin administrador a mano no necesita nada
> preinstalado salvo Windows.

> **El instalador deja el nodo sin PACS.** Recibir estudios y reenviarlos son dos
> cosas distintas: al terminar, el nodo recibe y almacena, pero no reenvía hasta
> que alguien le asigne un PACS desde la consola del Hub. Es esperado, y el paso
> 10 lo reporta como `Degraded`.

---

## 2. Roles y responsabilidades

| Rol | Responsabilidad en este runbook |
|---|---|
| **Ingeniero de despliegue** | Ejecuta el instalador. Requiere consola elevada en el servidor de la sede. |
| **Administrador de red** | Confirma que el servidor alcanza al Hub, y entrega las direcciones de las modalidades y del PACS para acotar el firewall. |
| **Responsable de la aplicación** | Define `NodeName` y `AeTitle`, asigna el PACS y las reglas de ruteo después de instalar. |
| **Responsable de la sede** | Confirma la ventana y avisa a los técnicos de las modalidades. |
| **Aprobador de cambio** | Autoriza la ventana. Recibe la evidencia de cierre (§7.4). |

**Regla de separación.** Si se usa un token de arranque preemitido, quien ejecuta
el instalador lo ve en claro en `node-install.psd1`. El token es de un solo uso y
expira en cinco minutos, así que el riesgo es acotado — pero si aun así no es
aceptable, déjelo vacío: el nodo pide el suyo al Hub al arrancar.

---

## 3. Prerrequisitos

### 3.1 En el servidor de la sede

| Requisito | Cómo se comprueba | Lo verifica el paso 01 |
|---|---|---|
| Windows Server o Windows 10/11 | `Get-CimInstance Win32_OperatingSystem` | Sí, lo registra |
| Consola de PowerShell **elevada** | El título de la ventana dice «Administrador» | Sí, aborta si no |
| **3 GB libres** en la unidad de binarios y en la de datos | `Get-PSDrive C` | Sí, aborta si no |
| Puerto DICOM libre (11112 por defecto) | `Get-NetTCPConnection -LocalPort 11112 -State Listen` | Sí, aborta si lo ocupa otro proceso |
| Puerto de la API libre (5120 por defecto) | Ídem con 5120 | Sí |

> **Por qué los puertos se comprueban antes.** Si el nodo no puede abrir su socket,
> arranca igualmente, falla, y la política de recuperación lo reinicia tres veces.
> El resultado es un servicio «en marcha» que no recibe nada — un síntoma que no se
> parece a su causa.

### 3.2 En la red

| Requisito | Quién lo confirma |
|---|---|
| El servidor alcanza al Hub por HTTP o HTTPS | Administrador de red |
| Las modalidades alcanzan al servidor en el puerto DICOM | Administrador de red |
| El servidor alcanza al PACS en su puerto DICOM | Administrador de red |

El paso 04 comprueba el primero de los tres de verdad, antes de tocar nada.

### 3.3 Datos a recabar antes de la ventana

Sin estos cinco, la instalación se detiene a la mitad.

| Dato | Ejemplo | Quién lo entrega |
|---|---|---|
| **URL del Hub** | `http://hub.miclinica.com` | Responsable de la aplicación |
| **Nombre del nodo**, único entre todos los del Hub | `RIS_HOSPITAL_NORTE` | Responsable de la aplicación |
| **AE title**, máximo 16 caracteres sin espacios | `EDGE_NORTE` | Responsable de la aplicación |
| **IP con la que el nodo se anuncia** | `192.168.1.50` | Administrador de red |
| Direcciones de las modalidades y del PACS | `192.168.1.0/24` | Administrador de red |

> **El AE title es la identidad del nodo.** Con él se anuncia al Hub, con él lo
> llaman las modalidades y con él sale hacia el PACS. Los tres se derivan de un
> solo valor, así que no pueden desalinearse entre sí — pero elegirlo mal los
> desalinea a los tres a la vez respecto del resto de la red. Acuérdelo con quien
> configura las modalidades **antes** de instalar.

---

## 4. El paquete de entrega

### 4.1 Qué se recibe

Un solo archivo:

```
edgeguard-node-installer-<versión>.zip
```

### 4.2 Dos zips, y no son lo mismo

Dentro del anterior viaja un segundo zip con un nombre parecido. **Distinguirlos
importa**, porque confundirlos cuesta media hora de diagnóstico con un mensaje de
error que habla de empaquetado y no de archivos cruzados.

| Archivo | Qué es | Dónde va |
|---|---|---|
| `edgeguard-node-installer-<versión>.zip` | **El instalador.** Esto es lo que se extrae en el servidor | En una carpeta de trabajo, p. ej. `C:\Instaladores\EdgeGuard\node` |
| `edgeguard-node-<versión>.zip` | **La aplicación.** Ya viene dentro del anterior, en `data\`. El instalador la despliega | Se queda donde está. No se extrae a mano |

Los dos llevan la misma versión.

### 4.3 Contenido del instalador

```
install.ps1                    El instalador
node-install.example.psd1      Plantilla de configuración
README.md                      Referencia técnica
bin\                           Los ocho pasos + módulo común + desinstalador
data\
  edgeguard-node-<versión>.zip La aplicación, self-contained
  checksums.sha256             Integridad de lo anterior
manuales\                      Los PDF que se quedan en la sede
log\                           Registro de cada ejecución
```

> **No debe venir `node-install.psd1`**, solo el `.example`. Si el paquete llega
> con uno, es la configuración de **otra sede** y puede llevar su token de
> arranque: bórrelo antes de empezar y créelo de cero según §5.2.

### 4.4 Verificación de integridad

El paso 01 la hace sola, pero conviene comprobarla al recibir el paquete:

```powershell
cd C:\Instaladores\EdgeGuard\node\data
Get-FileHash edgeguard-node-*.zip -Algorithm SHA256
Get-Content checksums.sha256
```

Los dos valores deben coincidir. **Si no coinciden, no instale**: un zip a medio
copiar produce un despliegue que parece correcto y falla al arrancar, con un error
que no señala la causa.

---

## 5. Fase T-1 — Preparación del servidor

### 5.1 Extraer el instalador

```powershell
Expand-Archive -Path .\edgeguard-node-installer-1.4.1.zip `
               -DestinationPath C:\Instaladores\EdgeGuard\node
```

**Esta carpeta no es el directorio de instalación**: es desde donde se ejecuta. Los
binarios acabarán en `InstallPath` (`C:\EdgeGuard\Node` por defecto), que crea el
paso 05.

Compruebe que quedó `install.ps1` en la raíz de lo extraído y `data\` con **un
solo** `.zip`. Si hay dos, el paso 01 elegirá el de versión mayor y lo dirá en el
resumen — pero conviene decidirlo antes que descubrirlo.

### 5.2 Crear el archivo de configuración

```powershell
cd C:\Instaladores\EdgeGuard\node
Copy-Item node-install.example.psd1 node-install.psd1
notepad node-install.psd1
```

Rellene con los datos de §3.3. Las claves que hay que tocar siempre:

```powershell
HubBaseUrl       = 'http://hub.miclinica.com'
NodeName         = 'RIS_HOSPITAL_NORTE'
AeTitle          = 'EDGE_NORTE'
IpAddress        = '192.168.1.50'
FacilityName     = 'Hospital Norte'
Location         = 'Sala de RX - Piso 2'
```

Las rutas por defecto sirven para la mayoría de las sedes:

| Clave | Valor | Qué guarda |
|---|---|---|
| `InstallPath` | `C:\EdgeGuard\Node` | Binarios. **Se reemplaza entero en cada actualización** |
| `DatabasePath` | `C:\ProgramData\EdgeGuard\Node\persistence\edge-node.db` | Base del nodo |
| `StorageRootPath` | `C:\ProgramData\EdgeGuard\Node\data` | Estudios en tránsito |
| `LogPath` | `C:\ProgramData\EdgeGuard\Node\logs` | Registro diario |

> **La base no puede quedar dentro de la carpeta de estudios**, y el instalador lo
> rechaza. Si estuvieran juntas, la medición del espacio contaría el archivo de la
> base como carga DICOM, y la limpieza de emergencia borraría estudios para aliviar
> una presión que en parte no puede aliviar. La convención es `persistence\`
> hermana de `data\`, y el propio nodo se niega a arrancar si se violan.

> **Nada de lo que debe sobrevivir puede ir bajo `InstallPath`.** El instalador
> también lo rechaza: el despliegue reemplaza ese directorio completo.

### 5.3 Punto de control C-1 — validación sin escribir

```powershell
.\install.ps1 -DryRun
```

Recorre **la misma ruta de código** que la instalación real y solo omite los
efectos: valida la configuración, verifica los checksums y prueba la conectividad
con el Hub de verdad.

**No continúe hasta que el dry-run termine sin errores.** Revise en el resumen:

- Que cada valor venga del origen esperado — la columna final dice `default`,
  `archivo` o `parámetro`.
- Que el modo resuelto sea `Install` en una sede nueva.
- Que el paquete resuelto sea la versión que se pretende instalar.

---

## 6. Fase T-0 — Ejecución

### 6.1 Antes de empezar

- [ ] Avisar a los técnicos de las modalidades: durante la instalación no se
      reciben estudios.
- [ ] Confirmar la ventana con el responsable de la sede.
- [ ] Tener a mano la URL de la consola del Hub para verificar el registro.

### 6.2 Ejecutar

```powershell
.\install.ps1
```

Muestra el resumen y pide confirmación. Para instalación desatendida — varias
sedes, un `.psd1` por sede:

```powershell
.\install.ps1 -NonInteractive -ConfigFile .\sedes\norte.psd1
```

### 6.3 Qué hace, paso a paso

| # | Qué hace | Si falla |
|---|---|---|
| **01** | Privilegios, sistema, disco, puertos, paquete, checksums, contenido del zip. Resuelve el modo | Nada se tocó |
| **04** | Conectividad con el Hub: DNS, certificado, que responda y que **sea** un Hub, que esté sano | Nada se tocó |
| **05** | Detiene el servicio, respalda, despliega, escribe `installed.json` | Se revierte al respaldo |
| **06** | Registra el servicio y su recuperación ante fallos. Lo deja **detenido** | Se revierte |
| **07** | Genera `appsettings.Production.json` desde el `.psd1` | Se revierte |
| **08** | Crea los directorios de datos y concede permisos si hace falta | Se revierte |
| **09** | Crea las dos reglas de firewall | Se revierte |
| **10** | Arranca, espera `/health/ready`, comprueba DICOM y el registro con el Hub | Se revierte |

> **El servicio se arranca en el paso 10, no en el 06.** Arrancarlo antes de que
> el 07 escriba la configuración y el 08 cree los directorios produce un ciclo de
> fallo y reinicio contra una configuración a medias, y llena el registro de
> errores que no son el problema real.

### 6.4 Reversión automática

Si algo falla **después** del paso 05, el instalador restaura la instalación
anterior desde el respaldo y vuelve a arrancar el servicio. El respaldo queda en
`<InstallPath>.backup-<fecha>` y no se borra.

Antes del 05 no hay nada que revertir porque nada se tocó.

### 6.5 Códigos de salida

| Código | Significa |
|---|---|
| `0` | Instalación completa |
| `1` | Abortó. El registro dice en qué paso y por qué |
| `2` | El operador canceló en la confirmación |

---

## 7. Fase T+0 — Verificación funcional

El paso 10 ya comprobó lo básico. Esto confirma que el nodo sirve para lo que se
instaló.

### 7.1 El nodo responde

```powershell
Invoke-RestMethod http://localhost:5120/health/ready
```

| Revisión | Esperado al terminar |
|---|---|
| `database` | `Healthy` |
| `storage` | `Healthy` |
| `pacs` | **`Degraded`** — todavía no hay PACS asignado. Es lo normal |

### 7.2 El nodo aparece en el Hub

En la consola del Hub, busque el nodo por su `NodeName`. Debe aparecer **En
línea**. Confirme que el AE title y el puerto DICOM coincidan con lo acordado en
§3.3.

> Si no aparece, el servicio está en marcha y reintentará solo. Revise el registro
> del día en `LogPath` antes de escalar.

### 7.3 Punto de control C-2 — recepción real

Este es el que cuenta. Con el responsable de la aplicación:

1. Asigne el PACS destino al nodo desde la consola del Hub.
2. Pida a un técnico que envíe un **C-ECHO** desde una modalidad al nodo.
   Debe responder.
3. Pida un **estudio de prueba** desde esa modalidad.
4. En la consola del Hub, siga el estudio: debe pasar de **Recibido** a
   **Enviando** y terminar en **Completado**.
5. Confirme que el estudio se ve en el PACS.

**Si el estudio se queda en *Enviando***, el nodo recibe pero el PACS no acepta.
Casi siempre es que el PACS no reconoce el AE title del nodo. Se resuelve del lado
del PACS, no del nodo.

### 7.4 Evidencia de cierre

Para el aprobador del cambio:

- [ ] Salida completa de `install.ps1` (queda en `log\install-<fecha>.log`)
- [ ] Respuesta de `/health/ready`
- [ ] Captura del nodo **En línea** en la consola del Hub
- [ ] `StudyInstanceUID` del estudio de prueba y su estado **Completado**
- [ ] Versión instalada, de `installed.json`

---

## 8. Fase T+1 — Endurecimiento

### 8.1 Acotar el firewall

El instalador deja los dos puertos abiertos a cualquier origen si el `.psd1` no
dice otra cosa. DICOM es TCP sin TLS ni autenticación fuerte, y transporta PHI.

En `node-install.psd1`:

```powershell
DicomRemoteAddress   = @('192.168.1.0/24')   # modalidades y PACS
NodeApiRemoteAddress = @('10.0.0.5')         # solo el Hub
```

Y reaplique sin tocar binarios:

```powershell
.\install.ps1 -Mode Repair
```

### 8.2 Cuenta del servicio

El servicio se registra como `LocalSystem`. Si la política de la institución exige
mínimo privilegio, cambie la cuenta en `services.msc` → Propiedades → Iniciar
sesión. Los permisos sobre los tres directorios ya quedan preparados si declaró
`ServiceAccount` en el `.psd1`.

La contraseña no pasa por el instalador, a propósito.

### 8.3 Respaldos

| Qué | Dónde | Frecuencia |
|---|---|---|
| Base del nodo | `DatabasePath` | Diaria |
| Configuración | `node-install.psd1` | Al cambiar |

Los estudios de `StorageRootPath` **no se respaldan**: son copias de paso, y el
original está en el PACS.

### 8.4 Entregar los manuales

Deje en la sede los dos PDF de `manuales\`, y la tarjeta impresa junto al
servidor.

---

## 9. Actualización

```powershell
# 1. Reemplace el .zip en data\ por el de la versión nueva, con su checksums.sha256
# 2. Valide
.\install.ps1 -DryRun
# 3. Actualice
.\install.ps1
```

El modo `Auto` detecta la instalación previa por `installed.json` y elige `Update`.

**Qué sobrevive:** la base, los estudios y los registros — viven fuera de
`InstallPath`. **Qué se reemplaza:** los binarios y `appsettings.Production.json`,
que se regenera desde el `.psd1`.

> **No edite `appsettings.Production.json` a mano.** Lo reescribe cada
> actualización. Lo que se edita es el `.psd1`.

### 9.1 Lo que una actualización NO cambia

En un nodo **ya registrado**, la dirección del Hub, el AE title, los puertos y la
carpeta de estudios **los manda la base de datos**, no el archivo de
configuración. Cambiarlos en el `.psd1` y reinstalar no surte efecto.

Es deliberado: así sobreviven al reinicio las ediciones hechas desde la consola
del Hub. El paso 07 lo avisa cuando detecta una base previa.

- **Para cambiarlos:** desde la consola del Hub.
- **Para mover el nodo a otro Hub:** detenga el servicio y elimine la base. Se
  pierde el historial de qué estudios se entregaron; los estudios no.

---

## 10. Reversión y desinstalación

### 10.1 Volver a la versión anterior

El paso 05 deja un respaldo íntegro en `<InstallPath>.backup-<fecha>`:

```powershell
Stop-Service -Name EdgeGuardNode
Remove-Item C:\EdgeGuard\Node -Recurse -Force
Copy-Item C:\EdgeGuard\Node.backup-20260922-140312 C:\EdgeGuard\Node -Recurse
Start-Service -Name EdgeGuardNode
```

### 10.2 Desinstalar

```powershell
# Quita servicio, reglas de firewall y binarios. CONSERVA los datos.
.\bin\Uninstall-Node.ps1

# Retiro definitivo: borra también base, estudios y registros.
.\bin\Uninstall-Node.ps1 -PurgeData
```

Conservar los datos por defecto es deliberado: la desinstalación más frecuente no
es un retiro, sino un paso de una reinstalación o de una migración de servidor.
`-PurgeData` pide **escribir el nombre del servicio** para confirmar: una
confirmación de una letra se contesta por reflejo.

---

## 11. Diagnóstico de fallos

Cada ejecución deja su registro completo en `log\install-<fecha>.log`.

| Mensaje | Causa | Qué hacer |
|---|---|---|
| `debe ejecutarse como Administrador` | Consola sin elevar | Abrir PowerShell como Administrador |
| `El puerto 11112 … ya lo escucha: <proceso>` | Otro programa tiene el puerto | Liberarlo, o dar otro puerto en el `.psd1` |
| `El hash de '<archivo>' no coincide` | Zip corrupto o incompleto | Volver a copiar desde el paquete original |
| `El paquete no trae el runtime (falta hostfxr.dll)` | Se publicó framework-dependent | Rehacer el paquete self-contained |
| `El nombre '<host>' no resuelve` | DNS de la sede | Usar la IP del Hub, o escalar a redes |
| `La dirección respondió, pero no como un Hub` | Proxy o portal cautivo en medio | Verificar `HubBaseUrl`; escalar a redes |
| `El certificado del Hub no valida` | Certificado autofirmado | Instalar uno de confianza en el Hub, o usar HTTP en red privada |
| `'DatabasePath' está dentro de 'StorageRootPath'` | Rutas mal puestas | Separar las carpetas (§5.2) |
| `no puede estar dentro de 'InstallPath'` | Datos bajo los binarios | Moverlos fuera (§5.2) |
| `El AE title … tiene N caracteres` | Supera los 16 de DICOM | Acortarlo |
| `El nodo no respondió en /health/ready` | El nodo no terminó de arrancar | Ver `logs\node-bootstrap-*.log` en `InstallPath` |
| `NO está escuchando DICOM en el puerto N` | El puerto se ocupó entre el 01 y el 10 | Ver el registro del nodo del día |

### 11.1 Instalar sin Hub disponible

Si el Hub todavía no existe o el enlace no está tendido:

```powershell
.\install.ps1 -SkipHubCheck
```

El paso 04 registra el fallo como aviso y continúa, y el paso 10 no espera el
registro. El nodo lo intentará solo cuando el Hub esté disponible.

**Por defecto no se salta**, porque el caso común no es una instalación
adelantada: es una URL mal escrita.

---

## 12. Apéndices

### Apéndice A — Parámetros de `install.ps1`

| Parámetro | Para qué |
|---|---|
| `-DryRun` | Recorre todo sin escribir |
| `-NonInteractive` | No pregunta. Aborta listando todo lo que falte de una vez |
| `-Mode` | `Auto` (default), `Install`, `Update`, `Repair` |
| `-ConfigFile` | Un `.psd1` distinto del de al lado |
| `-PackagePath` | Forzar un `.zip` concreto de `data\` |
| `-SkipHubCheck` | Continuar aunque el Hub no responda |
| `-InstallPath`, `-ServiceName`, `-AeTitle`, `-DicomPort`, `-NodeApiPort`, `-HubBaseUrl`, `-NodeName`, `-IpAddress` | Sobreescrituras puntuales; ganan sobre el archivo |

### Apéndice B — Precedencia de la configuración

`parámetro de línea de comandos` > `node-install.psd1` > `valor por defecto`

El resumen dice de dónde salió cada valor. Saberlo es la mitad del trabajo cuando
una instalación no queda como se esperaba.

### Apéndice C — Dónde manda cada clave

Lo menos evidente del nodo, y la causa de la mayoría de las confusiones al
actualizar.

| Capa | Claves | Quién manda |
|---|---|---|
| **A** | `ConnectionStrings`, `Diagnostics:*` | El archivo, siempre |
| **B** | Todo `HubConnection` | El archivo en el primer arranque; la base después |
| **C** | AE title, puerto DICOM, puerto de la API, carpeta de estudios | El archivo mientras el nodo no esté registrado; la base después |

En las capas B y C, «después» significa «desde que el nodo tiene credencial del
Hub». Ver §9.1.

### Apéndice D — Los dos puertos

| Puerto | Default | Quién lo usa | Regla de firewall |
|---|---|---|---|
| DICOM | 11112 | Las modalidades, para enviar estudios | `EdgeGuard Node - DICOM SCP` |
| API | 5120 | El Hub, para empujar configuración | `EdgeGuard Node - API` |

Ambas reglas se recrean en cada ejecución del instalador. Cambiar un puerto y
dejar la regla vieja dejaría el puerto nuevo cerrado y el viejo abierto sin nada
detrás: los dos errores a la vez.

### Apéndice E — Comprobaciones rápidas

```powershell
Get-Service -Name EdgeGuardNode                              # ¿corre?
Invoke-RestMethod http://localhost:5120/health/ready         # ¿está sano?
Get-NetTCPConnection -LocalPort 11112 -State Listen          # ¿recibe DICOM?
Get-Content C:\EdgeGuard\Node\installed.json | ConvertFrom-Json   # ¿qué versión?
Get-NetFirewallRule -DisplayName 'EdgeGuard Node*'           # ¿puertos abiertos?
```

---

## Control del documento

| Campo | Valor |
|---|---|
| Procedimiento | Instalación de un nodo EdgeGuard en el servidor de una sede |
| Aplica a | `setup\node\install.ps1` |
| Requiere | Consola elevada en el servidor de la sede |
| Documento relacionado | *Nodo: operación, diagnóstico y soporte* |
