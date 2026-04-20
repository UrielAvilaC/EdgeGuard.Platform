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
];
