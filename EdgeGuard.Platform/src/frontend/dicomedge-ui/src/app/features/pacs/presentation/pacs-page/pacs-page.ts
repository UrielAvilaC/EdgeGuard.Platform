import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-pacs-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="space-y-6">
      <h1 class="text-2xl font-bold text-gray-900 dark:text-white">Servidores PACS</h1>
      <p class="text-gray-500 dark:text-gray-400">Gestión de servidores PACS — próximamente.</p>
    </div>
  `,
})
export default class PacsPage {}
