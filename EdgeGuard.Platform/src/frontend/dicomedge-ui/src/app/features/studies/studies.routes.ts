import { Routes } from '@angular/router';

export const studiesRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./presentation/studies-page/studies-page'),
  },
  {
    path: ':id',
    loadComponent: () => import('./presentation/study-detail-page/study-detail-page'),
  },
];
