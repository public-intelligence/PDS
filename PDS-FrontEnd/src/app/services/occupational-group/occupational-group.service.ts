import { Injectable } from '@angular/core';
import { CrudService } from '../crud.service';
import {
  OccupationalGroup,
  OccupationalGroupResponse,
} from '../../models/occupational-group.model';
import { OccupationalGroupAdapter } from '../../adapter/occupational-group.adapter';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class OccupationalGroupService extends CrudService<
  OccupationalGroupResponse,
  OccupationalGroup,
  { name: string }
> {
  protected override readonly endpoint = `${environment.apiUrl}occupational-groups`;
  private adapter = new OccupationalGroupAdapter();

  protected override adapt(item: OccupationalGroupResponse): OccupationalGroup {
    return this.adapter.adapt(item);
  }
}
