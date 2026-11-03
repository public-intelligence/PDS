import { AreaType } from './area-type';

interface Structure {
  id: string;
  parentId?: string;
  areaTypeId: string;
  areaType: AreaType;
  code: string;
  name: string;
  showDependencies?: boolean;
  dependencies: Structure[];
}

export interface StructureResponse {
  id: string;
  code: string;
  name: string;
  parentId?: string;
  areaTypeId: string;
  areaType: AreaType;
  dependencies: Structure[];
  parent?: string;
}

export interface StructureUpsert {
  id?: string;
  parentId?: string | null;
  parentName?: string;
  areaTypeId: string;
  name?: string;
  code?: string;
}

export interface StructureVisualizer {
  id: string;
  parentId?: string;
  code: string;
  name: string;
  areaTypeId: string;
  areaName?: string;
  areaType: AreaType;
  showDependencies?: boolean;
  dependencies: Structure[];
}
