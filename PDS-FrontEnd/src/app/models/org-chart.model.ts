import { StructureVersionSummary } from './structure-version.model';

export interface OrgChartPosition {
  positionId: string;
  name: string;
  isSupervisor: boolean;
  authorizedPosts: number;
  occupationalGroupName?: string | null;
}

/** Nodo plano del organigrama (id/parent lógicos). El visor reconstruye el árbol con d3-hierarchy. */
export interface OrgChartNode {
  sourceUnitId: string;
  parentSourceUnitId?: string | null;
  code: string;
  name: string;
  areaTypeId: string;
  areaTypeName?: string | null;
  level: number;
  positions: OrgChartPosition[];
}

export interface OrgChart {
  version: StructureVersionSummary;
  nodes: OrgChartNode[];
}

export interface OrgChartQuery {
  date?: string | null;
  versionId?: string | null;
  rootUnitId?: string | null;
  maxLevel?: number | null;
  includePositions?: boolean;
  organizationId?: number | null;
}

/** Organización (tenant) resuelta por slug para el organigrama público. */
export interface PublicOrganization {
  organizationId: number;
  name: string;
  acronym?: string | null;
  slug: string;
}
