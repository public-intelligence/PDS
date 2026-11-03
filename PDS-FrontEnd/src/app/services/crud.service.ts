import { inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../environments/environment.development';

export interface PaginationMeta {
  currentPage: number;
  pageCount: number;
  pageSize: number;
  total: number;
}

export interface PagedResult<T> {
  items: T[];
  pagination: PaginationMeta;
}

export abstract class CrudService<TResponse, TDomain = TResponse, TUpsert = Partial<TResponse>> {
  protected http = inject(HttpClient);

  protected readonly endpoint = environment.apiUrl;

  protected adapt(item: TResponse): TDomain {
    return item as unknown as TDomain;
  }

  getAll(
    pageNumber = 1,
    pageSize = 10,
    filters: Record<string, string | number | boolean | null | undefined> = {},
  ): Observable<PagedResult<TDomain>> {
    let params = new HttpParams().set('pageNumber', pageNumber).set('pageSize', pageSize);
    for (const [key, value] of Object.entries(filters)) {
      if (value != null && value !== '') {
        params = params.set(key, String(value));
      }
    }
    return this.http.get<TResponse[]>(this.endpoint, { params, observe: 'response' }).pipe(
      map((response) => {
        return {
          items: (response.body ?? []).map((item) => this.adapt(item)),
          pagination: {
            currentPage: Number(response.headers.get('x-current-page')) || 1,
            pageCount: Number(response.headers.get('x-page-count')) || 0,
            pageSize: Number(response.headers.get('x-page-size')) || pageSize,
            total: Number(response.headers.get('x-total')) || 0,
          },
        };
      }),
    );
  }

  getById(id: string | number): Observable<TDomain> {
    return this.http.get<TResponse>(`${this.endpoint}/${id}`).pipe(map((item) => this.adapt(item)));
  }

  create(body: TUpsert): Observable<TDomain | null> {
    return this.http
      .post<TResponse | null>(this.endpoint, body)
      .pipe(map((item) => (item != null ? this.adapt(item) : null)));
  }

  update(id: string | number, body: TUpsert): Observable<TDomain> {
    return this.http
      .put<TResponse>(`${this.endpoint}/${id}`, body)
      .pipe(map((item) => this.adapt(item)));
  }

  delete(id: string | number): Observable<void> {
    return this.http.delete<void>(`${this.endpoint}/${id}`);
  }

  searchPaged(q: string, pageNumber = 1, pageSize = 10): Observable<PagedResult<TDomain>> {
    const params = new HttpParams()
      .set('q', q)
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);
    return this.http.get<TResponse[]>(`${this.endpoint}/search`, { params, observe: 'response' }).pipe(
      map((response) => ({
        items: (response.body ?? []).map((item) => this.adapt(item)),
        pagination: {
          currentPage: Number(response.headers.get('x-current-page')) || 1,
          pageCount: Number(response.headers.get('x-page-count')) || 0,
          pageSize: Number(response.headers.get('x-page-size')) || pageSize,
          total: Number(response.headers.get('x-total')) || 0,
        },
      })),
    );
  }

  search(): Observable<TDomain[]> {
    return this.http
      .get<TResponse[]>(this.endpoint)
      .pipe(map((response) => response.map((item) => this.adapt(item))));
  }
}
