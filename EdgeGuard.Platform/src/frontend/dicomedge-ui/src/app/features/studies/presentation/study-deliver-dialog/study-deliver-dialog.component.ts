import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatChipInputEvent, MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { ENTER, COMMA, TAB } from '@angular/cdk/keycodes';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faPaperPlane, faXmark, faEnvelope, faCommentSms } from '@fortawesome/free-solid-svg-icons';

import { UiButton } from '../../../../shared/components/ui-button/ui-button.component';
import { UiIconButton } from '../../../../shared/components/ui-icon-button/ui-icon-button.component';
import { UiSlideToggle } from '../../../../shared/forms/slide-toggle/slide-toggle.component';
import { UiDropdown, DropdownOption } from '../../../../shared/forms/dropdown/dropdown.component';
import { ToastService } from '../../../../core/services/toast.service';
import { NotificationChannelsService } from '../../../../core/services/notification-channels.service';
import { StudiesApiService, DeliveryHistory } from '../../infrastructure/studies-api.service';

export interface StudyDeliverDialogData {
  studyId: string;
  hasPdf: boolean;
}

/** Basic email check, intentionally permissive. */
const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/;
/** E.164 phone: leading +, country code (1-9) and 7-14 more digits. */
const PHONE_RE = /^\+[1-9]\d{7,14}$/;

/**
 * Modal to deliver a study's results over email and/or WhatsApp. Only the channels that
 * are actually active on the Hub are shown (see {@link NotificationChannelsService}); if a
 * channel is inactive its recipients/template controls are hidden.
 */
@Component({
  selector: 'app-study-deliver-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    MatDialogModule,
    MatChipsModule,
    MatFormFieldModule,
    MatInputModule,
    FontAwesomeModule,
    UiButton,
    UiIconButton,
    UiSlideToggle,
    UiDropdown,
  ],
  template: `
    <div class="p-6 w-[560px] max-w-full">
      <!-- Header -->
      <div class="flex items-start justify-between mb-6">
        <div class="flex items-center gap-3">
          <div class="flex items-center justify-center w-10 h-10 rounded-lg bg-blue-100 dark:bg-blue-900/30">
            <fa-icon [icon]="faPaperPlane" class="text-blue-600 dark:text-blue-400" />
          </div>
          <div>
            <h2 class="text-lg font-semibold text-gray-900 dark:text-white">Entregar resultados</h2>
            <p class="text-sm text-gray-500 dark:text-gray-400">
              Envía el reporte y las ligas de imágenes al paciente.
            </p>
          </div>
        </div>
        <ui-icon-button [icon]="faXmark" tooltip="Cerrar" ariaLabel="Cerrar diálogo" (clicked)="onCancel()" />
      </div>

      <form class="space-y-5" (ngSubmit)="send()">
        <!-- Email channel -->
        @if (channels.emailEnabled()) {
          <div class="rounded-lg border border-gray-200 dark:border-gray-700 p-4 space-y-4 bg-gray-50/50 dark:bg-gray-800/30">
            <h3 class="text-sm font-semibold text-gray-800 dark:text-gray-200 flex items-center gap-2">
              <fa-icon [icon]="faEnvelope" class="text-gray-400" /> Email
            </h3>
            <ui-dropdown
              label="Plantilla de email"
              placeholder="— seleccionar —"
              [options]="emailTemplateOptions()"
              [(ngModel)]="emailTemplateId"
              name="emailTemplateId"
            />
            <div>
              <mat-form-field appearance="outline" class="w-full">
                <mat-label>Destinatarios (email)</mat-label>
                <mat-chip-grid #emailChips aria-label="Destinatarios de email">
                  @for (email of emails(); track email) {
                    <mat-chip-row (removed)="removeEmail(email)">
                      {{ email }}
                      <button matChipRemove [attr.aria-label]="'Quitar ' + email">
                        <fa-icon [icon]="faXmark" />
                      </button>
                    </mat-chip-row>
                  }
                  <input
                    placeholder="paciente@correo.com"
                    [matChipInputFor]="emailChips"
                    [matChipInputSeparatorKeyCodes]="separatorKeys"
                    [matChipInputAddOnBlur]="true"
                    (matChipInputTokenEnd)="addEmail($event)"
                  />
                </mat-chip-grid>
                <mat-hint>Enter o Tab para agregar cada correo.</mat-hint>
              </mat-form-field>
              @if (emailError()) {
                <p class="mt-1 text-xs text-red-500">{{ emailError() }}</p>
              }
            </div>
            @if (data.hasPdf) {
              <ui-slide-toggle label="Adjuntar PDF" [(ngModel)]="attachPdf" name="attachPdf" />
            }
          </div>
        }

        <!-- WhatsApp channel -->
        <div
          class="rounded-lg border border-gray-200 dark:border-gray-700 p-4 space-y-4 bg-gray-50/50 dark:bg-gray-800/30"
          [class.opacity-60]="!channels.whatsAppEnabled()"
        >
          <h3 class="text-sm font-semibold text-gray-800 dark:text-gray-200 flex items-center gap-2">
            <fa-icon [icon]="faCommentSms" class="text-gray-400" /> WhatsApp
            @if (!channels.whatsAppEnabled()) {
              <span class="ml-auto text-xs font-medium px-2 py-0.5 rounded-full bg-gray-200 text-gray-600 dark:bg-gray-700 dark:text-gray-300">
                No disponible
              </span>
            }
          </h3>
          <ui-dropdown
            label="Plantilla de WhatsApp"
            placeholder="— seleccionar —"
            [options]="whatsAppTemplateOptions()"
            [(ngModel)]="whatsAppTemplateId"
            name="whatsAppTemplateId"
            [disabled]="!channels.whatsAppEnabled()"
          />
          <div>
            <mat-form-field appearance="outline" class="w-full">
              <mat-label>Números (con código de país)</mat-label>
              <mat-chip-grid #phoneChips [disabled]="!channels.whatsAppEnabled()" aria-label="Números de WhatsApp">
                @for (phone of phones(); track phone) {
                  <mat-chip-row (removed)="removePhone(phone)">
                    {{ phone }}
                    <button matChipRemove [attr.aria-label]="'Quitar ' + phone">
                      <fa-icon [icon]="faXmark" />
                    </button>
                  </mat-chip-row>
                }
                <input
                  placeholder="+52155..."
                  [matChipInputFor]="phoneChips"
                  [matChipInputSeparatorKeyCodes]="separatorKeys"
                  [matChipInputAddOnBlur]="true"
                  [disabled]="!channels.whatsAppEnabled()"
                  (matChipInputTokenEnd)="addPhone($event)"
                />
              </mat-chip-grid>
              <mat-hint>
                @if (channels.whatsAppEnabled()) {
                  Enter o Tab para agregar cada número.
                } @else {
                  Canal de WhatsApp no disponible.
                }
              </mat-hint>
            </mat-form-field>
            @if (phoneError()) {
              <p class="mt-1 text-xs text-red-500">{{ phoneError() }}</p>
            }
          </div>
        </div>

        @if (!channels.anyEnabled()) {
          <p class="text-sm text-gray-500 dark:text-gray-400">
            No hay canales de entrega activos.
          </p>
        }

        <!-- Delivery history -->
        @if (deliveries().length > 0) {
          <div class="space-y-1.5 pt-1">
            <div class="text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider">
              Historial de entregas
            </div>
            @for (d of deliveries(); track d.id) {
              <div class="text-xs flex items-center gap-2">
                <span class="px-1.5 py-0.5 rounded bg-gray-100 dark:bg-gray-700">{{ d.channel }}</span>
                <span class="text-gray-600 dark:text-gray-300 truncate flex-1">{{ d.to }}</span>
                <span
                  [class.text-emerald-600]="d.status === 'Sent'"
                  [class.text-red-500]="d.status === 'Failed'"
                >{{ d.status }}</span>
              </div>
            }
          </div>
        }

        <div class="flex justify-end gap-3 pt-4 border-t border-gray-200 dark:border-gray-700">
          <ui-button variant="secondary" type="button" (clicked)="onCancel()">Cerrar</ui-button>
          <ui-button type="submit" [icon]="faPaperPlane" [disabled]="sending() || !channels.anyEnabled()">
            Enviar
          </ui-button>
        </div>
      </form>
    </div>
  `,
})
export class StudyDeliverDialog {
  private readonly dialogRef = inject(MatDialogRef<StudyDeliverDialog>);
  protected readonly data: StudyDeliverDialogData = inject(MAT_DIALOG_DATA);
  private readonly api = inject(StudiesApiService);
  private readonly toast = inject(ToastService);
  protected readonly channels = inject(NotificationChannelsService);

  protected readonly faPaperPlane = faPaperPlane;
  protected readonly faXmark = faXmark;
  protected readonly faEnvelope = faEnvelope;
  protected readonly faCommentSms = faCommentSms;

  protected readonly sending = signal(false);
  protected readonly deliveries = signal<DeliveryHistory[]>([]);

  private readonly emailTemplates = signal<{ id: string; name: string }[]>([]);
  private readonly whatsAppTemplates = signal<{ id: string; name: string }[]>([]);

  protected readonly emailTemplateOptions = computed<DropdownOption<string>[]>(() =>
    this.emailTemplates().map((t) => ({ value: t.id, label: t.name })),
  );
  protected readonly whatsAppTemplateOptions = computed<DropdownOption<string>[]>(() =>
    this.whatsAppTemplates().map((t) => ({ value: t.id, label: t.name })),
  );

  protected readonly separatorKeys = [ENTER, TAB, COMMA];

  protected emailTemplateId: string | null = null;
  protected whatsAppTemplateId: string | null = null;
  protected readonly emails = signal<string[]>([]);
  protected readonly phones = signal<string[]>([]);
  protected readonly emailError = signal<string | null>(null);
  protected readonly phoneError = signal<string | null>(null);
  protected attachPdf = false;

  constructor() {
    // Re-check channel enablement on open so a channel toggled server-side
    // (e.g. WhatsApp enabled in settings/DB) is reflected without a full reload.
    this.channels.refresh().subscribe(() => {
      if (this.channels.emailEnabled()) {
        this.api.getEmailTemplates().subscribe((t) => this.emailTemplates.set(t));
      }
      if (this.channels.whatsAppEnabled()) {
        this.api.getWhatsAppTemplates().subscribe((t) => this.whatsAppTemplates.set(t));
      }
    });
    this.loadDeliveries();
  }

  private loadDeliveries(): void {
    this.api.getDeliveries(this.data.studyId).subscribe((d) => this.deliveries.set(d));
  }

  protected addEmail(event: MatChipInputEvent): void {
    const value = (event.value || '').trim();
    if (!value) { event.chipInput?.clear(); return; }
    if (!EMAIL_RE.test(value)) {
      this.emailError.set(`"${value}" no es un correo válido`);
      return;
    }
    if (!this.emails().includes(value)) this.emails.update((list) => [...list, value]);
    this.emailError.set(null);
    event.chipInput?.clear();
  }

  protected removeEmail(email: string): void {
    this.emails.update((list) => list.filter((e) => e !== email));
  }

  protected addPhone(event: MatChipInputEvent): void {
    const value = (event.value || '').trim().replace(/[\s-]/g, '');
    if (!value) { event.chipInput?.clear(); return; }
    if (!PHONE_RE.test(value)) {
      this.phoneError.set(`"${value}" no es un teléfono válido (formato +<código país>...)`);
      return;
    }
    if (!this.phones().includes(value)) this.phones.update((list) => [...list, value]);
    this.phoneError.set(null);
    event.chipInput?.clear();
  }

  protected removePhone(phone: string): void {
    this.phones.update((list) => list.filter((p) => p !== phone));
  }

  protected send(): void {
    const emails = this.emails();
    const phones = this.phones();

    const sendEmail = this.channels.emailEnabled() && !!this.emailTemplateId && emails.length > 0;
    const sendWhatsApp = this.channels.whatsAppEnabled() && !!this.whatsAppTemplateId && phones.length > 0;

    if (!sendEmail && !sendWhatsApp) {
      this.toast.error('Selecciona una plantilla y al menos un destinatario en algún canal');
      return;
    }

    this.sending.set(true);
    this.api.deliver(this.data.studyId, {
      emails: sendEmail ? emails : [],
      phones: sendWhatsApp ? phones : [],
      attachPdf: this.attachPdf,
      includeQr: true,
      emailTemplateId: sendEmail ? this.emailTemplateId : null,
      whatsAppTemplateId: sendWhatsApp ? this.whatsAppTemplateId : null,
    }).subscribe({
      next: (r) => {
        this.toast.success('Entrega encolada (' + r.enqueued + ')');
        this.sending.set(false);
        this.loadDeliveries();
      },
      error: () => {
        this.toast.error('No se pudo entregar');
        this.sending.set(false);
      },
    });
  }

  protected onCancel(): void {
    this.dialogRef.close();
  }
}
