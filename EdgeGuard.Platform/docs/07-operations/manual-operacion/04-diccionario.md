## 6. Diccionario

Dos listas. La primera explica las palabras que aparecen en este manual, en la
interfaz y en boca del soporte. La segunda traduce los mensajes que el sistema
muestra y dice a qué apartado ir.

Ninguna de las dos se lee de corrido: se consultan.

### 6.1 Términos

#### App pool

El contenedor de IIS dentro del cual corre el Hub. Se llama `EdgeGuardHub`.

Cuando este manual dice "reiniciar el Hub", quiere decir reiniciar este app pool
(apartado 2.1). Cuando IIS lo detiene solo, es porque el Hub falló varias veces
seguidas al arrancar (apartado 4.2).

#### Asociación

La conversación completa entre una modalidad y un nodo: desde que el equipo
saluda hasta que se despide, incluyendo las imágenes que envía en medio.

Importa porque el nodo guarda **un archivo por asociación**, incluidas las que
rechaza. Ese archivo es el que se le manda al proveedor de la modalidad cuando
un equipo no logra enviar (apartado 4.4).

#### Correlation ID

El identificador que el Hub le pone a cada operación y que arrastra por todos
los eventos que esa operación genera. Aparece en pantalla cuando el sistema
muestra un error.

Es el dato más útil que puede pedirle a un usuario que reporta una falla:
convierte "algo no funcionó a media mañana" en una búsqueda exacta
(apartado 5.3).

#### DICOM

El estándar con el que hablan los equipos de imagen. Define tanto el formato de
los estudios como la forma de transmitirlos.

Aquí aparece en tres momentos: la modalidad envía por DICOM al nodo, el nodo
guarda, y el Hub reenvía por DICOM al PACS.

#### Estudio

El conjunto de imágenes de un paciente producidas en un mismo acto. Es la unidad
con la que trabaja todo el sistema: llega uno, se procesa uno, se envía uno.

#### Health (salud)

Las dos direcciones que el Hub publica para reportar su propio estado:
`/health/live` dice si está vivo, `/health/ready` dice si además puede trabajar.
Se explican en el apartado 1.2.

#### HIS y RIS

Los sistemas administrativos del hospital: el HIS gestiona pacientes y
admisiones; el RIS, la operación de imagenología.

Le envían al Hub las órdenes y las admisiones por HL7. Cuando alguien dice "no
llegan las órdenes", habla de este camino (apartado 4.6).

#### HL7

El estándar con el que hablan los sistemas administrativos, distinto de DICOM y
con otro propósito: DICOM transporta imágenes, HL7 transporta datos —quién es el
paciente, qué estudio se le ordenó, cuál fue el resultado.

Los tipos que el Hub procesa son ADT (admisiones y fusiones), ORM (órdenes) y
ORU (resultados). Cualquier otro se rechaza por diseño (apartado 4.7).

#### Key ring

La carpeta `C:\inetpub\edgeguard\dp-keys` y las llaves de cifrado que contiene.
Con ellas el Hub protege los secretos que guarda, entre ellos la credencial de
cada nodo registrado.

**Es el elemento más frágil de la instalación.** Perderlo no rompe nada de
inmediato —el sistema sigue en pie y sin errores visibles— pero obliga a que
cada nodo vuelva a autenticarse. Se respalda (apartado 3.4) y no se toca
(apartado 2.4).

#### Lista de trabajo (worklist)

La lista de estudios programados que la modalidad consulta para saber qué le
toca hacer. Se alimenta de las órdenes que envía el HIS/RIS.

Si llega vacía, casi siempre es un desajuste de fechas o de nombre AE, no una
falla (apartado 4.8).

#### MLLP

La envoltura que exigen los mensajes HL7 al viajar por red: un byte al inicio y
dos al final que marcan dónde empieza y termina cada mensaje.

Un sistema que envía HL7 sin esa envoltura logra conectarse y aun así no es
entendido. Es una causa frecuente de "envié y no llegó" (apartado 4.6).

#### Modalidad

El equipo que produce las imágenes: tomógrafo, resonador, rayos X, ultrasonido.

#### Nodo (Edge Node)

El componente instalado en cada sitio. Recibe los estudios de las modalidades de
ese sitio y los entrega al Hub.

Corre como servicio de Windows, se llama `EdgeGuardNode` y su instalación es un
procedimiento aparte, no incluido en este paquete. Cuando un nodo está fuera de
línea, ese sitio no envía nada (apartado 4.3).

#### Nombre AE

El nombre con el que un equipo DICOM se identifica ante otro. Cada modalidad
tiene el suyo, cada nodo el suyo y el PACS el suyo.

Distingue mayúsculas, admite hasta 16 caracteres y no tolera espacios sobrantes.
Un nombre AE mal escrito es la causa más frecuente de que una modalidad no logre
enviar, y la más rápida de descartar (apartado 4.4).

El nombre AE de un nodo se configura en **Configuración › DICOM › AE Title**, y
sólo ahí. Ese valor gobierna las tres cosas que el nodo hace con su nombre:
presentarse ante las modalidades, llamar al PACS al reenviar y registrarse en el
Hub. El nombre que aparece en el catálogo de nodos es una copia informativa; si
alguna vez discrepa del de configuración, manda el de configuración
(apartado 4.13).

Dos nodos no pueden compartir nombre AE.

#### PACS

El sistema donde las imágenes quedan archivadas y desde donde el médico las
consulta. Es el destino final de los estudios.

El Hub le entrega los estudios; no los almacena. Si el PACS no recibe, los
estudios se acumulan en estado *Enviando* (apartado 4.5).

#### Reciclaje del app pool

Que IIS apague y vuelva a levantar el proceso del Hub. Ocurre al reiniciarlo a
mano y, en otras instalaciones, de forma automática por inactividad.

**Aquí está deshabilitado el reciclaje automático a propósito**: el receptor de
mensajes HL7 vive dentro de ese proceso, y si IIS lo apagara por falta de
visitas web, el HIS dejaría de ser atendido sin que nadie se enterara.

#### Redacción de datos

La sustitución automática de los datos del paciente por `[REDACTED]` antes de
escribir cualquier registro. Aplica a nombre, identificador, fecha de
nacimiento, número de solicitud y médico solicitante.

Por eso los registros nunca sirven para saber *de quién* era un estudio, y sí
para saber qué pasó con él (apartado 5.4).

#### Sesión

La autorización temporal que recibe quien inicia sesión en la interfaz. Dura
alrededor de una hora y se renueva sola mientras se usa el sistema.

Que expire es normal. Que expiren todas a la vez, no (apartado 4.9).

#### Sitio de IIS

El sitio web que publica el Hub. Se llama `EdgeGuard.Hub` y ocupa el puerto 80
del servidor, sin nombre de host: responde por cualquier dirección IP del
equipo.

De ahí la advertencia sobre el Default Web Site: dos sitios no pueden compartir
el mismo puerto (apartado 1.4).

#### UTC

El horario universal, sin ajustes locales ni horario de verano. **Los registros
del Hub están en UTC.**

Si su zona es UTC−6, un evento registrado a las 14:30 ocurrió a las 08:30
locales. Es la confusión más común al leer registros (apartado 5.2).

### 6.2 Mensajes del sistema

#### Durante la instalación

| Mensaje | Qué significa | Dónde se resuelve |
|---|---|---|
| `El puerto 80 ya está reservado en IIS por: 'Default Web Site'…` | Otro sitio de IIS tiene el puerto que el Hub necesita | Elimine ese binding o dé al Hub otro puerto. Detenerlo no basta |
| `El puerto 80 está ocupado por un proceso ajeno a IIS` | Un servicio que no es un sitio web escucha ahí | Libere el puerto o cambie el del Hub |
| `Este instalador requiere una consola elevada` | La consola no se abrió como administrador | Abra PowerShell con «Ejecutar como administrador» |
| `El paquete no contiene wwwroot/index.html` | El paquete llegó sin la interfaz web | No se repara en el servidor: solicite un paquete nuevo al proveedor |
| `Checksum incorrecto en '…'` | El archivo llegó dañado o no es el que se declaró | Vuelva a copiar el paquete completo |
| `Espacio insuficiente en C:` | Menos de 5 GB libres | Libere espacio o instale en otra unidad |
| `No se puede alcanzar PostgreSQL en …` | La base no responde en esa dirección y puerto | Confirme con el DBA que el servidor está arriba y el puerto abierto |
| `La base '…' no existe. El instalador no la crea` | Falta crear la base de datos | El DBA la crea antes de instalar |
| `El rol '…' no existe o pg_hba.conf rechaza la conexión` | El usuario de base no existe, o el servidor no acepta conexiones desde este equipo | Coordine con el DBA |
| `Contraseña incorrecta para el rol '…'` | La contraseña del archivo de configuración no es la del rol | Corrija la credencial y repita |
| `El rol '…' no tiene permiso CREATE en el esquema public` | Falta un permiso en la base | El DBA lo otorga sobre la base del Hub |
| `El módulo WebAdministration no está disponible` | Falta la consola de administración de IIS | Revise el resultado del paso de características de IIS |
| `El Hub no respondió en /health/live tras … s` | Se instaló pero no arrancó | Es el caso del apartado 4.2 |

#### Durante la operación

| Mensaje o señal | Qué significa | Dónde se resuelve |
|---|---|---|
| `Healthy` | Todo en orden | — |
| `Degraded` | Algo no crítico está al límite, casi siempre el disco | Apartado 4.12 |
| `Unhealthy` | Algo crítico falló, casi siempre la base de datos | Apartado 4.2 |
| Error `502.5` o `500.30` en el navegador | El Hub no logró arrancar | Apartado 4.2 |
| `Critical startup error: EDGEGUARD_HUB_CONNECTIONSTRING is not set or empty` | El Hub no sabe dónde está su base de datos | Apartado 4.2. Es un cambio de configuración: escale |
| `401` o «sesión expirada» | La sesión caducó | Apartado 4.9 |
| `429 Too Many Requests` | Un sistema conectado está consultando en exceso | Identifíquelo con el registro y limite su frecuencia |
| *Association rejected* en la modalidad | El nodo rechazó la conexión del equipo | Apartado 4.4 |
| El HIS recibe un rechazo (NACK) | El mensaje llegó pero no se pudo procesar | Apartado 4.7 |
| Estudio detenido en *Enviando* | El PACS no está recibiendo | Apartado 4.5 |
| Nodo *fuera de línea* | Ese sitio no está comunicando | Apartado 4.3 |
