import { Routes } from '@angular/router';
import { adminGuard } from './core/guards/admin-guard';
import { authGuard } from './core/guards/auth-guard';

export const routes: Routes = [
    {
        path: '',
        loadChildren: () =>
            import('./features/home/home.routes').then(m => m.homeRoutes),
    },
    {
        path: 'auth',
        loadChildren: () =>
            import('./features/auth/auth.routes').then(m => m.authRoutes),
    },
    {
        path: 'product',
        loadChildren: () =>
            import('./features/products/product.routes').then(m => m.productRoutes),
        canActivate: [adminGuard],
    },
    {
        path: 'cart',
        loadChildren: () =>
            import('./features/cart/cart.routes').then(m => m.cartRoutes),
        canActivate: [authGuard],
    },
    {
        path: 'order',
        loadChildren: () =>
            import('./features/orders/order.routes').then(m => m.orderRoutes),
        canActivate: [authGuard],
    },
    {
        path: 'coupon',
        loadChildren: () =>
            import('./features/coupons/coupon.routes').then(m => m.couponRoutes),
        canActivate: [adminGuard],
    },
    {
        path: '**',
        redirectTo: '',
    },
]

