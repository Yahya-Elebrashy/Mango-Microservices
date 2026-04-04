import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { catchError, throwError } from 'rxjs';

export const jwtInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const toastr = inject(ToastrService);

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      switch (err.status) {
        case 401:
          toastr.error('Unauthorized. Please log in.');
          router.navigate(['/auth/login']);
          break;
        case 403:
          toastr.error('Access denied.');
          break;
        case 404:
          toastr.error('Resource not found.');
          break;
        case 500:
          toastr.error('Server error. Please try again later.');
          break;
        default:
          toastr.error(err.message || 'Something went wrong.');
      }
      return throwError(() => err);
    })
  );
};
