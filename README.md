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
