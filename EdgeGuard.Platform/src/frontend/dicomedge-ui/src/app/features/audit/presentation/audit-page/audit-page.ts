import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-audit-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="space-y-6">
      <h1 class="text-2xl font-bold text-gray-900 dark:text-white">Auditoría</h1>
      <p class="text-gray-500 dark:text-gray-400">Registros de auditoría — próximamente.</p>
    </div>
  `,
})
export default class AuditPage {}
