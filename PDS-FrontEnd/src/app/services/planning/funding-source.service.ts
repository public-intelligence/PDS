import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { FundingSourceDto, CreateFundingSourceDto, UpdateFundingSourceDto } from '../../modules/planning/data/planning-api.models';

const MOCK_FUNDING_SOURCES: FundingSourceDto[] = [
  { id: 'fs-1', organizationId: 1, code: '1', name: 'Fuentes internas', description: 'Recursos que provienen del propio Estado o de la gestión financiera interna de la institución.', isActive: true, createdAt: new Date().toISOString(), updatedAt: new Date().toISOString() },
  { id: 'fs-10', organizationId: 1, code: '10', name: 'Fondo General', description: 'Recursos del presupuesto nacional que financian el gasto general de la institución.', isActive: true, createdAt: new Date().toISOString(), updatedAt: new Date().toISOString() },
  { id: 'fs-20', organizationId: 1, code: '20', name: 'Fondos con destino específico', description: 'Ingresos internos que deben usarse para un fin concreto definido por norma o por su origen.', isActive: true, createdAt: new Date().toISOString(), updatedAt: new Date().toISOString() },
  { id: 'fs-30', organizationId: 1, code: '30', name: 'Fondos propios', description: 'Recursos generados por la propia entidad, como cobros, ventas, tasas o servicios.', isActive: true, createdAt: new Date().toISOString(), updatedAt: new Date().toISOString() },
  { id: 'fs-40', organizationId: 1, code: '40', name: 'Transferencias', description: 'Recursos recibidos desde otra entidad pública.', isActive: true, createdAt: new Date().toISOString(), updatedAt: new Date().toISOString() },
  { id: 'fs-50', organizationId: 1, code: '50', name: 'Crédito interno', description: 'Financiamiento obtenido dentro del país mediante deuda interna.', isActive: true, createdAt: new Date().toISOString(), updatedAt: new Date().toISOString() },
  { id: 'fs-90', organizationId: 1, code: '90', name: 'Fondos de terceros', description: 'Recursos que la institución administra, pero que pertenecen a otra persona o entidad.', isActive: true, createdAt: new Date().toISOString(), updatedAt: new Date().toISOString() },
  { id: 'fs-91', organizationId: 1, code: '91', name: 'Pasivos financieros', description: 'Recursos asociados a obligaciones financieras registradas como pasivos.', isActive: true, createdAt: new Date().toISOString(), updatedAt: new Date().toISOString() },
  { id: 'fs-99', organizationId: 1, code: '99', name: 'Fondos contables', description: 'Recursos de naturaleza contable o de registro, no necesariamente ligados a ingreso operativo directo.', isActive: true, createdAt: new Date().toISOString(), updatedAt: new Date().toISOString() },
  { id: 'fs-2', organizationId: 1, code: '2', name: 'Fuentes externas', description: 'Recursos que provienen del exterior del país.', isActive: true, createdAt: new Date().toISOString(), updatedAt: new Date().toISOString() },
  { id: 'fs-60', organizationId: 1, code: '60', name: 'Crédito externo', description: 'Préstamos o financiamiento reembolsable proveniente del exterior.', isActive: true, createdAt: new Date().toISOString(), updatedAt: new Date().toISOString() },
  { id: 'fs-70', organizationId: 1, code: '70', name: 'Donación externa', description: 'Cooperación externa no reembolsable.', isActive: true, createdAt: new Date().toISOString(), updatedAt: new Date().toISOString() }
];

@Injectable({
  providedIn: 'root'
})
export class FundingSourceService {
  private baseUrl = `${environment.apiUrl}planning/funding-sources`;

  constructor(private http: HttpClient) {}

  list(q?: string, onlyActive?: boolean): Observable<FundingSourceDto[]> {
    let params = new HttpParams();
    if (q) params = params.set('q', q);
    if (onlyActive !== undefined && onlyActive !== null) params = params.set('onlyActive', onlyActive.toString());

    return this.http.get<FundingSourceDto[]>(this.baseUrl, { params }).pipe(
      catchError(() => {
        let items = [...MOCK_FUNDING_SOURCES];
        if (q) {
          const term = q.toLowerCase();
          items = items.filter(f => f.code.toLowerCase().includes(term) || f.name.toLowerCase().includes(term));
        }
        if (onlyActive) {
          items = items.filter(f => f.isActive);
        }
        return of(items);
      })
    );
  }

  getById(id: string): Observable<FundingSourceDto> {
    return this.http.get<FundingSourceDto>(`${this.baseUrl}/${id}`).pipe(
      catchError(() => {
        const found = MOCK_FUNDING_SOURCES.find(f => f.id === id);
        return of(found || MOCK_FUNDING_SOURCES[0]);
      })
    );
  }

  create(dto: CreateFundingSourceDto): Observable<FundingSourceDto> {
    return this.http.post<FundingSourceDto>(this.baseUrl, dto).pipe(
      catchError(() => {
        const newObj: FundingSourceDto = {
          id: `fs-${Date.now()}`,
          organizationId: 1,
          code: dto.code,
          name: dto.name,
          description: dto.description || null,
          isActive: dto.isActive ?? true,
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString()
        };
        MOCK_FUNDING_SOURCES.unshift(newObj);
        return of(newObj);
      })
    );
  }

  update(id: string, dto: UpdateFundingSourceDto): Observable<FundingSourceDto> {
    return this.http.put<FundingSourceDto>(`${this.baseUrl}/${id}`, dto).pipe(
      catchError(() => {
        const found = MOCK_FUNDING_SOURCES.find(f => f.id === id);
        if (found) {
          found.code = dto.code;
          found.name = dto.name;
          found.description = dto.description || null;
          found.isActive = dto.isActive;
          found.updatedAt = new Date().toISOString();
        }
        return of(found || (dto as any));
      })
    );
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`).pipe(
      catchError(() => {
        const idx = MOCK_FUNDING_SOURCES.findIndex(f => f.id === id);
        if (idx >= 0) MOCK_FUNDING_SOURCES.splice(idx, 1);
        return of(void 0);
      })
    );
  }
}
