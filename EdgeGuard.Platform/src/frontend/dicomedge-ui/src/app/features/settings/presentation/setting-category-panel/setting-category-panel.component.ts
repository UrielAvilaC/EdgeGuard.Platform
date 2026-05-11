import { ChangeDetectionStrategy, Component, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faChevronDown, faChevronRight, faPen, faCheck, faTimes } from '@fortawesome/free-solid-svg-icons';

import { UiChip } from '../../../../shared/components/ui-chip/ui-chip.component';
import { UiIconButton } from '../../../../shared/components/ui-icon-button/ui-icon-button.component';
import { SystemSettingDto } from '../../models/settings.models';

@Component({
  selector: 'app-setting-category-panel',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, FontAwesomeModule, UiChip, UiIconButton],
  templateUrl: './setting-category-panel.component.html',
  styleUrl: './setting-category-panel.component.scss'
})
export class SettingCategoryPanel {
  readonly category = input.required<string>();
  readonly label = input.required<string>();
  readonly settings = input.required<SystemSettingDto[]>();
  readonly settingUpdated = output<{ key: string; value: string }>();

  protected readonly expanded = signal(false);
  protected readonly editingKey = signal<string | null>(null);
  protected readonly editValue = signal('');

  protected readonly faChevronDown = faChevronDown;
  protected readonly faChevronRight = faChevronRight;
  protected readonly faPen = faPen;
  protected readonly faCheck = faCheck;
  protected readonly faTimes = faTimes;

  protected startEdit(setting: SystemSettingDto): void {
    this.editingKey.set(setting.key);
    this.editValue.set(setting.value);
  }

  protected cancelEdit(): void {
    this.editingKey.set(null);
    this.editValue.set('');
  }

  protected save(setting: SystemSettingDto): void {
    const newValue = this.editValue();
    if (newValue !== setting.value) {
      this.settingUpdated.emit({ key: setting.key, value: newValue });
    }
    this.cancelEdit();
  }
}
