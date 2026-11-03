import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { MasterDataImportResultDto } from '../../modules/planning/data/planning-api.models';

@Injectable({
  providedIn: 'root'
})
export class MasterDataImportService {
  private baseUrl = `${environment.apiUrl}master-data`;

  constructor(private http: HttpClient) {}

  importFile(file: File, dryRun = false): Observable<MasterDataImportResultDto> {
    const formData = new FormData();
    formData.append('file', file, file.name);

    let params = new HttpParams().set('dryRun', dryRun.toString());
    return this.http.post<MasterDataImportResultDto>(`${this.baseUrl}/import`, formData, { params });
  }

  importFromRepository(dryRun = false): Observable<MasterDataImportResultDto> {
    let params = new HttpParams().set('dryRun', dryRun.toString());
    return this.http.post<MasterDataImportResultDto>(`${this.baseUrl}/import/from-repository`, {}, { params });
  }
}
