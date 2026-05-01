import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faSync, faServer } from '@fortawesome/free-solid-svg-icons';

import { UiPageHeader } from '../../../../shared/components/ui-page-header/ui-page-header.component';
import { UiButton } from '../../../../shared/components/ui-button/ui-button.component';
import { UiAlert } from '../../../../shared/components/ui-alert/ui-alert.component';
import { ToastService } from '../../../../core/services/toast.service';
import { SystemSettingsApiService } from '../../infrastructure/system-settings-api.service';
import { SystemSettingDto, SETTING_CATEGORIES } from '../../models/settings.models';
import { SettingCategoryPanel } from '../setting-category-panel/setting-category-panel.component';

@Component({
  selector: 'app-settings-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink, FontAwesomeModule, UiPageHeader, UiButton,
    UiAlert, SettingCategoryPanel,
  ],
  templateUrl: './settings-page.component.html',
  styleUrl: './settings-page.component.scss'
})
export default class SettingsPage {
  private readonly api = inject(SystemSettingsApiService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly faSync = faSync;
  protected readonly faServer = faServer;

  protected readonly allSettings = signal<SystemSettingDto[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly categories = SETTING_CATEGORIES;

  constructor() {
    this.loadSettings();
  }

  protected loadSettings(): void {
    this.loading.set(true);
    this.api.getAll().pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: settings => {
        this.allSettings.set(settings);
        this.loading.set(false);
        this.error.set(null);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Error al cargar la configuración');
      },
    });
  }

  protected settingsByCategory(category: string): SystemSettingDto[] {
    return this.allSettings().filter(s => s.category === category);
  }

  protected onSettingUpdated(event: { key: string; value: string }): void {
    this.api.update(event.key, { value: event.value }).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.toast.success('Configuración actualizada');
        this.allSettings.update(list =>
          list.map(s => s.key === event.key ? { ...s, value: event.value } : s),
        );
      },
      error: () => this.toast.error('Error al actualizar la configuración'),
    });
  }
}
