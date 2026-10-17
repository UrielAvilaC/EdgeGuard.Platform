import { ChangeDetectionStrategy, Component, inject, OnInit, output, signal } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { NgOptimizedImage } from '@angular/common';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { IconProp } from '@fortawesome/fontawesome-svg-core';
import { catchError, of } from 'rxjs';

import { AuthStore } from '../../core/auth/store/auth.store';
import { ApiClient } from '../../core/api/api-client';

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
      { label: 'Outbox', route: '/outbox', icon: 'inbox', permission: 'ViewSystemStatus' },
      { label: 'WhatsApp', route: '/whatsapp', icon: 'comments', permission: 'ViewConfiguration' },
      { label: 'Plantillas Email', route: '/email-templates', icon: 'envelope', permission: 'ViewConfiguration' },
      { label: 'Notificaciones', route: '/notification-settings', icon: 'bell', permission: 'ViewConfiguration' },
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
  imports: [RouterLink, RouterLinkActive, FontAwesomeModule, NgOptimizedImage],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.scss'
})
export class Sidebar implements OnInit {
  readonly navigated = output();

  private readonly authStore = inject(AuthStore);
  private readonly api = inject(ApiClient);

  protected readonly version = signal<string>('1.4.2');

  ngOnInit(): void {
    this.api.get<{ version?: string }>('/info').pipe(
      catchError(() => of(null)),
    ).subscribe((info) => {
      if (info?.version) {
        this.version.set(info.version);
      }
    });
  }

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
