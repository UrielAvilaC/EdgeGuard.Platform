# Internal UI Component Library

The EdgeGuard SPA includes a small library of shared UI components located in `src/app/shared/ui/`. These components encapsulate design system decisions (colors, spacing, interaction patterns) so that feature components remain focused on domain logic.

All components are standalone, use `ChangeDetectionStrategy.OnPush`, and accept typed Angular signal inputs where applicable.

---

## UiIconButton

A compact icon-only button with an optional tooltip and support for loading and disabled states. Used for action buttons in table rows, detail page toolbars, and card headers.

**Selector:** `<ui-icon-button>`

### Inputs

| Input | Type | Default | Description |
|-------|------|---------|-------------|
| `icon` | `IconDefinition` | required | Font Awesome icon definition (e.g. `faEdit`, `faTrash`) |
| `tooltip` | `string` | `''` | Text shown in a Material tooltip on hover |
| `variant` | `'primary' \| 'danger' \| 'ghost'` | `'ghost'` | Color variant |
| `size` | `'sm' \| 'md' \| 'lg'` | `'md'` | Button size |
| `disabled` | `boolean` | `false` | Disables click events and applies muted styling |
| `loading` | `boolean` | `false` | Replaces icon with a spinner; disables click |
| `ariaLabel` | `string` | `''` | Accessible label (required for screen readers when tooltip is not descriptive) |

### Outputs

| Output | Type | Description |
|--------|------|-------------|
| `clicked` | `EventEmitter<void>` | Emitted on click when not disabled or loading |

### Usage Example

```html
<!-- Edit action in a table row -->
<ui-icon-button
  [icon]="faEdit"
  tooltip="Edit node"
  variant="ghost"
  ariaLabel="Edit node CT-SALA-1"
  (clicked)="onEdit(node)"
/>

<!-- Danger delete button with loading state -->
<ui-icon-button
  [icon]="faTrash"
  tooltip="Delete"
  variant="danger"
  [loading]="isDeleting()"
  (clicked)="onDelete(node)"
/>
```

---

## UiSlideToggle

A labeled on/off toggle switch. Wraps the native HTML checkbox with consistent styling. Used for enabling/disabling nodes, routing rules, and settings.

**Selector:** `<ui-slide-toggle>`

### Inputs

| Input | Type | Default | Description |
|-------|------|---------|-------------|
| `checked` | `boolean` | `false` | Current toggle state |
| `label` | `string` | `''` | Text label displayed beside the toggle |
| `labelPosition` | `'before' \| 'after'` | `'after'` | Label placement relative to the toggle |
| `disabled` | `boolean` | `false` | Prevents interaction |
| `loading` | `boolean` | `false` | Shows a spinner overlay while an async operation is in progress |

### Outputs

| Output | Type | Description |
|--------|------|-------------|
| `changed` | `EventEmitter<boolean>` | Emits the new state after user interaction |

### Usage Example

```html
<!-- Enable/disable a routing rule -->
<ui-slide-toggle
  [checked]="rule.isEnabled"
  label="Enabled"
  [loading]="isSavingRule()"
  (changed)="onToggleRule(rule, $event)"
/>

<!-- Compact toggle without label -->
<ui-slide-toggle
  [checked]="node.isActive"
  labelPosition="before"
  label="Active"
  (changed)="onToggleNode(node, $event)"
/>
```

---

## UiStatusBadge

Displays a `StudyStatus` enum value as a colored pill badge. Color coding is consistent across the entire application.

**Selector:** `<ui-status-badge>`

### Inputs

| Input | Type | Default | Description |
|-------|------|---------|-------------|
| `status` | `StudyStatus` | required | The status value to display |
| `size` | `'sm' \| 'md'` | `'md'` | Controls badge font size and padding |

### Color Mapping

| Status | Color | Tailwind classes |
|--------|-------|-----------------|
| `Pending` | Amber | `bg-amber-100 text-amber-800` |
| `Sent` | Green | `bg-green-100 text-green-800` |
| `Failed` | Red | `bg-red-100 text-red-800` |
| `Cancelled` | Gray | `bg-gray-100 text-gray-600` |

### Usage Example

```html
<!-- In a studies table column -->
<ui-status-badge [status]="study.status" />

<!-- Small variant for compact lists -->
<ui-status-badge [status]="study.status" size="sm" />
```

### TypeScript

```typescript
export type StudyStatus = 'Pending' | 'Sent' | 'Failed' | 'Cancelled';
```

---

## UiConditionChip

A color-coded chip that represents a single condition in a DICOM or HL7 routing rule. Each condition type has a distinct color to aid visual scanning of complex rule sets.

**Selector:** `<ui-condition-chip>`

### Inputs

| Input | Type | Default | Description |
|-------|------|---------|-------------|
| `field` | `ConditionField` | required | The condition field type |
| `operator` | `string` | required | The comparison operator (e.g. `Equals`, `Contains`, `StartsWith`) |
| `value` | `string` | required | The match value |
| `removable` | `boolean` | `false` | Shows a remove (×) button; used in form builders |

### Outputs

| Output | Type | Description |
|--------|------|-------------|
| `removed` | `EventEmitter<void>` | Emitted when the remove button is clicked |

### Color Mapping by Field

| Field | Color | Visual |
|-------|-------|--------|
| `Modality` | Purple | `bg-purple-100 text-purple-800 border-purple-300` |
| `SourceAeTitle` | Gray | `bg-gray-100 text-gray-700 border-gray-300` |
| `InstitutionName` | Gray | `bg-gray-100 text-gray-700 border-gray-300` |
| `StudyDescription` | Amber | `bg-amber-100 text-amber-800 border-amber-300` |
| `PatientClass` | Blue | `bg-blue-100 text-blue-800 border-blue-300` |
| `MessageType` | Teal | `bg-teal-100 text-teal-800 border-teal-300` |

### Usage Example

```html
<!-- Read-only display of rule conditions -->
@for (condition of rule.conditions; track condition.field) {
  <ui-condition-chip
    [field]="condition.field"
    [operator]="condition.operator"
    [value]="condition.value"
  />
}

<!-- In a form builder with remove capability -->
<ui-condition-chip
  [field]="condition.field"
  [operator]="condition.operator"
  [value]="condition.value"
  [removable]="true"
  (removed)="onRemoveCondition(i)"
/>
```

### Rendered Output Example

A chip for `Modality = CT` renders as:

```
[ Modality: CT × ]   ← purple chip with optional remove button
```

---

## UiPaginator

Pagination controls with page-size selector. Wraps the Material paginator with consistent sizing and project-specific page-size options.

**Selector:** `<ui-paginator>`

### Inputs

| Input | Type | Default | Description |
|-------|------|---------|-------------|
| `totalCount` | `number` | required | Total number of records across all pages |
| `page` | `number` | `1` | Current page number (1-based) |
| `pageSize` | `number` | `20` | Number of records per page |
| `pageSizeOptions` | `number[]` | `[10, 20, 50, 100]` | Available page size choices |
| `disabled` | `boolean` | `false` | Disables navigation buttons during loading |

### Outputs

| Output | Type | Description |
|--------|------|-------------|
| `pageChanged` | `EventEmitter<PageChangeEvent>` | Emitted when the user changes page or page size |

```typescript
export interface PageChangeEvent {
  page: number;
  pageSize: number;
}
```

### Usage Example

```html
<ui-paginator
  [totalCount]="totalStudies()"
  [page]="filters().page"
  [pageSize]="filters().pageSize"
  [disabled]="isLoading()"
  (pageChanged)="onPageChange($event)"
/>
```

```typescript
onPageChange(event: PageChangeEvent): void {
  this.filters.update(f => ({ ...f, page: event.page, pageSize: event.pageSize }));
}
```

---

## Priority Badge

A small circular badge displaying a routing rule's numeric priority. Used in the routing rules list to show evaluation order at a glance. This is an inline component (not a standalone named component) implemented directly within the routing rules feature but documented here for consistency.

**Visual design:** Azure blue (`bg-sky-600 text-white`) filled circle, 24 px diameter, containing the priority number.

### Usage Example

```html
<!-- Routing rules list row -->
<span class="priority-badge">{{ rule.priority }}</span>
```

```scss
.priority-badge {
  @apply inline-flex items-center justify-center w-6 h-6
         rounded-full bg-sky-600 text-white text-xs font-semibold;
}
```

Rules with lower numbers are evaluated first (priority `1` is highest). The badge renders the number in the circle:

```
  [1]  Route CT to PACS-Principal   CT = Modality    [toggle] [edit] [delete]
  [2]  Route MG to Archive          MG = Modality    [toggle] [edit] [delete]
 [10]  Catch-all to PACS-Principal  (no conditions)  [toggle] [edit] [delete]
```

---

## Component Import Pattern

All UI library components are standalone and must be explicitly imported in each consuming component:

```typescript
import { UiIconButton } from '@shared/ui/ui-icon-button/ui-icon-button.component';
import { UiSlideToggle } from '@shared/ui/ui-slide-toggle/ui-slide-toggle.component';
import { UiStatusBadge } from '@shared/ui/ui-status-badge/ui-status-badge.component';
import { UiConditionChip } from '@shared/ui/ui-condition-chip/ui-condition-chip.component';
import { UiPaginator } from '@shared/ui/ui-paginator/ui-paginator.component';

@Component({
  standalone: true,
  imports: [UiIconButton, UiSlideToggle, UiStatusBadge, UiConditionChip, UiPaginator],
  // ...
})
export class MyFeaturePageComponent { ... }
```

The `@shared/ui` path alias is configured in `tsconfig.json` and maps to `src/app/shared/ui/`.
