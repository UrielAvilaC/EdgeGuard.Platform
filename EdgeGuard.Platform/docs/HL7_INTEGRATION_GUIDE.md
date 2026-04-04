# 📋 Guía de Integración HL7 v2.x — EdgeGuard Platform

> **Versión**: 1.0.0  
> **Protocolo**: HL7 v2.5 sobre MLLP (Minimal Lower Layer Protocol)  
> **Plataforma**: EdgeGuard Hub → Edge Node  
> **Última actualización**: Enero 2026

---

## Tabla de Contenido

1. [Introducción](#1-introducción)
2. [Arquitectura del Pipeline HL7](#2-arquitectura-del-pipeline-hl7)
3. [Protocolo MLLP — Framing de Mensajes](#3-protocolo-mllp--framing-de-mensajes)
4. [Tipos de Mensaje y Trigger Events](#4-tipos-de-mensaje-y-trigger-events)
5. [Estructura de Segmentos HL7](#5-estructura-de-segmentos-hl7)
6. [Referencia Detallada de Campos por Segmento](#6-referencia-detallada-de-campos-por-segmento)
7. [Segmentos Requeridos por Tipo de Mensaje](#7-segmentos-requeridos-por-tipo-de-mensaje)
8. [Reglas de Validación](#8-reglas-de-validación)
9. [Motor de Enrutamiento](#9-motor-de-enrutamiento)
10. [Ciclo de Vida del Mensaje](#10-ciclo-de-vida-del-mensaje)
11. [Respuesta ACK — Formato y Ejemplos](#11-respuesta-ack--formato-y-ejemplos)
12. [Dispatch Hub → Node (Worklist Push)](#12-dispatch-hub--node-worklist-push)
13. [API de Monitoreo](#13-api-de-monitoreo)
14. [Configuración del Listener](#14-configuración-del-listener)
15. [Configuración del Dispatch Worker](#15-configuración-del-dispatch-worker)
16. [Ejemplos de Mensajes Completos](#16-ejemplos-de-mensajes-completos)
17. [FAQ — Errores Comunes y Soluciones](#17-faq--errores-comunes-y-soluciones)
18. [Glosario](#18-glosario)

---

## 1. Introducción

EdgeGuard Platform implementa una integración HL7 v2.x enterprise que actúa como **bridge** entre sistemas HIS/RIS hospitalarios y nodos DICOM Edge. El flujo principal es:

```
┌──────────┐    MLLP/TCP     ┌──────────────┐   HTTP/JSON    ┌─────────────┐    DICOM MWL
│  HIS/RIS │ ──────────────► │  EdgeGuard   │ ─────────────► │  Edge Node  │ ──────────────►  Modalidades
│  System   │   HL7 v2.x     │     Hub      │   Worklist     │  (C-FIND)   │    (CT, MR, etc.)
└──────────┘                 └──────────────┘    Push         └─────────────┘
                                    │
                                    ▼
                              PostgreSQL DB
                              (Audit Trail)
```

### Capacidades Principales

| Capacidad | Descripción |
|-----------|-------------|
| **Recepción TCP** | Listener MLLP con soporte para conexiones concurrentes |
| **Validación Enterprise** | Validación estructural y semántica por tipo de mensaje |
| **Enrutamiento Inteligente** | Motor de reglas con prioridad, fallback automático |
| **Dispatch Confiable** | Reintentos automáticos, backoff configurable |
| **Worklist Bridge** | Conversión HL7 → DICOM MWL para modalidades |
| **Monitoreo Completo** | API REST para estado del listener, cola y mensajes |
| **Auditoría** | Trazabilidad completa del ciclo de vida del mensaje |

### Estándares Soportados

- **HL7 v2.5** — Messaging Standard
- **MLLP** — Minimal Lower Layer Protocol (RFC sin número, HL7 Specification)
- **DICOM MWL** — Modality Worklist SOP Class (C-FIND)

---

## 2. Arquitectura del Pipeline HL7

El pipeline procesa cada mensaje HL7 a través de 6 etapas secuenciales:

```
    ┌─────────┐    ┌────────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐
    │ RECEIVE │───►│  VALIDATE  │───►│  PERSIST │───►│  ROUTE   │───►│  QUEUE   │───►│ DISPATCH │
    │  (TCP)  │    │ (Enterprise│    │  (DB)    │    │ (Engine) │    │ (Memory) │    │  (HTTP)  │
    └─────────┘    │   Rules)   │    └──────────┘    └──────────┘    └──────────┘    └──────────┘
         │              │                                                                  │
         ▼              ▼                                                                  ▼
     ACK Response   Validation                                                      Node Delivery
                     Failed                                                          (Worklist)
```

### Componentes del Pipeline

| Componente | Capa | Clase | Responsabilidad |
|-----------|------|-------|-----------------|
| TCP Listener | Infrastructure | `Hl7TcpListener` | Recepción MLLP, deframing, ACK |
| Validation | Application | `Hl7ValidationService` | Validación estructural y semántica |
| Processor | Application | `Hl7MessageProcessor` | Orquestador del pipeline completo |
| Routing Engine | Application | `Hl7RoutingEngine` | Evaluación de reglas, fallback |
| Dispatcher | Infrastructure | `NodeHttpDispatcher` | HTTP POST al Edge Node |
| Dispatch Worker | Infrastructure | `MessageDispatchHostedService` | Background polling de cola |

---

## 3. Protocolo MLLP — Framing de Mensajes

Todos los mensajes HL7 se transmiten sobre TCP usando el protocolo MLLP.

### Delimitadores

| Carácter | Hex | Nombre | Posición |
|----------|-----|--------|----------|
| `VT` | `0x0B` | Start Block | Inicio del mensaje |
| `CR` | `0x0D` | Segment Terminator | Fin de cada segmento |
| `FS` | `0x1C` | End Block | Fin del mensaje |
| `CR` | `0x0D` | Trailing CR | Después del End Block |

### Estructura del Frame MLLP

```
<VT> MSH|^~\&|... <CR> PID|... <CR> ... <FS> <CR>
 │                  │              │     │
 0x0B              0x0D           0x1C  0x0D
```

### Ejemplo Binario

```
\x0B                          ← Start Block
MSH|^~\&|HIS|HOSP|...\r      ← Segmento MSH + CR
PID|||12345||...\r            ← Segmento PID + CR
PV1||I||...\r                 ← Segmento PV1 + CR
\x1C\r                        ← End Block + trailing CR
```

> **⚠️ Importante**: EdgeGuard limpia automáticamente los caracteres `\x0B` y `\x1C` del contenido antes del parsing de campos.

---

## 4. Tipos de Mensaje y Trigger Events

EdgeGuard soporta los siguientes tipos de mensaje HL7 v2.x. Esta sección detalla cada tipo, sus trigger events, el significado clínico de cada evento y los campos esperados.

### Resumen de Tipos Soportados

| Tipo | Nombre Completo | Uso Principal | Triggers Soportados |
|------|----------------|---------------|---------------------|
| **ADT** | Admission, Discharge, Transfer | Movimientos y gestión de pacientes | `A01`, `A02`, `A03`, `A04`, `A08`, `A11`, `A12`, `A13` |
| **ORM** | Order Message | Órdenes de estudio/procedimiento radiológico | `O01` |
| **ORU** | Observation Result | Resultados de estudios y observaciones clínicas | `R01` |

> **⚠️ Importante**: Mensajes con tipos no listados arriba (ej: `SIU`, `MDM`, `BAR`) serán rechazados con el error: `Unsupported message type '{TYPE}'. Supported: ADT, ORM, ORU`.

---

### 4.1 ADT — Admission, Discharge, Transfer

Mensajes de gestión de pacientes. Se utilizan para sincronizar admisiones, altas, traslados y actualizaciones demográficas con los nodos Edge, permitiendo mantener el registro de pacientes actualizado en las modalidades DICOM.

**Segmentos requeridos para todos los ADT**: `MSH` + `EVN` + `PID` + `PV1`

#### Trigger Events ADT — Detalle Completo

##### `ADT^A01` — Admisión de Paciente (Admit/Visit Notification)

| Aspecto | Detalle |
|---------|---------|
| **Significado Clínico** | Un paciente ha sido formalmente admitido en una institución o servicio (hospitalización, urgencias, etc.) |
| **Cuándo enviar** | Al completar el registro de admisión en el HIS/ADT |
| **Acción en EdgeGuard** | Crea registro de paciente en el worklist del nodo destino. Las modalidades DICOM podrán consultar este paciente vía C-FIND MWL |
| **Campos clave** | `PID.3` (Patient ID), `PID.5` (Patient Name), `PV1.2` (Patient Class: `I`=Inpatient, `E`=Emergency), `PV1.3` (Location) |

```hl7
MSH|^~\&|HIS|HOSPITAL|EDGEGUARD|HUB|20260115143022||ADT^A01|MSG001|P|2.5
EVN|A01|20260115143022
PID|||PAT12345||GARCIA^JUAN||19850315|M
PV1||I|4N^401^1
```

##### `ADT^A02` — Transferencia de Paciente (Transfer a Patient)

| Aspecto | Detalle |
|---------|---------|
| **Significado Clínico** | Un paciente ha sido transferido de una ubicación física a otra dentro de la institución (cambio de cama, piso o servicio) |
| **Cuándo enviar** | Al registrar un cambio de ubicación en el sistema ADT |
| **Acción en EdgeGuard** | Actualiza la ubicación del paciente en el worklist. Puede redirigir al paciente a un nodo Edge diferente según las reglas de enrutamiento |
| **Campos clave** | `PID.3` (Patient ID), `PV1.3` (**nueva** ubicación: `Punto^Habitación^Cama`) |

```hl7
MSH|^~\&|HIS|HOSPITAL|EDGEGUARD|HUB|20260115160000||ADT^A02|MSG002|P|2.5
EVN|A02|20260115160000
PID|||PAT12345||GARCIA^JUAN||19850315|M
PV1||I|5S^502^2
```

##### `ADT^A03` — Alta de Paciente (Discharge a Patient)

| Aspecto | Detalle |
|---------|---------|
| **Significado Clínico** | Un paciente ha sido dado de alta formalmente de la institución |
| **Cuándo enviar** | Al registrar el alta médica o administrativa en el HIS |
| **Acción en EdgeGuard** | Marca al paciente como dado de alta. Los nodos Edge pueden limpiar el worklist para este paciente |
| **Campos clave** | `PID.3` (Patient ID), `PV1.2` (Patient Class) |

```hl7
MSH|^~\&|HIS|HOSPITAL|EDGEGUARD|HUB|20260116080000||ADT^A03|MSG003|P|2.5
EVN|A03|20260116080000
PID|||PAT12345||GARCIA^JUAN||19850315|M
PV1||I|4N^401^1
```

##### `ADT^A04` — Registro Ambulatorio (Register a Patient)

| Aspecto | Detalle |
|---------|---------|
| **Significado Clínico** | Un paciente ambulatorio se ha registrado para recibir servicios sin ser admitido formalmente (consulta externa, estudios programados) |
| **Cuándo enviar** | Al registrar un paciente ambulatorio en el sistema de admisión |
| **Acción en EdgeGuard** | Crea registro ambulatorio en el worklist. Funciona de forma análoga a `A01` pero con `PV1.2 = O` (Outpatient) |
| **Campos clave** | `PID.3` (Patient ID), `PID.5` (Patient Name), `PV1.2` (siempre `O` — Outpatient) |

```hl7
MSH|^~\&|HIS|HOSPITAL|EDGEGUARD|HUB|20260115090000||ADT^A04|MSG004|P|2.5
EVN|A04|20260115090000
PID|||PAT67890||LOPEZ^MARIA||19901220|F
PV1||O|CONS^101
```

##### `ADT^A08` — Actualización de Información (Update Patient Information)

| Aspecto | Detalle |
|---------|---------|
| **Significado Clínico** | Se ha actualizado información demográfica o administrativa del paciente sin cambio de estado de admisión |
| **Cuándo enviar** | Cuando se corrigen datos demográficos (nombre, fecha de nacimiento, sexo) o se actualiza ubicación, médico tratante u otros datos |
| **Acción en EdgeGuard** | Actualiza los datos del paciente en el worklist del nodo. Los campos actualizados se reflejan en consultas C-FIND MWL posteriores |
| **Campos clave** | `PID.3` (identifica al paciente a actualizar), `PID.5` (nuevo nombre), `PID.7`, `PID.8` |

> **⚠️ Enviar todos los campos demográficos**, no solo los que cambiaron. EdgeGuard reemplaza el registro completo.

```hl7
MSH|^~\&|HIS|HOSPITAL|EDGEGUARD|HUB|20260115110000||ADT^A08|MSG005|P|2.5
EVN|A08|20260115110000
PID|||PAT12345||GARCIA^JUAN^ANTONIO||19850315|M
PV1||I|4N^401^1
```

##### `ADT^A11` — Cancelación de Admisión (Cancel Admit)

| Aspecto | Detalle |
|---------|---------|
| **Significado Clínico** | Se cancela una admisión previamente registrada con `A01`. Indica que la admisión fue un error o fue revertida |
| **Cuándo enviar** | Cuando se revierte o anula un registro de admisión en el sistema ADT |
| **Acción en EdgeGuard** | Revierte el estado del paciente asociado al `A01` original. Los nodos Edge pueden eliminar el registro del worklist |
| **Campos clave** | `PID.3` (Patient ID cuya admisión se cancela) |

```hl7
MSH|^~\&|HIS|HOSPITAL|EDGEGUARD|HUB|20260115120000||ADT^A11|MSG006|P|2.5
EVN|A11|20260115120000
PID|||PAT12345||GARCIA^JUAN||19850315|M
PV1||I|4N^401^1
```

##### `ADT^A12` — Cancelación de Transferencia (Cancel Transfer)

| Aspecto | Detalle |
|---------|---------|
| **Significado Clínico** | Se cancela una transferencia previamente registrada con `A02`. El paciente permanece en su ubicación anterior |
| **Cuándo enviar** | Cuando se revierte un traslado de paciente en el sistema ADT |
| **Acción en EdgeGuard** | Restaura la ubicación anterior del paciente en el worklist |
| **Campos clave** | `PID.3` (Patient ID), `PV1.3` (ubicación original antes de la transferencia cancelada) |

```hl7
MSH|^~\&|HIS|HOSPITAL|EDGEGUARD|HUB|20260115130000||ADT^A12|MSG007|P|2.5
EVN|A12|20260115130000
PID|||PAT12345||GARCIA^JUAN||19850315|M
PV1||I|4N^401^1
```

##### `ADT^A13` — Cancelación de Alta (Cancel Discharge)

| Aspecto | Detalle |
|---------|---------|
| **Significado Clínico** | Se cancela un alta previamente registrada con `A03`. El paciente sigue admitido en la institución |
| **Cuándo enviar** | Cuando se revierte un alta de paciente (el paciente no fue realmente dado de alta, o se readmite inmediatamente) |
| **Acción en EdgeGuard** | Restaura el estado del paciente como admitido en el worklist del nodo |
| **Campos clave** | `PID.3` (Patient ID), `PV1.2` (Patient Class restaurado) |

```hl7
MSH|^~\&|HIS|HOSPITAL|EDGEGUARD|HUB|20260115140000||ADT^A13|MSG008|P|2.5
EVN|A13|20260115140000
PID|||PAT12345||GARCIA^JUAN||19850315|M
PV1||I|4N^401^1
```

#### Relación entre Trigger Events ADT

Los eventos ADT forman pares de acción/cancelación:

| Acción | Trigger | Cancelación | Trigger |
|--------|:-------:|-------------|:-------:|
| Admisión de paciente | `A01` | Cancelar admisión | `A11` |
| Transferencia | `A02` | Cancelar transferencia | `A12` |
| Alta de paciente | `A03` | Cancelar alta | `A13` |
| Registro ambulatorio | `A04` | — | — |
| Actualización de datos | `A08` | — | — |

> **Recomendación**: Si su sistema envía un evento de cancelación (`A11`, `A12`, `A13`), asegúrese de que el `PID.3` coincida exactamente con el del mensaje original para que EdgeGuard pueda correlacionar correctamente.

---

### 4.2 ORM — Order Message

Mensajes de órdenes médicas. Representan solicitudes de estudios radiológicos que generan entradas en el **Modality Worklist (MWL)**. Este es el tipo de mensaje **más importante** para la integración con modalidades DICOM, ya que permite que equipos como CT, MR, CR, etc. vean los estudios programados.

**Segmentos requeridos**: `MSH` + `PID` + `ORC` + `OBR`

#### `ORM^O01` — General Order

| Aspecto | Detalle |
|---------|---------|
| **Significado Clínico** | Se ha creado, modificado o cancelado una orden de estudio radiológico |
| **Cuándo enviar** | Al crear una nueva orden en el RIS/HIS, al modificar una orden existente, o al cancelar una orden |
| **Acción en EdgeGuard** | Genera o actualiza una entrada en el Modality Worklist del nodo Edge destino. Las modalidades DICOM podrán ver este estudio al consultar el MWL vía C-FIND |

#### Códigos de Control de Orden (`ORC.1`)

El campo `ORC.1` determina la acción a realizar sobre la orden:

| Código | Nombre | Significado Clínico | Acción en EdgeGuard |
|:------:|--------|---------------------|---------------------|
| `NW` | New Order | Nueva orden de estudio creada | Crea nueva entrada en el Worklist MWL |
| `CA` | Cancel Order | La orden ha sido cancelada | Elimina la entrada del Worklist si existe |
| `SC` | Status Changed | El estado de la orden ha cambiado | Actualiza el estado en el Worklist |
| `XO` | Change Order | Modificación de orden existente | Actualiza campos de la entrada existente |
| `DC` | Discontinue Order | Orden discontinuada | Marca como cancelada en el Worklist |

#### Estados de Orden (`ORC.5`)

| Código | Nombre | Descripción |
|:------:|--------|-------------|
| `SC` | Scheduled | Estudio programado — aparecerá en el MWL de la modalidad |
| `IP` | In Progress | Estudio en curso — la modalidad lo está realizando |
| `CM` | Complete | Estudio completado |
| `CA` | Cancelled | Orden cancelada |
| `HD` | Hold | Orden en espera — no aparece en MWL |

#### Campos Clave por Escenario ORM

| Escenario | Campos Críticos | Campos Recomendados |
|-----------|----------------|---------------------|
| **Nueva orden** (`NW`) | `PID.3`, `ORC.1=NW`, `OBR.4` (estudio) | `OBR.18` (accession), `OBR.27` (fecha programada), `OBR.16` (médico) |
| **Cancelar orden** (`CA`) | `PID.3`, `ORC.1=CA`, `ORC.2` o `OBR.2` (order number) | `OBR.18` (accession para identificar) |
| **Cambio de estado** (`SC`) | `PID.3`, `ORC.1=SC`, `ORC.5` (nuevo estado) | `OBR.18` (accession) |

**Ejemplo — Nueva Orden:**
```hl7
MSH|^~\&|RIS|HOSPITAL|EDGEGUARD|HUB|20260115150000||ORM^O01|MSG010|P|2.5
PID|||PAT12345||GARCIA^JUAN^ANTONIO||19850315|M
ORC|NW|ORD20260001|||SC
OBR||ORD20260001||CR001^CHEST XRAY^L|||20260120100000||||||||12345^SMITH^JOHN|||ACC20260001||||||||^^^20260120100000^^R
```

**Ejemplo — Cancelar una Orden:**
```hl7
MSH|^~\&|RIS|HOSPITAL|EDGEGUARD|HUB|20260115160000||ORM^O01|MSG011|P|2.5
PID|||PAT12345||GARCIA^JUAN||19850315|M
ORC|CA|ORD20260001|||CA
OBR||ORD20260001||CR001^CHEST XRAY^L||||||||||||||ACC20260001
```

---

### 4.3 ORU — Observation Result (Unsolicited)

Mensajes de resultados no solicitados. Contienen resultados de estudios radiológicos, reportes de lectura y observaciones clínicas. El sistema emisor los envía a EdgeGuard cuando un resultado está disponible.

**Segmentos requeridos**: `MSH` + `PID` + `OBR` + `OBX`

#### `ORU^R01` — Unsolicited Observation Result

| Aspecto | Detalle |
|---------|---------|
| **Significado Clínico** | Se ha generado un resultado o reporte de un estudio previamente ordenado |
| **Cuándo enviar** | Cuando un radiólogo firma un reporte (preliminar o final), o cuando el PACS genera resultados automáticos |
| **Acción en EdgeGuard** | Asocia el resultado al estudio en el worklist y lo distribuye al nodo Edge correspondiente |

#### Códigos de Estado del Resultado (`OBX.11`)

| Código | Nombre | Significado Clínico |
|:------:|--------|---------------------|
| `F` | Final | Resultado final — reporte firmado por el radiólogo. No se esperan cambios |
| `P` | Preliminary | Resultado preliminar — pendiente de revisión. Puede ser modificado |
| `C` | Correction | Corrección de un resultado previamente reportado como Final |
| `X` | Cancelled | Resultado cancelado — invalida un resultado previo |
| `I` | Incomplete | Resultado incompleto — aún en proceso de generación |

#### Tipos de Valor de Observación (`OBX.2`)

| Código | Nombre | Descripción | Ejemplo de `OBX.5` |
|:------:|--------|-------------|---------------------|
| `TX` | Text | Texto libre (reporte narrativo) | `Normal chest radiograph. No acute disease.` |
| `NM` | Numeric | Valor numérico | `120` |
| `CE` | Coded Entry | Entrada codificada `Código^Descripción^Sistema` | `F-04560^PNEUMONIA^SNM` |
| `FT` | Formatted Text | Texto con formato HL7 | `\.br\Line 1\.br\Line 2` |
| `ST` | String | Cadena de texto corta | `POSITIVE` |

#### Campos Clave para ORU

| Campo | Importancia | Descripción |
|-------|:-----------:|-------------|
| `PID.3` | ✅ Crítico | Identifica al paciente del resultado |
| `OBR.4` | ✅ Crítico | Identifica el estudio (debe coincidir con la orden original) |
| `OBR.18` | ⚠️ Alta | Accession Number — vincula resultado con orden ORM |
| `OBX.2` | ✅ Crítico | Tipo de dato del resultado |
| `OBX.3` | ✅ Crítico | Identificador de la observación (qué se reporta) |
| `OBX.5` | ✅ Crítico | Contenido del resultado |
| `OBX.11` | ✅ Crítico | Estado del resultado (`F`, `P`, `C`) |

**Ejemplo — Resultado Final:**
```hl7
MSH|^~\&|PACS|HOSPITAL|EDGEGUARD|HUB|20260120120000||ORU^R01|MSG020|P|2.5
PID|||PAT12345||GARCIA^JUAN^ANTONIO||19850315|M
OBR||ORD20260001||CR001^CHEST XRAY^L|||20260120100000||||||||12345^SMITH^JOHN|||ACC20260001
OBX|1|TX|RR001^RADIOLOGY REPORT||Normal chest radiograph. No acute cardiopulmonary disease.||||||F
```

**Ejemplo — Múltiples Observaciones:**
```hl7
MSH|^~\&|PACS|HOSPITAL|EDGEGUARD|HUB|20260120130000||ORU^R01|MSG022|P|2.5
PID|||PAT12345||GARCIA^JUAN||19850315|M
OBR||ORD20260001||CR001^CHEST XRAY^L||||||||||||||ACC20260001
OBX|1|TX|RR001^RADIOLOGY REPORT||Normal chest radiograph. No acute disease.||||||F
OBX|2|TX|RR002^CLINICAL IMPRESSION||No evidence of pneumonia or pleural effusion.||||||F
OBX|3|CE|RR003^FINDING CODE||F-01234^NORMAL^SNM||||||F
```

---

## 5. Estructura de Segmentos HL7

### Notación de Campos

Los campos HL7 se identifican con la notación `SEGMENTO.POSICIÓN`:
- `MSH.3` = Segmento MSH, campo en posición 3
- `PID.5` = Segmento PID, campo en posición 5

### Separadores Estándar

| Separador | Símbolo | Uso |
|-----------|---------|-----|
| Field Separator | `\|` | Separa campos dentro de un segmento |
| Component Separator | `^` | Separa componentes dentro de un campo |
| Repetition Separator | `~` | Separa repeticiones de un campo |
| Escape Character | `\` | Carácter de escape |
| Sub-Component Separator | `&` | Separa sub-componentes |

> Estos separadores se definen en `MSH.2` como `^~\&`.

---

## 6. Referencia Detallada de Campos por Segmento

Esta sección proporciona la referencia completa de cada campo HL7 que EdgeGuard procesa, incluyendo el tipo de dato HL7, la estructura de componentes para campos compuestos y las reglas de formato.

### Convenciones de Tipos de Dato HL7

| Tipo de Dato | Nombre | Formato | Ejemplo |
|:------------:|--------|---------|---------|
| `ST` | String | Texto libre | `MSG00001` |
| `ID` | Coded Value | Valor de tabla HL7 | `P` |
| `TS` | Timestamp | `YYYYMMDD[HHmmss[.SSSS]][+/-ZZZZ]` | `20260115143022` |
| `XPN` | Extended Person Name | `Last^First^Middle^Suffix^Prefix^Degree` | `GARCIA^JUAN^ANTONIO` |
| `XCN` | Extended Composite ID/Name | `ID^Last^First^Middle^Suffix^Prefix` | `12345^SMITH^JOHN` |
| `PL` | Person Location | `PointOfCare^Room^Bed^Facility` | `4N^401^1` |
| `CWE` | Coded With Exceptions | `Code^Description^CodingSystem` | `CR001^CHEST XRAY^L` |
| `TQ` | Timing/Quantity | `Qty^Interval^Duration^Start^End^Priority` | `^^^20260120100000^^R` |
| `IS` | Coded Value (User) | Valor de tabla definida por usuario | `I` |

---

### MSH — Message Header

El segmento MSH es **obligatorio** en todos los mensajes HL7 y debe ser siempre el primer segmento.

| Campo | Pos. | Nombre | Tipo Dato | Req. | Max | Descripción | Ejemplo |
|-------|:----:|--------|:---------:|:----:|:---:|-------------|---------|
| MSH.1 | 1 | Field Separator | `ST` | ✅ | 1 | Siempre `\|` | `\|` |
| MSH.2 | 2 | Encoding Characters | `ST` | ✅ | 4 | Siempre `^~\&` | `^~\&` |
| MSH.3 | 3 | Sending Application | `ST` | ⚠️ | 100 | Nombre de la aplicación origen (HIS, RIS, PACS) | `EPIC` |
| MSH.4 | 4 | Sending Facility | `ST` | ⚠️ | 100 | Nombre de la institución origen | `HOSPITAL_CENTRAL` |
| MSH.5 | 5 | Receiving Application | `ST` | ❌ | 100 | Aplicación destino | `EDGEGUARD` |
| MSH.6 | 6 | Receiving Facility | `ST` | ❌ | 100 | Institución destino | `EDGEGUARD_HUB` |
| MSH.7 | 7 | Date/Time of Message | `TS` | ❌ | 26 | Timestamp de creación del mensaje | `20260115143022` |
| MSH.8 | 8 | Security | `ST` | ❌ | 40 | Campo de seguridad | — |
| MSH.9 | 9 | Message Type | `CM` | ✅ | 20 | Tipo de mensaje compuesto | `ORM^O01` |
| MSH.10 | 10 | Message Control ID | `ST` | ✅ | 50 | Identificador único del mensaje | `MSG00001` |
| MSH.11 | 11 | Processing ID | `ID` | ❌ | 3 | Entorno de procesamiento | `P` |
| MSH.12 | 12 | Version ID | `ID` | ❌ | 10 | Versión del estándar HL7 | `2.5` |

**Leyenda**: ✅ = Obligatorio | ⚠️ = Recomendado (warning si vacío) | ❌ = Opcional

#### Formato de Componentes — MSH.9 (Message Type)

El campo `MSH.9` es un campo compuesto con el formato:

```
MessageType^TriggerEvent
```

| Componente | Descripción | Ejemplos |
|-----------|-------------|----------|
| 1 — Message Type | Tipo base del mensaje | `ADT`, `ORM`, `ORU` |
| 2 — Trigger Event | Evento específico que disparó el mensaje | `A01`, `O01`, `R01` |

**Ejemplos válidos**: `ADT^A01`, `ADT^A08`, `ORM^O01`, `ORU^R01`

#### Formato de Componentes — MSH.7 (Timestamp)

```
YYYYMMDD[HHmmss[.SSSS]][+/-ZZZZ]
```

| Formato | Ejemplo | Precisión |
|---------|---------|-----------|
| `YYYYMMDD` | `20260115` | Día |
| `YYYYMMDDHHmmss` | `20260115143022` | Segundo (recomendado) |
| `YYYYMMDDHHmmss.SSSS` | `20260115143022.1234` | Fracción de segundo |

#### Valores de MSH.11 (Processing ID)

| Valor | Nombre | Descripción |
|:-----:|--------|-------------|
| `P` | Production | Mensaje de producción (recomendado) |
| `D` | Debug | Mensaje de depuración |
| `T` | Training | Mensaje de entrenamiento/pruebas |

---

### EVN — Event Type (Requerido para ADT)

| Campo | Pos. | Nombre | Tipo Dato | Req. | Max | Descripción | Ejemplo |
|-------|:----:|--------|:---------:|:----:|:---:|-------------|---------|
| EVN.1 | 1 | Event Type Code | `ID` | ✅ | 3 | Código del evento — debe coincidir con el trigger de MSH.9 | `A01` |
| EVN.2 | 2 | Recorded Date/Time | `TS` | ❌ | 26 | Fecha/hora en que ocurrió el evento | `20260115143022` |
| EVN.5 | 5 | Operator ID | `XCN` | ❌ | 60 | ID del operador que registró el evento | `ADMIN` |

> **⚠️ EVN.1 debe coincidir con el trigger event de MSH.9**. Si `MSH.9 = ADT^A01`, entonces `EVN.1 = A01`.

#### Valores de EVN.1 por Tipo de Mensaje

| Mensaje | Valores EVN.1 Válidos |
|---------|----------------------|
| ADT | `A01`, `A02`, `A03`, `A04`, `A08`, `A11`, `A12`, `A13` |

---

### PID — Patient Identification

| Campo | Pos. | Nombre | Tipo Dato | Req. | Max | Descripción | Ejemplo |
|-------|:----:|--------|:---------:|:----:|:---:|-------------|---------|
| PID.2 | 2 | Patient ID (External) | `ST` | ❌ | 100 | ID externo del paciente (asignado por sistema externo) | `EXT001` |
| PID.3 | 3 | Patient ID (Internal) | `ST` | ✅ | 100 | ID interno del paciente — **identificador principal** | `PAT12345` |
| PID.5 | 5 | Patient Name | `XPN` | ⚠️ | 200 | Nombre completo del paciente (campo compuesto) | `GARCIA^JUAN^ANTONIO` |
| PID.7 | 7 | Date of Birth | `TS` | ❌ | 8 | Fecha de nacimiento | `19850315` |
| PID.8 | 8 | Administrative Sex | `IS` | ❌ | 1 | Sexo administrativo | `M` |
| PID.18 | 18 | Patient Account Number | `ST` | ❌ | 20 | Número de cuenta del paciente | `ACC001` |

#### Formato de Componentes — PID.5 (Patient Name / XPN)

```
FamilyName^GivenName^MiddleName^Suffix^Prefix^Degree
```

| Componente | Descripción | Ejemplo |
|-----------|-------------|---------|
| 1 — Family Name | Apellido paterno / apellido principal | `GARCIA` |
| 2 — Given Name | Nombre de pila | `JUAN` |
| 3 — Middle Name | Segundo nombre o apellido materno | `ANTONIO` |
| 4 — Suffix | Sufijo (Jr., Sr., III) | `JR` |
| 5 — Prefix | Prefijo (Dr., Mr., Sra.) | `DR` |
| 6 — Degree | Título académico | `MD` |

**Ejemplos válidos**:
- `GARCIA^JUAN` — Apellido y nombre
- `GARCIA^JUAN^ANTONIO` — Con segundo nombre
- `GARCIA^JUAN^^JR` — Con sufijo (componente 3 vacío)

#### Formato de PID.7 (Date of Birth)

```
YYYYMMDD
```

**Ejemplo**: `19850315` = 15 de marzo de 1985

#### Valores de PID.8 (Administrative Sex)

| Valor | Descripción |
|:-----:|-------------|
| `M` | Masculino |
| `F` | Femenino |
| `O` | Otro |
| `U` | Desconocido |

---

### PV1 — Patient Visit (Requerido para ADT)

| Campo | Pos. | Nombre | Tipo Dato | Req. | Max | Descripción | Ejemplo |
|-------|:----:|--------|:---------:|:----:|:---:|-------------|---------|
| PV1.2 | 2 | Patient Class | `IS` | ✅ | 1 | Clasificación del paciente | `I` |
| PV1.3 | 3 | Assigned Patient Location | `PL` | ❌ | 80 | Ubicación física del paciente (campo compuesto) | `4N^401^1` |
| PV1.7 | 7 | Attending Doctor | `XCN` | ❌ | 60 | Médico tratante (campo compuesto) | `12345^SMITH^JOHN` |
| PV1.19 | 19 | Visit Number | `ST` | ❌ | 20 | Número de visita/encuentro | `V001` |

#### Valores de PV1.2 (Patient Class)

| Valor | Nombre | Descripción | Trigger Events Típicos |
|:-----:|--------|-------------|----------------------|
| `I` | Inpatient | Paciente hospitalizado | `A01`, `A02`, `A03` |
| `O` | Outpatient | Paciente ambulatorio | `A04` |
| `E` | Emergency | Urgencias | `A01` |
| `P` | Preadmit | Pre-admisión | — |
| `R` | Recurring | Paciente recurrente | — |

#### Formato de Componentes — PV1.3 (Patient Location / PL)

```
PointOfCare^Room^Bed^Facility^LocationStatus^PersonLocationType^Building^Floor
```

| Componente | Descripción | Ejemplo |
|-----------|-------------|---------|
| 1 — Point of Care | Unidad/servicio (ej: piso, ala) | `4N` (4to piso Norte) |
| 2 — Room | Número de habitación | `401` |
| 3 — Bed | Número de cama | `1` |
| 4 — Facility | Identificador de instalación | `HOSP_CENTRAL` |

**Ejemplos válidos**:
- `4N^401^1` — Piso 4 Norte, habitación 401, cama 1
- `ER^TRAUMA^3` — Urgencias, área de trauma, posición 3
- `CONS^101` — Consultorio 101 (ambulatorio)

#### Formato de Componentes — PV1.7 (Attending Doctor / XCN)

```
IDNumber^FamilyName^GivenName^MiddleName^Suffix^Prefix^Degree
```

| Componente | Descripción | Ejemplo |
|-----------|-------------|---------|
| 1 — ID Number | Identificador del médico | `12345` |
| 2 — Family Name | Apellido | `SMITH` |
| 3 — Given Name | Nombre | `JOHN` |

**Ejemplo**: `12345^SMITH^JOHN`

---

### ORC — Common Order (Requerido para ORM)

| Campo | Pos. | Nombre | Tipo Dato | Req. | Max | Descripción | Ejemplo |
|-------|:----:|--------|:---------:|:----:|:---:|-------------|---------|
| ORC.1 | 1 | Order Control | `ID` | ✅ | 2 | Código de acción sobre la orden | `NW` |
| ORC.2 | 2 | Placer Order Number | `ST` | ❌ | 22 | Número de orden asignado por el solicitante (HIS/RIS) | `ORD001` |
| ORC.3 | 3 | Filler Order Number | `ST` | ❌ | 22 | Número de orden asignado por el ejecutor (PACS/Modalidad) | `FIL001` |
| ORC.5 | 5 | Order Status | `ID` | ❌ | 2 | Estado actual de la orden | `SC` |

#### Valores de ORC.1 (Order Control)

| Código | Nombre | Cuándo Usar |
|:------:|--------|-------------|
| `NW` | New Order | Crear una nueva orden de estudio |
| `CA` | Cancel | Cancelar una orden existente |
| `SC` | Status Changed | Notificar cambio de estado de una orden |
| `XO` | Change Order | Modificar campos de una orden existente |
| `DC` | Discontinue | Discontinuar una orden |

#### Valores de ORC.5 (Order Status)

| Código | Nombre | Descripción |
|:------:|--------|-------------|
| `SC` | Scheduled | Programado — visible en MWL |
| `IP` | In Progress | En ejecución por la modalidad |
| `CM` | Complete | Completado |
| `CA` | Cancelled | Cancelado |
| `HD` | Hold | En espera |

---

### OBR — Observation Request (Requerido para ORM y ORU)

| Campo | Pos. | Nombre | Tipo Dato | Req. | Max | Descripción | Ejemplo |
|-------|:----:|--------|:---------:|:----:|:---:|-------------|---------|
| OBR.2 | 2 | Placer Order Number | `ST` | ❌ | 22 | Número de orden del solicitante | `ORD001` |
| OBR.4 | 4 | Universal Service ID | `CWE` | ✅ | 200 | Identificador del estudio (campo compuesto) | `CR001^CHEST XRAY^L` |
| OBR.16 | 16 | Ordering Provider | `XCN` | ❌ | 60 | Médico solicitante (campo compuesto) | `12345^SMITH^JOHN` |
| OBR.18 | 18 | Placer Field 1 (Accession) | `ST` | ⚠️ | 100 | Número de acceso del estudio — clave para trazabilidad | `ACC20260001` |
| OBR.27 | 27 | Quantity/Timing | `TQ` | ❌ | 200 | Fecha/hora programada (campo compuesto) | `^^^20260120100000` |
| OBR.31 | 31 | Reason for Study | `ST` | ❌ | 300 | Razón clínica para el estudio | `CHEST PAIN` |
| OBR.44 | 44 | Procedure Code | `CWE` | ❌ | 200 | Código de procedimiento | `71020^CHEST 2 VIEW^CPT` |

#### Formato de Componentes — OBR.4 (Universal Service ID / CWE)

```
Identifier^Text^NameOfCodingSystem
```

| Componente | Descripción | Ejemplo |
|-----------|-------------|---------|
| 1 — Identifier | Código del estudio en el sistema emisor | `CR001` |
| 2 — Text | Descripción legible del estudio | `CHEST XRAY` |
| 3 — Coding System | Nombre del sistema de codificación | `L` (local), `CPT`, `LOINC` |

**Ejemplos válidos**:
- `CR001^CHEST XRAY^L` — Código local de radiografía de tórax
- `71020^CHEST 2 VIEW^CPT` — Código CPT
- `RAD-CT-001^CT ABDOMEN WITH CONTRAST^L` — Tomografía con contraste

#### Formato de Componentes — OBR.27 (Quantity/Timing / TQ)

```
Quantity^Interval^Duration^StartDateTime^EndDateTime^Priority
```

Para programar la fecha del estudio, solo se utilizan los componentes 4 y 6:

| Componente | Descripción | Ejemplo |
|-----------|-------------|---------|
| 4 — Start DateTime | Fecha/hora programada del estudio (`YYYYMMDDHHmmss`) | `20260120100000` |
| 6 — Priority | Prioridad del estudio | `R` (Routine), `S` (Stat/Urgente) |

**Ejemplo**: `^^^20260120100000^^R` = Programado para 20 enero 2026 a las 10:00, prioridad rutinaria.

#### Valores de Prioridad en OBR.27

| Valor | Nombre | Descripción |
|:-----:|--------|-------------|
| `R` | Routine | Prioridad normal |
| `S` | Stat | Urgente — atención inmediata |
| `A` | ASAP | Lo antes posible |
| `T` | Timed | Programado a hora específica |

---

### OBX — Observation Result (Requerido para ORU)

| Campo | Pos. | Nombre | Tipo Dato | Req. | Max | Descripción | Ejemplo |
|-------|:----:|--------|:---------:|:----:|:---:|-------------|---------|
| OBX.1 | 1 | Set ID | `ST` | ❌ | 4 | Número secuencial de observación (1, 2, 3...) | `1` |
| OBX.2 | 2 | Value Type | `ID` | ✅ | 3 | Tipo de dato del valor en OBX.5 | `TX` |
| OBX.3 | 3 | Observation Identifier | `CWE` | ✅ | 200 | Identificador de la observación (campo compuesto) | `RR001^RADIOLOGY REPORT` |
| OBX.5 | 5 | Observation Value | Varía | ✅ | 65536 | Valor/resultado de la observación | `Normal chest radiograph` |
| OBX.11 | 11 | Observation Result Status | `ID` | ✅ | 1 | Estado del resultado | `F` |

#### Formato de Componentes — OBX.3 (Observation Identifier / CWE)

```
Identifier^Text^NameOfCodingSystem
```

| Componente | Descripción | Ejemplo |
|-----------|-------------|---------|
| 1 — Identifier | Código de la observación | `RR001` |
| 2 — Text | Descripción de la observación | `RADIOLOGY REPORT` |
| 3 — Coding System | Sistema de codificación (opcional) | `L`, `LOINC` |

**Ejemplos válidos**:
- `RR001^RADIOLOGY REPORT` — Reporte de radiología (código local)
- `18782-3^Radiology Study observation^LN` — LOINC coding

#### Valores de OBX.2 (Value Type)

| Código | Nombre | Tipo de Valor en OBX.5 |
|:------:|--------|------------------------|
| `TX` | Text | Texto libre (reporte narrativo) — más común para radiología |
| `NM` | Numeric | Valor numérico |
| `CE` | Coded Entry | `Código^Descripción^Sistema` |
| `FT` | Formatted Text | Texto con escapes de formato HL7 (`\.br\` = line break) |
| `ST` | String | Cadena de texto corta |

#### Valores de OBX.11 (Observation Result Status)

| Código | Nombre | Descripción |
|:------:|--------|-------------|
| `F` | Final | Resultado final firmado — definitivo |
| `P` | Preliminary | Resultado preliminar — puede cambiar |
| `C` | Correction | Corrección de resultado final previo |
| `X` | Cancelled | Resultado cancelado |
| `I` | Incomplete | Resultado incompleto |

#### Múltiples Observaciones en un Mensaje ORU

Un mensaje `ORU^R01` puede contener múltiples segmentos OBX, cada uno representando una observación diferente:

```hl7
OBX|1|TX|RR001^RADIOLOGY REPORT||Texto del reporte narrativo...||||||F
OBX|2|TX|RR002^CLINICAL IMPRESSION||Impresión clínica del radiólogo...||||||F
OBX|3|CE|RR003^FINDING CODE||F-01234^NORMAL^SNM||||||F
```

> **Nota**: El campo `OBX.1` (Set ID) debe ser secuencial: `1`, `2`, `3`, etc.

---

## 7. Segmentos Requeridos por Tipo de Mensaje

La validación Enterprise exige la presencia de segmentos específicos según el tipo de mensaje:

| Tipo | MSH | EVN | PID | PV1 | ORC | OBR | OBX |
|------|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| **ADT** | ✅ | ✅ | ✅ | ✅ | — | — | — |
| **ORM** | ✅ | — | ✅ | — | ✅ | ✅ | — |
| **ORU** | ✅ | — | ✅ | — | — | ✅ | ✅ |

> Si un segmento requerido está ausente, el mensaje es rechazado con error: `"Required segment '{SEGMENTO}' missing for {TIPO} message"`

---

## 8. Reglas de Validación

EdgeGuard aplica las siguientes reglas de validación en orden secuencial sobre cada mensaje recibido. La validación se detiene al primer error (fail-fast).

### Errores (rechazan el mensaje)

| # | Regla | Campo | Mensaje de Error |
|---|-------|-------|------------------|
| E1 | Tipo de mensaje extraíble | MSH.9 | `MSH.9: Message type could not be extracted` |
| E2 | Tipo de mensaje soportado | MSH.9 | `Unsupported message type '{TYPE}'. Supported: ADT, ORM, ORU` |
| E3 | Segmentos requeridos presentes | Varios | `Required segment '{SEG}' missing for {TYPE} message` |
| E4 | Identificación de paciente | PID.3 | `PID.3: Patient ID is required` |

### Warnings (no rechazan, se registran en log)

| # | Regla | Campo | Mensaje de Warning |
|---|-------|-------|-------------------|
| W1 | Sending Application presente | MSH.3 | `MSH.3: Sending Application is empty` |
| W2 | Sending Facility presente | MSH.4 | `MSH.4: Sending Facility is empty` |
| W3 | Accession Number en ORM/ORU | OBR.18 | `OBR.18: Accession Number is empty for {TYPE} message` |
| W4 | Patient Name presente | PID.5 | `PID.5: Patient Name is empty` |

### Flujo de Validación

```
Mensaje recibido
       │
       ▼
  ┌─ MSH.9 extraíble? ──── NO ──► ERROR: Message type not extracted
  │    YES
  ▼
  ┌─ Tipo soportado? ───── NO ──► ERROR: Unsupported type
  │    YES
  ▼
  ┌─ Segmentos requeridos? ─ NO ──► ERROR: Missing segment
  │    YES
  ▼
  ┌─ MSH.3 presente? ──── NO ──► WARNING (continúa)
  ├─ MSH.4 presente? ──── NO ──► WARNING (continúa)
  ▼
  ┌─ PID.3 presente? ──── NO ──► ERROR: Patient ID required
  │    YES
  ▼
  ┌─ OBR.18 en ORM/ORU? ── NO ──► WARNING (continúa)
  ├─ PID.5 presente? ──── NO ──► WARNING (continúa)
  ▼
  ✅ VALIDACIÓN EXITOSA (con 0-N warnings)
```

---

## 9. Motor de Enrutamiento

El motor de enrutamiento de EdgeGuard determina a qué Edge Node se envía cada mensaje HL7 validado.

### Algoritmo

1. Obtener todas las reglas habilitadas ordenadas por **prioridad ascendente** (menor número = mayor prioridad)
2. Para cada regla, evaluar si el mensaje coincide con los criterios de la regla
3. **First match wins** — la primera regla que coincida determina el destino
4. Si la regla coincide pero el nodo destino no está disponible, continuar con la siguiente regla
5. Si ninguna regla coincide → **Fallback**: enviar al primer nodo activo disponible
6. Si no hay nodos activos → el mensaje queda en estado `Processed` sin ruta

### Criterios de Matching

Cada regla puede filtrar por uno o más criterios. Un criterio `null` significa "cualquier valor".

| Criterio | Campo del Mensaje | Comparación |
|----------|-------------------|-------------|
| `MatchMessageType` | `MSH.9` (tipo base) | Case-insensitive |
| `MatchTriggerEvent` | `MSH.9` (trigger event) | Case-insensitive |
| `MatchSendingFacility` | `MSH.4` | Case-insensitive |
| `MatchSendingApplication` | `MSH.3` | Case-insensitive |

### Ejemplo de Reglas

| Prioridad | Nombre | MessageType | TriggerEvent | Facility | Application | Nodo Destino |
|:---------:|--------|:-----------:|:------------:|:--------:|:-----------:|:------------:|
| 1 | Urgencias ORM | `ORM` | `O01` | `ER` | — | Node-ER |
| 5 | ADT Hospital Central | `ADT` | — | `HOSP_CENTRAL` | — | Node-Central |
| 10 | Catch-all ORM | `ORM` | — | — | — | Node-Default |
| 100 | Default | — | — | — | — | Node-Fallback |

### Prioridad de Fallback

Cuando ninguna regla coincide, el sistema aplica routing por defecto:
- **Priority**: `10` (configurable vía `FallbackPriority`)
- **RuleId**: `FALLBACK`
- **Target**: Primer nodo activo disponible

---

## 10. Ciclo de Vida del Mensaje

Cada mensaje HL7 tiene **dos máquinas de estado** independientes que se rastrean en paralelo:

### Estado de Procesamiento (`Hl7MessageStatus`)

```
  ┌──────────┐      MarkAsProcessing()     ┌────────────┐     MarkAsProcessed()    ┌───────────┐
  │ Received │ ──────────────────────────► │ Processing │ ──────────────────────► │ Processed │
  └──────────┘                             └────────────┘                         └───────────┘
                                                │
                                       MarkAsFailed(error)
                                                │
                                                ▼
                                           ┌────────┐
                                           │ Failed │
                                           └────────┘
```

| Estado | Descripción |
|--------|-------------|
| `Received` | Mensaje recibido por el TCP Listener y persistido |
| `Processing` | Pipeline de validación/routing en progreso |
| `Processed` | Pipeline completado exitosamente |
| `Failed` | Error en validación o routing |

### Estado de Dispatch (`Hl7DispatchStatus`)

```
┌───────────────────┐
│ PendingValidation │
└─────────┬─────────┘
          │
     ┌────┴─────┐
     ▼          ▼
┌───────────┐  ┌──────────────────┐
│ Validated │  │ ValidationFailed │
└─────┬─────┘  └──────────────────┘
      │
      ▼
  ┌────────┐
  │ Routed │
  └───┬────┘
      │
      ▼
  ┌────────┐       ┌─────────────┐
  │ Queued │◄──────│ Requeued    │ (retry)
  └───┬────┘       └─────────────┘
      │                   ▲
      ▼                   │
┌──────────────┐          │
│ Dispatching  │──────────┘ (dispatch failed, retries left)
└──────┬───────┘
       │
  ┌────┴─────┐
  ▼          ▼
┌───────────┐  ┌────────────────┐
│ Delivered │  │ DeliveryFailed │ (max retries exceeded)
└───────────┘  └────────────────┘
```

| Estado | Descripción |
|--------|-------------|
| `PendingValidation` | Inicial — esperando validación |
| `Validated` | Validación exitosa, esperando routing |
| `ValidationFailed` | Validación rechazó el mensaje |
| `Routed` | Nodo destino asignado |
| `Queued` | En cola de dispatch |
| `Dispatching` | Intento de envío al nodo en progreso |
| `Delivered` | Entregado exitosamente al nodo |
| `DeliveryFailed` | Falló después de agotar reintentos |

---

## 11. Respuesta ACK — Formato y Ejemplos

EdgeGuard responde con un ACK HL7 v2.x estándar inmediatamente después de recibir cada mensaje.

### Estructura del ACK

```
<VT>MSH|^~\&|EdgeGuardHub|EdgeGuard|{OrigSendingApp}|{OrigSendingFacility}|{Timestamp}||ACK|{AckId}|P|2.5<CR>MSA|AA|{OrigMessageControlId}<CR><FS><CR>
```

### Descripción de Campos del ACK

#### Segmento MSH del ACK

| Campo | Valor | Descripción |
|-------|-------|-------------|
| MSH.3 | `EdgeGuardHub` | Aplicación que envía el ACK |
| MSH.4 | `EdgeGuard` | Facility que envía el ACK |
| MSH.5 | `{OrigSendingApp}` | Eco de la aplicación original |
| MSH.6 | `{OrigSendingFacility}` | Eco de la facility original |
| MSH.7 | `yyyyMMddHHmmss` | Timestamp del ACK |
| MSH.9 | `ACK` | Tipo de mensaje |
| MSH.10 | `{GUID[0:10]}` | ID único del ACK (10 chars) |
| MSH.11 | `P` | Processing ID (Production) |
| MSH.12 | `2.5` | Versión HL7 |

#### Segmento MSA del ACK

| Campo | Valor | Descripción |
|-------|-------|-------------|
| MSA.1 | `AA` | Application Accept — mensaje recibido |
| MSA.2 | `{OrigMessageControlId}` | Message Control ID del mensaje **original** |

### Códigos MSA.1 — Acknowledgment Codes

| Código | Nombre | Significado |
|--------|--------|-------------|
| `AA` | Application Accept | Mensaje recibido y aceptado para procesamiento |
| `AE` | Application Error | Error en la aplicación al procesar el mensaje |
| `AR` | Application Reject | Mensaje rechazado (formato inválido, tipo no soportado) |

> **Nota**: EdgeGuard actualmente responde siempre `AA` en la capa TCP (recepción). Los errores de validación y routing se manejan de forma asíncrona en el pipeline.

### Ejemplo Completo de ACK

**Mensaje Original (enviado por HIS):**
```hl7
MSH|^~\&|EPIC|HOSPITAL_CENTRAL|EDGEGUARD|HUB|20260115143022||ORM^O01|MSG00001|P|2.5
PID|||PAT12345||GARCIA^JUAN||19850315|M
ORC|NW|ORD001|||SC
OBR||ORD001||CR001^CHEST XRAY|||20260120100000
```

**ACK Response (enviado por EdgeGuard):**
```hl7
MSH|^~\&|EdgeGuardHub|EdgeGuard|EPIC|HOSPITAL_CENTRAL|20260115143022||ACK|A1B2C3D4E5|P|2.5
MSA|AA|MSG00001
```

> El ACK se envuelve con delimitadores MLLP: `\x0B` al inicio, `\x1C\r` al final.

---

## 12. Dispatch Hub → Node (Worklist Push)

Una vez que el mensaje HL7 es validado, enrutado y encolado, EdgeGuard lo envía automáticamente al Edge Node destino vía HTTP POST.

### Endpoint del Node

```
POST {NodeApiEndpoint}/api/hl7/worklist
Content-Type: application/json
```

### Request Body — `Hl7WorklistPushRequest`

| Campo | Tipo | Obligatorio | Descripción |
|-------|------|:-----------:|-------------|
| `HubMessageId` | `Guid` | ✅ | ID del mensaje en la BD del Hub |
| `MessageType` | `string` | ✅ | Tipo de mensaje HL7 (ADT, ORM, ORU) |
| `TriggerEvent` | `string?` | ❌ | Trigger event (A01, O01, R01) |
| `RawContent` | `string` | ✅ | Contenido HL7 crudo completo |
| `PatientId` | `string?` | ❌ | PID.3 — ID del paciente |
| `PatientName` | `string?` | ❌ | PID.5 — Nombre del paciente |
| `PatientBirthDate` | `string?` | ❌ | PID.7 — Fecha de nacimiento |
| `PatientSex` | `string?` | ❌ | PID.8 — Sexo |
| `AccessionNumber` | `string?` | ❌ | OBR.18 — Número de acceso |
| `StudyInstanceUid` | `string?` | ❌ | UID del estudio DICOM |
| `ReferringPhysicianName` | `string?` | ❌ | Médico referente |
| `ProcedureDescription` | `string?` | ❌ | Descripción del procedimiento |
| `RequestedProcedureId` | `string?` | ❌ | ID del procedimiento solicitado |
| `Modality` | `string?` | ❌ | Modalidad DICOM (CT, MR, CR, etc.) |
| `ScheduledDateTime` | `DateTime?` | ❌ | Fecha/hora programada |
| `ScheduledStationAeTitle` | `string?` | ❌ | AE Title de la estación programada |
| `ScheduledPerformingPhysicianName` | `string?` | ❌ | Médico programado |
| `ScheduledProcedureStepId` | `string?` | ❌ | ID del paso de procedimiento |
| `SendingFacility` | `string?` | ❌ | Facility de origen |
| `SendingApplication` | `string?` | ❌ | Aplicación de origen |
| `Priority` | `int` | ❌ | Prioridad (default: 5, 1=máxima) |
| `SentAtUtc` | `DateTime` | ❌ | Timestamp de envío |

### Response Body — `Hl7WorklistPushResponse`

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Accepted` | `bool` | `true` si el nodo aceptó el mensaje |
| `NodeAckId` | `string?` | ID de acknowledgment del nodo (si aceptado) |
| `Error` | `string?` | Mensaje de error (si rechazado) |
| `ReceivedAtUtc` | `DateTime` | Timestamp de recepción en el nodo |

### Ejemplo de Request

```json
{
  "hubMessageId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "messageType": "ORM",
  "triggerEvent": "O01",
  "rawContent": "MSH|^~\\&|EPIC|HOSPITAL|...",
  "patientId": "PAT12345",
  "patientName": "GARCIA^JUAN",
  "accessionNumber": "ACC20260001",
  "modality": "CR",
  "scheduledDateTime": "2026-01-20T10:00:00Z",
  "sendingFacility": "HOSPITAL_CENTRAL",
  "sendingApplication": "EPIC",
  "priority": 5,
  "sentAtUtc": "2026-01-15T14:30:22Z"
}
```

### Ejemplo de Response (Éxito)

```json
{
  "accepted": true,
  "nodeAckId": "NODE-ACK-001",
  "receivedAtUtc": "2026-01-15T14:30:23Z"
}
```

### Ejemplo de Response (Rechazo)

```json
{
  "accepted": false,
  "error": "Worklist capacity exceeded",
  "receivedAtUtc": "2026-01-15T14:30:23Z"
}
```

### Política de Reintentos

| Parámetro | Default | Descripción |
|-----------|---------|-------------|
| `MaxRetries` | 5 | Intentos máximos antes de marcar como `DeliveryFailed` |
| `RetryDelaySeconds` | 30 | Delay entre reintentos |
| `DispatchTimeoutSeconds` | 30 | Timeout del HTTP request |
| `DispatchBatchSize` | 20 | Mensajes procesados por ciclo |
| `DispatchIntervalSeconds` | 5 | Intervalo entre ciclos de dispatch |

---

## 13. API de Monitoreo

EdgeGuard expone endpoints REST para monitorear el sistema HL7 en tiempo real.

### Endpoints del Listener

| Método | Ruta | Descripción | Response |
|--------|------|-------------|----------|
| `GET` | `/api/hl7status/status` | Estado del listener TCP | `Hl7ListenerStatusDto` |
| `GET` | `/api/hl7status/recent-messages?count=10` | Mensajes recientes | `Hl7MessageSummaryDto[]` |
| `GET` | `/api/hl7status/messages/{id}` | Detalle de un mensaje por ID | `Hl7MessageDetailDto` |

### Endpoints de Cola / Dispatch

| Método | Ruta | Descripción | Response |
|--------|------|-------------|----------|
| `GET` | `/api/queuemonitoring/summary` | Resumen de la cola por estado | `QueueSummaryDto` |
| `GET` | `/api/queuemonitoring/by-dispatch-status/{status}` | Mensajes filtrados por dispatch status | `Hl7MessageDto[]` |
| `GET` | `/api/queuemonitoring/queued?batchSize=50` | Mensajes en cola pendientes | `Hl7MessageQueuedDto[]` |

### Response DTOs

#### `Hl7ListenerStatusDto`

```json
{
  "isRunning": true,
  "port": 8001,
  "activeConnections": 3
}
```

#### `QueueSummaryDto`

```json
{
  "pendingValidation": 0,
  "validated": 2,
  "routed": 1,
  "queued": 15,
  "dispatching": 3,
  "delivered": 1247,
  "deliveryFailed": 2,
  "validationFailed": 5,
  "totalInPipeline": 21
}
```

> **`totalInPipeline`** = `pendingValidation + validated + routed + queued + dispatching` (no incluye estados terminales).

#### `Hl7MessageSummaryDto`

```json
{
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "messageType": "ORM",
  "sendingApplication": "EPIC",
  "sendingFacility": "HOSPITAL_CENTRAL",
  "receivedAt": "2026-01-15T14:30:22Z",
  "clientEndpoint": "192.168.1.100:54321",
  "status": "Processed",
  "processedAt": "2026-01-15T14:30:22.5Z",
  "errorMessage": null
}
```

#### `Hl7MessageDetailDto`

Igual que `Hl7MessageSummaryDto` + campo `content` con el mensaje HL7 crudo completo.

---

## 14. Configuración del Listener

La configuración del TCP Listener se gestiona de forma centralizada por el administrador de EdgeGuard.

### Parámetros de Configuración

| Setting Key | Tipo | Default | Rango | Descripción |
|-------------|------|---------|-------|-------------|
| `hl7.tcp.port` | `int` | `8001` | 1024-65535 | Puerto TCP del listener |
| `hl7.tcp.enabled` | `bool` | `true` | — | Habilitar/deshabilitar listener |
| `hl7.max_concurrent_connections` | `int` | `100` | 1-1000 | Conexiones TCP simultáneas |
| `hl7.max_queued_messages` | `int` | `1000` | 100-50000 | Capacidad del channel interno |
| `hl7.processing_workers` | `int` | `4` | 1-32 | Workers de procesamiento paralelo |
| `hl7.connection_timeout_ms` | `int` | `300000` | 10000-600000 | Timeout por conexión (5 min) |
| `hl7.buffer_size` | `int` | `8192` | 1024-65536 | Buffer de lectura TCP (bytes) |

### Recomendaciones de Producción

| Escenario | Connections | Workers | Buffer | Queue |
|-----------|:-----------:|:-------:|:------:|:-----:|
| Bajo volumen (<100 msg/día) | 20 | 2 | 8192 | 500 |
| Medio volumen (100-1000 msg/día) | 50 | 4 | 8192 | 1000 |
| Alto volumen (>1000 msg/día) | 100 | 8 | 16384 | 5000 |
| Enterprise (>10000 msg/día) | 200 | 16 | 32768 | 10000 |

---

## 15. Configuración del Dispatch Worker

### Parámetros de Configuración

| Setting Key | Tipo | Default | Descripción |
|-------------|------|---------|-------------|
| `dispatch.enabled` | `bool` | `true` | Habilitar/deshabilitar dispatch |
| `dispatch.batch_size` | `int` | `20` | Mensajes por ciclo de dispatch |
| `dispatch.interval_seconds` | `int` | `5` | Segundos entre ciclos |
| `dispatch.max_retries` | `int` | `5` | Reintentos antes de `DeliveryFailed` |
| `dispatch.retry_delay_seconds` | `int` | `30` | Delay entre reintentos |
| `dispatch.timeout_seconds` | `int` | `30` | Timeout HTTP por intento |
| `queue.max_pending_messages` | `int` | `10000` | Máximo de mensajes pendientes |
| `queue.retention_days` | `int` | `30` | Días de retención de mensajes |

---

## 16. Ejemplos de Mensajes Completos

### ADT^A01 — Admisión de Paciente

```hl7
MSH|^~\&|EPIC|HOSPITAL_CENTRAL|EDGEGUARD|HUB|20260115143022||ADT^A01|MSG001|P|2.5
EVN|A01|20260115143022
PID|||PAT12345||GARCIA^JUAN^ANTONIO||19850315|M|||AV REFORMA 100^^CDMX^MX^06600||5551234567
PV1||I|4N^401^1|||12345^SMITH^JOHN|||MED||||ADM|||12345^SMITH^JOHN|||||||||||||||||||||||||20260115143022
```

### ORM^O01 — Orden de Estudio Radiológico

```hl7
MSH|^~\&|RIS_SYSTEM|HOSPITAL_CENTRAL|EDGEGUARD|HUB|20260115150000||ORM^O01|MSG002|P|2.5
PID|||PAT12345||GARCIA^JUAN^ANTONIO||19850315|M
ORC|NW|ORD20260001|||SC
OBR||ORD20260001||CR001^CHEST XRAY^L|||20260120100000||||||||12345^SMITH^JOHN|||ACC20260001||||||||^^^20260120100000^^R
```

### ORU^R01 — Resultado de Estudio

```hl7
MSH|^~\&|PACS|HOSPITAL_CENTRAL|EDGEGUARD|HUB|20260120120000||ORU^R01|MSG003|P|2.5
PID|||PAT12345||GARCIA^JUAN^ANTONIO||19850315|M
OBR||ORD20260001||CR001^CHEST XRAY^L|||20260120100000||||||||12345^SMITH^JOHN|||ACC20260001
OBX|1|TX|RR001^RADIOLOGY REPORT||Normal chest radiograph. No acute cardiopulmonary disease.||||||F
```

---

## 17. FAQ — Errores Comunes y Soluciones

### 🔴 Errores de Conexión TCP

| Error | Causa Probable | Solución |
|-------|---------------|----------|
| `Connection refused on port 8001` | Listener no iniciado o puerto bloqueado | Verificar que `hl7.tcp.enabled = true` y que el puerto no esté ocupado. Revisar firewall. |
| `Connection timeout after 300000ms` | Mensaje muy grande o conexión lenta | Aumentar `hl7.connection_timeout_ms`. Verificar conectividad de red. |
| `Max concurrent connections reached` | Demasiadas conexiones simultáneas | Aumentar `hl7.max_concurrent_connections` o verificar que los clientes cierren conexiones. |
| `Channel is full — writer is waiting` | Cola interna saturada | Aumentar `hl7.max_queued_messages` o agregar más workers. |

### 🟡 Errores de Validación

| Error | Causa Probable | Solución |
|-------|---------------|----------|
| `MSH.9: Message type could not be extracted` | Segmento MSH malformado o ausente | Verificar que el mensaje comience con `MSH\|^~\\&\|...` y que el campo 9 contenga el tipo. |
| `Unsupported message type 'SIU'` | Tipo de mensaje no soportado | Solo se aceptan ADT, ORM, ORU. Contactar al equipo para agregar soporte. |
| `Required segment 'EVN' missing for ADT` | Segmento EVN no presente en ADT | Agregar segmento EVN al mensaje ADT. Es requerido por la especificación. |
| `Required segment 'ORC' missing for ORM` | Segmento ORC no presente en ORM | Agregar segmento ORC. Las órdenes requieren MSH + PID + ORC + OBR. |
| `PID.3: Patient ID is required` | Campo PID.3 vacío o PID ausente | Asegurar que el segmento PID tenga un valor en el campo 3 (Patient ID). |

### 🟠 Errores de Routing

| Error | Causa Probable | Solución |
|-------|---------------|----------|
| `No routing rule matched and no active nodes` | Sin reglas configuradas y sin nodos activos | Crear al menos una regla de routing o activar un nodo Edge. |
| `Rule matched but target node is unavailable` | Nodo destino deshabilitado o inexistente | Verificar que el nodo esté habilitado (`IsEnabled = true`) y registrado. |
| Fallback routing activado (warnings en log) | Ninguna regla específica coincidió | Crear reglas de routing más específicas para evitar el fallback. |

### 🔵 Errores de Dispatch

| Error | Causa Probable | Solución |
|-------|---------------|----------|
| `Target node not found or has no API endpoint` | Nodo sin endpoint configurado | Configurar `ApiEndpoint` del nodo (ej: `http://node:5001`). |
| `Dispatch timed out` | Nodo no respondió dentro del timeout | Verificar conectividad con el nodo. Aumentar `dispatch.timeout_seconds`. |
| `HTTP 503: Service Unavailable` | Nodo Edge caído o reiniciándose | Verificar estado del nodo. El sistema reintentará automáticamente. |
| `Connection error: {message}` | Error de red entre Hub y Node | Verificar firewall, DNS, y que el nodo esté escuchando. |
| `Max retries exceeded. Last error: ...` | Mensaje falló después de N reintentos | Revisar el error original. El mensaje queda en `DeliveryFailed`. Puede ser re-encolado manualmente. |
| `Node rejected the message without reason` | Nodo devolvió `Accepted: false` sin error | Revisar logs del Edge Node para entender el rechazo. |

### ⚪ Warnings Comunes (No Bloquean)

| Warning | Significado | Acción Recomendada |
|---------|-------------|-------------------|
| `MSH.3: Sending Application is empty` | Sistema origen no identificado | Configurar el campo MSH.3 en el sistema emisor. |
| `MSH.4: Sending Facility is empty` | Facility origen no identificada | Configurar MSH.4. Importante para routing por facility. |
| `OBR.18: Accession Number is empty for ORM` | Orden sin número de acceso | Asegurar que OBR.18 tenga un valor para trazabilidad. |
| `PID.5: Patient Name is empty` | Nombre del paciente vacío | Configurar PID.5 en el sistema emisor. |

---

## 18. Glosario

| Término | Definición |
|---------|------------|
| **ACK** | Acknowledgment — mensaje de confirmación HL7 |
| **ADT** | Admission, Discharge, Transfer — movimientos de pacientes |
| **AE Title** | Application Entity Title — identificador DICOM de aplicación |
| **C-FIND** | DICOM service para búsquedas (ej: Modality Worklist) |
| **Dispatch** | Envío HTTP del mensaje procesado al Edge Node |
| **Edge Node** | Servidor DICOM local que recibe worklist y almacena estudios |
| **HIS** | Hospital Information System — sistema de gestión hospitalaria |
| **HL7** | Health Level 7 — estándar de mensajería en salud |
| **Hub** | Servidor central EdgeGuard que recibe HL7 y coordina nodos |
| **MLLP** | Minimal Lower Layer Protocol — framing TCP para HL7 |
| **MSA** | Message Acknowledgment — segmento de confirmación |
| **MSH** | Message Header — segmento de cabecera HL7 |
| **MWL** | Modality Worklist — lista de trabajo DICOM |
| **OBR** | Observation Request — segmento de solicitud de observación |
| **OBX** | Observation Result — segmento de resultado de observación |
| **ORC** | Common Order — segmento de orden |
| **ORM** | Order Message — mensaje de órdenes |
| **ORU** | Observation Result — mensaje de resultados |
| **PID** | Patient Identification — segmento de identificación de paciente |
| **PV1** | Patient Visit — segmento de visita del paciente |
| **RIS** | Radiology Information System — sistema de gestión radiológica |
| **Routing Rule** | Regla de enrutamiento que determina el nodo destino |
| **SCP** | Service Class Provider — servidor DICOM |
| **Trigger Event** | Sub-tipo de mensaje HL7 (ej: A01 dentro de ADT) |

---

## Apéndice A — Rutas API Completas

### Hub API — Endpoints HL7

| Método | Ruta | Descripción |
|--------|------|-------------|
| `GET` | `/api/hl7status/status` | Estado del listener |
| `GET` | `/api/hl7status/recent-messages?count={n}` | Mensajes recientes |
| `GET` | `/api/hl7status/messages/{id}` | Detalle de mensaje |
| `GET` | `/api/queuemonitoring/summary` | Resumen de cola |
| `GET` | `/api/queuemonitoring/by-dispatch-status/{status}` | Por estado |
| `GET` | `/api/queuemonitoring/queued?batchSize={n}` | Mensajes en cola |
| `GET` | `/api/routingrules` | Listar reglas de routing |
| `POST` | `/api/routingrules` | Crear regla de routing |
| `PUT` | `/api/routingrules/{id}/enable` | Habilitar regla |
| `PUT` | `/api/routingrules/{id}/disable` | Deshabilitar regla |
| `DELETE` | `/api/routingrules/{id}` | Eliminar regla |

### Hub API — Edge Endpoints (Node-facing)

| Método | Ruta | Descripción |
|--------|------|-------------|
| `POST` | `/edge/register` | Registro de nodo |
| `POST` | `/edge/heartbeat` | Heartbeat periódico |
| `POST` | `/edge/studies` | Notificación de estudio |
| `POST` | `/edge/health` | Reporte de salud |
| `GET` | `/edge/configuration` | Pull de configuración |

### Node API — Endpoints (Hub-facing)

| Método | Ruta | Descripción |
|--------|------|-------------|
| `POST` | `/api/hl7/worklist` | Recibir worklist push |
| `GET` | `/api/health` | Health check |
| `GET` | `/api/dicom/worklist` | Worklist items |
| `GET` | `/api/dicom/worklist/count` | Worklist count |
| `POST` | `/api/configuration/apply` | Aplicar configuración |
| `GET` | `/api/configuration/version` | Versión de configuración |

---

> **Contacto**: Para soporte técnico o solicitudes de nuevos tipos de mensaje, contactar al equipo de EdgeGuard Platform.  
> **Repositorio**: [EdgeGuard.Platform](https://github.com/UrielAvilaC/EdgeGuard.Platform)
