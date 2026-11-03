import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

// El refresh token vive en una cookie HttpOnly de la API (otro origen): token, refresh y logout
// deben viajar con credenciales, o el navegador descarta o no envía esa cookie.
describe('AuthService (sesión con cookie HttpOnly)', () => {
  let service: AuthService;
  let backend: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AuthService);
    backend = TestBed.inject(HttpTestingController);
    localStorage.clear();
  });

  afterEach(() => backend.verify());

  it('canjea el código con credenciales para recibir la cookie de refresh', () => {
    service.completeGobIdOAuthCallback('code-1', '7').subscribe();

    const req = backend.expectOne(`${environment.apiUrl}auth/token`);
    expect(req.request.withCredentials).toBe(true);
    expect(req.request.body).toEqual({ code: 'code-1', organizationApplicationId: '7' });
    req.flush({ accessToken: 'AT-1', mustChangePassword: false });

    expect(localStorage.getItem('accessToken')).toBe('AT-1');
  });

  it('refresca con credenciales', () => {
    service.onRefreshToken().subscribe();

    const req = backend.expectOne(`${environment.apiUrl}auth/refresh`);
    expect(req.request.withCredentials).toBe(true);
    req.flush({ accessToken: 'AT-2', mustChangePassword: false });

    expect(localStorage.getItem('accessToken')).toBe('AT-2');
  });

  it('al cerrar sesión pide a la API revocar la cookie y borra el access token', () => {
    localStorage.setItem('accessToken', 'AT-3');

    service.onLogout();

    const req = backend.expectOne(`${environment.apiUrl}auth/logout`);
    expect(req.request.method).toBe('POST');
    expect(req.request.withCredentials).toBe(true);
    expect(localStorage.getItem('accessToken')).toBeNull();
    req.flush(null, { status: 204, statusText: 'No Content' });
  });
});
