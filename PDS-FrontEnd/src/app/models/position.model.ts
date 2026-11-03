import { OccupationalGroup } from './occupational-group.model';
import { StructureResponse } from './structure.model';

export interface Position {
  id: string;
  name: string;
  occupationalGroup: OccupationalGroup;
  organizationalStructureId: string;
  organizationalStructureName: string;
  isManager: boolean;
  minSalary: string;
  maxSalary: string;
  competencies: string;
  yearsOfExperienceRequired?: string;
  natureOfWork?: string;
  typicalTasks?: string;
  requiredEducation?: string;
  characteristics?: string;
  otherDesirableRequirements?: string;
  probatoryPeriod?: string;
  punctuation?: string;
  grade?: string;
  code?: string;
}

export interface PositionResponse {
  id: string;
  name: string;
  occupationalGroup: OccupationalGroup;
  organizationalStructure: StructureResponse;
  isManager: boolean;
  minSalary: string;
  maxSalary: string;
  competencies: string;
  yearsOfExperienceRequired?: string;
  natureOfWork?: string;
  typicalTasks?: string;
  requiredEducation?: string;
  characteristics?: string;
  otherDesirableRequirements?: string;
  probatoryPeriod?: string;
  punctuation?: string;
  grade?: string;
  code?: string;
}

export interface PositionUpsert {
  id?: string;
  name?: string;
  occupationalGroupId?: string;
  organizationalStructureId?: string;
  isManager?: boolean;
  minSalary?: string;
  maxSalary?: string;
  competencies?: string;
  yearsOfExperienceRequired?: string;
  natureOfWork?: string;
  typicalTasks?: string;
  requiredEducation?: string;
  characteristics?: string;
  otherDesirableRequirements?: string;
  probatoryPeriod?: string;
  punctuation?: string;
  grade?: string;
  code?: string;
}
