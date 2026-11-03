import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { UnifiedCodeDto, UnifiedCodeDetailDto } from '../../modules/planning/data/planning-api.models';

export interface UnifiedCodeFilters {
  level?: string;
  parentId?: string;
  q?: string;
  enabled?: boolean;
  pageNumber?: number;
  pageSize?: number;
}

const MOCK_UNSPSC_CODES: UnifiedCodeDto[] = [
  { id: 'u-43211503', code: '43211503', name: 'Computadores portátiles (Laptops)', level: 'Clase', parentId: null, englishName: 'Laptop computers', institutionallyEnabled: true, childCount: 0 },
  { id: 'u-43211902', code: '43211902', name: 'Monitores para computador', level: 'Clase', parentId: null, englishName: 'Computer monitors', institutionallyEnabled: true, childCount: 0 },
  { id: 'u-43212105', code: '43212105', name: 'Impresoras láser', level: 'Clase', parentId: null, englishName: 'Laser printers', institutionallyEnabled: true, childCount: 0 },
  { id: 'u-14111507', code: '14111507', name: 'Papel para impresora y fotocopia', level: 'Clase', parentId: null, englishName: 'Printer paper', institutionallyEnabled: true, childCount: 0 },
  { id: 'u-56112102', code: '56112102', name: 'Sillas de oficina', level: 'Clase', parentId: null, englishName: 'Office chairs', institutionallyEnabled: true, childCount: 0 },
  { id: 'u-25101507', code: '25101507', name: 'Camionetas de carga liviana', level: 'Clase', parentId: null, englishName: 'Light trucks', institutionallyEnabled: true, childCount: 0 },
  { id: 'u-72101511', code: '72101511', name: 'Servicios de mantenimiento de aire acondicionado', level: 'Clase', parentId: null, englishName: 'AC maintenance services', institutionallyEnabled: true, childCount: 0 },
  { id: 'u-44103103', code: '44103103', name: 'Cartuchos de tóner', level: 'Clase', parentId: null, englishName: 'Toner cartridges', institutionallyEnabled: true, childCount: 0 }
];

@Injectable({ providedIn: 'root' })
export class UnifiedCodeService {
  private http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}unified-codes`;

  list(filters: UnifiedCodeFilters = {}): Observable<UnifiedCodeDto[]> {
    let params = new HttpParams();
    if (filters.level) params = params.set('level', filters.level);
    if (filters.parentId) params = params.set('parentId', filters.parentId);
    if (filters.q) params = params.set('q', filters.q);
    if (filters.enabled != null) params = params.set('enabled', String(filters.enabled));
    if (filters.pageNumber) params = params.set('pageNumber', String(filters.pageNumber));
    if (filters.pageSize) params = params.set('pageSize', String(filters.pageSize));

    return this.http.get<UnifiedCodeDto[]>(this.base, { params }).pipe(
      catchError(() => {
        let items = [...MOCK_UNSPSC_CODES];
        if (filters.q) {
          const query = filters.q.toLowerCase();
          items = items.filter(c => c.code.toLowerCase().includes(query) || c.name.toLowerCase().includes(query));
        }
        const page = filters.pageNumber || 1;
        const pageSize = filters.pageSize || 50;
        const paged = items.slice((page - 1) * pageSize, page * pageSize);
        return of(paged);
      })
    );
  }

  getById(id: string): Observable<UnifiedCodeDetailDto> {
    return this.http.get<UnifiedCodeDetailDto>(`${this.base}/${id}`).pipe(
      catchError(() => {
        const found = MOCK_UNSPSC_CODES.find(c => c.id === id) || MOCK_UNSPSC_CODES[0];
        const detail: UnifiedCodeDetailDto = {
          ...found,
          definition: 'Definición de clase UNSPSC de la DGCP',
          sourceRole: 'Primary',
          includedInMaster: true,
          legacyExpenseObjectCode: null,
          legacyExpenseObjectName: null,
          source: 'DGCP',
          aliases: []
        };
        return of(detail);
      })
    );
  }

  create(dto: Partial<UnifiedCodeDto>): Observable<UnifiedCodeDto> {
    return this.http.post<UnifiedCodeDto>(this.base, dto).pipe(
      catchError(() => {
        const newCode: UnifiedCodeDto = {
          id: `u-${dto.code || Date.now()}`,
          code: dto.code || '43219999',
          name: dto.name || 'Nuevo Código UNSPSC',
          level: dto.level || 'Clase',
          parentId: null,
          englishName: null,
          institutionallyEnabled: dto.institutionallyEnabled ?? true,
          childCount: 0
        };
        MOCK_UNSPSC_CODES.unshift(newCode);
        return of(newCode);
      })
    );
  }

  update(id: string, dto: Partial<UnifiedCodeDto>): Observable<UnifiedCodeDto> {
    return this.http.put<UnifiedCodeDto>(`${this.base}/${id}`, dto).pipe(
      catchError(() => {
        const found = MOCK_UNSPSC_CODES.find(c => c.id === id);
        if (found) {
          if (dto.code) found.code = dto.code;
          if (dto.name) found.name = dto.name;
          if (dto.level) found.level = dto.level;
          if (dto.institutionallyEnabled !== undefined) found.institutionallyEnabled = dto.institutionallyEnabled;
        }
        return of(found || (dto as UnifiedCodeDto));
      })
    );
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`).pipe(
      catchError(() => {
        const idx = MOCK_UNSPSC_CODES.findIndex(c => c.id === id);
        if (idx >= 0) MOCK_UNSPSC_CODES.splice(idx, 1);
        return of(void 0);
      })
    );
  }
}
