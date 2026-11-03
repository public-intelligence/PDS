import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ConsolidationBoardDto,
  ProductDemandBoardDto,
  ExpenseObjectsBoardPagedDto,
  ProductsBoardPagedDto,
  ConsolidatorAreasBoardPagedDto,
} from '../../modules/planning/data/planning-api.models';

@Injectable({
  providedIn: 'root',
})
export class ConsolidationService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl.endsWith('/') ? `${environment.apiUrl}consolidation` : `${environment.apiUrl}/consolidation`;

  getBoard(cycleId: string): Observable<ConsolidationBoardDto> {
    const params = new HttpParams().set('cycleId', cycleId);
    return this.http.get<ConsolidationBoardDto>(`${this.baseUrl}/board`, { params });
  }

  getProductBoard(cycleId: string, consolidatorAreaId?: string): Observable<ProductDemandBoardDto> {
    let params = new HttpParams().set('cycleId', cycleId);
    if (consolidatorAreaId) params = params.set('consolidatorAreaId', consolidatorAreaId);
    return this.http.get<ProductDemandBoardDto>(`${this.baseUrl}/product-board`, { params });
  }

  getExpenseObjectsBoard(
    cycleId: string,
    pageNumber: number = 1,
    pageSize: number = 40,
    search?: string,
  ): Observable<ExpenseObjectsBoardPagedDto> {
    let params = new HttpParams()
      .set('cycleId', cycleId)
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());
    if (search && search.trim()) params = params.set('search', search.trim());

    return this.http.get<ExpenseObjectsBoardPagedDto>(`${this.baseUrl}/by-expense-object`, { params });
  }

  getProductsBoardPaged(
    cycleId: string,
    consolidatorAreaId?: string,
    pageNumber: number = 1,
    pageSize: number = 40,
    search?: string,
  ): Observable<ProductsBoardPagedDto> {
    let params = new HttpParams()
      .set('cycleId', cycleId)
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());
    if (consolidatorAreaId) params = params.set('consolidatorAreaId', consolidatorAreaId);
    if (search && search.trim()) params = params.set('search', search.trim());

    return this.http.get<ProductsBoardPagedDto>(`${this.baseUrl}/by-product`, { params });
  }

  getConsolidatorAreasBoard(
    cycleId: string,
    pageNumber: number = 1,
    pageSize: number = 40,
    search?: string,
  ): Observable<ConsolidatorAreasBoardPagedDto> {
    let params = new HttpParams()
      .set('cycleId', cycleId)
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());
    if (search && search.trim()) params = params.set('search', search.trim());

    return this.http.get<ConsolidatorAreasBoardPagedDto>(`${this.baseUrl}/by-consolidator-area`, { params });
  }

  downloadPacc(cycleId: string): Observable<Blob> {
    const params = new HttpParams().set('cycleId', cycleId);
    return this.http.get(`${this.baseUrl}/pacc-export`, {
      params,
      responseType: 'blob',
    });
  }
}

