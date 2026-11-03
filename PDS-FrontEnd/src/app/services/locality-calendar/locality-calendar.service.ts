import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CalendarioEfectivo,
  CalendarioLocalidad,
  EstadoCalendario,
  ExcepcionCalendario,
  ExcepcionInput,
  JornadaInput,
  JornadaVersion,
  LocalidadGestionable,
  SuscripcionInput,
  ZonaHorariaActual,
} from '../../models/locality-calendar.model';

/** Calendario laboral de las localidades (RF-LO-22 a 36, 40; endpoints de §11.2). */
@Injectable({ providedIn: 'root' })
export class LocalityCalendarService {
  private http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}localidades`;

  // RF-LO-22
  zonaHoraria(id: string): Observable<ZonaHorariaActual> {
    return this.http.get<ZonaHorariaActual>(`${this.base}/${id}/zona-horaria`);
  }

  // RF-LO-24 a 31: definición (horarios y suscripciones).
  calendario(id: string): Observable<CalendarioLocalidad> {
    return this.http.get<CalendarioLocalidad>(`${this.base}/${id}/calendario`);
  }

  // RF-LO-34
  efectivo(id: string, desde: string, hasta: string): Observable<CalendarioEfectivo> {
    return this.http.get<CalendarioEfectivo>(`${this.base}/${id}/calendario/efectivo`, {
      params: new HttpParams().set('desde', desde).set('hasta', hasta),
    });
  }
  estado(id: string): Observable<EstadoCalendario> {
    return this.http.get<EstadoCalendario>(`${this.base}/${id}/calendario/estado`);
  }

  // RF-LO-25
  crearJornada(id: string, body: JornadaInput): Observable<JornadaVersion> {
    return this.http.post<JornadaVersion>(`${this.base}/${id}/calendario/jornadas`, body);
  }
  eliminarJornada(id: string, jornadaId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}/calendario/jornadas/${jornadaId}`);
  }

  // RF-LO-30: reemplaza el conjunto completo.
  reemplazarSuscripciones(id: string, body: SuscripcionInput[]): Observable<void> {
    return this.http.put<void>(`${this.base}/${id}/calendario/suscripciones`, body);
  }

  // RF-LO-31
  excepciones(id: string, desde: string, hasta: string): Observable<ExcepcionCalendario[]> {
    return this.http.get<ExcepcionCalendario[]>(`${this.base}/${id}/calendario/excepciones`, {
      params: new HttpParams().set('desde', desde).set('hasta', hasta),
    });
  }
  crearExcepcion(id: string, body: ExcepcionInput): Observable<ExcepcionCalendario> {
    return this.http.post<ExcepcionCalendario>(`${this.base}/${id}/calendario/excepciones`, body);
  }
  actualizarExcepcion(
    id: string,
    excepcionId: string,
    body: ExcepcionInput,
  ): Observable<ExcepcionCalendario> {
    return this.http.put<ExcepcionCalendario>(
      `${this.base}/${id}/calendario/excepciones/${excepcionId}`,
      body,
    );
  }
  eliminarExcepcion(id: string, excepcionId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}/calendario/excepciones/${excepcionId}`);
  }

  // RF-LO-36
  marcarPredeterminada(id: string): Observable<void> {
    return this.http.put<void>(`${this.base}/${id}/calendario/predeterminada`, {});
  }

  // RF-LO-40
  gestionables(): Observable<LocalidadGestionable[]> {
    return this.http.get<LocalidadGestionable[]>(`${this.base}/calendario/gestionables`);
  }
}
