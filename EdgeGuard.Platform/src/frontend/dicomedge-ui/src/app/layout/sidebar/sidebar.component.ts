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
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.scss'
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
