import { Routes } from '@angular/router';

export const nodesRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./presentation/nodes-page/nodes-page.component'),
  },
  {
    path: ':id',
    loadComponent: () => import('./presentation/node-detail-page/node-detail-page.component'),
  },
  {
    path: ':id/config',
    loadComponent: () => import('./presentation/node-config-page/node-config-page.component'),
  },
  {
    path: ':id/equipment',
    loadComponent: () => import('./presentation/node-equipment-page/node-equipment-page.component'),
  },
  {
    path: ':id/telemetry',
    loadComponent: () => import('./presentation/node-telemetry-page/node-telemetry-page.component'),
  },
];
