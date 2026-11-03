import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpContext, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { SKIP_AUTH } from '../../interceptors/http-context';
import { OrgChart, OrgChartQuery, PublicOrganization } from '../../models/org-chart.model';
import { StructureVersionSummary } from '../../models/structure-version.model';

/**
 * Consume únicamente los endpoints públicos (/public/org-chart). Marca cada petición con SKIP_AUTH
 * para que el interceptor no adjunte token ni fuerce el login del visitante anónimo.
 */
@Injectable({ providedIn: 'root' })
export class PublicOrgChartService {
  private http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}public/org-chart`;

  current(query: OrgChartQuery = {}): Observable<OrgChart> {
    return this.http.get<OrgChart>(`${this.base}/current`, this.opts(query));
  }

  asOf(date: string, query: OrgChartQuery = {}): Observable<OrgChart> {
    return this.http.get<OrgChart>(`${this.base}/as-of`, this.opts({ ...query, date }));
  }

  byNumber(num: number, query: OrgChartQuery = {}): Observable<OrgChart> {
    return this.http.get<OrgChart>(`${this.base}/versions/${num}`, this.opts(query));
  }

  versions(organizationId?: number): Observable<StructureVersionSummary[]> {
    return this.http.get<StructureVersionSummary[]>(`${this.base}/versions`, this.opts({ organizationId }));
  }

  /** Resuelve la organización (tenant) por slug para el organigrama público multitenant. */
  organization(slug: string): Observable<PublicOrganization> {
    return this.http.get<PublicOrganization>(
      `${this.base}/organization/${encodeURIComponent(slug)}`,
      { context: new HttpContext().set(SKIP_AUTH, true) },
    );
  }

  private opts(query: OrgChartQuery): { params: HttpParams; context: HttpContext } {
    let params = new HttpParams();
    if (query.date) params = params.set('date', query.date);
    if (query.organizationId != null) params = params.set('organizationId', query.organizationId);
    if (query.rootUnitId) params = params.set('rootUnitId', query.rootUnitId);
    if (query.maxLevel != null) params = params.set('maxLevel', query.maxLevel);
    if (query.includePositions != null) params = params.set('includePositions', query.includePositions);
    return { params, context: new HttpContext().set(SKIP_AUTH, true) };
  }
}
