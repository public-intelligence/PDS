import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  PlanningTemplateDto,
  PlanningTemplateDetailDto,
  CreatePlanningTemplateDto,
  UpdatePlanningTemplateDto,
  CreatePlanningTemplateColumnDto,
  UpdatePlanningTemplateColumnDto,
} from '../../modules/planning/data/planning-api.models';

@Injectable({ providedIn: 'root' })
export class PlanningTemplateService {
  private http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}planning-templates`;

  list(): Observable<PlanningTemplateDto[]> {
    return this.http.get<PlanningTemplateDto[]>(this.base);
  }

  getById(id: string): Observable<PlanningTemplateDetailDto> {
    return this.http.get<PlanningTemplateDetailDto>(`${this.base}/${id}`);
  }

  create(dto: CreatePlanningTemplateDto): Observable<PlanningTemplateDetailDto> {
    return this.http.post<PlanningTemplateDetailDto>(this.base, dto);
  }

  update(id: string, dto: UpdatePlanningTemplateDto): Observable<PlanningTemplateDetailDto> {
    return this.http.put<PlanningTemplateDetailDto>(`${this.base}/${id}`, dto);
  }

  addColumn(templateId: string, dto: CreatePlanningTemplateColumnDto): Observable<PlanningTemplateDetailDto> {
    return this.http.post<PlanningTemplateDetailDto>(`${this.base}/${templateId}/columns`, dto);
  }

  updateColumn(templateId: string, columnId: string, dto: UpdatePlanningTemplateColumnDto): Observable<PlanningTemplateDetailDto> {
    return this.http.put<PlanningTemplateDetailDto>(`${this.base}/${templateId}/columns/${columnId}`, dto);
  }

  activate(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/activate`, {});
  }

  seedStandardColumns(id: string): Observable<PlanningTemplateDetailDto> {
    return this.http.post<PlanningTemplateDetailDto>(`${this.base}/${id}/seed-standard-columns`, {});
  }

  deleteColumn(templateId: string, columnId: string): Observable<PlanningTemplateDetailDto> {
    return this.http.delete<PlanningTemplateDetailDto>(`${this.base}/${templateId}/columns/${columnId}`);
  }

  deleteTemplate(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
