import { Routes } from '@angular/router';

export const hl7Routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./presentation/hl7-page/hl7-page'),
  },
];
