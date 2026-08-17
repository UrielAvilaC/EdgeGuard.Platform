# Plan de Implementación — RIS: Jerarquía Organizacional, Agenda y Campos Dinámicos

> **Estado:** Versión final (v1.0) — todas las decisiones de diseño cerradas; lista para aprobación e inicio de F0
> **Fecha:** 2026-06-08
> **Alcance:** Evolución de EdgeGuard hacia un RIS ligero. Cubre la fundación organizacional/tenancy (F0), el bounded context `Scheduling` (Agenda + Lista de Trabajo), el módulo transversal `DynamicForms`, identidad (Master IDs) e integración HL7 bidireccional.
> **Fuera de alcance (fases posteriores):** módulo de Reporte (autoría/firma), KPIs/reportería, modo Standalone (documentado a nivel de estrategia).
> **Referencias:** `docs/04-features/modality-worklist.md`, `docs/04-features/hl7-orm.md`, `docs/09-development/domain-model.md`, `docs/03-architecture/database-schema.md`

---

## 1. Objetivo y contexto

Hoy EdgeGuard es una plataforma DICOM edge-to-hub: las órdenes **entran** desde un RIS externo vía HL7 ORM (`Study(Scheduled)` → MWL C-FIND en el Node). Para tener agenda propia invertimos el flujo: el sistema **origina y programa** la cita, y esa cita genera la orden que alimenta la MWL existente.

```
HOY:   RIS externo → ORM → Study(Scheduled) → MWL
META:  Agenda (cita) → Order → Study(Scheduled) → MWL   (coexistiendo con el RIS externo)
```

El extremo DICOM/MWL del Node **no se toca**. La agenda se enchufa antes, por eventos de dominio.

---

## 2. Principios de diseño

| Principio | Implicación |
|---|---|
| **Dos ejes de estado, no uno** | El estatus técnico de entrega (`Study.Status`: Received→Sending→Sent) es automático y se mantiene aparte. El estatus de workflow clínico (Requested→…→Finalized) es el eje humano nuevo. La Lista de Trabajo muestra ambos lado a lado. |
| **Un aggregate por concepto, no un módulo por estatus** | Agregar un estado = un valor de enum + un método de transición con guard. Mismo patrón que el `Study.TransitionStatus()` actual. |
| **Reusar antes que reinventar** | Patient, Study/MWL, pipeline HL7, `audit_logs`, permisos JWT, SignalR y notificaciones (WhatsApp/Twilio) ya existen y se reutilizan. |
| **Disponibilidad on-the-fly** | No se materializan slots; se computan por álgebra de intervalos al consultar. |
| **Lo dinámico no se traga el dominio** | El núcleo (paciente, accession, estatus, recurso) son columnas reales tipadas. Los campos dinámicos son para la cola larga específica por sede. |
| **Topología ≠ dominio** | La separación Hub/Node es decisión de despliegue, no de dominio. El RIS vive 100% en el Hub. |

---

## 3. Topología y hoja de ruta

### 3.1 Separación Hub/Node

La separación Hub/Node es de **topología de despliegue**, no de dominio. El RIS (agenda, órdenes, reporte, HL7) corre **íntegro en el Hub**; el Node solo recibe DICOM, sirve MWL y reenvía a PACS. Razones del Node (proximidad DICOM, offline-first, fan-in multi-sede, PACS en el borde, frontera de seguridad) **siguen vigentes** y el RIS no las elimina.

- **Multi-sede / nube:** Hub central + Nodes remotos (cada sitio conserva su Node por resiliencia offline de la MWL).
- **Sitio único:** se puede **colapsar a un solo proceso** (Standalone) — co-hospedar el Node como módulo, no eliminar su lógica.

### 3.2 No se hace una solución RIS dedicada

El RIS va como **bounded contexts dentro de la plataforma existente**, no como producto aparte. Una solución separada tiraría el reuso (~60% del núcleo RIS ya existe: Patient, HL7, MWL, Study, audit, auth) y obligaría a re-integrarse con el mismo DICOM/MWL — duplicando mantenimiento y matando el diferenciador (agenda → MWL → DICOM → reporte en uno).

### 3.3 Orden de ejecución

1. **F0 — Jerarquía Organizacional + Tenancy** (primero; barato ahora, caro de retrofitear).
2. **F1 — Agenda RIS + DynamicForms** sobre el Hub, en topología **desacoplada**.
3. **Standalone** (empaquetado, al final; aditivo, no perturba el RIS).

---

## 4. Jerarquía organizacional y multi-tenancy (F0)

### 4.1 Jerarquía

```
Tenant (organización / cliente — aislamiento de datos)
  └─ Facility / Sede (ubicación; ancla compartida; ES el filtro del header)
        ├─ Node(s)        ← agente(s) de borde DICOM (técnico)
        ├─ Resource(s)    ← equipos/salas agendables (RIS)
        └─ Orders / Appointments / Studies   ← ocurren en esta sede
```

### 4.2 Cardinalidades y scope

| Relación | Cardinalidad |
|---|---|
| Tenant → Facility | 1 : N |
| Facility → Node | 1 : N (normalmente 1; varios permitidos) |
| Facility → Resource | 1 : N |
| Tenant → Node | 1 : N (transitivo, vía Facility) |

| Entidad | Scope |
|---|---|
| **Patient** | **Tenant-scoped** (se comparte entre sedes de la organización) → `tenant_id` |
| Order / Appointment / Study | **Facility-scoped** → `tenant_id` + `facility_id` |
| Resource, Node | Facility-scoped |

> **Regla:** el paciente es de la organización; lo que se le *hace* (orden/cita/estudio) es de una sede.

- `Node` **no se renombra**; solo gana `facility_id`. `Tenant ≠ Node ≠ Resource` (niveles distintos; no se fusionan conceptualmente).
- `TenantId` se implementa **siempre** como ciudadano de primera clase: columna + EF Core global query filters + claim en JWT.

### 4.3 Single-site: configuración englobada en un solo registro

En despliegue de sitio único, configurar "el sitio" **crea/configura Tenant + Facility + Node en el mismo registro/pantalla** (sembrado: 1 Tenant + 1 Facility + 1 Node). Todo nace con ese `tenant_id`/`facility_id`. El modelo **escala a multi-sede sin rediseño**: se agregan filas de Facility/Node y el filtro de Sede del header empieza a ofrecer más de una opción.

### 4.4 Filtro de sede en el header

Cada sede va **aislada** en el agendamiento. Si el usuario tiene permitido más de un sitio, **debe elegir** la Sede activa en el header (= `Facility`).

---

## 5. Identidad y Master IDs

El sistema **genera** identificadores maestros propios, independientes de los externos.

### 5.1 Unicidad system-wide (trascienden el Tenant)

**Master Patient ID, Master Order ID y Master Study ID son únicos a nivel SISTEMA** — trascienden el Tenant, como un GUID (no se reinician por tenant ni por sede). Existen **dos capas** de identidad:

| Capa | Ejemplo | Naturaleza |
|---|---|---|
| **Surrogate interno** (ya existe) | `PatientId`, `StudyId` (UUID) | GUID técnico, estable ante merges/cambios |
| **Master Business ID** (nuevo) | MPI, Master Order ID, Master Study ID | **Legible, único system-wide** |

| Identificador | Qué es |
|---|---|
| **Master Patient ID (MPI)** | ID maestro de paciente, único en todo el sistema; unifica los MRN externos de todas las sedes/fuentes |
| **Master Order ID** | ID maestro de la orden; distinto del placer/filler externo |
| **Master Study ID = Accession Number** | ID maestro de estudio; **fluye a DICOM `(0008,0050)` y a la MWL** |

> No confundir: `Study Instance UID` es el identificador **técnico DICOM** (único global), independiente del Accession Number (identificador maestro de negocio) y del `StudyId` (surrogate UUID).

### 5.2 Formato y generación

```
MasterID = {PrefijoFacility_por_tipo}{consecutivo de 10 dígitos, relleno con ceros}
```

- **Prefijo:** configurado **por Facility** (en configuración de base de datos), uno por tipo: paciente, orden, estudio.
- **Consecutivo:** **general** (global) por tipo; incremento **atómico** (Postgres `SEQUENCE`, no read-increment-write) para garantizar unicidad bajo concurrencia. **Editable desde la interfaz** (admin puede ver/ajustar el valor actual vía `setval`, con validación para no retroceder por debajo del máximo emitido).
- **Relleno:** 10 dígitos tras el prefijo, con ceros a la izquierda.

Ejemplos (prefijos de la Facility: paciente `MTY`, estudio `ACC`):

| Tipo | Resultado |
|---|---|
| Master Patient ID | `MTY0000000123` |
| Master Study ID / Accession | `ACC0000004567` |

La unicidad system-wide la garantiza el **consecutivo global por tipo** (un solo contador atómico por paciente / orden / estudio en todo el sistema); el prefijo de Facility identifica el origen.

### 5.3 Accession Number: generación configurable

```
identity.accession_generation = System | Manual   (default: System)
```

- **System:** al **agendar**, el sistema genera el Accession con el formato de §5.2.
- **Manual:** el usuario lo captura (el sistema valida unicidad system-wide).

El MPI habilita el escenario enterprise donde un paciente llega desde múltiples fuentes/sedes y debe consolidarse en una sola identidad.

---

## 6. Bounded contexts y proyectos nuevos

```
Dicom.Edge.Hub.Scheduling.Domain        ← Order, Appointment, Resource, ProcedureCatalog, AvailabilityTemplate, masters
Dicom.Edge.Hub.Scheduling.Application   ← casos de uso, motor de disponibilidad, ISchedulingPolicy, handlers de eventos
Dicom.Edge.Hub.DynamicForms.*           ← módulo transversal de campos dinámicos (compartido)
(persistencia)                           ← tablas en Hub.Persistence (mismo Postgres), schema propio opcional
src/frontend/.../features/scheduling     ← Agenda (calendario) + Lista de Trabajo (Angular lazy-loaded)
src/frontend/.../features/dynamic-forms  ← renderizador genérico + form builder
```

### 6.1 Distribución física de esquemas (híbrido: `public` + `scheduling`)

Lo **compartido** (lo tocan tanto el lado DICOM/MWL como el RIS) vive en `public`; lo **exclusivo del RIS** (que el lado DICOM nunca toca) vive en `scheduling`. Las FKs apuntan en una sola dirección: **`scheduling → public`** (nunca al revés), lo que mantiene el acoplamiento limpio.

```
┌──────────────────────── schema: public (compartido) ────────────────────────┐
│  Núcleo + transversal — referenciado por DICOM/MWL y por RIS                  │
│                                                                              │
│  tenants · facilities             ← jerarquía organizacional                 │
│  patients · patient_contacts      ← identidad (MPI)                          │
│  studies · study_status_audit     ← eje técnico, compartido con MWL/DICOM    │
│  resources (equipos/salas)        ← ejecutor de estudio + agendable          │
│  procedure_catalog                ← referenciado por MWL y por agenda        │
│  nodes · pacs_servers             ← borde DICOM                              │
│  hl7_messages                     ← log inbound existente                    │
│  tags · priorities                ← catálogos compartidos                    │
│  users · audit_logs · system_settings                                        │
│  dynamic_forms (metadata)         ← módulo transversal                       │
└──────────────────────────────────────────────────────────────────────────────┘
                          ▲  FKs (scheduling → public)
                          │
┌──────────────────────── schema: scheduling (RIS puro) ──────────────────────┐
│  Lo que el lado DICOM/MWL NUNCA toca                                          │
│                                                                              │
│  orders · order_items · order_status_audit                                   │
│  appointments · appointment_status_audit                                     │
│  availability_templates · maintenance_blocks                                 │
│  holidays (Global/Facility/Resource)                                         │
│  procedure_packages                                                          │
│  code_mapping_sections · code_mappings · pending_mapping_queue               │
│  referring_physicians · insurers                                             │
│  documents (adjuntos)                                                        │
│  hl7_destinations · hl7_subscriptions   (saliente)                           │
└──────────────────────────────────────────────────────────────────────────────┘
```

> Los valores de **campos dinámicos** (`custom_fields jsonb`) viven en la tabla anfitriona (`public.patients`, `scheduling.appointments`, …); solo la **metadata** del form builder es transversal (`public.dynamic_forms`).

---

## 7. Modelo de dominio

### 7.1 Aggregates principales

| Aggregate | Responde a | Ciclo de vida |
|---|---|---|
| **`Order`** | El **qué/por qué** (solicitud clínica) | `Requested → Approved → Rejected` |
| **`Appointment`** | El **cuándo/dónde** (evento reservado) | Ver eje workflow (§8) |
| **`SchedulableResource`** | Equipo / sala (calendario, capacidad, TZ) | Master operativo |
| **`ProcedureCatalogItem`** | Definición de examen | Master |
| **`AvailabilityTemplate`** | Plantilla de horario por recurso | Master |

`Order` y `Appointment` se mantienen **separados**: una orden puede existir sin agendarse, ser rechazada o reagendarse varias veces (cancelas el Appointment, la Order sobrevive).

```
Solicitar  → Order(Requested)
Aprobar    → Order(Approved)  ──evento──► crea Appointment(Scheduled) ──► Study(Scheduled) ──► MWL
```

### 7.2 Una orden, varios estudios + paquetes/protocolos (F1)

- Una `Order` puede contener **varios procedimientos**.
- **Paquetes/protocolos**: bundle que al seleccionarse **carga automáticamente** sus procedimientos (ej. `US + MG`). Master `ProcedurePackage` → lista de `ProcedureCatalogItem`.

### 7.3 Códigos de procedimiento y mapeo externo→interno

- Cada `ProcedureCatalogItem` tiene un **código interno** propio (fuente de verdad), flag **`IsInterpretable`** y un **`standard_code`** opcional (nullable) para mapeo a estándar (CPT/SNOMED/LOINC).
  - El campo **se incluye desde F1** (columna opcional); su **uso** (facturación / interoperabilidad por código estándar) queda **diferido** hasta que se requiera.
- El mapeo de códigos externos hacia el interno se organiza en **secciones definidas por el usuario**.
- **Unicidad:** dentro de una sección, **un código interno admite un solo mapeo externo**.

```
CodeMappingSection (definida por el usuario)
  └─ CodeMapping → external_code → ProcedureCatalogItem (interno)
        UNIQUE (section_id, internal_procedure_id)
```

---

## 8. Ejes de estado

### 8.1 Eje workflow (humano) — el nuevo

```
Requested → Scheduled → Arrived → Started → Completed ─┬─ [no interpretable] → Finalized Without Report
                                                       └─ [interpretable]    → In Dictation → Finalized With Report
Cancelled: terminal de excepción (desde Requested/Scheduled/Arrived). NoShow = razón de cancelación.
```

| Estado | Español | Dispara | Notas |
|---|---|---|---|
| Requested | Solicitado | Módulo Solicitar / ORM externo | Vive en `Order` |
| Scheduled | Agendado | Aprobación → Agenda | Crea Study(Scheduled) → MWL |
| Arrived | Arribado | Lista de Trabajo (check-in) / walk-in | Sella `ArrivedAt`; realiza hora exacta en modo capacidad |
| Started | Iniciado | Lista de Trabajo | Captura técnico que da clic; sella `PerformedResource` |
| Completed | Completo | Auto o manual (§9) | Imágenes adquiridas = listo para leer |
| Finalized Without Report | Finalizado sin informe | Auto si `IsInterpretable=false` | Terminal |
| In Dictation | En Dictado | Módulo Reporte (fase futura) | Solo si interpretable |
| Finalized With Report | Finalizado con Informe | Módulo Reporte (fase futura) | Terminal |
| Cancelled | Cancelado | Agenda / Lista de Trabajo | Requiere razón (catálogo) |

**Regla:** `ProcedureCatalogItem.IsInterpretable` decide la bifurcación final en `Completed`.

### 8.2 Eje técnico (sistema) — el existente, NO se mezcla

`Study.Status`: `Received → Sending → Sent`. Automático, no configurable. Se muestra junto al eje workflow en la Lista de Trabajo.

### 8.3 Dos módulos sobre un mismo aggregate

| | **Módulo Agenda** | **Módulo Lista de Trabajo** |
|---|---|---|
| Naturaleza | Write (booking) | Read + pocos comandos |
| Transiciones | Schedule, Reschedule, Cancel | CheckIn (Arrived), Start, Complete |
| UI | Calendario día/semana/mes + auto | Tablero del día con semáforo |

---

## 9. Modo de finalización (configurable)

- **Eje técnico:** automático fijo. No configurable.
- **Eje clínico (`Completed`):**

```
scheduling.appointment_completion_mode = Auto | Manual   (default: Auto)
```

En `Auto`, el handler de `StudyReceivedEvent` (ya existe) avanza la cita a `Completed`. En `Manual`, la Lista de Trabajo muestra un botón "Completar".

---

## 10. Modelo de disponibilidad (on-the-fly)

### 10.1 Motor: álgebra de intervalos

```
Disponible(equipo, sala, procedimiento, rango) =
      Horario_equipo ∩ Horario_sala ∩ Días_operativos
    − Feriados (jerárquicos) − Mantenimiento/Bloqueos − Citas − Holds
```

Resultado = intervalos libres, computados al consultar. Sin slots materializados.

### 10.2 Reserva: equipo + sala

- La cita reserva **equipo + sala**.
- El **técnico** NO se reserva: se captura a quien **da clic y envía** en la Lista de Trabajo (`Started`).
- **Ventanas de mantenimiento/bloqueo** de equipo restan disponibilidad.

### 10.3 Capacidad unificada (slot = capacidad 1)

| Capacidad | Comportamiento | Enforce |
|---|---|---|
| `capacity = 1` | Slot exclusivo, hora exacta (ej. CT con contraste) | `EXCLUDE USING gist` (tstzrange + resource_id) |
| `capacity = N` | Bloque/wave: N cupos en una ventana, sin hora exacta (ej. US, RX) | Conteo transaccional |

La hora exacta en modo capacidad se **realiza en `CheckIn()`** (`ArrivedAt = now`). Configurado por (recurso, procedimiento).

### 10.4 Dos vistas (un solo motor)

| Vista | Qué hace |
|---|---|
| **Automática** | Agrupa intervalos libres en franjas (mañana/mediodía/tarde) configurables; muestra solo disponibles |
| **Manual** | Grilla día/semana/mes con ocupados/bloqueados; el usuario elige hora exacta |

### 10.5 Matriz de capacidad (opt-out)

Por defecto **todos los procedimientos se pueden hacer en todos los equipos de su modalidad**; el usuario **excluye** los que no. Profundidad: modalidad → procedimiento.

---

## 11. MWL scoping (pool + recurso ejecutor)

- `Appointment.ScheduledResourceId` (dónde se agendó; equipo específico o **pool de modalidad**).
- `Appointment.PerformedResourceId` (dónde se ejecutó realmente).
- El equipo ejecutor se **auto-detecta del DICOM** (Station Name `(0008,1010)` / source AE; `Study` ya captura `source_ae_title`) en el **primer C-STORE**. Fallback manual en la Lista de Trabajo.
- El "off-MWL al primer C-STORE" existente (Study pasa a `Receiving`, sale de la MWL) **mitiga la doble-adquisición** del modelo pool.

```
mwl.scope = OwnOnly | ModalityPool | All   (default: OwnOnly, por equipo/nodo)
```

---

## 12. Integración con RIS externo (coexistencia configurable)

La máquina de estados interna es la única fuente de verdad; lo externo llama **las mismas transiciones** vía una **capa anticorrupción (inbound adapter)** que mapea SIU/ORM/ADT → transiciones.

| Knob (`system_settings` / por feed) | Opciones |
|---|---|
| Dirección | Entrante / saliente / bidireccional |
| Estados que puede empujar lo externo | Requested, Scheduled, Arrived, Started |
| ¿Agendar internamente con integración activa? | Sí (mixto) / No (espejo solo-lectura) |
| Resolución de conflicto | Quién gana si ambos tocan el mismo estado |

**Correlación/idempotencia:** por accession / order number (reusa infra HL7). Reenvíos no duplican.

### 12.1 HL7 saliente y módulo de destinos (F1)

Salida configurable de **ORM, ORU y ADT** hacia uno o varios sistemas externos, con **módulo de destinos** donde cada sistema **se suscribe a los tipos de mensaje** que recibe.

```
Hl7Destination (sistema externo: host, puerto MLLP, AE/aplicación, habilitado)
  └─ Hl7Subscription → { ORM | ORU | ADT }
```

| Destino | ORM | ADT | ORU |
|---|---|---|---|
| Sistema 1 | ✅ | ✅ | ✅ |
| Sistema 2 | ✅ | — | ✅ |

- Al ocurrir un evento interno, se enruta el mensaje a **todos los destinos suscritos** a ese tipo. Reusa el outbox/dispatch existente con reintentos. Cada destino habilitable independientemente.

### 12.2 Códigos no mapeados — "Pendiente de Mapeo" (F1)

Una orden externa **nunca se pierde**.

```
hl7.inbound.unmapped_code_policy = Quarantine | AutoProvision   (default: Quarantine)
```

- **Strict + cuarentena (default):** la orden se **captura** (paciente, accession, código/descripción externos, sistema origen) pero **no avanza a `Scheduled`**; queda en **"Pendiente de Mapeo"**.
- **Auto-provisión (opcional):** si el mensaje trae modalidad, auto-crea el procedimiento (mínimo modalidad + nombre, "auto-creado / revisar"); si no, cae a cuarentena.

**Submódulo de resolución (UI):** bandeja "Pendiente de Mapeo" donde el usuario (1) mapea a un procedimiento existente, o (2) **crea el registro** (mínimo modalidad + nombre, resto por defecto) y mapea; tras resolver, la orden **se desbloquea y se agenda**. El mapeo queda persistido en su sección → la próxima vez se resuelve solo.

---

## 13. Semáforo / SLA (F1)

Campo **computado** (no estado nuevo), recalculado por consulta / refrescado por SignalR.

- **Reloj principal:** espera desde arribo = `now − ArrivedAt` (citas `Arrived` sin iniciar).
- **Umbrales:** por **modalidad + prioridad** (verde < t1, amarillo t1–t2, rojo > t2).
- **Acción:** solo visual en F1. La Lista de Trabajo ordena por demora descendente.

---

## 14. Cancelaciones, no-show, holds, prerequisitos

| Tema | Decisión |
|---|---|
| **Razones de cancelación** | Catálogo sembrado con genéricos; **autocompletado** que crece al agregar |
| **No-show** | Razón de cancelación (no estado propio) |
| **Cancelación tardía** | Flag `scheduling.late_cancel_threshold_hours` — solo marca |
| **Liberación de slot** | Inmediata al cancelar (automática en modelo on-the-fly) |
| **Hold TTL** | Reserva temporal mientras se confirma; expira y libera solo. Configurable según recepcionistas concurrentes |
| **Prerrequisitos / gating** | Por procedimiento; cada requisito **bloqueante** o **advertencia** (ej. pre-auth, creatinina, responsiva, NPO). **Unificado** con "campo dinámico requerido para transición" (§18) |
| **Walk-in / urgencia** | Paciente sin agenda previa entra directo a `Arrived` |

---

## 15. Notificaciones

Enviar solo si: (1) plantilla **activa** para ese estatus, **y** (2) el paciente tiene contacto (email/teléfono), **y** (3) el canal está habilitado (email/WhatsApp). Reusa `auto-send-rules` existente extendida con **selector de canal**.

---

## 16. Catálogos / masters nuevos y atributos

| Concepto | Dónde vive | Master nuevo |
|---|---|---|
| **Tags** (VIP, silla de ruedas, diabetes) icono/color, categorizados (clínico/logístico/servicio) | Catálogo + asignación a Patient (persistentes) y/o Appointment (de la visita) | Sí `tags` |
| **Prioridades** configurables | Catálogo (Study ya tiene priority/is_urgent) | Sí `priorities` |
| **Contactos del paciente** (varios) | `PatientContact` (tel/email/relación) | Extiende Patient |
| **Documentos** (responsivas, consentimiento) | Archivo físico + metadatos | Sí + storage (§16.1) |
| **Tipo/nivel de visita** (externa, hospitalización) | Atributo en Order (Encounter/Visit) | Campo/enum |
| **Hospital origen, Departamento** | Atributos de Order | Campo |
| **Médico referente** | Master + asignación a Order | Sí `referring_physicians` |
| **Aseguradoras** | Master + asignación a Order/Patient (alimenta pre-auth) | Sí `insurers` |
| **`IsInterpretable`** | Flag en ProcedureCatalogItem (bifurca estatus final) | Campo |
| **`standard_code`** | Campo opcional en ProcedureCatalogItem (CPT/SNOMED/LOINC); uso diferido | Campo |

### 16.1 Almacenamiento de documentos

- **Archivos físicos** en carpeta **configurable**, **sin límite** de tamaño. Tipos: PDF, imagen, DOCX, etc.
- **Retención configurable** en `system_settings`. Aplica PHI: auditoría y control de acceso por permisos.

---

## 17. (reservado)

---

## 18. Módulo de Campos Dinámicos (`DynamicForms`) — F1

### 18.1 Almacenamiento: JSONB (no EAV)

Valores en columna `custom_fields jsonb` en la entidad anfitriona (`Patient.custom_fields`, `Appointment.custom_fields`, …). Encaja con el uso de JSONB existente. Indexable con GIN.

### 18.2 Metadatos (form builder)

```
FormDefinition   → contexto destino (Patient | Modality | Appointment | Worklist | Order) + versión
  └─ FormSection → agrupación + orden
       └─ FieldDefinition → key, label, tipo de control, tipo de dato, validaciones, orden
            └─ FieldOption → opciones (radio/dropdown)
```

### 18.3 Controles y validaciones

| Control | Tipo dato | Validaciones |
|---|---|---|
| text / textarea | string | required, maxLength, regex |
| number | numérico | required, min, max |
| radio / dropdown | enum (FieldOption) | required |
| (sugeridos) checkbox, date | — | required |

Validación en **ambos lados** desde la misma definición (Angular + servidor).

### 18.4 Un solo renderizador

Componente Angular genérico que recibe un `FormDefinition` y produce el formulario reactivo. **Un renderizador, N formularios.**

### 18.5 Versionado

- `FormDefinition` **versionado**; los registros guardan la versión bajo la que se llenaron.
- Cambiar/eliminar un campo crea **nueva versión**; los valores históricos permanecen estables y se renderizan con su versión.

### 18.6 Captura vs visualización vs reportería

- **F1:** capturar y **mostrar** en la interfaz; si el campo está marcado para el **flujo de trabajo**, se muestra ahí.
- **Filtrado/reportería** por campos dinámicos → **reportería y KPIs** (fase posterior). F1 difiere índices JSONB para filtrado pesado.

### 18.7 Regla de oro + solape con gating

Lo dinámico es para la **cola larga** por sede; el núcleo permanece como columnas reales. "Campo dinámico requerido para una transición" **es** el gating de prerrequisitos (§14): un solo concepto de "requerido para transición".

---

## 19. Zona horaria

Tiempos en **UTC (`timestamptz`)**; cada recurso/sede con IANA tz; render local en el SPA.

---

## 20. Auditoría

- Reusar `audit_logs` y permisos existentes.
- Replicar el patrón `StudyStatusAudit` (child append-only ya existente) con un **`AppointmentStatusAudit`** (FromStatus, ToStatus, ChangedAt, Reason, ChangedBy).

---

## 21. Feriados jerárquicos

- Entradas con **nivel de alcance**: `Global` / `Facility` / `Resource` + fecha/rango + recurrencia anual.
- Feriados efectivos de un recurso = unión de Global + su Sede + propios.
- Soportar **excepciones positivas** (un recurso sí trabaja en feriado global).
- El motor de disponibilidad resta el conjunto resuelto.

---

## 22. Persistencia en Standalone (estrategia — fase posterior)

- **Standalone corre sobre un único Postgres** — el diseño RIS (exclusion constraints `gist`, JSONB) lo exige; SQLite ya no alcanza para el lado central. SQLite queda solo para Nodes remotos.
- **Dos `DbContext` lógicos en schemas separados** (`hub` / `node`) en la misma BD; provider conmutable por config (Npgsql en Standalone, SQLite en Node remoto).
- **Duplicación `studies` vs `node_studies`:** válida en desacoplado (autonomía offline), sobra en Standalone. Se elimina con un **seam de interfaces** en la persistencia de borde (que ya existe: `IRepository<>`, `INodeWorkQueue`, `IWorklistManager`, …) + una implementación respaldada por el Hub. El proyecto **`.Standalone` (host de composición) elige** qué implementación se inyecta.
- **Modos:** (A) conservar duplicación con sync por loopback (v1, riesgo cero); (B) colapsar a una sola fuente de verdad — `node_studies` desaparece (estado final).

---

## 23. Resumen de claves en `system_settings`

| Clave | Default | Descripción |
|---|---|---|
| `scheduling.appointment_completion_mode` | `Auto` | Auto vs Manual para `Completed` clínico |
| `scheduling.late_cancel_threshold_hours` | — | Umbral de cancelación tardía (solo marca) |
| `scheduling.hold_ttl_minutes` | `10` | Vida del hold temporal |
| `scheduling.concurrent_receptionists` | — | Habilita/ajusta holds según concurrencia |
| `scheduling.semaphore.thresholds` | — | Umbrales verde/amarillo/rojo por modalidad+prioridad |
| `mwl.scope` (por equipo) | `OwnOnly` | OwnOnly / ModalityPool / All |
| `documents.storage_path` | — | Carpeta configurable de adjuntos |
| `documents.retention` | — | Política de retención |
| `notifications.*` | — | Canal por estatus (email/WhatsApp) |
| `hl7.outbound.destinations` | — | Sistemas externos + suscripción por tipo (ORM/ORU/ADT) |
| `hl7.inbound.unmapped_code_policy` | `Quarantine` | Quarantine / AutoProvision |
| `identity.patient_seq` / `order_seq` / `study_seq` | — | Consecutivos **globales** por tipo (10 dígitos); Postgres `SEQUENCE` atómica, **valor editable desde la interfaz** (admin) |
| `identity.accession_generation` | `System` | System (genera al agendar) / Manual (lo captura el usuario) |
| _(config de BD por Facility)_ | — | Prefijos por sede para paciente / orden / estudio (no en system_settings) |

---

## 24. Registro de decisiones tomadas

| # | Decisión |
|---|---|
| 1 | Agenda **coexiste** con RIS externo; integración configurable; acepta estados externos (Requested/Scheduled/Arrived/Started) |
| 2 | `Order` (Requested→Approved) separado de `Appointment`; aprobar crea cita agendada |
| 3 | Reserva = **equipo + sala**; técnico capturado al iniciar en Lista de Trabajo |
| 4 | Matriz de capacidad **opt-out** (todo permitido; el usuario excluye), nivel procedimiento |
| 5 | MWL **pool** con scheduled/performed resource; ejecutor auto-detectado del DICOM; `mwl.scope` configurable |
| 6 | Disponibilidad **on-the-fly**; capacidad unificada (slot = capacidad 1); dos vistas (auto/manual) |
| 7 | Completed clínico **configurable** (Auto/Manual); técnico siempre automático |
| 8 | Semáforo F1 (reloj espera-desde-arribo, umbrales modalidad+prioridad, solo visual) |
| 9 | No-show = razón de cancelación; razones con autocompletado sembrado |
| 10 | Documentos: archivos físicos, carpeta configurable, sin límite, retención en settings |
| 11 | Holds: TTL configurable por concurrencia de recepcionistas |
| 12 | Paquetes/protocolos en **F1** |
| 13 | Estados del estudio aprobados (§8.1) con bifurcación por `IsInterpretable` |
| 14 | Tags, prioridades, contactos, visita, referente, aseguradoras, interpretable: incluidos |
| 15 | Campos dinámicos en **F1**, JSONB, con **versionado**; F1 = captura/visualización; filtrado→reportería |
| 16 | Cada sede aislada; filtro de sede (Facility) en header |
| 17 | **`TenantId` se implementa siempre**; sede única = un solo sitio sembrado |
| 18 | Procedimientos con **código interno** + mapeo externo→interno por **secciones**; un externo por interno por sección |
| 19 | HL7 **saliente ORM/ORU/ADT en F1**, configurable, con **módulo de destinos** |
| 20 | Códigos no mapeados: **Strict + cuarentena** (default) con submódulo "Pendiente de Mapeo"; auto-provisión opcional |
| 21 | Jerarquía **Tenant → Facility → Node**; `Node` no se renombra (solo +`facility_id`); Tenant/Node/Resource no se fusionan |
| 22 | **Paciente = Tenant-scoped**; Orden/Cita/Estudio = Facility-scoped |
| 23 | Single-site: configurar el sitio **engloba Tenant + Facility + Node en un solo registro** |
| 24 | **Master IDs generados, únicos system-wide** (trascienden el Tenant, como GUID): MPI, Master Order ID, **Master Study ID = Accession Number** (≠ StudyId surrogate ≠ Study Instance UID). Formato `{prefijo Facility}{consecutivo global 10 dígitos, relleno con ceros}`; prefijo en config de BD por Facility, consecutivo global en system_settings (Postgres SEQUENCE atómica). Accession **configurable** System/Manual, generado al agendar |
| 25 | **No** se hace solución RIS dedicada; RIS = bounded contexts en la plataforma existente |
| 26 | Orden: **F0 (tenancy) → F1 (RIS) → Standalone**; Standalone aditivo y posterior |
| 27 | **Esquema híbrido** (§6.1): compartido en `public` (studies, resources, procedure_catalog, patients, tags…), RIS puro en `scheduling`; FKs solo `scheduling → public` |
| 28 | **Sin estados intermedios de paciente** (en-prep/en-sala): la lista de estados de §8.1 queda **final** (incluye Finalizado sin informe / En Dictado / Finalizado con informe) |
| 29 | **`standard_code` (opcional) se agrega desde F1** en `ProcedureCatalogItem`; el **uso** del mapeo a estándar (CPT/SNOMED/LOINC) para facturación/interoperabilidad queda **diferido** |

---

## 25. Decisiones pendientes (a confirmar)

_Ninguna — todas las decisiones de diseño están cerradas. Pendiente solo la **aprobación** del plan para iniciar F0._

---

## 26. Fases de implementación

| Fase | Alcance |
|---|---|
| **F0 — Jerarquía Organizacional + Tenancy** | `tenants` + `facilities` (siembra 1+1); `Node` +`facility_id`; `tenant_id` (+`facility_id`) + global query filters + claim JWT en todo lo transversal; Master IDs **únicos system-wide** (MPI, Master Order, Accession) con prefijo por Facility + consecutivo global (Postgres SEQUENCE) de 10 dígitos; accession configurable System/Manual; configuración single-site englobada (Tenant+Facility+Node en un registro); filtro de Sede en header |
| **F1.0 — Masters** | Catálogo de procedimientos (+`IsInterpretable`, código interno); secciones y mapeo externo→interno; recursos (equipo/sala), plantillas de disponibilidad (capacidad, mantenimiento), feriados jerárquicos, tags, prioridades, referentes, aseguradoras, paquetes |
| **F1.1 — Órdenes** | Módulos Solicitar / Aprobar; `Order(Requested→Approved)`; una orden/varios estudios; paquetes |
| **F1.2 — Agenda** | Motor de disponibilidad on-the-fly; vistas auto/manual; booking equipo+sala; holds; `Appointment(Scheduled)` → Study(Scheduled) → MWL |
| **F1.3 — Lista de Trabajo** | Check-in (Arrived), Start (captura técnico + performed resource), Complete (Auto/Manual); semáforo; walk-in/urgencia; dos ejes de estado en una vista |
| **F1.4 — Campos dinámicos** | `DynamicForms` (JSONB + metadatos + renderizador + versionado); captura/visualización; gating unificado |
| **F1.5 — Integración externa** | Capa anticorrupción configurable (entrante); idempotencia por accession; cuarentena "Pendiente de Mapeo" + submódulo de resolución; HL7 saliente ORM/ORU/ADT + módulo de destinos; notificaciones por canal |
| **Fase 2+** | Reporte (autoría/firma), KPIs/reportería (incl. filtrado por campos dinámicos), escalamiento de semáforo, **Standalone** (host de composición + Postgres único + de-duplicación) |
