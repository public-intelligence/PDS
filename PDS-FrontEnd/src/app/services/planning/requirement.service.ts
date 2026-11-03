import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  RequirementDto,
  RequirementDetailDto,
  CreateRequirementDto,
  UpdateRequirementDto,
  RequirementDecisionDto,
  ConfirmPriceDto,
  ReduceRequirementDto,
  MergeRequirementsDto,
  AddRequirementCommentDto,
  RequirementCommentDto,
  PagedResponseDto,
  TaskRequirementsDto,
} from '../../modules/planning/data/planning-api.models';

/**
 * Cliente de `api/v1/requirements`.
 *
 * El área origen y la consolidadora las deriva el controlador del contexto del token
 * (`IPlanningContextService`), por eso ninguna operación las manda como query param.
 */
@Injectable({
  providedIn: 'root',
})
export class RequirementService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}requirements`;

  listByOrigin(
    cycleId: string,
    areaId: string,
    status?: string,
    pageNumber = 1,
    pageSize = 20,
  ): Observable<PagedResponseDto<RequirementDto>> {
    let params = new HttpParams()
      .set('originAreaId', areaId)
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    if (cycleId) params = params.set('cycleId', cycleId);
    if (status) params = params.set('status', status);

    return this.http.get<PagedResponseDto<RequirementDto>>(this.baseUrl, { params });
  }

  listByConsolidator(
    cycleId: string,
    consolidatorAreaId: string,
    status?: string,
    pageNumber = 1,
    pageSize = 20,
  ): Observable<PagedResponseDto<RequirementDto>> {
    let params = new HttpParams()
      .set('consolidatorAreaId', consolidatorAreaId)
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    if (cycleId) params = params.set('cycleId', cycleId);
    if (status) params = params.set('status', status);

    return this.http.get<PagedResponseDto<RequirementDto>>(this.baseUrl, { params });
  }

  listByTask(taskId: string): Observable<RequirementDto[]> {
    return this.http.get<RequirementDto[]>(`${this.baseUrl}/by-task/${taskId}`);
  }

  getById(id: string): Observable<RequirementDetailDto> {
    return this.http.get<RequirementDetailDto>(`${this.baseUrl}/${id}`);
  }

  create(dto: CreateRequirementDto): Observable<RequirementDetailDto> {
    return this.http.post<RequirementDetailDto>(this.baseUrl, dto);
  }

  update(id: string, dto: UpdateRequirementDto): Observable<RequirementDetailDto> {
    return this.http.put<RequirementDetailDto>(`${this.baseUrl}/${id}`, dto);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  getTaskRequirements(taskId: string): Observable<TaskRequirementsDto[]> {
    return this.http.get<TaskRequirementsDto[]>(`${this.baseUrl}/task/${taskId}`);
  }

  confirmPrice(id: string, dto: ConfirmPriceDto): Observable<RequirementDetailDto> {
    return this.http.post<RequirementDetailDto>(`${this.baseUrl}/${id}/confirm-price`, dto);
  }

  decide(id: string, dto: RequirementDecisionDto): Observable<RequirementDetailDto> {
    return this.http.post<RequirementDetailDto>(`${this.baseUrl}/${id}/decide`, dto);
  }

  reduce(id: string, dto: ReduceRequirementDto): Observable<RequirementDetailDto> {
    return this.http.post<RequirementDetailDto>(`${this.baseUrl}/${id}/reduce`, dto);
  }

  merge(dto: MergeRequirementsDto): Observable<RequirementDetailDto> {
    return this.http.post<RequirementDetailDto>(`${this.baseUrl}/merge`, dto);
  }

  addComment(
    id: string,
    dto: AddRequirementCommentDto,
    areaName?: string,
  ): Observable<RequirementCommentDto> {
    let params = new HttpParams();
    if (areaName) {
      params = params.set('areaName', areaName);
    }
    return this.http.post<RequirementCommentDto>(`${this.baseUrl}/${id}/comments`, dto, { params });
  }
}
