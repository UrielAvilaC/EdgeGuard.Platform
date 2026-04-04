# EdgeGuard.Platform

## Plataforma distribuida para integración de imágenes médicas

**EdgeGuard.Platform** es una plataforma diseñada para la recepción,
gestión y distribución de estudios médicos **DICOM** en entornos
clínicos.

La solución sigue una arquitectura **Edge + Hub**, donde nodos locales
instalados en hospitales reciben estudios desde modalidades médicas y
los envían directamente al PACS destino. El Hub central orquesta la
configuración, monitoreo y administración de los nodos.

------------------------------------------------------------------------

# Objetivos del proyecto

-   Recibir estudios médicos desde modalidades (CT, MR, CR, US, etc.)
-   Proveer **Modality Worklist (MWL)** a equipos médicos
-   Almacenar temporalmente estudios en nodos Edge
-   Enviar estudios directamente al PACS destino de forma segura
-   Proporcionar herramientas administrativas mediante una interfaz web
-   Operar correctamente en entornos con conectividad intermitente

------------------------------------------------------------------------

# Arquitectura general

                  ┌────────────────────┐
                  │   EdgeGuard.Spa    │
                  │   EdgeGuard.Admin  │
                  └─────────┬──────────┘
                            │
                            ▼
                   ┌─────────────────┐
                   │  EdgeGuard.Api  │
                   └─────────┬───────┘
                             │
                             ▼
                    ┌───────────────┐
                    │ EdgeGuard Hub │
                    └───────┬───────┘
                            │
                            ▼
                  ┌──────────────────┐
                  │ EdgeGuard.Edge   │
                  │   DICOM Gateway  │
                  └──────────────────┘

------------------------------------------------------------------------

# Componentes principales

La plataforma está organizada como un **monorepositorio** que contiene
múltiples aplicaciones.

    EdgeGuard.Platform
    │
    ├── backend
    │   └── EdgeGuard.Api
    │
    ├── frontend
    │   ├── EdgeGuard.Spa
    │   └── EdgeGuard.AdminUi
    │
    ├── edge
    │   └── EdgeGuard.EdgeNode
    │
    └── shared
        └── EdgeGuard.Contracts

------------------------------------------------------------------------

# Descripción de componentes

## backend

Contiene los servicios del lado servidor responsables de la lógica de
negocio y APIs.

    backend/
    └── EdgeGuard.Api

Responsabilidades:

-   Gestión de estudios
-   Orquestación de nodos Edge
-   Integración con sistemas hospitalarios
-   Servicios administrativos
-   Notificación de resultados vía WhatsApp

------------------------------------------------------------------------

## frontend

Aplicaciones web utilizadas para operar y administrar la plataforma.

    frontend/
    ├── EdgeGuard.Spa
    └── EdgeGuard.AdminUi

Responsabilidades:

-   Visualización de estudios
-   Administración del sistema
-   Monitoreo de nodos Edge
-   Configuración de la plataforma

------------------------------------------------------------------------

## edge

Nodo instalado en hospitales o clínicas que actúa como **gateway
DICOM**.

    edge/
    └── EdgeGuard.EdgeNode

Responsabilidades del Edge Node:

-   Servidor **DICOM C-STORE SCP** para recibir imágenes
-   Servidor **DICOM Modality Worklist (MWL)**
-   Almacenamiento temporal de estudios
-   Cola de procesamiento
-   Reintentos automáticos en caso de fallos de red
-   Envío de estudios directamente al PACS destino

------------------------------------------------------------------------

## shared

Componentes compartidos entre aplicaciones.

    shared/
    └── EdgeGuard.Contracts

Incluye:

-   DTOs
-   Modelos compartidos
-   Contratos de API
-   Eventos de integración

------------------------------------------------------------------------

# Flujo de datos

    Modalidades médicas
          │
          │  C-FIND (Worklist)
          ▼
    EdgeGuard EdgeNode
          │
          │  C-STORE
          ▼
    Almacenamiento local
          │
          ▼
    Cola de estudios
          │
          ▼
    Envío al PACS destino
          │
          ▼
    Notificación al Hub central
          │
          ▼
    Entrega de resultado vía WhatsApp

------------------------------------------------------------------------

# Tecnologías utilizadas

### Backend

-   .NET
-   ASP.NET Core
-   Servicios REST
-   Integración DICOM

### Edge Node

-   .NET Worker Service
-   Servidor DICOM
-   Procesamiento asíncrono

### Frontend

-   SPA Web
-   Administración del sistema

### Persistencia

-   Bases de datos SQL
-   almacenamiento persistente de estudios

------------------------------------------------------------------------

# Características principales

-   Arquitectura **Edge + Hub**
-   Integración con estándares **DICOM**
-   Soporte para **C-STORE**
-   Soporte para **Modality Worklist (MWL)**
-   Procesamiento asíncrono mediante colas
-   Operación **offline-first**
-   Sincronización resiliente con el Hub
-   **Entrega de resultados vía WhatsApp** (automática y manual)

------------------------------------------------------------------------

# Entrega de resultados vía WhatsApp

La plataforma permite notificar al paciente o médico referente cuando un
estudio ha sido procesado y almacenado exitosamente en el PACS. La
entrega del resultado se realiza mediante un mensaje de **WhatsApp** que
incluye el enlace directo al visor PACS.

> **Nota:** La notificación solo se ejecuta si el paciente tiene un
> número de teléfono registrado. Si no existe teléfono en el registro
> del paciente, el flujo de WhatsApp se omite silenciosamente.

## Configuración dinámica

La entrega de resultados se controla mediante **claves de configuración
del sistema** (`system_settings`), lo que permite habilitar o
deshabilitar cada comportamiento en tiempo de ejecución sin necesidad de
reiniciar la aplicación.

| Clave                                         | Tipo    | Descripción                                                       |
|-----------------------------------------------|---------|-------------------------------------------------------------------|
| `whatsapp.enabled`                            | bool    | Habilita o deshabilita globalmente el servicio de WhatsApp        |
| `whatsapp.auto_send_on_oru`                   | bool    | Envío automático al recibir ACK tras un ORU^R01                   |
| `whatsapp.require_pacs_link`                  | bool    | Si `true`, solo envía si el enlace al visor PACS está disponible  |
| `whatsapp.api_base_url`                       | string  | URL base de la API de mensajería WhatsApp                         |
| `whatsapp.default_message_template`           | string  | Identificador de la plantilla de mensaje por defecto              |
| `whatsapp.retry_max_attempts`                 | int     | Número máximo de reintentos en caso de fallo de envío             |
| `whatsapp.retry_delay_seconds`                | int     | Segundos de espera entre reintentos                               |

## Flujo automático (ORU → PACS → WhatsApp)

    Hub HL7 Listener
          │
          │  Recibe mensaje ORU^R01
          ▼
    Pipeline HL7 (validación + enrutamiento)
          │
          │  Encolado y despacho
          ▼
    Edge Node destino
          │
          │  C-STORE al PACS
          ▼
    PACS destino
          │
          │  ACK de confirmación
          ▼
    Edge Node → notifica al Hub
          │
          ▼
    Hub evalúa condiciones:
      ├─ whatsapp.enabled = true?
      ├─ whatsapp.auto_send_on_oru = true?
      ├─ ¿Paciente tiene teléfono?
      └─ ¿Enlace PACS disponible? (si require_pacs_link = true)
          │
          │  Si todas las condiciones se cumplen
          ▼
    Servicio de notificación WhatsApp
          │
          │  Genera enlace al visor PACS
          │  Envía mensaje al paciente / referente
          ▼
    WhatsApp (API de mensajería)

1.  El Hub recibe un mensaje **ORU^R01** (resultado no solicitado) a
    través del listener HL7.
2.  El mensaje es validado, enrutado y encolado hacia el nodo Edge
    correspondiente.
3.  El nodo Edge envía el estudio al **PACS destino** mediante
    C-STORE.
4.  Al recibir el **ACK** de confirmación del PACS, el nodo notifica al
    Hub que el estudio fue almacenado exitosamente.
5.  El Hub evalúa las condiciones de envío:
    -   `whatsapp.enabled` y `whatsapp.auto_send_on_oru` deben ser
        `true`.
    -   El paciente debe tener un **número de teléfono** registrado.
    -   Si `whatsapp.require_pacs_link` es `true`, el enlace al visor
        PACS debe estar disponible.
6.  Si todas las condiciones se cumplen, genera el enlace al visor PACS
    y envía la notificación vía **WhatsApp**.

> Si alguna condición no se cumple, el flujo se omite y se registra un
> log informativo. No se genera error.

## Entrega manual (desde la interfaz)

Desde la interfaz de administración, un operador puede iniciar la
entrega de resultados de forma manual:

-   **Si el enlace al visor PACS está disponible**: se entrega
    directamente al paciente vía WhatsApp.
-   **Si el enlace no está disponible**: el sistema solicita al operador
    que proporcione o confirme el enlace antes de enviar la
    notificación.
-   **Si el paciente no tiene teléfono**: el operador puede ingresar el
    número manualmente antes de enviar.

Este modo permite manejar casos donde el estudio fue almacenado
previamente, donde el enlace PACS requiere validación manual, o donde el
paciente no tiene teléfono registrado en el sistema.

## Datos utilizados para la notificación

| Campo                  | Obligatorio | Origen                                         |
|------------------------|-------------|-------------------------------------------------|
| Número de teléfono     | No*         | Datos del paciente (HL7 / registro) o manual    |
| Nombre del paciente    | Sí          | Mensaje ORU / registro del estudio              |
| Enlace al visor PACS   | Configurable| PACS destino o ingreso manual                   |
| Descripción del estudio| Sí          | Metadata del estudio DICOM                      |

\* Si no existe teléfono, el flujo automático se omite. En modo manual
el operador puede ingresarlo.

------------------------------------------------------------------------

# Estado del proyecto

En desarrollo activo.

Las primeras funcionalidades se enfocan en:

1.  Implementación del servidor **C-STORE**
2.  Implementación de **Modality Worklist**
3.  Pipeline de ingestión de estudios
4.  Envío de estudios al PACS destino
5.  Pipeline HL7 (recepción, validación, enrutamiento, despacho)
6.  Diagnósticos enterprise (Serilog, OpenTelemetry, PHI redaction)
7.  Entrega de resultados vía **WhatsApp** (automática y manual)

------------------------------------------------------------------------

# Seguridad

La plataforma está diseñada considerando:

-   interoperabilidad con estándares médicos
-   integridad de estudios clínicos
-   operación segura en redes hospitalarias
-   aislamiento de nodos Edge

------------------------------------------------------------------------

# Licencia

Software propietario.

Distribución, copia o modificación no autorizada está prohibida.

------------------------------------------------------------------------

# Autor

Equipo de ingeniería de **EdgeGuard Platform**
