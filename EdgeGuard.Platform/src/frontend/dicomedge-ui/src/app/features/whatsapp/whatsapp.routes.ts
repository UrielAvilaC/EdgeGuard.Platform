import { Routes } from '@angular/router';

export const whatsappRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./presentation/whatsapp-page/whatsapp-page.component'),
  },
];
