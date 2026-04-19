import { Routes } from '@angular/router';

export const queueRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./presentation/queue-page/queue-page'),
  },
];
