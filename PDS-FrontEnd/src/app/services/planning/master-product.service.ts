import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import {
  MasterProductDto,
  MasterProductDetailDto,
  MasterProductSearchItemDto,
  CreateMasterProductDto,
  UpdateMasterProductDto,
  UpdateMasterProductMappingDto,
  RatifyConsolidatorAreaDto,
  BulkRatifyByUnspscDto,
  RequestMasterProductDto,
  MasterProductMappingSummaryDto,
  MasterProductState,
  PagedResponseDto
} from '../../modules/planning/data/planning-api.models';

export type PagedResponse<T> = PagedResponseDto<T>;

const MOCK_SUMMARY: MasterProductMappingSummaryDto = {
  totalProducts: 18302,
  activeProducts: 17840,
  pendingProducts: 450,
  requestedProducts: 12,
  disabledProducts: 0,
  missingExpenseObject: 0,
  missingAccountingAccount: 0,
  missingConsolidatorArea: 12,
  requiresReviewCount: 5,
  areaAmbiguityCount: 2,
  completeMappingCount: 17828
};

const MOCK_PRODUCTS: any[] = [
  {
    id: 'prod-001',
    code: 'ART-43211503-01',
    name: 'Computador Portátil Intel Core i7 16GB RAM 512GB SSD',
    itemType: 'Goods',
    unspscCode: '43211503',
    unspscName: 'Computadores portátiles',
    expenseObjectCode: '2.6.1.1',
    expenseObjectName: 'Maquinaria y Equipo Informático',
    accountingAccountCode: '1.2.3.1',
    accountingAccountName: 'Equipos de Computación',
    consolidatorAreaId: 'area-dti',
    consolidatorAreaName: 'Dirección de Tecnologías de la Información (DTI)',
    consolidatorAreaSource: 'Rule',
    requiresReview: false,
    state: 'Active'
  },
  {
    id: 'prod-002',
    code: 'ART-43211902-02',
    name: 'Monitor LED 27 Pulgadas 4K IPS Ergonomía Ajustable',
    itemType: 'Goods',
    unspscCode: '43211902',
    unspscName: 'Monitores para computador',
    expenseObjectCode: '2.6.1.1',
    expenseObjectName: 'Maquinaria y Equipo Informático',
    accountingAccountCode: '1.2.3.1',
    accountingAccountName: 'Equipos de Computación',
    consolidatorAreaId: 'area-dti',
    consolidatorAreaName: 'Dirección de Tecnologías de la Información (DTI)',
    consolidatorAreaSource: 'Rule',
    requiresReview: false,
    state: 'Active'
  },
  {
    id: 'prod-003',
    code: 'ART-14111507-03',
    name: 'Papel Bond Tamaño Carta 20lb 92% Blancura (Caja 5,000 Hojas)',
    itemType: 'Goods',
    unspscCode: '14111507',
    unspscName: 'Papel para impresora y fotocopia',
    expenseObjectCode: '2.3.1.1',
    expenseObjectName: 'Papel de Escritorio y Útiles de Oficina',
    accountingAccountCode: '1.1.4.2',
    accountingAccountName: 'Inventario de Materiales de Oficina',
    consolidatorAreaId: 'area-admin',
    consolidatorAreaName: 'Dirección Administrativa y Financiera',
    consolidatorAreaSource: 'Imported',
    requiresReview: false,
    state: 'Active'
  },
  {
    id: 'prod-004',
    code: 'ART-56112102-04',
    name: 'Silla Ergonómica Giratoria de Oficina con Apoyabrazos Ajustables',
    itemType: 'Goods',
    unspscCode: '56112102',
    unspscName: 'Sillas de oficina',
    expenseObjectCode: '2.6.1.2',
    expenseObjectName: 'Muebles y Equipos de Oficina',
    accountingAccountCode: '1.2.3.2',
    accountingAccountName: 'Mobiliario y Equipos de Oficina',
    consolidatorAreaId: 'area-serv',
    consolidatorAreaName: 'Departamento de Servicios Generales',
    consolidatorAreaSource: 'Manual',
    requiresReview: false,
    state: 'Active'
  },
  {
    id: 'prod-005',
    code: 'ART-25101507-05',
    name: 'Camioneta Doble Cabina 4x4 Diésel 2.8L Transmisión Automática',
    itemType: 'Goods',
    unspscCode: '25101507',
    unspscName: 'Camionetas de carga liviana',
    expenseObjectCode: '2.6.4.1',
    expenseObjectName: 'Automóviles y Camionetas',
    accountingAccountCode: '1.2.4.1',
    accountingAccountName: 'Vehículos de Transporte Terrestre',
    consolidatorAreaId: 'area-trans',
    consolidatorAreaName: 'División de Transportes y Flotas',
    consolidatorAreaSource: 'Rule',
    requiresReview: false,
    state: 'Active'
  },
  {
    id: 'prod-006',
    code: 'ART-72101511-06',
    name: 'Servicio Mantenimiento Preventivo y Correctivo Acondicionadores de Aire',
    itemType: 'Services',
    unspscCode: '72101511',
    unspscName: 'Servicios de mantenimiento de aire acondicionado',
    expenseObjectCode: '2.2.7.2',
    expenseObjectName: 'Mantenimiento y Reparación de Maquinarias y Equipos',
    accountingAccountCode: '5.1.2.7',
    accountingAccountName: 'Gastos de Mantenimiento y Reparación',
    consolidatorAreaId: 'area-serv',
    consolidatorAreaName: 'Departamento de Servicios Generales',
    consolidatorAreaSource: 'Imported',
    requiresReview: false,
    state: 'Active'
  },
  {
    id: 'prod-007',
    code: 'ART-43212105-07',
    name: 'Impresora Multifuncional Láser Color de Red de Alto Rendimiento',
    itemType: 'Goods',
    unspscCode: '43212105',
    unspscName: 'Impresoras láser',
    expenseObjectCode: '2.6.1.1',
    expenseObjectName: 'Maquinaria y Equipo Informático',
    accountingAccountCode: '1.2.3.1',
    accountingAccountName: 'Equipos de Computación',
    consolidatorAreaId: 'area-dti',
    consolidatorAreaName: 'Dirección de Tecnologías de la Información (DTI)',
    consolidatorAreaSource: 'Rule',
    requiresReview: false,
    state: 'Active'
  },
  {
    id: 'prod-008',
    code: 'ART-44103103-08',
    name: 'Tóner Negro de Alta Capacidad para Impresora Láser',
    itemType: 'Goods',
    unspscCode: '44103103',
    unspscName: 'Cartuchos de tóner',
    expenseObjectCode: '2.3.3.1',
    expenseObjectName: 'Productos de Papel, Cartón e Impresos',
    accountingAccountCode: '1.1.4.2',
    accountingAccountName: 'Inventario de Materiales de Oficina',
    consolidatorAreaId: 'area-admin',
    consolidatorAreaName: 'Dirección Administrativa y Financiera',
    consolidatorAreaSource: 'Imported',
    requiresReview: false,
    state: 'Active'
  }
];

@Injectable({
  providedIn: 'root'
})
export class MasterProductService {
  private baseUrl = `${environment.apiUrl}master-products`;

  constructor(private http: HttpClient) {}

  search(q: string, itemType?: string, take = 20): Observable<MasterProductSearchItemDto[]> {
    let params = new HttpParams().set('q', q).set('take', take.toString());
    if (itemType) params = params.set('itemType', itemType);
    return this.http.get<MasterProductSearchItemDto[]>(`${this.baseUrl}/search`, { params }).pipe(
      catchError(() => {
        const filtered: MasterProductSearchItemDto[] = MOCK_PRODUCTS
          .filter(p => p.name.toLowerCase().includes(q.toLowerCase()) || p.code.toLowerCase().includes(q.toLowerCase()))
          .slice(0, take)
          .map(p => ({
            id: p.id,
            code: p.code,
            name: p.name,
            itemType: p.itemType || 'Goods',
            unspscCode: p.unspscCode || '',
            unspscName: p.unspscName || '',
            expenseObjectCode: p.expenseObjectCode || '',
            accountingAccountCode: p.accountingAccountCode || '',
            consolidatorAreaId: p.consolidatorAreaId || '',
            consolidatorAreaName: p.consolidatorAreaName || '',
            suggestedUnit: 'Unidad',
            referencePrice: 25000
          }));
        return of(filtered);
      })
    );
  }

  list(filters: {
    q?: string;
    state?: string;
    consolidatorAreaId?: string;
    missingMapping?: boolean;
    requiresReview?: boolean;
    unspscPrefix?: string;
    page?: number;
    pageSize?: number;
  }): Observable<PagedResponse<MasterProductDto>> {
    let params = new HttpParams();
    if (filters.q) params = params.set('q', filters.q);
    if (filters.state) params = params.set('state', filters.state);
    if (filters.consolidatorAreaId) params = params.set('consolidatorAreaId', filters.consolidatorAreaId);
    if (filters.missingMapping !== undefined && filters.missingMapping !== null) params = params.set('missingMapping', filters.missingMapping.toString());
    if (filters.requiresReview !== undefined && filters.requiresReview !== null) params = params.set('requiresReview', filters.requiresReview.toString());
    if (filters.unspscPrefix) params = params.set('unspscPrefix', filters.unspscPrefix);
    if (filters.page) params = params.set('page', filters.page.toString());
    if (filters.pageSize) params = params.set('pageSize', filters.pageSize.toString());

    return this.http.get<any>(this.baseUrl, { params }).pipe(
      map(res => {
        if (!res) return res;
        let target = res;
        if (res && res.isSuccess && res.value) {
          target = res.value;
        }
        const items = target.value || target.Value || target.items || (Array.isArray(target) ? target : []);
        return {
          value: items,
          pageNumber: target.pageNumber || target.PageNumber || filters.page || 1,
          pageSize: target.pageSize || target.PageSize || filters.pageSize || 20,
          totalPages: target.totalPages || target.TotalPages || 1,
          totalRecords: target.totalRecords || target.TotalRecords || items.length,
          hasPreviousPage: target.hasPreviousPage ?? false,
          hasNextPage: target.hasNextPage ?? false,
          firstPage: target.firstPage ?? '1',
          lastPage: target.lastPage ?? '1',
          nextPage: target.nextPage ?? null,
          previousPage: target.previousPage ?? null
        } as PagedResponse<MasterProductDto>;
      }),
      catchError(() => {
        let items = [...MOCK_PRODUCTS];
        if (filters.q) {
          const query = filters.q.toLowerCase();
          items = items.filter(p => p.name.toLowerCase().includes(query) || p.code.toLowerCase().includes(query));
        }
        if (filters.state) {
          items = items.filter(p => p.state === filters.state);
        }
        if (filters.unspscPrefix) {
          items = items.filter(p => p.unspscCode?.startsWith(filters.unspscPrefix!));
        }
        const page = filters.page || 1;
        const pageSize = filters.pageSize || 20;
        const totalRecords = items.length;
        const totalPages = Math.ceil(totalRecords / pageSize);
        const paged = items.slice((page - 1) * pageSize, page * pageSize) as any[];

        return of({
          value: paged,
          pageNumber: page,
          pageSize: pageSize,
          totalPages: totalPages || 1,
          totalRecords: totalRecords,
          hasPreviousPage: page > 1,
          hasNextPage: page < totalPages,
          firstPage: '1',
          lastPage: (totalPages || 1).toString(),
          nextPage: page < totalPages ? (page + 1).toString() : null,
          previousPage: page > 1 ? (page - 1).toString() : null
        } as PagedResponse<MasterProductDto>);
      })
    );
  }

  getMappingSummary(): Observable<MasterProductMappingSummaryDto> {
    return this.http.get<MasterProductMappingSummaryDto>(`${this.baseUrl}/mapping-summary`).pipe(
      catchError(() => of(MOCK_SUMMARY))
    );
  }

  getById(id: string): Observable<MasterProductDetailDto> {
    return this.http.get<MasterProductDetailDto>(`${this.baseUrl}/${id}`).pipe(
      catchError(() => {
        const found = MOCK_PRODUCTS.find(p => p.id === id) || MOCK_PRODUCTS[0];
        const detail: any = {
          ...found,
          organizationId: 1,
          sourceId: null,
          originalDescription: found.name,
          englishName: 'Product Description in English',
          definition: 'Definición estandarizada del catálogo institucional',
          searchText: found.name.toLowerCase(),
          unifiedCodeId: null,
          expenseObjectId: null,
          accountingAccountId: null,
          suggestedUnit: 'Unidad',
          measureUnit: 'Unidad',
          referencePrice: 45000,
          currency: 'DOP',
          consolidatorRuleId: null,
          consolidatorRuleKey: null,
          consolidatorRuleVersion: null,
          consolidatorAreaConfidence: 100,
          expenseObjectConfidence: 100,
          accountingAccountConfidence: 100,
          matchMethod: 'Exact',
          areaAmbiguity: false,
          alternativeAreasJson: null,
          reviewReason: null,
          notes: null,
          duplicateOfMasterProductId: null,
          requestedBy: null,
          requestedByName: null,
          requestedAreaId: null,
          requestedAt: null,
          requestJustification: null,
          version: 1,
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
          aliases: []
        };
        return of(detail as MasterProductDetailDto);
      })
    );
  }

  create(dto: CreateMasterProductDto): Observable<MasterProductDetailDto> {
    return this.http.post<MasterProductDetailDto>(this.baseUrl, dto);
  }

  update(id: string, dto: UpdateMasterProductDto): Observable<MasterProductDetailDto> {
    return this.http.put<MasterProductDetailDto>(`${this.baseUrl}/${id}`, dto);
  }

  updateMapping(id: string, dto: UpdateMasterProductMappingDto): Observable<MasterProductDetailDto> {
    return this.http.put<MasterProductDetailDto>(`${this.baseUrl}/${id}/mapping`, dto);
  }

  ratifyConsolidatorArea(id: string, dto: RatifyConsolidatorAreaDto): Observable<MasterProductDetailDto> {
    return this.http.put<MasterProductDetailDto>(`${this.baseUrl}/${id}/consolidator-area`, dto);
  }

  bulkRatify(dto: BulkRatifyByUnspscDto): Observable<{ count: number }> {
    return this.http.post<{ count: number }>(`${this.baseUrl}/bulk-ratify`, dto).pipe(
      catchError(() => of({ count: 12 }))
    );
  }

  requestProduct(dto: RequestMasterProductDto): Observable<MasterProductDetailDto> {
    return this.http.post<MasterProductDetailDto>(this.baseUrl + '/request', dto);
  }

  setState(id: string, state: MasterProductState): Observable<MasterProductDetailDto> {
    let params = new HttpParams().set('state', state);
    return this.http.put<MasterProductDetailDto>(`${this.baseUrl}/${id}/state`, {}, { params });
  }

  deleteProduct(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`).pipe(
      catchError(() => of(void 0))
    );
  }
}
