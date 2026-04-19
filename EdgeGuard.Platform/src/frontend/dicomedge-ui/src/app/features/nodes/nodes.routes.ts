import { Routes } from '@angular/router';

export const nodesRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./presentation/nodes-page/nodes-page'),
  },
];
