import { Routes } from '@angular/router';

export const couponRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./components/coupon-list/coupon-list')
        .then(m => m.CouponList),
  },
  {
    path: 'create',
    loadComponent: () =>
      import('./components/coupon-create/coupon-create')
        .then(m => m.CouponCreate),
  },
  {
    path: 'delete/:id',
    loadComponent: () =>
      import('./components/coupon-delete/coupon-delete')
        .then(m => m.CouponDelete),
  },
];