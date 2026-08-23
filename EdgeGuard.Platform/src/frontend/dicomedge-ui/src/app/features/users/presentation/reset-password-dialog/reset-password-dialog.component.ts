import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogRef } from '@angular/material/dialog';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faTimes, faKey } from '@fortawesome/free-solid-svg-icons';

import { UiButton } from '../../../../shared/components/ui-button/ui-button.component';
import { UiIconButton } from '../../../../shared/components/ui-icon-button/ui-icon-button.component';
import { UiInputText } from '../../../../shared/forms/input-text/input-text.component';
import { UiDialog } from '../../../../shared/components/ui-dialog/ui-dialog.component';

@Component({
  selector: 'app-reset-password-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, FontAwesomeModule, UiButton, UiIconButton, UiInputText, UiDialog],
  templateUrl: './reset-password-dialog.component.html',
  styleUrl: './reset-password-dialog.component.scss'
})
export class ResetPasswordDialog {
  protected readonly dialogRef = inject(MatDialogRef<ResetPasswordDialog>);
  private readonly fb = inject(FormBuilder);
  protected readonly faTimes = faTimes;
  protected readonly faKey = faKey;

  protected readonly form = this.fb.nonNullable.group({
    newPassword: ['', [Validators.required, Validators.minLength(8)]],
    confirmPassword: ['', Validators.required],
  }, {
    validators: (group: import('@angular/forms').AbstractControl) => {
      const pw = group.get('newPassword')?.value;
      const confirm = group.get('confirmPassword')?.value;
      return pw === confirm ? null : { passwordMismatch: true };
    },
  });

  protected onSubmit(): void {
    if (this.form.invalid) return;
    this.dialogRef.close(this.form.getRawValue().newPassword);
  }
}
