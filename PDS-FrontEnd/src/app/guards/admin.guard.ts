import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth/auth.service';

export const adminGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const permissions = authService.decodeToken()?.permissions || [];
  if (permissions.includes('planning:admin')) {
    return true;
  }

  router.navigate(['/mi-acceso']);
  return false;
};
