import { AuditLog } from '../audit-log.model';
import { LegalInstrument, VersionStatus } from '../structure-version.model';
import { ComparisonSummary, VersionComparison } from '../structure-diff.model';

export type ReportGrouping = 'Unit' | 'HierarchyLevel' | 'SalaryCategory' | 'OccupationalGroup';
export type SalaryBasis = 'Min' | 'Midpoint' | 'Max';

export const REPORT_GROUPING_LABELS: Record<ReportGrouping, string> = {
  Unit: 'Unidad',
  HierarchyLevel: 'Nivel jerárquico',
  SalaryCategory: 'Categoría salarial (grado)',
  OccupationalGroup: 'Grupo ocupacional',
};

export const SALARY_BASIS_LABELS: Record<SalaryBasis, string> = {
  Min: 'Mínimo del rango',
  Midpoint: 'Punto medio del rango',
  Max: 'Máximo del rango',
};

/** Etiqueta del contexto resuelto que devuelve el backend en cada reporte. */
export interface ReportContextLabel {
  versionId: string;
  versionNumber?: number | null;
  versionName: string;
  status: VersionStatus;
  effectiveFrom?: string | null;
  effectiveTo?: string | null;
  resolvedAsOf: string;
  generatedAtUtc: string;
  generatedByName?: string | null;
}

export interface ReportSubtotal {
  groupKey: string;
  groupLabel: string;
  distinctPositionCount: number;
  authorizedPosts: number;
  estimatedCost?: number | null;
}

// ───────── RF-RE-01: plazas autorizadas ─────────
export interface AuthorizedPostsRow {
  groupKey?: string | null;
  unitId?: string | null;
  unitCode?: string | null;
  unitName?: string | null;
  areaTypeName?: string | null;
  level?: number | null;
  positionId?: string | null;
  positionName?: string | null;
  occupationalGroupName?: string | null;
  grade?: number | null;
  authorizedPosts: number;
}

export interface AuthorizedPostsReport {
  context: ReportContextLabel;
  groupBy: ReportGrouping;
  rows: AuthorizedPostsRow[];
  subtotals: ReportSubtotal[];
  grandTotal: ReportSubtotal;
}

// ───────── RF-RE-02: dotación definida ─────────
export interface StaffingComplementRow {
  unitId: string;
  code: string;
  name: string;
  areaTypeName?: string | null;
  level: number;
  parentUnitId?: string | null;
  ownDistinctPositions: number;
  ownAuthorizedPosts: number;
  consolidatedDistinctPositions: number;
  consolidatedAuthorizedPosts: number;
  hasSupervisorDefined: boolean;
}

export interface StaffingComplementReport {
  context: ReportContextLabel;
  units: StaffingComplementRow[];
  byLevel: ReportSubtotal[];
  grandTotal: ReportSubtotal;
}

// ───────── RF-RE-03: costo estimado ─────────
export interface EstimatedCostRow {
  groupKey?: string | null;
  unitId: string;
  unitCode: string;
  unitName: string;
  level: number;
  positionId: string;
  positionName: string;
  occupationalGroupName?: string | null;
  grade: number;
  authorizedPosts: number;
  unitSalary?: number | null;
  lineCost?: number | null;
  salaryResolved: boolean;
}

export interface EstimatedCostReport {
  context: ReportContextLabel;
  basis: SalaryBasis;
  groupBy: ReportGrouping;
  isEstimate: boolean;
  rows: EstimatedCostRow[];
  subtotals: ReportSubtotal[];
  grandTotal: ReportSubtotal;
  unresolvedPositionCount: number;
  historicalFidelityNote: string;
}

// ───────── RF-RE-04: comparativo ─────────
export interface PositionDelta {
  positionId: string;
  positionName: string;
  unitId?: string | null;
  changeType: 'Added' | 'Removed' | 'PostsChanged';
  fromAuthorizedPosts: number;
  toAuthorizedPosts: number;
  authorizedPostsDelta: number;
}

export interface PositionDeltaSummary {
  addedCount: number;
  removedCount: number;
  postCountChangedCount: number;
  fromTotalAuthorizedPosts: number;
  toTotalAuthorizedPosts: number;
  authorizedPostsDelta: number;
  changes: PositionDelta[];
}

export interface CostDelta {
  basis: SalaryBasis;
  fromEstimatedCost?: number | null;
  toEstimatedCost?: number | null;
  costDelta?: number | null;
  isEstimate: boolean;
}

export interface ComparativeReport {
  fromContext: ReportContextLabel;
  toContext: ReportContextLabel;
  unitComparison: VersionComparison;
  positionDeltas: PositionDeltaSummary;
  costDelta: CostDelta;
}

// ───────── RF-RE-05: cambios estructurales ─────────
export interface StructuralChangesFilter {
  from: string;
  to: string;
  changeType?: string | null;
  unitId?: string | null;
  approverId?: string | null;
}

export interface StructuralChangeEntry {
  versionId: string;
  number?: number | null;
  name: string;
  status: VersionStatus;
  effectiveFrom?: string | null;
  legalInstrument?: LegalInstrument | null;
  createdBy?: string | null;
  createdByName?: string | null;
  createdAt: string;
  reviewedByName?: string | null;
  reviewedAt?: string | null;
  approvedByName?: string | null;
  approvedAt?: string | null;
  publishedByName?: string | null;
  publishedAt?: string | null;
  auditLogs: AuditLog[];
  unitChangeSummary?: ComparisonSummary | null;
  positionDeltaSummary?: PositionDeltaSummary | null;
}

export interface StructuralChangesReport {
  from: string;
  to: string;
  filters: StructuralChangesFilter;
  entries: StructuralChangeEntry[];
}
