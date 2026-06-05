import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { ApiClient } from '../../core/api/api-client';
import { API_ROUTES } from '../../core/api/api-routes';
import { ToastService } from '../../core/services/toast.service';

interface NotificationSettings {
  autoMode: boolean;
  smtpEnabled: boolean;
  smtpHost: string;
  smtpFrom: string;
  smtpPort: number;
}

/** Hub notification settings: auto-mode master switch + SMTP status and test send. */
@Component({
  selector: 'app-notification-settings-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule],
  template: `
    <div class="p-6 space-y-6 max-w-3xl">
      <h1 class="text-xl font-semibold text-gray-900 dark:text-white">Notificaciones</h1>

      @if (settings(); as s) {
        <!-- Auto-mode -->
        <div class="rounded-xl border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 p-5 space-y-2">
          <div class="flex items-center justify-between">
            <div>
              <div class="font-medium text-gray-900 dark:text-white">Modo automático</div>
              <p class="text-sm text-gray-500">Entrega automática de resultados al finalizar un estudio.</p>
            </div>
            <label class="inline-flex items-center cursor-pointer">
              <input type="checkbox" class="sr-only peer" [ngModel]="s.autoMode" (ngModelChange)="setAutoMode($event)" />
              <div class="relative w-11 h-6 bg-gray-200 peer-checked:bg-blue-600 rounded-full after:content-[''] after:absolute after:top-0.5 after:left-0.5 after:bg-white after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:after:translate-x-5"></div>
            </label>
          </div>
        </div>

        <!-- SMTP -->
        <div class="rounded-xl border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 p-5 space-y-3">
          <div class="flex items-center justify-between">
            <div class="font-medium text-gray-900 dark:text-white">SMTP (Email)</div>
            <span class="text-xs px-2 py-0.5 rounded-full"
              [class.bg-emerald-100]="s.smtpEnabled" [class.text-emerald-700]="s.smtpEnabled"
              [class.bg-gray-100]="!s.smtpEnabled" [class.text-gray-500]="!s.smtpEnabled">
              {{ s.smtpEnabled ? 'Habilitado' : 'Deshabilitado' }}
            </span>
          </div>
          <div class="text-sm text-gray-600 dark:text-gray-300 space-y-1">
            <div>Host: <span class="font-mono">{{ s.smtpHost }}:{{ s.smtpPort }}</span></div>
            <div>From: <span class="font-mono">{{ s.smtpFrom }}</span></div>
            <p class="text-xs text-gray-400">La configuración SMTP (host/credenciales) se define en <code>appsettings</code> del Hub.</p>
          </div>
          <div class="flex items-center gap-2 pt-1">
            <input class="flex-1 text-sm border rounded-lg px-3 py-2 dark:bg-gray-700 dark:border-gray-600"
              [(ngModel)]="testEmail" placeholder="correo-de-prueba@dominio.com" />
            <button class="px-3 py-2 text-sm rounded-lg bg-blue-600 text-white hover:bg-blue-700 disabled:opacity-50"
              [disabled]="!s.smtpEnabled || testing()" (click)="testSmtp()">Enviar prueba</button>
          </div>
        </div>
      }
    </div>
  `,
})
export default class NotificationSettingsPage {
  private readonly api = inject(ApiClient);
  private readonly toast = inject(ToastService);

  protected readonly settings = signal<NotificationSettings | null>(null);
  protected readonly testing = signal(false);
  protected testEmail = '';

  constructor() {
    this.load();
  }

  private load(): void {
    this.api.get<NotificationSettings>(API_ROUTES.NOTIFICATION_SETTINGS.GET).subscribe((s) => this.settings.set(s));
  }

  protected setAutoMode(value: boolean): void {
    this.api.put(API_ROUTES.NOTIFICATION_SETTINGS.AUTO_MODE, { autoMode: value }).subscribe({
      next: () => { this.toast.success('Modo automático ' + (value ? 'activado' : 'desactivado')); this.load(); },
      error: () => this.toast.error('No se pudo actualizar'),
    });
  }

  protected testSmtp(): void {
    if (!this.testEmail.trim()) { this.toast.error('Ingresa un correo'); return; }
    this.testing.set(true);
    this.api.post<{ success: boolean; error: string | null }>(API_ROUTES.NOTIFICATION_SETTINGS.SMTP_TEST, { toEmail: this.testEmail.trim() })
      .subscribe({
        next: (r) => {
          this.testing.set(false);
          if (r.success) this.toast.success('Correo de prueba enviado');
          else this.toast.error('Falló: ' + (r.error ?? 'error'));
        },
        error: () => { this.testing.set(false); this.toast.error('No se pudo enviar la prueba'); },
      });
  }
}
