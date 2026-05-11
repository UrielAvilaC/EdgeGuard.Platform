import { DestroyRef, inject, Injectable } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';

import { ToastService } from '../../../core/services/toast.service';
import { SortParams } from '../../../shared/models/sort.model';
import { UsersApiService } from '../infrastructure/users-api.service';
import {
  CreateUserRequest,
  ResetPasswordRequest,
  UpdateProfileRequest,
  UserFilter,
} from '../models/users.models';
import { UsersStore } from './users.store';

@Injectable()
export class UsersFacade {
  private readonly api = inject(UsersApiService);
  private readonly store = inject(UsersStore);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  readonly users = this.store.users;
  readonly selectedUser = this.store.selectedUser;
  readonly loading = this.store.loading;
  readonly error = this.store.error;
  readonly pagination = this.store.pagination;
  readonly sort = this.store.sort;
  readonly filter = this.store.filter;
  readonly hasData = this.store.hasData;
  readonly activeCount = this.store.activeCount;

  loadUsers(): void {
    const filter = this.store.filter();
    const pagination = this.store.pagination();
    const sort = this.store.sort();

    const params: UserFilter = {
      ...filter,
      page: pagination.page,
      pageSize: pagination.pageSize,
      sortBy: sort.sortBy,
      sortDir: sort.sortDir,
    };

    this.store.setLoading(true);
    this.api.getUsers(params).pipe(
      finalize(() => this.store.setLoading(false)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: result => this.store.setUsers(result.items, result.total),
      error: () => {
        this.store.setError('Error al cargar usuarios');
        this.toast.error('No se pudieron cargar los usuarios');
      },
    });
  }

  loadUserDetail(id: string): void {
    this.store.setLoading(true);
    this.api.getById(id).pipe(
      finalize(() => this.store.setLoading(false)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: user => this.store.setSelectedUser(user),
      error: () => this.toast.error('No se pudo cargar el usuario'),
    });
  }

  changePage(page: number, pageSize: number): void {
    this.store.setPagination(page, pageSize);
    this.loadUsers();
  }

  changeSort(sort: SortParams): void {
    this.store.setSort(sort);
    this.loadUsers();
  }

  updateFilter(partial: Partial<UserFilter>): void {
    this.store.updateFilter(partial);
    this.loadUsers();
  }

  clearFilters(): void {
    this.store.clearFilters();
    this.loadUsers();
  }

  createUser(request: CreateUserRequest): void {
    this.store.setLoading(true);
    this.api.create(request).pipe(
      finalize(() => this.store.setLoading(false)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.toast.success('Usuario creado correctamente');
        this.loadUsers();
      },
      error: () => this.toast.error('Error al crear el usuario'),
    });
  }

  updateProfile(id: string, request: UpdateProfileRequest): void {
    this.api.updateProfile(id, request).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.toast.success('Perfil actualizado');
        this.loadUserDetail(id);
        this.loadUsers();
      },
      error: () => this.toast.error('Error al actualizar el perfil'),
    });
  }

  deactivateUser(id: string): void {
    this.api.deactivate(id).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.toast.success('Usuario desactivado');
        this.loadUserDetail(id);
        this.loadUsers();
      },
      error: () => this.toast.error('Error al desactivar el usuario'),
    });
  }

  activateUser(id: string): void {
    this.api.activate(id).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.toast.success('Usuario activado');
        this.loadUserDetail(id);
        this.loadUsers();
      },
      error: () => this.toast.error('Error al activar el usuario'),
    });
  }

  assignRole(id: string, role: string): void {
    this.api.assignRole(id, role).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.toast.success('Rol asignado');
        this.loadUserDetail(id);
      },
      error: () => this.toast.error('Error al asignar el rol'),
    });
  }

  removeRole(id: string, role: string): void {
    this.api.removeRole(id, role).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.toast.success('Rol removido');
        this.loadUserDetail(id);
      },
      error: () => this.toast.error('Error al remover el rol'),
    });
  }

  grantPermission(id: string, permission: string): void {
    this.api.grantPermission(id, permission).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.toast.success('Permiso otorgado');
        this.loadUserDetail(id);
      },
      error: () => this.toast.error('Error al otorgar el permiso'),
    });
  }

  revokePermission(id: string, permission: string): void {
    this.api.revokePermission(id, permission).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.toast.success('Permiso revocado');
        this.loadUserDetail(id);
      },
      error: () => this.toast.error('Error al revocar el permiso'),
    });
  }

  resetPassword(id: string, request: ResetPasswordRequest): void {
    this.api.resetPassword(id, request).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => this.toast.success('Contraseña restablecida'),
      error: () => this.toast.error('Error al restablecer la contraseña'),
    });
  }

  unlockUser(id: string): void {
    this.api.unlock(id).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.toast.success('Usuario desbloqueado');
        this.loadUserDetail(id);
      },
      error: () => this.toast.error('Error al desbloquear el usuario'),
    });
  }
}
