import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faTimes, faCopy } from '@fortawesome/free-solid-svg-icons';
import { Clipboard } from '@angular/cdk/clipboard';

import { UiButton } from '../../../../shared/components/ui-button/ui-button.component';
import { UiChip } from '../../../../shared/components/ui-chip/ui-chip.component';
import { RelativeTimePipe } from '../../../../shared/pipes/relative-time.pipe';
import { AuditLogDto, getSeverityColor, getSeverityLabel } from '../../models/audit.models';

@Component({
  selector: 'app-audit-detail-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FontAwesomeModule, UiButton, UiChip, RelativeTimePipe],
  templateUrl: './audit-detail-dialog.component.html',
  styleUrl: './audit-detail-dialog.component.scss'
})
export class AuditDetailDialog {
  protected readonly dialogRef = inject(MatDialogRef<AuditDetailDialog>);
  protected readonly log: AuditLogDto = inject(MAT_DIALOG_DATA);
  private readonly clipboard = inject(Clipboard);

  protected readonly faTimes = faTimes;
  protected readonly faCopy = faCopy;
  protected readonly getSeverityColor = getSeverityColor;
  protected readonly getSeverityLabel = getSeverityLabel;

  protected copyCorrelation(): void {
    if (this.log.correlationId) {
      this.clipboard.copy(this.log.correlationId);
    }
  }
}
