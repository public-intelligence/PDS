import { HttpInterceptorFn, HttpResponse } from '@angular/common/http';
import { tap } from 'rxjs';

/** Recursos de calendario que el API versiona con `ETag` (RF-LO-42). */
const CALENDAR_RESOURCE = /\/(calendario|calendarios-feriados)(\/|$)/;

/** Último `ETag` leído de cada recurso, por URL. */
const etagByUrl = new Map<string, string>();

/**
 * Concurrencia optimista (RF-LO-42): recuerda el `ETag` de cada GET de calendario y lo envía como
 * `If-Match` en el PUT o DELETE del mismo recurso. Si otro usuario lo cambió, el API responde 412.
 */
export const concurrencyInterceptor: HttpInterceptorFn = (req, next) => {
  if (!CALENDAR_RESOURCE.test(req.url)) return next(req);

  if (req.method === 'GET') {
    return next(req).pipe(
      tap((event) => {
        const etag = event instanceof HttpResponse ? event.headers.get('ETag') : null;
        if (etag) etagByUrl.set(req.url, etag);
      }),
    );
  }

  if (req.method !== 'PUT' && req.method !== 'DELETE') return next(req);

  // El ETag vale para un solo intento: tras escribir, el recurso tiene otra versión.
  const etag = etagByUrl.get(req.url);
  etagByUrl.delete(req.url);
  return next(etag ? req.clone({ setHeaders: { 'If-Match': etag } }) : req);
};
