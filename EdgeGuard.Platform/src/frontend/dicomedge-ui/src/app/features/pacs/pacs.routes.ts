import { Routes } from '@angular/router';

export const pacsRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./presentation/pacs-page/pacs-page'),
  },
];
