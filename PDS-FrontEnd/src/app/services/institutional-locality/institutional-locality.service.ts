import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams, HttpResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { PagedResult } from '../../models/paged-response.model';
import {
  CloseLocalityBody,
  DuplicateCheckQuery,
  DuplicateCheckResult,
  LocalityAudit,
  LocalityDetail,
  LocalityDocument,
  LocalityDocumentInput,
  LocalityListItem,
  LocalityListQuery,
  LocalityResponsibleLink,
  LocalityResponsibleInput,
  LocalityServiceLink,
  LocalityServiceInput,
  LocalityUnitPresence,
  LocalityUnitPresenceInput,
  LocalityUpsert,
  MapPoint,
  ReactivateLocalityBody,
  SuspendLocalityBody,
} from '../../models/institutional-locality.model';

/**
 * Servicio de localidades institucionales (RF-LO). Escrito a mano por la lista filtrada + paginada
 * (cabeceras), las acciones de ciclo de vida y los sub-recursos.
 */
@Injectable({ providedIn: 'root' })
export class InstitutionalLocalityService {
  private http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}localidades`;

  // RF-LO-15: lista filtrada + paginada (la paginación llega en cabeceras X-*).
  list(query: LocalityListQuery): Observable<PagedResult<LocalityListItem>> {
    return this.http
      .get<LocalityListItem[]>(this.base, { params: this.toParams(query), observe: 'response' })
      .pipe(map((resp) => this.toPaged(resp, query)));
  }

  // RF-LO-16: puntos del mapa.
  mapa(query: LocalityListQuery): Observable<MapPoint[]> {
    return this.http.get<MapPoint[]>(`${this.base}/mapa`, { params: this.toParams(query) });
  }

  // RF-LO-14: detalle.
  detail(id: string): Observable<LocalityDetail> {
    return this.http.get<LocalityDetail>(`${this.base}/${id}`);
  }

  // RF-LO-14: historial.
  historial(id: string): Observable<LocalityAudit[]> {
    return this.http.get<LocalityAudit[]>(`${this.base}/${id}/historial`);
  }

  // RF-LO-01.
  create(body: LocalityUpsert): Observable<LocalityDetail> {
    return this.http.post<LocalityDetail>(this.base, body);
  }

  // RF-LO-06.
  update(id: string, body: LocalityUpsert): Observable<LocalityDetail> {
    return this.http.put<LocalityDetail>(`${this.base}/${id}`, body);
  }

  // RF-LO-09 / RF-LO-10.
  suspender(id: string, body: SuspendLocalityBody): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/suspender`, body);
  }
  reactivar(id: string, body: ReactivateLocalityBody): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/reactivar`, body);
  }
  cerrar(id: string, body: CloseLocalityBody): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/cerrar`, body);
  }

  // RF-LO-13.
  validarDuplicado(query: DuplicateCheckQuery): Observable<DuplicateCheckResult> {
    return this.http.get<DuplicateCheckResult>(`${this.base}/validar-duplicado`, {
      params: this.toParams(query),
    });
  }

  // ── Sub-recursos ──
  getServicios(id: string): Observable<LocalityServiceLink[]> {
    return this.http.get<LocalityServiceLink[]>(`${this.base}/${id}/servicios`);
  }
  addServicio(id: string, body: LocalityServiceInput): Observable<LocalityServiceLink> {
    return this.http.post<LocalityServiceLink>(`${this.base}/${id}/servicios`, body);
  }
  removeServicio(id: string, linkId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}/servicios/${linkId}`);
  }

  getResponsables(id: string): Observable<LocalityResponsibleLink[]> {
    return this.http.get<LocalityResponsibleLink[]>(`${this.base}/${id}/responsables`);
  }
  addResponsable(id: string, body: LocalityResponsibleInput): Observable<LocalityResponsibleLink> {
    return this.http.post<LocalityResponsibleLink>(`${this.base}/${id}/responsables`, body);
  }
  removeResponsable(id: string, linkId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}/responsables/${linkId}`);
  }

  getDocumentos(id: string): Observable<LocalityDocument[]> {
    return this.http.get<LocalityDocument[]>(`${this.base}/${id}/documentos`);
  }
  addDocumento(id: string, body: LocalityDocumentInput): Observable<LocalityDocument> {
    return this.http.post<LocalityDocument>(`${this.base}/${id}/documentos`, body);
  }
  removeDocumento(id: string, linkId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}/documentos/${linkId}`);
  }

  getUnidadesPresencia(id: string): Observable<LocalityUnitPresence[]> {
    return this.http.get<LocalityUnitPresence[]>(`${this.base}/${id}/unidades-presencia`);
  }
  addUnidadPresencia(id: string, body: LocalityUnitPresenceInput): Observable<LocalityUnitPresence> {
    return this.http.post<LocalityUnitPresence>(`${this.base}/${id}/unidades-presencia`, body);
  }
  removeUnidadPresencia(id: string, linkId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}/unidades-presencia/${linkId}`);
  }

  // ── Utilidades ──
  private toParams(query: object): HttpParams {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(query)) {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, String(value));
      }
    }
    return params;
  }

  /** Lee la paginación de las cabeceras; degrada con sensatez si faltan. */
  private toPaged(
    resp: HttpResponse<LocalityListItem[]>,
    query: LocalityListQuery,
  ): PagedResult<LocalityListItem> {
    const items = resp.body ?? [];
    const num = (h: string, fallback: number) => {
      const v = Number(resp.headers.get(h));
      return Number.isFinite(v) && v > 0 ? v : fallback;
    };
    const page = num('X-Current-Page', query.pageNumber ?? 1);
    const pageSize = num('X-Page-Size', query.pageSize ?? 20);
    const total = num('X-Total', items.length);
    const totalPages = num('X-Page-Count', Math.max(1, Math.ceil(total / pageSize)));
    return { items, total, page, pageSize, totalPages };
  }
}
