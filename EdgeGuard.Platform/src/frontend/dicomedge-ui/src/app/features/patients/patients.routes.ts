import { Routes } from '@angular/router';

export const patientsRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./presentation/patients-page/patients-page'),
  },
  {
    path: ':id',
    loadComponent: () => import('./presentation/patient-detail-page/patient-detail-page'),
  },
];
