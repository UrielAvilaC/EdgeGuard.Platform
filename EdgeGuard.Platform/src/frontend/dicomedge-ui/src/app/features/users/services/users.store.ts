import { computed, Injectable, signal } from '@angular/core';

import { PaginationMeta } from '../../../shared/models/pagination.model';
import { SortParams } from '../../../shared/models/sort.model';
import { UserDto, UserFilter } from '../models/users.models';

@Injectable()
export class UsersStore {
  private readonly _users = signal<UserDto[]>([]);
  private readonly _selectedUser = signal<UserDto | null>(null);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);
  private readonly _pagination = signal<PaginationMeta>({ page: 1, pageSize: 25, total: 0 });
  private readonly _sort = signal<SortParams>({ sortBy: 'username', sortDir: 'asc' });
  private readonly _filter = signal<UserFilter>({});

  readonly users = this._users.asReadonly();
  readonly selectedUser = this._selectedUser.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();
  readonly pagination = this._pagination.asReadonly();
  readonly sort = this._sort.asReadonly();
  readonly filter = this._filter.asReadonly();

  readonly hasData = computed(() => this._users().length > 0);
  readonly activeCount = computed(() => this._users().filter(u => u.isActive).length);

  setUsers(users: UserDto[], total: number): void {
    this._users.set(users);
    this._pagination.update(p => ({ ...p, total }));
    this._error.set(null);
  }

  setSelectedUser(user: UserDto | null): void { this._selectedUser.set(user); }
  setLoading(loading: boolean): void { this._loading.set(loading); }
  setError(error: string | null): void { this._error.set(error); }

  setPagination(page: number, pageSize: number): void {
    this._pagination.update(p => ({ ...p, page, pageSize }));
  }

  setSort(sort: SortParams): void { this._sort.set(sort); }

  updateFilter(partial: Partial<UserFilter>): void {
    this._filter.update(f => ({ ...f, ...partial }));
    this._pagination.update(p => ({ ...p, page: 1 }));
  }

  clearFilters(): void {
    this._filter.set({});
    this._pagination.update(p => ({ ...p, page: 1 }));
  }

  updateUserInList(updated: UserDto): void {
    this._users.update(list => list.map(u => u.id === updated.id ? updated : u));
    if (this._selectedUser()?.id === updated.id) this._selectedUser.set(updated);
  }
}
