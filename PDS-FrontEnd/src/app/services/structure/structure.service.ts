import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { CrudService } from '../crud.service';
import { StructureAdapter } from '../../adapter/structure.adapter';
import {
  StructureResponse,
  StructureUpsert,
  StructureVisualizer,
} from '../../models/structure.model';
import { StructureTemplate } from '../../models/structure-template.model';
import { ConsolidatorUnitImpactDto } from '../../modules/planning/data/planning-api.models';

@Injectable({
  providedIn: 'root',
})
export class StructureService extends CrudService<
  StructureResponse,
  StructureVisualizer,
  StructureUpsert
> {
  protected override readonly endpoint = `${environment.apiUrl}organizational-structures`;
  private adapter = new StructureAdapter();

  protected override adapt(item: StructureResponse): StructureVisualizer {
    return this.adapter.adapt(item);
  }

  createStructure(body: StructureUpsert): Observable<void> {
    return this.http.post<void>(this.endpoint, body);
  }

  updateStructure(id: string, body: StructureUpsert): Observable<void> {
    return this.http.put<void>(`${this.endpoint}/${id}`, body);
  }

  /**
   * Importa una estructura organizacional completa desde una plantilla portable.
   * El backend valida el formato, regenera códigos/identificadores y la crea de forma atómica
   * dentro del borrador en curso.
   */
  importStructure(body: StructureTemplate): Observable<StructureVisualizer[]> {
    return this.http
      .post<StructureResponse[]>(`${this.endpoint}/import`, body)
      .pipe(map((response) => response.map((item) => this.adapt(item))));
  }

  /**
   * Consulta el impacto de gobernanza al eliminar o desactivar una unidad organizativa.
   */
  getDeletionImpact(id: string): Observable<ConsolidatorUnitImpactDto> {
    return this.http.get<ConsolidatorUnitImpactDto>(`${this.endpoint}/${id}/deletion-impact`);
  }
}
