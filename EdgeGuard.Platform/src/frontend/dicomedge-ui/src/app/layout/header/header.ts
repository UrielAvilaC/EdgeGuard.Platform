import { ChangeDetectionStrategy, Component, inject, input, output, signal } from '@angular/core';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatMenuModule } from '@angular/material/menu';
import { MatDividerModule } from '@angular/material/divider';
import { MatTooltipModule } from '@angular/material/tooltip';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { MatIconButton } from '@angular/material/button';

import { AuthStore } from '../../core/auth/store/auth.store';
import { AuthService } from '../../core/auth/services/auth.service';

@Component({
  selector: 'app-header',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatToolbarModule,
    MatMenuModule,
    MatDividerModule,
    MatTooltipModule,
    MatIconButton,
    FontAwesomeModule,
  ],
  template: `
    <header class="header-bar">
      <div class="header-left">
        <button
          mat-icon-button
          (click)="toggleSidenav.emit()"
          aria-label="Alternar menú lateral"
          class="hamburger-btn"
        >
          <fa-icon icon="bars"></fa-icon>
        </button>
      </div>

      <div class="header-right">
        <button
          mat-icon-button
          (click)="toggleDarkMode()"
          [matTooltip]="darkMode() ? 'Modo claro' : 'Modo oscuro'"
          [attr.aria-label]="darkMode() ? 'Cambiar a modo claro' : 'Cambiar a modo oscuro'"
          class="action-btn"
        >
          <fa-icon [icon]="darkMode() ? 'sun' : 'moon'"></fa-icon>
        </button>

        <div class="header-divider"></div>

        <button
          mat-icon-button
          [matMenuTriggerFor]="userMenu"
          aria-label="Menú de usuario"
          class="user-avatar-btn"
        >
          <div class="user-avatar">
            {{ initials() }}
          </div>
        </button>

        <mat-menu #userMenu="matMenu" xPosition="before">
          <div class="user-menu-header">
            <div class="user-menu-avatar">{{ initials() }}</div>
            <div class="user-menu-info">
              <p class="user-menu-name">{{ authStore.user()?.fullName }}</p>
              <p class="user-menu-email">{{ authStore.user()?.userName }}</p>
            </div>
          </div>
          <mat-divider></mat-divider>
          <button mat-menu-item (click)="authService.logout()">
            <fa-icon icon="sign-out-alt" class="mr-3 text-[var(--eg-text-muted)]"></fa-icon>
            <span>Cerrar sesión</span>
          </button>
        </mat-menu>
      </div>
    </header>
  `,
  styles: `
    :host {
      display: block;
    }

    .header-bar {
      display: flex;
      align-items: center;
      justify-content: space-between;
      height: var(--eg-header-height);
      padding: 0 12px 0 4px;
      background: var(--eg-surface);
      border-bottom: 1px solid var(--eg-border);
      z-index: 100;
    }

    .header-left {
      display: flex;
      align-items: center;
      gap: 8px;
    }

    .header-right {
      display: flex;
      align-items: center;
      gap: 4px;
    }

    .hamburger-btn {
      color: var(--eg-text-secondary);
    }

    .action-btn {
      color: var(--eg-text-secondary);
      transition: color var(--eg-transition-fast);

      &:hover {
        color: var(--eg-text-primary);
      }
    }

    .header-divider {
      width: 1px;
      height: 24px;
      background: var(--eg-border);
      margin: 0 8px;
    }

    .user-avatar-btn {
      padding: 0;
    }

    .user-avatar {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 34px;
      height: 34px;
      border-radius: 8px;
      background: linear-gradient(135deg, #0078d4, #005a9e);
      color: #fff;
      font-size: 0.8125rem;
      font-weight: 600;
      letter-spacing: 0.02em;
    }

    .user-menu-header {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 16px 16px 12px;
    }

    .user-menu-avatar {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 40px;
      height: 40px;
      border-radius: 10px;
      background: linear-gradient(135deg, #0078d4, #005a9e);
      color: #fff;
      font-size: 0.875rem;
      font-weight: 600;
      flex-shrink: 0;
    }

    .user-menu-info {
      min-width: 0;
    }

    .user-menu-name {
      font-size: 0.875rem;
      font-weight: 600;
      color: var(--eg-text-primary);
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .user-menu-email {
      font-size: 0.75rem;
      color: var(--eg-text-muted);
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }
  `,
})
export class Header {
  readonly sidenavOpen = input(true);
  readonly toggleSidenav = output();

  protected readonly authStore = inject(AuthStore);
  protected readonly authService = inject(AuthService);
  protected readonly darkMode = signal(false);

  constructor() {
    const stored = localStorage.getItem('edgeguard_dark_mode');
    if (stored === 'true' || (!stored && window.matchMedia('(prefers-color-scheme: dark)').matches)) {
      this.darkMode.set(true);
      document.documentElement.classList.add('dark');
    }
  }

  protected initials(): string {
    const name = this.authStore.user()?.fullName ?? '';
    return name
      .split(' ')
      .map((w) => w[0])
      .filter(Boolean)
      .slice(0, 2)
      .join('')
      .toUpperCase();
  }

  protected toggleDarkMode(): void {
    this.darkMode.update((v) => !v);
    if (this.darkMode()) {
      document.documentElement.classList.add('dark');
      localStorage.setItem('edgeguard_dark_mode', 'true');
    } else {
      document.documentElement.classList.remove('dark');
      localStorage.setItem('edgeguard_dark_mode', 'false');
    }
  }
}
