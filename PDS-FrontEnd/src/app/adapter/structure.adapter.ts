import { Injectable } from '@angular/core';
import { StructureResponse, StructureVisualizer } from '../models/structure.model';

@Injectable({
  providedIn: 'root',
})
export class StructureAdapter {
  adapt(apiData: StructureResponse): StructureVisualizer {
    return {
      id: apiData.id,
      name: apiData.name,
      parentId: apiData?.parentId,
      areaTypeId: apiData?.areaTypeId,
      areaType: apiData.areaType,
      code: apiData.code,
      dependencies: apiData.dependencies,
    };
  }
}
