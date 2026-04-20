import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faPlus, faTimes, faCheck } from '@fortawesome/free-solid-svg-icons';

import { ROLES, PERMISSION_GROUPS } from '../../models/users.models';

@Component({
  selector: 'app-role-permission-panel',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FontAwesomeModule],
  templateUrl: './role-permission-panel.component.html',
  styleUrl: './role-permission-panel.component.scss'
})
export class RolePermissionPanel {
  readonly roles = input.required<string[]>();
  readonly permissions = input.required<string[]>();

  readonly addRoleClicked = output<string>();
  readonly removeRoleClicked = output<string>();
  readonly grantPermissionClicked = output<string>();
  readonly revokePermissionClicked = output<string>();

  protected readonly faPlus = faPlus;
  protected readonly faTimes = faTimes;
  protected readonly faCheck = faCheck;

  protected readonly allRoles = ROLES;
  protected readonly permissionGroups = PERMISSION_GROUPS;

  protected hasRole(role: string): boolean {
    return this.roles().includes(role);
  }

  protected hasPermission(perm: string): boolean {
    return this.permissions().includes(perm);
  }

  protected togglePermission(perm: string): void {
    if (this.hasPermission(perm)) {
      this.revokePermissionClicked.emit(perm);
    } else {
      this.grantPermissionClicked.emit(perm);
    }
  }
}
