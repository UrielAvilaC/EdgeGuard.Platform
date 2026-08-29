# Runbook de instalación — EdgeGuard Hub

**Documento operativo controlado.** Procedimiento de instalación, verificación y
reversión del Hub EdgeGuard sobre Windows Server + IIS mediante el instalador
`setup\hub\install.ps1`.

| Campo | Valor |
|---|---|
| Componente | EdgeGuard Hub (API + SPA + listener HL7 MLLP) |
| Plataforma destino | Windows Server 2019 / 2022, IIS 10 |
| Motor de datos | PostgreSQL 15 / 16 (externo, preexistente) |
| Mecanismo | `setup\hub\install.ps1` — 10 pasos idempotentes con reversión automática |
| Duración estimada | 45–90 min (ventana recomendada: 2 h) |
| Requiere ventana de cambio | Sí para `Update`; no necesariamente para `Install` en servidor nuevo |
| Reversible | Sí, desde el paso 05 en adelante (respaldo automático) |

---

## Índice

1. [Alcance y límites](#1-alcance-y-límites)
2. [Roles y responsabilidades](#2-roles-y-responsabilidades)
3. [Modelo de despliegue y decisiones de arquitectura](#3-modelo-de-despliegue-y-decisiones-de-arquitectura)
4. [Prerrequisitos](#4-prerrequisitos)
5. [Fase T-7 — Preparación del paquete](#5-fase-t-7--preparación-del-paquete-build)
6. [Fase T-1 — Preparación del servidor](#6-fase-t-1--preparación-del-servidor)
7. [Fase T-0 — Ejecución de la instalación](#7-fase-t-0--ejecución-de-la-instalación)
8. [Fase T+0 — Verificación funcional](#8-fase-t0--verificación-funcional)
9. [Fase T+1 — Endurecimiento post-instalación](#9-fase-t1--endurecimiento-post-instalación)
10. [Actualización de una instalación existente](#10-actualización-de-una-instalación-existente)
11. [Reversión y desinstalación](#11-reversión-y-desinstalación)
12. [Diagnóstico de fallos](#12-diagnóstico-de-fallos)
13. [Apéndices](#13-apéndices)

---

## 1. Alcance y límites

### Qué hace este procedimiento

Despliega un paquete **ya compilado** (backend + SPA en un solo `.zip`), configura
el sitio y el app pool de IIS, crea los directorios de estado, abre el puerto MLLP
en el firewall y verifica que el Hub arranque y responda `/health/live`.

### Qué NO hace — y por lo tanto debe estar resuelto antes

| Fuera de alcance | Responsable | Cuándo |
|---|---|---|
| Compilar el backend o el SPA | Build / DevOps | T-7 |
| Instalar o configurar PostgreSQL | DBA | T-7 |
| Crear el rol y la base de datos | DBA | T-7 |
| Emitir o instalar certificados TLS | PKI / Seguridad | T-7 |
| Configurar el binding HTTPS en IIS | Operaciones | T+1 (manual, post-instalación) |
| Instalar los Edge Nodes | Ver `deployment-node.md` | Posterior |
| Respaldos de la base de datos | DBA | Continuo |

> **El instalador solo verifica la base, nunca la crea.** Comprueba que el rol y la
> base existan, que las credenciales sirvan y que el rol tenga `CREATE` en el
> esquema `public`, porque el Hub aplica las migraciones de EF Core al arrancar.

> **El instalador deja el sitio en HTTP.** El binding HTTPS y el certificado se
> añaden a mano en IIS Manager después de instalar. El instalador no los toca ni
> los elimina en actualizaciones posteriores.

---

## 2. Roles y responsabilidades

| Rol | Responsabilidad en este runbook |
|---|---|
| **Ingeniero de despliegue** | Ejecuta el instalador. Requiere consola elevada en el servidor. |
| **DBA** | Entrega rol, base, credenciales y confirma el `GRANT CREATE`. Firma el punto de control C-2. |
| **Administrador de red / Seguridad** | Abre puertos, entrega la subred del HIS/RIS para acotar MLLP, entrega el certificado TLS. |
| **Responsable de la aplicación** | Define y custodia la credencial del administrador inicial, valida funcionalmente. |
| **Aprobador de cambio** | Autoriza la ventana. Recibe la evidencia de cierre (§8.5). |

**Regla de separación:** quien ejecuta el instalador ve la contraseña de
PostgreSQL en claro (queda en `hub-install.psd1` o se teclea). Si eso no es
aceptable en la organización, use el mecanismo de variable de entorno o
`-DbCredential` descrito en el [Apéndice C](#apéndice-c--precedencia-de-la-contraseña-de-postgresql).

---

## 3. Modelo de despliegue y decisiones de arquitectura

Cuatro decisiones del instalador condicionan la operación. Entenderlas evita
diagnósticos caros más adelante.

### 3.1 Toda la configuración vive en variables de entorno del app pool

`appsettings.Production.json` va **vacío a propósito**. El estado de configuración
vive en IIS, de modo que una actualización nunca tiene que fusionar archivos de
configuración. El paso 07 reemplaza la colección completa de variables: una clave
retirada de la configuración desaparece de verdad en lugar de sobrevivir de una
instalación anterior.

**Consecuencia operativa:** si alguien recrea el app pool a mano, pierde toda la
configuración. La recuperación es `.\install.ps1 -Mode Repair`.

### 3.2 El key ring de Data Protection vive FUERA del directorio de instalación

El paso 05 reemplaza el directorio de instalación completo en cada actualización.
Si el key ring estuviera dentro, se perdería, y con él la capacidad de descifrar
los `SigningSecret` de **todos los nodos registrados**. Los pasos 07 y 08 abortan
si la ruta cae dentro de `InstallPath`.

**Valor por defecto:** `C:\inetpub\edgeguard\dp-keys` — fuera de
`C:\inetpub\EdgeGuard\Hub`.

### 3.3 El listener HL7 MLLP vive dentro del proceso de IIS

`startMode=AlwaysRunning` e `idleTimeout=00:00:00` **no son afinación de
rendimiento**. Con la configuración por defecto de IIS, el proceso se apaga tras
20 minutos sin peticiones HTTP y el listener MLLP deja de aceptar mensajes del
HIS/RIS sin que nadie se entere: el sitio web sigue respondiendo con normalidad.

Por la misma razón el paso 06 activa la precarga (`preloadEnabled`): sin ella el
proceso no arranca hasta la primera petición HTTP y el listener no existe hasta
entonces.

### 3.4 WebSockets se habilita en dos niveles

Habilitar la característica de Windows (paso 03) **no basta**: el sitio también
debe permitirlo (paso 06). Si falta a nivel de sitio, SignalR cae a long polling o
falla, el monitoreo en tiempo real del SPA queda mudo y el resto del sitio funciona
con normalidad — un fallo que se diagnostica tarde y mal.

---

## 4. Prerrequisitos

### 4.1 Hardware del servidor Hub

| Recurso | Mínimo | Recomendado producción |
|---|---|---|
| CPU | 2 núcleos | 4+ |
| RAM | 4 GB | 8 GB+ |
| Disco | 50 GB | 200 GB+ (retención de auditoría y HL7) |
| Tipo de disco | HDD | SSD |
| Red | 100 Mbps | 1 Gbps |
| **Espacio libre en el volumen de instalación** | **5 GB** | — |

> El paso 01 **aborta** con menos de 5 GB libres en el volumen de `InstallPath`.
> El Hub almacena metadatos DICOM, auditoría y mensajes HL7 — no píxeles.

### 4.2 Software del servidor

| Componente | Versión | Quién lo instala |
|---|---|---|
| Windows Server | 2019 / 2022 | Provisión de infraestructura |
| PowerShell | 5.1 (el del sistema) | Incluido |
| IIS 10 + WebSockets + consola de administración | — | **Paso 03 del instalador** |
| ASP.NET Core Hosting Bundle | 10.x | **Paso 02 del instalador** |
| PostgreSQL 15 / 16 | — | DBA, servidor externo |
| `psql.exe` (cliente) | opcional | Recomendado — habilita la verificación completa del paso 04 |

> **Sistemas cliente (Windows 10/11 Pro):** soportados para sitios pequeños y
> desarrollo. El paso 01 emite un aviso y el paso 03 usa
> `Enable-WindowsOptionalFeature` en lugar de `Install-WindowsFeature`.

> **`psql` no es obligatorio, pero sí conveniente.** Sin él, el paso 04 solo puede
> comprobar el puerto y sondear el protocolo; la contraseña y el permiso `CREATE`
> quedan sin validar hasta el paso 10, donde un fallo cuesta más de diagnosticar.
> Instalar el cliente de PostgreSQL en el servidor Hub es la recomendación.

### 4.3 Base de datos — entregable del DBA

```sql
-- Ejecutar en el servidor PostgreSQL antes de T-1
CREATE ROLE edgeguard LOGIN PASSWORD '<contraseña fuerte>';
CREATE DATABASE edgeguard_hub OWNER edgeguard ENCODING 'UTF8';

-- Verificar el permiso que el paso 04 exige:
\c edgeguard_hub
GRANT CREATE ON SCHEMA public TO edgeguard;
```

| Requisito | Valor |
|---|---|
| Codificación | `UTF8` |
| Privilegios del rol | `CONNECT`, `CREATE` en `public`, DML completo. **Sin superusuario.** |
| Alcance de red | TCP 5432 alcanzable desde el servidor Hub |
| `pg_hba.conf` | Debe admitir la conexión desde la IP del servidor Hub |

**Verificación previa del DBA** (desde el propio servidor Hub, si hay `psql`):

```bash
psql -h <DbHost> -p 5432 -U edgeguard -d edgeguard_hub -c "SELECT has_schema_privilege(current_user,'public','CREATE');"
```

Debe devolver `t`. Si devuelve `f`, el paso 04 abortará la instalación.

### 4.4 Puertos y red

| Puerto | Protocolo | Dirección | Propósito | Regla |
|---|---|---|---|---|
| 80 (o el configurado) | TCP | Entrante | Sitio IIS: SPA + API + SignalR | La registra IIS al crear el sitio |
| 443 | TCP | Entrante | HTTPS — **producción** | Manual, fase T+1 |
| 8001 | TCP | Entrante | Listener HL7 MLLP | **Paso 09 del instalador** |
| 5432 | TCP | Saliente | PostgreSQL | Firewall corporativo |

> **El paso 09 no abre el puerto HTTP.** IIS registra sus propias reglas al crear
> el sitio; duplicarlas solo confunde la auditoría del firewall.

> **MLLP es TCP crudo, sin TLS ni autenticación, y transporta PHI.** El valor por
> defecto `Hl7RemoteAddress = 'Any'` expone el puerto a toda la red que alcance al
> servidor. **Obtenga la subred del HIS/RIS antes de instalar** (p. ej.
> `10.20.30.0/24`) y configúrela. Si instala con `Any`, el instalador emite un
> aviso en el resumen y otro en el paso 09; anótelo como deuda de seguridad con
> fecha de cierre.

### 4.5 Insumos a reunir antes de T-1

Complete esta tabla y adjúntela al registro de cambio:

| # | Insumo | Valor | Origen |
|---|---|---|---|
| 1 | Paquete `edgeguard-hub-<versión>.zip` | | Build (§5) |
| 2 | `checksums.sha256` | | Build (§5) |
| 3 | `dotnet-hosting-10.x.x-win.exe` | | Descarga oficial .NET |
| 4 | Host, puerto, base, rol y contraseña de PostgreSQL | | DBA |
| 5 | Host header del sitio (FQDN) | | Red / DNS |
| 6 | Subred del HIS/RIS para MLLP | | Red |
| 7 | Identificador de instancia (`InstanceId`) | | Operaciones |
| 7b | Usuario y contraseña del administrador inicial | | Responsable de la aplicación |
| 8 | Certificado TLS (`.pfx`) + contraseña | | PKI |
| 9 | Cuenta con privilegios de administrador local | | Directorio |
| 10 | Ventana de cambio aprobada | | Gestión de cambios |

---

## 5. Fase T-7 — Preparación del paquete (build)

Se ejecuta en la **máquina de compilación**, no en el servidor. Requiere .NET SDK
10+, Node.js 22+ LTS y acceso al repositorio.

> ### El orden importa y equivocarse produce un paquete que parece correcto
>
> `angular.json` escribe su salida directamente en
> `src\backend\Dicom.Edge.Hub.Api\wwwroot`, y el `.csproj` **no** tiene ningún
> target que dispare el build del SPA: es pura convención. Si `dotnet publish`
> corre antes que `npm run build`, el paquete sale sin front y el sitio publica
> solo la API — algo que de otro modo solo se descubre al abrir el navegador.
>
> El paso 01 del instalador verifica que el zip contenga `wwwroot/index.html`
> precisamente por esto, y aborta si falta.

### 5.1 Compilar el SPA — primero

```powershell
cd src\frontend\dicomedge-ui
npm ci
npm run build
```

### 5.2 Publicar el backend

```powershell
dotnet publish src\backend\Dicom.Edge.Hub.Api --configuration Release --runtime win-x64 --self-contained false --output .\publish
```

### 5.3 Comprimir

```powershell
Compress-Archive -Path .\publish\* -DestinationPath .\edgeguard-hub-1.2.0.zip
```

### 5.4 Generar los checksums

```powershell
Get-FileHash .\edgeguard-hub-1.2.0.zip -Algorithm SHA256 | ForEach-Object { "$($_.Hash)  $(Split-Path $_.Path -Leaf)" } | Out-File .\checksums.sha256 -Encoding utf8
```

Añada al mismo archivo el hash del Hosting Bundle:

```powershell
Get-FileHash .\dotnet-hosting-10.0.8-win.exe -Algorithm SHA256 | ForEach-Object { "$($_.Hash)  $(Split-Path $_.Path -Leaf)" } | Out-File .\checksums.sha256 -Append -Encoding utf8
```

**Formato exigido** — una línea por artefacto, hash en mayúsculas, dos espacios de
separación:

```
A3F5...9C  edgeguard-hub-1.2.0.zip
7B21...4E  dotnet-hosting-10.0.8-win.exe
```

### 5.5 Verificación del build — punto de control C-1

- [ ] El zip contiene `wwwroot/index.html`
- [ ] El zip contiene `Dicom.Edge.Hub.Api.dll` y `web.config`
- [ ] El zip contiene `Npgsql.dll`
- [ ] `checksums.sha256` declara ambos artefactos y ninguna línea tiene formato inválido
- [ ] El nombre del zip contiene la versión en formato `n.n.n` (el instalador la extrae de ahí)

Comprobación rápida del contenido sin descomprimir:

```powershell
Add-Type -AssemblyName System.IO.Compression.FileSystem; [System.IO.Compression.ZipFile]::OpenRead((Resolve-Path .\edgeguard-hub-1.2.0.zip)).Entries | Where-Object { $_.FullName -match 'index.html|Hub.Api.dll|web.config|Npgsql' } | Select-Object FullName
```

**Firma C-1 — Build:** ______________________  Fecha: __________

---

## 6. Fase T-1 — Preparación del servidor

### 6.1 Copiar el árbol del instalador

Copie la carpeta `setup\hub\` completa al servidor, por ejemplo a
`C:\Deploy\EdgeGuard\hub\`. Estructura esperada:

```
hub\
├── install.ps1
├── hub-install.example.psd1
├── bin\        (10 pasos + módulo + desinstalador)
├── data\       ← depositar aquí los artefactos
└── log\        ← se genera aquí el registro
```

### 6.2 Depositar los artefactos en `data\`

| Archivo | Obligatorio |
|---|---|
| `edgeguard-hub-<versión>.zip` | Sí |
| `checksums.sha256` | Sí |
| `dotnet-hosting-10.x.x-win.exe` | No, pero recomendado |

> Si hay más de un `.zip`, el instalador toma el de **versión mayor** y lo indica
> en el resumen. Para forzar otro, use `-PackagePath`.

> **Servidores sin salida a internet:** depositar el Hosting Bundle en `data\` es
> la vía soportada. La descarga automática requiere fijar URL y hash en
> `bin\02-Install-HostingBundle.ps1`, que se dejan vacíos a propósito: un hash de
> relleno convertiría la verificación en un trámite que siempre pasa.

### 6.3 Crear el archivo de configuración

```powershell
cd C:\Deploy\EdgeGuard\hub
Copy-Item hub-install.example.psd1 hub-install.psd1
notepad hub-install.psd1
```

Valores a revisar obligatoriamente antes de continuar:

| Clave | Acción requerida |
|---|---|
| `HostHeader` | FQDN real del sitio. Alimenta también `Jwt__Issuer`. |
| `DbHost` / `DbPort` / `DbName` / `DbUser` | Datos entregados por el DBA |
| `DbPassword` | **Cambiar** `CAMBIAR_ANTES_DE_INSTALAR` — o dejar vacío y usar variable de entorno |
| `AdminUsername` | Usuario del administrador inicial. Se normaliza a minúsculas |
| `AdminPassword` | **Obligatoria en instalaciones nuevas.** Sin ella no se crea cuenta y no habrá con qué entrar al SPA. Mínimo 8 caracteres con mayúscula, minúscula, dígito y carácter especial |
| `Hl7RemoteAddress` | Subred del HIS/RIS. **No dejar en `Any` en producción.** |
| `DataProtectionKeyPath` | Debe quedar **fuera** de `InstallPath` |
| `InstanceId` | Identificador único de esta instancia |
| `RedactionMode` | `Strict` en producción (PHI redactado) |
| `NodeAuthEnforce` | `$false` en la instalación inicial. Ver §9.4. |
| `Hl7ValidateBeforeAck` | `$false` en la instalación inicial. Ver §9.4. |
| `AppPoolIdentity` | Ver la advertencia de §9.2 |

### 6.4 Punto de control C-2 — prerrequisitos externos

Antes de ejecutar nada, confirme con los responsables:

- [ ] **DBA:** rol y base creados, `GRANT CREATE ON SCHEMA public` aplicado, `pg_hba.conf` admite la IP del Hub
- [ ] **Red:** 5432 saliente abierto; subred del HIS/RIS documentada
- [ ] **DNS:** el `HostHeader` resuelve a la IP del servidor
- [ ] **PKI:** certificado `.pfx` disponible para la fase T+1
- [ ] **Servidor:** ≥5 GB libres en el volumen de instalación
- [ ] **Cuenta:** privilegios de administrador local confirmados
- [ ] **Ventana de cambio:** aprobada y comunicada

**Firma C-2 — Preparación:** ______________________  Fecha: __________

---

## 7. Fase T-0 — Ejecución de la instalación

### 7.1 Abrir una consola elevada

El paso 01 **aborta** si la sesión no está elevada.

```powershell
Start-Process powershell -Verb RunAs
```

### 7.2 Ensayo obligatorio — `-DryRun`

**No omita este paso.** `-DryRun` recorre el procedimiento completo sin escribir
nada. Las verificaciones de solo lectura se ejecutan **de verdad**: checksums,
contenido del paquete y conexión a PostgreSQL. Detectar ahí un paquete corrupto o
una credencial incorrecta es justo donde sale barato.

```powershell
cd C:\Deploy\EdgeGuard\hub
.\install.ps1 -DryRun
```

Revise el resumen que muestra el instalador antes de confirmar. Cada valor viene
etiquetado con su origen (archivo, parámetro, default, interactivo).

**Criterio de aprobación del ensayo:**

- [ ] `Configuración validada`
- [ ] Los 10 pasos recorridos sin excepción
- [ ] Checksums verificados: *n* artefactos
- [ ] `Contenido del paquete válido (… entradas, SPA incluido)`
- [ ] Modo resuelto correcto (`Install` en servidor nuevo)
- [ ] Sin avisos de MLLP abierto a `Any` — o aceptados formalmente
- [ ] El paso 04 reportó `Servidor PostgreSQL confirmado`

**Si el ensayo falla, deténgase.** Corrija y repita el ensayo. Un `-DryRun` que
falla es un `install` que fallará más caro.

### 7.3 Instalación

```powershell
.\install.ps1
```

Con `hub-install.psd1` presente el instalador **no pregunta campo por campo**:
valida, muestra el resumen y pide una confirmación (`s/N`).

Para instalación desatendida (automatización, ITSM):

```powershell
.\install.ps1 -NonInteractive
```

> `-NonInteractive` sin archivo de configuración falla listando de una vez todo lo
> que falta. Es el comportamiento deseado en pipelines.

### 7.4 Qué hace cada paso — referencia de seguimiento

| # | Paso | Modos | Qué observar |
|---|---|---|---|
| 01 | Prerrequisitos y validación del paquete | Install, Update, Repair | Elevación, SO, IIS, disco, checksums, contenido del zip, resolución del modo |
| 02 | ASP.NET Core Hosting Bundle | Install, Update | **Código 3010 = requiere reinicio del servidor** |
| 03 | Características de IIS | Install, Update, Repair | WebSockets es crítico: si falla, aborta |
| 04 | Conectividad y permisos de PostgreSQL | Install, Update | Nivel alcanzado: `protocolo` o `completa` |
| 05 | Despliegue del paquete | Install, Update | Ruta del respaldo — **anótela** |
| 06 | App pool y sitio IIS | Install, Update, Repair | Aviso de HTTP; recordatorio de HTTPS manual |
| 07 | Variables de entorno del app pool | Install, Update, Repair | Conservación o generación del `Jwt__SecretKey` |
| 08 | Directorios de datos | Install, Update, Repair | Validación de la ruta del key ring |
| 09 | Regla de firewall MLLP | Install, Update, Repair | Puerto y origen efectivos |
| 10 | Verificación post-instalación | Install, Update, Repair | `/health/live`, Data Protection, **creación y verificación del administrador** |

### 7.5 Creación automática del administrador inicial

El paso 10 crea y verifica la cuenta a partir de `AdminUsername` y
`AdminPassword` del `.psd1`. No hay que capturar ningún token ni ejecutar
llamadas manuales.

**Cómo funciona:**

1. El paso 07 escribe `EDGEGUARD_ADMIN_USERNAME` y `EDGEGUARD_ADMIN_PASSWORD`
   como variables del app pool.
2. Al arrancar, `AdminUserSeed` del Hub siembra la cuenta si no existe ninguna
   con ese nombre. Es idempotente.
3. El paso 10 **inicia sesión de verdad** contra `POST /api/auth/login` para
   comprobar que el operador podrá entrar al SPA.
4. Confirmada la cuenta, el paso 10 **retira `EDGEGUARD_ADMIN_USERNAME` y
   `EDGEGUARD_ADMIN_PASSWORD`** del app pool antes de terminar, para no dejar la
   contraseña del administrador en claro en `applicationHost.config`.

Salida esperada:

```
  ─────────────────────────────────────────────────────────────
   ADMINISTRADOR INICIAL
  ─────────────────────────────────────────────────────────────

   Usuario   admin
   Acceso    http://hub.local:80/
```

**Por qué se verifica con un inicio de sesión y no leyendo el log:** el sembrado
del Hub, ante una contraseña ausente o fuera de política, se limita a un
`LogWarning` y no crea la cuenta. El arranque parece correcto y `/health/live`
responde. Autenticarse es la única prueba de que hay con qué entrar.

**Política de contraseña** — el instalador la valida antes de empezar, así que un
valor inválido se detecta en el `-DryRun` y no al final: mínimo 8 caracteres, con
mayúscula, minúscula, dígito y un carácter especial.

**Si `AdminPassword` va vacía**, el paso 10 no falla, pero emite un aviso
explícito: en una instalación nueva no habrá con qué iniciar sesión. La
corrección es rellenarla y ejecutar `-Mode Repair`.

> **El instalador no cambia la contraseña de una cuenta que ya existe.** Si
> `AdminUsername` ya está en la base con otra contraseña, el paso 10 fallará al
> intentar autenticarse. Para cuentas existentes, el cambio se hace desde el SPA;
> si la contraseña se perdió, use `scripts\seed-admin.sql`.

> **El bootstrap token es otra cosa.** El token que sí existe en el sistema sirve
> para **registrar Edge Nodes**, no para crear administradores: lo genera un admin
> ya autenticado con `POST /api/nodes/bootstrap-tokens`, o el propio nodo con
> `POST /api/edge/token`. Se emite bajo demanda, nunca al arrancar. Ver
> `deployment-node.md`.

### 7.6 Registro de la ejecución

Todo queda en `log\install-<timestamp>.log`, creado **antes** del paso 01 para que
un fallo de prerrequisitos también quede registrado. Las contraseñas y el
`Jwt:SecretKey` se redactan. Adjunte este archivo al registro de cambio.

---

## 8. Fase T+0 — Verificación funcional

El paso 10 ya verificó automáticamente `/health/live`, la persistencia de Data
Protection y la cuenta de administrador. Lo que sigue es la validación independiente que
firma el responsable de la aplicación.

### 8.1 Estado de IIS

```powershell
Import-Module WebAdministration; Get-WebAppPoolState -Name EdgeGuardHub; Get-WebsiteState -Name EdgeGuard.Hub
```

Ambos deben reportar `Started`.

### 8.2 Configuración efectiva del app pool

```powershell
Import-Module WebAdministration; (Get-ItemProperty "IIS:\AppPools\EdgeGuardHub" -Name environmentVariables).Collection | Select-Object name, value
```

Verifique presencia (no el valor, que es secreto) de:

- [ ] `ASPNETCORE_ENVIRONMENT` = `Production`
- [ ] `EDGEGUARD_HUB_CONNECTIONSTRING` presente y no vacío
- [ ] `Jwt__SecretKey` presente, 64 caracteres
- [ ] `Jwt__Issuer` = `http://<HostHeader>`
- [ ] `DataProtection__KeyPath` presente y **fuera** de `InstallPath`
- [ ] `Diagnostics__Redaction__Mode` = `Strict`
- [ ] `Hl7Listener__Enabled` / `__Port` según lo configurado

```powershell
Import-Module WebAdministration; Get-ItemProperty "IIS:\AppPools\EdgeGuardHub" -Name startMode, processModel.idleTimeout, managedRuntimeVersion
```

- [ ] `startMode` = `AlwaysRunning`
- [ ] `idleTimeout` = `00:00:00`
- [ ] `managedRuntimeVersion` vacío (No Managed Code)

### 8.3 Salud de la aplicación

```powershell
$r = [System.Net.HttpWebRequest]::Create("http://localhost:80/health/live"); $r.Host = "hub.local"; $resp = $r.GetResponse(); (New-Object System.IO.StreamReader($resp.GetResponseStream())).ReadToEnd()
```

> La cabecera `Host` es imprescindible: el sitio está enlazado a un host header y
> el servidor no resolvería la petición sin ella. Es la misma técnica que usa el
> paso 10.

### 8.4 Verificación del SPA — el fallo silencioso

Abra `http://<HostHeader>/` en un navegador. **Debe cargar la interfaz de
usuario, no un JSON de la API.** Si aparece la API, el paquete se armó sin el SPA
(§5) — aunque el paso 01 debería haberlo impedido.

En la consola del navegador, confirme que la conexión SignalR se establece por
WebSocket y no cae a long polling. Si cae, revise WebSockets a nivel de sitio:

```powershell
Get-WebConfigurationProperty -PSPath "IIS:\Sites\EdgeGuard.Hub" -Filter "system.webServer/webSocket" -Name "enabled"
```

### 8.5 Data Protection — verificación independiente

```powershell
Get-ChildItem C:\inetpub\edgeguard\dp-keys -Filter 'key-*.xml'
```

Debe existir al menos un archivo. **Si el directorio está vacío y el log no
confirma la persistencia, no registre ningún nodo todavía:** en el siguiente
reciclaje del app pool los `SigningSecret` de todos los nodos dejarían de ser
descifrables. Ejecute `-Mode Repair` y vuelva a verificar.

### 8.6 Listener HL7 MLLP

```powershell
Get-NetFirewallRule -DisplayName 'EdgeGuard Hub - HL7 MLLP' | Get-NetFirewallPortFilter
Test-NetConnection -ComputerName localhost -Port 8001
```

- [ ] La regla existe, está habilitada y apunta al puerto configurado
- [ ] `RemoteAddress` es la subred del HIS/RIS, **no** `Any`
- [ ] El puerto acepta conexiones TCP

Pruebas funcionales de mensajería con `scripts\Test-Hl7Listener.ps1` y
`scripts\Test-Hl7ConcurrentLoad.ps1`.

### 8.7 Administrador inicial

El paso 10 ya creó la cuenta y comprobó el inicio de sesión (§7.5). Confirme de
forma independiente:

- [ ] Inicio de sesión correcto en el SPA con `AdminUsername`
- [ ] La contraseña se cambió tras el primer acceso
- [ ] Ninguna variable `EDGEGUARD_ADMIN_*` sigue en el app pool

```powershell
Import-Module WebAdministration; (Get-ItemProperty "IIS:\AppPools\EdgeGuardHub" -Name environmentVariables).Collection | Where-Object { $_.name -like 'EDGEGUARD_ADMIN*' } | Select-Object name
```

**No debe devolver nada.** El paso 10 retira `EDGEGUARD_ADMIN_USERNAME` y
`EDGEGUARD_ADMIN_PASSWORD` al confirmar la cuenta. Si alguna sigue ahí, el
instalador avisó de que no pudo retirarla: elimínela a mano desde IIS Manager,
con prioridad la contraseña, que es la del administrador en claro dentro de
`applicationHost.config`.

### 8.8 Punto de control C-3 — cierre de la instalación

- [ ] App pool y sitio en `Started`
- [ ] `/health/live` responde 2xx
- [ ] El SPA carga en el navegador
- [ ] SignalR negocia por WebSocket
- [ ] Variables de entorno completas y verificadas (§8.2)
- [ ] Data Protection persiste llaves en disco
- [ ] Regla de firewall MLLP correcta y acotada
- [ ] Administrador inicial creado y verificado por el paso 10
- [ ] Cuenta administradora creada y probada
- [ ] `installed.json` presente con la versión esperada
- [ ] Log del instalador adjunto al registro de cambio
- [ ] Ruta del respaldo anotada (si hubo instalación previa)
- [ ] Si el paso 02 devolvió 3010: reinicio del servidor programado

**Firma C-3 — Instalación:** ______________________  Fecha: __________

---

## 9. Fase T+1 — Endurecimiento post-instalación

El instalador entrega un sistema **funcional**, no **endurecido**. Estas tareas
son manuales y deben cerrarse antes de poner el Hub en servicio productivo.

### 9.1 HTTPS — obligatorio en producción

El instalador deja el sitio en HTTP por diseño. El binding se añade a mano y
sobrevive a las actualizaciones: el paso 06 no lo toca ni lo elimina.

1. Importe el certificado en el almacén `LocalMachine\My` (IIS Manager →
   *Server Certificates* → *Import*).
2. Añada el binding HTTPS al sitio en el puerto 443 con el host header
   correspondiente.
3. Configure la redirección HTTP → HTTPS.
4. **Actualice `Jwt__Issuer`** a `https://<HostHeader>` en las variables del app
   pool: el paso 07 lo escribe como `http://` porque el instalador solo configura
   HTTP.
5. Reinicie el app pool y repita §8.3 y §8.4 contra HTTPS.
6. Restrinja a TLS 1.2+ según la política de la organización.

> Los Edge Nodes se configuran con `HubBaseUrl` apuntando a HTTPS. Complete este
> apartado **antes** de registrar nodos.

### 9.2 Identidad del app pool

**El valor por defecto es `LocalSystem`, contra la recomendación del checklist de
producción de `deployment-hub.md`.** Con esa identidad, cualquier ejecución de
código dentro de `w3wp.exe` equivale a control total de la máquina.

Para usar una identidad de menor privilegio, configure `AppPoolIdentity` en
`hub-install.psd1` (`ApplicationPoolIdentity`, `NetworkService` o `LocalService`)
antes de instalar. En ese caso, conceda manualmente permisos de modificación
sobre:

| Directorio | Motivo |
|---|---|
| `<InstallPath>\logs` | Registro rotativo |
| `<InstallPath>\workspace\reports` | PDF referenciados por `Study.ReportPdfPath` |
| `<DataProtectionKeyPath>` | Key ring |

```powershell
icacls "C:\inetpub\EdgeGuard\Hub\logs" /grant "IIS AppPool\EdgeGuardHub:(OI)(CI)M" /T
icacls "C:\inetpub\EdgeGuard\Hub\workspace" /grant "IIS AppPool\EdgeGuardHub:(OI)(CI)M" /T
icacls "C:\inetpub\edgeguard\dp-keys" /grant "IIS AppPool\EdgeGuardHub:(OI)(CI)M" /T
```

> El paso 08 no hace trabajo de ACL porque con `LocalSystem` no hace falta. Si
> cambia la identidad, ese trabajo es suyo.

### 9.3 Acotar el listener MLLP

Si instaló con `Hl7RemoteAddress = 'Any'`, ciérrelo:

```powershell
Get-NetFirewallRule -DisplayName 'EdgeGuard Hub - HL7 MLLP' | Set-NetFirewallRule -RemoteAddress '10.20.30.0/24'
```

O ajuste `hub-install.psd1` y ejecute `.\install.ps1 -Mode Repair`, que es la vía
que deja el estado del servidor alineado con el archivo de configuración.

### 9.4 Banderas de despliegue coordinado

Dos banderas quedan en `$false` a propósito y se activan **solo cuando todos los
nodos ya salieron con la versión nueva**:

| Bandera | Efecto al activarse |
|---|---|
| `NodeAuthEnforce` | El Hub rechaza peticiones de nodos sin firma. Nodos antiguos dejan de comunicarse. |
| `Hl7ValidateBeforeAck` | El Hub valida el mensaje HL7 antes de emitir el ACK. Cambia el contrato con el HIS/RIS. |

Procedimiento de activación: modificar `hub-install.psd1` → `.\install.ps1 -Mode Repair`.

### 9.5 Higiene de secretos

- [ ] **Borrar `hub-install.psd1` del servidor** — contiene en texto plano la contraseña de PostgreSQL y la del administrador
- [ ] Limpiar la variable `EDGEGUARD_SETUP_DBPASSWORD` si se usó
- [ ] Archivar el log del instalador en un repositorio de acceso controlado

> Las variables `EDGEGUARD_ADMIN_USERNAME` y `EDGEGUARD_ADMIN_PASSWORD` **no
> aparecen en esta lista porque el paso 10 ya las retiró** del app pool al
> confirmar la cuenta. Solo hay algo que hacer si el instalador avisó de que no
> pudo retirarlas; en ese caso lo dice explícitamente y la comprobación está en
> §8.7.

### 9.6 Operación continua

| Tarea | Referencia |
|---|---|
| Respaldos de PostgreSQL y del key ring | `docs/07-operations/backup-recovery.md` |
| Monitoreo y alertas | `docs/07-operations/monitoring.md` |
| Migraciones controladas | `docs/07-operations/database-migrations.md` |
| Instalación de nodos | `docs/07-operations/deployment-node.md` |

> **El key ring entra en el plan de respaldo.** Perderlo obliga a que cada nodo
> registrado vuelva a autenticarse contra el Hub para que se le recomponga el
> `SigningSecret`.

---

## 10. Actualización de una instalación existente

### 10.1 Cómo decide el modo `Auto`

El instalador lee `installed.json` del directorio de instalación:

| Condición | Modo resuelto |
|---|---|
| No existe `installed.json` | `Install` |
| Existe y el `PackageHash` coincide con el paquete de `data\` | `Repair` |
| Existe con otro hash | `Update` |

> No se usa la versión del ensamblado: el `.csproj` no declara `<Version>` y
> `Dicom.Edge.Hub.Api.dll` siempre reporta `1.0.0.0`.

### 10.2 Qué preserva cada modo

| Modo | Binarios | `workspace\` | `logs\` | Key ring | `Jwt__SecretKey` | Base de datos |
|---|---|---|---|---|---|---|
| `Install` | Reemplaza | Preserva | Preserva | Preserva | Conserva si existe | Nunca toca |
| `Update` | Reemplaza | Preserva | Preserva | Preserva | **Conserva** | Nunca toca |
| `Repair` | **No toca** | Preserva | Preserva | Preserva | **Conserva** | **No toca** (omite el paso 04) |

> **El `Jwt__SecretKey` no se rota en las actualizaciones a propósito.** Rotarlo
> invalidaría todas las sesiones activas y sacaría a los usuarios del SPA sin
> explicación.

> **`workspace\` y `logs\` se preservan siempre.** `workspace\` guarda los PDF que
> `Study.ReportPdfPath` referencia; reemplazarlo dejaría los estudios existentes
> apuntando a archivos que ya no están. `logs\` conserva el rastro de arranque de
> la instalación previa, útil para diagnosticar una actualización que salió mal.

### 10.3 Procedimiento de actualización

1. **Ventana de cambio requerida.** El paso 05 detiene el app pool y espera hasta
   30 s a que `w3wp` libere los DLL. Hay interrupción de servicio.
2. Confirme que existe un respaldo reciente de la base de datos (DBA).
3. Deposite el nuevo `.zip` y actualice `checksums.sha256` en `data\`.
4. Retire el `.zip` de la versión anterior, o use `-PackagePath` explícito, para
   no depender de la selección por versión mayor.
5. Ensaye: `.\install.ps1 -DryRun` → confirme `Modo resuelto: Auto → Update`.
6. Ejecute: `.\install.ps1`
7. **Anote la ruta del respaldo** que reporta el paso 05
   (`<InstallPath>.backup-<timestamp>`).
8. Ejecute la verificación de §8 completa.
9. El respaldo se conserva; retírelo solo tras un periodo de estabilización
   acordado.

### 10.4 Cuándo usar `Repair`

`Repair` es para cuando alguien tocó IIS a mano y algo dejó de funcionar:
variables de entorno borradas al recrear el app pool, sitio apuntando a otra ruta,
directorio de `dp-keys` desaparecido, o para aplicar un cambio de configuración
(`Hl7RemoteAddress`, `NodeAuthEnforce`, `CorsAllowedOrigins`) sin redesplegar
binarios.

```powershell
.\install.ps1 -Mode Repair
```

Omite los pasos 02, 04 y 05: no toca el Hosting Bundle, ni la base, ni los
binarios.

---

## 11. Reversión y desinstalación

### 11.1 Reversión automática

Si un paso **del 05 en adelante** falla, el instalador:

1. Restaura el respaldo `<InstallPath>.backup-<timestamp>`.
2. Vuelve a levantar el app pool anterior.
3. Conserva el respaldo.

Antes del paso 05 no hay nada que revertir: no se había tocado la instalación.

> **Si la reversión falla**, el log lo dice explícitamente y el respaldo íntegro
> sigue en su sitio. Restáurelo a mano copiando el contenido del directorio de
> respaldo sobre `InstallPath` y arrancando el app pool.

### 11.2 Reversión manual de una actualización

```powershell
Import-Module WebAdministration
Stop-WebAppPool -Name EdgeGuardHub
Remove-Item -Path "C:\inetpub\EdgeGuard\Hub\*" -Recurse -Force -Exclude 'workspace','logs'
Copy-Item -Path "C:\inetpub\EdgeGuard\Hub.backup-20260828-143000\*" -Destination "C:\inetpub\EdgeGuard\Hub" -Recurse -Force
Start-WebAppPool -Name EdgeGuardHub
```

> **Advertencia sobre migraciones.** El Hub aplica migraciones de EF Core al
> arrancar y no las revierte. Volver a una versión anterior de binarios sobre una
> base ya migrada puede no ser compatible. Consulte
> `docs/07-operations/database-migrations.md` y coordine con el DBA antes de
> revertir una actualización que incluyó migraciones.

### 11.3 Desinstalación

```powershell
.\bin\Uninstall-Hub.ps1 -DryRun
.\bin\Uninstall-Hub.ps1
```

Pide escribir `DESINSTALAR` como confirmación. Elimina el sitio, el app pool, la
regla de firewall MLLP y los binarios.

**Preserva por defecto:** el key ring de Data Protection, `workspace\` y `logs\`.

`-Purge` los elimina también. **Es irreversible y tiene consecuencias en cascada:**
sin el key ring, los `SigningSecret` de todos los nodos registrados dejan de ser
descifrables y cada nodo tiene que volver a autenticarse para que se le recomponga.

**La base de datos nunca se toca.** Los datos clínicos no son del instalador.

---

## 12. Diagnóstico de fallos

### 12.1 Por paso

| Síntoma | Causa | Acción |
|---|---|---|
| **01** — «requiere una consola elevada» | Sesión sin privilegios | Abra PowerShell como administrador |
| **01** — «no contiene wwwroot/index.html» | El paquete se publicó sin el SPA | `npm run build` **antes** de `dotnet publish`; rearme el zip (§5) |
| **01** — «Checksum incorrecto» | Zip corrupto o distinto del declarado | Vuelva a copiarlo y regenere `checksums.sha256` |
| **01** — «Espacio insuficiente» | <5 GB libres | Libere espacio o cambie `InstallPath` |
| **01** — «checksums.sha256 no verificó ningún archivo» | Los nombres no coinciden con los de `data\` | Corrija los nombres en el archivo de checksums |
| **02** — «Falta el Hosting Bundle» | No está instalado ni depositado en `data\` | Deposite el instalador en `data\`. La descarga automática exige fijar URL y hash en `bin\02-Install-HostingBundle.ps1` |
| **02** — código 3010 | Instalado correctamente, requiere reinicio | Programe el reinicio antes de poner el Hub en servicio |
| **03** — no se pudo habilitar WebSockets | Característica bloqueada o imagen recortada | Bloqueante: SignalR no negocia sin ella. Habilítela manualmente y repita |
| **04** — `3D000` | La base no existe | El instalador no la crea. Créela (§4.3) |
| **04** — `28000` | El rol no existe o `pg_hba.conf` rechaza la conexión | Coordine con el DBA |
| **04** — `28P01` | Contraseña incorrecta | Corrija la credencial |
| **04** — «no tiene permiso CREATE» | Falta el `GRANT` | `GRANT CREATE ON SCHEMA public TO <rol>;` |
| **04** — «psql no está disponible» | Cliente ausente | **No es un error.** La validación se difiere al paso 10 |
| **05** — descompresión falla por archivos bloqueados | `w3wp` no soltó los DLL | Detenga el app pool a mano y reintente |
| **06** — «El módulo WebAdministration no está disponible» | Falta la consola de administración de IIS | Revise el resultado del paso 03 |
| **07** — «La variable X no quedó escrita» | Fallo al escribir en la configuración de IIS | Verifique permisos sobre `applicationHost.config` |
| **08** — «key ring dentro del directorio de instalación» | `DataProtectionKeyPath` mal configurado | Muévalo fuera de `InstallPath` |
| **10** — llaves de Data Protection **EFÍMERAS** | `DataProtection__KeyPath` no llegó al proceso | **Grave.** Revise las variables del app pool y ejecute `-Mode Repair` antes de registrar nodos |
| **10** — «no respondió en /health/live» | Arranque fallido | Revise `<InstallPath>\logs`. Causa más común: credencial de PostgreSQL incorrecta cuando el paso 04 no pudo validarla |
| **10** — «respondió 404 en /health/live» | El proceso está en pie, falta la ruta | El paquete desplegado no registra los endpoints de diagnóstico (`MapDiagnosticsEndpoints`) o es anterior a este instalador. No se reintenta: un 404 no es transitorio |
| **10** — `/health/ready` HTTP 503 | Algún check con tag `ready` no está verde | **Informativo, no reprueba la instalación.** El cuerpo dice cuál: `database`, `storage` o `hl7-listener`. Revíselo antes de poner el Hub en servicio |

### 12.2 Fallos posteriores al despliegue

| Síntoma | Diagnóstico |
|---|---|
| El sitio devuelve **500.19** en cada petición | Está el runtime de ASP.NET Core pero falta `AspNetCoreModuleV2`. Reinstale el Hosting Bundle completo (paso 02 lo detecta) |
| El navegador muestra JSON en lugar del SPA | El paquete se armó sin front (§5) |
| El monitoreo en tiempo real está mudo, el resto funciona | WebSockets deshabilitado a nivel de sitio (§8.4) |
| **El HIS/RIS deja de recibir ACK tras un rato de inactividad** | El app pool se apagó por `idleTimeout`. Verifique `AlwaysRunning` e `idleTimeout=00:00:00` (§8.2). Repare con `-Mode Repair` |
| Los usuarios pierden la sesión tras una actualización | El `Jwt__SecretKey` se regeneró. No debería ocurrir: el paso 07 lo conserva. Revise si el app pool fue recreado a mano |
| Los nodos empiezan a fallar la autenticación tras un reciclaje | Llaves de Data Protection efímeras o key ring perdido (§8.5, §11.3) |
| El paso 10 falla: «la cuenta de administrador no pudo iniciar sesión» | La cuenta ya existía con otra contraseña, o el sembrado se omitió. El mensaje incluye el `Admin seed skipped` que reportó el Hub |
| Instalación correcta pero no se puede entrar al SPA | `AdminPassword` iba vacía. Rellénela y ejecute `-Mode Repair` |
| Se perdió la contraseña del administrador | El instalador no la cambia en cuentas existentes. Cámbiela desde el SPA, o dé de alta la cuenta con `scripts\seed-admin.sql` |

### 12.3 Recolección de evidencia para escalar

```powershell
$stamp = Get-Date -Format yyyyMMdd-HHmmss
$out = "C:\Temp\edgeguard-diag-$stamp"; New-Item -ItemType Directory -Force -Path $out | Out-Null
Copy-Item "C:\Deploy\EdgeGuard\hub\log\*.log" $out -Force
Copy-Item "C:\inetpub\EdgeGuard\Hub\logs\*.log" $out -Force
Copy-Item "C:\inetpub\EdgeGuard\Hub\installed.json" $out -Force
Get-EventLog -LogName Application -Newest 200 | Export-Csv "$out\app-eventlog.csv" -NoTypeInformation
Compress-Archive -Path "$out\*" -DestinationPath "$out.zip"
```

> **Revise el paquete antes de enviarlo.** Los logs del instalador redactan
> secretos, pero los logs de la aplicación pueden contener PHI si
> `Diagnostics__Redaction__Mode` no es `Strict`.

---

## 13. Apéndices

### Apéndice A — Configuración → variables de entorno

| Clave de `hub-install.psd1` | Variable del app pool |
|---|---|
| `DbHost` / `DbPort` / `DbName` / `DbUser` + contraseña | `EDGEGUARD_HUB_CONNECTIONSTRING` |
| *(generada por el instalador, 64 caracteres)* | `Jwt__SecretKey` |
| `HostHeader` | `Jwt__Issuer` (como `http://<HostHeader>`) |
| `AdminUsername` | `EDGEGUARD_ADMIN_USERNAME` (en minúsculas) — **el paso 10 la retira** |
| `AdminPassword` | `EDGEGUARD_ADMIN_PASSWORD` — **el paso 10 la retira** tras confirmar la cuenta |
| `DataProtectionKeyPath` | `DataProtection__KeyPath` |
| `InstanceId` | `Diagnostics__InstanceId` |
| `RedactionMode` | `Diagnostics__Redaction__Mode` |
| `CorsAllowedOrigins` | `Cors__AllowedOrigins__0`, `__1`, … |
| `Hl7Enabled` / `Hl7Port` | `Hl7Listener__Enabled` / `Hl7Listener__Port` |
| `Hl7ValidateBeforeAck` | `Hl7Listener__ValidateBeforeAck` |
| `NodeAuthEnforce` | `NodeAuth__Enforce` |
| — | `ASPNETCORE_ENVIRONMENT` = `Production` |

Consumidas solo por el instalador, nunca llegan a la aplicación: `PackagePath`,
`InstallPath`, `SiteName`, `AppPoolName`, `AppPoolIdentity`, `Hl7RemoteAddress`,
`AllowHostingBundleDownload`.

> Los nombres están tomados del código, no de la guía de despliegue: son
> `EDGEGUARD_HUB_CONNECTIONSTRING` (no `HUB_DB_CONNECTION_STRING`) y
> `Jwt__SecretKey` (no `Jwt__Secret`).

### Apéndice B — Parámetros de `install.ps1`

| Parámetro | Uso |
|---|---|
| `-ConfigFile <ruta>` | Archivo de configuración alterno. Por defecto busca `hub-install.psd1` junto al script |
| `-Mode Auto\|Install\|Update\|Repair` | Modo de operación. Default `Auto` |
| `-NonInteractive` | Sin preguntas. Exige configuración completa; si falta algo, aborta listándolo todo |
| `-DryRun` | Simula el recorrido completo sin escribir nada |
| `-AllowHostingBundleDownload` | Permite descargar el bundle (exige URL y hash fijados en el paso 02) |
| `-PackagePath` | Fuerza un paquete específico |
| `-InstallPath`, `-SiteName`, `-AppPoolName`, `-HostHeader`, `-Port` | Sobreescrituras puntuales, ganan sobre el archivo |
| `-DbHost`, `-DbPort`, `-DbName`, `-DbUser` | Sobreescrituras de base de datos |
| `-DbCredential <PSCredential>` | Credencial de PostgreSQL — máxima precedencia |

**Códigos de salida:** `0` correcto · `1` abortado con error · `2` cancelado por el operador.

### Apéndice C — Precedencia de la contraseña de PostgreSQL

De mayor a menor:

1. `-DbCredential [PSCredential]`
2. Variable de entorno `EDGEGUARD_SETUP_DBPASSWORD`
3. `DbPassword` en `hub-install.psd1`
4. Pregunta interactiva
5. En `-NonInteractive`: falla indicándolo

**Recomendación para entornos con separación de funciones** — evita escribir la
contraseña en disco:

```powershell
$cred = Get-Credential -UserName edgeguard -Message "PostgreSQL"
.\install.ps1 -DbCredential $cred -NonInteractive
```

O, en un pipeline que la obtiene de una bóveda:

```powershell
$env:EDGEGUARD_SETUP_DBPASSWORD = (Get-SecretFromVault); .\install.ps1 -NonInteractive; Remove-Item Env:\EDGEGUARD_SETUP_DBPASSWORD
```

### Apéndice D — Comportamiento interactivo

| Situación | Comportamiento |
|---|---|
| Sin `hub-install.psd1` | Pregunta campo por campo, muestra resumen, confirma |
| Con `hub-install.psd1` | Solo valida, muestra el resumen y confirma |
| Con archivo + `-NonInteractive` | Valida y ejecuta sin confirmar |
| Sin archivo + `-NonInteractive` | Falla listando de una vez todo lo que falta |

### Apéndice E — Rutas y artefactos

| Ruta | Contenido | Sobrevive a actualizaciones |
|---|---|---|
| `C:\inetpub\EdgeGuard\Hub\` | Binarios + SPA | No — se reemplaza |
| `…\Hub\logs\` | Registro rotativo del Hub | **Sí** |
| `…\Hub\workspace\reports\` | PDF referenciados por `Study.ReportPdfPath` | **Sí** |
| `…\Hub\installed.json` | Manifiesto: versión, hash, fecha, operador, máquina | No — se reescribe |
| `C:\inetpub\edgeguard\dp-keys\` | Key ring de Data Protection | **Sí** — fuera de `InstallPath` |
| `<InstallPath>.backup-<timestamp>` | Respaldo de la instalación previa | Se conserva |
| `setup\hub\log\install-<timestamp>.log` | Registro del instalador (secretos redactados) | Se acumula |

### Apéndice F — Checklist maestro de un vistazo

**T-7 · Build**
- [ ] `npm ci && npm run build` en `src\frontend\dicomedge-ui`
- [ ] `dotnet publish` del backend
- [ ] Zip armado y `checksums.sha256` generado
- [ ] Contenido verificado: `wwwroot/index.html`, `Hub.Api.dll`, `web.config`, `Npgsql.dll`
- [ ] Firma C-1

**T-7 · Externos**
- [ ] Rol y base de PostgreSQL creados con `GRANT CREATE`
- [ ] `pg_hba.conf` admite la IP del Hub
- [ ] Certificado TLS emitido
- [ ] Subred del HIS/RIS documentada
- [ ] Ventana de cambio aprobada

**T-1 · Servidor**
- [ ] Árbol `setup\hub\` copiado
- [ ] Artefactos en `data\` (zip, checksums, hosting bundle)
- [ ] `hub-install.psd1` creado y revisado clave por clave
- [ ] `AdminUsername` y `AdminPassword` definidos y conformes a la política
- [ ] ≥5 GB libres en el volumen de instalación
- [ ] Firma C-2

**T-0 · Ejecución**
- [ ] Consola elevada
- [ ] `-DryRun` ejecutado y aprobado
- [ ] Instalación ejecutada
- [ ] **Administrador inicial creado y verificado** (§7.5)
- [ ] Log del instalador archivado
- [ ] Ruta del respaldo anotada

**T+0 · Verificación**
- [ ] App pool y sitio `Started`
- [ ] `/health/live` responde 2xx
- [ ] SPA carga en el navegador
- [ ] SignalR por WebSocket
- [ ] Variables del app pool completas
- [ ] Key ring con llaves en disco
- [ ] Regla MLLP correcta y acotada
- [ ] Inicio de sesión correcto en el SPA con el administrador
- [ ] Ninguna variable `EDGEGUARD_ADMIN_*` sigue en el app pool
- [ ] Firma C-3

**T+1 · Endurecimiento**
- [ ] HTTPS configurado y `Jwt__Issuer` actualizado a `https://`
- [ ] Identidad del app pool revisada (§9.2)
- [ ] MLLP acotado a la subred del HIS/RIS
- [ ] **`hub-install.psd1` borrado del servidor**
- [ ] Respaldos configurados, incluido el key ring
- [ ] Monitoreo y alertas activos
- [ ] Reinicio pendiente ejecutado si el paso 02 devolvió 3010

---

## Control del documento

| Campo | Valor |
|---|---|
| Fuente | `setup\hub\` (instalador y sus 10 pasos), `docs\02-getting-started\prerequisites.md`, `docs\07-operations\deployment-hub.md` |
| Documentos relacionados | `deployment-node.md`, `backup-recovery.md`, `database-migrations.md`, `monitoring.md`, `troubleshooting.md` |
| Revisión | 1.0 — 2026-08-28 |

> **Mantenimiento.** Este runbook describe el comportamiento del instalador tal
> como está implementado. Un cambio en `setup\hub\bin\*.ps1` —un paso nuevo, una
> variable de entorno distinta, otra validación— obliga a revisar los apartados
> §7.4, §9 y el Apéndice A.
