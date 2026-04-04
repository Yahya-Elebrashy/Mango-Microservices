import { CanActivateFn, Router } from '@angular/router';
import { AuthState } from '../services/auth-state.service';
import { inject } from '@angular/core';
import { SD } from '../../constants/app.constants';

export const adminGuard: CanActivateFn = (route, state) => {
  const authState = inject(AuthState);
  const router = inject(Router);

  if (authState.isLoggedIn && authState.currentRole === SD.ROLE_ADMIN) {
    return true;
  }

  router.navigate(['/']);
  return false;
};
