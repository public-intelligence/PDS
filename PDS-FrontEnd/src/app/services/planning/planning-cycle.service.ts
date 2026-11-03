import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  PlanningCycleDto,
  PlanningCycleDetailDto,
  CreatePlanningCycleDto,
  UpdatePlanningCycleDto,
  EligibleAreaDto,
} from '../../modules/planning/data/planning-api.models';

@Injectable({
  providedIn: 'root',
})
export class PlanningCycleService {
  private readonly http = inject(HttpClient);
  // environment.apiUrl ya trae "/api/v1/" al final (mismo patrón que el resto de servicios).
  private readonly baseUrl = `${environment.apiUrl}planning-cycles`;

  list(): Observable<PlanningCycleDto[]> {
    return this.http.get<PlanningCycleDto[]>(this.baseUrl);
  }

  getEligibleAreas(): Observable<EligibleAreaDto[]> {
    return this.http.get<EligibleAreaDto[]>(`${this.baseUrl}/eligible-areas`);
  }

  getById(id: string): Observable<PlanningCycleDetailDto> {
    return this.http.get<PlanningCycleDetailDto>(`${this.baseUrl}/${id}`);
  }

  createDraft(dto: CreatePlanningCycleDto): Observable<PlanningCycleDetailDto> {
    return this.http.post<PlanningCycleDetailDto>(this.baseUrl, dto);
  }

  updateDraft(id: string, dto: UpdatePlanningCycleDto): Observable<PlanningCycleDetailDto> {
    return this.http.put<PlanningCycleDetailDto>(`${this.baseUrl}/${id}`, dto);
  }

  publish(id: string): Observable<PlanningCycleDetailDto> {
    return this.http.post<PlanningCycleDetailDto>(`${this.baseUrl}/${id}/publish`, {});
  }

  close(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/close`, {});
  }
}
