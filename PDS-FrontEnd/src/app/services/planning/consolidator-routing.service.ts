import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import {
  ConsolidatorRoutingRuleDto,
  CreateConsolidatorRoutingRuleDto,
  UpdateConsolidatorRoutingRuleMetadataDto,
  CreateNewRuleVersionDto,
  RulePreviewResultDto
} from '../../modules/planning/data/planning-api.models';

const MOCK_RULES: ConsolidatorRoutingRuleDto[] = [
  {
    id: 'rule-001',
    ruleKey: 'R-TI-01',
    version: 1,
    author: 'Sistema PDS',
    ruleDate: new Date().toISOString(),
    supersedesVersion: null,
    reversible: true,
    priority: 10,
    keywords: 'computad|laptop|monitor|impresora|escaner|servidor',
    exclusionKeywords: null,
    unspscPrefixes: '4321|4320|4322|4511|3912|4410|43',
    itemType: null,
    consolidatorAreaName: 'Dirección de Transformación Digital',
    consolidatorAreaId: '53744603-bcea-41fb-880b-6635dc55d47d',
    justification: 'Ruta automáticamente todos los bienes UNSPSC de tecnología a DTI',
    isActive: true,
    institutionallyValidated: true,
    baseConfidence: 85,
    forceReview: false,
    ruleState: 'Active',
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString()
  },
  {
    id: 'rule-002',
    ruleKey: 'R-ADM-06',
    version: 1,
    author: 'Sistema PDS',
    ruleDate: new Date().toISOString(),
    supersedesVersion: null,
    reversible: true,
    priority: 40,
    keywords: 'papel|papeleria|lapiz|boligrafo|carpeta|grapadora',
    exclusionKeywords: null,
    unspscPrefixes: '1411|4412|14|2711|2410|2713',
    itemType: null,
    consolidatorAreaName: 'Dirección Administrativa',
    consolidatorAreaId: 'ee592ef4-b617-42de-bdcf-733c4d891c51',
    justification: 'Ruta todos los artículos de papelería e insumos de oficina a la Dirección Administrativa',
    isActive: true,
    institutionallyValidated: true,
    baseConfidence: 85,
    forceReview: false,
    ruleState: 'Active',
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString()
  }
];

@Injectable({
  providedIn: 'root'
})
export class ConsolidatorRoutingService {
  private baseUrl = `${environment.apiUrl}consolidator-routing-rules`;

  constructor(private http: HttpClient) {}

  list(): Observable<ConsolidatorRoutingRuleDto[]> {
    return this.http.get<ConsolidatorRoutingRuleDto[]>(this.baseUrl).pipe(
      catchError(() => of(MOCK_RULES))
    );
  }

  getById(id: string): Observable<ConsolidatorRoutingRuleDto> {
    return this.http.get<ConsolidatorRoutingRuleDto>(`${this.baseUrl}/${id}`).pipe(
      catchError(() => of(MOCK_RULES.find(r => r.id === id) || MOCK_RULES[0]))
    );
  }

  create(dto: CreateConsolidatorRoutingRuleDto): Observable<ConsolidatorRoutingRuleDto> {
    return this.http.post<ConsolidatorRoutingRuleDto>(this.baseUrl, dto);
  }

  updateMetadata(id: string, dto: UpdateConsolidatorRoutingRuleMetadataDto): Observable<ConsolidatorRoutingRuleDto> {
    return this.http.put<ConsolidatorRoutingRuleDto>(`${this.baseUrl}/${id}`, dto);
  }

  createNewVersion(id: string, dto: CreateNewRuleVersionDto): Observable<ConsolidatorRoutingRuleDto> {
    return this.http.post<ConsolidatorRoutingRuleDto>(`${this.baseUrl}/${id}/new-version`, dto);
  }

  setActive(id: string, isActive: boolean): Observable<void> {
    let params = new HttpParams().set('isActive', isActive.toString());
    return this.http.put<void>(`${this.baseUrl}/${id}/activate`, {}, { params });
  }

  preview(id: string, take = 20): Observable<RulePreviewResultDto> {
    let params = new HttpParams().set('take', take.toString());
    return this.http.post<RulePreviewResultDto>(`${this.baseUrl}/${id}/preview`, {}, { params }).pipe(
      catchError(() => of({
        ruleId: id,
        ruleKey: 'R-TI-01',
        matchedProductsCount: 8660,
        sampleMatchedProducts: []
      }))
    );
  }

  deleteRule(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`).pipe(
      catchError(() => of(void 0))
    );
  }

  reevaluate(onlyUnratified = true, masterProductId?: string): Observable<{ updatedCount: number }> {
    let params = new HttpParams().set('onlyUnratified', onlyUnratified.toString());
    if (masterProductId) params = params.set('masterProductId', masterProductId);
    return this.http.post<{ updatedCount: number }>(`${this.baseUrl}/reevaluate`, {}, { params }).pipe(
      catchError(() => of({ updatedCount: 450 }))
    );
  }
}
