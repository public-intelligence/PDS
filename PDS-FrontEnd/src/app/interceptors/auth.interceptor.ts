import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, switchMap, throwError } from 'rxjs';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth/auth.service';
import { SKIP_AUTH } from './http-context';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const accessToken = authService.getAccessToken();

  // El organigrama público es anónimo y de solo lectura: no se adjunta token ni se fuerza el login.
  if (req.context.get(SKIP_AUTH) || req.url.includes('/auth') || req.url.includes('/public/')) {
    return next(req);
  }

  const authReq = req.clone({
    setHeaders: {
      Authorization: `Bearer ${accessToken}`,
    },
  });

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        return authService.onRefreshToken().pipe(
          switchMap(() => {
            const newAccessToken = authService.getAccessToken();
            const updateHeaders = req.headers.set('Authorization', `Bearer ${newAccessToken}`);
            const newAuthReq = req.clone({ headers: updateHeaders });
            return next(newAuthReq);
          }),
        );
      }
      return throwError(() => error);
    }),
  );
};
