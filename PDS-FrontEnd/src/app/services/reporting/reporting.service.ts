import { HttpClient, HttpParams, HttpResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ReportingContext } from '../../models/reporting/reporting-context.model';
import {
  AuthorizedPostsReport,
  ComparativeReport,
  EstimatedCostReport,
  ReportGrouping,
  SalaryBasis,
  StaffingComplementReport,
  StructuralChangesFilter,
  StructuralChangesReport,
} from '../../models/reporting/reporting-reports.model';

export interface CompareOptions {
  fromVersionId?: string;
  fromDate?: string;
  toVersionId?: string;
  toDate?: string;
  basis?: SalaryBasis;
}

// RF-RE-01..06: datos de reportes (JSON) + exportaciones (Blob). Mismo contexto/filtros ⇒ exportación
// idéntica a la pantalla. Las descargas usan observe:'response' para leer Content-Disposition.
@Injectable({ providedIn: 'root' })
export class ReportingService {
  private http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}reports`;

  // ───────── datos ─────────

  authorizedPosts(ctx: ReportingContext, groupBy: ReportGrouping): Observable<AuthorizedPostsReport> {
    return this.http.get<AuthorizedPostsReport>(`${this.base}/authorized-posts`, {
      params: this.params(ctx, { groupBy }),
    });
  }

  staffingComplement(ctx: ReportingContext): Observable<StaffingComplementReport> {
    return this.http.get<StaffingComplementReport>(`${this.base}/staffing-complement`, {
      params: this.params(ctx),
    });
  }

  estimatedCost(ctx: ReportingContext, basis: SalaryBasis, groupBy: ReportGrouping): Observable<EstimatedCostReport> {
    return this.http.get<EstimatedCostReport>(`${this.base}/estimated-cost`, {
      params: this.params(ctx, { basis, groupBy }),
    });
  }

  comparative(opts: CompareOptions): Observable<ComparativeReport> {
    return this.http.get<ComparativeReport>(`${this.base}/comparative`, {
      params: this.compareParams(opts),
    });
  }

  structuralChanges(filter: StructuralChangesFilter): Observable<StructuralChangesReport> {
    return this.http.get<StructuralChangesReport>(`${this.base}/structural-changes`, {
      params: this.filterParams(filter),
    });
  }

  // ───────── exportaciones (Blob) ─────────

  exportAuthorizedPosts(ctx: ReportingContext, groupBy: ReportGrouping, format: string): Observable<HttpResponse<Blob>> {
    return this.blob(`${this.base}/authorized-posts/export`, this.params(ctx, { groupBy, format }));
  }

  exportStaffingComplement(ctx: ReportingContext, format: string): Observable<HttpResponse<Blob>> {
    return this.blob(`${this.base}/staffing-complement/export`, this.params(ctx, { format }));
  }

  exportEstimatedCost(ctx: ReportingContext, basis: SalaryBasis, groupBy: ReportGrouping, format: string): Observable<HttpResponse<Blob>> {
    return this.blob(`${this.base}/estimated-cost/export`, this.params(ctx, { basis, groupBy, format }));
  }

  exportComparative(opts: CompareOptions, format: string): Observable<HttpResponse<Blob>> {
    return this.blob(`${this.base}/comparative/export`, this.compareParams(opts).set('format', format));
  }

  exportStructuralChanges(filter: StructuralChangesFilter, format: string): Observable<HttpResponse<Blob>> {
    return this.blob(`${this.base}/structural-changes/export`, this.filterParams(filter).set('format', format));
  }

  // ───────── helpers ─────────

  private blob(url: string, params: HttpParams): Observable<HttpResponse<Blob>> {
    return this.http.get(url, { params, responseType: 'blob', observe: 'response' });
  }

  private params(ctx: ReportingContext, extra: Record<string, string> = {}): HttpParams {
    let params = new HttpParams();
    if (ctx.versionId) params = params.set('versionId', ctx.versionId);
    else if (ctx.asOfDate) params = params.set('asOfDate', ctx.asOfDate);
    for (const [key, value] of Object.entries(extra)) params = params.set(key, value);
    return params;
  }

  private compareParams(opts: CompareOptions): HttpParams {
    let params = new HttpParams();
    if (opts.fromVersionId) params = params.set('fromVersionId', opts.fromVersionId);
    if (opts.fromDate) params = params.set('fromDate', opts.fromDate);
    if (opts.toVersionId) params = params.set('toVersionId', opts.toVersionId);
    if (opts.toDate) params = params.set('toDate', opts.toDate);
    if (opts.basis) params = params.set('basis', opts.basis);
    return params;
  }

  private filterParams(filter: StructuralChangesFilter): HttpParams {
    let params = new HttpParams().set('from', filter.from).set('to', filter.to);
    if (filter.changeType) params = params.set('changeType', filter.changeType);
    if (filter.unitId) params = params.set('unitId', filter.unitId);
    if (filter.approverId) params = params.set('approverId', filter.approverId);
    return params;
  }
}
