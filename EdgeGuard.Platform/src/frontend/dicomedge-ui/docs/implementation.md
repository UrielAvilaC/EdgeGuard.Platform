# EdgeGuard SPA — Plan de Implementación Enterprise

## Arquitectura Objetivo

```
src/app/
├── core/                          # Singleton — importado UNA vez en app.config
│   ├── auth/
│   │   ├── guards/
│   │   │   ├── auth.guard.ts
│   │   │   └── permission.guard.ts
│   │   ├── interceptors/
│   │   │   ├── auth.interceptor.ts          # Inyecta Bearer token
│   │   │   └── token-refresh.interceptor.ts # Auto-refresh en 401
│   │   ├── models/
│   │   │   └── auth.models.ts
│   │   ├── services/
│   │   │   └── auth.service.ts             # login, logout, refresh, me()
│   │   └── store/
│   │       └── auth.store.ts               # signal store (user, token, permisos)
│   ├── api/
│   │   ├── api-routes.ts                   # Constantes de rutas "/api/studies", etc.
│   │   ├── api-client.ts                   # Wrapper tipado sobre HttpClient
│   │   └── api-error.model.ts              # ProblemDetails / error shape
│   ├── interceptors/
│   │   ├── error.interceptor.ts            # Global error handler → toast/redirect
│   │   ├── loading.interceptor.ts          # Señal global isLoading
│   │   └── correlation-id.interceptor.ts   # X-Correlation-Id header
│   ├── services/
│   │   ├── toast.service.ts                # Notificaciones snackbar
│   │   └── loading.service.ts              # Signal de carga global
│   └── config/
│       └── environment.config.ts           # InjectionToken con apiBaseUrl
│
├── shared/                        # Reutilizable — importado por features
│   ├── components/
│   │   ├── ui-button/
│   │   ├── ui-icon-button/
│   │   ├── ui-alert/
│   │   ├── ui-chip/
│   │   ├── ui-chip-list/
│   │   ├── ui-data-table/          # Paginación + Ordenamiento + Headers custom
│   │   ├── ui-confirm-dialog/
│   │   ├── ui-empty-state/
│   │   ├── ui-loading-spinner/
│   │   ├── ui-page-header/
│   │   ├── ui-search-bar/
│   │   ├── ui-status-badge/
│   │   └── ui-stat-card/
│   ├── forms/
│   │   ├── form-field/              # Wrapper mat-form-field + Tailwind
│   │   ├── input-text/
│   │   ├── input-password/
│   │   ├── textarea/
│   │   ├── dropdown/                # mat-select estilizado
│   │   ├── datepicker/
│   │   ├── date-range-picker/
│   │   ├── checkbox/
│   │   ├── radio-group/
│   │   ├── slide-toggle/
│   │   └── autocomplete/
│   ├── directives/
│   │   ├── has-permission.directive.ts    # *hasPermission="'ViewStudies'"
│   │   └── auto-focus.directive.ts
│   ├── pipes/
│   │   ├── relative-time.pipe.ts          # "hace 5 min"
│   │   ├── file-size.pipe.ts              # "1.2 GB"
│   │   ├── truncate.pipe.ts
│   │   └── safe-html.pipe.ts
│   ├── models/
│   │   ├── pagination.model.ts            # PagedResult<T>, PaginationParams
│   │   ├── sort.model.ts                  # SortDirection, SortParams
│   │   └── filter.model.ts               # BaseFilter
│   └── utils/
│       ├── form.utils.ts
│       └── date.utils.ts
│
├── layout/                        # Shell de la app
│   ├── main-layout/
│   │   └── main-layout.ts         # Sidenav + Toolbar + <router-outlet>
│   ├── sidebar/
│   │   └── sidebar.ts             # Nav links con iconos FA, colapsable
│   ├── header/
│   │   └── header.ts              # Toolbar: user menu, notificaciones, dark mode
│   └── models/
│       └── nav-item.model.ts
│
├── features/                      # Cada feature = lazy loaded
│   ├── dashboard/
│   │   ├── infrastructure/
│   │   │   └── dashboard-api.service.ts
│   │   ├── services/
│   │   │   ├── dashboard.store.ts
│   │   │   └── dashboard.facade.ts
│   │   ├── presentation/
│   │   │   ├── dashboard-page/
│   │   │   ├── stats-overview/
│   │   │   ├── recent-studies-widget/
│   │   │   ├── node-status-widget/
│   │   │   └── queue-summary-widget/
│   │   └── dashboard.routes.ts
│   │
│   ├── studies/
│   │   ├── infrastructure/
│   │   │   └── studies-api.service.ts
│   │   ├── services/
│   │   │   ├── studies.store.ts
│   │   │   └── studies.facade.ts
│   │   ├── presentation/
│   │   │   ├── studies-page/
│   │   │   ├── study-detail-page/
│   │   │   ├── study-filters/
│   │   │   └── study-status-timeline/
│   │   └── studies.routes.ts
│   │
│   ├── patients/
│   │   ├── infrastructure/
│   │   │   └── patients-api.service.ts
│   │   ├── services/
│   │   │   ├── patients.store.ts
│   │   │   └── patients.facade.ts
│   │   ├── presentation/
│   │   │   ├── patients-page/
│   │   │   ├── patient-detail-page/
│   │   │   └── patient-form-dialog/
│   │   └── patients.routes.ts
│   │
│   ├── nodes/
│   │   ├── infrastructure/
│   │   │   └── nodes-api.service.ts
│   │   ├── services/
│   │   │   ├── nodes.store.ts
│   │   │   └── nodes.facade.ts
│   │   ├── presentation/
│   │   │   ├── nodes-page/
│   │   │   ├── node-detail-page/
│   │   │   ├── node-form-dialog/
│   │   │   └── node-health-card/
│   │   └── nodes.routes.ts
│   │
│   ├── pacs/
│   │   ├── infrastructure/
│   │   │   └── pacs-api.service.ts
│   │   ├── services/
│   │   │   ├── pacs.store.ts
│   │   │   └── pacs.facade.ts
│   │   ├── presentation/
│   │   │   ├── pacs-page/
│   │   │   └── pacs-form-dialog/
│   │   └── pacs.routes.ts
│   │
│   ├── hl7/
│   │   ├── infrastructure/
│   │   │   ├── hl7-status-api.service.ts
│   │   │   ├── routing-rules-api.service.ts
│   │   │   └── queue-api.service.ts
│   │   ├── services/
│   │   │   ├── hl7.store.ts
│   │   │   └── hl7.facade.ts
│   │   ├── presentation/
│   │   │   ├── hl7-overview-page/
│   │   │   ├── hl7-listener-status/
│   │   │   ├── hl7-message-list/
│   │   │   ├── hl7-message-detail-dialog/
│   │   │   ├── routing-rules-page/
│   │   │   ├── routing-rule-form-dialog/
│   │   │   └── queue-monitor-page/
│   │   └── hl7.routes.ts
│   │
│   ├── whatsapp/
│   │   ├── infrastructure/
│   │   │   └── whatsapp-api.service.ts
│   │   ├── services/
│   │   │   ├── whatsapp.store.ts
│   │   │   └── whatsapp.facade.ts
│   │   ├── presentation/
│   │   │   ├── whatsapp-overview-page/
│   │   │   ├── templates-page/
│   │   │   ├── template-form-dialog/
│   │   │   ├── auto-send-rules-page/
│   │   │   ├── rule-form-dialog/
│   │   │   └── send-manual-dialog/
│   │   └── whatsapp.routes.ts
│   │
│   ├── users/
│   │   ├── infrastructure/
│   │   │   └── users-api.service.ts
│   │   ├── services/
│   │   │   ├── users.store.ts
│   │   │   └── users.facade.ts
│   │   ├── presentation/
│   │   │   ├── users-page/
│   │   │   ├── user-detail-page/
│   │   │   ├── user-form-dialog/
│   │   │   └── role-permission-panel/
│   │   └── users.routes.ts
│   │
│   ├── settings/
│   │   ├── infrastructure/
│   │   │   ├── system-settings-api.service.ts
│   │   │   └── node-config-api.service.ts
│   │   ├── services/
│   │   │   ├── settings.store.ts
│   │   │   └── settings.facade.ts
│   │   ├── presentation/
│   │   │   ├── settings-page/
│   │   │   ├── setting-category-panel/
│   │   │   └── node-config-page/
│   │   └── settings.routes.ts
│   │
│   ├── audit/
│   │   ├── infrastructure/
│   │   │   └── audit-api.service.ts
│   │   ├── services/
│   │   │   ├── audit.store.ts
│   │   │   └── audit.facade.ts
│   │   ├── presentation/
│   │   │   ├── audit-page/
│   │   │   └── audit-detail-dialog/
│   │   └── audit.routes.ts
│   │
│   └── auth/
│       └── presentation/
│           ├── login-page/
│           └── unauthorized-page/
│
├── app.ts                         # Standalone root component
├── app.config.ts                  # provideRouter, provideHttpClient, etc.
└── app.routes.ts                  # Lazy-loaded feature routes
```

---

## Patrones Clave

### Store (Signal-based State)

```typescript
// Ejemplo: studies.store.ts
@Injectable()
export class StudiesStore {
  // Estado privado
  private readonly _studies = signal<StudyDto[]>([]);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);
  private readonly _pagination = signal<PaginationMeta>({ page: 1, pageSize: 25, total: 0 });
  private readonly _filter = signal<StudyFilter>({});
  private readonly _sort = signal<SortParams>({ sortBy: 'studyDate', sortDir: 'desc' });

  // Estado público (readonly)
  readonly studies = this._studies.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();
  readonly pagination = this._pagination.asReadonly();

  // Computed
  readonly isEmpty = computed(() => this._studies().length === 0 && !this._loading());
  readonly totalPages = computed(() => Math.ceil(this._pagination().total / this._pagination().pageSize));

  // Mutaciones
  setStudies(studies: StudyDto[], meta: PaginationMeta) { ... }
  setLoading(loading: boolean) { ... }
  setError(error: string | null) { ... }
  updateFilter(filter: Partial<StudyFilter>) { ... }
  updateSort(sort: SortParams) { ... }
}
```

### Facade (Orquestador)

```typescript
// Ejemplo: studies.facade.ts
@Injectable()
export class StudiesFacade {
  private readonly api = inject(StudiesApiService);
  private readonly store = inject(StudiesStore);
  private readonly toast = inject(ToastService);

  // Expone estado del store
  readonly studies = this.store.studies;
  readonly loading = this.store.loading;
  readonly pagination = this.store.pagination;

  // Acciones
  loadStudies(): void {
    this.store.setLoading(true);
    this.api.getStudies(this.store.currentFilter(), this.store.currentPagination())
      .pipe(finalize(() => this.store.setLoading(false)))
      .subscribe({
        next: (result) => this.store.setStudies(result.items, result.meta),
        error: (err) => this.store.setError(err.message),
      });
  }

  changePage(page: number): void { ... }
  changeSort(sort: SortParams): void { ... }
  updateFilter(filter: Partial<StudyFilter>): void { ... }
  exportCsv(): void { ... }
}
```

### Infrastructure (API Service)

```typescript
// Ejemplo: studies-api.service.ts
@Injectable({ providedIn: 'root' })
export class StudiesApiService {
  private readonly api = inject(ApiClient);

  getStudies(filter: StudyFilter, pagination: PaginationParams): Observable<PagedResult<StudyDto>> {
    return this.api.get<PagedResult<StudyDto>>(API_ROUTES.STUDIES.LIST, {
      params: { ...filter, ...pagination },
    });
  }

  getStudyById(id: string): Observable<StudyDto> {
    return this.api.get<StudyDto>(API_ROUTES.STUDIES.BY_ID(id));
  }

  updateStudy(id: string, request: UpdateStudyRequest): Observable<StudyDto> {
    return this.api.put<StudyDto>(API_ROUTES.STUDIES.BY_ID(id), request);
  }

  exportCsv(filter: StudyFilter): Observable<Blob> {
    return this.api.getBlob(API_ROUTES.STUDIES.EXPORT, { params: filter });
  }
}
```

### API Routes (Constantes)

```typescript
// api-routes.ts
export const API_ROUTES = {
  AUTH: {
    LOGIN:       '/api/auth/login',
    REFRESH:     '/api/auth/refresh',
    REVOKE:      '/api/auth/revoke',
    ME:          '/api/auth/me',
  },
  STUDIES: {
    LIST:        '/api/studies',
    BY_ID:       (id: string) => `/api/studies/${id}`,
    BY_UID:      (uid: string) => `/api/studies/by-uid/${uid}`,
    BY_PATIENT:  (patientId: string) => `/api/studies/by-patient/${patientId}`,
    PENDING:     '/api/studies/pending-pacs',
    COUNT:       '/api/studies/count',
    EXPORT:      '/api/studies/export',
  },
  // ... demás rutas
} as const;
```

---

## Fases de Implementación

---

### FASE 0 — Fundación y Migración (2-3 días)

> Migrar de NgModule a standalone, configurar la infraestructura base.

**0.1 — Migración a standalone bootstrap**
- [x] Reemplazar `main.ts` → `bootstrapApplication(App, appConfig)`
- [x] Crear `app.config.ts` con `provideRouter`, `provideHttpClient`, `provideAnimationsAsync`
- [x] Crear `app.routes.ts` con rutas lazy-loaded
- [x] Eliminar `app-module.ts`, `app-routing-module.ts`
- [x] Eliminar `material/material.module.ts` (importar Material por componente)
- [x] Convertir `App` a standalone con `changeDetection: OnPush`
- [x] Renombrar carpeta `feaures/` → `features/`

**0.2 — Environment config**
- [x] Crear `core/config/environment.config.ts` con `InjectionToken<EnvironmentConfig>`
- [x] Definir `EnvironmentConfig` interface: `{ apiBaseUrl, production, signalRUrl }`
- [x] Poblar `environment.ts` y `environment.development.ts`

**0.3 — Core API layer**
- [x] Crear `core/api/api-routes.ts` con TODAS las constantes de ruta (16 controllers)
- [x] Crear `core/api/api-client.ts` — wrapper tipado sobre `HttpClient` (get, post, put, delete, getBlob)
- [x] Crear `core/api/api-error.model.ts` — `ApiError`, `ValidationError`, `ProblemDetails`

**0.4 — Interceptors**
- [x] Crear `core/interceptors/error.interceptor.ts` — captura errores HTTP, muestra toast, redirige 401→login
- [x] Crear `core/interceptors/loading.interceptor.ts` — señal global `isLoading`
- [x] Crear `core/interceptors/correlation-id.interceptor.ts` — agrega `X-Correlation-Id` UUID
- [x] Registrar en `app.config.ts` con `withInterceptors([...])`

**0.5 — Core services**
- [x] Crear `core/services/toast.service.ts` — wrapper de `MatSnackBar` con métodos success/error/warning/info
- [x] Crear `core/services/loading.service.ts` — signal que los interceptors leen/escriben

**0.6 — Font Awesome setup (standalone)**
- [x] Configurar FA icon library en `app.config.ts` usando `FaIconLibrary` via `APP_INITIALIZER` o en `App` constructor
- [x] Agregar iconos usados: navigation, CRUD, status, etc.

**Entregable**: La app compila, arranca con `ng serve`, tiene `<router-outlet>` y toda la capa core lista.

---

### FASE 1 — Shared Components Library (4-5 días)

> Crear todos los componentes reutilizables estilizados con Material + Tailwind.

**1.1 — Shared models**
- [x] `shared/models/pagination.model.ts` — `PagedResult<T>`, `PaginationParams`, `PaginationMeta`
- [x] `shared/models/sort.model.ts` — `SortDirection`, `SortParams`
- [x] `shared/models/filter.model.ts` — `BaseFilter`
- [x] `shared/models/table.model.ts` — `TableColumn<T>` con `key`, `header`, `sortable`, `cellTemplate`, `width`, `align`

**1.2 — UI Components básicos**
- [x] `ui-button` — wraps `mat-button` con variantes: primary, secondary, danger, ghost + tamaños sm/md/lg + loading state
- [x] `ui-icon-button` — wraps `mat-icon-button` con FA icon input
- [x] `ui-alert` — tipo: success/error/warning/info, dismissible, con ícono auto
- [x] `ui-chip` / `ui-chip-list` — wraps `mat-chip` con colores semánticos
- [x] `ui-loading-spinner` — overlay o inline, usa `mat-progress-spinner`
- [x] `ui-empty-state` — ícono + título + descripción + CTA opcional
- [x] `ui-confirm-dialog` — diálogo genérico de confirmación (inyecta título, mensaje, botones)
- [x] `ui-page-header` — título + breadcrumb + acciones slot
- [x] `ui-search-bar` — input con debounce, ícono search, clear button, emite `searchChange`
- [x] `ui-status-badge` — chip de color según status string (mapeo configurable)
- [x] `ui-stat-card` — tarjeta KPI: ícono + valor + label + trend opcional

**1.3 — Form Components**
- [x] `input-text` — `ControlValueAccessor` + mat-input + Tailwind, soporta prefix/suffix
- [x] `input-password` — como input-text con toggle visibilidad
- [x] `textarea` — `ControlValueAccessor` + mat-input textarea + autosize
- [x] `dropdown` — `ControlValueAccessor` + mat-select + opciones tipadas
- [x] `datepicker` — `ControlValueAccessor` + mat-datepicker
- [x] `date-range-picker` — `ControlValueAccessor` + mat-date-range-picker
- [x] `checkbox` — `ControlValueAccessor` + mat-checkbox
- [x] `radio-group` — `ControlValueAccessor` + mat-radio-group con opciones tipadas
- [x] `slide-toggle` — `ControlValueAccessor` + mat-slide-toggle
- [x] `autocomplete` — `ControlValueAccessor` + mat-autocomplete con búsqueda async

**1.4 — Data Table Component** (componente estrella)
- [x] `ui-data-table` — componente genérico con:
  - Input: `columns: TableColumn<T>[]` — define headers, keys, templates
  - Input: `data: T[]` — datos de la página actual
  - Input: `pagination: PaginationMeta` — total, page, pageSize
  - Input: `sort: SortParams` — columna y dirección actual
  - Input: `loading: boolean`
  - Input: `emptyMessage: string`
  - Input: `selectable: boolean` — checkbox de selección
  - Output: `pageChange`, `sortChange`, `rowClick`, `selectionChange`
  - Usa `mat-table` + `mat-sort` + `mat-paginator` internamente
  - Template customizable por columna via `ng-template` con `let-row`
  - Estilizado con Tailwind: hover rows, bordes sutiles, header sticky

**1.5 — Pipes y Directivas**
- [x] `relative-time.pipe.ts` — "hace 5 minutos", "hace 2 horas"
- [x] `file-size.pipe.ts` — bytes → "1.2 GB"
- [x] `truncate.pipe.ts` — corta texto con "..."
- [x] `has-permission.directive.ts` — `*hasPermission="'ViewStudies'"` oculta/muestra elementos
- [x] `auto-focus.directive.ts` — foco automático en inputs

**Entregable**: Librería de ~25 componentes reutilizables, todos standalone, con OnPush, inputs via `input()`, outputs via `output()`, estilizados con Material + Tailwind.

---

### FASE 2 — Layout Shell + Auth (2-3 días)

> Crear el shell de navegación y el flujo de autenticación.

**2.1 — Auth infrastructure**
- [x] `core/auth/models/auth.models.ts` — `LoginRequest`, `AuthResponse`, `UserProfile`, `Permission`
- [x] `core/auth/store/auth.store.ts` — signals: `currentUser`, `accessToken`, `isAuthenticated`, `permissions`
- [x] `core/auth/services/auth.service.ts` — login, logout, refresh, me(), persistencia en localStorage
- [x] `core/auth/interceptors/auth.interceptor.ts` — inyecta `Authorization: Bearer <token>`
- [x] `core/auth/interceptors/token-refresh.interceptor.ts` — detecta 401, intenta refresh, re-envía request
- [x] `core/auth/guards/auth.guard.ts` — redirige a login si no autenticado
- [x] `core/auth/guards/permission.guard.ts` — verifica permiso específico en `data.permission`

**2.2 — Login page**
- [x] `features/auth/presentation/login-page/` — formulario reactivo, validación, error display, redirect post-login
- [x] `features/auth/presentation/unauthorized-page/` — página 403 con mensaje y botón volver

**2.3 — Layout components**
- [x] `layout/main-layout/` — estructura `mat-sidenav-container`:
  ```
  ┌───────────────────────────────────┐
  │  Header (toolbar)                 │
  ├──────────┬────────────────────────┤
  │ Sidebar  │  <router-outlet>       │
  │ (nav)    │  (content area)        │
  │          │                        │
  └──────────┴────────────────────────┘
  ```
- [x] `layout/sidebar/` — navegación con FA icons, items agrupados, colapsable, resalta ruta activa
  - Dashboard, Estudios, Pacientes, Nodos, PACS, HL7, WhatsApp, Usuarios, Config, Auditoría
- [x] `layout/header/` — toolbar con: logo, título de página, botón hamburger (mobile), dark mode toggle, user menu dropdown (perfil, logout)

**2.4 — Routing con layout**
- [x] Configurar `app.routes.ts`:
  ```typescript
  {
    path: 'login', loadComponent: () => import('./features/auth/...'),
  },
  {
    path: '',
    component: MainLayout,
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', loadChildren: () => import('./features/dashboard/dashboard.routes') },
      { path: 'studies', loadChildren: () => import('./features/studies/studies.routes') },
      // ... demás features lazy
    ]
  },
  { path: '**', redirectTo: 'dashboard' }
  ```

**Entregable**: Login funcional, layout con sidebar + header, navegación entre secciones vacías, guards de auth y permisos activos.

---

### FASE 3 — Feature: Dashboard (2-3 días)

> Página principal con KPIs y widgets en tiempo real.

**3.1 — Infrastructure**
- [x] `dashboard-api.service.ts` — `getSummary(): Observable<DashboardSummaryDto>`

**3.2 — Store + Facade**
- [x] `dashboard.store.ts` — signals: `summary`, `loading`, `lastRefreshedAt`
- [x] `dashboard.facade.ts` — `loadSummary()`, auto-refresh cada 30s, `refresh()`

**3.3 — Presentation**
- [x] `dashboard-page/` — grid responsivo de widgets
- [x] `stats-overview/` — 4-6 `ui-stat-card`: Total Estudios, Pacientes, Nodos Activos, Pendientes PACS, Fallidos, Cola
- [x] `recent-studies-widget/` — `ui-data-table` con últimos 10 estudios (status badge, fecha relativa)
- [x] `node-status-widget/` — lista de nodos con status badge, último heartbeat, barras de storage
- [x] `queue-summary-widget/` — indicadores de la cola HL7 (pending, dispatching, delivered, failed)

**3.4 — SignalR (opcional fase avanzada)**
- [x] Conectar `/hubs/notifications` para refresh en tiempo real

**Entregable**: Dashboard funcional con datos reales del backend, auto-refresh, responsive.

---

### FASE 4 — Feature: Studies (3-4 días)

> CRUD completo de estudios con filtros avanzados y exportación.

**4.1 — Infrastructure**
- [x] `studies-api.service.ts` — getStudies, getById, getByUid, update, updateStatus, export, count

**4.2 — Store + Facade**
- [x] `studies.store.ts` — lista paginada, filtros, sort, estudio seleccionado
- [x] `studies.facade.ts` — loadStudies, changePage, changeSort, updateFilter, exportCsv, updateStatus

**4.3 — Presentation**
- [x] `studies-page/` — `ui-page-header` + filtros + `ui-data-table`
- [x] `study-filters/` — panel con: búsqueda texto, status dropdown, modality dropdown, nodo dropdown, date range, urgente toggle
- [x] `study-detail-page/` — detalle completo: metadata, series, audit trail, acciones (editar, cambiar status)
- [x] `study-status-timeline/` — visualización del lifecycle del estudio con `StudyStatusAudit`

**Entregable**: Listado paginado/filtrado/ordenado, detalle, edición inline de metadata, cambio de status, export CSV.

---

### FASE 5 — Feature: Patients (2-3 días)

**5.1 — Infrastructure**
- [x] `patients-api.service.ts` — getPaged, getById, getByDicomId, search, update, count, export, import

**5.2 — Store + Facade**
- [x] `patients.store.ts` / `patients.facade.ts`

**5.3 — Presentation**
- [x] `patients-page/` — tabla paginada con filtros (búsqueda, nodo, activo, tiene teléfono/email)
- [x] `patient-detail-page/` — datos demográficos, estudios asociados, historial de notificaciones
- [x] `patient-form-dialog/` — edición de datos del paciente (phone, email)
- [x] Import/Export CSV

**Entregable**: CRUD pacientes, búsqueda, filtrado, import/export.

---

### FASE 6 — Feature: Nodes (2-3 días)

**6.1 — Infrastructure**
- [ ] `nodes-api.service.ts` — getPaged, getAll, getActive, getById, create, update, enable/disable, count

**6.2 — Store + Facade**
- [ ] `nodes.store.ts` / `nodes.facade.ts`

**6.3 — Presentation**
- [ ] `nodes-page/` — tabla con status badges, last heartbeat relativo, storage bars
- [ ] `node-detail-page/` — info completa, PACS assignments, estudios del nodo, health checks, config
- [ ] `node-form-dialog/` — crear/editar nodo (Name, AeTitle, IP, Port, Location, Facility)
- [ ] `node-health-card/` — tarjeta con CPU, memoria, disco, queue depth

**Entregable**: Gestión completa de nodos, monitoreo de salud, asignación de PACS.

---

### FASE 7 — Feature: PACS Servers (1-2 días)

**7.1 — Infrastructure + Store + Facade**
- [ ] `pacs-api.service.ts` — CRUD + enable/disable

**7.2 — Presentation**
- [ ] `pacs-page/` — tabla con status (reachable/unreachable), último C-ECHO
- [ ] `pacs-form-dialog/` — crear/editar servidor PACS

**Entregable**: Administración de servidores PACS.

---

### FASE 8 — Feature: HL7 Integration (3-4 días)

> Agrupa: HL7 Listener Status, Routing Rules, Queue Monitoring

**8.1 — Infrastructure**
- [ ] `hl7-status-api.service.ts` — getStatus, getRecentMessages, getMessageDetail
- [ ] `routing-rules-api.service.ts` — CRUD + enable/disable + updatePriority
- [ ] `queue-api.service.ts` — getSummary, getByStatus, getQueued

**8.2 — Store + Facade**
- [ ] `hl7.store.ts` / `hl7.facade.ts`

**8.3 — Presentation**
- [ ] `hl7-overview-page/` — tabs: Listener Status | Routing Rules | Queue
- [ ] `hl7-listener-status/` — estado (running/stopped), puerto, conexiones activas, badge auto
- [ ] `hl7-message-list/` — tabla de mensajes recientes con status chips
- [ ] `hl7-message-detail-dialog/` — contenido raw HL7, metadata, error info
- [ ] `routing-rules-page/` — tabla draggable (prioridad), enable/disable inline
- [ ] `routing-rule-form-dialog/` — crear/editar regla (name, target node, matchers, priority)
- [ ] `queue-monitor-page/` — visual de pipeline: barras por estado, tabla de mensajes encolados

**Entregable**: Monitoreo completo de la integración HL7.

---

### FASE 9 — Feature: WhatsApp (2-3 días)

**9.1 — Infrastructure**
- [ ] `whatsapp-api.service.ts` — configStatus, templates CRUD, autoSendRules CRUD, sendManual, notifications

**9.2 — Store + Facade**
- [ ] `whatsapp.store.ts` / `whatsapp.facade.ts`

**9.3 — Presentation**
- [ ] `whatsapp-overview-page/` — config status card + tabs: Templates | Auto-Send Rules
- [ ] `templates-page/` — tabla de templates con tags chips
- [ ] `template-form-dialog/` — crear/editar template (name, ContentSid, description, tags)
- [ ] `auto-send-rules-page/` — tabla de reglas: status trigger → template, enable/disable inline
- [ ] `rule-form-dialog/` — crear/editar regla
- [ ] `send-manual-dialog/` — seleccionar estudio, template, recipients con phone input

**Entregable**: Configuración completa de WhatsApp.

---

### FASE 10 — Feature: Users + Settings + Audit (3-4 días)

**10.1 — Users**
- [ ] `users-api.service.ts`, `users.store.ts`, `users.facade.ts`
- [ ] `users-page/` — tabla con filtros, status badges
- [ ] `user-detail-page/` — perfil, roles, permisos, acciones (activate/deactivate, unlock, reset password)
- [ ] `user-form-dialog/` — crear usuario
- [ ] `role-permission-panel/` — panel de asignación drag/checkboxes

**10.2 — Settings**
- [ ] `system-settings-api.service.ts`, `node-config-api.service.ts`
- [ ] `settings-page/` — settings agrupados por categoría, edición inline
- [ ] `setting-category-panel/` — panel expandible por categoría
- [ ] `node-config-page/` — selector de nodo + settings del nodo, push config button

**10.3 — Audit**
- [ ] `audit-api.service.ts`, `audit.store.ts`, `audit.facade.ts`
- [ ] `audit-page/` — tabla con filtros avanzados (tipo, severidad, usuario, entity, fecha)
- [ ] `audit-detail-dialog/` — detalles completos del log

**Entregable**: Gestión de usuarios/roles, configuración del sistema, auditoría completa.

---

### FASE 11 — Polish & Cross-cutting (2-3 días)

**11.1 — UX / Accesibilidad**
- [ ] Verificar todos los componentes con AXE
- [ ] Focus management en diálogos y navegación por teclado
- [ ] ARIA labels en todos los controles interactivos
- [ ] Skip-to-content link
- [ ] Color contrast WCAG AA en todos los estados

**11.2 — Dark mode**
- [ ] Toggle en header que agrega/remueve clase `.dark` en `<html>`
- [ ] Persistir preferencia en localStorage
- [ ] Verificar todos los componentes en dark mode

**11.3 — Error handling robusto**
- [ ] Página 404 not-found con redirect
- [ ] Página 500 error genérico
- [ ] Retry automático en errores de red (interceptor con backoff)
- [ ] Empty states en todas las tablas

**11.4 — Responsive**
- [ ] Sidebar colapsable en mobile (hamburger menu)
- [ ] Tablas con scroll horizontal en mobile
- [ ] Dashboard widgets stack vertical en mobile

**11.5 — Performance**
- [ ] `trackBy` en todos los `@for`
- [ ] Virtual scroll en tablas con muchos registros
- [ ] Preload strategy para rutas frecuentes

**Entregable**: App pulida, accesible, responsive, con manejo de errores completo.

---

## Resumen de Fases

| Fase | Módulo | Estimación | Dependencias |
|------|--------|------------|--------------|
| **0** | Fundación + Migración | 2-3 días | — |
| **1** | Shared Components Library | 4-5 días | Fase 0 |
| **2** | Layout + Auth | 2-3 días | Fase 0, 1 |
| **3** | Dashboard | 2-3 días | Fase 2 |
| **4** | Studies | 3-4 días | Fase 2 |
| **5** | Patients | 2-3 días | Fase 2 |
| **6** | Nodes | 2-3 días | Fase 2 |
| **7** | PACS Servers | 1-2 días | Fase 2 |
| **8** | HL7 Integration | 3-4 días | Fase 2 |
| **9** | WhatsApp | 2-3 días | Fase 2 |
| **10** | Users + Settings + Audit | 3-4 días | Fase 2 |
| **11** | Polish & Cross-cutting | 2-3 días | Todas |

---

## Convenciones de Código

| Aspecto | Convención |
|---------|-----------|
| **Componentes** | Standalone, `OnPush`, `input()` / `output()`, signals |
| **Templates** | `@if`, `@for`, `@switch` — NO `*ngIf`/`*ngFor` |
| **Estado** | Signal store + Facade por feature |
| **Estilos** | Tailwind utilities + Angular Material tokens |
| **Nombres** | kebab-case archivos, PascalCase clases, camelCase propiedades |
| **Rutas** | Lazy loading con `loadChildren` / `loadComponent` |
| **Servicios** | `inject()` function, NO constructor injection |
| **HTTP** | `ApiClient` wrapper → feature `ApiService` → Facade |
| **Formularios** | Reactive Forms con `FormBuilder` |
| **Iconos** | Font Awesome via `<fa-icon>` + Material Icons opcional |
| **Tests** | Vitest, un `.spec.ts` por componente/servicio |
| **Imports Material** | Por componente (no barrel module) |
