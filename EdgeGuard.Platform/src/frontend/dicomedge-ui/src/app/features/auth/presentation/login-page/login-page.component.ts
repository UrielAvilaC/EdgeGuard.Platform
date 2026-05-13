import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { NgOptimizedImage } from '@angular/common';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { HttpErrorResponse } from '@angular/common/http';

import { UiInputText } from '../../../../shared/forms/input-text/input-text.component';
import { UiInputPassword } from '../../../../shared/forms/input-password/input-password.component';
import { UiButton } from '../../../../shared/components/ui-button/ui-button.component';
import { UiAlert } from '../../../../shared/components/ui-alert/ui-alert.component';
import { AuthService } from '../../../../core/auth/services/auth.service';

@Component({
  selector: 'app-login-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    NgOptimizedImage,
    FontAwesomeModule,
    UiInputText,
    UiInputPassword,
    UiButton,
    UiAlert,
  ],
  templateUrl: './login-page.component.html',
  styleUrl: './login-page.component.scss'
})
export default class LoginPage {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  protected readonly currentYear = signal(new Date().getFullYear());
  protected readonly loading = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly isLocked = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    username: ['', Validators.required],
    password: ['', Validators.required],
  });

  onSubmit(): void {
    if (this.form.invalid) return;

    this.loading.set(true);
    this.errorMessage.set(null);

    const { username, password } = this.form.getRawValue();

    this.authService.login({ username, password }).subscribe({
      next: () => {
        this.loading.set(false);
        this.router.navigate(['/dashboard']);
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);

        if (err.status === 423) {
          this.isLocked.set(true);
          this.errorMessage.set('Cuenta bloqueada. Intente nuevamente más tarde.');
          return;
        }

        this.isLocked.set(false);
        const reason = err.error?.failureReason;
        this.errorMessage.set(reason ?? 'Credenciales inválidas. Intente nuevamente.');
        console.error('Login error:', err);
      },
    });
  }
}
