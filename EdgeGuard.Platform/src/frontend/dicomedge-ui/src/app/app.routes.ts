import { Routes } from '@angular/router';

import { MainLayout } from './layout/main-layout/main-layout.component';
import { authGuard } from './core/auth/guards/auth.guard';
import { permissionGuard } from './core/auth/guards/permission.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/auth/presentation/login-page/login-page.component'),
  },
  {
    path: 'unauthorized',
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
        loadChildren: () => import('./features/dashboard/dashboard.routes').then((m) => m.dashboardRoutes),
      },
      {
        path: 'studies',
        canActivate: [permissionGuard],
        data: { permission: 'ViewStudies' },
        loadChildren: () => import('./features/studies/studies.routes').then((m) => m.studiesRoutes),
      },
      {
        path: 'patients',
        canActivate: [permissionGuard],
        data: { permission: 'ViewStudies' },
        loadChildren: () => import('./features/patients/patients.routes').then((m) => m.patientsRoutes),
      },
      {
        path: 'nodes',
        canActivate: [permissionGuard],
        data: { permission: 'ViewNodes' },
        loadChildren: () => import('./features/nodes/nodes.routes').then((m) => m.nodesRoutes),
      },
      {
        path: 'pacs',
        canActivate: [permissionGuard],
        data: { permission: 'ViewConfiguration' },
        loadChildren: () => import('./features/pacs/pacs.routes').then((m) => m.pacsRoutes),
      },
      {
        path: 'routing-rules',
        canActivate: [permissionGuard],
        data: { permission: 'ManageRoutingRules' },
        loadChildren: () => import('./features/routing-rules/routing-rules.routes').then((m) => m.routingRulesRoutes),
      },
      {
        path: 'hl7',
        canActivate: [permissionGuard],
        data: { permission: 'ViewQueue' },
        loadChildren: () => import('./features/hl7/hl7.routes').then((m) => m.hl7Routes),
      },
      {
        path: 'queue',
        canActivate: [permissionGuard],
        data: { permission: 'ViewQueue' },
        loadChildren: () => import('./features/queue/queue.routes').then((m) => m.queueRoutes),
      },
      {
        path: 'whatsapp',
        canActivate: [permissionGuard],
        data: { permission: 'ViewConfiguration' },
        loadChildren: () => import('./features/whatsapp/whatsapp.routes').then((m) => m.whatsappRoutes),
      },
      {
        path: 'users',
        canActivate: [permissionGuard],
        data: { permission: 'ViewUsers' },
        loadChildren: () => import('./features/users/users.routes').then((m) => m.usersRoutes),
      },
      {
        path: 'settings',
        canActivate: [permissionGuard],
        data: { permission: 'ViewConfiguration' },
        loadChildren: () => import('./features/settings/settings.routes').then((m) => m.settingsRoutes),
      },
      {
        path: 'audit',
        canActivate: [permissionGuard],
        data: { permission: 'ViewAuditLogs' },
        loadChildren: () => import('./features/audit/audit.routes').then((m) => m.auditRoutes),
      },
    ],
  },
  {
    path: '**',
    loadComponent: () => import('./features/auth/presentation/not-found-page/not-found-page.component'),
  },
];
