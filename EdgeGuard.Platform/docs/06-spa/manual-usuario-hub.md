# Manual de Usuario — Hub de EdgeGuard

**Versión del documento:** 1.0
**Audiencia:** personal de nuevo ingreso en el área de salud (recepción, técnicos radiólogos, coordinadores de imagen, TI hospitalaria, soporte)
**Requisitos previos:** ninguno. Este manual asume que **nunca** has trabajado con imagenología médica ni con sistemas DICOM.

---

## Cómo leer este manual

Si es tu primer día, lee en este orden:

1. **Capítulo 1** — Qué es EdgeGuard y por qué existe.
2. **Capítulo 2** — Glosario. No lo memorices; regresa a él cada vez que veas una palabra rara.
3. **Capítulo 3** — Cómo entrar y cómo está organizada la pantalla.
4. **Capítulo 4 en adelante** — Cada pantalla, campo por campo.

> **Convención de este documento**
> - 🟦 **Concepto** — explicación conceptual, sin pasos.
> - ✅ **Cómo hacerlo** — procedimiento paso a paso.
> - ⚠️ **Cuidado** — acción que puede afectar a otras personas o a datos de pacientes.
> - 📸 `[IMAGEN: ...]` — espacio reservado para captura de pantalla.

---

## Índice

1. [Introducción: qué es EdgeGuard](#1-introducción-qué-es-edgeguard)
2. [Glosario de términos](#2-glosario-de-términos)
3. [Primeros pasos: acceso y anatomía de la pantalla](#3-primeros-pasos-acceso-y-anatomía-de-la-pantalla)
4. [Dashboard](#4-dashboard)
5. [Estudios](#5-estudios)
6. [Pacientes](#6-pacientes)
7. [Nodos](#7-nodos)
8. [Servidores PACS](#8-servidores-pacs)
9. [Reglas de Ruteo (HL7)](#9-reglas-de-ruteo-hl7)
10. [Estado HL7](#10-estado-hl7)
11. [Cola de Mensajes](#11-cola-de-mensajes)
12. [Outbox](#12-outbox)
13. [WhatsApp](#13-whatsapp)
14. [Plantillas de Email](#14-plantillas-de-email)
15. [Notificaciones](#15-notificaciones)
16. [Usuarios, roles y permisos](#16-usuarios-roles-y-permisos)
17. [Configuración del sistema](#17-configuración-del-sistema)
18. [Auditoría](#18-auditoría)
19. [Apéndices](#19-apéndices)

---

# 1. Introducción: qué es EdgeGuard

## 1.1 El problema que resuelve

🟦 **Concepto**

Cuando a un paciente le hacen una tomografía, una resonancia o una radiografía, el equipo médico (el "tomógrafo", el "rayos X") produce **imágenes digitales**. Esas imágenes tienen que viajar desde el cuarto donde está el equipo hasta:

- un **archivo digital** donde el radiólogo las lee,
- el **expediente** del paciente,
- y a veces hasta el **celular del paciente**, en forma de liga para ver su estudio.

Ese viaje suena simple, pero en la práctica falla mucho: la red del hospital se cae, el equipo está en otra ciudad, el archivo pesa 800 MB, el sistema del hospital habla un idioma distinto al del equipo, etc.

**EdgeGuard es el sistema que hace ese viaje confiable.**

## 1.2 Las dos mitades: Nodo y Hub

EdgeGuard tiene dos piezas:

| Pieza | Dónde vive | Qué hace |
|---|---|---|
| **Nodo (Edge)** | Dentro de la clínica u hospital, cerca de los equipos médicos | Recibe las imágenes directamente del tomógrafo, las guarda localmente y las reenvía |
| **Hub** | En el centro de datos / la nube | Coordina todos los nodos, concentra la información, notifica a pacientes, y es **la interfaz que ves en este manual** |

📸 `[IMAGEN 1.1: Diagrama Equipo médico → Nodo → Hub → PACS / Paciente]`

**Este manual documenta el Hub**, es decir, la pantalla web a la que entras desde el navegador.

## 1.3 El recorrido típico de un estudio

Este es el flujo que verás reflejado en casi todas las pantallas:

```
1. Se agenda la cita          → llega un mensaje HL7 al Hub    → estado "Agendado"
2. El paciente llega y le hacen el estudio
3. El equipo envía imágenes   → las recibe el Nodo             → estado "Recibiendo"
4. Termina el envío                                            → estado "Completado"
5. El Nodo sube el estudio al Hub y/o al PACS                  → estado "Enviado a PACS"
6. Llega la liga de imágenes                                   → estado "En espera de reporte"
7. El radiólogo firma el reporte                               → estado "Finalizado"
8. El sistema manda WhatsApp / Email al paciente
```

Si algo falla en cualquier punto, el estudio queda en **"Fallido"** y aparece en las pantallas de monitoreo para que alguien lo atienda.

---

# 2. Glosario de términos

> Esta es la sección que más vas a consultar las primeras semanas. Está ordenada alfabéticamente.

📸 `[IMAGEN 2.1: opcional — infografía de términos DICOM/HL7]`

### Términos del mundo médico-digital

**Accession Number (Número de accesión)**
Es el **folio del estudio**. Un número que el hospital le asigna a "esta radiografía, de este paciente, en esta fecha". Es la forma más común de buscar un estudio. No confundir con el número de expediente del paciente: un paciente tiene muchos accession numbers a lo largo de su vida, uno por estudio.

**AE Title (Application Entity Title)**
Es el **nombre en la red** de un equipo que habla DICOM. Piensa en él como el "apodo" con el que dos máquinas se reconocen. Máximo 16 caracteres, sin espacios, y **distingue mayúsculas**. Ejemplo: `CT_SALA3`, `PACS_PRINCIPAL`.
Si el AE Title está mal escrito, la comunicación falla aunque la IP y el puerto estén bien. Es la causa #1 de errores de conexión.

**Anonimizar**
Quitar del estudio los datos que identifican al paciente (nombre, fecha de nacimiento, etc.) antes de enviarlo. Se usa para investigación, docencia o envíos a terceros.

**Asociación (DICOM Association)**
Una "llamada telefónica" entre dos equipos DICOM. Se abre, se mandan imágenes, se cierra. En la pantalla de telemetría verás cuántas asociaciones fueron **aceptadas**, **rechazadas** o **abortadas**.

**C-ECHO**
El "¿me escuchas?" del mundo DICOM. Es un mensaje de prueba que un sistema le manda a otro para confirmar que está vivo y responde. Equivale a un *ping*. Si el C-ECHO falla, el PACS no va a recibir estudios.

**DICOM (Digital Imaging and Communications in Medicine)**
El **estándar mundial** para imágenes médicas. Define tanto el formato del archivo como la forma de mandarlo por red. Gracias a DICOM, un tomógrafo de una marca puede mandarle imágenes a un sistema de otra marca.

**DICOM ID / Patient ID**
El identificador del paciente **tal como viene escrito dentro del archivo DICOM**. Puede diferir del número de expediente del hospital.

**Estudio (Study)**
El conjunto completo de imágenes de un procedimiento. Ejemplo: "TAC de abdomen del 3 de marzo". Un estudio contiene **series**, y cada serie contiene **instancias**.

**Facility (Institución / Sede)**
El nombre de la clínica, hospital o sucursal de donde viene la información. Sirve para separar operaciones de varias sedes en un mismo Hub.

**HL7 (Health Level Seven)**
El estándar para mandar **texto e información administrativa** en salud: "se dio de alta al paciente X", "se agendó el estudio Y", "el resultado del laboratorio es Z". DICOM mueve imágenes, HL7 mueve datos.
Un mensaje HL7 se ve así (fragmento):
```
MSH|^~\&|RIS|HOSPITAL_A|EDGEGUARD|HUB|20260802120000||ORM^O01|123|P|2.5
```

**Instancia (Instance)**
Una imagen individual — literalmente un corte, una placa. Un TAC de abdomen puede tener 800 instancias.

**Issuer of Patient ID**
Quién emitió el identificador del paciente. Sirve cuando dos hospitales usan el mismo número para pacientes distintos.

**Modalidad (Modality)**
El **tipo de equipo/estudio**. Se escribe con códigos de dos letras:

| Código | Significado |
|---|---|
| `CT` | Tomografía computarizada |
| `MR` | Resonancia magnética |
| `CR` | Radiografía computarizada |
| `DX` | Radiografía digital |
| `US` | Ultrasonido |
| `NM` | Medicina nuclear |
| `PT` | PET |

**PACS (Picture Archiving and Communication System)**
El **archivo digital de imágenes** del hospital. Es donde viven las imágenes a largo plazo y desde donde el radiólogo las abre para leerlas. EdgeGuard no reemplaza al PACS: le entrega estudios.

**Serie (Series)**
Un subconjunto del estudio. En una resonancia, cada secuencia (T1, T2, FLAIR) es una serie.

**Study Instance UID**
El identificador **único mundial** del estudio. Es una cadena larga de números separados por puntos, tipo `1.2.840.113619.2.55.3.604688.987...`. No lo vas a teclear nunca; solo lo copias cuando soporte te lo pide.

**Trigger Event**
En HL7, el "por qué" del mensaje. Si el tipo de mensaje es `ADT` (movimientos del paciente), el trigger event `A01` significa "admisión", `A08` significa "actualización de datos".

**Worklist**
La lista de estudios agendados que el equipo médico consulta para saber qué le toca hacer. En EdgeGuard se alimenta de los mensajes HL7.

### Términos de la plataforma EdgeGuard

**Dead-letter**
Cuando un envío falla tantas veces que ya no tiene sentido seguir reintentando, se manda al "buzón de cartas muertas". Queda registrado pero ya no se reintenta solo.

**Heartbeat (Latido)**
Señal periódica que cada nodo manda al Hub diciendo "sigo vivo". Si el Hub deja de recibir latidos, marca al nodo como **Offline**.

**Hub**
El servidor central. La interfaz de este manual.

**Nodo (Edge Node)**
El pequeño servidor instalado en la clínica que habla directamente con los equipos médicos.

**Outbox (Bandeja de salida)**
Patrón técnico de confiabilidad: en lugar de mandar algo directo y rezar, el sistema **primero anota** lo que quiere mandar en una bandeja, y un proceso aparte se encarga de mandarlo y reintentar si falla. Por eso hay una pantalla llamada Outbox.

**Reintento (Retry)**
Volver a intentar un envío que falló.

**SignalR / "En vivo"**
La tecnología que hace que la pantalla se actualice sola sin que aprietes F5. Cuando ves el indicador verde **En vivo**, los datos están llegando en tiempo real.

**Telemetría**
Métricas de desempeño que el nodo reporta al Hub: cuántas conexiones hubo, cuántas imágenes, a qué velocidad.

**Topic (Tema)**
Categoría de mensaje dentro del Outbox. Ejemplo: "sincronización de configuración de nodo", "notificación de WhatsApp".

---

# 3. Primeros pasos: acceso y anatomía de la pantalla

## 3.1 Iniciar sesión

📸 `[IMAGEN 3.1: Pantalla de login]`

| Campo | Qué escribir |
|---|---|
| **Usuario** | El nombre de usuario que te dio el administrador. No es tu correo. |
| **Contraseña** | La contraseña asignada. |

✅ **Cómo hacerlo**
1. Abre el navegador y ve a la dirección del Hub que te proporcionaron.
2. Escribe usuario y contraseña.
3. Presiona **Iniciar sesión**.

⚠️ **Cuidado**
- Después de varios intentos fallidos, la cuenta se **bloquea**. Solo un administrador puede desbloquearla.
- Nunca compartas tu usuario. Todo lo que se hace en el Hub queda registrado en **Auditoría** con tu nombre.

**Si ves "Acceso denegado"**: tu usuario existe pero **no tiene permiso** para esa sección. Habla con tu administrador; no es un error del sistema.

## 3.2 Anatomía de la pantalla

📸 `[IMAGEN 3.2: Pantalla completa señalando las 3 zonas]`

La interfaz tiene tres zonas fijas:

**① Barra superior (header)**

| Elemento | Función |
|---|---|
| ☰ (hamburguesa) | Muestra u oculta el menú lateral. Útil en pantallas chicas. |
| 🌙 / ☀️ | Cambia entre **modo oscuro** y **modo claro**. Es solo estética. |
| Avatar con tus iniciales | Abre el menú de usuario: muestra tu nombre y el botón **Cerrar sesión**. |

**② Menú lateral (sidebar)**

Está organizado en cuatro grupos. **Solo verás las opciones para las que tienes permiso** — si a un compañero le aparecen más opciones que a ti, es por su rol, no por un error.

| Grupo | Opciones |
|---|---|
| **Principal** | Dashboard · Estudios · Pacientes |
| **Infraestructura** | Nodos · Servidores PACS · Reglas de Ruteo |
| **Monitoreo** | Estado HL7 · Cola de Mensajes · Outbox · WhatsApp · Plantillas Email · Notificaciones |
| **Administración** | Usuarios · Configuración · Auditoría |

**③ Área de contenido**

Donde se dibuja la pantalla que elegiste.

## 3.3 Elementos que se repiten en todas las pantallas

Aprende estos una vez y los reconocerás en todos lados.

📸 `[IMAGEN 3.3: Collage de los componentes comunes]`

**Encabezado de página**
Título, una descripción breve y a la derecha los botones de acción de esa pantalla (**Actualizar**, **Exportar CSV**, **Nuevo…**).

**Barra de búsqueda**
Busca conforme escribes. Cada pantalla indica en el texto gris qué campos busca.

**Botón "Filtros"**
Despliega los filtros avanzados. El **número azul** en el botón te dice cuántos filtros tienes activos. Si aparece, también aparece **Limpiar** para quitarlos todos de golpe.

> 💡 Si una tabla te sale vacía y no entiendes por qué, revisa ese número azul: casi siempre es un filtro que quedó puesto.

**Tabla de datos**
- Los encabezados con flechita se pueden **ordenar** (clic para ascendente, otro clic para descendente).
- Al pie hay **paginación**: página actual y cuántos registros por página.
- En muchas tablas, **hacer clic en una fila abre el detalle**.

**Insignias de estado (badges)**
Etiquetas de color que resumen una condición:

| Color | Significado general |
|---|---|
| 🟢 Verde | Todo bien / completado / entregado |
| 🟡 Ámbar | En proceso / en espera / requiere atención pronto |
| 🔴 Rojo | Falla / error / urgente |
| ⚪ Gris | Neutro / deshabilitado / sin información |

**Tiempos relativos**
Verás "hace 5 minutos" en lugar de la fecha exacta. Pasa el mouse encima para ver la fecha completa.

**Mensajes emergentes (toasts)**
Aparecen abajo unos segundos para confirmar ("Guardado") o avisar de un error.

---

# 4. Dashboard

> **Ruta:** `/dashboard` · **Permiso:** ninguno especial (todos los usuarios)

📸 `[IMAGEN 4.1: Dashboard completo]`

Es la pantalla de inicio. Responde de un vistazo: *"¿está todo bien ahorita?"*

## 4.1 Encabezado

| Elemento | Qué significa |
|---|---|
| **hace X minutos** | Cuándo se refrescaron los datos por última vez |
| **En vivo** (verde) | La conexión en tiempo real funciona; los números se actualizan solos |
| **Desconectado** (gris) | Perdiste el tiempo real. Los datos siguen siendo válidos pero no se refrescan solos. Usa **Actualizar** o recarga la página. |
| **Actualizar** | Refresca manualmente |

## 4.2 Tarjetas de indicadores (KPIs)

📸 `[IMAGEN 4.2: Fila de KPIs]`

| Tarjeta | Qué cuenta | Cómo interpretarla |
|---|---|---|
| **Total Estudios** | Todos los estudios registrados en el Hub | Volumen histórico |
| **Pacientes** | Pacientes distintos registrados | Volumen histórico |
| **Nodos Activos** | Nodos en línea vs. total de nodos | Si dice `3/5`, hay **2 nodos caídos**. Ve a la pantalla Nodos. |
| **Pendientes PACS** | Estudios que aún no llegan al archivo | Un número que crece y no baja = problema de conexión con el PACS |
| **En Cola** | Mensajes HL7 esperando ser procesados | Debe subir y bajar. Si solo sube, hay atoro. |
| **Fallidos** | Estudios en estado *Fallido* | **Cualquier número mayor a 0 requiere revisión** |

## 4.3 Estudios recientes

Los últimos estudios que llegaron, con paciente, descripción y estado. Sirve para confirmar en vivo que un estudio que se acaba de hacer ya entró al sistema.

📸 `[IMAGEN 4.3: Widget de estudios recientes]`

## 4.4 Estado de nodos

Lista de nodos con su semáforo (En línea / Fuera de línea / Degradado) y su último latido.

📸 `[IMAGEN 4.4: Widget de nodos]`

## 4.5 Resumen de la cola HL7

Muestra el pipeline de mensajes HL7 por etapa. Se explica a detalle en el [capítulo 11](#11-cola-de-mensajes).

📸 `[IMAGEN 4.5: Widget de cola]`

---

# 5. Estudios

> **Ruta:** `/studies` · **Permiso:** `ViewStudies`

Es la pantalla que más usarás. Aquí vive el trabajo diario.

## 5.1 Lista de estudios

📸 `[IMAGEN 5.1: Lista de estudios]`

### Acciones del encabezado

| Botón | Qué hace |
|---|---|
| **Actualizar** | Vuelve a consultar la lista |
| **Exportar CSV** | Descarga los estudios **con los filtros aplicados** en un archivo abrible en Excel |

⚠️ El CSV exportado contiene **datos de pacientes**. Trátalo como información confidencial: no lo mandes por correo personal ni lo dejes en carpetas compartidas abiertas.

### Búsqueda

La barra busca por **accession number, nombre del paciente y descripción del estudio**.

### Filtros avanzados

| Filtro | Qué hace | Cuándo usarlo |
|---|---|---|
| **Estado** | Filtra por etapa del ciclo de vida | "Muéstrame solo los Fallidos" |
| **Modalidad** | CT, MR, CR, DX, US, NM, PT | "Solo tomografías" |
| **Nodo origen** | De qué sede/nodo llegó | "Solo los de la sucursal Norte" |
| **Rango de fechas** | Desde–hasta | Cierre del día o del mes |
| **Solo urgentes** | Interruptor. Deja solo los marcados como urgentes | Priorizar trabajo |

### Columnas de la tabla

| Columna | Contenido | Notas |
|---|---|---|
| **Paciente** | Nombre formateado. Si el estudio es urgente, aparece la etiqueta roja **Urgente** | Los nombres DICOM vienen como `APELLIDO^NOMBRE`; el sistema los presenta legibles |
| **Accession** | Folio del estudio | |
| **Descripción** | Qué se estudió. Se recorta a 60 caracteres | |
| **Nodo** | AE Title del nodo de origen | |
| **Estado** | Insignia de color | Ver tabla 5.2 |
| **Tamaño** | Peso total en MB/GB | Útil para diagnosticar lentitud |
| **Series/Inst.** | `3s / 480i` = 3 series, 480 imágenes | |
| **Recibido** | Hace cuánto llegó | |

👉 **Clic en cualquier fila** abre el detalle del estudio.

## 5.2 Estados de un estudio (tabla de referencia)

Memoriza esta tabla; es el vocabulario del día a día.

| Estado | Etiqueta en pantalla | Qué está pasando | ¿Requiere acción? |
|---|---|---|---|
| `Scheduled` | Agendado | Llegó la orden por HL7, aún no hay imágenes | No |
| `Receiving` | Recibiendo | Las imágenes están entrando en este momento | No |
| `Completed` | Completado | Terminó de recibir todas las imágenes | No |
| `WaitingForImageLinks` | En espera de liga | Falta la URL para ver las imágenes | Si tarda mucho, revisar |
| `WaitingForReport` | En espera de reporte | Ya hay liga de imágenes, falta el reporte del radiólogo | Depende del radiólogo |
| `Finalized` | Finalizado | Liga + reporte listos. Es el estado "completo" clínicamente | No |
| `QueuedForSend` | En cola PACS | Esperando turno para enviarse al archivo | No |
| `Sending` | Enviando | Enviándose al PACS ahora | No |
| `SentToPacs` | Enviado a PACS | Llegó al archivo correctamente | No |
| `Failed` | Fallido | Algo salió mal | **Sí** |

## 5.3 Detalle del estudio

📸 `[IMAGEN 5.2: Detalle del estudio — vista completa]`

### Bloque superior (hero)

Muestra iniciales del paciente, nombre, `ID` y `Accession`, y la insignia de estado. Debajo, etiquetas rápidas: **Urgente**, fecha del estudio y AE Title de origen.

**Botón Reenviar** → abre el diálogo para reenviar el estudio a uno o varios PACS (sección 5.4).

### Ciclo de Vida

📸 `[IMAGEN 5.3: Línea de tiempo del estudio]`

Línea de tiempo visual que marca en qué etapa está el estudio. Sirve para explicarle a un compañero (o a un médico) dónde se atoró algo sin usar jerga técnica.

### Métricas

| Tarjeta | Significado |
|---|---|
| **Series** | Cuántas series tiene |
| **Instancias** | Cuántas imágenes individuales |
| **Tamaño** | Peso total |
| **Intentos PACS** | Cuántas veces se intentó enviar al archivo. **Un número alto (>3) indica problema de conexión** |

### Resultados: reporte y ligas

Panel con el reporte del estudio (HTML o texto plano) y, si existe, el PDF. Debajo, las **Ligas de Imágenes**: URLs para ver el estudio en visor. Son las mismas ligas que se le envían al paciente por WhatsApp o email.

### Información del Estudio

Campos de solo lectura, y con **Editar** se vuelven editables:

| Campo | Qué es | Editable |
|---|---|---|
| **Study Instance UID** | Identificador único mundial | ❌ No |
| **Descripción** | Texto libre de qué se estudió | ✅ |
| **Médico Referente** | Quién solicitó el estudio | ✅ |
| **Accession Number** | Folio | ✅ |
| **Fecha de Estudio** | Cuándo se realizó | ❌ (viene del DICOM) |
| **Prioridad (0-10)** | Orden de atención. **0 = normal, 10 = máxima** | ✅ |
| **Urgente** | Interruptor. Marca el estudio como urgente y lo resalta en rojo en toda la app | ✅ |

⚠️ Editar metadatos **no cambia el archivo DICOM original**, solo el registro del Hub. Requiere permiso `EditStudyMetadata`.

✅ **Cómo editar**
1. Clic en **Editar**.
2. Cambia lo necesario.
3. **Guardar** (o **Cancelar** para descartar).

### Cambiar estado

Menú desplegable + botón **Aplicar** para forzar manualmente un cambio de estado.

⚠️ **Cuidado.** Esto se usa para destrabar casos excepcionales. Cambiar el estado a mano puede disparar notificaciones al paciente (por ejemplo, ponerlo en *Finalizado* puede mandar el WhatsApp de "tu estudio está listo"). **Si no estás seguro, no lo toques.**

### Panel de Infraestructura

| Campo | Qué te dice |
|---|---|
| **Nodo Origen** | De qué nodo vino, con punto de color según su estado |
| **PACS Destino** | A qué archivo va, con punto según alcanzabilidad |
| **Enviado a PACS** | Cuándo llegó al archivo |
| **Último error** | Texto del error del último intento fallido. **Cópialo tal cual cuando levantes un ticket** |

### Timestamps

Creado · Actualizado · Primera imagen · Última imagen. La diferencia entre *primera* y *última* imagen te dice **cuánto tardó la transferencia**.

## 5.4 Reenviar un estudio

📸 `[IMAGEN 5.4: Diálogo de reenvío]`

✅ **Cómo hacerlo**
1. En el detalle del estudio, clic en **Reenviar**.
2. Se abre la lista de **PACS asignados al nodo de origen**. Cada uno muestra su nombre, AE Title, host y puerto.
3. Marca las casillas de los destinos deseados. El pie del diálogo cuenta cuántos llevas seleccionados.
4. Clic en **Reenviar**.

**Si dice "Sin PACS asignados"**: el nodo de origen no tiene ningún PACS activo asignado. Se resuelve en la pantalla del nodo → *Servidores PACS asignados* → **Gestionar**.

---

# 6. Pacientes

> **Ruta:** `/patients` · **Permiso:** `ViewStudies`

Aquí vive el **directorio de pacientes**: los datos de identidad y contacto que EdgeGuard usa para notificar resultados.

## 6.1 Lista de pacientes

📸 `[IMAGEN 6.1: Lista de pacientes]`

### Acciones del encabezado

| Botón | Qué hace |
|---|---|
| **Actualizar** | Recarga la lista |
| **Exportar CSV** | Descarga el listado filtrado |
| **Importar CSV** | Carga masiva de pacientes desde archivo |

⚠️ Contiene datos personales de salud. Aplica todas las políticas de privacidad de tu institución.

### Filtros

| Filtro | Uso típico |
|---|---|
| **Estado** | Activo / inactivo |
| **Nodo origen** | Pacientes registrados desde cierta sede |
| **Con teléfono** | Para saber a quiénes **sí** se les puede mandar WhatsApp |
| **Con email** | Para saber a quiénes **sí** se les puede mandar correo |

> 💡 Los filtros **Con teléfono** / **Con email** son la herramienta principal para depurar la base antes de una campaña de notificaciones: los que no tienen contacto simplemente nunca recibirán nada.

### Columnas

| Columna | Notas |
|---|---|
| **Nombre** | Nombre formateado |
| **DICOM ID** | Identificador como viene en el archivo DICOM |
| **Sexo** | `M` masculino, `F` femenino, `O` otro |
| **Fecha Nac.** | Fecha de nacimiento |
| **Teléfono** | Destino de WhatsApp |
| **Email** | Destino de correo |
| **Registrado** | Cuándo entró al sistema |

## 6.2 Detalle del paciente

📸 `[IMAGEN 6.2: Detalle del paciente]`

Muestra dos bloques: **identidad** y **actividad**.

| Campo | Explicación |
|---|---|
| **Nombre completo** | |
| **DICOM ID** | Identificador dentro del archivo DICOM |
| **Issuer of Patient ID** | Institución que emitió ese ID |
| **Fecha de Nacimiento** | El sistema calcula y muestra la edad |
| **Sexo** | |
| **Teléfono** | Se usa para WhatsApp. **Debe incluir el formato correcto**; ver nota abajo |
| **Email** | Se usa para notificaciones por correo |
| **Facility** | Sede de origen |
| **Nodo creador** | Qué nodo lo dio de alta |
| **Estudios** | Cuántos estudios tiene. Lleva a la lista filtrada |
| **Tamaño total** | Espacio que ocupan sus estudios |
| **Último estudio** | Fecha del más reciente |
| **Creado / Actualizado** | Auditoría básica |

> 📱 **Sobre el teléfono:** el Hub tiene un **prefijo de país por defecto** configurado (ver [Configuración → WhatsApp](#17-configuración-del-sistema)). Si capturas un número local, el sistema le antepone el prefijo automáticamente. Si el paciente es del extranjero, captura el número completo con su código de país.

## 6.3 Editar un paciente

📸 `[IMAGEN 6.3: Diálogo de edición de paciente]`

Campos editables: **Nombre**, **Fecha de Nacimiento**, **Sexo**, **Teléfono**, **Email**.

✅ El uso más frecuente de esta pantalla es **agregar el teléfono o el correo** de un paciente cuyo estudio ya llegó pero que no trae datos de contacto en el DICOM.

## 6.4 Importar pacientes por CSV

📸 `[IMAGEN 6.4: Resultado de importación]`

✅ **Cómo hacerlo**
1. Clic en **Importar CSV** y elige el archivo.
2. El sistema procesa y devuelve un resumen: **total de registros**, **exitosos**, **con error**.
3. Revisa el detalle fila por fila: número de fila, estado, identificador y el error si lo hubo.

⚠️ Corrige el CSV y vuelve a subir **solo las filas con error**, para no duplicar.

---

# 7. Nodos

> **Ruta:** `/nodes` · **Permiso:** `ViewNodes`

Un **nodo** es el servidor de EdgeGuard instalado en la clínica. Esta sección es donde TI y soporte pasan la mayor parte del tiempo.

## 7.1 Lista de nodos

📸 `[IMAGEN 7.1: Lista de nodos]`

### Columnas

| Columna | Qué te dice |
|---|---|
| **Nombre** | Nombre legible ("Sucursal Norte") |
| **AE Title** | Nombre DICOM en red |
| **IP:Puerto** | Dirección de red |
| **Estado** | Ver tabla 7.2 |
| **Último Heartbeat** | Hace cuánto dio señales de vida. **Si dice "hace 2 horas", el nodo está caído** |
| **Storage** | Espacio de disco usado/disponible |
| **Errores 24h** | Errores en el último día. **>0 amerita revisar** |

### Filtros

**Estado** (desplegable) y **Solo habilitados** (interruptor).

## 7.2 Estados de un nodo

| Estado | Etiqueta | Significado | Acción |
|---|---|---|---|
| `Online` | En línea | Todo normal | Ninguna |
| `Offline` | Fuera de línea | No manda latidos | **Revisar red / servicio del nodo** |
| `Degraded` | Degradado | Responde pero con problemas (disco lleno, errores) | Revisar |
| `Maintenance` | Mantenimiento | Puesto fuera a propósito | Ninguna |
| `Starting` | Iniciando | Arrancando | Esperar |
| `Stopping` | Deteniendo | Apagándose | Esperar |

## 7.3 Crear o editar un nodo

📸 `[IMAGEN 7.2: Diálogo de nodo]`

| Campo | Explicación | Obligatorio | Consejo |
|---|---|---|---|
| **Nombre** | Nombre legible para humanos | ✅ | Usa un nombre que identifique la sede: "Clínica Roma – Nodo 1" |
| **AE Title** | Identificador DICOM | ✅ (al crear) | Máx. 16 caracteres, sin espacios, sin acentos. **Debe coincidir exactamente** con lo configurado en los equipos |
| **Dirección IP** | IP del nodo en la red | ✅ | |
| **Puerto** | Puerto DICOM de escucha | ✅ | Típicamente `104` u `11112` |
| **API Endpoint** | URL del API del nodo | ❌ | Sirve para que el Hub le mande órdenes |
| **Ubicación** | Texto libre: "Sótano, cuarto de máquinas" | ❌ | Ayuda muchísimo a soporte en campo |
| **Facility** | Nombre de la institución/sede | ❌ | Se usa en notificaciones y reportes |
| **Zona horaria** | Zona del nodo | ❌ | Importante si hay sedes en husos distintos |
| **Health Check (seg)** | Cada cuántos segundos se revisa la salud | ❌ | Valor típico: 30–60 |
| **Max Storage (MB)** | Cuota de disco asignada | ❌ | Cuando se acerca al límite, el nodo pasa a *Degradado* |

⚠️ El **AE Title** normalmente no se cambia una vez que el nodo está en producción: los equipos médicos apuntan a él.

## 7.4 Detalle del nodo

📸 `[IMAGEN 7.3: Detalle del nodo]`

Desde el encabezado accedes a las tres sub-pantallas del nodo:

| Botón | Lleva a |
|---|---|
| **Configuración** | Ajustes finos del nodo (7.6) |
| **Equipos** | Catálogo de equipos médicos conectados (7.7) |
| **Editar** | Diálogo de la sección 7.3 |
| **Deshabilitar / Habilitar** | Saca o mete al nodo de operación |

⚠️ **Deshabilitar un nodo** hace que deje de recibir trabajo. Los equipos médicos que le mandan estudios empezarán a fallar. Coordínalo antes.

### Bloques de información

**Información del Nodo** — Nombre, AE Title, Dirección (`IP:puerto`), API Endpoint, Ubicación, Facility.

**Servidores PACS asignados** — La lista de archivos a los que este nodo puede enviar. Botón **Gestionar** para asignar/desasignar (sección 7.5).

**Estado** — Insignia, **Último Heartbeat** e intervalo de **Health Check**.

**Conectividad PACS** — 📸 `[IMAGEN 7.4: Tarjeta de conectividad PACS]`
El bloque más útil para diagnosticar. Muestra un marcador tipo `2/3 alcanzables` y, por cada destino:
- ✅ o ❌ según el resultado del C-ECHO
- La **latencia en milisegundos** si respondió
- Si falló, un **código de razón** en rojo y el texto del error

**Códigos de rechazo frecuentes:**

| Código | Qué significa en español |
|---|---|
| `CalledAENotRecognized` | El PACS no reconoce el AE Title que le mandaste. **Está mal escrito o no está dado de alta en el PACS** |
| `CallingAENotRecognized` | El PACS no reconoce **al nodo**. Hay que dar de alta el AE Title del nodo en el PACS |
| *Timeout / sin respuesta* | Problema de red o el PACS está apagado |

Si dice *"El nodo aún no ha reportado estado C-ECHO al Hub"*, el nodo no ha hecho su primer chequeo. Espera al siguiente ciclo o usa **Actualizar**.

**Salud** — Resumen de disco, errores y estudios procesados.

**Estudios recientes del nodo** — Últimos estudios recibidos por ese nodo.

**Timestamps** — Creado / Actualizado.

## 7.5 Asignar servidores PACS a un nodo

📸 `[IMAGEN 7.5: Diálogo de asignación de PACS]`

🟦 **Concepto.** Un nodo solo puede mandar estudios a los PACS que tenga **asignados**. Un PACS marcado como **Global** se asigna solo a todos los nodos.

| Elemento | Explicación |
|---|---|
| **Buscar servidor PACS** | Filtra la lista de disponibles |
| **Servidores PACS disponibles** | Marca los que quieres asignar |
| **Intervalo C-ECHO (segundos)** | Cada cuánto el nodo verificará que ese PACS responde. Valor típico: 300 (5 min) |
| 🔒 **"Asignado desde el Hub, no modificable"** | Asignación heredada de un PACS global; no se quita desde aquí |

## 7.6 Configuración del nodo

> **Ruta:** `/nodes/:id/config`

📸 `[IMAGEN 7.6: Configuración de nodo]`

Aquí se ajustan los parámetros internos del nodo, agrupados por categoría. Es una pantalla **técnica**; si no eres de TI, no la modifiques.

### Cómo funciona el guardado (importante)

Esta pantalla usa un flujo de **dos pasos**:

1. **Guardar** — anota el cambio en el Hub. El nodo **todavía no lo sabe**.
2. **Aplicar al nodo** — empuja la configuración al nodo para que entre en vigor.

| Botón | Qué hace |
|---|---|
| **Actualizar** | Recarga desde el servidor |
| **Guardar (N)** | Guarda los N cambios pendientes en el Hub |
| **Guardar y aplicar** | Guarda **y** empuja al nodo en una sola acción (lo más común) |
| **Aplicar al nodo** | Cuando no hay cambios pendientes, reenvía la config actual al nodo |

**Versión de config** — un identificador que cambia cada vez que se aplica configuración. Sirve para confirmar que el nodo quedó sincronizado.

⚠️ Si sales de la pantalla con la alerta ámbar *"N configuración(es) sin guardar"*, **pierdes los cambios**.

## 7.7 Equipos del nodo

> **Ruta:** `/nodes/:id/equipment`

📸 `[IMAGEN 7.7: Lista de equipos]`

🟦 **Concepto.** Aquí registras **cada equipo médico** (tomógrafo, ultrasonido, rayos X) que le manda imágenes a este nodo. Registrarlos permite que el nodo acepte sus conexiones y que puedas identificar de qué máquina vino cada estudio.

📸 `[IMAGEN 7.8: Formulario de equipo]`

| Campo | Explicación | Ejemplo |
|---|---|---|
| **AE Title** | Nombre DICOM del equipo. **Debe coincidir exactamente con el configurado en la consola del equipo** | `CT_SIEMENS_01` |
| **Nombre visible** | Cómo quieres verlo en las pantallas | "Tomógrafo Sala 3" |
| **Modalidades permitidas** | Qué tipos de estudio puede mandar. Selección múltiple | CT |
| **Scheduled Station AE** | AE Title usado para la worklist (si difiere del anterior) | |
| **Nombre de estación** | Nombre de estación DICOM | `SALA3` |
| **Dirección IP** | IP del equipo | `10.0.4.22` |
| **Ubicación** | Dónde está físicamente | "Piso 2, Sala 3" |
| **Departamento** | Área responsable | "Imagenología" |
| **Fabricante** | Marca | "Siemens" |
| **Modelo** | Modelo | "Somatom go.Top" |
| **Notas** | Texto libre | "Contrato de mantenimiento vence en marzo" |

La lista muestra además si el equipo está **en línea** y su **última conexión**.

## 7.8 Reglas DICOM del nodo

📸 `[IMAGEN 7.9: Formulario de regla DICOM]`

🟦 **Concepto.** Una **regla de ruteo DICOM** le dice al nodo: *"cuando llegue un estudio que cumpla estas condiciones, mándalo a estos destinos"*.

Las reglas se evalúan **de mayor a menor prioridad**, y se aplica la primera que coincida.

**Condiciones (todas opcionales; se combinan con Y lógico):**

| Campo | Coincide cuando… |
|---|---|
| **Modalidad** | El estudio es de esa modalidad (CT, MR, …) |
| **AE Title origen** | Vino de ese equipo específico |
| **Institución** | El campo de institución del DICOM coincide |
| **Descripción del estudio contiene** | La descripción incluye ese texto |
| **Instancias mínimas** | El estudio tiene al menos N imágenes |
| **Instancias máximas** | El estudio tiene cuando mucho N imágenes |

> 💡 **Instancias mínimas** es un truco práctico contra los "estudios basura": placas de prueba o calibraciones que traen 1–2 imágenes y no vale la pena archivar.

**Acciones:**

| Campo | Efecto |
|---|---|
| **Enviar al PACS destino** | Manda el estudio al PACS indicado |
| **Enviar al Hub** | Sube el estudio al Hub central |
| **Anonimizar antes de enviar** | Quita datos identificatorios antes de mandarlo |

⚠️ **Anonimizar** es irreversible para la copia enviada. Úsalo solo cuando el destino lo requiera (investigación, docencia, terceros).

**Otros campos:** **Nombre de la regla** (usa nombres descriptivos: "CT sala 3 → PACS principal") y **Prioridad** (número; mayor = se evalúa antes).

## 7.9 Telemetría del nodo

> **Ruta:** `/nodes/:id/telemetry`

📸 `[IMAGEN 7.10: Telemetría]`

Historial de desempeño. El nodo manda "fotos" (snapshots) periódicas al Hub.

**Tarjetas del último período:**

| Tarjeta | Qué mide | Cómo leerla |
|---|---|---|
| **Asociaciones totales** | Cuántas conexiones DICOM hubo | Volumen de trabajo |
| **Tasa de aceptación** | % de conexiones aceptadas | **Debe estar cerca de 100%.** Si baja, hay equipos intentando conectarse sin estar dados de alta |
| **Imágenes recibidas** | Total de instancias | |
| **Rendimiento promedio** | MB por segundo | Si baja mucho, hay problema de red |

**Tabla de historial** — por cada snapshot: Reportado, Período, Asociaciones (totales / aceptadas / rechazadas / abortadas), Imágenes, Estudios, Datos, Duración promedio y MB/s.

| Concepto | Significado |
|---|---|
| **Rechazada** | El nodo dijo "no" a la conexión (normalmente, AE Title desconocido) |
| **Abortada** | La conexión empezó pero se cortó a medias (red inestable, equipo apagado) |

Si dice *"Sin datos de telemetría"*, el nodo aún no ha enviado su primer snapshot.

---

# 8. Servidores PACS

> **Ruta:** `/pacs` · **Permiso:** `ViewConfiguration`

Aquí se da de alta **a dónde** se archivan los estudios.

## 8.1 Lista

📸 `[IMAGEN 8.1: Lista de PACS]`

| Columna | Qué te dice |
|---|---|
| **Nombre** | Nombre legible |
| **AE Title** | Nombre DICOM del PACS |
| **Host:Puerto** | Dirección de red |
| **Conectividad** | ✅ alcanzable / ❌ no alcanzable, según el último C-ECHO |
| **Último C-ECHO** | Hace cuánto se probó |
| **Max Asoc.** | Conexiones simultáneas permitidas |
| **Acciones** | Editar · Habilitar/Deshabilitar · Probar conexión · Eliminar |

**Filtros:** búsqueda por texto, **Solo habilitados**, **Solo globales**.

## 8.2 Alta / edición de un PACS

📸 `[IMAGEN 8.2: Formulario de PACS]`

| Campo | Explicación | Consejo |
|---|---|---|
| **Nombre** | Nombre legible | "PACS Corporativo" |
| **AE Title** | Identificador DICOM del PACS | **Te lo da el proveedor del PACS. Cópialo exactamente, respetando mayúsculas** |
| **Host** | Nombre DNS o IP | |
| **Puerto** | Puerto DICOM | Normalmente `104`, `11112` o `4242` |
| **Descripción** | Texto libre | Anota a quién llamar si falla |
| **Servidor global (asignado a todos los nodos)** | Si se activa, **todos** los nodos, presentes y futuros, quedan asignados automáticamente | Úsalo para el PACS principal de la organización |
| **Asociaciones concurrentes** | Cuántos envíos simultáneos tolera | Si el PACS se satura, **baja este número** |
| **Timeout (seg)** | Cuánto esperar antes de dar por fallida una conexión | Súbelo si la red es lenta |

⚠️ Marcar un PACS como **global** afecta a toda la plataforma de inmediato.

✅ **Después de crear un PACS, siempre prueba la conexión** (botón de conectividad en la lista) antes de dar por terminado el trabajo.

---

# 9. Reglas de Ruteo (HL7)

> **Ruta:** `/routing-rules` · **Permiso:** `ManageRoutingRules`

🟦 **Concepto.** Cuando llega un mensaje HL7 al Hub (una orden, una admisión), hay que decidir **a qué nodo mandarlo**. Estas reglas toman esa decisión.

Se evalúan **por prioridad**, y gana la primera que coincida.

## 9.1 Lista

📸 `[IMAGEN 9.1: Lista de reglas de ruteo]`

| Columna | Qué te dice |
|---|---|
| **Prioridad** | Orden de evaluación |
| **Nombre** | Nombre descriptivo |
| **Tipo Msg** | Tipo de mensaje HL7 que busca |
| **Evento** | Trigger event que busca |
| **Facility** | Sede emisora que busca |
| **Nodo Destino** | A dónde manda el mensaje |
| **Coincidencias** | **Cuántas veces se ha aplicado.** Si dice `0` después de días, la regla no está sirviendo |
| **Última coincidencia** | Cuándo fue la última vez |

**Acciones por fila:** Editar · Habilitar/Deshabilitar · Eliminar.
**Filtros:** búsqueda y **Solo habilitadas**.

> 💡 La columna **Coincidencias** es tu mejor herramienta de diagnóstico. "El estudio no llegó a la sucursal" casi siempre se explica revisando qué regla coincidió (o ninguna).

## 9.2 Crear o editar una regla

📸 `[IMAGEN 9.2: Formulario de regla de ruteo]`

| Campo | Explicación | Obligatorio |
|---|---|---|
| **Nombre** | Descriptivo | ✅ |
| **Nodo destino** | A qué nodo se manda el mensaje | ✅ |
| **Prioridad** | Número; mayor = se evalúa antes | ✅ |
| **Tipo de mensaje** | `ADT`, `ORM`, `ORU`… Vacío = cualquiera | ❌ |
| **Trigger Event** | `A01`, `A08`, `O01`… Vacío = cualquiera | ❌ |
| **Sending Facility** | Sede que envió el mensaje | ❌ |
| **Sending Application** | Sistema que envió el mensaje (ej. el RIS) | ❌ |

**Los cuatro campos de coincidencia se combinan con Y lógico.** Si dejas todos vacíos, la regla coincide con **todo**: úsala solo como regla "atrapatodo" con la prioridad **más baja**.

✅ **Estrategia recomendada:** reglas específicas con prioridad alta, y una regla general con prioridad 0 al final para que ningún mensaje se quede sin destino.

---

# 10. Estado HL7

> **Ruta:** `/hl7` · **Permiso:** `ViewQueue`

📸 `[IMAGEN 10.1: Pantalla HL7 con pestañas]`

Pantalla con tres pestañas:

## 10.1 Listener

El "oído" HL7 del Hub: el proceso que escucha mensajes entrantes.

| Dato | Qué significa |
|---|---|
| **Estado (corriendo / detenido)** | Si está detenido, **no está entrando nada por HL7** |
| **Puerto** | En qué puerto escucha |
| **Conexiones activas** | Cuántos sistemas están conectados en este momento |

## 10.2 Reglas de enrutamiento

Mismo contenido que el [capítulo 9](#9-reglas-de-ruteo-hl7), accesible desde aquí.

## 10.3 Cola de mensajes

Contadores por etapa del pipeline, con acceso al listado de mensajes. Ver [capítulo 11](#11-cola-de-mensajes).

## 10.4 Detalle de un mensaje HL7

📸 `[IMAGEN 10.2: Detalle de mensaje HL7]`

Al abrir un mensaje ves su **contenido crudo** (el texto HL7 tal cual llegó), tipo de mensaje, aplicación y sede emisoras, endpoint del cliente, estado, cuándo se procesó y el error si lo hubo.

Algunos mensajes permiten **Reprocesar**: volver a intentar el procesamiento. Útil cuando el error fue temporal (nodo caído que ya volvió).

---

# 11. Cola de Mensajes

> **Ruta:** `/queue` · **Permiso:** `ViewQueue`

📸 `[IMAGEN 11.1: Cola de mensajes]`

🟦 **Concepto.** Cada mensaje HL7 recorre un **pipeline** de etapas. Esta pantalla te muestra cuántos mensajes hay en cada una.

## 11.1 Las etapas del pipeline

| Etapa | Etiqueta | Qué está pasando | ¿Normal? |
|---|---|---|---|
| `PendingValidation` | Pendiente validación | Recién llegó, aún no se revisa | ✅ transitorio |
| `Validated` | Validado | Pasó la revisión de formato | ✅ transitorio |
| `Routed` | Enrutado | Ya se decidió a qué nodo va | ✅ transitorio |
| `Queued` | En cola | Esperando turno de envío | ✅ transitorio |
| `Dispatching` | Despachando | Enviándose ahora | ✅ transitorio |
| `Delivered` | Entregado | Llegó al nodo. **Fin feliz** | ✅ |
| `ValidationFailed` | Validación fallida | El mensaje venía mal formado | ❌ **revisar** |
| `DeliveryFailed` | Entrega fallida | No se pudo entregar al nodo | ❌ **revisar** |

> 🔑 **Regla de oro:** las etapas transitorias deben **subir y bajar**. Si un contador se queda quieto y alto, hay un atoro.

## 11.2 Cómo usar la pantalla

✅ **Cómo hacerlo**
1. Las tarjetas de arriba son **botones**. Haz clic en la etapa que te interese.
2. Abajo aparece la tabla de mensajes en esa etapa.
3. **Cerrar** quita el filtro.

**Totales del pipeline:** *Total en pipeline* (todo lo que aún no termina), *Entregados* y *Fallidos*.

## 11.3 Columnas de la tabla de mensajes

| Columna | Contenido |
|---|---|
| **Tipo** | `ADT^A01`: tipo de mensaje ^ trigger event |
| **Paciente** | Nombre y, entre paréntesis, su ID |
| **Facility** | Sede emisora |
| **Nodo destino** | A dónde va |
| **Prioridad** | Número de prioridad |
| **Intentos** | Cuántas veces se ha intentado entregar. **Un número alto = problema persistente** |
| **Recibido** | Hace cuánto llegó |
| **Error** | *(solo en las etapas fallidas)* El texto del error |

---

# 12. Outbox

> **Ruta:** `/outbox` · **Permiso:** `ViewSystemStatus` (para actuar: `ManageQueue`)

📸 `[IMAGEN 12.1: Outbox]`

🟦 **Concepto.** El Outbox es la **bandeja de salida unificada** del Hub. Todo lo que el Hub tiene que mandar hacia afuera pasa por aquí, con reintentos automáticos.

Se manejan dos categorías:

| Categoría | Qué contiene |
|---|---|
| **Node Sync** | Configuración y órdenes que el Hub le empuja a los nodos |
| **Notificaciones** | WhatsApp y correos hacia pacientes |

## 12.1 Filtros

**Categoría** (Todas / Node Sync / Notificaciones) y **Estado** (Todos / Pending / Sent / Failed).

| Estado | Significado |
|---|---|
| `Pending` | Esperando ser enviado o reintentado |
| `Sent` | Enviado con éxito |
| `Failed` | Falló |

## 12.2 Columnas

| Columna | Qué te dice |
|---|---|
| **Topic** | Qué tipo de mensaje es |
| **Categoría** | Node Sync o Notificación |
| **Destino** | A dónde va (nodo, teléfono, correo) |
| **Estado** | Pending / Sent / Failed |
| **Intentos** | Cuántas veces se ha intentado |
| **Creado** | Cuándo se generó |
| **Procesado** | Cuándo terminó de procesarse |
| **Error** | Último error. Pasa el mouse para verlo completo |

## 12.3 Acciones

*(Solo visibles si tienes el permiso `ManageQueue`.)*

| Acción | Cuándo aparece | Qué hace |
|---|---|---|
| **Reintentar** 🔄 | En cualquier entrada que no esté `Sent` | Vuelve a intentar el envío inmediatamente |
| **Dead-letter** 🚫 | Solo en entradas `Pending` | Saca la entrada de la cola: deja de reintentarse |

⚠️ **Dead-letter cancela el envío.** Si era la notificación de resultados de un paciente, ese paciente **no la recibirá**. Úsalo solo para entradas que sabes que están mal (número telefónico inválido, nodo dado de baja).

> 🔴 La pantalla se actualiza **en tiempo real**: verás cambiar los estados sin recargar.

---

# 13. WhatsApp

> **Ruta:** `/whatsapp` · **Permiso:** `ViewConfiguration`

📸 `[IMAGEN 13.1: Pantalla WhatsApp]`

Aquí se configura cómo se le avisa al paciente por WhatsApp que su estudio está listo.

## 13.1 Panel de estado

| Indicador | Qué significa |
|---|---|
| **Habilitado** | Si el canal de WhatsApp está prendido |
| **Entrega automática** | Si se manda solo al cambiar de estado el estudio, o solo manualmente |
| **Proveedor / Configurado** | Qué servicio se usa y si tiene credenciales válidas |
| **Prefijo de país por defecto** | El código que se antepone a números sin código de país (ej. `+52`) |
| **Templates activos** | Cuántas plantillas hay listas |
| **Reglas activas** | Cuántas reglas de envío automático hay |
| **Notificaciones pendientes** | Cuántas están en cola |
| **Notificaciones fallidas** | **Si es > 0, revisa el Outbox** |

## 13.2 Pestaña "Templates"

🟦 **Concepto importante.** WhatsApp **no permite mandar texto libre** a un paciente que no te escribió primero. Hay que usar **plantillas aprobadas previamente por WhatsApp/Meta**. El texto de la plantilla **no vive en EdgeGuard**: vive del lado del proveedor. Lo que EdgeGuard guarda es un **identificador** de esa plantilla y **con qué datos rellenar sus huecos**.

📸 `[IMAGEN 13.2: Formulario de template]`

| Campo | Explicación |
|---|---|
| **Nombre** | Nombre interno para identificarla ("Estudio listo – español") |
| **Content SID** | El **identificador de la plantilla en el proveedor**. Te lo da quien aprobó la plantilla. Empieza típicamente con `HX...` |
| **Descripción** | Para qué sirve, notas internas |
| **Variables** | Los "huecos" de la plantilla, **en orden** |

### Cómo funcionan las variables

Si la plantilla aprobada dice:

> "Hola {{1}}, tu estudio {{2}} ya está disponible: {{3}}"

Entonces necesitas **tres variables**, y el **orden importa**:

| Posición | Etiqueta que eliges | Se sustituye por |
|---|---|---|
| 1 | `patientName` | El nombre del paciente |
| 2 | `studyDescription` | La descripción del estudio |
| 3 | `imageLink` | La liga para ver las imágenes |

En la interfaz, cada variable es una fila que puedes **arrastrar para reordenar**. La etiqueta se elige de un catálogo de etiquetas disponibles (el sistema muestra la descripción y un ejemplo de cada una).

⚠️ **Si el orden de las variables no coincide con la plantilla aprobada, el paciente recibirá un mensaje con los datos revueltos.** Verifica siempre contra el texto aprobado.

## 13.3 Pestaña "Reglas Auto-Send"

📸 `[IMAGEN 13.3: Formulario de regla auto-send]`

🟦 **Concepto.** Una regla dice: *"cuando un estudio llegue a este estado, manda automáticamente esta plantilla al paciente"*.

| Campo | Explicación |
|---|---|
| **Estado del estudio (trigger)** | Qué estado dispara el envío |
| **Template** | Qué plantilla se manda |
| **Descripción** | Notas internas |

**Estados disponibles como disparador:**

| Estado | Cuándo ocurre | Mensaje típico |
|---|---|---|
| **Agendado** | Se agendó la cita (llegó por HL7) | Recordatorio de cita e indicaciones |
| **Completado** | Ya se le tomaron las imágenes | "Tu estudio se realizó correctamente" |
| **Con URL de imágenes** | Ya hay liga para ver imágenes, falta reporte | "Ya puedes ver tus imágenes" |
| **Con reporte finalizado** | Liga **y** reporte listos | "Tu estudio y reporte están listos" |

Cada regla tiene un interruptor **Habilitada/Deshabilitada**.

⚠️ **Estas reglas mandan mensajes reales a pacientes reales.** Antes de habilitar una regla nueva:
1. Prueba con un **envío manual** a tu propio número.
2. Verifica que el texto y las variables salgan bien.
3. Recién entonces habilita la regla.

## 13.4 Envío manual

📸 `[IMAGEN 13.4: Diálogo de envío manual]`

| Campo | Explicación |
|---|---|
| **Estudio** | De qué estudio se trata (de ahí salen los datos de las variables) |
| **Template** | Qué plantilla usar |
| **Destinatarios** | Uno o más números telefónicos, con nombre opcional |

Al enviar, el sistema devuelve un resumen: cuántos **exitosos**, cuántos **fallidos**, y por cada número el resultado y el error si aplica. También muestra el **número normalizado** (cómo quedó después de aplicar el prefijo de país).

## 13.5 Estados de una notificación

| Estado | Significado |
|---|---|
| **Pendiente** | En cola, aún no sale |
| **Enviado** | Entregado al proveedor correctamente |
| **Fallido** | No se pudo enviar. Revisa el campo de error |
| **Omitido** | Se decidió no enviarlo (típicamente, el paciente no tiene teléfono registrado) |

También se registra si el envío fue **Automático** (por regla) o **Manual**.

---

# 14. Plantillas de Email

> **Ruta:** `/email-templates` · **Permiso:** `ViewConfiguration`

📸 `[IMAGEN 14.1: Editor de plantillas de email]`

A diferencia de WhatsApp, aquí **sí redactas el contenido completo**.

## 14.1 Estructura de la pantalla

**Izquierda:** lista de plantillas existentes (nombre, formato y asunto). Clic para editar.
**Derecha:** editor.

## 14.2 Campos del editor

| Campo | Explicación |
|---|---|
| **Nombre** | Identificación interna |
| **Formato** | `HTML` (con diseño) o `Texto plano` (sin formato) |
| **Asunto** | El asunto del correo. **También acepta etiquetas** |
| **Cuerpo** | El contenido del correo |

## 14.3 Etiquetas de sustitución (merge tags)

Debajo del asunto hay una **paleta de etiquetas** en forma de píldoras azules, tipo `{{patientName}}`.

✅ **Cómo insertarlas**
1. Coloca el cursor en el cuerpo, donde quieras el dato.
2. Haz clic en la píldora.
3. La etiqueta se inserta en esa posición exacta.

Pasa el mouse sobre una píldora para ver su descripción y un ejemplo del valor.

**Ejemplo de cuerpo:**

```html
<p>Estimado(a) {{patientName}},</p>
<p>Su estudio <strong>{{studyDescription}}</strong> realizado el
   {{studyDate}} ya se encuentra disponible.</p>
<p><a href="{{imageLink}}">Ver mis imágenes</a></p>
```

## 14.4 Previsualizar

Clic en **Previsualizar** para ver cómo queda el correo **con datos de ejemplo**. Si el formato es HTML, se renderiza; si es texto plano, se muestra tal cual.

✅ **Siempre previsualiza antes de guardar.** Una etiqueta mal escrita (`{{pacientName}}`) aparecerá literalmente en el correo del paciente.

## 14.5 Acciones

| Botón | Qué hace |
|---|---|
| **Nueva plantilla** | Limpia el editor para empezar de cero |
| **Guardar** | Crea o actualiza |
| **Previsualizar** | Vista previa con datos de ejemplo |
| **Eliminar** | Borra la plantilla ⚠️ irreversible |

---

# 15. Notificaciones

> **Ruta:** `/notification-settings` · **Permiso:** `ViewConfiguration`

📸 `[IMAGEN 15.1: Configuración de notificaciones]`

## 15.1 Modo automático

Un interruptor maestro: **entrega automática de resultados al finalizar un estudio**.

⚠️ Este es el **apagador general** de las notificaciones automáticas. Apagarlo detiene los envíos automáticos a todos los pacientes; encenderlo los reactiva. Considera apagarlo durante mantenimientos o pruebas, y **avisa al equipo** cuando lo hagas.

## 15.2 SMTP (Email)

Muestra el estado del servidor de correo saliente:

| Dato | Qué es |
|---|---|
| **Habilitado / Deshabilitado** | Si el envío de correo está activo |
| **Host** | Servidor y puerto de correo saliente |
| **From** | La dirección desde la que salen los correos |

> ℹ️ Estos valores **no se editan desde la interfaz**: se definen en la configuración del Hub (`appsettings`). Si necesitas cambiarlos, es tarea de TI.

## 15.3 Enviar correo de prueba

✅ **Cómo hacerlo**
1. Escribe un correo en el campo de texto.
2. Clic en **Enviar prueba**.
3. Revisa la bandeja (y la carpeta de spam) del destinatario.

El botón está deshabilitado si SMTP está apagado.

---

# 16. Usuarios, roles y permisos

> **Ruta:** `/users` · **Permiso:** `ViewUsers`

## 16.1 Lista de usuarios

📸 `[IMAGEN 16.1: Lista de usuarios]`

| Columna | Qué te dice |
|---|---|
| **Usuario** | Nombre de inicio de sesión |
| **Nombre** | Nombre completo |
| **Roles** | Qué roles tiene asignados |
| **Estado** | Activo / Inactivo |
| **Bloqueo** | Si la cuenta está bloqueada (por intentos fallidos) |
| **Último login** | Cuándo entró por última vez |

## 16.2 Crear un usuario

📸 `[IMAGEN 16.2: Formulario de usuario]`

| Campo | Explicación |
|---|---|
| **Nombre de usuario** | Con el que inicia sesión. **No se puede cambiar después** |
| **Nombre completo** | Como aparecerá en la interfaz y en Auditoría |
| **Contraseña** | Contraseña inicial |
| **Roles** | Uno o varios roles |

⚠️ Entrega la contraseña inicial por un canal seguro y pide que se cambie.

## 16.3 Roles disponibles

| Rol | Perfil típico |
|---|---|
| **Administrador** | Control total del sistema |
| **Operador** | Opera el día a día: estudios, cola, reenvíos |
| **Visor** | Solo lectura |
| **Técnico** | Personal de sala; consulta estudios y equipos |
| **Auditor** | Acceso a bitácoras de auditoría |
| **Gerente** | Consulta métricas y reportes |
| **Cuenta de Servicio** | Para integraciones automatizadas, no para personas |
| **Soporte** | Diagnóstico y resolución de incidentes |

## 16.4 Catálogo de permisos

📸 `[IMAGEN 16.3: Panel de permisos por rol]`

Los permisos son **la unidad mínima** de autorización. Los roles agrupan permisos, y además puedes otorgar **permisos directos** a un usuario específico.

### Estudios
| Permiso | Permite |
|---|---|
| `ViewStudies` | Ver estudios (**y pacientes**) |
| `SendStudies` | Enviar estudios a PACS |
| `DeleteStudies` | Eliminar estudios |
| `ArchiveStudies` | Archivar estudios |
| `ExportStudies` | Exportar estudios |
| `EditStudyMetadata` | Editar los datos del estudio |
| `AnonymizeStudies` | Anonimizar |

### Cola
| Permiso | Permite |
|---|---|
| `ViewQueue` | Ver la cola HL7 y el estado HL7 |
| `ManageQueue` | Gestionar la cola; **reintentar y dead-letter en Outbox** |
| `RetryTransfers` | Reintentar transferencias |
| `CancelTransfers` | Cancelar transferencias |

### Configuración
| Permiso | Permite |
|---|---|
| `ViewConfiguration` | Ver PACS, WhatsApp, plantillas, notificaciones, configuración |
| `EditConfiguration` | Modificarlas |
| `ManageModalities` | Gestionar catálogo de modalidades |
| `ManageRoutingRules` | Gestionar reglas de ruteo |
| `ManageStorage` | Gestionar almacenamiento |
| `ManageNetwork` | Gestionar parámetros de red |

### Monitoreo
| Permiso | Permite |
|---|---|
| `ViewMetrics` | Ver métricas |
| `ViewAuditLogs` | Ver la bitácora de auditoría |
| `ViewSystemStatus` | Ver estado del sistema y **Outbox** |
| `ExportReports` | Exportar reportes |
| `ManageAlerts` | Gestionar alertas |

### Usuarios
| Permiso | Permite |
|---|---|
| `ViewUsers` | Ver usuarios |
| `ManageUsers` | Crear/editar usuarios |
| `ManageRoles` | Gestionar roles |
| `GrantPermissions` | Otorgar permisos directos |

### Nodos
| Permiso | Permite |
|---|---|
| `ViewNodes` | Ver nodos |
| `ManageEdgeNodes` | Crear/editar/deshabilitar nodos |
| `RestartNodes` | Reiniciar nodos |
| `UpdateNodes` | Actualizar nodos |

### Sistema
| Permiso | Permite |
|---|---|
| `SystemBackup` | Respaldos |
| `SystemRestore` | Restauración |
| `ViewSystemLogs` | Ver logs del sistema |
| `SystemMaintenance` | Mantenimiento |
| `DatabaseOperations` | Operaciones de base de datos |

⚠️ Los permisos de **Sistema** son de altísimo impacto. Restringirlos a administradores.

## 16.5 Restablecer contraseña

📸 `[IMAGEN 16.4: Diálogo de restablecer contraseña]`

Campos: **Nueva contraseña** y **Confirmar contraseña**. Es la vía para desatorar a un usuario que olvidó la suya o quedó bloqueado.

---

# 17. Configuración del sistema

> **Ruta:** `/settings` · **Permiso:** `ViewConfiguration`

📸 `[IMAGEN 17.1: Configuración global]`

Ajustes globales del Hub, organizados en paneles plegables por categoría:

| Categoría | Qué contiene |
|---|---|
| **General** | Parámetros generales de la plataforma |
| **HL7** | Puerto del listener, validaciones, timeouts |
| **Despacho** | Reglas de envío a nodos: reintentos, ventanas |
| **Cola** | Tamaños de lote, concurrencia |
| **WhatsApp** | Credenciales del proveedor, prefijo de país, activación |
| **SMTP (Email)** | Parámetros de correo saliente |
| **Tareas en segundo plano** | Frecuencia de los procesos automáticos |
| **Retención de datos** | Cuánto tiempo se conservan estudios, logs y mensajes |

## 17.1 Anatomía de un ajuste

Cada ajuste muestra:

| Elemento | Qué es |
|---|---|
| **Nombre para mostrar** | Nombre legible |
| **Clave** | El identificador técnico. Cítalo cuando pidas ayuda |
| **Valor** | El valor actual, editable según su tipo |
| **Tipo de valor** | Texto, número, booleano, lista… El editor se adapta |
| **Descripción** | Para qué sirve |
| **Solo lectura** | Algunos ajustes están bloqueados por seguridad y no se pueden editar desde la interfaz |

⚠️ **Regla práctica:** si no entiendes qué hace un ajuste después de leer su descripción, **no lo cambies**. Muchos afectan a todos los nodos a la vez.

## 17.2 Configuración por nodo

Desde el botón **Config. de nodos** entras a una vista donde eliges un **Nodo** y editas sus valores específicos, que **sobrescriben** los globales.

| Indicador | Significado |
|---|---|
| **Sobrescrito** | Ese nodo tiene un valor propio, distinto del global |
| **Restablecer** | Quita el valor propio y regresa al global |

Aplica el mismo flujo de **Guardar** → **Aplicar al nodo** descrito en la [sección 7.6](#76-configuración-del-nodo).

---

# 18. Auditoría

> **Ruta:** `/audit` · **Permiso:** `ViewAuditLogs`

📸 `[IMAGEN 18.1: Bitácora de auditoría]`

🟦 **Concepto.** La bitácora registra **quién hizo qué y cuándo**. En salud esto no es opcional: es un requisito regulatorio y la única forma de reconstruir qué pasó cuando algo sale mal.

## 18.1 Columnas

| Columna | Qué te dice |
|---|---|
| **Evento** | Categoría del suceso |
| **Acción** | Qué se hizo específicamente |
| **Severidad** | Ver tabla 18.2 |
| **Usuario** | Quién lo hizo |
| **Resultado** | Si la operación fue exitosa o falló |
| **Fecha** | Cuándo |

**Filtros:** **Severidad** y **Tipo de evento**.

## 18.2 Niveles de severidad

| Nivel | Etiqueta | Cuándo se usa |
|---|---|---|
| `Information` | Información | Operación normal registrada |
| `Warning` | Advertencia | Algo inusual pero no roto |
| `Error` | Error | Una operación falló |
| `Critical` | Crítico | Falla grave. **Requiere atención inmediata** |

## 18.3 Detalle de un registro

📸 `[IMAGEN 18.2: Detalle de auditoría]`

Al abrir un registro ves:

| Campo | Para qué sirve |
|---|---|
| **Usuario e IP** | Quién y desde dónde |
| **Correlation ID** | Identificador que **enlaza todas las operaciones de una misma acción**. Es el dato más valioso para soporte |
| **Entidad afectada** | Tipo e ID del objeto modificado |
| **Mensaje de error** | Si falló |
| **Detalles** | Información técnica completa |

El botón **Copiar** copia el registro al portapapeles.

✅ **Al reportar un incidente, incluye siempre el Correlation ID.** Reduce dramáticamente el tiempo de diagnóstico.

---

# 19. Apéndices

## 19.1 Guía rápida de resolución de problemas

| Síntoma | Dónde mirar primero | Causa habitual |
|---|---|---|
| "El estudio no llegó al PACS" | Estudios → detalle → *Infraestructura* → **Último error**; luego Nodos → **Conectividad PACS** | AE Title mal escrito, PACS apagado, firewall |
| "El nodo aparece Offline" | Nodos → **Último Heartbeat** | Servicio del nodo detenido, red caída |
| "El paciente no recibió su WhatsApp" | Paciente → ¿tiene **Teléfono**? → Outbox filtrado por *Notificaciones* / *Failed* | Sin teléfono registrado, número inválido, regla auto-send deshabilitada |
| "No entran órdenes / worklist" | HL7 → **Listener** (¿corriendo?) → Cola de Mensajes | Listener detenido, puerto bloqueado |
| "Los mensajes se quedan en cola" | Cola de Mensajes → etapa con más acumulación | Nodo destino caído, sin regla de ruteo que coincida |
| "Un estudio quedó Fallido" | Estudios → detalle → **Intentos PACS** y **Último error** | Rechazo del PACS, timeout |
| "No veo una sección del menú" | Usuarios → tus roles | Falta de permisos (no es un error) |
| "La tabla sale vacía" | El **número azul** en el botón Filtros | Filtro olvidado |
| "Los datos no se actualizan solos" | Dashboard → indicador **En vivo** | Se perdió la conexión en tiempo real; recarga |

## 19.2 Qué incluir al levantar un ticket

Copia y pega esta plantilla:

```
Pantalla:              (ej. Estudios → detalle)
Fecha y hora del caso:
Usuario que lo detectó:
Study Instance UID / Accession:
Nodo involucrado:
PACS destino:
Estado actual del estudio:
Texto exacto del error:
Correlation ID (de Auditoría):
Pasos para reproducir:
```

## 19.3 Buenas prácticas

**Diarias**
- Revisa el KPI **Fallidos** del Dashboard al iniciar el turno.
- Verifica que todos los nodos estén **En línea**.
- Revisa el Outbox filtrado por **Failed**.

**Semanales**
- Revisa la telemetría de cada nodo: la **tasa de aceptación** debe estar cerca de 100%.
- Revisa la columna **Coincidencias** de las reglas de ruteo: las que estén en 0 sobran o están mal.
- Revisa Auditoría filtrando por severidad **Error** y **Crítico**.

**Antes de cualquier cambio de configuración**
- Anota el valor anterior.
- Haz el cambio en horario de baja actividad.
- Verifica el efecto antes de irte.

**Siempre**
- Los datos de pacientes son confidenciales: los CSV exportados también.
- No compartas usuarios. Todo queda registrado con tu nombre.
- Ante la duda en una acción marcada con ⚠️, pregunta antes de hacerla.

## 19.4 Mapa rápido: "necesito hacer X, ¿a dónde voy?"

| Necesito… | Voy a… |
|---|---|
| Buscar el estudio de un paciente | **Estudios** → buscar por accession o nombre |
| Ver si un estudio ya llegó al archivo | **Estudios** → detalle → estado *Enviado a PACS* |
| Reenviar un estudio al PACS | **Estudios** → detalle → **Reenviar** |
| Agregar el teléfono de un paciente | **Pacientes** → detalle → **Editar** |
| Saber por qué un nodo no responde | **Nodos** → detalle → *Conectividad PACS* y *Salud* |
| Dar de alta un tomógrafo nuevo | **Nodos** → detalle → **Equipos** → nuevo |
| Dar de alta un PACS nuevo | **Servidores PACS** → nuevo |
| Cambiar a qué nodo se mandan las órdenes de una sede | **Reglas de Ruteo** |
| Ver si los mensajes HL7 están entrando | **Estado HL7** → *Listener* |
| Reintentar una notificación fallida | **Outbox** → filtrar *Failed* → **Reintentar** |
| Cambiar el texto de un correo a pacientes | **Plantillas Email** |
| Apagar todas las notificaciones automáticas | **Notificaciones** → *Modo automático* |
| Dar de alta a un compañero nuevo | **Usuarios** → nuevo |
| Saber quién borró algo | **Auditoría** |

---

## Historial de versiones del documento

| Versión | Fecha | Cambios |
|---|---|---|
| 1.0 | 2026-08-02 | Versión inicial |
