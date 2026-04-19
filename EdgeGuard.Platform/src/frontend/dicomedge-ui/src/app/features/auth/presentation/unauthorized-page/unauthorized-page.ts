import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';

import { UiButton } from '../../../../shared/components/ui-button/ui-button';

@Component({
  selector: 'app-unauthorized-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, FontAwesomeModule, UiButton],
  template: `
    <div class="unauthorized-page">
      <div class="unauthorized-card eg-animate-in">
        <div class="unauthorized-icon">
          <fa-icon icon="exclamation-triangle"></fa-icon>
        </div>
        <h1 class="unauthorized-title">Acceso denegado</h1>
        <p class="unauthorized-message">
          No tiene permisos para acceder a esta sección.
          Contacte al administrador si cree que es un error.
        </p>
        <ui-button variant="primary" routerLink="/dashboard" icon="arrow-left">
          Volver al inicio
        </ui-button>
      </div>
    </div>
  `,
  styles: `
    :host {
      display: block;
      height: 100%;
    }

    .unauthorized-page {
      display: flex;
      align-items: center;
      justify-content: center;
      min-height: 60vh;
      padding: 32px 24px;
    }

    .unauthorized-card {
      text-align: center;
      max-width: 420px;
    }

    .unauthorized-icon {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 72px;
      height: 72px;
      border-radius: 50%;
      background: rgba(239, 68, 68, 0.1);
      color: #ef4444;
      font-size: 1.75rem;
      margin-bottom: 20px;
    }

    .unauthorized-title {
      font-size: 1.5rem;
      font-weight: 700;
      color: var(--eg-text-primary);
      letter-spacing: -0.02em;
      margin-bottom: 8px;
    }

    .unauthorized-message {
      font-size: 0.9375rem;
      color: var(--eg-text-secondary);
      line-height: 1.6;
      margin-bottom: 24px;
    }
  `,
})
export default class UnauthorizedPage {}
