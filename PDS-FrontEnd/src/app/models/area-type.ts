export interface AreaType {
  id: string;
  name: string;
  parentId?: string;
  order?: number;
}

export interface AreaTypeUpsert {
  id?: string;
  name?: string;
  parentId?: string;
  order?: number;
}
