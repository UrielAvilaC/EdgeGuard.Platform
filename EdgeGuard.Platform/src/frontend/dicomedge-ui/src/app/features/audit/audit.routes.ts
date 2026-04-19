import { Routes } from '@angular/router';

export const auditRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./presentation/audit-page/audit-page'),
  },
];
