import { StructureVersionSummary } from './structure-version.model';

export type DiffStatus = 'created' | 'deleted' | 'modified' | 'relocated';

export const DIFF_STATUS_LABELS: Record<DiffStatus, string> = {
  created: 'Creada',
  deleted: 'Eliminada',
  modified: 'Modificada',
  relocated: 'Reubicada',
};

export interface FieldChange {
  field: string;
  from?: string | null;
  to?: string | null;
}

export interface UnitSnapshot {
  sourceUnitId: string;
  code: string;
  name: string;
  areaTypeName?: string | null;
  level: number;
  parentSourceUnitId?: string | null;
}

export interface UnitChange {
  sourceUnitId: string;
  /** Marcas combinables del backend: 'Created' | 'Deleted' | 'Modified' | 'Relocated'. */
  changeType: string[];
  before?: UnitSnapshot | null;
  after?: UnitSnapshot | null;
  fieldChanges: FieldChange[];
  fromParentSourceUnitId?: string | null;
  toParentSourceUnitId?: string | null;
}

export interface ComparisonSummary {
  createdCount: number;
  deletedCount: number;
  modifiedCount: number;
  relocatedCount: number;
}

export interface VersionComparison {
  fromVersion: StructureVersionSummary;
  toVersion: StructureVersionSummary;
  summary: ComparisonSummary;
  unitChanges: UnitChange[];
}
