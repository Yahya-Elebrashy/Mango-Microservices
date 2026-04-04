import { Routes } from '@angular/router';

export const orderRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./components/order-list/order-list')
        .then(m => m.OrderList),
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./components/order-detail/order-detail')
        .then(m => m.OrderDetail),
  },
];