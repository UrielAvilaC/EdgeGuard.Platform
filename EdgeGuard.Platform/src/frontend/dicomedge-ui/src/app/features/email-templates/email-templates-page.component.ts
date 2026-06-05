import { ChangeDetectionStrategy, Component, ElementRef, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';

import { ApiClient } from '../../core/api/api-client';
import { API_ROUTES } from '../../core/api/api-routes';
import { ToastService } from '../../core/services/toast.service';

interface EmailTemplate {
  id: string;
  name: string;
  format: 'Html' | 'PlainText';
  subject: string;
  body: string;
  isActive: boolean;
}
interface TemplateTag { tag: string; description: string; example: string; }
interface TemplateForm { id: string | null; name: string; format: 'Html' | 'PlainText'; subject: string; body: string; isActive: boolean; }
interface Preview { subject: string; body: string; }

const EMPTY_FORM: TemplateForm = { id: null, name: '', format: 'Html', subject: '', body: '', isActive: true };

/**
 * Enterprise Email template editor: template list + editor with a predefined merge-tag
 * palette (click/insert at cursor), HTML ↔ plain-text toggle and live server-side preview.
 */
@Component({
  selector: 'app-email-templates-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule],
  template: `
    <div class="p-6 space-y-6">
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-xl font-semibold text-gray-900 dark:text-white">Plantillas de Email</h1>
          <p class="text-sm text-gray-500">Editor con etiquetas predefinidas y previsualización.</p>
        </div>
        <button class="px-3 py-2 text-sm rounded-lg bg-blue-600 text-white hover:bg-blue-700" (click)="newTemplate()">Nueva plantilla</button>
      </div>

      <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <!-- List -->
        <div class="space-y-2">
          @for (t of templates(); track t.id) {
            <button class="w-full text-left p-3 rounded-lg border border-gray-200 dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-700/40"
              [class.ring-2]="form().id === t.id" [class.ring-blue-500]="form().id === t.id" (click)="edit(t)">
              <div class="flex items-center justify-between">
                <span class="font-medium text-gray-900 dark:text-white">{{ t.name }}</span>
                <span class="text-xs px-2 py-0.5 rounded bg-gray-100 dark:bg-gray-700">{{ t.format }}</span>
              </div>
              <div class="text-xs text-gray-500 truncate">{{ t.subject }}</div>
            </button>
          }
          @if (templates().length === 0) {
            <p class="text-sm text-gray-400 italic">Sin plantillas todavía.</p>
          }
        </div>

        <!-- Editor -->
        <div class="lg:col-span-2 space-y-4">
          <div class="grid grid-cols-1 md:grid-cols-2 gap-3">
            <label class="text-sm">Nombre
              <input class="mt-1 w-full border rounded-lg px-3 py-2 dark:bg-gray-700 dark:border-gray-600" [(ngModel)]="form().name" name="name" />
            </label>
            <label class="text-sm">Formato
              <select class="mt-1 w-full border rounded-lg px-3 py-2 dark:bg-gray-700 dark:border-gray-600" [(ngModel)]="form().format" name="format">
                <option value="Html">HTML</option>
                <option value="PlainText">Texto plano</option>
              </select>
            </label>
          </div>
          <label class="text-sm block">Asunto
            <input class="mt-1 w-full border rounded-lg px-3 py-2 dark:bg-gray-700 dark:border-gray-600" [(ngModel)]="form().subject" name="subject" />
          </label>

          <!-- Tag palette -->
          <div class="flex flex-wrap gap-1.5">
            @for (tag of tags(); track tag.tag) {
              <button class="text-xs px-2 py-1 rounded-full bg-blue-50 text-blue-700 dark:bg-blue-900/30 dark:text-blue-300 hover:bg-blue-100"
                [title]="tag.description + ' — ej: ' + tag.example" (click)="insertTag(tag.tag)">{{ '{{' + tag.tag + '}}' }}</button>
            }
          </div>

          <label class="text-sm block">Cuerpo ({{ form().format === 'Html' ? 'HTML' : 'texto' }})
            <textarea #bodyArea rows="10" class="mt-1 w-full font-mono text-sm border rounded-lg px-3 py-2 dark:bg-gray-700 dark:border-gray-600"
              [(ngModel)]="form().body" name="body"></textarea>
          </label>

          <div class="flex items-center gap-2">
            <button class="px-3 py-2 text-sm rounded-lg bg-emerald-600 text-white hover:bg-emerald-700" (click)="save()">Guardar</button>
            <button class="px-3 py-2 text-sm rounded-lg border dark:border-gray-600" (click)="preview()">Previsualizar</button>
            @if (form().id) {
              <button class="px-3 py-2 text-sm rounded-lg text-red-600 hover:bg-red-50" (click)="remove()">Eliminar</button>
            }
          </div>

          @if (previewResult(); as p) {
            <div class="rounded-lg border border-gray-200 dark:border-gray-700 p-4 space-y-2">
              <div class="text-xs font-medium text-gray-500">Previsualización (datos de ejemplo)</div>
              <div class="font-medium text-gray-900 dark:text-white">{{ p.subject }}</div>
              @if (form().format === 'Html') {
                <div class="prose prose-sm dark:prose-invert max-w-none" [innerHTML]="previewHtml()"></div>
              } @else {
                <pre class="whitespace-pre-wrap text-sm">{{ p.body }}</pre>
              }
            </div>
          }
        </div>
      </div>
    </div>
  `,
})
export default class EmailTemplatesPage {
  private readonly api = inject(ApiClient);
  private readonly toast = inject(ToastService);
  private readonly sanitizer = inject(DomSanitizer);

  private readonly bodyArea = viewChild<ElementRef<HTMLTextAreaElement>>('bodyArea');

  protected readonly templates = signal<EmailTemplate[]>([]);
  protected readonly tags = signal<TemplateTag[]>([]);
  protected readonly form = signal<TemplateForm>({ ...EMPTY_FORM });
  protected readonly previewResult = signal<Preview | null>(null);
  protected readonly previewHtml = signal<SafeHtml | null>(null);

  constructor() {
    this.load();
    this.api.get<TemplateTag[]>(API_ROUTES.NOTIFICATION_TEMPLATES.TAGS).subscribe((t) => this.tags.set(t));
  }

  private load(): void {
    this.api.get<EmailTemplate[]>(API_ROUTES.NOTIFICATION_TEMPLATES.LIST).subscribe((t) => this.templates.set(t));
  }

  protected newTemplate(): void {
    this.form.set({ ...EMPTY_FORM });
    this.previewResult.set(null);
  }

  protected edit(t: EmailTemplate): void {
    this.form.set({ id: t.id, name: t.name, format: t.format, subject: t.subject, body: t.body, isActive: t.isActive });
    this.previewResult.set(null);
  }

  protected insertTag(tag: string): void {
    const token = `{{${tag}}}`;
    const el = this.bodyArea()?.nativeElement;
    const f = this.form();
    if (el) {
      const start = el.selectionStart ?? f.body.length;
      const end = el.selectionEnd ?? f.body.length;
      this.form.set({ ...f, body: f.body.slice(0, start) + token + f.body.slice(end) });
    } else {
      this.form.set({ ...f, body: f.body + token });
    }
  }

  protected save(): void {
    const f = this.form();
    const body = { name: f.name, format: f.format, subject: f.subject, body: f.body, isActive: f.isActive };
    const req = f.id
      ? this.api.put<EmailTemplate>(API_ROUTES.NOTIFICATION_TEMPLATES.BY_ID(f.id), body)
      : this.api.post<EmailTemplate>(API_ROUTES.NOTIFICATION_TEMPLATES.LIST, body);
    req.subscribe({
      next: (saved) => { this.toast.success('Plantilla guardada'); this.load(); this.edit(saved); },
      error: () => this.toast.error('No se pudo guardar la plantilla'),
    });
  }

  protected remove(): void {
    const id = this.form().id;
    if (!id) return;
    this.api.delete(API_ROUTES.NOTIFICATION_TEMPLATES.BY_ID(id)).subscribe({
      next: () => { this.toast.success('Plantilla eliminada'); this.newTemplate(); this.load(); },
      error: () => this.toast.error('No se pudo eliminar'),
    });
  }

  protected preview(): void {
    const f = this.form();
    this.api.post<Preview>(API_ROUTES.NOTIFICATION_TEMPLATES.PREVIEW, { format: f.format, subject: f.subject, body: f.body })
      .subscribe({
        next: (p) => {
          this.previewResult.set(p);
          this.previewHtml.set(f.format === 'Html' ? this.sanitizer.bypassSecurityTrustHtml(p.body) : null);
        },
        error: () => this.toast.error('No se pudo previsualizar'),
      });
  }
}
