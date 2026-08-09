import { Routes } from '@angular/router';

import { MainLayout } from './layout/main-layout/main-layout.component';
import { authGuard } from './core/auth/guards/auth.guard';
import { permissionGuard } from './core/auth/guards/permission.guard';

export const routes: Routes = [
  {
    path: 'login',
    title: 'Iniciar sesión',
    loadComponent: () => import('./features/auth/presentation/login-page/login-page.component'),
  },
  {
    path: 'unauthorized',
    title: 'Acceso denegado',
    loadComponent: () => import('./features/auth/presentation/unauthorized-page/unauthorized-page.component'),
  },
  {
    path: '',
    component: MainLayout,
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'dashboard',
        title: 'Dashboard',
        loadChildren: () => import('./features/dashboard/dashboard.routes').then((m) => m.dashboardRoutes),
      },
      {
        path: 'studies',
        title: 'Estudios',
        canActivate: [permissionGuard],
        data: { permission: 'ViewStudies' },
        loadChildren: () => import('./features/studies/studies.routes').then((m) => m.studiesRoutes),
      },
      {
        path: 'patients',
        title: 'Pacientes',
        canActivate: [permissionGuard],
        data: { permission: 'ViewStudies' },
        loadChildren: () => import('./features/patients/patients.routes').then((m) => m.patientsRoutes),
      },
      {
        path: 'nodes',
        title: 'Nodos',
        canActivate: [permissionGuard],
        data: { permission: 'ViewNodes' },
        loadChildren: () => import('./features/nodes/nodes.routes').then((m) => m.nodesRoutes),
      },
      {
        path: 'pacs',
        title: 'Servidores PACS',
        canActivate: [permissionGuard],
        data: { permission: 'ViewConfiguration' },
        loadChildren: () => import('./features/pacs/pacs.routes').then((m) => m.pacsRoutes),
      },
      {
        path: 'routing-rules',
        title: 'Reglas de Ruteo',
        canActivate: [permissionGuard],
        data: { permission: 'ManageRoutingRules' },
        loadChildren: () => import('./features/routing-rules/routing-rules.routes').then((m) => m.routingRulesRoutes),
      },
      {
        path: 'hl7',
        title: 'Estado HL7',
        canActivate: [permissionGuard],
        data: { permission: 'ViewQueue' },
        loadChildren: () => import('./features/hl7/hl7.routes').then((m) => m.hl7Routes),
      },
      {
        path: 'queue',
        title: 'Cola de Mensajes',
        canActivate: [permissionGuard],
        data: { permission: 'ViewQueue' },
        loadChildren: () => import('./features/queue/queue.routes').then((m) => m.queueRoutes),
      },
      {
        path: 'outbox',
        title: 'Outbox',
        canActivate: [permissionGuard],
        data: { permission: 'ViewSystemStatus' },
        loadChildren: () => import('./features/outbox/outbox.routes').then((m) => m.outboxRoutes),
      },
      {
        path: 'whatsapp',
        title: 'WhatsApp',
        canActivate: [permissionGuard],
        data: { permission: 'ViewConfiguration' },
        loadChildren: () => import('./features/whatsapp/whatsapp.routes').then((m) => m.whatsappRoutes),
      },
      {
        path: 'email-templates',
        title: 'Plantillas Email',
        canActivate: [permissionGuard],
        data: { permission: 'ViewConfiguration' },
        loadComponent: () => import('./features/email-templates/email-templates-page.component'),
      },
      {
        path: 'notification-settings',
        title: 'Notificaciones',
        canActivate: [permissionGuard],
        data: { permission: 'ViewConfiguration' },
        loadComponent: () => import('./features/notification-settings/notification-settings-page.component'),
      },
      {
        path: 'users',
        title: 'Usuarios',
        canActivate: [permissionGuard],
        data: { permission: 'ViewUsers' },
        loadChildren: () => import('./features/users/users.routes').then((m) => m.usersRoutes),
      },
      {
        path: 'settings',
        title: 'Configuración',
        canActivate: [permissionGuard],
        data: { permission: 'ViewConfiguration' },
        loadChildren: () => import('./features/settings/settings.routes').then((m) => m.settingsRoutes),
      },
      {
        path: 'audit',
        title: 'Auditoría',
        canActivate: [permissionGuard],
        data: { permission: 'ViewAuditLogs' },
        loadChildren: () => import('./features/audit/audit.routes').then((m) => m.auditRoutes),
      },
    ],
  },
  {
    path: '**',
    title: 'Página no encontrada',
    loadComponent: () => import('./features/auth/presentation/not-found-page/not-found-page.component'),
  },
];
