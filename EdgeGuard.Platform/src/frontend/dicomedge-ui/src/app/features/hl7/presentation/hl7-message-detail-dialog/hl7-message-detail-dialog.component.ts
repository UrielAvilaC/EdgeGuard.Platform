import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faXmark, faCopy, faRotateRight } from '@fortawesome/free-solid-svg-icons';
import { Clipboard } from '@angular/cdk/clipboard';

import { UiButton } from '../../../../shared/components/ui-button/ui-button.component';
import { UiIconButton } from '../../../../shared/components/ui-icon-button/ui-icon-button.component';
import { UiChip } from '../../../../shared/components/ui-chip/ui-chip.component';
import { RelativeTimePipe } from '../../../../shared/pipes/relative-time.pipe';
import { ChipColor } from '../../../../shared/components/ui-chip/ui-chip.component';
import { Hl7MessageDetail, MESSAGE_STATUS_OPTIONS } from '../../models/hl7.models';
import { UiDialog } from '../../../../shared/components/ui-dialog/ui-dialog.component';

export interface Hl7MessageDetailDialogData {
  message: Hl7MessageDetail;
}

@Component({
  selector: 'app-hl7-message-detail-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatDialogModule,
    FontAwesomeModule,
    UiButton,
    UiIconButton,
    UiChip,
    RelativeTimePipe,
    UiDialog,
  ],
  templateUrl: './hl7-message-detail-dialog.component.html',
  styleUrl: './hl7-message-detail-dialog.component.scss'
})
export class Hl7MessageDetailDialog {
  private readonly dialogRef = inject(MatDialogRef<Hl7MessageDetailDialog>);
  private readonly clipboard = inject(Clipboard);
  protected readonly message: Hl7MessageDetail = inject(MAT_DIALOG_DATA).message;

  protected readonly faXmark = faXmark;
  protected readonly faCopy = faCopy;
  protected readonly faRotateRight = faRotateRight;

  protected readonly statusLabel: string;
  protected readonly statusColor: ChipColor;

  constructor() {
    const opt = MESSAGE_STATUS_OPTIONS.find(o => o.value === this.message.status);
    this.statusLabel = opt?.label ?? this.message.status;
    this.statusColor = opt?.color ?? 'default';
  }

  protected copyContent(): void {
    this.clipboard.copy(this.message.content);
  }

  protected onReprocess(): void {
    this.dialogRef.close({ action: 'reprocess', id: this.message.id });
  }

  protected onClose(): void {
    this.dialogRef.close();
  }
}
