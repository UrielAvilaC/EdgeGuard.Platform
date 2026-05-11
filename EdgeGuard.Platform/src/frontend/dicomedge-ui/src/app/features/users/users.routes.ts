import { Routes } from '@angular/router';

export const usersRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./presentation/users-page/users-page.component'),
  },
  {
    path: ':id',
    loadComponent: () => import('./presentation/user-detail-page/user-detail-page.component'),
  },
];
