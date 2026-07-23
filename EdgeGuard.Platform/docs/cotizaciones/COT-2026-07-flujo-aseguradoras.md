# Cotización — Flujo de Aseguradoras con Confirmación de Pago

| Campo | Valor |
|---|---|
| **Folio** | COT-2026-07-ASEG-01 |
| **Fecha** | 2026-07-15 |
| **Módulo** | EdgeGuard / Dicom.Edge.Hub — Ingesta HL7 + Notificaciones |
| **Solicitante** | Uriel Avila |
| **Tipo** | Ajuste evolutivo (nueva funcionalidad + catálogo) |
| **Rama base sugerida** | `feat/insurance-payment-gate` (a partir de `main`) |

---

## 1. Contexto y objetivo

Hoy el Hub recibe mensajes HL7 (`ORM^O01`, `ORU^R01`, `ADT`) por el listener TCP, los
parsea en `Hl7Message` (segmentos MSH/PID/OBR/OBX/MRG) y sincroniza estudios. Cuando un
estudio llega a **`Finalized`** (tiene liga de imágenes **y** reporte), se dispara la
entrega de resultados de forma **automática** (por reglas de auto-envío) o **manual**
(diálogo de entrega), por WhatsApp y/o Email mediante el outbox unificado de
notificaciones.

**Lo que NO existe hoy:**

- No se parsea el segmento **`IN1`** (Insurance) ni ningún mensaje específico de aseguradora.
- No existe un **catálogo de aseguradoras** ni de estatus de cobertura/pago.
- No hay ninguna **compuerta (gate) de pago**: los resultados se entregan en cuanto el
  estudio se finaliza, sin considerar si la aseguradora requiere confirmación de pago.

**Objetivo del ajuste:** incorporar un flujo de aseguradoras que, a partir de la
información recibida vía HL7 (`IN1` u otro mensaje de aseguradora), capture **código,
descripción y estatus** de la cobertura, determine si **requiere confirmación de pago**
y, en ese caso, **retenga la entrega de resultados** hasta que el pago sea confirmado —
disparando en el proceso la mensajería correspondiente (WhatsApp/Email). Si **no** requiere
confirmación, el estudio sigue el **flujo actual** sin cambios de comportamiento.

---

## 2. Alcance funcional

### 2.1 Catálogo de aseguradoras (nuevo)
- Nueva entidad/agregado **`Insurer` (Aseguradora)** con: `Code`, `Description`, `Status`
  (Activa/Inactiva), y bandera **`RequiresPaymentConfirmation`** usada como **default/fallback**
  (ver §2.2 — el valor autoritativo viaja en el `IN1`).
- **Dos canales de mantenimiento del catálogo:**
  - **UI:** CRUD (alta, edición, activación/desactivación).
  - **HL7 `BAR^P01` (Add/Change Billing Account) — nueva integración:** upsert del catálogo
    a partir del `IN1` del mensaje (**alta** si el `IN1-3` no existe, **edición** si existe).
    Ver Anexo A.3.
- Mapeo de códigos HL7 entrantes (`IN1-3` / identificador de plan) → registro de catálogo.

### 2.2 Parseo del mensaje de aseguradora (HL7)
- Extraer de `IN1` (o del mensaje específico acordado) los campos:
  - **Código** de aseguradora/plan → `IN1-3` (o el campo que defina el emisor).
  - **Descripción** → `IN1-4` (Insurance Company Name) o `IN1-2`.
  - **Estatus** de cobertura/autorización → campo acordado (p. ej. `IN1-15`, `IN1-45`
    o un `ZXX` custom del emisor).
  - **¿Requiere confirmación de pago?** → **viene en el propio `IN1`** como una bandera
    `Y`/`N` en un campo acordado (recomendado `IN1-45` componente 2). Es el **valor
    autoritativo por mensaje**; el catálogo solo se usa como **default** cuando la bandera
    no está presente en el mensaje.
- Persistir estos datos ligados al **estudio** (por AccessionNumber).
- **Relación paciente–aseguradora 1:1 (explícito).** Cada **paciente se relaciona con una
  sola aseguradora**. El vínculo vive en el **paciente**; un nuevo `IN1` con una aseguradora
  distinta **reemplaza** la anterior (no se acumulan). El estudio toma la aseguradora del
  paciente al momento de la evaluación.

### 2.3 Compuerta de pago (gate) sobre la entrega
- Nuevo estado de pago a nivel estudio: **`PaymentStatus`** = `NotRequired` |
  `PendingConfirmation` | `Confirmed` | `Rejected`.
- Al finalizar el estudio:
  - Si la aseguradora **no requiere** confirmación → entrega inmediata (**flujo actual**).
  - Si **requiere** confirmación → el estudio queda **retenido** (`PendingConfirmation`);
    **no** se enqueuen las notificaciones automáticas de resultados hasta confirmar el pago.
- El gate aplica **solo a la entrega automática**. La entrega **manual** no se altera (ver §2.6).

**Independencia de orden (idempotente).** La confirmación de pago (por `DFT^P03` o UI) se
**guarda en el estudio en cualquier momento**, sin importar su estado actual. El bit de pago
persiste por separado y la evaluación del gate ocurre **al llegar a `Finalized`**:

- Si la confirmación llega **antes** de `Finalized` (p. ej. estudio en `Scheduled`,
  `Receiving`, `Completed`): **solo se guarda** el bit `PaymentStatus.Confirmed` y se espera.
  Al alcanzar `Finalized`, como el bit **ya está puesto**, se **encola automáticamente**
  (sin retención).
- Si la confirmación llega **después** de `Finalized` (estudio retenido en
  `PendingConfirmation`): al confirmarse, se **libera y se encola** en ese momento.
- No depende de la secuencia de llegada de mensajes (`ORM` / `ORU` / `DFT`) ni de la acción
  de UI; el resultado final es el mismo.

### 2.4 Confirmación de pago — canales
La confirmación converge en la misma transición de dominio (`PaymentStatus.Confirmed` →
libera la entrega por el trigger `Finalized`), desde dos canales:

- **HL7 `DFT^P03` (Detail Financial Transaction) — nueva integración.** Es el tipo de
  mensaje estándar de HL7 v2 para transacciones financieras (cargos/pagos/ajustes). Se
  procesa el segmento **`FT1`**:
  - `FT1-6` Transaction Type = **`PY`** (Payment) identifica el pago.
  - `FT1-7` Transaction Code → concepto del pago (opcional). _(No se consume el monto — `FT1-11` se omite.)_
  - `FT1-4` Transaction Date + clave de emparejamiento con el estudio (AccessionNumber /
    PatientId / código de procedimiento) — **a confirmar en kickoff**.
  - Requiere **habilitar `DFT` en el pipeline/listener HL7** (hoy solo procesa ORM/ORU/ADT)
    y un **handler dedicado** que marque `PaymentStatus.Confirmed` y libere la entrega.
- **UI:** acción manual **"Confirmar pago"** en el estudio (mismo efecto: Confirmed → libera).

### 2.5 Triggers de mensajería (sin cambios al mecanismo actual)
- **No se crean triggers ni plantillas nuevas.** La mensajería sigue usando el trigger
  actual de entrega de resultados: **`Finalized`**.
- El gate de pago **solo difiere el encolado**: mientras el estudio esté en
  `PendingConfirmation` no se encola la notificación; al confirmarse el pago se libera y se
  **encola la entrega por el flujo actual** (mismo trigger `Finalized`, mismas plantillas
  WhatsApp/Email, mismo outbox).
- No se agregan valores nuevos a `NotificationTriggerSource`.

### 2.6 Entrega manual (sin cambios — a discreción del usuario)
- La **entrega manual** de resultados (diálogo de entrega) **no se altera**: el usuario
  puede entregar en cualquier momento, **aun con el pago en `PendingConfirmation`**.
- El gate de pago **nunca bloquea** la acción manual; solo difiere la **entrega automática**.

### 2.7 Flexibilidad
- Todo el gate es **condicional**: si la bandera del `IN1` (o su default en catálogo) indica
  `N`, el comportamiento es **idéntico al actual**.
- La decisión viaja **en el `IN1`** (por aseguradora, por mensaje); el catálogo aporta el
  **default**. No hay override global ni interruptor en la configuración de notificaciones.

---

## 3. Análisis de impacto técnico

### 3.1 Diagrama del cambio de flujo

```
                       ORU^R01 → Study Finalized
                                  │
                    ┌─────────────┴──────────────┐
                    │  ¿Aseguradora requiere      │
                    │  confirmación de pago?      │
                    └─────────────┬──────────────┘
             NO ──────────────────┤────────────────── SÍ
              │                    │                    │
   Flujo ACTUAL (sin cambios)      │        Estudio → PaymentStatus.PendingConfirmation
   StudyAutoDeliveryHandler        │        Entrega AUTOMÁTICA retenida (no se encola)
   → DeliveryService               │        (entrega manual sigue disponible)
   → Outbox (WhatsApp/Email)       │                    │
   trigger = Finalized             │        Confirmación de pago
                                   │        (HL7 DFT^P03 · FT1  /  UI)
                                   │                    │
                                   │        PaymentStatus.Confirmed
                                   └──────► Libera → encola por trigger Finalized
                                            (flujo ACTUAL, sin plantilla nueva)
```

### 3.2 Componentes afectados / nuevos

| Capa | Archivo / componente | Cambio |
|---|---|---|
| Domain | **`Insurer` (nuevo agregado)** | Catálogo aseguradoras: Code/Description/Status/RequiresPaymentConfirmation |
| Domain | [Hl7Message.cs](src/backend/Dicom.Edge.Hub.Domain/Entities/Hl7Message.cs) | Parseo de `IN1` (código, descripción, estatus) — nuevos campos + extractores |
| Domain | [Study.cs](src/backend/Dicom.Edge.Hub.Domain/Aggregates/Studies/Study.cs) | Nuevo `PaymentStatus` + datos de aseguradora + transiciones/eventos |
| Application | [Hl7StudySyncService.cs](src/backend/Dicom.Edge.Hub.Application/Hl7/Hl7StudySyncService.cs) | Ligar aseguradora al estudio desde `IN1`; **leer la bandera de confirmación del `IN1`** (fallback al default del catálogo) |
| Application | **`Hl7PaymentSyncService` (nuevo)** | Procesar `DFT^P03`: parsear `FT1` (Type=`PY`, sin `FT1-11`), emparejar estudio, confirmar pago y liberar entrega |
| Application | **`InsurerCatalogSyncService` (nuevo)** | Procesar `BAR^P01`: parsear `IN1`, **upsert** del catálogo (alta/edición por `IN1-3`) |
| Application | [Hl7ValidationConstants.cs](src/backend/Dicom.Edge.Hub.Application/Constants/Hl7ValidationConstants.cs) + pipeline HL7 | Registrar/aceptar los tipos de mensaje **`DFT`** y **`BAR`** en el listener/validación (hoy ORM/ORU/ADT) |
| Domain | **`Patient` (aggregate)** | Vínculo **1:1** con aseguradora (`InsurerCode`/`InsurerName`); un `IN1` nuevo reemplaza el previo |
| Application | [DeliveryService.cs](src/backend/Dicom.Edge.Hub.Application/Notifications/DeliveryService.cs) | **Sin cambios** — la entrega manual queda a discreción del usuario |
| Infra | [StudyAutoDeliveryHandler.cs](src/backend/Dicom.Edge.Hub.Infrastructure/EventHandlers/StudyAutoDeliveryHandler.cs) | Bifurcar la entrega **automática**: si requiere pago, retener (no encolar); si no, entrega directa (flujo actual) |
| Application | **`InsurerService` / `PaymentConfirmationService` (nuevos)** | CRUD catálogo + confirmar/rechazar pago (UI y HL7 convergen aquí; libera entrega) |
| Persistence | **Configuración EF + Migración** | Tabla `insurers`, columnas de aseguradora/PaymentStatus en `studies` |
| Contracts | [WhatsAppDtos.cs](src/shared/Dicom.Edge.Contracts/WhatsApp/WhatsAppDtos.cs) y DTOs de entrega | DTOs de catálogo, estatus de pago, plantillas |
| API | Controllers de Studies/Notifications | Endpoints: catálogo aseguradoras, confirmar pago, consulta estatus |
| Frontend | UI Angular (`whatsapp`, `studies`) | Pantalla catálogo aseguradoras, indicador de gate en estudio, acción "confirmar pago", plantilla nueva |

### 3.3 Migración de base de datos
- Nueva tabla **`insurers`** (catálogo).
- Nuevas columnas en `patients`: `InsurerCode`, `InsurerName` — **vínculo 1:1** paciente↔aseguradora.
- Nuevas columnas en `studies`: `InsurerCode`, `InsurerName`, `InsuranceStatus` (snapshot del
  paciente), `PaymentStatus`, `PaymentConfirmedAt`, `PaymentConfirmedBy`.
- Migración EF Core aditiva (sin ruptura de datos existentes; `PaymentStatus` default
  `NotRequired` para estudios previos → preserva flujo actual).

---

## 4. Supuestos

1. El emisor HL7 entrega la información de aseguradora en el segmento **`IN1`** de un
   `ORM^O01` (o en un mensaje/segmento específico a confirmar en kickoff). **Se requiere
   una muestra real del mensaje** para fijar los índices de campo exactos (código,
   descripción, estatus, bandera de confirmación).
2. La **confirmación de pago** se cotiza por **dos canales en el alcance base**: (a) HL7
   **`DFT^P03`** (segmento `FT1`) y (b) acción manual en la **UI**. Se requiere una **muestra
   real del `DFT`** para fijar los índices de `FT1` y la **clave de emparejamiento** con el
   estudio (AccessionNumber / PatientId / procedimiento). El webhook de pasarela externa
   queda como opcional (ver §6).
3. La entrega reutiliza las plantillas WhatsApp/Email **existentes** del flujo `Finalized`;
   no se crean ni se aprueban plantillas nuevas ante Meta/Twilio.
4. El comportamiento sin confirmación es **idéntico** al actual (no hay regresión). La
   entrega **manual** de resultados no se altera en ningún caso.

---

## 5. Desglose de esfuerzo

Estimación en **jornadas ideales de desarrollo (JID = 8 h)**. Incluye implementación,
pruebas unitarias y ajuste de documentación técnica.

**Tarifa aplicada:** **$162.50 MXN/hora** → **$1,300.00 MXN/JID (8 h)**.

| # | Entregable | Backend | Frontend | JID | Horas | Costo (MXN) |
|---|---|---:|---:|---:|---:|---:|
| 1 | Catálogo de aseguradoras (domain, EF, migración, CRUD API + UI) | 2.0 | 1.5 | 3.5 | 28 | $4,550.00 |
| 2 | **Integración HL7 `BAR^P01` (nueva):** pipeline/listener + upsert del catálogo (alta/edición) | 2.0 | — | 2.0 | 16 | $2,600.00 |
| 3 | Parseo `IN1` + bandera de confirmación + **vínculo 1:1 paciente↔aseguradora** | 2.0 | — | 2.0 | 16 | $2,600.00 |
| 4 | `PaymentStatus` en `Study` + eventos + transiciones | 1.5 | — | 1.5 | 12 | $1,950.00 |
| 5 | Gate de entrega automática: diferir encolado (AutoDeliveryHandler) | 1.5 | — | 1.5 | 12 | $1,950.00 |
| 6 | **Integración HL7 `DFT^P03` (nueva):** pipeline/listener + parseo `FT1` + handler que confirma pago y libera | 2.5 | — | 2.5 | 20 | $3,250.00 |
| 7 | Confirmación de pago vía UI (libera → reusa trigger `Finalized`) | 0.5 | 1.5 | 2.0 | 16 | $2,600.00 |
| 8 | Indicadores en UI de estudios (estatus aseguradora / pago) | — | 1.5 | 1.5 | 12 | $1,950.00 |
| 9 | Pruebas end-to-end + QA (incluye `BAR`, `DFT` y flujo condicional) | 2.0 | 0.5 | 2.5 | 20 | $3,250.00 |
| | **Subtotal** | **14.0** | **5.0** | **19.0** | **152** | **$24,700.00** |
| | Gestión / kickoff / buffer de riesgo (~15%) | | | 3.0 | 24 | $3,900.00 |
| | **Total estimado** | | | **22.0** | **176** | **$28,600.00** |

> **Total del proyecto base: $28,600.00 MXN** (176 h × $162.50/h), sin IVA.
> _Incluye las integraciones HL7 `BAR^P01` (catálogo) y `DFT^P03` (confirmación de pago). La
> entrega manual no se altera y la entrega automática reutiliza el trigger `Finalized`._

---

## 6. Opcionales (no incluidos en el total base)

| Opcional | Descripción | JID | Horas | Costo (MXN) |
|---|---|---:|---:|---:|
| A | **Webhook** de pasarela de pago externa (Stripe/Conekta/etc.) con validación de firma | 2.5 | 20 | $3,250.00 |
| B | **Reintentos/expiración** del gate (auto-cancelar si no se confirma en N horas) | 1.5 | 12 | $1,950.00 |
| C | Reporte/tablero de estudios retenidos por pago | 1.5 | 12 | $1,950.00 |
| | **Total opcionales (si se toman los 3)** | **5.5** | **44** | **$7,150.00** |

> _La confirmación de pago por HL7 (`DFT^P03`) se movió al alcance base a solicitud del cliente._

---

## 7. Riesgos y consideraciones

- **Formato del mensaje de aseguradora (`IN1`):** los índices exactos dependen del emisor.
  Sin muestra real, el ítem 2 puede variar ±0.5 JID. Se fija en kickoff.
- **Formato del `DFT^P03` y clave de emparejamiento:** la vinculación del pago con el estudio
  (por AccessionNumber, PatientId o procedimiento en `FT1`) depende de cómo lo emita el
  origen. Sin muestra real, el ítem 5 puede variar ±0.5 JID. Se fija en kickoff.
- **Cumplimiento/PHI:** los mensajes de resultados reutilizan las plantillas actuales; el
  `DFT` de pago no debe transportar datos clínicos sensibles.
- **Compatibilidad hacia atrás:** estudios existentes quedan en `PaymentStatus.NotRequired`
  → sin cambio de comportamiento (mitiga riesgo de regresión). La **entrega manual** se
  mantiene intacta en todos los casos.

---

## 8. Entregables

1. Catálogo de aseguradoras operativo (backend + UI + migración).
2. **Integración HL7 `BAR^P01`** para alta/edición del catálogo (upsert por `IN1-3`).
3. Parseo de aseguradora desde HL7 (`IN1`) ligado al estudio, con **vínculo 1:1 paciente↔aseguradora**.
4. Gate de **entrega automática** condicional con estados de pago auditables (entrega manual intacta).
5. Reutilización del trigger `Finalized` actual (sin triggers ni plantillas nuevas).
6. **Integración HL7 `DFT^P03`** que confirma el pago y libera la entrega (`FT1-11` omitido).
7. Confirmación de pago vía **UI** que libera la entrega.
8. Pruebas automatizadas del flujo con y sin confirmación (incluye `BAR`, `DFT` y llegada en cualquier orden).
9. Documentación técnica del flujo + **especificación de mensajes `IN1`, `DFT^P03` y `BAR^P01`** (Anexo A).

---

## 9. Cronograma sugerido

| Semana | Actividades |
|---|---|
| 1 | Kickoff (muestras HL7 `IN1`/`DFT`/`BAR`), catálogo, integración `BAR^P01`, parseo `IN1` + vínculo 1:1 |
| 2 | `PaymentStatus`, gate de entrega automática, integración HL7 `DFT^P03`, confirmación por UI |
| 3 | Indicadores UI, QA end-to-end (incluye `BAR` y `DFT`), documentación y entrega |

> Estimado: **≈ 22 JID (~3–3.5 semanas)** con 1 desarrollador full-stack, o **~2 semanas**
> con backend + frontend en paralelo.

---

## Anexo A — Documentación de mensajes HL7

> Ejemplos de referencia (HL7 v2.5.1). Los índices de campo y la **clave de emparejamiento**
> se confirman en kickoff contra una **muestra real** del emisor. `|` = campo, `^` =
> componente, `~` = repetición.

### A.1 `IN1` — Datos de aseguradora (dentro de un `ORM^O01`)

Mensaje de ejemplo:

```
MSH|^~\&|RIS|HOSP|EDGEHUB|EDGE|20260715090000||ORM^O01|MSG1001|P|2.5.1
PID|1||PAT12345^^^HOSP^MR||PEREZ^JUAN||19800101|M
IN1|1|PLAN-GMM^Gastos Médicos Mayores|GNP|Grupo Nacional Provincial|||||||||||PEREZ^JUAN|SELF|19800101||||||||||||||||||POL-778899||||||||AUTORIZADO^Y
ORC|NW|ORD-555|||||^^^20260715
OBR|1|ORD-555|ACC-000123|CT-ABD^TC Abdomen^L|||20260715
```

Campos consumidos del segmento `IN1`:

| Campo | Nombre HL7 | Uso en EdgeGuard | Ejemplo |
|---|---|---|---|
| `IN1-2` | Insurance Plan ID | Código de plan (opcional) | `PLAN-GMM^Gastos Médicos Mayores` |
| `IN1-3` | Insurance Company ID | **Código de aseguradora** → llave al catálogo | `GNP` |
| `IN1-4` | Insurance Company Name | **Descripción** de la aseguradora | `Grupo Nacional Provincial` |
| `IN1-36` | Policy Number | Nº de póliza (opcional, auditoría) | `POL-778899` |
| `IN1-45.1` | Verification Status (comp. 1) | **Estatus** de cobertura/autorización | `AUTORIZADO` |
| `IN1-45.2` | *(componente acordado)* | **¿Requiere confirmación de pago?** `Y`/`N` | `Y` |

- **AccessionNumber** del estudio se toma de `OBR-3` (flujo actual), y liga el `IN1` al estudio.
- **La bandera de confirmación viaja en el `IN1`** (ejemplo: `IN1-45` componente 2 = `Y`).
  Es el **valor autoritativo por mensaje**; si el `IN1` no la trae, se usa el **default del
  catálogo** por `IN1-3` (§2.1). El campo/encoding exacto se confirma en kickoff.

### A.2 `DFT^P03` — Confirmación de pago (nueva integración)

`DFT^P03` (Detail Financial Transaction) es el mensaje HL7 v2 estándar para transacciones
financieras. El detalle del pago viaja en el segmento **`FT1`**.

Mensaje de ejemplo:

```
MSH|^~\&|BILLING|HOSP|EDGEHUB|EDGE|20260715123000||DFT^P03|MSG2001|P|2.5.1
EVN|P03|20260715123000
PID|1||PAT12345^^^HOSP^MR||PEREZ^JUAN||19800101|M
PV1|1|O
FT1|1|ACC-000123|BATCH-01|20260715123000|20260715123000|PY|PYMT^Pago confirmado^L
```

Campos consumidos del segmento `FT1`:

| Campo | Nombre HL7 | Uso en EdgeGuard | Ejemplo |
|---|---|---|---|
| `FT1-1` | Set ID | Identificador de la transacción | `1` |
| `FT1-2` | Transaction ID | **Clave de emparejamiento** con el estudio (recomendado = AccessionNumber) | `ACC-000123` |
| `FT1-4` | Transaction Date | Fecha/hora del pago | `20260715123000` |
| `FT1-6` | Transaction Type | **`PY` = Payment** → dispara `PaymentStatus.Confirmed` | `PY` |
| `FT1-7` | Transaction Code | Concepto/código del pago (opcional) | `PYMT^Pago confirmado^L` |

> **`FT1-11` (Transaction Amount) se omite** — el gate solo requiere el hecho del pago
> (`FT1-6 = PY`), no el importe. No se parsea ni se persiste el monto.

**Semántica de procesamiento:**

1. Se reconoce un pago cuando **`FT1-6 = PY`** (opcionalmente restringido a un `FT1-7` acordado).
2. El estudio se localiza por la **clave de emparejamiento** (`FT1-2`); si no existe aún, el
   pago se **guarda pendiente de asociación** y se aplica cuando el estudio aparezca
   (coherente con la independencia de orden, §2.3).
3. Al asociarse, se marca `PaymentStatus.Confirmed`; si el estudio ya está `Finalized`, se
   **libera y encola** de inmediato; si no, se espera al `Finalized` (auto-encolado).
4. **Emparejamiento:** HL7 `DFT` no tiene campo dedicado de AccessionNumber; se **acuerda**
   transportarlo en `FT1-2` (recomendado) o en un segmento `Zxx` propio del emisor.

> **Opcional (fuera de alcance base):** reverso/rechazo de pago vía `FT1-6` de ajuste/crédito
> → `PaymentStatus.Rejected` (re-retiene la entrega). Ver opcionales §6.

### A.3 `BAR^P01` — Alta/edición de aseguradora (upsert de catálogo, nueva integración)

`BAR^P01` (Add/Change Billing Account) alimenta el **catálogo de aseguradoras** desde HL7.
Se procesa el segmento **`IN1`** del mensaje y se hace **upsert** por `IN1-3`: **alta** si el
código no existe, **edición** si ya existe.

Mensaje de ejemplo:

```
MSH|^~\&|BILLING|HOSP|EDGEHUB|EDGE|20260715080000||BAR^P01|MSG3001|P|2.5.1
EVN|P01|20260715080000
PID|1||PAT12345^^^HOSP^MR||PEREZ^JUAN||19800101|M
IN1|1|PLAN-GMM^Gastos Médicos Mayores|GNP|Grupo Nacional Provincial||||||||||||||||||||||||||||||||||||AUTORIZADO^Y
```

Campos consumidos (upsert sobre `Insurer`):

| Campo | Nombre HL7 | Uso en EdgeGuard | Ejemplo |
|---|---|---|---|
| `IN1-3` | Insurance Company ID | **Clave del catálogo** (`Insurer.Code`) — upsert | `GNP` |
| `IN1-4` | Insurance Company Name | `Insurer.Description` | `Grupo Nacional Provincial` |
| `IN1-45.1` | Verification Status (comp. 1) | Estatus (Activa/Inactiva) | `AUTORIZADO` |
| `IN1-45.2` | *(componente acordado)* | **Default** de `RequiresPaymentConfirmation` (`Y`/`N`) | `Y` |

**Semántica de procesamiento:**

1. Requiere **habilitar `BAR` en el pipeline/listener HL7** y un **handler dedicado** de upsert.
2. Si `IN1-3` **no existe** en el catálogo → **alta**; si existe → **edición** (idempotente).
3. La bandera `IN1-45.2` fija el **default** del catálogo; el valor autoritativo por estudio
   sigue viniendo del `IN1` del `ORM` en tiempo de orden (§2.2).
4. `BAR^P01` mantiene el **catálogo** (aseguradoras); **no** liga aseguradora a paciente ni a
   estudio (eso ocurre con el `IN1` del `ORM`, §2.2).

---

_Cotización sujeta a validación en kickoff del formato exacto de los mensajes HL7 `IN1`
(aseguradora), `DFT^P03` (confirmación de pago) y `BAR^P01` (catálogo), incluida la clave de
emparejamiento con el estudio. Montos calculados a tarifa de **$162.50 MXN/hora**, no
incluyen IVA._
