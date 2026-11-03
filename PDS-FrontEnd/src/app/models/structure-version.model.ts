import { AuditLog } from './audit-log.model';

export type VersionStatus =
  | 'Draft'
  | 'InReview'
  | 'Approved'
  | 'Published'
  | 'Rejected'
  | 'Superseded';

export const VERSION_STATUS_LABELS: Record<VersionStatus, string> = {
  Draft: 'Borrador',
  InReview: 'En revisión',
  Approved: 'Aprobada',
  Published: 'Publicada',
  Rejected: 'Rechazada',
  Superseded: 'Histórica',
};

/**
 * Etiqueta de versión derivada del número (no del nombre libre).
 * Publicada/Histórica → "v.{n}"; en curso (aún sin número) → "v.{próximo} - draft".
 * El número se asigna al publicar, por lo que para una versión en curso se pasa
 * el próximo número previsto (máximo publicado + 1).
 */
export function versionLabel(
  v: Pick<StructureVersionSummary, 'number'>,
  prospectiveNumber?: number,
): string {
  if (v.number != null) return `v.${v.number}`;
  return prospectiveNumber != null ? `v.${prospectiveNumber} - draft` : 'Borrador';
}

export type LegalInstrumentType = 'Decree' | 'Resolution' | 'Law' | 'Other';

export const LEGAL_INSTRUMENT_TYPE_LABELS: Record<LegalInstrumentType, string> = {
  Decree: 'Decreto',
  Resolution: 'Resolución',
  Law: 'Ley',
  Other: 'Otro',
};

export interface LegalInstrument {
  id: string;
  type: LegalInstrumentType;
  number: string;
  date: string;
  issuingAuthority: string;
  documentUrl?: string | null;
}

export interface StructureVersionSummary {
  id: string;
  number?: number | null;
  name: string;
  description?: string | null;
  status: VersionStatus;
  effectiveFrom?: string | null;
  effectiveTo?: string | null;
  isPublic: boolean;
  createdBy: string;
  createdByName?: string | null;
  createdAt: string;
  publishedAt?: string | null;
  publishedByName?: string | null;
  legalInstrument?: LegalInstrument | null;
}

export interface StructureVersionDetail extends StructureVersionSummary {
  reviewedByName?: string | null;
  reviewedAt?: string | null;
  approvedByName?: string | null;
  approvedAt?: string | null;
  auditLogs: AuditLog[];
}

export interface CreateStructureVersion {
  name: string;
  description?: string;
}

export interface CreateLegalInstrument {
  type: LegalInstrumentType;
  number: string;
  date: string;
  issuingAuthority: string;
  documentUrl?: string | null;
}

export interface PublishVersion {
  effectiveFrom: string;
  effectiveTo?: string | null;
  isPublic: boolean;
  legalInstrumentId?: string | null;
  legalInstrument?: CreateLegalInstrument | null;
  authorizedPosts?: Record<string, number>;
}
