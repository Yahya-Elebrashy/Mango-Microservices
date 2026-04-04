import { Routes } from '@angular/router';
import { authGuard } from '../../core/guards/auth-guard';
export const homeRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./components/home/home').then(m => m.Home),
  },
  {
    path: 'details/:id',
    loadComponent: () =>
      import('./components/detail/detail').then(m => m.Detail),
    canActivate: [authGuard],
  },
];