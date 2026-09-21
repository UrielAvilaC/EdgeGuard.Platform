## 3. Respaldo y recuperación

Un respaldo sirve el día que hace falta, no el día que se configura. Este
apartado dice qué copiar, cada cuánto, cómo comprobar que la copia sirve y qué
hacer el día que hay que usarla.

### 3.1 Qué se respalda

| Qué | Dónde vive | ¿Se respalda? | Cada cuánto |
|---|---|---|---|
| Base de datos del Hub | Servidor PostgreSQL, base `edgeguard_hub` | **Sí, crítico** | Diario |
| Carpeta de llaves (key ring) | `C:\inetpub\edgeguard\dp-keys` | **Sí, crítico** | Semanal, y después de cada instalación o actualización |
| Configuración del sitio y del app pool | `C:\Windows\System32\inetsrv\config\applicationHost.config` | **Sí** | Después de cada cambio |
| Certificado TLS | Almacén de certificados de Windows | Sí, si se configuró HTTPS | Al renovarlo |
| Registros de operación | `C:\inetpub\EdgeGuard\Hub\logs\` | Opcional | Según su política |
| Paquete del instalador | El que entregó el proveedor | Consérvelo | — |

**Lo que no se respalda, y por qué:**

- **Las imágenes.** No pasan por el Hub ni se guardan en él. Viajan del equipo
  al nodo y del nodo al PACS. El respaldo de las imágenes es el respaldo del
  PACS, y es responsabilidad de quien lo opere.
- **Los archivos instalados del Hub.** Se reponen ejecutando el instalador; no
  tiene sentido copiarlos.
- **`hub-install.psd1`.** Ese archivo se borra del servidor al terminar la
  instalación porque contiene la contraseña de la base en texto plano. No entra
  en ningún respaldo.

> **Las dos cosas críticas son la base de datos y la carpeta de llaves.** Con
> esas dos y el paquete del instalador, el Hub se rehace completo. Sin la
> carpeta de llaves, se rehace pero cada nodo registrado tiene que volver a
> autenticarse antes de poder trabajar.

### 3.2 Cuánto se puede perder y cuánto tarda volver

| Compromiso | Valor con el esquema de este manual |
|---|---|
| Cuánta información se pierde en el peor caso | Lo del día en curso, hasta 24 horas |
| Cuánto tarda el servicio en volver | Menos de una hora, con el paquete y los respaldos a la mano |

Si su operación no tolera perder 24 horas de información, el camino es una
réplica continua de PostgreSQL a un segundo servidor. Es un cambio de
arquitectura, no un ajuste: coordínelo con el proveedor.

### 3.3 Respaldo diario de la base de datos

**Cuándo:** todos los días, en automático, a una hora de baja actividad.

El respaldo se toma con `pg_dump`, la herramienta de PostgreSQL. No interrumpe
el servicio: el Hub sigue operando normalmente mientras corre.

**Paso 1 — Prepare la contraseña sin escribirla en el script.**

Guárdela como variable de entorno del equipo, una sola vez, en una consola
elevada:

```powershell
[Environment]::SetEnvironmentVariable("EDGEGUARD_BACKUP_PGPASSWORD", "<contraseña del rol edgeguard>", "Machine")
```

Así la contraseña no queda en un archivo que se copia, se comparte o se sube a
un repositorio por accidente.

**Paso 2 — Cree el script** en `C:\EdgeGuard\Scripts\Respaldo-BaseHub.ps1`:

```powershell
$ErrorActionPreference = 'Stop'

$Destino     = 'C:\Backups\EdgeGuard\Hub'
$Base        = 'edgeguard_hub'
$Usuario     = 'edgeguard'
$Servidor    = 'localhost'
$DiasRetener = 14
$PgDump      = 'C:\Program Files\PostgreSQL\16\bin\pg_dump.exe'

New-Item -ItemType Directory -Force -Path $Destino | Out-Null

$Marca   = Get-Date -Format 'yyyyMMdd_HHmmss'
$Archivo = Join-Path $Destino "${Base}_${Marca}.dump"

$env:PGPASSWORD = [Environment]::GetEnvironmentVariable('EDGEGUARD_BACKUP_PGPASSWORD', 'Machine')

& $PgDump --host=$Servidor --port=5432 --username=$Usuario `
          --format=custom --compress=9 --file=$Archivo $Base

if ($LASTEXITCODE -ne 0) { throw "pg_dump devolvio el codigo $LASTEXITCODE" }

$Mb = [math]::Round((Get-Item $Archivo).Length / 1MB, 2)
Write-Host "$(Get-Date -Format o)  Respaldo listo: $Archivo ($Mb MB)"

Get-ChildItem $Destino -Filter '*.dump' |
    Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-$DiasRetener) } |
    Remove-Item -Force
```

Ajuste `$Servidor` si PostgreSQL no está en el mismo equipo que el Hub, y la
ruta de `$PgDump` a la versión instalada.

**Paso 3 — Pruébelo a mano antes de programarlo.**

```powershell
& C:\EdgeGuard\Scripts\Respaldo-BaseHub.ps1
```

Debe terminar imprimiendo la ruta del archivo y su tamaño en MB. Un respaldo de
unos pocos KB no es un respaldo: revise el mensaje de error.

**Paso 4 — Prográmelo a diario.**

```powershell
$Accion  = New-ScheduledTaskAction -Execute 'powershell.exe' `
             -Argument '-NoProfile -ExecutionPolicy Bypass -File C:\EdgeGuard\Scripts\Respaldo-BaseHub.ps1'
$Disparo = New-ScheduledTaskTrigger -Daily -At 02:00
Register-ScheduledTask -TaskName 'EdgeGuard - Respaldo base del Hub' `
    -Action $Accion -Trigger $Disparo -User 'SYSTEM' -RunLevel Highest
```

**Paso 5 — Sáquelo del servidor.** Un respaldo que vive en el mismo disco que la
base no protege del único escenario que importa de verdad, que es perder el
servidor. Cópielo a otro almacenamiento: unidad de red, cinta o nube, según su
política.

### 3.4 Respaldo del key ring y de la configuración

**Cuándo:** semanalmente, y **siempre después de instalar o actualizar el Hub**.

```powershell
$Fecha  = Get-Date -Format yyyyMMdd
$Destino = 'C:\Backups\EdgeGuard\Hub'
New-Item -ItemType Directory -Force -Path $Destino | Out-Null

Compress-Archive -Force `
  -Path 'C:\inetpub\edgeguard\dp-keys\*', `
        'C:\Windows\System32\inetsrv\config\applicationHost.config' `
  -DestinationPath "$Destino\llaves-y-configuracion-$Fecha.zip"
```

**Qué acaba de copiar, y por qué importa que se trate como un secreto:** ese
archivo contiene las llaves de cifrado del Hub y, dentro de la configuración de
IIS, la cadena de conexión a la base de datos y el secreto de firma de sesiones.
Guárdelo donde guarda las credenciales, no donde guarda los informes.

**Qué debe ver:** el `.zip` creado, con al menos un archivo `key-*.xml` dentro.
Compruébelo:

```powershell
(Get-Item "$Destino\llaves-y-configuracion-$Fecha.zip").Length / 1KB
```

### 3.5 Comprobar que el respaldo sirve

**Cuándo:** una vez al mes. Anótelo en su bitácora de operación.

Un respaldo que nunca se ha restaurado es una suposición. La prueba consiste en
restaurarlo sobre una base temporal, contar unos cuantos registros y borrarla.
**No toca la base de producción en ningún momento.**

```powershell
$psql      = 'C:\Program Files\PostgreSQL\16\bin\psql.exe'
$pgRestore = 'C:\Program Files\PostgreSQL\16\bin\pg_restore.exe'
$Respaldo  = 'C:\Backups\EdgeGuard\Hub\edgeguard_hub_20260907_020000.dump'

& $psql -U postgres -c "CREATE DATABASE edgeguard_hub_prueba;"

& $pgRestore --dbname=edgeguard_hub_prueba --username=postgres --no-owner $Respaldo

& $psql -U postgres -d edgeguard_hub_prueba -c 'SELECT COUNT(*) FROM "Studies";'
& $psql -U postgres -d edgeguard_hub_prueba -c 'SELECT COUNT(*) FROM "Nodes";'

& $psql -U postgres -c "DROP DATABASE edgeguard_hub_prueba;"
```

**Qué debe ver:** dos números. El de estudios debe parecerse al volumen real de
su operación; el de nodos debe coincidir con los sitios instalados. Un cero en
cualquiera de los dos significa que el respaldo no sirve, aunque el archivo
exista y pese lo esperado.

- [ ] La restauración termina sin errores
- [ ] Los conteos son verosímiles
- [ ] La base de prueba queda eliminada

### 3.6 Recuperación: la base de datos se dañó

**Síntoma:** el Hub responde `Unhealthy` con un fallo en `database`, o arranca y
la interfaz muestra errores al consultar información.

1. **Detenga el Hub** para que no siga escribiendo:

   ```powershell
   Stop-WebAppPool -Name EdgeGuardHub
   ```

2. **Conserve la base dañada.** Cámbiele el nombre en lugar de borrarla; si el
   respaldo resultara inservible, es lo único que quedaría.

3. **Restaure el último respaldo bueno** sobre una base nueva, con el
   procedimiento del apartado 3.5 pero apuntando al nombre real
   (`edgeguard_hub`).

4. **Vuelva a levantar el Hub:**

   ```powershell
   Start-WebAppPool -Name EdgeGuardHub
   Invoke-RestMethod http://localhost/health/ready
   ```

5. **Confirme** que los nodos vuelven a aparecer en línea y que la información
   está al día hasta la fecha del respaldo.

**Lo que se pierde:** lo ocurrido entre el respaldo y la falla. Los estudios de
ese lapso siguen existiendo en las modalidades y en el PACS; lo que falta es el
registro en el Hub.

### 3.7 Recuperación: se perdió el servidor completo

**Lo que necesita a la mano:** el paquete del instalador, el último respaldo de
la base, el respaldo de llaves y configuración, y la contraseña del rol de
PostgreSQL.

1. **Prepare un servidor nuevo** con Windows Server y acceso a PostgreSQL, según
   los prerrequisitos del manual de instalación.

2. **Restaure la base de datos** desde el respaldo, sobre una base
   `edgeguard_hub` recién creada.

3. **Ejecute el instalador** del Hub como en una instalación nueva. Es el
   camino corto y el correcto: reinstala la aplicación, recrea el sitio y el app
   pool, y deja la configuración consistente.

4. **Restaure las llaves.** Descomprima el respaldo de llaves y copie los
   archivos `key-*.xml` a `C:\inetpub\edgeguard\dp-keys`, **reemplazando** lo
   que el instalador haya creado. Después reinicie el app pool.

   Este paso es el que evita que todos los nodos tengan que volver a
   autenticarse. Si lo omite, el sistema funciona igual, pero cada nodo hay que
   atenderlo por separado.

5. **Verifique**, en este orden:

   - [ ] `Invoke-RestMethod http://localhost/health/ready` responde `Healthy`
   - [ ] La interfaz abre desde un equipo de la red y permite iniciar sesión
   - [ ] La información histórica está completa hasta la fecha del respaldo
   - [ ] Los nodos aparecen en línea en dos o tres minutos
   - [ ] El puerto 8001 acepta conexiones del HIS/RIS

6. **Avise a los sitios** que reenvíen desde la modalidad los estudios de las
   horas de la caída.

### 3.8 Lo que ningún respaldo recupera

**Los estudios en tránsito.** Un estudio que estaba viajando de la modalidad al
nodo, o del nodo al PACS, en el momento de la falla, no está en ningún respaldo.
Se recupera reenviándolo desde la modalidad. Contémplelo en el aviso a los
sitios después de cualquier recuperación: es el trámite que más tiempo toma y el
que más se olvida.
