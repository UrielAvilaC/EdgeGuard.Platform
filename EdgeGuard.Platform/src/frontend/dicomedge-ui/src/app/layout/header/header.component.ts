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
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss'
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
