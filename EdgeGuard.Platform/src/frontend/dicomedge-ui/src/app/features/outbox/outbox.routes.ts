import { Routes } from '@angular/router';

export const outboxRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./presentation/outbox-page/outbox-page.component'),
  },
];
