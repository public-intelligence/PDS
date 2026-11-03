import { Injectable } from '@angular/core';
import { HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CrudService } from '../crud.service';
import { environment } from '../../../environments/environment';
import {
  CalendarioFeriados,
  CalendarioFeriadosUpsert,
  Feriado,
  FeriadoInput,
  ImportacionResultado,
  ModoImportacion,
} from '../../models/holiday-calendar.model';

export interface ImportacionOpciones {
  modo: ModoImportacion;
  /** Año que se reemplaza en modo `reemplazarAnio`. */
  anio?: number;
  /** Ensayo: valida sin guardar. */
  validarSolamente: boolean;
}

/** Calendarios de feriados compartidos y sus días (RF-LO-27 a 29, 40). */
@Injectable({ providedIn: 'root' })
export class HolidayCalendarService extends CrudService<
  CalendarioFeriados,
  CalendarioFeriados,
  CalendarioFeriadosUpsert
> {
  protected override readonly endpoint = `${environment.apiUrl}catalogos/calendarios-feriados`;

  // RF-LO-40: calendarios cuyos feriados puede mantener el usuario.
  gestionables(): Observable<CalendarioFeriados[]> {
    return this.http.get<CalendarioFeriados[]>(`${this.endpoint}/gestionables`);
  }

  // RF-LO-28
  dias(id: string, anio: number): Observable<Feriado[]> {
    return this.http.get<Feriado[]>(`${this.endpoint}/${id}/dias`, {
      params: new HttpParams().set('anio', anio),
    });
  }
  crearDia(id: string, body: FeriadoInput): Observable<Feriado> {
    return this.http.post<Feriado>(`${this.endpoint}/${id}/dias`, body);
  }
  actualizarDia(id: string, diaId: string, body: FeriadoInput): Observable<Feriado> {
    return this.http.put<Feriado>(`${this.endpoint}/${id}/dias/${diaId}`, body);
  }
  eliminarDia(id: string, diaId: string): Observable<void> {
    return this.http.delete<void>(`${this.endpoint}/${id}/dias/${diaId}`);
  }

  /** RF-LO-29: `contenido` es un arreglo JSON o un CSV, según `esCsv`. */
  importar(
    id: string,
    contenido: string,
    esCsv: boolean,
    opciones: ImportacionOpciones,
  ): Observable<ImportacionResultado> {
    let params = new HttpParams()
      .set('modo', opciones.modo)
      .set('validarSolamente', opciones.validarSolamente);
    if (opciones.anio) params = params.set('anio', opciones.anio);
    return this.http.post<ImportacionResultado>(`${this.endpoint}/${id}/dias/importar`, contenido, {
      params,
      headers: { 'Content-Type': esCsv ? 'text/csv' : 'application/json' },
    });
  }
}
