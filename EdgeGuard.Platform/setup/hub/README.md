# Instalador del Hub EdgeGuard

Despliega el Hub sobre Windows Server + IIS a partir de un paquete ya compilado.
No compila nada, no instala PostgreSQL y no emite certificados.

## Requisitos

| Componente | Versión |
|---|---|
| Windows Server | 2019 / 2022 |
| PowerShell | 5.1 (el que trae el sistema) |
| ASP.NET Core Hosting Bundle | 10.0 — lo instala el paso 02 |
| PostgreSQL | 15 / 16, **con el rol y la base ya creados** |

El instalador **no crea el rol ni la base**: solo verifica que existan, que las
credenciales sirvan y que el rol tenga `CREATE` en el esquema `public`, porque
el Hub aplica las migraciones de EF al arrancar.

## Uso rápido

```powershell
# 1. Copiar la carpeta al servidor y depositar el paquete
#    setup\hub\data\edgeguard-hub-1.2.0.zip  (ver data\README.md)

# 2. Configurar
Copy-Item hub-install.example.psd1 hub-install.psd1
notepad hub-install.psd1

# 3. Ensayo sin escribir nada — recomendado antes de producción
.\install.ps1 -DryRun

# 4. Instalar
.\install.ps1
```

Consola **elevada**. El paso 01 aborta si no lo está.

## Modos

`-Mode` acepta cuatro valores; el default es `Auto`.

| Modo | Qué hace | Qué preserva |
|---|---|---|
| `Install` | Instalación nueva completa | — |
| `Update` | Despliega binarios nuevos y reaplica configuración | `workspace\`, `logs\`, `dp-keys\` y el `Jwt:SecretKey` existente |
| `Repair` | Reaplica configuración sin tocar binarios ni base | Todo lo anterior más los binarios |
| `Auto` | Detecta el estado y elige | — |

`Auto` decide leyendo `installed.json` del directorio de instalación: sin
manifiesto → `Install`; con el mismo hash de paquete → `Repair`; con otro →
`Update`. No se usa la versión del ensamblado porque el `.csproj` no declara
`<Version>` y siempre reporta `1.0.0.0`.

`Repair` es para cuando alguien tocó IIS a mano y algo dejó de funcionar:
variables borradas al recrear el app pool, sitio apuntando a otra ruta,
`dp-keys` desaparecida.

## Interactivo y desatendido

| Situación | Comportamiento |
|---|---|
| Sin `hub-install.psd1` | Pregunta campo por campo, resumen, confirma |
| Con `hub-install.psd1` | Solo valida, muestra el resumen y confirma |
| Con archivo + `-NonInteractive` | Valida y ejecuta sin confirmar |
| Sin archivo + `-NonInteractive` | Falla listando de una vez todo lo que falta |

`-DryRun` simula el recorrido completo. Las verificaciones de solo lectura
—checksums, contenido del paquete, conexión a PostgreSQL— se ejecutan de verdad;
nada se escribe.

## Contraseña de PostgreSQL

Precedencia, de mayor a menor:

1. `-DbCredential [PSCredential]`
2. Variable de entorno `EDGEGUARD_SETUP_DBPASSWORD`
3. `DbPassword` en `hub-install.psd1`
4. Pregunta interactiva
5. En `-NonInteractive`, falla indicándolo

`hub-install.psd1` contiene la contraseña en texto plano. Está en `.gitignore`,
pero **bórralo del servidor al terminar**.

## Administrador inicial

`AdminUsername` y `AdminPassword` del `.psd1` crean la cuenta con la que se entra
al SPA. **Sin `AdminPassword` no se crea ninguna cuenta**, y en una instalación
nueva eso significa un Hub sano al que nadie puede entrar.

El paso 07 las escribe como variables del app pool; `AdminUserSeed` siembra la
cuenta al arrancar; el paso 10 comprueba el resultado iniciando sesión de verdad
contra `/api/auth/login` y, confirmada la cuenta, retira ambas variables del app
pool antes de terminar.

La contraseña debe cumplir la política del Hub —8 caracteres o más, con
mayúscula, minúscula, dígito y carácter especial—, y el instalador la valida
antes de empezar: el Hub, ante una que no la cumple, se limita a un `LogWarning`
y no crea la cuenta.

**El instalador no cambia la contraseña de una cuenta que ya existe.** Eso se
hace desde el SPA; si se perdió, `scripts\seed-admin.sql`.

No confundir con el **bootstrap token**, que sirve para registrar nodos y se
emite bajo demanda desde `POST /api/nodes/bootstrap-tokens`.

## Configuración → variables de entorno

Todo se escribe como variables de entorno del app pool.
`appsettings.Production.json` va vacío a propósito, de modo que el estado vive
en IIS y una actualización nunca tiene que fusionar archivos de configuración.

| Clave del `.psd1` | Variable |
|---|---|
| `DbHost`/`DbPort`/`DbName`/`DbUser` + contraseña | `EDGEGUARD_HUB_CONNECTIONSTRING` |
| *(generada por el instalador)* | `Jwt__SecretKey` |
| `HostHeader` | `Jwt__Issuer` |
| `AdminUsername` | `EDGEGUARD_ADMIN_USERNAME` — retirada por el paso 10 |
| `AdminPassword` | `EDGEGUARD_ADMIN_PASSWORD` — retirada por el paso 10 |
| `DataProtectionKeyPath` | `DataProtection__KeyPath` |
| `InstanceId` | `Diagnostics__InstanceId` |
| `RedactionMode` | `Diagnostics__Redaction__Mode` |
| `CorsAllowedOrigins` | `Cors__AllowedOrigins__0`, `__1`, … |
| `Hl7Enabled` / `Hl7Port` | `Hl7Listener__Enabled` / `__Port` |
| `Hl7ValidateBeforeAck` | `Hl7Listener__ValidateBeforeAck` |
| `NodeAuthEnforce` | `NodeAuth__Enforce` |
| — | `ASPNETCORE_ENVIRONMENT=Production` |

`PackagePath`, `InstallPath`, `SiteName`, `AppPoolName`, `AppPoolIdentity`,
`Hl7RemoteAddress` y `AllowHostingBundleDownload` los consume el instalador y no
llegan a la aplicación.

## Los diez pasos

| # | Hace |
|---|---|
| 01 | Elevación, sistema operativo, IIS, disco, checksums, contenido del paquete, resolución del modo |
| 02 | ASP.NET Core Hosting Bundle |
| 03 | Características de IIS, incluido WebSockets |
| 04 | Puerto, protocolo y permisos de PostgreSQL |
| 05 | Respaldo, despliegue e `installed.json` |
| 06 | App pool y sitio (HTTP) |
| 07 | Variables de entorno del app pool |
| 08 | `logs\`, `workspace\reports\` y el key ring |
| 09 | Regla de firewall del listener MLLP |
| 10 | Arranque, `/health`, Data Protection y administrador inicial |

## Cuando algo falla

**01 — «requiere una consola elevada».** Abre PowerShell como administrador.

**01 — «no contiene wwwroot/index.html».** El paquete se publicó sin el SPA.
`npm run build` tiene que correr **antes** de `dotnet publish`: `angular.json`
escribe su salida directamente en `Hub.Api\wwwroot` y el `.csproj` no dispara
ese build. Ver `data\README.md`.

**01 — «Checksum incorrecto».** El zip está corrupto o no es el declarado.
Vuelve a copiarlo y regenera `checksums.sha256`.

**02 — «Falta el Hosting Bundle».** Deposita el instalador en `data\`. La
descarga automática requiere fijar URL y hash en
`bin\02-Install-HostingBundle.ps1`, que se dejan vacíos a propósito: un hash de
relleno convertiría la verificación en un trámite que siempre pasa.

**04 — «no tiene permiso CREATE».** `GRANT CREATE ON SCHEMA public TO <rol>;`

**04 — «psql no está disponible».** No es un error. Sin psql no se puede
verificar aquí la contraseña ni los permisos; ambos se comprueban en el paso 10.

**10 — «llaves de Data Protection EFÍMERAS».** `DataProtection__KeyPath` no
llegó al proceso. Es grave: en el siguiente reciclaje del app pool los
`SigningSecret` de todos los nodos dejan de ser descifrables. Revisa las
variables del app pool y ejecuta `-Mode Repair`.

**10 — «no respondió en /health».** Revisa `<InstallPath>\logs`. La causa más
común es una credencial de PostgreSQL incorrecta cuando el paso 04 no pudo
validarla.

**10 — «la cuenta de administrador no pudo iniciar sesión».** O la cuenta ya
existía con otra contraseña —el instalador no la cambia—, o el Hub omitió el
sembrado. El mensaje incluye el `Admin seed skipped` que el Hub escribió en su
log, que dice la causa. Para una cuenta existente cuya contraseña se perdió, usa
`scripts\seed-admin.sql`.

**Se instaló sin `AdminPassword` y no hay con qué entrar.** El paso 10 lo avisa
en vez de fallar. Rellena `AdminPassword` en el `.psd1` y ejecuta
`.\install.ps1 -Mode Repair`.

## Reversión y desinstalación

Si algo falla del paso 05 en adelante, el instalador restaura el respaldo
—`<InstallPath>.backup-<timestamp>`— y vuelve a levantar el app pool anterior.
El respaldo se conserva.

```powershell
.\bin\Uninstall-Hub.ps1 -DryRun    # ver qué se eliminaría
.\bin\Uninstall-Hub.ps1            # pide escribir DESINSTALAR
```

Preserva por defecto el key ring, `workspace\` y `logs\`. `-Purge` los elimina
también. **La base de datos nunca se toca.**

Cuidado con `-Purge`: sin el key ring, los `SigningSecret` de todos los nodos
registrados dejan de ser descifrables y cada nodo tiene que volver a
autenticarse para que se le recomponga.

## Notas de diseño

**Solo HTTP.** Para HTTPS, añade el binding y el certificado en IIS Manager
después de instalar; el instalador no los toca ni los elimina.

**App pool en LocalSystem.** Es lo configurado, contra la recomendación del
checklist de producción de `docs/07-operations/deployment-hub.md`. Con esa
identidad, cualquier ejecución de código dentro de `w3wp.exe` es control total
de la máquina. `-AppPoolIdentity` permite cambiarlo sin tocar el script.

**Firewall MLLP en `Any` por defecto.** El puerto 8001 es TCP crudo, sin TLS ni
autenticación, y transporta PHI. `Hl7RemoteAddress` lo acota a la subred del
HIS/RIS.

**`AlwaysRunning` e `idleTimeout` en cero no son afinación.** El listener MLLP
vive dentro del proceso del Hub; con la configuración por defecto IIS apaga el
proceso tras 20 minutos sin peticiones HTTP y el listener deja de aceptar
mensajes del HIS sin que nadie se entere.

**El key ring va fuera del directorio de instalación.** El paso 05 reemplaza ese
directorio en cada actualización. El instalador aborta si la ruta cae dentro.

## Registro

`log\install-<timestamp>.log`, creado antes del paso 01 para que un fallo de
prerrequisitos también quede registrado. Las contraseñas y el `Jwt:SecretKey` se
redactan. En `-DryRun` se escribe igual, marcado como simulación.
