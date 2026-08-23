# Internal UI Component Library

The EdgeGuard SPA includes a small library of shared UI components located in `src/app/shared/ui/`. These components encapsulate design system decisions (colors, spacing, interaction patterns) so that feature components remain focused on domain logic.

All components are standalone, use `ChangeDetectionStrategy.OnPush`, and accept typed Angular signal inputs where applicable.

---

## UiDialog

The standard shell for every dialog: fixed header and footer, scrollable body.
Only the body grows and overflows, so the action buttons stay reachable even when
the content is taller than the screen.

**Selector:** `<ui-dialog>`  
**Location:** `src/app/shared/components/ui-dialog/`

New dialogs must use it. A spec (`ui-dialog.spec.ts`) scans every
`*-dialog.component` in the app and fails if one does not, or if it otherwise
fails to bound its height and scroll its body.

### Inputs

| Input | Type | Default | Description |
|-------|------|---------|-------------|
| `width` | `string` | `'560px'` | Desired width; shrinks if it does not fit the viewport |
| `maxHeight` | `string` | `'85dvh'` | Height cap. `dvh` keeps the mobile browser chrome from covering the footer |
| `bodyLayout` | `'scroll'` or `'flex'` | `'scroll'` | `flex` turns the body into a flex column so the content can decide what stays pinned and what scrolls (see below) |
| `footerAlign` | `'end'` or `'between'` | `'end'` | `between` for footers that separate a destructive action from the rest |
| `bodyFocusable` | `boolean` | `false` | Makes the body focusable so it can be scrolled with the keyboard. Only applies in `scroll` mode, and only needed in read-only dialogs — when the body has form fields, tabbing through them already scrolls it |

### Content Slots

Project one element per slot, marked with the matching attribute:
`uiDialogHeader`, `uiDialogBody`, `uiDialogFooter`.

### Usage Example

Dialogs with a form must wrap the whole component, **not** the body — otherwise
the footer's `type="submit"` buttons fall outside the `<form>` and saving
silently stops working:

```html
<form #templateForm="ngForm" (ngSubmit)="onSubmit()">
  <ui-dialog width="560px">

    <div uiDialogHeader class="flex items-start justify-between">
      <h2 class="text-lg font-semibold">Edit template</h2>
      <ui-icon-button [icon]="faXmark" ariaLabel="Close" (clicked)="onCancel()" />
    </div>

    <div uiDialogBody class="space-y-5">
      <!-- form fields -->
    </div>

    <div uiDialogFooter class="flex items-center gap-3">
      <ui-button variant="secondary" type="button" (clicked)="onCancel()">Cancel</ui-button>
      <ui-button type="submit" [disabled]="!isFormValid">Save changes</ui-button>
    </div>

  </ui-dialog>
</form>
```

### Pinning part of the body (`bodyLayout="flex"`)

By default the whole body scrolls. Set `bodyLayout="flex"` when part of the
body should stay pinned while an inner region scrolls on its own — for
example metadata above a payload viewer. The projected body element becomes
the flex column:

```html
<ui-dialog width="640px" bodyLayout="flex">
  <div uiDialogBody class="flex-1 min-h-0 flex flex-col">
    <div class="grid grid-cols-2 gap-3 mb-4 shrink-0"><!-- metadata pinned --></div>
    <pre tabindex="0" class="flex-1 min-h-[8rem] overflow-auto"><!-- scrolls --></pre>
  </div>
</ui-dialog>
```

Three rules make it behave:

- Mark everything that stays pinned as `shrink-0`, or it gets squeezed.
- Give the scrolling child a `min-h`. Without it, pinned content eats the
  available height and the scroller collapses to a line or two on short
  screens — measured at 1280x420, an 8rem floor is the difference between a
  usable viewer and 32px of it.
- Put `tabindex="0"` on the scrolling child, not on the body. In this mode the
  body is not what scrolls, so `bodyFocusable` would only add a dead focus stop.

The body keeps its own `overflow-y-auto` as a backstop: if those `min-h`
floors do not fit, the body scrolls instead of clipping them.

### Notes

- `MAT_DIALOG_DEFAULT_OPTIONS` in `app.config.ts` caps every dialog at
  `90dvh` / `95vw` as a safety net, so a dialog that forgets the shell still
  cannot grow past the viewport.
- `UiConfirmDialog` does not use the shell: it is a centered alert card with no
  header bar. It applies the same principle on its own.

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
