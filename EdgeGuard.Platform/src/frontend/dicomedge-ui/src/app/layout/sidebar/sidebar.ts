import { ChangeDetectionStrategy, Component, inject, output } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { IconProp } from '@fortawesome/fontawesome-svg-core';

import { AuthStore } from '../../core/auth/store/auth.store';

interface NavGroup {
  label: string;
  items: NavItem[];
}

interface NavItem {
  label: string;
  route: string;
  icon: IconProp;
  permission?: string;
}

const NAV_GROUPS: NavGroup[] = [
  {
    label: 'Principal',
    items: [
      { label: 'Dashboard', route: '/dashboard', icon: 'tachometer-alt' },
      { label: 'Estudios', route: '/studies', icon: 'clipboard-list', permission: 'ViewStudies' },
      { label: 'Pacientes', route: '/patients', icon: 'user', permission: 'ViewStudies' },
    ],
  },
  {
    label: 'Infraestructura',
    items: [
      { label: 'Nodos', route: '/nodes', icon: 'server', permission: 'ViewNodes' },
      { label: 'Servidores PACS', route: '/pacs', icon: 'database', permission: 'ViewConfiguration' },
      { label: 'Reglas de Ruteo', route: '/routing-rules', icon: 'network-wired', permission: 'ManageRoutingRules' },
    ],
  },
  {
    label: 'Monitoreo',
    items: [
      { label: 'Estado HL7', route: '/hl7', icon: 'chart-bar', permission: 'ViewQueue' },
      { label: 'Cola de Mensajes', route: '/queue', icon: 'sync', permission: 'ViewQueue' },
      { label: 'WhatsApp', route: '/whatsapp', icon: 'comments', permission: 'ViewConfiguration' },
    ],
  },
  {
    label: 'Administración',
    items: [
      { label: 'Usuarios', route: '/users', icon: 'users-cog', permission: 'ViewUsers' },
      { label: 'Configuración', route: '/settings', icon: 'sliders-h', permission: 'ViewConfiguration' },
      { label: 'Auditoría', route: '/audit', icon: 'shield-alt', permission: 'ViewAuditLogs' },
    ],
  },
];

@Component({
  selector: 'app-sidebar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, RouterLinkActive, FontAwesomeModule],
  template: `
    <nav class="sidebar-nav" aria-label="Navegación principal">
      <div class="sidebar-brand">
        <div class="brand-icon">
          <fa-icon icon="shield-alt"></fa-icon>
        </div>
        <span class="brand-text">EdgeGuard</span>
      </div>

      <div class="sidebar-scroll">
        @for (group of visibleGroups(); track group.label) {
          <div class="nav-group">
            <p class="nav-group-label">{{ group.label }}</p>
            <ul class="nav-list">
              @for (item of group.items; track item.route) {
                <li>
                  <a
                    [routerLink]="item.route"
                    routerLinkActive="nav-item-active"
                    class="nav-item"
                    (click)="navigated.emit()"
                  >
                    <fa-icon [icon]="item.icon" class="nav-item-icon"></fa-icon>
                    <span>{{ item.label }}</span>
                  </a>
                </li>
              }
            </ul>
          </div>
        }
      </div>

      <div class="sidebar-footer">
        <div class="sidebar-version">v1.0.0</div>
      </div>
    </nav>
  `,
  styles: `
    :host {
      display: block;
      height: 100%;
    }

    .sidebar-nav {
      display: flex;
      flex-direction: column;
      height: 100%;
      background: var(--eg-surface);
    }

    .sidebar-brand {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 20px 20px 16px;
      border-bottom: 1px solid var(--eg-border-subtle);
    }

    .brand-icon {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 32px;
      height: 32px;
      border-radius: 8px;
      background: linear-gradient(135deg, #0078d4, #005a9e);
      color: #fff;
      font-size: 0.875rem;
    }

    .brand-text {
      font-size: 1.1rem;
      font-weight: 700;
      color: var(--eg-text-primary);
      letter-spacing: -0.02em;
    }

    .sidebar-scroll {
      flex: 1;
      overflow-y: auto;
      padding: 16px 12px;
    }

    .nav-group {
      margin-bottom: 20px;
    }

    .nav-group-label {
      padding: 0 12px;
      margin-bottom: 6px;
      font-size: 0.6875rem;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.06em;
      color: var(--eg-text-muted);
    }

    .nav-list {
      list-style: none;
      margin: 0;
      padding: 0;
      display: flex;
      flex-direction: column;
      gap: 2px;
    }

    .nav-item {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 9px 12px;
      border-radius: var(--eg-radius-md);
      font-size: 0.875rem;
      font-weight: 500;
      color: var(--eg-text-secondary);
      text-decoration: none;
      transition: all var(--eg-transition-fast);
      position: relative;

      &:hover {
        background: var(--eg-surface-container);
        color: var(--eg-text-primary);
      }
    }

    .nav-item-icon {
      width: 18px;
      text-align: center;
      font-size: 0.875rem;
      opacity: 0.7;
      transition: opacity var(--eg-transition-fast);
    }

    .nav-item:hover .nav-item-icon {
      opacity: 1;
    }

    .nav-item-active {
      background: rgba(0, 120, 212, 0.08);
      color: #0078d4;
      font-weight: 600;

      &::before {
        content: '';
        position: absolute;
        left: 0;
        top: 6px;
        bottom: 6px;
        width: 3px;
        border-radius: 0 3px 3px 0;
        background: #0078d4;
      }

      .nav-item-icon {
        opacity: 1;
        color: #0078d4;
      }
    }

    :host-context(.dark) .nav-item-active {
      background: rgba(0, 120, 212, 0.15);
      color: #4da6ff;

      &::before {
        background: #4da6ff;
      }

      .nav-item-icon {
        color: #4da6ff;
      }
    }

    .sidebar-footer {
      padding: 12px 20px;
      border-top: 1px solid var(--eg-border-subtle);
    }

    .sidebar-version {
      font-size: 0.6875rem;
      color: var(--eg-text-muted);
    }
  `,
})
export class Sidebar {
  readonly navigated = output();

  private readonly authStore = inject(AuthStore);

  protected visibleGroups(): NavGroup[] {
    return NAV_GROUPS
      .map((group) => ({
        ...group,
        items: group.items.filter((item) =>
          !item.permission || this.authStore.hasPermission(item.permission),
        ),
      }))
      .filter((group) => group.items.length > 0);
  }
}
