import { isPlatformServer } from '@angular/common';
import { inject, PLATFORM_ID } from '@angular/core';
import { CanActivateFn } from '@angular/router';
import { environment } from '../../environments/environment';
import { AuthService } from '../services/auth/auth.service';

export const authGuard: CanActivateFn = () => {
  const platformId = inject(PLATFORM_ID);
  const authService = inject(AuthService);
  const loginPth = `${environment.gobIdPortalOrigin}/app/${environment.gobIdApplicationId}/login`;

  if (isPlatformServer(platformId)) {
    return false;
  }

  // El token lo administra AuthService (localStorage). Leer el storage directamente aquí
  // hacía que el guard nunca viera la sesión iniciada.
  const token = authService.getAccessToken();

  if (!token) {
    window.location.href = loginPth;
    return false;
  } else {
    return true;
  }
};
