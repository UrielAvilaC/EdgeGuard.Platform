import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import {
  faArrowLeft, faKey, faLock, faLockOpen, faToggleOn, faToggleOff,
} from '@fortawesome/free-solid-svg-icons';

import { UiPageHeader } from '../../../../shared/components/ui-page-header/ui-page-header.component';
import { UiButton } from '../../../../shared/components/ui-button/ui-button.component';
import { UiChip } from '../../../../shared/components/ui-chip/ui-chip.component';
import { UiConfirmDialog, ConfirmDialogData } from '../../../../shared/components/ui-confirm-dialog/ui-confirm-dialog.component';
import { RelativeTimePipe } from '../../../../shared/pipes/relative-time.pipe';
import { UsersStore } from '../../services/users.store';
import { UsersFacade } from '../../services/users.facade';
import { RolePermissionPanel } from '../role-permission-panel/role-permission-panel.component';
import { ResetPasswordDialog } from '../reset-password-dialog/reset-password-dialog.component';

@Component({
  selector: 'app-user-detail-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [UsersStore, UsersFacade],
  imports: [
    FontAwesomeModule, UiPageHeader, UiButton, UiChip,
    RelativeTimePipe, RolePermissionPanel,
  ],
  templateUrl: './user-detail-page.component.html',
  styleUrl: './user-detail-page.component.scss'
})
export default class UserDetailPage {
  protected readonly facade = inject(UsersFacade);
  protected readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly dialog = inject(MatDialog);

  protected readonly faArrowLeft = faArrowLeft;
  protected readonly faKey = faKey;
  protected readonly faLock = faLock;
  protected readonly faLockOpen = faLockOpen;
  protected readonly faToggleOn = faToggleOn;
  protected readonly faToggleOff = faToggleOff;

  constructor() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) this.facade.loadUserDetail(id);
  }

  protected confirmDeactivate(): void {
    const user = this.facade.selectedUser();
    if (!user) return;
    this.dialog.open(UiConfirmDialog, {
      data: {
        title: 'Desactivar usuario',
        message: `¿Desactivar a "${user.fullName}"? El usuario no podrá iniciar sesión.`,
        confirmText: 'Desactivar',
        confirmColor: 'warn',
      } satisfies ConfirmDialogData,
    }).afterClosed().subscribe((confirmed: boolean) => {
      if (confirmed) this.facade.deactivateUser(user.id);
    });
  }

  protected openResetPasswordDialog(): void {
    const user = this.facade.selectedUser();
    if (!user) return;
    this.dialog.open(ResetPasswordDialog, { disableClose: true }).afterClosed().subscribe((newPassword: string | null) => {
      if (newPassword) this.facade.resetPassword(user.id, { newPassword });
    });
  }
}
