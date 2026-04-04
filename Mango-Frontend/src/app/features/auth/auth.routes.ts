import { Routes } from '@angular/router';

export const authRoutes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./components/login/login').then(m => m.Login),
  },
  {
    path: 'register',
    loadComponent: () =>
      import('./components/register/register').then(m => m.Register),
  },
  {
    path: '',
    redirectTo: 'login',
    pathMatch: 'full',
  },
];