import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { jwtDecode } from 'jwt-decode';
import { AuthTokenResponse, JwtPayload, RefreshTokenResponse } from '../../models/auth.model';
import { catchError, finalize, Observable, tap, throwError } from 'rxjs';
import { HttpClient } from '@angular/common/http';

/** Administrador de calendario de toda la organización (RF-LO-39). */
export const CALENDAR_MANAGE_PERMISSION = 'localities:calendar:manage';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl;
  private gobIdUrl = `${environment.gobIdPortalOrigin}/app/${environment.gobIdApplicationId}/login`;

  decodeToken(): JwtPayload | null {
    try {
      const decoded: JwtPayload = jwtDecode(this.getAccessToken());
      return decoded;
    } catch {
      return null;
    }
  }

  hasPermission(permission: string): boolean {
    return this.decodeToken()?.permissions?.includes(permission) ?? false;
  }

  /** El servidor es la autoridad: esto solo decide qué se muestra. */
  isCalendarAdmin(): boolean {
    return this.hasPermission(CALENDAR_MANAGE_PERMISSION);
  }

  /**
   * Redirige el navegador al login hospedado por GobID.
   * @returns false si falta configuración (no se navega).
   */
  redirectToGobIdLogin(): void {
    window.location.href = this.gobIdUrl;
  }

  completeGobIdOAuthCallback(
    code: string,
    organizationApplicationId: string,
  ): Observable<AuthTokenResponse> {
    return this.http
      // withCredentials: sin esto el navegador descarta la cookie HttpOnly con el refresh token
      // que la API (otro origen) devuelve, y la sesión no podría renovarse.
      .post<AuthTokenResponse>(
        `${this.apiUrl}auth/token`,
        { code, organizationApplicationId },
        { withCredentials: true },
      )
      .pipe(
        tap((response) => {
          localStorage.setItem('accessToken', response.accessToken);
        }),
        catchError((error) => {
          return throwError(() => new Error(error?.error?.message || 'An unknown error occurred'));
        }),
      );
  }

  /**
   * Refresca el token del usuario usando el refresh token enviado automáticamente via HTTP-only cookie.
   * En caso de que el refreshToken expire, el usuario será deslogeado.
   * @returns
   */
  onRefreshToken() {
    return this.http
      .post<RefreshTokenResponse>(`${this.apiUrl}auth/refresh`, {}, { withCredentials: true })
      .pipe(
        tap((response) => {
          localStorage.setItem('accessToken', response.accessToken);
        }),
        catchError((error) => {
          this.onLogout();
          return throwError(() => new Error(error?.error?.title || 'An unknown error occurred'));
        }),
      );
  }

  /**
   * Cierra la sesión: la API revoca el refresh token en GobID y borra su cookie HttpOnly.
   * La redirección al login ocurre aunque la llamada falle.
   */
  onLogout(): void {
    localStorage.removeItem('accessToken');
    this.http
      .post(`${this.apiUrl}auth/logout`, {}, { withCredentials: true })
      .pipe(finalize(() => (window.location.href = this.gobIdUrl)))
      .subscribe({ error: () => undefined });
  }

  /**
   * Consigue el accessToken del usuario.
   * @returns Retorna el accessToken.
   */
  getAccessToken() {
    return localStorage.getItem('accessToken') || '';
  }
}
