import { ChangeDetectionStrategy, Component, effect, inject, input, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { DomSanitizer, SafeHtml, SafeUrl } from '@angular/platform-browser';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faFilePdf, faQrcode, faPaperPlane } from '@fortawesome/free-solid-svg-icons';

import { UiButton } from '../../../../shared/components/ui-button/ui-button.component';
import { StudiesApiService } from '../../infrastructure/studies-api.service';
import { StudyReport } from '../../models/study.models';
import { NotificationChannelsService } from '../../../../core/services/notification-channels.service';
import { StudyDeliverDialog, StudyDeliverDialogData } from '../study-deliver-dialog/study-deliver-dialog.component';

/**
 * Read-only panel showing the diagnostic report (sanitized HTML/text), the QR code of
 * the image link, the image links, and a button to open the report PDF. PDF/QR are
 * fetched as authenticated blobs (object URLs) since &lt;img&gt;/&lt;iframe&gt; can't carry the bearer token.
 * Results delivery is handled in a separate modal ({@link StudyDeliverDialog}), shown only
 * when at least one delivery channel (email / WhatsApp) is active.
 */
@Component({
  selector: 'app-study-report-panel',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatCardModule, FontAwesomeModule, UiButton],
  template: `
    @if (report(); as r) {
      @if (r.content || r.hasPdf || r.imageLinks.length > 0) {
        <mat-card class="!p-6 space-y-4">
          <div class="flex items-center justify-between gap-3">
            <h3 class="text-lg font-semibold text-gray-900 dark:text-white">Resultados (reporte e imágenes)</h3>
            <div class="flex items-center gap-2">
              @if (r.hasPdf) {
                <ui-button variant="ghost" size="sm" [icon]="faFilePdf" (clicked)="openPdf()">Ver PDF</ui-button>
              }
              @if (channels.anyEnabled()) {
                <ui-button size="sm" [icon]="faPaperPlane" (clicked)="openDeliverDialog()">Entregar resultados</ui-button>
              }
            </div>
          </div>

          <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div class="md:col-span-2 space-y-3">
              @if (safeHtml(); as html) {
                <div class="prose prose-sm dark:prose-invert max-w-none text-justify" [innerHTML]="html"></div>
              } @else if (r.reportFormat === 'PlainText' && r.content) {
                <pre class="whitespace-pre-wrap text-sm text-gray-800 dark:text-gray-200 font-sans text-justify">{{ r.content }}</pre>
              } @else if (!r.content) {
                <p class="text-sm text-gray-400 italic">Sin reporte textual.</p>
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
        </mat-card>
      }
    }
  `,
})
export class StudyReportPanel {
  private readonly api = inject(StudiesApiService);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly dialog = inject(MatDialog);
  protected readonly channels = inject(NotificationChannelsService);

  readonly studyId = input.required<string>();

  protected readonly faFilePdf = faFilePdf;
  protected readonly faQrcode = faQrcode;
  protected readonly faPaperPlane = faPaperPlane;

  protected readonly report = signal<StudyReport | null>(null);
  protected readonly safeHtml = signal<SafeHtml | null>(null);
  protected readonly qrUrl = signal<SafeUrl | null>(null);

  constructor() {
    this.channels.refresh().subscribe();
    effect(() => {
      const id = this.studyId();
      if (id) this.load(id);
    });
  }

  protected openDeliverDialog(): void {
    this.dialog.open(StudyDeliverDialog, {
      data: { studyId: this.studyId(), hasPdf: this.report()?.hasPdf ?? false } satisfies StudyDeliverDialogData,
      autoFocus: false,
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
