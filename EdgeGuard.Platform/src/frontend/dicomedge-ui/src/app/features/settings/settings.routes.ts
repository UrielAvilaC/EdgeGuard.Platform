import { Routes } from '@angular/router';

export const settingsRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./presentation/settings-page/settings-page.component'),
  },
  {
    path: 'node-config',
    loadComponent: () => import('./presentation/node-config-page/node-config-page.component'),
  },
];
