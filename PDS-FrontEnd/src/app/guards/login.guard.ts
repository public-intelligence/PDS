import { isPlatformServer } from '@angular/common';
import { inject, PLATFORM_ID } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

export const loginGuard: CanActivateFn = () => {
  const router = inject(Router);
  const platformId = inject(PLATFORM_ID);

  if (isPlatformServer(platformId)) {
    return false;
  }

  const token = sessionStorage.getItem('accessToken');

  if (token) {
    router.navigate(['/dashboard']);
    return false;
  } else {
    return true;
  }
};
