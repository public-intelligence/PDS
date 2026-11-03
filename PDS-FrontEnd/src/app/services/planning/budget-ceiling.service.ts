import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { PagedResponseDto } from '../../modules/planning/data/planning-api.models';
import {
  AreaConsumptionReportDto,
  BudgetAdjustmentDto,
  BudgetCeilingDetailDto,
  BudgetCeilingDto,
  BudgetImpactPreviewDto,
  BudgetMonitoringDto,
  BudgetValidationRequestDto,
  BudgetValidationResultDto,
  CreateBudgetAdjustmentDto,
  CreateBudgetCeilingDto,
  CrossExecutionReportDto,
  UpdateBudgetCeilingDto,
} from '../../modules/planning/data/planning-api.models';

const MOCK_CEILINGS: BudgetCeilingDetailDto[] = [
  {
    id: 'ceiling-1',
    organizationId: 1,
    fiscalYear: 2026,
    expenseObjectId: 'exp-2.3',
    expenseObjectCode: '2.3',
    expenseObjectName: 'Materiales y Suministros',
    expenseObjectLevel: 2,
    expenseObjectLevelName: 'Nivel 2 - Objeto',
    fundingSource: 'Fuente 10 - Recursos del Tesoro',
    budgetProgram: 'Programa 01 - Gestión Institucional',
    initialAmount: 5000000,
    currentAmount: 5000000,
    reservedAmount: 500000,
    plannedAmount: 1500000,
    committedAmount: 800000,
    executedAmount: 400000,
    availableAmount: 2200000,
    utilizationPercentage: 56,
    thresholdPercentage: 10,
    validationBehavior: 'Block',
    status: 'Green',
    totalAdjustments: 0,
    unconfirmedAmount: 0,
    adjustments: [],
    consumptionByProduct: [],
    consumptionByArea: [],
    consumptionByChildExpenseObject: [],
    recentRequirements: [],
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString()
  }
];

@Injectable({ providedIn: 'root' })
export class BudgetCeilingService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}budget-ceilings`;

  list(params?: {
    fiscalYear?: number;
    fundingSource?: string;
    status?: string;
    q?: string;
    expenseObjectId?: string;
    page?: number;
    pageSize?: number;
  }): Observable<PagedResponseDto<BudgetCeilingDto>> {
    let httpParams = new HttpParams();
    if (params?.fiscalYear) httpParams = httpParams.set('fiscalYear', params.fiscalYear.toString());
    if (params?.fundingSource) httpParams = httpParams.set('fundingSource', params.fundingSource);
    if (params?.status) httpParams = httpParams.set('status', params.status);
    if (params?.q) httpParams = httpParams.set('q', params.q);
    if (params?.expenseObjectId) httpParams = httpParams.set('expenseObjectId', params.expenseObjectId);
    if (params?.page) httpParams = httpParams.set('page', params.page.toString());
    if (params?.pageSize) httpParams = httpParams.set('pageSize', params.pageSize.toString());

    return this.http.get<PagedResponseDto<BudgetCeilingDto>>(this.baseUrl, { params: httpParams }).pipe(
      catchError(() => {
        let items: BudgetCeilingDto[] = [...MOCK_CEILINGS];
        if (params?.fiscalYear) items = items.filter(c => c.fiscalYear === params.fiscalYear);
        if (params?.fundingSource) items = items.filter(c => c.fundingSource.toLowerCase().includes(params.fundingSource!.toLowerCase()));
        if (params?.q) {
          const term = params.q.toLowerCase();
          items = items.filter(c => c.expenseObjectCode.toLowerCase().includes(term) || c.expenseObjectName.toLowerCase().includes(term));
        }
        const resp: PagedResponseDto<BudgetCeilingDto> = {
          value: items,
          items,
          pageNumber: params?.page || 1,
          pageSize: params?.pageSize || 20,
          totalPages: 1,
          totalRecords: items.length,
          totalCount: items.length,
          firstPage: '1',
          lastPage: '1',
          nextPage: null,
          previousPage: null
        };
        return of(resp);
      })
    );
  }

  getById(id: string): Observable<BudgetCeilingDetailDto> {
    return this.http.get<BudgetCeilingDetailDto>(`${this.baseUrl}/${id}`).pipe(
      catchError(() => {
        const found = MOCK_CEILINGS.find(c => c.id === id);
        return of(found || MOCK_CEILINGS[0]);
      })
    );
  }

  create(dto: CreateBudgetCeilingDto): Observable<BudgetCeilingDetailDto> {
    return this.http.post<BudgetCeilingDetailDto>(this.baseUrl, dto).pipe(
      catchError(() => {
        const newCeiling: BudgetCeilingDetailDto = {
          id: `ceiling-${Date.now()}`,
          organizationId: 1,
          fiscalYear: dto.fiscalYear,
          expenseObjectId: dto.expenseObjectId,
          expenseObjectCode: dto.expenseObjectId.substring(0, 10),
          expenseObjectName: `Objeto del Gasto ${dto.expenseObjectId}`,
          expenseObjectLevel: 2,
          expenseObjectLevelName: 'Nivel 2',
          fundingSource: dto.fundingSource,
          budgetProgram: dto.budgetProgram || null,
          initialAmount: dto.initialAmount,
          currentAmount: dto.initialAmount,
          reservedAmount: 0,
          plannedAmount: 0,
          committedAmount: 0,
          executedAmount: 0,
          availableAmount: dto.initialAmount,
          utilizationPercentage: 0,
          thresholdPercentage: dto.thresholdPercentage ?? 10,
          validationBehavior: dto.validationBehavior || 'Block',
          status: 'Green',
          totalAdjustments: 0,
          unconfirmedAmount: 0,
          adjustments: [],
          consumptionByProduct: [],
          consumptionByArea: [],
          consumptionByChildExpenseObject: [],
          recentRequirements: [],
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString()
        };
        MOCK_CEILINGS.unshift(newCeiling);
        return of(newCeiling);
      })
    );
  }

  update(id: string, dto: UpdateBudgetCeilingDto): Observable<BudgetCeilingDetailDto> {
    return this.http.put<BudgetCeilingDetailDto>(`${this.baseUrl}/${id}`, dto).pipe(
      catchError(() => {
        const found = MOCK_CEILINGS.find(c => c.id === id);
        if (found) {
          found.thresholdPercentage = dto.thresholdPercentage;
          found.validationBehavior = dto.validationBehavior;
          if (dto.budgetProgram !== undefined) found.budgetProgram = dto.budgetProgram;
        }
        return of(found || MOCK_CEILINGS[0]);
      })
    );
  }

  addAdjustment(id: string, dto: CreateBudgetAdjustmentDto): Observable<BudgetCeilingDetailDto> {
    return this.http.post<BudgetCeilingDetailDto>(`${this.baseUrl}/${id}/adjustments`, dto).pipe(
      catchError(() => {
        const found = MOCK_CEILINGS.find(c => c.id === id);
        if (found) {
          const adjAmount = dto.adjustmentType === 'Increase' ? dto.amount : -dto.amount;
          found.currentAmount += adjAmount;
          found.availableAmount += adjAmount;
          found.adjustments.unshift({
            id: `adj-${Date.now()}`,
            budgetCeilingId: id,
            adjustmentType: dto.adjustmentType,
            amount: dto.amount,
            reason: dto.reason,
            documentUrl: dto.documentUrl || null,
            createdBy: 'user',
            createdByName: 'Usuario Planificación',
            createdAt: new Date().toISOString()
          });
        }
        return of(found || MOCK_CEILINGS[0]);
      })
    );
  }

  validateRequirement(request: BudgetValidationRequestDto): Observable<BudgetValidationResultDto> {
    return this.http.post<BudgetValidationResultDto>(`${this.baseUrl}/validate`, request).pipe(
      catchError(() => {
        const resp: BudgetValidationResultDto = {
          isValid: true,
          exceedsCeiling: false,
          ceilingId: 'ceiling-1',
          expenseObjectCode: '2.3',
          expenseObjectName: 'Materiales y Suministros',
          currentCeiling: 5000000,
          availableBudget: 2200000,
          requestedAmount: request.requiredAmount,
          exceededAmount: 0,
          validationBehavior: 'Allow',
          message: 'Validación exitosa'
        };
        return of(resp);
      })
    );
  }

  previewImpact(id: string, proposedAmount: number): Observable<BudgetImpactPreviewDto> {
    const params = new HttpParams().set('proposedAmount', proposedAmount.toString());
    return this.http.post<BudgetImpactPreviewDto>(`${this.baseUrl}/${id}/preview-impact`, null, { params }).pipe(
      catchError(() => {
        const resp: BudgetImpactPreviewDto = {
          ceilingId: id,
          expenseObjectCode: '2.3',
          expenseObjectName: 'Materiales y Suministros',
          currentCeiling: 5000000,
          proposedCeiling: proposedAmount,
          reductionAmount: Math.max(0, 5000000 - proposedAmount),
          newAvailableBudget: Math.max(0, proposedAmount - 2800000),
          affectedRequirementsCount: 0,
          affectedAreasCount: 0,
          totalExceededAmount: 0,
          affectedRequirements: []
        };
        return of(resp);
      })
    );
  }

  getMonitoring(fiscalYear?: number, fundingSource?: string): Observable<BudgetMonitoringDto[]> {
    let params = new HttpParams();
    if (fiscalYear) params = params.set('fiscalYear', fiscalYear.toString());
    if (fundingSource) params = params.set('fundingSource', fundingSource);
    return this.http.get<BudgetMonitoringDto[]>(`${this.baseUrl}/monitoring`, { params }).pipe(
      catchError(() => of([]))
    );
  }

  getAreaConsumption(fiscalYear?: number, expenseObjectId?: string): Observable<AreaConsumptionReportDto[]> {
    let params = new HttpParams();
    if (fiscalYear) params = params.set('fiscalYear', fiscalYear.toString());
    if (expenseObjectId) params = params.set('expenseObjectId', expenseObjectId);
    return this.http.get<AreaConsumptionReportDto[]>(`${this.baseUrl}/area-consumption`, { params }).pipe(
      catchError(() => of([]))
    );
  }

  getCrossExecution(fiscalYear?: number): Observable<CrossExecutionReportDto[]> {
    let params = new HttpParams();
    if (fiscalYear) params = params.set('fiscalYear', fiscalYear.toString());
    return this.http.get<CrossExecutionReportDto[]>(`${this.baseUrl}/cross-execution`, { params }).pipe(
      catchError(() => of([]))
    );
  }
}
