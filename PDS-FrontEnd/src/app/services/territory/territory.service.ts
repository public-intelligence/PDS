import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Distrito, Municipio, Pais, Provincia, Region } from '../../models/territory.model';

/** Lectura en cascada del catálogo territorial (RF-LO). */
@Injectable({ providedIn: 'root' })
export class TerritoryService {
  private http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}territorio`;

  paises(): Observable<Pais[]> {
    return this.http.get<Pais[]>(`${this.base}/paises`);
  }

  regiones(paisId?: string): Observable<Region[]> {
    let params = new HttpParams();
    if (paisId) params = params.set('paisId', paisId);
    return this.http.get<Region[]>(`${this.base}/regiones`, { params });
  }

  provincias(regionId?: string, paisId?: string): Observable<Provincia[]> {
    let params = new HttpParams();
    if (regionId) params = params.set('regionId', regionId);
    if (paisId) params = params.set('paisId', paisId);
    return this.http.get<Provincia[]>(`${this.base}/provincias`, { params });
  }

  municipios(provinciaId: string): Observable<Municipio[]> {
    return this.http.get<Municipio[]>(`${this.base}/municipios`, {
      params: new HttpParams().set('provinciaId', provinciaId),
    });
  }

  distritos(municipioId: string): Observable<Distrito[]> {
    return this.http.get<Distrito[]>(`${this.base}/distritos`, {
      params: new HttpParams().set('municipioId', municipioId),
    });
  }
}
