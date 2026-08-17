import { ChangeDetectionStrategy, Component, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import {
  faChevronDown,
  faChevronRight,
  faPen,
  faCheck,
  faTimes,
  faUndo,
  faPlus,
  faTrash,
} from '@fortawesome/free-solid-svg-icons';

import { UiChip } from '../../../../shared/components/ui-chip/ui-chip.component';
import { UiIconButton } from '../../../../shared/components/ui-icon-button/ui-icon-button.component';
import { NodeConfigurationProfileDto } from '../../../settings/models/settings.models';

@Component({
  selector: 'app-node-config-category-panel',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, FontAwesomeModule, UiChip, UiIconButton],
  templateUrl: './node-config-category-panel.component.html',
})
export class NodeConfigCategoryPanel {
  readonly category = input.required<string>();
  readonly label = input.required<string>();
  readonly settings = input.required<NodeConfigurationProfileDto[]>();
  readonly pendingKeys = input<Set<string>>(new Set());

  readonly settingChanged = output<{ key: string; value: string }>();
  readonly settingReset = output<string>();

  protected readonly expanded = signal(true);
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
  protected readonly faUndo = faUndo;
  protected readonly faPlus = faPlus;
  protected readonly faTrash = faTrash;

  /**
   * Setting keys whose value is DERIVED from the canonical node AE Title
   * (`dicom.ae_title`, single source of truth). They are shown read-only so they
   * cannot drift from the SCP AE. Edit `dicom.ae_title` to change them.
   */
  private static readonly DERIVED_AE_KEYS = new Set<string>([
    'sender.local_ae_title',
    'node.ae_title',
  ]);

  protected isDerived(settingKey: string): boolean {
    return NodeConfigCategoryPanel.DERIVED_AE_KEYS.has(settingKey);
  }

  protected isJsonArray(valueType: string): boolean {
    const t = valueType.toLowerCase();
    return t === 'jsonarray' || t === 'json_array';
  }

  protected isJson(valueType: string): boolean {
    return valueType.toLowerCase() === 'json';
  }

  protected displayValue(setting: NodeConfigurationProfileDto): string {
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

  protected startEdit(setting: NodeConfigurationProfileDto): void {
    this.editingKey.set(setting.settingKey);
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

  protected save(setting: NodeConfigurationProfileDto): void {
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
      this.settingChanged.emit({ key: setting.settingKey, value: newValue });
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

  protected reset(settingKey: string): void {
    this.settingReset.emit(settingKey);
  }
}
