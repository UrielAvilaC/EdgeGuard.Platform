import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faPlus, faSync } from '@fortawesome/free-solid-svg-icons';

import { UiPageHeader } from '../../../../shared/components/ui-page-header/ui-page-header.component';
import { UiButton } from '../../../../shared/components/ui-button/ui-button.component';
import { UiDataTable, UiCellDef } from '../../../../shared/components/ui-data-table/ui-data-table.component';
import { UiChip } from '../../../../shared/components/ui-chip/ui-chip.component';
import { UiSearchBar } from '../../../../shared/components/ui-search-bar/ui-search-bar.component';
import { UiAlert } from '../../../../shared/components/ui-alert/ui-alert.component';
import { TableColumn } from '../../../../shared/models/table.model';
import { RelativeTimePipe } from '../../../../shared/pipes/relative-time.pipe';
import { UserDto, ROLES, CreateUserRequest } from '../../models/users.models';
import { UsersStore } from '../../services/users.store';
import { UsersFacade } from '../../services/users.facade';
import { UserFormDialog } from '../user-form-dialog/user-form-dialog.component';

@Component({
  selector: 'app-users-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [UsersStore, UsersFacade],
  imports: [
    FontAwesomeModule, UiPageHeader, UiButton, UiDataTable, UiCellDef,
    UiChip, UiSearchBar, UiAlert, RelativeTimePipe,
  ],
  templateUrl: './users-page.component.html',
  styleUrl: './users-page.component.scss'
})
export default class UsersPage {
  protected readonly facade = inject(UsersFacade);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);

  protected readonly faPlus = faPlus;
  protected readonly faSync = faSync;

  protected readonly columns: TableColumn<UserDto>[] = [
    { key: 'username', header: 'Usuario', sortable: true },
    { key: 'fullName', header: 'Nombre', sortable: true },
    { key: 'roles', header: 'Roles' },
    { key: 'isActive', header: 'Estado', sortable: true, width: '100px' },
    { key: 'isLocked', header: 'Bloqueo', width: '100px' },
    { key: 'lastLoginAt', header: 'Último login', sortable: true },
  ];

  constructor() {
    this.facade.loadUsers();
  }

  protected onRowClick(user: UserDto): void {
    this.router.navigate(['/users', user.id]);
  }

  protected openCreateDialog(): void {
    this.dialog.open(UserFormDialog).afterClosed().subscribe((result: CreateUserRequest | null) => {
      if (result) this.facade.createUser(result);
    });
  }

  protected getRoleLabel(role: string): string {
    return ROLES.find(r => r.value === role)?.label ?? role;
  }

  protected asStringArray(value: unknown): string[] {
    return value as string[];
  }

  protected asString(value: unknown): string {
    return value as string;
  }
}
