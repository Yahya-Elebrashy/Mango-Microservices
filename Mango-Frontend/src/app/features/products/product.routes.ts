import { Routes } from '@angular/router';

export const productRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./components/product-list/product-list')
        .then(m => m.ProductList),
  },
  {
    path: 'create',
    loadComponent: () =>
      import('./components/product-create/product-create')
        .then(m => m.ProductCreate),
  },
  {
    path: 'edit/:id',
    loadComponent: () =>
      import('./components/product-edit/product-edit')
        .then(m => m.ProductEdit),
  },
  {
    path: 'delete/:id',
    loadComponent: () =>
      import('./components/product-delete/product-delete')
        .then(m => m.ProductDelete),
  },
];