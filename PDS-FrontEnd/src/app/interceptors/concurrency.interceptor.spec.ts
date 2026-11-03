import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { concurrencyInterceptor } from './concurrency.interceptor';

describe('concurrencyInterceptor', () => {
  let http: HttpClient;
  let backend: HttpTestingController;
  const url = '/api/v1/catalogos/calendarios-feriados/abc';

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([concurrencyInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
  });

  it('envía como If-Match el ETag del último GET y lo usa una sola vez', () => {
    http.get(url).subscribe();
    backend.expectOne(url).flush({}, { headers: { ETag: '"v42"' } });

    http.put(url, {}).subscribe();
    const first = backend.expectOne(url);
    expect(first.request.headers.get('If-Match')).toBe('"v42"');
    first.flush({});

    http.put(url, {}).subscribe();
    expect(backend.expectOne(url).request.headers.has('If-Match')).toBe(false);
  });

  it('no envía If-Match sin un GET previo ni toca otros recursos', () => {
    http.delete('/api/v1/catalogos/calendarios-feriados/sin-get').subscribe();
    expect(
      backend.expectOne('/api/v1/catalogos/calendarios-feriados/sin-get').request.headers.has('If-Match'),
    ).toBe(false);

    http.get('/api/v1/catalogos/servicios/1').subscribe();
    backend.expectOne('/api/v1/catalogos/servicios/1').flush({}, { headers: { ETag: '"x"' } });
    http.put('/api/v1/catalogos/servicios/1', {}).subscribe();
    expect(backend.expectOne('/api/v1/catalogos/servicios/1').request.headers.has('If-Match')).toBe(false);
  });
});
