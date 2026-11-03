import { Injectable } from '@angular/core';
import { AreaType } from '../models/area-type';
@Injectable({
  providedIn: 'root',
})
export class AreaTypeAdapter {
  adapt(apiData: AreaType): AreaType {
    return {
      id: apiData.id,
      name: apiData?.name,
      order: apiData?.order,
      parentId: apiData?.parentId,
    };
  }
}
