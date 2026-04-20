import { Routes } from '@angular/router';

export const routingRulesRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./presentation/routing-rules-page/routing-rules-page.component'),
  },
];
