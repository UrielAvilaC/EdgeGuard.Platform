import { ChangeDetectionStrategy, Component, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faChevronDown, faChevronRight, faPen, faCheck, faTimes, faPlus, faTrash } from '@fortawesome/free-solid-svg-icons';

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
  protected readonly editItems = signal<string[]>([]);
  protected readonly newItemValue = signal('');
  protected readonly jsonError = signal<string | null>(null);

  protected readonly faChevronDown = faChevronDown;
  protected readonly faChevronRight = faChevronRight;
  protected readonly faPen = faPen;
  protected readonly faCheck = faCheck;
  protected readonly faTimes = faTimes;
  protected readonly faPlus = faPlus;
  protected readonly faTrash = faTrash;

  protected isJsonArray(valueType: string): boolean {
    const t = valueType.toLowerCase();
    return t === 'jsonarray' || t === 'json_array';
  }

  protected isJson(valueType: string): boolean {
    return valueType.toLowerCase() === 'json';
  }

  protected displayValue(setting: SystemSettingDto): string {
    if (this.isJsonArray(setting.valueType)) {
      try {
        const arr = JSON.parse(setting.value || '[]') as unknown[];
        if (Array.isArray(arr)) {
          if (arr.length === 0) return '(vacío)';
          const preview = arr.slice(0, 3).map(String);
          return arr.length > 3 ? `${preview.join(', ')} … (+${arr.length - 3})` : preview.join(', ');
        }
      } catch { /* fall through */ }
    }
    if (this.isJson(setting.valueType)) return '{JSON}';
    return setting.value;
  }

  protected startEdit(setting: SystemSettingDto): void {
    this.editingKey.set(setting.key);
    this.jsonError.set(null);
    if (this.isJsonArray(setting.valueType)) {
      try {
        const arr = JSON.parse(setting.value || '[]') as unknown[];
        this.editItems.set(Array.isArray(arr) ? arr.map(String) : []);
      } catch {
        this.editItems.set([]);
      }
      this.newItemValue.set('');
    } else if (this.isJson(setting.valueType)) {
      try {
        this.editValue.set(JSON.stringify(JSON.parse(setting.value), null, 2));
      } catch {
        this.editValue.set(setting.value);
      }
    } else {
      this.editValue.set(setting.value);
    }
  }

  protected cancelEdit(): void {
    this.editingKey.set(null);
    this.editValue.set('');
    this.editItems.set([]);
    this.newItemValue.set('');
    this.jsonError.set(null);
  }

  protected save(setting: SystemSettingDto): void {
    let newValue: string;
    if (this.isJsonArray(setting.valueType)) {
      newValue = JSON.stringify(this.editItems());
    } else if (this.isJson(setting.valueType)) {
      try {
        newValue = JSON.stringify(JSON.parse(this.editValue()));
        this.jsonError.set(null);
      } catch (e) {
        this.jsonError.set('JSON inválido: ' + (e as Error).message);
        return;
      }
    } else {
      newValue = this.editValue();
    }
    if (newValue !== setting.value) {
      this.settingUpdated.emit({ key: setting.key, value: newValue });
    }
    this.cancelEdit();
  }

  protected addEditItem(): void {
    const item = this.newItemValue().trim();
    if (!item) return;
    this.editItems.update(items => [...items, item]);
    this.newItemValue.set('');
  }

  protected removeEditItem(index: number): void {
    this.editItems.update(items => items.filter((_, i) => i !== index));
  }

  protected updateEditItem(index: number, value: string): void {
    this.editItems.update(items => items.map((item, i) => i === index ? value : item));
  }
}
