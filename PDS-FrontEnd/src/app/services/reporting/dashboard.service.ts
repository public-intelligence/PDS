import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ReportingContext } from '../../models/reporting/reporting-context.model';
import { ReportingDashboard } from '../../models/reporting/reporting-dashboard.model';

// RF-RE-07: KPIs definicionales del contexto en un solo payload.
@Injectable({ providedIn: 'root' })
export class DashboardService {
  private http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}dashboard`;

  getDashboard(ctx: ReportingContext): Observable<ReportingDashboard> {
    let params = new HttpParams();
    if (ctx.versionId) params = params.set('versionId', ctx.versionId);
    else if (ctx.asOfDate) params = params.set('asOfDate', ctx.asOfDate);
    return this.http.get<ReportingDashboard>(this.base, { params });
  }
}
