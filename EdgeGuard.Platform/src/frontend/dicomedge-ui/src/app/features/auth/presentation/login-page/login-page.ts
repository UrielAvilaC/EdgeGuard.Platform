import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { HttpErrorResponse } from '@angular/common/http';

import { UiInputText } from '../../../../shared/forms/input-text/input-text';
import { UiInputPassword } from '../../../../shared/forms/input-password/input-password';
import { UiButton } from '../../../../shared/components/ui-button/ui-button';
import { UiAlert } from '../../../../shared/components/ui-alert/ui-alert';
import { AuthService } from '../../../../core/auth/services/auth.service';

@Component({
  selector: 'app-login-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    FontAwesomeModule,
    UiInputText,
    UiInputPassword,
    UiButton,
    UiAlert,
  ],
  template: `
    <div class="login-container">
      <!-- Branding Panel -->
      <div class="login-branding">
        <div class="branding-content">
          <div class="branding-logo">
            <div class="logo-icon">
              <fa-icon icon="shield-alt" size="lg"></fa-icon>
            </div>
            <span class="logo-text">EdgeGuard</span>
          </div>

          <h1 class="branding-title">
            Plataforma DICOM<br />de última generación
          </h1>
          <p class="branding-subtitle">
            Gestione, monitoree y distribuya imágenes médicas con la infraestructura
            más segura y confiable.
          </p>

          <div class="branding-features">
            <div class="feature-item">
              <fa-icon icon="server" class="feature-icon"></fa-icon>
              <span>Enrutamiento inteligente multi-nodo</span>
            </div>
            <div class="feature-item">
              <fa-icon icon="shield-alt" class="feature-icon"></fa-icon>
              <span>Cifrado extremo a extremo</span>
            </div>
            <div class="feature-item">
              <fa-icon icon="chart-bar" class="feature-icon"></fa-icon>
              <span>Monitoreo y auditoría en tiempo real</span>
            </div>
          </div>
        </div>

        <p class="branding-footer">
          &copy; {{ currentYear }} EdgeGuard Platform
        </p>
      </div>

      <!-- Login Form Panel -->
      <div class="login-form-panel">
        <div class="form-wrapper">
          <div class="form-header">
            <div class="form-logo-mobile">
              <fa-icon icon="shield-alt"></fa-icon>
            </div>
            <h2 class="form-title">Iniciar sesión</h2>
            <p class="form-subtitle">Ingrese sus credenciales para continuar</p>
          </div>

          @if (errorMessage()) {
            <ui-alert
              [type]="isLocked() ? 'warning' : 'error'"
              [dismissible]="true"
              (dismissed)="errorMessage.set(null)"
              class="mb-5 block"
            >
              {{ errorMessage() }}
            </ui-alert>
          }

          <form [formGroup]="form" (ngSubmit)="onSubmit()" class="login-form">
            <ui-input-text
              formControlName="username"
              label="Usuario"
              placeholder="Ingrese su usuario"
            ></ui-input-text>

            <ui-input-password
              formControlName="password"
              label="Contraseña"
              placeholder="Ingrese su contraseña"
            ></ui-input-password>

            <ui-button
              type="submit"
              variant="primary"
              size="lg"
              [loading]="loading()"
              [disabled]="form.invalid || loading()"
              class="mt-1 block w-full"
            >
              Iniciar sesión
            </ui-button>
          </form>
        </div>
      </div>
    </div>
  `,
  styles: `
    :host {
      display: block;
      height: 100vh;
    }

    .login-container {
      display: flex;
      height: 100%;
    }

    // ── Branding Panel ──
    .login-branding {
      display: none;
      flex-direction: column;
      justify-content: space-between;
      width: 44%;
      min-width: 400px;
      padding: 48px;
      background: linear-gradient(135deg, #0078d4 0%, #005a9e 50%, #003d6b 100%);
      color: #fff;

      @media (min-width: 1024px) {
        display: flex;
      }
    }

    .branding-content {
      margin-top: 32px;
    }

    .branding-logo {
      display: flex;
      align-items: center;
      gap: 12px;
      margin-bottom: 48px;
    }

    .logo-icon {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 40px;
      height: 40px;
      border-radius: 10px;
      background: rgba(255 255 255 / 0.2);
      backdrop-filter: blur(8px);
    }

    .logo-text {
      font-size: 1.25rem;
      font-weight: 700;
      letter-spacing: -0.02em;
    }

    .branding-title {
      font-size: 2.25rem;
      font-weight: 800;
      line-height: 1.2;
      letter-spacing: -0.03em;
      margin-bottom: 16px;
    }

    .branding-subtitle {
      font-size: 1.05rem;
      line-height: 1.6;
      color: rgba(255 255 255 / 0.8);
      max-width: 380px;
      margin-bottom: 40px;
    }

    .branding-features {
      display: flex;
      flex-direction: column;
      gap: 16px;
    }

    .feature-item {
      display: flex;
      align-items: center;
      gap: 14px;
      font-size: 0.9375rem;
      color: rgba(255 255 255 / 0.9);
    }

    .feature-icon {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 36px;
      height: 36px;
      border-radius: 8px;
      background: rgba(255 255 255 / 0.15);
      font-size: 0.875rem;
    }

    .branding-footer {
      font-size: 0.8125rem;
      color: rgba(255 255 255 / 0.5);
    }

    // ── Form Panel ──
    .login-form-panel {
      flex: 1;
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 32px 24px;
      background: var(--eg-surface);
    }

    .form-wrapper {
      width: 100%;
      max-width: 400px;
      animation: eg-fade-in var(--eg-transition-normal) both;
    }

    .form-header {
      margin-bottom: 32px;
    }

    .form-logo-mobile {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 48px;
      height: 48px;
      border-radius: 12px;
      background: #0078d4;
      color: #fff;
      font-size: 1.25rem;
      margin-bottom: 24px;

      @media (min-width: 1024px) {
        display: none;
      }
    }

    .form-title {
      font-size: 1.75rem;
      font-weight: 700;
      color: var(--eg-text-primary);
      letter-spacing: -0.02em;
      margin-bottom: 6px;
    }

    .form-subtitle {
      font-size: 0.9375rem;
      color: var(--eg-text-secondary);
    }

    .login-form {
      display: flex;
      flex-direction: column;
      gap: 20px;
    }
  `,
})
export default class LoginPage {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  protected readonly currentYear = new Date().getFullYear();
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
      },
    });
  }
}
