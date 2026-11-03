import { Injectable } from '@angular/core';
import { AreaType, AreaTypeUpsert } from '../../models/area-type';
import { AreaTypeAdapter } from '../../adapter/area-type.adapter';
import { CrudService } from '../crud.service';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class AreaTypeService extends CrudService<AreaType, AreaType, AreaTypeUpsert> {
  protected override readonly endpoint = `${environment.apiUrl}areatypes`;
  private adapter = new AreaTypeAdapter();

  protected override adapt(item: AreaType): AreaType {
    return this.adapter.adapt(item);
  }
}
