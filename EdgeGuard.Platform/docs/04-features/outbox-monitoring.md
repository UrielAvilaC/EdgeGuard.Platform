# Outbox Monitoring

The Hub dispatches two kinds of side effects through **durable outboxes** instead of in-memory
queues, so nothing is lost on a restart:

| Outbox | Store | What it delivers |
|---|---|---|
| **Node-sync** | `node_outbox_messages` | Configuration pushes to Edge Nodes: config, routing rules, PACS destinations, equipment catalog |
| **Notifications** | `notifications` | Patient results delivery over Email / WhatsApp |

Both reference a shared **topic catalog** (`outbox_topics`, seeded at startup) and are drained by a
shared dispatcher skeleton (`OutboxDispatcherBase`) with **at-least-once delivery, exponential
backoff and dead-lettering**. Node-sync uses per-node lanes (preserving order, isolating a slow
node); notifications use per-channel lanes.

## Topics

| Key | Category | Delivers via |
|---|---|---|
| `node.config` / `node.rules` / `node.pacs` / `node.equipment` | `NodeSync` | the matching node push service |
| `notification.email` / `notification.whatsapp` | `Notification` | the matching channel sender |

## Monitoring UI

**Sidebar → Monitoreo → Outbox** (`/outbox`). A unified, live table over the
`vw_outbox_activity` view (UNION of both stores ⋈ topics):

- **Tabs**: All · Node Sync · Notifications. Filter by status (Pending / Sent / Failed).
- **Columns**: topic, category, target (node id or recipient), status, attempts, created, processed, last error.
- **Live updates**: the dispatchers broadcast `OutboxEntryChanged` over SignalR; the page refreshes
  (debounced) without polling.
- **Actions** (require `ManageQueue`): **Reintentar** (requeue for immediate dispatch) and
  **Dead-letter** (mark permanently failed).

### Permissions

| Capability | Permission | Roles |
|---|---|---|
| View the outbox | `ViewSystemStatus` | Admin, Manager, Operator, Viewer, Support |
| Retry / dead-letter | `ManageQueue` | Admin, Manager, Operator |

Enforced in both the API (`OutboxController`) and the SPA (route guard + sidebar + action-button gating).

## REST API

| Method | Route | Permission |
|---|---|---|
| `GET` | `/api/outbox?category=&topic=&status=&page=&pageSize=` | `ViewSystemStatus` |
| `GET` | `/api/outbox/topics` | `ViewSystemStatus` |
| `POST` | `/api/outbox/{store}/{id}/retry` — `store` = `node` \| `notifications` | `ManageQueue` |
| `POST` | `/api/outbox/{store}/{id}/dead-letter` | `ManageQueue` |

## Retention

Terminal rows are purged by the data-retention job using DB-driven, hot-reloadable settings:

| Setting key | Default | Purges |
|---|---|---|
| `retention.notification_days` | 60 | `notifications` |
| `retention.node_outbox_days` | 14 | terminal `node_outbox_messages` |

Changing a retention setting applies on the next cycle without a Hub restart.
