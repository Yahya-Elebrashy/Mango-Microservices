import { Routes } from '@angular/router';

export const cartRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./components/cart-index/cart-index')
        .then(m => m.CartIndex),
  },
  {
    path: 'checkout',
    loadComponent: () =>
      import('./components/checkout/checkout')
        .then(m => m.Checkout),
  },
  {
    path: 'confirmation',
    loadComponent: () =>
      import('./components/confirmation/confirmation')
        .then(m => m.Confirmation),
  },
];