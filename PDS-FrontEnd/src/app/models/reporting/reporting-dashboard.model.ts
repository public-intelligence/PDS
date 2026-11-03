import { ReportKey } from './report-key.model';
import { SalaryBasis } from './reporting-reports.model';

export interface LevelCount {
  level: number;
  label: string;
  count: number;
  authorizedPosts: number;
}

export interface CategoryCount {
  key: string;
  label: string;
  distinctPositions: number;
  authorizedPosts: number;
}

export interface StructuralChangeSummary {
  createdUnits: number;
  modifiedUnits: number;
  relocatedUnits: number;
  deletedUnits: number;
  fromVersionId?: string | null;
  toVersionId?: string | null;
}

export interface QualityAlert {
  code: string;
  message: string;
  unitId?: string | null;
  positionId?: string | null;
  count: number;
}

/** RF-RE-07: payload de KPIs definicionales que devuelve GET /dashboard. */
export interface ReportingDashboard {
  context: {
    versionId: string;
    versionNumber?: number | null;
    versionName: string;
    status: string;
    effectiveFrom?: string | null;
    effectiveTo?: string | null;
    resolvedAsOf: string;
    generatedAtUtc: string;
    generatedByName?: string | null;
  };
  activeUnitsByLevel: LevelCount[];
  totalActiveUnits: number;
  totalDefinedPositions: number;
  totalAuthorizedPosts: number;
  authorizedPostsByLevel: LevelCount[];
  authorizedPostsBySalaryCategory: CategoryCount[];
  authorizedPostsByOccupationalGroup: CategoryCount[];
  estimatedTotalCost?: number | null;
  costIsEstimate: boolean;
  costBasis: SalaryBasis;
  pendingApprovalCount: number;
  recentStructuralChanges: StructuralChangeSummary;
  qualityAlerts: QualityAlert[];
}

/** Mapea el código de alerta de calidad al reporte donde se investiga (drill-through). */
export const QUALITY_ALERT_REPORT: Record<string, ReportKey> = {
  UnitNoAuthorizedPosts: 'authorized-posts',
  UnitMissingSupervisor: 'staffing-complement',
  PositionZeroAuthorizedPosts: 'authorized-posts',
  PositionNoSalaryCategory: 'estimated-cost',
};
