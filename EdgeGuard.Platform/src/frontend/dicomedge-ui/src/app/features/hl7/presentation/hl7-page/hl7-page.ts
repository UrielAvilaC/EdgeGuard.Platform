import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-hl7-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="space-y-6">
      <h1 class="text-2xl font-bold text-gray-900 dark:text-white">Estado HL7</h1>
      <p class="text-gray-500 dark:text-gray-400">Monitoreo de mensajes HL7 — próximamente.</p>
    </div>
  `,
})
export default class Hl7Page {}
