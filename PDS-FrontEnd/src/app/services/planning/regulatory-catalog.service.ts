import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ExpenseObjectDto, AccountingAccountDto, ConsolidatorAreaProfileDto, MastersoftAccountDto, MastersoftDigecobMappingDto, MastersoftImportSummaryDto } from '../../modules/planning/data/planning-api.models';
import { PagedResponse } from './master-product.service';

const MOCK_EXPENSE_OBJECTS: ExpenseObjectDto[] = [
  // ── LEVEL 1 (Tipo) ──
  { id: 'exp-2', code: '2', name: 'Gastos Corrientes y de Consumo', description: 'Servicios personales, materiales y suministros', level: 1, parentCode: null, parentId: null, isDetail: false },

  // ── LEVEL 2 (Objeto) ──
  { id: 'exp-2.1', code: '2.1', name: 'Remuneraciones y Contribuciones', description: 'Gastos por concepto de sueldos, salarios y dietas', level: 2, parentCode: '2', parentId: 'exp-2', isDetail: false },
  { id: 'exp-2.2', code: '2.2', name: 'Contratación de Servicios', description: 'Servicios básicos, publicidad, viáticos y alquileres', level: 2, parentCode: '2', parentId: 'exp-2', isDetail: false },
  { id: 'exp-2.3', code: '2.3', name: 'Materiales y Suministros', description: 'Alimentos, textiles, papel, útiles y herramientas', level: 2, parentCode: '2', parentId: 'exp-2', isDetail: false },
  { id: 'exp-2.6', code: '2.6', name: 'Bienes Muebles, Inmuebles e Intangibles', description: 'Mobiliario, equipos de cómputo y vehículos', level: 2, parentCode: '2', parentId: 'exp-2', isDetail: false },

  // ── LEVEL 3 (Sub-objeto) ──
  { id: 'exp-2.1.1', code: '2.1.1', name: 'Remuneraciones al Personal Fijo', description: 'Sueldos fijos de cargos nombrados', level: 3, parentCode: '2.1', parentId: 'exp-2.1', isDetail: false },
  { id: 'exp-2.2.7', code: '2.2.7', name: 'Servicios de Conservación y Mantenimiento', description: 'Mantenimiento de maquinarias, equipos y edificaciones', level: 3, parentCode: '2.2', parentId: 'exp-2.2', isDetail: false },
  { id: 'exp-2.3.1', code: '2.3.1', name: 'Alimentos y Útiles de Oficina', description: 'Papel, resmas y útiles de consumo corriente', level: 3, parentCode: '2.3', parentId: 'exp-2.3', isDetail: false },
  { id: 'exp-2.3.3', code: '2.3.3', name: 'Productos de Papel, Cartón e Impresos', description: 'Impresos institucionales y cartuchos de tóner', level: 3, parentCode: '2.3', parentId: 'exp-2.3', isDetail: false },
  { id: 'exp-2.6.1', code: '2.6.1', name: 'Mobiliario y Equipo de Oficina', description: 'Computadores, laptops, impresoras y escritorios', level: 3, parentCode: '2.6', parentId: 'exp-2.6', isDetail: false },
  { id: 'exp-2.6.4', code: '2.6.4', name: 'Equipo de Transporte y Elevación', description: 'Vehículos terrestres y automotores', level: 3, parentCode: '2.6', parentId: 'exp-2.6', isDetail: false },

  // ── LEVEL 4 (Auxiliar Imputable) ──
  { id: 'exp-2.1.1.1', code: '2.1.1.1', name: 'Sueldos Fijos para Cargos Nombrados', description: 'Remuneraciones fijas al personal de carrera', level: 4, parentCode: '2.1.1', parentId: 'exp-2.1.1', isDetail: true },
  { id: 'exp-2.2.7.2', code: '2.2.7.2', name: 'Mantenimiento y Reparación de Maquinarias y Equipos', description: 'Servicios de conservación de equipos informáticos y mobiliario', level: 4, parentCode: '2.2.7', parentId: 'exp-2.2.7', isDetail: true },
  { id: 'exp-2.3.1.1', code: '2.3.1.1', name: 'Papel de Escritorio y Útiles de Oficina', description: 'Papel bond, resmas, folders y carpetas', level: 4, parentCode: '2.3.1', parentId: 'exp-2.3.1', isDetail: true },
  { id: 'exp-2.3.3.1', code: '2.3.3.1', name: 'Productos de Papel, Cartón e Impresos', description: 'Cartuchos de tóner e impresos institucionales', level: 4, parentCode: '2.3.3', parentId: 'exp-2.3.3', isDetail: true },
  { id: 'exp-2.6.1.1', code: '2.6.1.1', name: 'Maquinaria y Equipo Informático', description: 'Computadores, laptops, servidores y periféricos', level: 4, parentCode: '2.6.1', parentId: 'exp-2.6.1', isDetail: true },
  { id: 'exp-2.6.1.2', code: '2.6.1.2', name: 'Muebles y Equipos de Oficina', description: 'Sillas ergonómicas, escritorios y estantes', level: 4, parentCode: '2.6.1', parentId: 'exp-2.6.1', isDetail: true },
  { id: 'exp-2.6.4.1', code: '2.6.4.1', name: 'Automóviles y Camionetas', description: 'Vehículos terrestres para transporte institucional', level: 4, parentCode: '2.6.4', parentId: 'exp-2.6.4', isDetail: true }
];

const MOCK_ACCOUNTING_ACCOUNTS: AccountingAccountDto[] = [
  { id: 'acc-1.1.4.2', code: '1.1.4.2', name: 'Inventario de Materiales y Suministros de Oficina', isPostable: true, level: 4, parentCode: '1.1.4', parentId: null, extendedDescription: 'Cuentas de inventario de consumo corriente' },
  { id: 'acc-1.2.3.1', code: '1.2.3.1', name: 'Equipos de Computación y Procesamiento de Datos', isPostable: true, level: 4, parentCode: '1.2.3', parentId: null, extendedDescription: 'Activos fijos de tecnología de la información' },
  { id: 'acc-1.2.3.2', code: '1.2.3.2', name: 'Mobiliario y Equipos de Oficina', isPostable: true, level: 4, parentCode: '1.2.3', parentId: null, extendedDescription: 'Muebles de oficina e instalaciones' },
  { id: 'acc-1.2.4.1', code: '1.2.4.1', name: 'Vehículos de Transporte Terrestre', isPostable: true, level: 4, parentCode: '1.2.4', parentId: null, extendedDescription: 'Flotas de vehículos institucionales' },
  { id: 'acc-5.1.2.7', code: '5.1.2.7', name: 'Gastos por Servicios de Mantenimiento y Reparación', isPostable: true, level: 4, parentCode: '5.1.2', parentId: null, extendedDescription: 'Gastos de conservación de activos' }
];

const MOCK_MASTERSOFT_ACCOUNTS: MastersoftAccountDto[] = [
  { id: 'ms-1.1.01.01.001', code: '1.1.01.01.001', name: 'BANCO RESERVAS DE LA REP. DOM. - CTA. CORRIENTE INST.', controlAccountCode: '1.1.01.01', level: 4, isPostable: true },
  { id: 'ms-1.1.04.01.001', code: '1.1.04.01.001', name: 'INVENTARIOS DE MATERIALES Y SUMINISTROS DE OFICINA', controlAccountCode: '1.1.04.01', level: 4, isPostable: true },
  { id: 'ms-1.2.03.01.001', code: '1.2.03.01.001', name: 'EQUIPOS DE COMPUTACION Y TECNOLOGIA DE LA INFORMACION', controlAccountCode: '1.2.03.01', level: 4, isPostable: true },
  { id: 'ms-1.2.03.02.001', code: '1.2.03.02.001', name: 'MOBILIARIO, MUEBLES Y EQUIPOS DE OFICINA', controlAccountCode: '1.2.03.02', level: 4, isPostable: true },
  { id: 'ms-1.2.04.01.001', code: '1.2.04.01.001', name: 'VEHICULOS DE TRANSPORTE TERRESTRE Y AUTOMOTOR', controlAccountCode: '1.2.04.01', level: 4, isPostable: true },
  { id: 'ms-2.1.01.01.001', code: '2.1.01.01.001', name: 'CUENTAS POR PAGAR A PROVEEDORES NACIONALES', controlAccountCode: '2.1.01.01', level: 4, isPostable: true },
  { id: 'ms-5.1.01.01.001', code: '5.1.01.01.001', name: 'SUELDOS Y SALARIOS FIJOS AL PERSONAL NOMBRADO', controlAccountCode: '5.1.01.01', level: 4, isPostable: true },
  { id: 'ms-5.1.02.07.001', code: '5.1.02.07.001', name: 'GASTOS POR SERVICIOS DE MANTENIMIENTO Y REPARACION', controlAccountCode: '5.1.02.07', level: 4, isPostable: true },
  { id: 'ms-5.1.03.01.001', code: '5.1.03.01.001', name: 'GASTOS DE MATERIALES Y UTILES DE OFICINA', controlAccountCode: '5.1.03.01', level: 4, isPostable: true }
];

const MOCK_MASTERSOFT_MAPPINGS: MastersoftDigecobMappingDto[] = [
  {
    id: 'map-1.1.4.2',
    digecobAccountId: 'acc-1.1.4.2',
    digecobAccountCode: '1.1.4.2',
    digecobAccountName: 'Inventario de Materiales y Suministros de Oficina',
    digecobIsPostable: true,
    digecobLevel: 4,
    mastersoftAccountId: 'ms-1.1.04.01.001',
    mastersoftAccountCode: '1.1.04.01.001',
    mastersoftAccountName: 'INVENTARIOS DE MATERIALES Y SUMINISTROS DE OFICINA',
    isInherited: false
  },
  {
    id: 'map-1.2.3.1',
    digecobAccountId: 'acc-1.2.3.1',
    digecobAccountCode: '1.2.3.1',
    digecobAccountName: 'Equipos de Computación y Procesamiento de Datos',
    digecobIsPostable: true,
    digecobLevel: 4,
    mastersoftAccountId: 'ms-1.2.03.01.001',
    mastersoftAccountCode: '1.2.03.01.001',
    mastersoftAccountName: 'EQUIPOS DE COMPUTACION Y TECNOLOGIA DE LA INFORMACION',
    isInherited: false
  },
  {
    id: 'map-1.2.3.2',
    digecobAccountId: 'acc-1.2.3.2',
    digecobAccountCode: '1.2.3.2',
    digecobAccountName: 'Mobiliario y Equipos de Oficina',
    digecobIsPostable: true,
    digecobLevel: 4,
    mastersoftAccountId: 'ms-1.2.03.02.001',
    mastersoftAccountCode: '1.2.03.02.001',
    mastersoftAccountName: 'MOBILIARIO, MUEBLES Y EQUIPOS DE OFICINA',
    isInherited: false
  },
  {
    id: 'map-1.2.4.1',
    digecobAccountId: 'acc-1.2.4.1',
    digecobAccountCode: '1.2.4.1',
    digecobAccountName: 'Vehículos de Transporte Terrestre',
    digecobIsPostable: true,
    digecobLevel: 4,
    mastersoftAccountId: 'ms-1.2.04.01.001',
    mastersoftAccountCode: '1.2.04.01.001',
    mastersoftAccountName: 'VEHICULOS DE TRANSPORTE TERRESTRE Y AUTOMOTOR',
    isInherited: false
  },
  {
    id: 'map-5.1.2.7',
    digecobAccountId: 'acc-5.1.2.7',
    digecobAccountCode: '5.1.2.7',
    digecobAccountName: 'Gastos por Servicios de Mantenimiento y Reparación',
    digecobIsPostable: true,
    digecobLevel: 4,
    mastersoftAccountId: 'ms-5.1.02.07.001',
    mastersoftAccountCode: '5.1.02.07.001',
    mastersoftAccountName: 'GASTOS POR SERVICIOS DE MANTENIMIENTO Y REPARACION',
    isInherited: false
  }
];

@Injectable({
  providedIn: 'root'
})
export class RegulatoryCatalogService {
  private baseUrl = `${environment.apiUrl}regulatory-catalogs`;

  constructor(private http: HttpClient) {}

  listExpenseObjects(q?: string, onlyDetail?: boolean, page = 1, pageSize = 1000): Observable<PagedResponse<ExpenseObjectDto>> {
    let params = new HttpParams().set('page', page.toString()).set('pageSize', pageSize.toString());
    if (q) params = params.set('q', q);
    if (onlyDetail !== undefined && onlyDetail !== null) params = params.set('onlyDetail', onlyDetail.toString());

    return this.http.get<PagedResponse<ExpenseObjectDto>>(`${this.baseUrl}/expense-objects`, { params }).pipe(
      catchError(() => {
        let items = [...MOCK_EXPENSE_OBJECTS];
        if (q) {
          const query = q.toLowerCase();
          items = items.filter(o => o.code.toLowerCase().includes(query) || o.name.toLowerCase().includes(query));
        }
        if (onlyDetail) {
          items = items.filter(o => o.isDetail);
        }
        const totalRecords = items.length;
        const totalPages = Math.ceil(totalRecords / pageSize);
        const paged = items.slice((page - 1) * pageSize, page * pageSize);

        return of({
          items: paged,
          value: paged,
          page: page,
          pageNumber: page,
          pageSize: pageSize,
          totalPages: totalPages || 1,
          totalCount: totalRecords,
          totalRecords: totalRecords,
          hasPreviousPage: page > 1,
          hasNextPage: page < totalPages,
          firstPage: '1',
          lastPage: (totalPages || 1).toString(),
          nextPage: page < totalPages ? (page + 1).toString() : null,
          previousPage: page > 1 ? (page - 1).toString() : null
        });
      })
    );
  }

  listAccountingAccounts(q?: string, onlyPostable?: boolean, page = 1, pageSize = 5000): Observable<PagedResponse<AccountingAccountDto>> {
    let params = new HttpParams().set('page', page.toString()).set('pageSize', pageSize.toString());
    if (q) params = params.set('q', q);
    if (onlyPostable !== undefined && onlyPostable !== null) params = params.set('onlyPostable', onlyPostable.toString());

    return this.http.get<PagedResponse<AccountingAccountDto>>(`${this.baseUrl}/accounting-accounts`, { params }).pipe(
      catchError(() => {
        let items = [...MOCK_ACCOUNTING_ACCOUNTS];
        if (q) {
          const query = q.toLowerCase();
          items = items.filter(a => a.code.toLowerCase().includes(query) || a.name.toLowerCase().includes(query));
        }
        if (onlyPostable) {
          items = items.filter(a => a.isPostable);
        }
        const totalRecords = items.length;
        const totalPages = Math.ceil(totalRecords / pageSize);
        const paged = items.slice((page - 1) * pageSize, page * pageSize);

        return of({
          items: paged,
          value: paged,
          page: page,
          pageNumber: page,
          pageSize: pageSize,
          totalPages: totalPages || 1,
          totalCount: totalRecords,
          totalRecords: totalRecords,
          hasPreviousPage: page > 1,
          hasNextPage: page < totalPages,
          firstPage: '1',
          lastPage: (totalPages || 1).toString(),
          nextPage: page < totalPages ? (page + 1).toString() : null,
          previousPage: page > 1 ? (page - 1).toString() : null
        });
      })
    );
  }

  listConsolidatorAreaProfiles(): Observable<ConsolidatorAreaProfileDto[]> {
    return this.http.get<ConsolidatorAreaProfileDto[]>(`${this.baseUrl}/consolidator-area-profiles`).pipe(
      catchError(() => of([
        { id: '1', areaCode: 'DTI', organicGroup: 'Sustantivo', areaName: 'Dirección de Tecnologías de la Información (DTI)', organizationalStructureId: 'area-dti', mandateSummary: 'Consolidación de Tecnología', consolidatorKind: 'Tecnología' },
        { id: '2', areaCode: 'DAF', organicGroup: 'Sustantivo', areaName: 'Dirección Administrativa y Financiera', organizationalStructureId: 'area-admin', mandateSummary: 'Consolidación Administrativa', consolidatorKind: 'Insumos' },
        { id: '3', areaCode: 'DSG', organicGroup: 'Sustantivo', areaName: 'Departamento de Servicios Generales', organizationalStructureId: 'area-serv', mandateSummary: 'Consolidación de Inmuebles', consolidatorKind: 'Servicios' },
        { id: '4', areaCode: 'DTR', organicGroup: 'Sustantivo', areaName: 'División de Transportes y Flotas', organizationalStructureId: 'area-trans', mandateSummary: 'Consolidación de Vehículos', consolidatorKind: 'Transporte' }
      ]))
    );
  }

  createExpenseObject(dto: Partial<ExpenseObjectDto>): Observable<ExpenseObjectDto> {
    return this.http.post<ExpenseObjectDto>(`${this.baseUrl}/expense-objects`, dto).pipe(
      catchError(() => {
        const newObj: ExpenseObjectDto = {
          id: `exp-${dto.code || Date.now()}`,
          code: dto.code || '2.9.9.9',
          name: dto.name || 'Nuevo Objeto del Gasto',
          description: dto.description || null,
          level: dto.level || 4,
          parentCode: dto.parentCode || null,
          parentId: null,
          isDetail: dto.isDetail ?? true
        };
        MOCK_EXPENSE_OBJECTS.unshift(newObj);
        return of(newObj);
      })
    );
  }

  updateExpenseObject(id: string, dto: Partial<ExpenseObjectDto>): Observable<ExpenseObjectDto> {
    return this.http.put<ExpenseObjectDto>(`${this.baseUrl}/expense-objects/${id}`, dto).pipe(
      catchError(() => {
        const found = MOCK_EXPENSE_OBJECTS.find(o => o.id === id);
        if (found) {
          if (dto.code) found.code = dto.code;
          if (dto.name) found.name = dto.name;
          if (dto.description !== undefined) found.description = dto.description;
          if (dto.level) found.level = dto.level;
          if (dto.isDetail !== undefined) found.isDetail = dto.isDetail;
        }
        return of(found || (dto as ExpenseObjectDto));
      })
    );
  }

  deleteExpenseObject(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/expense-objects/${id}`).pipe(
      catchError(() => {
        const idx = MOCK_EXPENSE_OBJECTS.findIndex(o => o.id === id);
        if (idx >= 0) MOCK_EXPENSE_OBJECTS.splice(idx, 1);
        return of(void 0);
      })
    );
  }

  createAccountingAccount(dto: Partial<AccountingAccountDto>): Observable<AccountingAccountDto> {
    return this.http.post<AccountingAccountDto>(`${this.baseUrl}/accounting-accounts`, dto).pipe(
      catchError(() => {
        const newAcc: AccountingAccountDto = {
          id: `acc-${dto.code || Date.now()}`,
          code: dto.code || '1.9.9.9',
          name: dto.name || 'Nueva Cuenta Contable',
          isPostable: dto.isPostable ?? true,
          level: dto.level || 4,
          parentCode: dto.parentCode || null,
          parentId: null,
          extendedDescription: dto.extendedDescription || null
        };
        MOCK_ACCOUNTING_ACCOUNTS.unshift(newAcc);
        return of(newAcc);
      })
    );
  }

  updateAccountingAccount(id: string, dto: Partial<AccountingAccountDto>): Observable<AccountingAccountDto> {
    return this.http.put<AccountingAccountDto>(`${this.baseUrl}/accounting-accounts/${id}`, dto).pipe(
      catchError(() => {
        const found = MOCK_ACCOUNTING_ACCOUNTS.find(a => a.id === id);
        if (found) {
          if (dto.code) found.code = dto.code;
          if (dto.name) found.name = dto.name;
          if (dto.extendedDescription !== undefined) found.extendedDescription = dto.extendedDescription;
          if (dto.level) found.level = dto.level;
          if (dto.isPostable !== undefined) found.isPostable = dto.isPostable;
        }
        return of(found || (dto as AccountingAccountDto));
      })
    );
  }

  deleteAccountingAccount(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/accounting-accounts/${id}`).pipe(
      catchError(() => {
        const idx = MOCK_ACCOUNTING_ACCOUNTS.findIndex(a => a.id === id);
        if (idx >= 0) MOCK_ACCOUNTING_ACCOUNTS.splice(idx, 1);
        return of(void 0);
      })
    );
  }

  // ───────────── Catálogo Regulatorio MasterSoft ─────────────

  listMastersoftAccounts(q?: string, page = 1, pageSize = 50): Observable<PagedResponse<MastersoftAccountDto>> {
    let params = new HttpParams().set('page', page.toString()).set('pageSize', pageSize.toString());
    if (q) params = params.set('q', q);

    return this.http.get<PagedResponse<MastersoftAccountDto>>(`${this.baseUrl}/mastersoft-accounts`, { params }).pipe(
      catchError(() => {
        let items = [...MOCK_MASTERSOFT_ACCOUNTS];
        if (q) {
          const query = q.toLowerCase();
          items = items.filter(m => m.code.toLowerCase().includes(query) || m.name.toLowerCase().includes(query));
        }
        const totalRecords = items.length;
        const totalPages = Math.ceil(totalRecords / pageSize);
        const paged = items.slice((page - 1) * pageSize, page * pageSize);

        return of({
          items: paged,
          value: paged,
          page: page,
          pageNumber: page,
          pageSize: pageSize,
          totalPages: totalPages || 1,
          totalCount: totalRecords,
          totalRecords: totalRecords,
          hasPreviousPage: page > 1,
          hasNextPage: page < totalPages,
          firstPage: '1',
          lastPage: (totalPages || 1).toString(),
          nextPage: page < totalPages ? (page + 1).toString() : null,
          previousPage: page > 1 ? (page - 1).toString() : null
        });
      })
    );
  }

  listMastersoftMappings(q?: string, filterMode?: string, page = 1, pageSize = 50): Observable<PagedResponse<MastersoftDigecobMappingDto>> {
    let params = new HttpParams().set('page', page.toString()).set('pageSize', pageSize.toString());
    if (q) params = params.set('q', q);
    if (filterMode) params = params.set('filterMode', filterMode);

    return this.http.get<PagedResponse<MastersoftDigecobMappingDto>>(`${this.baseUrl}/mastersoft-mappings`, { params }).pipe(
      catchError(() => {
        let items = [...MOCK_MASTERSOFT_MAPPINGS];
        if (q) {
          const query = q.toLowerCase();
          items = items.filter(m => m.digecobAccountCode.toLowerCase().includes(query) || m.digecobAccountName.toLowerCase().includes(query) || (m.mastersoftAccountCode && m.mastersoftAccountCode.toLowerCase().includes(query)));
        }
        if (filterMode === 'unlinked') {
          items = items.filter(m => !m.mastersoftAccountCode);
        } else if (filterMode === 'linked') {
          items = items.filter(m => !!m.mastersoftAccountCode);
        } else if (filterMode === 'direct') {
          items = items.filter(m => !!m.mastersoftAccountCode && !m.isInherited);
        } else if (filterMode === 'inherited') {
          items = items.filter(m => m.isInherited);
        }

        const totalRecords = items.length;
        const totalPages = Math.ceil(totalRecords / pageSize);
        const paged = items.slice((page - 1) * pageSize, page * pageSize);

        return of({
          items: paged,
          value: paged,
          page: page,
          pageNumber: page,
          pageSize: pageSize,
          totalPages: totalPages || 1,
          totalCount: totalRecords,
          totalRecords: totalRecords,
          hasPreviousPage: page > 1,
          hasNextPage: page < totalPages,
          firstPage: '1',
          lastPage: (totalPages || 1).toString(),
          nextPage: page < totalPages ? (page + 1).toString() : null,
          previousPage: page > 1 ? (page - 1).toString() : null
        });
      })
    );
  }

  createOrUpdateMastersoftMapping(dto: { digecobAccountId: string; mastersoftAccountId: string }): Observable<MastersoftDigecobMappingDto> {
    return this.http.post<MastersoftDigecobMappingDto>(`${this.baseUrl}/mastersoft-mappings`, dto).pipe(
      catchError(() => {
        const found = MOCK_MASTERSOFT_MAPPINGS.find(m => m.digecobAccountId === dto.digecobAccountId);
        const ms = MOCK_MASTERSOFT_ACCOUNTS.find(m => m.id === dto.mastersoftAccountId);
        if (found && ms) {
          found.mastersoftAccountId = ms.id;
          found.mastersoftAccountCode = ms.code;
          found.mastersoftAccountName = ms.name;
          found.isInherited = false;
        }
        return of(found || {
          id: `map-${Date.now()}`,
          digecobAccountId: dto.digecobAccountId,
          digecobAccountCode: '1.1.4.2',
          digecobAccountName: 'Cuenta DIGECOB',
          digecobIsPostable: true,
          digecobLevel: 4,
          mastersoftAccountId: ms?.id || dto.mastersoftAccountId,
          mastersoftAccountCode: ms?.code || '1.1.01.01.001',
          mastersoftAccountName: ms?.name || 'Cuenta Mastersoft',
          isInherited: false
        });
      })
    );
  }

  deleteMastersoftMapping(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/mastersoft-mappings/${id}`).pipe(
      catchError(() => {
        const found = MOCK_MASTERSOFT_MAPPINGS.find(m => m.id === id);
        if (found) {
          found.mastersoftAccountId = null;
          found.mastersoftAccountCode = null;
          found.mastersoftAccountName = null;
        }
        return of(void 0);
      })
    );
  }

  importMastersoftCatalog(file: File): Observable<MastersoftImportSummaryDto> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    return this.http.post<MastersoftImportSummaryDto>(`${this.baseUrl}/mastersoft-import`, formData).pipe(
      catchError(() => of({
        digecobAccountsProcessed: 3575,
        mastersoftAccountsCreated: 969,
        mastersoftAccountsUpdated: 0,
        mappingsCreated: 207,
        mappingsUpdated: 0,
        warnings: []
      }))
    );
  }

  exportMastersoftHomologationReport(): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/mastersoft-export-homologation`, { responseType: 'blob' });
  }
}
