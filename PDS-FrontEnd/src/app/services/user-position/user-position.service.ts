import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from '../auth/auth.service';
import { UserPositionPagedResultDto } from '../../models/user-position.model';

@Injectable({
  providedIn: 'root',
})
export class UserPositionService {
  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);

  getOrganizationApplicationId(): string | number {
    const token = this.authService.decodeToken();
    return token?.organization_application_id || 109;
  }

  getUserPositions(
    orgAppId?: string | number,
    searchPosition?: string,
    pageNumber: number = 1,
    pageSize: number = 10,
  ): Observable<UserPositionPagedResultDto> {
    const id = orgAppId || this.getOrganizationApplicationId();
    const url = `${environment.apiUrl}User/organization-application/${id}/position`;

    let params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    if (searchPosition && searchPosition.trim()) {
      params = params.set('searchposition', searchPosition.trim());
    }

    return this.http.get<UserPositionPagedResultDto>(url, { params });
  }
}
