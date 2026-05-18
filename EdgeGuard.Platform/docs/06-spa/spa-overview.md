# SPA Architecture Overview

## Technology Stack

| Technology | Version | Role |
|------------|---------|------|
| Angular | 19 | Application framework |
| Angular Material | 19 | Dialog, form, and layout primitives |
| Tailwind CSS | 3.x | Utility-first styling |
| `@microsoft/signalr` | 8.x | Real-time WebSocket client |
| Font Awesome | 6.x | Icon library |
| TypeScript | 5.x | Primary language |
| RxJS | 7.x | HTTP and async primitives (minimal use; signals preferred for state) |

The SPA is built with Angular 19 **standalone components** — there are no NgModules in the application. Every component, directive, and pipe declares its own `imports` array.

---

## Standalone Components

Angular 19 standalone mode eliminates NgModules entirely. Each component is self-contained:

```typescript
@Component({
  selector: 'app-studies-page',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatTableModule,
    UiStatusBadge,
    UiPaginator,
  ],
  templateUrl: './studies-page.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudiesPageComponent { ... }
```

This pattern applies to every component in the codebase — including dialogs, cards, and shared UI library components.

---

## Signal-Based State Management

The application uses Angular's built-in **signals** (`signal()`, `computed()`, `effect()`) as the primary state primitive. NgRx is not used; there is no global store.

### Patterns in use

| Pattern | API | Use Case |
|---------|-----|----------|
| Writable signal | `signal<T>(initial)` | Local mutable state (selected item, filter values) |
| Computed signal | `computed(() => ...)` | Derived values (filtered list, total count, UI flags) |
| Effect | `effect(() => ...)` | Side effects triggered by signal changes (auto-reload, analytics) |
| Facade | Plain class with signals | Encapsulating data-fetching logic for complex components |

```typescript
// Example: studies page state
readonly studies = signal<Study[]>([]);
readonly isLoading = signal(false);
readonly filters = signal<StudyFilters>({ page: 1, pageSize: 20 });

readonly filteredCount = computed(() => this.studies().length);

constructor() {
  effect(() => {
    const f = this.filters();
    this.loadStudies(f);
  });
}
```

RxJS `Observable` is still used for HTTP calls via `HttpClient` and for the SignalR message stream, but is converted to signals at the boundary using `toSignal()`.

---

## OnPush Change Detection

Every component uses `ChangeDetectionStrategy.OnPush`. Angular will only re-render a component when:

- A signal it reads has emitted a new value.
- An `@Input()` reference changes.
- An event handler fires.
- An async pipe resolves.

This means the component tree is not checked on every tick, resulting in significantly lower CPU overhead for pages with large lists or real-time updates.

---

## Lazy Loading

Every feature is a **lazy-loaded route**. The main bundle (`main.js`) contains only the shell layout and the router. Feature code is downloaded on demand when the user navigates to that route.

```typescript
// app.routes.ts
export const routes: Routes = [
  { path: 'dashboard', loadComponent: () => import('./features/dashboard/...') },
  { path: 'studies',   loadComponent: () => import('./features/studies/...') },
  { path: 'nodes',     loadComponent: () => import('./features/nodes/...') },
  // ...
];
```

This keeps the initial page load small (~150 KB gzipped) regardless of the number of features.

---

## Feature Folder Structure

Each feature follows a consistent three-layer structure:

```
src/app/features/<feature>/
├── presentation/        # Components, dialogs, cards (UI layer)
│   ├── <feature>-page/
│   │   ├── <feature>-page.component.ts
│   │   ├── <feature>-page.component.html
│   │   └── <feature>-page.component.scss
│   └── <sub-component>/
├── infrastructure/      # Services, API adapters
│   └── <feature>.service.ts
└── models/              # TypeScript interfaces and enums
    └── <feature>.model.ts
```

The `presentation/` layer imports from `infrastructure/` and `models/` but never the reverse. Services in `infrastructure/` call `ApiService` and return typed observables or promises; components convert these to signals.

---

## Core Services

Located in `src/app/core/`:

| Service | File | Responsibility |
|---------|------|---------------|
| `ApiService` | `api.service.ts` | Base HTTP wrapper; injects `HttpClient`, attaches auth headers, handles 401 refresh |
| `AuthService` | `auth.service.ts` | JWT management; stores access token in memory; exposes `currentUser` signal |
| `api-routes.ts` | `api-routes.ts` | Typed constants for every API URL; single source of truth |
| `NotificationHubService` | `notification-hub.service.ts` | Manages SignalR connection lifecycle |

### `api-routes.ts` Example

```typescript
export const API_ROUTES = {
  auth: {
    login: '/api/auth/login',
    refresh: '/api/auth/refresh',
    me: '/api/auth/me',
  },
  studies: {
    list: '/api/studies',
    byId: (id: string) => `/api/studies/${id}`,
    export: '/api/studies/export/csv',
  },
  nodes: {
    list: '/api/nodes',
    byId: (id: string) => `/api/nodes/${id}`,
    sync: (id: string) => `/api/nodes/${id}/sync`,
  },
} as const;
```

---

## Real-Time Notifications — SignalR

The SPA connects to the Hub's SignalR endpoint at `/hub/notifications` using `@microsoft/signalr`.

```typescript
// notification-hub.service.ts
const connection = new HubConnectionBuilder()
  .withUrl('/hub/notifications', {
    accessTokenFactory: () => this.authService.accessToken(),
  })
  .withAutomaticReconnect()
  .build();

connection.on('StudyReceived', (study: Study) => {
  this.studyReceivedSubject.next(study);
});
```

The dashboard subscribes to the `StudyReceived` event to update the live study feed without polling.

---

## Build and Deployment

### Production Build

```bash
ng build --configuration production
```

Output is written to `dist/dicomedge-ui/browser/`. The Hub's deployment pipeline copies this output to `wwwroot/` so that ASP.NET Core's static file middleware serves the SPA. The Hub is configured with a catch-all route that returns `index.html` for all non-API paths, enabling client-side routing.

```csharp
// Program.cs
app.UseStaticFiles();
app.MapFallbackToFile("index.html");
```

### Development Mode

```bash
ng serve
```

The Angular dev server proxies API requests to the Hub at `http://localhost:5000`:

```json
// proxy.conf.json
{
  "/api": {
    "target": "http://localhost:5000",
    "secure": false,
    "changeOrigin": true
  },
  "/hub": {
    "target": "http://localhost:5000",
    "secure": false,
    "ws": true
  }
}
```

The SPA is served at `http://localhost:4200` during development.

---

## Forms

All forms use Angular **Reactive Forms** with signal integration for dynamic behavior:

```typescript
readonly form = new FormGroup({
  name: new FormControl('', Validators.required),
  isEnabled: new FormControl(true),
});

// Read form value as a signal
readonly formValue = toSignal(this.form.valueChanges);
```

Form validation errors are displayed inline using Angular Material `mat-error` elements. Submit buttons are disabled while the form is invalid or a request is in-flight.

---

## Dialogs

Complex create/edit workflows use Angular Material `MatDialog`. Dialogs are opened from parent components:

```typescript
this.dialog.open(NodeFormDialogComponent, {
  width: '600px',
  data: { node: this.selectedNode() },
});
```

Dialog components receive data via `MAT_DIALOG_DATA` injection and emit results via `MatDialogRef.close()`. The parent subscribes to `afterClosed()` to refresh its list if the dialog confirmed.
