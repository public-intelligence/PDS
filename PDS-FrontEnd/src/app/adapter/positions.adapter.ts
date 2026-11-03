import { Injectable } from '@angular/core';
import { Position, PositionResponse } from '../models/position.model';

@Injectable({
  providedIn: 'root',
})
export class PositionAdapter {
  adapt(apiData: PositionResponse): Position {
    return {
      id: apiData?.id,
      name: apiData?.name,
      occupationalGroup: apiData?.occupationalGroup,
      organizationalStructureId: apiData?.organizationalStructure?.id,
      organizationalStructureName: apiData?.organizationalStructure?.name,
      isManager: apiData?.isManager,
      minSalary: apiData?.minSalary,
      maxSalary: apiData?.maxSalary,
      competencies: apiData?.competencies,
      yearsOfExperienceRequired: apiData?.yearsOfExperienceRequired,
      natureOfWork: apiData?.natureOfWork,
      typicalTasks: apiData?.typicalTasks,
      requiredEducation: apiData?.requiredEducation,
      characteristics: apiData?.characteristics,
      otherDesirableRequirements: apiData?.otherDesirableRequirements,
      probatoryPeriod: apiData?.probatoryPeriod,
      punctuation: apiData?.punctuation,
      grade: apiData?.grade,
      code: apiData?.code,
    };
  }
}
