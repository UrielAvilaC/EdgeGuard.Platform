import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogRef } from '@angular/material/dialog';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faTimes, faUserPlus } from '@fortawesome/free-solid-svg-icons';

import { UiButton } from '../../../../shared/components/ui-button/ui-button.component';
import { UiIconButton } from '../../../../shared/components/ui-icon-button/ui-icon-button.component';
import { UiInputText } from '../../../../shared/forms/input-text/input-text.component';
import { UiDropdown } from '../../../../shared/forms/dropdown/dropdown.component';
import { ROLES, CreateUserRequest } from '../../models/users.models';

@Component({
  selector: 'app-user-form-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, FontAwesomeModule, UiButton, UiIconButton, UiInputText, UiDropdown],
  templateUrl: './user-form-dialog.component.html',
  styleUrl: './user-form-dialog.component.scss'
})
export class UserFormDialog {
  protected readonly dialogRef = inject(MatDialogRef<UserFormDialog>);
  private readonly fb = inject(FormBuilder);
  protected readonly faTimes = faTimes;
  protected readonly faUserPlus = faUserPlus;

  protected readonly roleOptions = ROLES.map(r => ({ value: r.value, label: r.label }));

  protected readonly form = this.fb.nonNullable.group({
    username: ['', Validators.required],
    fullName: ['', Validators.required],
    password: ['', [Validators.required, Validators.minLength(8)]],
    roles: [[] as string[]],
  });

  protected onSubmit(): void {
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    const request: CreateUserRequest = {
      username: v.username,
      password: v.password,
      fullName: v.fullName,
      roles: v.roles.length > 0 ? v.roles : undefined,
    };
    this.dialogRef.close(request);
  }
}
