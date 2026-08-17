# Plan de Implementación — ORU, Reportes y Entrega de Resultados (detallado)

> **Estado:** ✅ Versión final detallada — para aprobación
> **Fecha:** 2026-05-31
> **Alcance:** Flujo ORU (image links + reporte HTML/texto + PDF base64), estados de estudio ampliados, almacenamiento físico de reportes en el workspace del Hub, visor de reporte, motor de notificaciones multicanal (Email + WhatsApp) con **outbox unificado durable**, QR, editor de plantillas de Email y modo automático por estatus.
>
> **Nota sobre migraciones:** las migraciones EF Core **no se detallan ni se implementan en este plan**; se generan con `dotnet ef migrations add <Nombre>`. Solo se indica el **nombre** de cada una en cada fase y un **ejemplo de la clase** en la §9.

---

## 1. Estado actual (análisis)

### Reutilizable
| Pieza | Ubicación | Nota |
|---|---|---|
| Parsing ORU → image links | `Hl7Message.ExtractObxImageLinks`, `Hl7StudySyncService.HandleOruAsync` | URLs de OBX (RP/ED/http). Crea *stub* si no existe el estudio. |
| `Study.ExternalImageLinks` + `AttachImageLinks()` | `Domain/Aggregates/Studies/Study.cs` | Newline-separated URLs. |
| Transporte WhatsApp (estrategia) | `IMessagingProvider` → `Infrastructure/Services/TwilioMessagingProvider.cs` | Ya es abstracción; `WhatsAppNotificationService` lo inyecta. |
| Outbox WhatsApp de facto | `Domain/Aggregates/Notifications/WhatsAppNotification.cs` + `WhatsAppNotificationHostedService.ProcessPendingAsync` | Cola durable DB (Pending/Sent/Failed/Skipped, Attempts, LastError). |
| Auto-send por estatus | `WhatsAppAutoSendRule` (StudyStatus→TemplateId) | Solo WhatsApp. |
| Etiquetas / resolver | `WhatsAppTemplateTags`, `WhatsAppVariableResolver` | Base para generalizar. |
| SignalR | `Api/Hubs/EdgeHubNotificationHub.cs` (patrón `NotifyNodePushStatus`) | Feedback de entrega. |

### Faltante
G1 estados nuevos · G2 reporte (texto/HTML) · G3 PDF b64 + workspace físico · G4 visor · G5 SMTP/Email · G6 QR · G7 módulo entrega · G8 editor plantillas Email · G9 settings SMTP · G10 auto multicanal.

---

## 2. Decisiones de arquitectura (confirmadas)

1. Separar **links** de **reporte** (modelo de reporte propio).
2. **Workspace del Hub**: `IReportStorage` persiste el PDF físico (`Hub:Workspace:ReportsPath`), servido por endpoint autenticado.
3. **Canales hermanos, sin herencia**: `INotificationChannelSender` común; `WhatsAppChannelSender` envuelve `IMessagingProvider`; `EmailChannelSender` SMTP. `INotificationDispatcher` orquesta.
4. **Plantillas por canal**: WhatsApp = `WhatsAppTemplate`/ContentSid (intacto); Email = nuevo `NotificationTemplate` (editor enterprise **solo Email**).
5. **Historial unificado**: `WhatsAppNotification` → `Notification` (+`Channel`); generalizar tags/resolver/auto-rule.
6. **Outbox unificado durable**: la tabla `Notification` es la cola; un `NotificationOutboxHostedService` con **carriles por canal** (concurrencia/rate-limit independientes).
7. `IWhatsAppNotificationService` = **fachada** (no rompe la UI actual), delega en el dispatcher.
8. **State machine** (P1-5): transiciones validadas; `Finalized` dispara entrega automática.

---

# FASE 1 — Dominio: estados de estudio + reporte

**Objetivo:** ciclo de vida ampliado + datos de reporte en el agregado `Study`.

### 1.1 Backend — dominio
- **`src/shared/Dicom.Edge.Models/Enums/StudyStatus.cs`** — añadir tras `Completed`:
  ```csharp
  WaitingForImageLinks,  // tiene reporte, faltan imágenes
  WaitingForReport,      // tiene imágenes, falta reporte
  Finalized,             // imágenes + reporte
  ```
- **Nuevo enum** `Models/Enums/ReportFormat.cs`: `None, Html, PlainText`.
- **`Study.cs`** — campos:
  ```csharp
  public ReportFormat ReportFormat { get; private set; } = ReportFormat.None;
  public string? ReportContent { get; private set; }
  public string? ReportPdfPath { get; private set; }   // ruta relativa en workspace
  public DateTime? ReportReceivedAt { get; private set; }
  public bool HasReport     => ReportFormat != ReportFormat.None || ReportPdfPath is not null;
  public bool HasImageLinks => !string.IsNullOrEmpty(ExternalImageLinks);
  ```
  - Métodos:
    ```csharp
    public void AttachReport(ReportFormat format, string? content, string? pdfPath);
    public void RecomputeCompletion();  // decide WaitingForReport / WaitingForImageLinks / Finalized
    ```
  - `RecomputeCompletion()` (state machine P1-5):
    - imágenes && HasReport → `Finalized` (+`StudyFinalizedEvent`)
    - imágenes && !HasReport → `WaitingForReport`
    - !imágenes && (HasReport||HasImageLinks) → `WaitingForImageLinks`
  - Invocar desde `RecordImagesReceived`/`MarkCompleted` y desde `AttachReport`/`AttachImageLinks`. Reusar `RecordStatusChange`.

### 1.2 Backend — persistencia
- **`Persistence/Configurations/StudyConfiguration.cs`** — mapear `ReportFormat` (enum→string), `ReportContent` (text), `ReportPdfPath`, `ReportReceivedAt`.
- **Migración:** `AddStudyReport` (solo nombre; ver §9).

### 1.3 Contracts
- `StudyDto` — exponer `Status` (nuevos), `HasReport`, `HasImageLinks`, `ReportFormat`.

### Tareas
- [ ] T1 `StudyStatus` (+3) y `ReportFormat`.
- [ ] T2 Campos + `AttachReport` + `RecomputeCompletion` + `StudyFinalizedEvent`.
- [ ] T3 `StudyConfiguration` (mapeo). *(Migración `AddStudyReport`: generar aparte.)*
- [ ] T4 `StudyDto` + mapping.

### Tests
Matriz de `RecomputeCompletion` (4 combinaciones); idempotencia.
### Rollback / flag
Revert; estados nuevos sin uso si no hay reporte.
### Riesgo
Medio.

---

# FASE 2 — Parsing ORU extendido (links + reporte + PDF)

**Objetivo:** capturar reporte texto/HTML y PDF base64 del ORU.

### 2.1 Backend — `Hl7Message.cs` (Domain/Entities)
- Constantes en `Hl7ValidationConstants`: `ObxValueTypeText=TX`, `ObxValueTypeEncapsulatedData=ED`. Añadir `FT`.
- Campos:
  ```csharp
  public string? ReportText { get; private set; }       // OBX TX/FT concatenado
  public ReportFormat ReportFormat { get; private set; }
  public string? ReportPdfBase64 { get; private set; }  // OBX ED application/pdf (transitorio, NO se persiste)
  ```
- Extractores:
  ```csharp
  private static (string? text, ReportFormat fmt) ExtractObxReportText(string content);
  private static string? ExtractObxPdfBase64(string content); // OBX-2=ED, OBX-5 "^application/pdf^Base64^<b64>"
  ```

### 2.2 Backend — `Hl7StudySyncService.HandleOruAsync`
```csharp
var study = await GetOrCreateStudyAsync(message, ct);   // helper extraído del path actual (links + stub)
if (message.ImageLinks.Count > 0) study.AttachImageLinks(message.ImageLinks);

string? pdfPath = null;
if (!string.IsNullOrEmpty(message.ReportPdfBase64))
{
    var bytes = SafeFromBase64(message.ReportPdfBase64);          // valida tamaño/MIME
    if (bytes is not null) pdfPath = await reportStorage.SavePdfAsync(study.Id, bytes, ct); // IReportStorage (Fase 3)
}
if (message.ReportText is not null || pdfPath is not null)
    study.AttachReport(message.ReportFormat, message.ReportText, pdfPath);

study.RecomputeCompletion();
await studyRepository.UpdateAsync(study, ct);
```
- Inyectar `IReportStorage`. Validar `Hub:Workspace:MaxPdfMb`; b64 inválido → log + skip (no romper el ACK ya enviado).

### 2.3 Persistencia
- `Hl7MessageConfiguration` — mapear `ReportText`, `ReportFormat`. **No** persistir `ReportPdfBase64` (PHI b64).
- **Migración:** `AddHl7MessageReport` (solo nombre; ver §9).

### Tareas
- [ ] T1 Extractores OBX (texto/FT, ED PDF) + constante `FT`.
- [ ] T2 Campos en `Hl7Message` + `Hl7MessageConfiguration`. *(Migración aparte.)*
- [ ] T3 Reescribir `HandleOruAsync` con `IReportStorage`.
- [ ] T4 Validación tamaño/MIME/b64.

### Tests
ORU: solo TX; solo ED-PDF; ambos; HTML; b64 inválido; PDF > límite.
### Rollback / flag
`Oru:ParseReport=true`. Off → solo links (actual).
### Riesgo
Medio.

---

# FASE 3 — Workspace del Hub + visor de reporte

**Objetivo:** almacenamiento físico del PDF y visualización en SPA.

### 3.1 Backend — almacenamiento
- **`Application/Reports/IReportStorage.cs`:**
  ```csharp
  Task<string> SavePdfAsync(string studyId, byte[] pdf, CancellationToken ct); // → ruta relativa
  Task<Stream?> OpenPdfAsync(string relativePath, CancellationToken ct);
  ```
- **`Infrastructure/Storage/HubFileReportStorage.cs`:** escribe `{Hub:Workspace:ReportsPath}/{studyId}.pdf`; crea carpetas; sanitiza `studyId`; valida ruta dentro del root (anti path-traversal).
- **Options** `HubWorkspaceOptions { ReportsPath, MaxPdfMb }` + `Configure` + appsettings `Hub:Workspace`.

### 3.2 Backend — API (`StudiesController`)
- `GET /api/studies/{id}/report` → `ReportDto { format, content (HTML sanitizado/text), hasPdf, imageLinks[], status }`.
- `GET /api/studies/{id}/report/pdf` → `FileStreamResult` (`application/pdf`), `[Authorize]`, `[EnableRateLimiting("api")]`.
- Sanitizar HTML con `Ganss.Xss.HtmlSanitizer`.

### 3.3 SPA — visor (feature `studies`)
- Servicio: `getReport(id)`, `reportPdfUrl(id)`.
- Componente `study-report-panel` (detalle del estudio): badge de estatus (incluye los 3 nuevos), render HTML sanitizado/`<pre>`, visor PDF embebido, lista de image links, botón "Entregar resultados" (→ Fase 6).

### Tareas
- [ ] T1 `IReportStorage` + `HubFileReportStorage` + options + appsettings.
- [ ] T2 Sanitizador HTML (`Ganss.Xss`).
- [ ] T3 Endpoints report/report-pdf + `ReportDto`.
- [ ] T4 SPA `study-report-panel` + servicio.

### Tests
Save/Open PDF; anti path-traversal; stream 401 sin token; sanitización XSS.
### Riesgo
Medio.

---

# FASE 4 — Motor multicanal + outbox unificado + SMTP + QR

**Objetivo:** abstracción de canal, outbox durable único, Email y QR.

### 4.1 Backend — dominio (unificar registro)
- **`WhatsAppNotification` → `Notification`** (`Domain/Aggregates/Notifications/Notification.cs`):
  - `NotificationChannel Channel { get; }` (`WhatsApp|Email`).
  - Campos Email: `ToEmail`, `Subject`, `RenderedBody`, `AttachmentPath`.
  - `DateTime? NextAttemptAt` (backoff). Conservar `Status`, `Attempts`, `LastError`, `ProviderMessageId`.
  - Métodos: `MarkSent`, `MarkFailed`, `ScheduleRetry(delay)`, `MarkSkipped`.
- Enums: `NotificationChannel`; `WhatsAppNotificationStatus` → `NotificationStatus`.
- **Migración:** `RenameWhatsAppNotificationToNotification` (rename tabla/columnas + `channel`/`next_attempt_at`/campos email + backfill `channel='WhatsApp'`) (solo nombre; ver §9).

### 4.2 Backend — abstracción de canal
- **`Application/Notifications/INotificationChannelSender.cs`:**
  ```csharp
  NotificationChannel Channel { get; }
  Task<ChannelSendResult> SendAsync(NotificationMessage msg, CancellationToken ct);
  ```
  `NotificationMessage { Channel, To, Subject?, Body?, ContentSid?, Variables, AttachmentPath?, QrPng? }`.
- **`Infrastructure/Notifications/WhatsAppChannelSender.cs`** — `Channel=WhatsApp`; **envuelve `IMessagingProvider`** (ContentSid + vars).
- **`Infrastructure/Notifications/EmailChannelSender.cs`** — `Channel=Email`; `MailKit` (`SmtpClient`); adjunta `AttachmentPath`; QR inline (cid); lee `SmtpOptions`.
- DI keyed por canal (`IEnumerable<INotificationChannelSender>` → dict por `Channel`).

### 4.3 Backend — QR
- `QRCoder`; **`Infrastructure/Notifications/QrCodeGenerator.cs`** (`IQrCodeGenerator.GeneratePng(link) → byte[]`).
- Endpoint `GET /api/studies/{id}/report/qr?link=…` → PNG.

### 4.4 Backend — dispatcher + outbox
- **`Application/Notifications/INotificationDispatcher.cs`:**
  ```csharp
  Task<DeliveryResult> DeliverAsync(DeliveryRequest req, CancellationToken ct);
  ```
  `DeliveryRequest { StudyId, Channels[], Recipients{Emails[],Phones[]}, AttachPdf, IncludeQr, TemplateIdByChannel }`.
  → resuelve plantilla por canal (WhatsApp→ContentSid; Email→`NotificationTemplate`), renderiza, **inserta filas `Notification` (Pending)** y retorna.
- **`Infrastructure/HostedServices/NotificationOutboxHostedService.cs`** (generaliza `WhatsAppNotificationHostedService`):
  - Drena `Pending && NextAttemptAt<=now` por lote; **carriles por canal** (`SemaphoreSlim`/rate-limit por `Channel`); despacha al sender del canal; `MarkSent`/`MarkFailed`+`ScheduleRetry(backoff)`; SignalR `DeliveryStatus`; supervisor P0-8.
- **Repos/EF:** `INotificationRepository.GetPendingAsync(channel?, batch)`; `NotificationConfiguration` con índice `(Channel, Status, NextAttemptAt)`.

### 4.5 Backend — Email settings
- **`SmtpOptions { Host, Port, User, Password, From, UseTls, Enabled }`** + appsettings `Smtp` + settings DB (`HubSettingKeys.Smtp.*`).

### 4.6 Compatibilidad
- `WhatsAppNotificationService` → **fachada**: arma `DeliveryRequest` (canal WhatsApp) y llama `INotificationDispatcher`. La UI WhatsApp no cambia.

### Tareas
- [ ] T1 `Notification` + enums + repos + `NotificationConfiguration`. *(Migración rename aparte.)*
- [ ] T2 `INotificationChannelSender` + `NotificationMessage`.
- [ ] T3 `WhatsAppChannelSender` (envuelve `IMessagingProvider`).
- [ ] T4 `EmailChannelSender` (MailKit) + `SmtpOptions` + settings.
- [ ] T5 `IQrCodeGenerator` (QRCoder) + endpoint QR.
- [ ] T6 `INotificationDispatcher` + `NotificationOutboxHostedService` (carriles, backoff).
- [ ] T7 Fachada `WhatsAppNotificationService`.

### Tests
SMTP fake; QR PNG; dispatcher 2 canales; outbox retry/backoff; aislamiento de carriles; `Channel` persistido; fachada WhatsApp OK.
### Rollback / flag
`Smtp:Enabled`, `Notifications:OutboxEnabled`.
### Riesgo
Medio (refactor del registro WhatsApp).

---

# FASE 5 — Plantillas de Email enterprise (editor drag-and-drop) — SOLO EMAIL

**Objetivo:** plantillas Email HTML/texto con etiquetas y editor enterprise. *(WhatsApp sigue con ContentSid.)*

### 5.1 Backend — dominio
- **`Domain/Aggregates/Notifications/NotificationTemplate.cs`:** `{ Id, Name, Channel(Email), Format(Html|PlainText), Subject, Body, IsActive, CreatedAt, UpdatedAt }` + repo + `NotificationTemplateConfiguration`.
- **Migración:** `AddNotificationTemplate` (solo nombre; ver §9).
- **`Application/Notifications/INotificationVariableResolver.cs`** (generaliza `WhatsAppVariableResolver`).
- **`NotificationTags`** (generaliza `WhatsAppTemplateTags`): `patientName, patientCode, accessionNumber, studyDescription, studyDate, modality, referringPhysician, facilityName, imageLink, reportLink, qrCode`.

### 5.2 Backend — API (`NotificationTemplatesController`)
- CRUD `GET/POST/PUT/DELETE /api/notification-templates` (filtro `Channel=Email`).
- `POST /api/notification-templates/{id}/preview` → render con datos de ejemplo (HTML sanitizado).
- `GET /api/notification-templates/tags` → catálogo (tag, descripción, ejemplo).

### 5.3 SPA — feature `notifications`
- Lista + form de plantillas Email.
- **Editor enterprise**: `grapesjs` (preset-newsletter) **o** `angular-email-editor` (unlayer): paleta de **merge tags** insertables (drag-and-drop), toggle HTML↔texto, **preview** en vivo (`/preview`).

### Tareas
- [ ] T1 `NotificationTemplate` + repo + `NotificationTemplateConfiguration`. *(Migración aparte.)*
- [ ] T2 `INotificationVariableResolver` + `NotificationTags`.
- [ ] T3 API CRUD + preview + tags.
- [ ] T4 SPA editor + paleta + preview.

### Tests
Render con todas las etiquetas; etiqueta desconocida; sanitización; preview.
### Riesgo
**Alto** (UI; licencia GrapesJS BSD vs unlayer freemium — decidir antes de T4).

---

# FASE 6 — Módulo de entrega de resultados

**Objetivo:** entrega manual desde el estudio.

### 6.1 Backend — API
- `POST /api/studies/{id}/deliver` `{ channels:[Email,WhatsApp], recipients:{emails[],phones[]}, attachPdf, includeQr, templateIdByChannel }` → `INotificationDispatcher.DeliverAsync` (encola).
- `GET /api/studies/{id}/deliveries` → historial (`Notification` por estudio).
- Auditoría de la solicitud.

### 6.2 SPA — diálogo "Entregar resultados" (detalle del estudio)
- **QR** (`/report/qr`) + **liga** copiable + preview del reporte.
- Si `hasPdf` → toggle **"Adjuntar PDF"** (oculto si no hay).
- **Destinatarios**: emails/teléfonos (precargados de `Patient`; editables).
- Selección de **plantilla** por canal (Email: `NotificationTemplate`; WhatsApp: `WhatsAppTemplate`) + preview.
- Enviar → toast + estado SignalR `DeliveryStatus`. Historial por canal.

### Tareas
- [ ] T1 Endpoint deliver + deliveries + auditoría.
- [ ] T2 SPA diálogo.
- [ ] T3 Suscripción SignalR `DeliveryStatus`.

### Tests
Email+WhatsApp con/sin PDF; recipients vacíos; PDF ausente oculta toggle; historial.
### Riesgo
Medio.

---

# FASE 7 — Modo automático por estatus (multicanal)

**Objetivo:** generalizar auto-send a nuevos estados y a Email.

### 7.1 Backend — dominio
- **`WhatsAppAutoSendRule` → `NotificationAutoSendRule`** `{ StudyStatus, Channel, TemplateId, AttachPdf, IncludeQr, IsEnabled }` (única por `Status×Channel`) + repo + `NotificationAutoSendRuleConfiguration`.
- **Migración:** `RenameAutoSendRuleAddChannel` (solo nombre; ver §9).
- Master switch **`Notifications:AutoMode`** (settings DB + appsettings).

### 7.2 Backend — hook de disparo
- En `Study.RecomputeCompletion()` / handler del `StudyFinalizedEvent`: al entrar a `Finalized` (y opcional `WaitingForReport`/`WaitingForImageLinks`):
  - si `AutoMode` y regla habilitada `(status, channel)` → `DeliveryRequest` con plantilla/toggles + destinatarios del paciente → `INotificationDispatcher.DeliverAsync` (encola).
- **Idempotencia**: no re-enviar si ya hay `Notification` `Sent` para `(study, status, channel)`.

### 7.3 SPA — config modo automático (`settings`/`notifications`)
- Switch global `AutoMode`. Por estatus: plantilla + canales + toggles `AttachPdf`/`IncludeQr` + enable/disable.

### Tareas
- [ ] T1 `NotificationAutoSendRule` + repo + config. *(Migración aparte.)*
- [ ] T2 Hook en state machine + idempotencia.
- [ ] T3 Master switch + settings.
- [ ] T4 SPA config por estatus + switch global.

### Tests
Finalized + AutoMode on + regla → entrega; off → no; sin regla → no; idempotencia.
### Rollback / flag
`Notifications:AutoMode=false`; modo `WarnOnly` inicial.
### Riesgo
Medio-Alto (PHI / dry-run obligatorio).

---

# FASE 8 — Settings del Hub (SMTP, Twilio, workspace) + consolidación

**Objetivo:** configuración homologada y cierre.

### 8.1 Backend
- Options + appsettings `Smtp` / `Hub:Workspace` / `Notifications:{AutoMode,OutboxEnabled}` + claves `HubSettingKeys` + seed de settings del Hub. *(El seed de settings no usa migración de esquema.)*

### 8.2 SPA — settings del Hub
- Sección **SMTP** (host/port/user/pass/from/TLS/enabled + "probar envío").
- **WhatsApp/Twilio** (existe — verificar). **Workspace/Notifications** (ruta reportes, MaxPdfMb, AutoMode).

### 8.3 Consolidación / docs
- Documentar coexistencia `WhatsAppTemplate` (ContentSid) vs `NotificationTemplate` (Email).
- Actualizar `docs/04-features/hl7-oru.md`, `real-time-notifications.md`, `docs/05-api/*`.

### Tareas
- [ ] T1 Options + appsettings + settings DB + seed.
- [ ] T2 SPA settings (SMTP + workspace/notifications + test envío).
- [ ] T3 Docs.

### Riesgo
Bajo.

---

## 9. Migraciones EF (solo nombre + ejemplo de clase)

> No se implementan aquí. **Todas son del Hub** (`HubDbContext`, PostgreSQL). El Node usa su propio `EdgeNodeDbContext` (SQLite) y **ninguna** de estas migraciones lo toca. Generar con:
> `dotnet ef migrations add <Nombre> -p src/backend/Dicom.Edge.Hub.Persistence -s src/backend/Dicom.Edge.Hub.Api`

| Migración | Fase | Proyecto / Contexto | Lado | Qué cubre |
|---|---|---|---|---|
| `AddStudyReport` | 1 | `Dicom.Edge.Hub.Persistence` / `HubDbContext` | **Hub** | columnas de reporte en `studies` |
| `AddHl7MessageReport` | 2 | `Dicom.Edge.Hub.Persistence` / `HubDbContext` | **Hub** | `report_text`, `report_format` en `hl7_messages` |
| `RenameWhatsAppNotificationToNotification` | 4 | `Dicom.Edge.Hub.Persistence` / `HubDbContext` | **Hub** | renombra tabla `whatsapp_notifications` → `notifications` + columnas `channel`, `to_email`, `subject`, `rendered_body`, `attachment_path`, `next_attempt_at` + índice `(channel,status,next_attempt_at)` |
| `AddNotificationTemplate` | 5 | `Dicom.Edge.Hub.Persistence` / `HubDbContext` | **Hub** | tabla `notification_templates` (Email) |
| `RenameAutoSendRuleAddChannel` | 7 | `Dicom.Edge.Hub.Persistence` / `HubDbContext` | **Hub** | rename + `channel`, `attach_pdf`, `include_qr` |

**Ejemplo de la clase generada** (forma; el cuerpo lo genera EF):
```csharp
public partial class AddStudyReport : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "report_format", table: "studies",
            type: "text", nullable: false, defaultValue: "None");
        migrationBuilder.AddColumn<string>(
            name: "report_content", table: "studies", type: "text", nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "report_pdf_path", table: "studies", type: "text", nullable: true);
        migrationBuilder.AddColumn<DateTime>(
            name: "report_received_at", table: "studies",
            type: "timestamp with time zone", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "report_format", table: "studies");
        migrationBuilder.DropColumn(name: "report_content", table: "studies");
        migrationBuilder.DropColumn(name: "report_pdf_path", table: "studies");
        migrationBuilder.DropColumn(name: "report_received_at", table: "studies");
    }
}
```

---

## 10. Dependencias nuevas
| Tipo | Paquete | Fase |
|---|---|---|
| NuGet | `MailKit` | 4 |
| NuGet | `QRCoder` | 4 |
| NuGet | `Ganss.Xss` (HtmlSanitizer) | 3 |
| npm | `grapesjs` (+preset newsletter) **o** `angular-email-editor` (unlayer) | 5 |

## 11. Orden recomendado
F1+F2 → F3 → F4 → F6 → F5 → F7+F8. Cada fase: PR independiente, build verde (Hub+Node+SPA), feature flag donde aplique.

## 12. Riesgos transversales
| Riesgo | Mitigación |
|---|---|
| PHI en email/WhatsApp/PDF | SMTP TLS; opt-in adjuntar PDF; auditoría (outbox); redacción en logs. |
| XSS del HTML del reporte/plantilla | `HtmlSanitizer` antes de servir/almacenar. |
| Envíos automáticos erróneos | `WarnOnly`/dry-run; master switch; idempotencia; validar destinatarios. |
| Un canal lento bloquea al otro | Carriles por canal en el outbox único. |
| Pérdida de entregas en reinicio | Outbox durable en DB + backoff. |
| Tamaño/almacenamiento PDFs | `MaxPdfMb` + retención del workspace. |
| Licencia editor drag-and-drop | GrapesJS (BSD) vs unlayer (freemium) — decidir en F5. |

## 13. Aprobaciones
| Rol | Responsabilidad | Aprobado |
|---|---|---|
| Tech Lead | Arquitectura (storage, dispatcher, outbox unificado, plantillas) | ☐ |
| Security/Compliance | PHI en email/WhatsApp/PDF, auditoría, dry-run | ☐ |
| Product Owner | UX entrega + editor Email + modo automático | ☐ |
| QA Lead | Tests parsing ORU, outbox/entrega, plantillas | ☐ |
