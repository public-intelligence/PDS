import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CreateStructureVersion,
  PublishVersion,
  StructureVersionDetail,
  StructureVersionSummary,
  VersionStatus,
} from '../../models/structure-version.model';
import { OrgChart, OrgChartQuery } from '../../models/org-chart.model';
import { VersionComparison } from '../../models/structure-diff.model';
import { StructureVersionImpactReportDto } from '../../modules/planning/data/planning-api.models';

@Injectable({ providedIn: 'root' })
export class StructureVersionService {
  private http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}structure-versions`;

  list(status?: VersionStatus): Observable<StructureVersionSummary[]> {
    let params = new HttpParams();
    if (status) params = params.set('status', status);
    return this.http.get<StructureVersionSummary[]>(this.base, { params });
  }

  get(id: string): Observable<StructureVersionDetail> {
    return this.http.get<StructureVersionDetail>(`${this.base}/${id}`);
  }

  createDraft(body: CreateStructureVersion): Observable<StructureVersionDetail> {
    return this.http.post<StructureVersionDetail>(this.base, body);
  }

  submitReview(id: string, comment?: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/submit-review`, { comment });
  }

  approve(id: string, comment?: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/approve`, { comment });
  }

  reject(id: string, comment: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/reject`, { comment });
  }

  publish(id: string, body: PublishVersion): Observable<StructureVersionSummary> {
    return this.http.post<StructureVersionSummary>(`${this.base}/${id}/publish`, body);
  }

  orgChart(id: string, query: OrgChartQuery = {}): Observable<OrgChart> {
    return this.http.get<OrgChart>(`${this.base}/${id}/org-chart`, { params: this.toParams(query) });
  }

  asOf(date: string, query: OrgChartQuery = {}): Observable<OrgChart> {
    return this.http.get<OrgChart>(`${this.base}/as-of`, { params: this.toParams({ ...query, date }) });
  }

  compare(opts: {
    fromVersionId?: string;
    fromDate?: string;
    toVersionId?: string;
    toDate?: string;
  }): Observable<VersionComparison> {
    let params = new HttpParams();
    if (opts.fromVersionId) params = params.set('fromVersionId', opts.fromVersionId);
    if (opts.fromDate) params = params.set('fromDate', opts.fromDate);
    if (opts.toVersionId) params = params.set('toVersionId', opts.toVersionId);
    if (opts.toDate) params = params.set('toDate', opts.toDate);
    return this.http.get<VersionComparison>(`${this.base}/compare`, { params });
  }

  getConsolidatorImpact(versionId: string): Observable<StructureVersionImpactReportDto> {
    return this.http.get<StructureVersionImpactReportDto>(`${this.base}/${versionId}/consolidator-impact`);
  }

  private toParams(query: OrgChartQuery): HttpParams {
    let params = new HttpParams();
    if (query.date) params = params.set('date', query.date);
    if (query.rootUnitId) params = params.set('rootUnitId', query.rootUnitId);
    if (query.maxLevel != null) params = params.set('maxLevel', query.maxLevel);
    if (query.includePositions != null) params = params.set('includePositions', query.includePositions);
    return params;
  }
}
