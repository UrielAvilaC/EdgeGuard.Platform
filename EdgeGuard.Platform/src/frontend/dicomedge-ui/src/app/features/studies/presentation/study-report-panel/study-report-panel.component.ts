import { ChangeDetectionStrategy, Component, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DomSanitizer, SafeHtml, SafeUrl } from '@angular/platform-browser';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faFilePdf, faImage, faQrcode, faPaperPlane } from '@fortawesome/free-solid-svg-icons';

import { input } from '@angular/core';
import { StudiesApiService, DeliveryHistory } from '../../infrastructure/studies-api.service';
import { StudyReport } from '../../models/study.models';
import { ToastService } from '../../../../core/services/toast.service';

/**
 * Read-only panel showing the diagnostic report (sanitized HTML/text), the QR code of
 * the image link, the image links, and a button to open the report PDF. PDF/QR are
 * fetched as authenticated blobs (object URLs) since &lt;img&gt;/&lt;iframe&gt; can't carry the bearer token.
 */
@Component({
  selector: 'app-study-report-panel',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FontAwesomeModule, FormsModule],
  template: `
    @if (report(); as r) {
      @if (r.content || r.hasPdf || r.imageLinks.length > 0) {
        <div class="rounded-xl border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 p-5 space-y-4">
          <div class="flex items-center justify-between">
            <h3 class="font-semibold text-gray-900 dark:text-white">Resultados (reporte e imágenes)</h3>
            @if (r.hasPdf) {
              <button type="button" (click)="openPdf()"
                class="inline-flex items-center gap-2 text-sm px-3 py-1.5 rounded-lg bg-red-600 text-white hover:bg-red-700">
                <fa-icon [icon]="faFilePdf" /> Ver PDF
              </button>
            }
          </div>

          <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div class="md:col-span-2 space-y-3">
              @if (safeHtml(); as html) {
                <div class="prose prose-sm dark:prose-invert max-w-none" [innerHTML]="html"></div>
              } @else if (r.reportFormat === 'PlainText' && r.content) {
                <pre class="whitespace-pre-wrap text-sm text-gray-800 dark:text-gray-200 font-sans">{{ r.content }}</pre>
              } @else if (!r.content) {
                <p class="text-sm text-gray-400 italic">Sin reporte textual.</p>
              }

              @if (r.imageLinks.length > 0) {
                <div class="space-y-1">
                  <div class="text-xs font-medium text-gray-500 flex items-center gap-1.5">
                    <fa-icon [icon]="faImage" /> Ligas de imágenes
                  </div>
                  @for (link of r.imageLinks; track link) {
                    <a [href]="link" target="_blank" rel="noopener"
                      class="block text-sm text-blue-600 dark:text-blue-400 hover:underline truncate">{{ link }}</a>
                  }
                </div>
              }
            </div>

            @if (qrUrl(); as qr) {
              <div class="flex flex-col items-center justify-start gap-1">
                <div class="text-xs font-medium text-gray-500 flex items-center gap-1.5">
                  <fa-icon [icon]="faQrcode" /> QR liga
                </div>
                <img [src]="qr" alt="QR de la liga de imágenes" class="w-32 h-32 rounded border border-gray-200 dark:border-gray-700" />
              </div>
            }
          </div>

          <!-- Entregar resultados -->
          <div class="border-t border-gray-100 dark:border-gray-700 pt-3 space-y-3">
            <button type="button" (click)="toggleDeliver()"
              class="inline-flex items-center gap-2 text-sm px-3 py-1.5 rounded-lg border dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-700/40">
              <fa-icon [icon]="faPaperPlane" /> Entregar resultados
            </button>

            @if (showDeliver()) {
              <div class="grid grid-cols-1 md:grid-cols-2 gap-3 text-sm">
                <label class="md:col-span-2">Plantilla de email
                  <select class="mt-1 w-full border rounded-lg px-3 py-2 dark:bg-gray-700 dark:border-gray-600" [(ngModel)]="emailTemplateId">
                    <option [ngValue]="null">— seleccionar —</option>
                    @for (t of emailTemplates(); track t.id) { <option [ngValue]="t.id">{{ t.name }}</option> }
                  </select>
                </label>
                <label class="md:col-span-2">Destinatarios email (separados por coma)
                  <input class="mt-1 w-full border rounded-lg px-3 py-2 dark:bg-gray-700 dark:border-gray-600" [(ngModel)]="emails" placeholder="paciente@correo.com" />
                </label>
                <label class="flex items-center gap-2"><input type="checkbox" [(ngModel)]="includeQr" /> Incluir QR</label>
                @if (r.hasPdf) {
                  <label class="flex items-center gap-2"><input type="checkbox" [(ngModel)]="attachPdf" /> Adjuntar PDF</label>
                }
                <div class="md:col-span-2">
                  <button type="button" (click)="send()" [disabled]="sending()"
                    class="px-3 py-2 text-sm rounded-lg bg-blue-600 text-white hover:bg-blue-700 disabled:opacity-50">Enviar</button>
                </div>
              </div>

              @if (deliveries().length > 0) {
                <div class="space-y-1">
                  <div class="text-xs font-medium text-gray-500">Historial de entregas</div>
                  @for (d of deliveries(); track d.id) {
                    <div class="text-xs flex items-center gap-2">
                      <span class="px-1.5 py-0.5 rounded bg-gray-100 dark:bg-gray-700">{{ d.channel }}</span>
                      <span class="text-gray-600 dark:text-gray-300 truncate">{{ d.to }}</span>
                      <span [class.text-emerald-600]="d.status === 'Sent'" [class.text-red-500]="d.status === 'Failed'">{{ d.status }}</span>
                    </div>
                  }
                </div>
              }
            }
          </div>
        </div>
      }
    }
  `,
})
export class StudyReportPanel {
  private readonly api = inject(StudiesApiService);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly toast = inject(ToastService);

  readonly studyId = input.required<string>();

  protected readonly faFilePdf = faFilePdf;
  protected readonly faImage = faImage;
  protected readonly faQrcode = faQrcode;
  protected readonly faPaperPlane = faPaperPlane;

  protected readonly report = signal<StudyReport | null>(null);
  protected readonly safeHtml = signal<SafeHtml | null>(null);
  protected readonly qrUrl = signal<SafeUrl | null>(null);

  // Delivery
  protected readonly showDeliver = signal(false);
  protected readonly emailTemplates = signal<{ id: string; name: string }[]>([]);
  protected readonly deliveries = signal<DeliveryHistory[]>([]);
  protected readonly sending = signal(false);
  protected emailTemplateId: string | null = null;
  protected emails = '';
  protected includeQr = true;
  protected attachPdf = false;

  protected toggleDeliver(): void {
    const open = !this.showDeliver();
    this.showDeliver.set(open);
    if (open) {
      this.api.getEmailTemplates().subscribe((t) => this.emailTemplates.set(t));
      this.loadDeliveries();
    }
  }

  private loadDeliveries(): void {
    this.api.getDeliveries(this.studyId()).subscribe((d) => this.deliveries.set(d));
  }

  protected send(): void {
    const emails = this.emails.split(',').map((e) => e.trim()).filter(Boolean);
    if (!this.emailTemplateId || emails.length === 0) {
      this.toast.error('Selecciona una plantilla y al menos un destinatario');
      return;
    }
    this.sending.set(true);
    this.api.deliver(this.studyId(), {
      emails, phones: [], attachPdf: this.attachPdf, includeQr: this.includeQr, emailTemplateId: this.emailTemplateId,
    }).subscribe({
      next: (r) => { this.toast.success('Entrega encolada (' + r.enqueued + ')'); this.sending.set(false); this.loadDeliveries(); },
      error: () => { this.toast.error('No se pudo entregar'); this.sending.set(false); },
    });
  }

  constructor() {
    effect(() => {
      const id = this.studyId();
      if (id) this.load(id);
    });
  }

  private load(id: string): void {
    this.api.getReport(id).subscribe({
      next: (r) => {
        this.report.set(r);
        this.safeHtml.set(
          r.reportFormat === 'Html' && r.content
            ? this.sanitizer.bypassSecurityTrustHtml(r.content)
            : null,
        );
        if (r.imageLinks.length > 0) this.loadQr(id);
      },
      error: () => this.report.set(null),
    });
  }

  private loadQr(id: string): void {
    this.api.getReportQr(id).subscribe({
      next: (resp) => {
        if (resp.body) this.qrUrl.set(this.sanitizer.bypassSecurityTrustUrl(URL.createObjectURL(resp.body)));
      },
      error: () => { /* no QR available */ },
    });
  }

  protected openPdf(): void {
    this.api.getReportPdf(this.studyId()).subscribe((resp) => {
      if (resp.body) window.open(URL.createObjectURL(resp.body), '_blank');
    });
  }
}
