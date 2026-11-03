import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Observable, catchError, of, throwError } from 'rxjs';
import { CrudService } from '../crud.service';
import { environment } from '../../../environments/environment';
import {
  EstadoLocalidad,
  EstadoLocalidadUpsert,
  FuncionLocalidad,
  FuncionLocalidadUpsert,
  NaturalezaAdministrativa,
  NaturalezaAdministrativaUpsert,
  Responsable,
  ResponsableUpsert,
  Servicio,
  ServicioUpsert,
  TipoLocalidad,
  TipoLocalidadUpsert,
  ZonaHoraria,
} from '../../models/locality-catalog.model';
import { browserTimeZones } from '../../utils/time-zone';

@Injectable({ providedIn: 'root' })
export class TipoLocalidadService extends CrudService<TipoLocalidad, TipoLocalidad, TipoLocalidadUpsert> {
  protected override readonly endpoint = `${environment.apiUrl}catalogos/tipos-localidad`;
}

@Injectable({ providedIn: 'root' })
export class NaturalezaAdministrativaService extends CrudService<
  NaturalezaAdministrativa,
  NaturalezaAdministrativa,
  NaturalezaAdministrativaUpsert
> {
  protected override readonly endpoint = `${environment.apiUrl}catalogos/naturalezas-administrativas`;
}

@Injectable({ providedIn: 'root' })
export class EstadoLocalidadService extends CrudService<
  EstadoLocalidad,
  EstadoLocalidad,
  EstadoLocalidadUpsert
> {
  protected override readonly endpoint = `${environment.apiUrl}catalogos/estados-localidad`;
}

@Injectable({ providedIn: 'root' })
export class ServicioService extends CrudService<Servicio, Servicio, ServicioUpsert> {
  protected override readonly endpoint = `${environment.apiUrl}catalogos/servicios`;
}

@Injectable({ providedIn: 'root' })
export class ResponsableService extends CrudService<Responsable, Responsable, ResponsableUpsert> {
  protected override readonly endpoint = `${environment.apiUrl}catalogos/responsables`;
}

@Injectable({ providedIn: 'root' })
export class FuncionLocalidadService extends CrudService<
  FuncionLocalidad,
  FuncionLocalidad,
  FuncionLocalidadUpsert
> {
  protected override readonly endpoint = `${environment.apiUrl}catalogos/funciones-localidad`;
}

/** Catálogo de zonas horarias IANA (RF-LO-21). */
@Injectable({ providedIn: 'root' })
export class ZonaHorariaService {
  private http = inject(HttpClient);
  private readonly endpoint = `${environment.apiUrl}catalogos/zonas-horarias`;

  /** Zonas del país (`paisCodigo` ISO 3166-1 alfa-2); todas si se omite. */
  list(paisCodigo?: string): Observable<ZonaHoraria[]> {
    let params = new HttpParams();
    if (paisCodigo) params = params.set('paisCodigo', paisCodigo);
    return this.http.get<ZonaHoraria[]>(this.endpoint, { params }).pipe(
      // El catálogo aún no está desplegado en todos los ambientes: se usa el del navegador.
      catchError((err: HttpErrorResponse) =>
        err.status === 404 ? of(browserTimeZones(paisCodigo)) : throwError(() => err),
      ),
    );
  }
}
