import { HttpContextToken } from '@angular/common/http';

/**
 * Marca una petición para que el authInterceptor NO adjunte el token ni dispare el refresh/logout.
 * Lo usan los servicios públicos (organigrama) para evitar forzar el login del usuario anónimo.
 */
export const SKIP_AUTH = new HttpContextToken<boolean>(() => false);
