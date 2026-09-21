## 4. Cuando algo falla

Este apartado está ordenado por **síntoma**: por lo que alguien ve o reporta, no
por la causa. Busque la descripción que más se parezca a lo que le contaron y
empiece ahí.

El primer apartado es distinto de los demás: no atiende un síntoma concreto sino
el reporte más común y más vago de todos —"no están llegando los estudios"— y
sirve para averiguar cuál de los demás apartados aplica.

### 4.1 Triage: "no están llegando los estudios"

**Antes de tocar nada, pregunte tres cosas a quien reporta:**

1. ¿Desde cuándo?
2. ¿Es un solo sitio o varios?
3. ¿Un estudio en particular o ninguno?

Con esas tres respuestas, siga el camino que corresponda. Cada paso termina en
una acción o en un apartado de este manual.

---

**Paso 1 — ¿Falla en todos los sitios o en uno?**

Entre al Hub y revise la pantalla de nodos.

- **Todos los sitios están afectados** → el problema está en el Hub o en la red
  central. Vaya al paso 2.
- **Solo un sitio** → el problema está en ese sitio. Vaya al paso 3.

---

**Paso 2 — ¿El Hub está en pie?**

Desde el servidor del Hub:

```powershell
Invoke-RestMethod http://localhost/health/ready
```

- **No responde nada** → el Hub está caído. **Apartado 4.2.**
- **Responde `Unhealthy`** → lea el bloque `checks`: dice qué componente falló.
  Si es `database`, **apartado 4.12**.
- **Responde `Healthy`** → el Hub está bien y el problema está entre los sitios y
  el Hub: red, firewall o los propios nodos. Confirme con el área de redes si
  hubo cambios, y siga con el paso 3 para cada sitio afectado.

---

**Paso 3 — ¿El nodo de ese sitio está en línea?**

En la pantalla de nodos, mire el estado y la hora del último contacto.

- **Fuera de línea o sin contacto reciente** → **apartado 4.3**. El sitio no está
  comunicando; nada de lo que envíe la modalidad va a llegar.
- **En línea** → el nodo está hablando con el Hub. El problema está entre la
  modalidad y el nodo, o después del nodo. Siga al paso 4.

---

**Paso 4 — ¿El estudio salió de la modalidad?**

Esto no se puede ver desde el Hub. Pídalo al técnico del sitio: en la consola de
la modalidad, la cola o el historial de envío del estudio.

- **La modalidad nunca lo envió, o lo tiene en error** → no es un problema del
  Hub. El técnico debe reenviarlo. Si la modalidad reporta que el envío fue
  rechazado, **apartado 4.4**.
- **La modalidad dice que lo envió correctamente** → siga al paso 5.

---

**Paso 5 — ¿El estudio aparece en el Hub?**

Búsquelo en la pantalla de estudios por fecha y sitio.

- **No aparece** → el estudio no llegó al nodo o el nodo lo rechazó.
  **Apartado 4.4.**
- **Aparece en *Enviando*** → llegó, pero no está saliendo hacia el PACS.
  **Apartado 4.5.**
- **Aparece completo pero al médico no le llega** → el estudio se entregó al
  PACS y el problema está del lado del visor o del PACS. No es un problema del
  Hub; escale al responsable del PACS con el identificador del estudio.

---

**Si el reporte era sobre la lista de trabajo** —la modalidad no ve los estudios
programados, no que falten imágenes— el camino es otro: **apartado 4.7**.

**Cierre siempre el triage anotando dónde se detuvo el flujo.** Esa frase
—"llega al nodo pero no sale al PACS", "el nodo no se comunica desde las 3 de la
mañana"— es la información más valiosa que puede entregarle a quien reciba el
escalamiento.

### 4.2 El Hub no responde

**Qué se ve:** la página no carga desde ningún equipo. El navegador muestra
error de conexión, o un error `502.5` o `500.30`.

**Qué suele ser:** el app pool se detuvo solo tras varios intentos fallidos de
arranque; casi siempre porque no alcanza la base de datos o porque una variable
de configuración quedó vacía.

**Qué hacer:**

1. Confirme el estado del app pool:

   ```powershell
   Import-Module WebAdministration
   Get-WebAppPoolState -Name EdgeGuardHub
   ```

   Si dice `Stopped`, IIS lo apagó por fallos repetidos: hay un error de
   arranque, no un problema de IIS.

2. Confirme que PostgreSQL responde:

   ```powershell
   Test-NetConnection -ComputerName localhost -Port 5432
   ```

   Ajuste el nombre del servidor si la base está en otro equipo. Si no responde,
   el problema es de la base de datos, no del Hub: escale al DBA.

3. Busque el error de arranque en el visor de eventos de Windows:

   ```powershell
   Get-EventLog -LogName Application -Newest 20 |
       Where-Object { $_.Source -like '*AspNetCore*' } |
       Format-List TimeGenerated, EntryType, Message
   ```

   Los mensajes que empiezan con `Critical startup error` dicen literalmente qué
   falta.

4. Intente levantarlo:

   ```powershell
   Start-WebAppPool -Name EdgeGuardHub
   Invoke-RestMethod http://localhost/health/ready
   ```

5. Si vuelve a caerse, **no repita el intento en bucle**: reúna lo del apartado
   5.5 y escale.

**Caso especial — después de un reinicio del servidor.** Si el Hub funcionaba y
dejó de funcionar tras reiniciar, revise si el Default Web Site de IIS quedó
activo: se lleva el puerto 80 y deja al Hub sin arrancar.

```powershell
Get-Website | Select-Object Name, State, @{n='Enlaces';e={$_.bindings.Collection.bindingInformation}}
```

### 4.3 Un nodo aparece fuera de línea

**Qué se ve:** en la pantalla de nodos, un sitio marcado fuera de línea o sin
contacto reciente. Ese sitio no está enviando nada.

**Qué suele ser, en orden de frecuencia:** el servidor del nodo apagado, el
servicio detenido, o la red entre el sitio y el Hub interrumpida.

**Qué hacer:**

1. **Descarte que sea el Hub.** Si *todos* los nodos están fuera de línea a la
   vez, el problema es central: apartado 4.2.

2. **Confirme que el equipo del sitio está encendido y en red.** Desde el
   servidor del Hub:

   ```powershell
   Test-NetConnection -ComputerName <ip-del-nodo> -Port 5001
   ```

3. **Pida que revisen el servicio en el servidor del nodo.** Se llama
   `EdgeGuardNode`:

   ```powershell
   Get-Service EdgeGuardNode
   Restart-Service EdgeGuardNode
   ```

4. **Espere dos o tres minutos** y vuelva a mirar la pantalla de nodos. La
   reconexión es automática: el nodo conserva su identidad y sus credenciales.

5. Si el nodo sigue sin aparecer, el diagnóstico continúa en el servidor del
   sitio y **excede lo que este manual cubre**. Escale con la información del
   apartado 5.5, indicando qué se probó de los pasos anteriores.

> **Los estudios no se pierden mientras el nodo está desconectado.** El nodo los
> guarda y los envía cuando recupera contacto. Lo que sí se pierde es tiempo: si
> el sitio estuvo desconectado horas, la puesta al día tarda.

### 4.4 La modalidad no logra enviar al nodo

**Qué se ve:** la modalidad reporta *asociación rechazada*, *C-STORE failed* o un
error de conexión. En el Hub no aparece nada, porque el estudio nunca llegó.

**Qué suele ser:** el nombre AE con el que la modalidad llama no coincide con el
configurado, o el puerto no es alcanzable, o esa modalidad no está dada de alta
en el nodo.

**Qué hacer:**

1. **Pida el mensaje exacto** que muestra la modalidad. La diferencia entre
   "rechazada" y "sin conexión" ya separa dos causas distintas: la primera
   significa que sí llegó y fue rechazada; la segunda, que no llegó.

2. **Verifique la conectividad** desde la propia modalidad o desde un equipo de
   su misma red, contra el puerto DICOM del nodo (por omisión, 11112).

3. **Confirme el nombre AE.** Debe coincidir **exactamente**: distingue
   mayúsculas, no admite espacios al inicio ni al final y tiene un máximo de 16
   caracteres. Es la causa más frecuente y la más fácil de descartar.

   El nombre AE del nodo se lee en **Configuración › DICOM › AE Title**, que es
   el que el nodo usa de verdad. No tome el del catálogo de nodos: es una copia
   informativa y puede estar desfasada (apartado 4.13).

4. **Confirme que la modalidad está dada de alta** en la configuración del nodo,
   desde la interfaz del Hub.

5. **Si hace falta el detalle, está en el servidor del nodo.** El nodo escribe un
   archivo por cada conexión de modalidad —incluidas las rechazadas—, con el
   motivo exacto del rechazo y sin mezclarse con las de otros equipos. Es el
   archivo que hay que enviarle al proveedor de la modalidad. Se encuentra en
   `C:\EdgeGuard\Node\logs\associations\<fecha>\`, con el nombre del equipo en el
   nombre del archivo.

### 4.5 Un estudio se quedó en *Enviando*

**Qué se ve:** el estudio llegó al Hub, pero su estado no avanza. Más de diez
minutos ya es anormal.

**Qué suele ser:** el PACS no está aceptando. Inalcanzable, con el nombre AE mal
configurado, o sin autorizar al Hub como emisor.

**Qué hacer:**

1. **¿Es uno o son todos?** Si todos los estudios recientes están detenidos, el
   PACS no está recibiendo. Si es solo uno, revise primero si se reintentó solo.

2. **Pruebe la conexión al PACS** desde la interfaz del Hub, en la configuración
   de servidores PACS. La prueba de conectividad dice de inmediato si responde.

3. **Si la prueba falla**, revise con el responsable del PACS, en este orden:
   que el servicio esté arriba, que el firewall permita la salida del Hub hacia
   el PACS, y que el nombre AE del Hub esté autorizado en la lista de emisores
   del PACS.

4. **Cuando el PACS vuelva**, reintente el envío desde la interfaz del Hub. Los
   estudios detenidos no se pierden.

5. Busque el motivo exacto en el registro del día, con el procedimiento del
   apartado 5.4.

### 4.6 No llegan los mensajes del HIS

**Qué se ve:** el HIS/RIS reporta que envió y en el Hub no aparece nada. El HIS
recibe tiempo de espera agotado o conexión rechazada.

**Qué suele ser:** el puerto 8001 bloqueado, o el Hub reiniciándose justo en ese
momento.

**Qué hacer:**

1. **Confirme que el Hub está escuchando:**

   ```powershell
   Get-NetTCPConnection -LocalPort 8001 -State Listen
   ```

   Si no devuelve nada, el receptor HL7 no está activo: revise que el Hub esté
   arriba (apartado 4.2) y que el receptor esté habilitado en la configuración.

2. **Confirme que el HIS alcanza el puerto.** Desde el servidor del HIS:

   ```powershell
   Test-NetConnection -ComputerName <ip-del-hub> -Port 8001
   ```

   Si falla, es firewall o red. El puerto 8001 debe estar abierto desde el
   segmento del HIS/RIS hacia el Hub.

3. **Confirme el formato.** El puerto 8001 exige envoltura MLLP: el mensaje va
   entre un byte inicial `0x0B` y dos finales `0x1C 0x0D`. Un HIS que envía texto
   plano sin esa envoltura conecta pero no es entendido. Es un ajuste del lado
   del HIS.

4. **Revise el registro del día** buscando `hl7` o `mllp`, con el procedimiento
   del apartado 5.4.

> **Si esto empezó justo después de un reinicio del Hub**, es esperado: el
> receptor HL7 vive dentro del proceso del Hub y no acepta conexiones durante el
> reinicio. Los mensajes de esos segundos hay que reenviarlos desde el HIS.

### 4.7 El HIS recibe rechazo del Hub

**Qué se ve:** el HIS envía, la conexión funciona, y el Hub contesta con un
rechazo. El estudio o la orden no se crea.

**Qué suele ser:** el mensaje es de un tipo no soportado, o le falta un dato
obligatorio.

**Qué hacer:**

1. **Lea la respuesta de rechazo.** El Hub explica el motivo en el propio
   mensaje de respuesta. Pídaselo al responsable del HIS: ahí está la causa
   completa.

2. **Confirme que el tipo de mensaje está soportado:**

   | Tipo | Eventos que el Hub procesa |
   |---|---|
   | ADT | A01 (admisión), A40 (fusión de pacientes) |
   | ORM | O01 (orden) |
   | ORU | R01 (resultado) |

   Cualquier otro evento se rechaza por diseño, no por falla.

3. **Si falta un dato obligatorio**, el rechazo dice cuál. Es una corrección del
   lado del HIS.

4. **Caso frecuente — la fusión de pacientes no ocurre:** un mensaje `ADT^A40`
   sin el segmento `MRG`, o con `MRG-1` vacío, se acepta pero no fusiona nada. El
   HIS debe incluir el identificador anterior del paciente en ese segmento.

### 4.8 La lista de trabajo llega vacía a la modalidad

**Qué se ve:** la modalidad consulta la worklist y no recibe estudios, sin error.

**Qué suele ser:** las órdenes no llegaron, o llegaron con una fecha que no
coincide con la que la modalidad consulta.

**Qué hacer:**

1. **Confirme que las órdenes llegaron.** En la interfaz del Hub, revise si hay
   entradas de lista de trabajo para la fecha de hoy. Si no hay ninguna, el
   problema es de las órdenes: apartado 4.6.

2. **Revise la fecha.** La mayoría de las modalidades consulta solo el día en
   curso. Una orden con fecha de mañana existe y no aparece.

3. **Revise el nombre AE** con el que consulta la modalidad: debe ser el del
   nodo del sitio.

4. **Revise el estado de las entradas:** solo las programadas se devuelven.

### 4.9 No puedo entrar / la sesión se cerró sola

**Qué se ve:** el sistema pide iniciar sesión de nuevo, o devuelve *no
autorizado*.

**Qué suele ser:** la sesión caducó. Duran alrededor de una hora.

**Qué hacer:**

1. Cierre sesión y vuelva a entrar. En la mayoría de los casos, ahí termina.

2. **Si le pasa a todos a la vez y de golpe**, no es caducidad: alguien cambió el
   secreto de firma o el emisor de tokens, o el Hub se reinstaló sin conservar
   esa configuración. Escale: es un cambio de configuración, no un problema de
   uso.

3. **Si el reloj del servidor está desfasado**, las sesiones se rechazan antes
   de tiempo. Confirme que el servidor sincroniza la hora con la red.

### 4.10 La página abre en blanco

**Qué se ve:** la dirección responde, pero la pantalla queda vacía; o aparece
texto crudo con datos en lugar de la interfaz.

**Qué suele ser:** el paquete instalado no incluía la interfaz web.

**Qué hacer:** esto no se repara en el servidor. **Solicite un paquete nuevo al
proveedor** e instálelo. El instalador detecta este defecto y se detiene antes
de tocar nada, así que si ocurrió es que se instaló con un paquete anterior a
esa verificación.

### 4.11 Los datos no se actualizan solos

**Qué se ve:** la interfaz funciona, pero hay que recargar la página para ver
cambios. Antes se actualizaba sola.

**Qué suele ser:** la conexión en tiempo real está bloqueada por un proxy o un
firewall intermedio, o quedó deshabilitada en el sitio de IIS.

**Qué hacer:**

1. Confirme que la función está habilitada en el sitio:

   ```powershell
   Get-WebConfigurationProperty -PSPath "IIS:\Sites\EdgeGuard.Hub" `
       -Filter "system.webServer/webSocket" -Name "enabled"
   ```

   Debe decir `True`.

2. Si está habilitada y aun así no actualiza, hay un intermediario de red
   bloqueando la conexión. Escale al área de redes.

> **No es una falla que detenga la operación.** El sistema sigue funcionando y
> los datos son correctos; solo dejan de refrescarse solos.

### 4.12 El servidor se está quedando sin espacio

**Qué se ve:** la salud reporta `Degraded` con un aviso de disco, o el sistema
empieza a fallar de forma errática.

**Qué hacer:**

1. Mire cuánto queda y quién lo está ocupando:

   ```powershell
   Get-PSDrive C | Select-Object Used, Free
   Get-ChildItem C:\inetpub\EdgeGuard\Hub\logs | Measure-Object Length -Sum
   ```

2. **No borre archivos del Hub para hacer espacio.** Los registros se depuran
   solos y el resto del directorio es la aplicación instalada.

3. Si el espacio lo consumen los registros de forma anormal, hay algo generando
   errores en volumen: revise el apartado 5.4 antes de limpiar, porque la causa
   importa más que el espacio.

4. Si son los respaldos los que llenan el disco, revise la retención del
   apartado 3.3 y saque las copias antiguas del servidor.

### 4.13 El nombre AE del nodo aparece distinto en dos pantallas

**Qué se ve:** el mismo nodo muestra un nombre AE en el catálogo de nodos y otro
en su pantalla de configuración. Uno de los dos es el que las modalidades y el
PACS están usando de verdad, y no hay forma de saber cuál con sólo mirarlos.

**Cuál manda:** el de **Configuración › DICOM › AE Title**.

Ese es el único valor que gobierna la asociación DICOM. De él el nodo deriva, al
arrancar, las tres cosas que un AE hace en la práctica:

| Para qué | De dónde sale |
|---|---|
| El nombre con el que el nodo se presenta ante las modalidades | Configuración › DICOM › AE Title |
| El nombre con el que el nodo llama al PACS al reenviar | El mismo, derivado |
| El nombre con el que el nodo se registra en el Hub | El mismo, derivado |

El nombre que aparece en el catálogo es una copia que el Hub mantiene para poder
listar, buscar y ordenar por AE. **Es informativa.** Si alguna vez discrepa de la
de configuración, la de configuración es la correcta.

**Qué hacer:**

1. **Abra el nodo en el Hub y vaya a Configuración › DICOM.** Lea el valor de
   *AE Title*. Ese es el nombre real del nodo, el que hay que dar a quien
   configure una modalidad o un PACS.

2. **Si el catálogo muestra otro valor, vuelva a guardar ese mismo ajuste.**
   Ábralo, guárdelo sin cambiarlo. El Hub actualiza la copia del catálogo al
   guardar, así que con eso vuelven a coincidir.

3. **Si tras guardar siguen discrepando**, es porque otro nodo ya tiene ese
   mismo nombre AE. Dos nodos no pueden compartirlo: las asociaciones acabarían
   yendo al sitio equivocado. Busque el nombre AE en el catálogo de nodos, decida
   cuál de los dos lo conserva y cambie el del otro. En el registro del Hub queda
   anotado cuál es el nodo en conflicto (apartado 5.4).

**Por qué pasaba:** hasta ahora el Hub fijaba el nombre del catálogo cuando el
nodo se daba de alta y no volvía a tocarlo nunca. Si después se cambiaba el AE
desde configuración, el catálogo seguía mostrando el del día del alta. Las
versiones actuales mantienen la copia al día sola; los nodos que ya hubieran
quedado descuadrados se corrigen con el paso 2.

**Lo que no hay que hacer:** cambiar el nombre AE de un nodo que está operando
sin avisar a quien configuró las modalidades. El nodo empieza a rechazar las
asociaciones que llegan con el nombre anterior, y el síntoma que ven en el área
de imagenología es el del apartado 4.4 — con la diferencia de que ahí la causa
era una configuración equivocada, y aquí es un cambio deliberado que nadie
comunicó.

---

## 5. Los registros y el Correlation ID

Este apartado es el que convierte "algo falló" en un dato concreto que se puede
escalar.

### 5.1 Dónde están los registros

| Qué | Dónde | Formato |
|---|---|---|
| Operación del Hub, día a día | `C:\inetpub\EdgeGuard\Hub\logs\hub-<AAAAMMDD>.log` | Una línea JSON por evento |
| Errores de arranque del Hub | Visor de eventos de Windows → Registros de Windows → Aplicación | Texto |
| Instalación del Hub | `setup\hub\log\install-<fecha>.log` | Texto |
| Operación de un nodo | `C:\EdgeGuard\Node\logs\` **en el servidor del sitio** | Una línea JSON por evento |
| Conexiones de modalidad | `C:\EdgeGuard\Node\logs\associations\<fecha>\` **en el servidor del sitio** | Un archivo por conexión |

El Hub conserva alrededor de un mes de registros y borra los más viejos solo. No
hay que hacer limpieza manual.

### 5.2 Cómo se leen

**El registro del Hub no es texto corrido: cada línea es un objeto JSON.** Abrirlo
en el Bloc de notas se ve mal a propósito. Se lee con PowerShell, que lo
interpreta:

**Los últimos errores del día:**

```powershell
$hoy = "C:\inetpub\EdgeGuard\Hub\logs\hub-$(Get-Date -Format yyyyMMdd).log"

Get-Content $hoy | ForEach-Object { $_ | ConvertFrom-Json } |
    Where-Object { $_.'@l' -in 'Error','Fatal','Warning' } |
    Select-Object -Last 20 @{n='Hora';e={$_.'@t'}}, @{n='Nivel';e={$_.'@l'}}, @{n='Mensaje';e={$_.'@mt'}}
```

**Qué significan esos campos:**

| Campo | Qué es |
|---|---|
| `@t` | Fecha y hora del evento, en horario universal (UTC) |
| `@l` | Gravedad: `Warning`, `Error`, `Fatal`. **Si no aparece, el evento es informativo** |
| `@mt` | El mensaje |
| `@x` | El detalle técnico del error, cuando lo hay |
| `CorrelationId` | El identificador de seguimiento; apartado 5.3 |

> **La hora está en UTC.** Si su zona horaria es UTC−6, un evento de las 14:30
> del registro ocurrió a las 08:30 locales. Téngalo presente al correlacionar con
> la hora que reporta el usuario: es la confusión más común al leer estos
> archivos.

**Todo lo de una franja horaria concreta:**

```powershell
Get-Content $hoy | ForEach-Object { $_ | ConvertFrom-Json } |
    Where-Object { $_.'@t' -ge '2026-09-07T14:00' -and $_.'@t' -le '2026-09-07T14:30' } |
    Select-Object @{n='Hora';e={$_.'@t'}}, @{n='Mensaje';e={$_.'@mt'}}
```

**Buscar una palabra en el registro completo** —cuando no importa el formato,
solo encontrar algo:

```powershell
Select-String -Path $hoy -Pattern 'pacs' | Select-Object -Last 20
```

### 5.3 El Correlation ID

**Qué es:** un identificador que el Hub le asigna a cada operación y que arrastra
por todos los eventos que esa operación genera. Sirve para reconstruir qué pasó
en una sola búsqueda, en lugar de leer el registro entero.

**Dónde lo consigue:**

- **Cuando el sistema muestra un error al usuario**, la respuesta incluye un
  `correlationId`. Ese es el dato de oro: pídalo siempre. Si el usuario mandó una
  captura de pantalla, suele estar ahí.
- **En cualquier respuesta del sistema**, viaja en el encabezado
  `X-Request-Id`.
- **En el registro**, cada evento lo lleva en el campo `CorrelationId`.

**Cómo se usa:**

```powershell
$id = "a3f9c1e07b4d4e8fa2c5"

Get-Content $hoy | ForEach-Object { $_ | ConvertFrom-Json } |
    Where-Object { $_.CorrelationId -eq $id } |
    Select-Object @{n='Hora';e={$_.'@t'}}, @{n='Nivel';e={$_.'@l'}}, @{n='Mensaje';e={$_.'@mt'}}, @{n='Detalle';e={$_.'@x'}}
```

**Qué obtiene:** la secuencia completa de esa operación, en orden, de principio a
fin. Es lo que hay que adjuntar al escalar.

**Si el error fue de ayer**, cambie el archivo por el de esa fecha. Si no sabe el
día, busque en varios:

```powershell
Get-ChildItem "C:\inetpub\EdgeGuard\Hub\logs\hub-*.log" |
    Select-String -Pattern $id | Select-Object Filename, LineNumber
```

### 5.4 Qué buscar según el síntoma

Todos estos comandos suponen que `$hoy` ya está definido como en el apartado
5.2. Cambie la palabra buscada según el caso.

| Síntoma | Qué buscar |
|---|---|
| Estudios detenidos en *Enviando* | `Select-String -Path $hoy -Pattern 'c-store\|pacs\|send'` |
| No llegan mensajes del HIS | `Select-String -Path $hoy -Pattern 'hl7\|mllp'` |
| Un nodo se desconecta | `Select-String -Path $hoy -Pattern 'node\|heartbeat\|register'` |
| Lista de trabajo vacía | `Select-String -Path $hoy -Pattern 'orm\|worklist\|scheduled'` |
| Sesiones rechazadas | `Select-String -Path $hoy -Pattern 'jwt\|unauthorized'` |
| El Hub consume demasiada memoria | `Select-String -Path $hoy -Pattern 'queue\|backlog'` |

**Qué no va a encontrar en los registros, y es correcto que no esté:** nombres de
pacientes, identificadores de paciente, fechas de nacimiento ni números de
solicitud. El Hub los sustituye por `[REDACTED]` antes de escribir. Tampoco se
guardan las imágenes ni el contenido de los mensajes HL7. Lo que sí queda es el
identificador técnico del estudio, que es suficiente para seguirle el rastro sin
exponer datos del paciente.

### 5.5 Qué reunir antes de escalar

Reúna esto **antes** de llamar. Un reporte con estos seis puntos se resuelve en
una fracción del tiempo que uno sin ellos.

- [ ] **Qué pasó**, en una frase, y **desde cuándo**
- [ ] **A quién afecta**: un usuario, un sitio, todos
- [ ] **Dónde se detuvo el flujo**, según el triage del apartado 4.1
- [ ] **El Correlation ID**, si el sistema mostró un error
- [ ] **La respuesta completa** de `Invoke-RestMethod http://localhost/health/ready`
- [ ] **Qué se intentó ya** de este manual, y qué resultado dio

Y si el problema involucra estudios o mensajes concretos:

- [ ] El identificador del estudio, o el número de control del mensaje HL7
- [ ] La hora aproximada, **indicando si es hora local o del registro**

**Extraiga los eventos relevantes a un archivo** para adjuntarlos:

```powershell
$hoy = "C:\inetpub\EdgeGuard\Hub\logs\hub-$(Get-Date -Format yyyyMMdd).log"

Get-Content $hoy | ForEach-Object { $_ | ConvertFrom-Json } |
    Where-Object { $_.'@l' -in 'Error','Fatal','Warning' } |
    Select-Object @{n='Hora';e={$_.'@t'}}, @{n='Nivel';e={$_.'@l'}},
                  @{n='Mensaje';e={$_.'@mt'}}, @{n='Detalle';e={$_.'@x'}},
                  CorrelationId |
    Export-Csv "$env:USERPROFILE\Desktop\eventos-hub-$(Get-Date -Format yyyyMMdd).csv" `
               -NoTypeInformation -Encoding UTF8
```

Deja un archivo en el escritorio, listo para adjuntar. Revíselo antes de
enviarlo por correo: aunque el Hub sustituye los datos del paciente, el archivo
sigue siendo información interna de la operación.
