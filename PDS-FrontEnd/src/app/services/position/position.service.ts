import { Injectable } from '@angular/core';

import { environment } from '../../../environments/environment';
import { CrudService } from '../crud.service';
import { PositionAdapter } from '../../adapter/positions.adapter';
import { Position, PositionResponse, PositionUpsert } from '../../models/position.model';

@Injectable({
  providedIn: 'root',
})
export class PositionService extends CrudService<PositionResponse, Position, PositionUpsert> {
  protected override readonly endpoint = `${environment.apiUrl}positions`;
  private adapter = new PositionAdapter();

  protected override adapt(item: PositionResponse): Position {
    return this.adapter.adapt(item);
  }
}
