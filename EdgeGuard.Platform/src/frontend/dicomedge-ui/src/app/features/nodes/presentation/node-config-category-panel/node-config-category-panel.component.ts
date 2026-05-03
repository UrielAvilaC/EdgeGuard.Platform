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

  protected readonly faChevronDown = faChevronDown;
  protected readonly faChevronRight = faChevronRight;
  protected readonly faPen = faPen;
  protected readonly faCheck = faCheck;
  protected readonly faTimes = faTimes;
  protected readonly faUndo = faUndo;

  protected startEdit(setting: NodeConfigurationProfileDto): void {
    this.editingKey.set(setting.settingKey);
    this.editValue.set(setting.value);
  }

  protected cancelEdit(): void {
    this.editingKey.set(null);
    this.editValue.set('');
  }

  protected save(setting: NodeConfigurationProfileDto): void {
    const newValue = this.editValue();
    if (newValue !== setting.value) {
      this.settingChanged.emit({ key: setting.settingKey, value: newValue });
    }
    this.cancelEdit();
  }

  protected reset(settingKey: string): void {
    this.settingReset.emit(settingKey);
  }
}
