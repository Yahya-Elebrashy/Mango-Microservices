import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthState } from '../services/auth-state.service';

export const authGuard: CanActivateFn = (route, state) => {
  const authState = inject(AuthState);
  const router = inject(Router);

  if (authState.isLoggedIn) {
    return true;
  }

  router.navigate(['/auth/login']);
  return false;
};
