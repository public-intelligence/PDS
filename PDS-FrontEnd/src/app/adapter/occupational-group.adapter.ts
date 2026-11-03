import { Injectable } from '@angular/core';
import {
  OccupationalGroup,
  OccupationalGroupResponse,
} from '../models/occupational-group.model';

@Injectable({
  providedIn: 'root',
})
export class OccupationalGroupAdapter {
  adapt(apiData: OccupationalGroupResponse): OccupationalGroup {
    return {
      id: apiData.id,
      name: apiData.name,
    };
  }
}
