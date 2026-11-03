import { VersionStatus, VERSION_STATUS_LABELS } from '../models/structure-version.model';
import { LocalityStatusCode } from '../models/institutional-locality.model';

/** Las clases de pill definidas en styles.css (@layer components). */
export type BadgeVariant =
  | 'badge-success'
  | 'badge-warning'
  | 'badge-error'
  | 'badge-info'
  | 'badge-neutral';

/**
 * Mapeo canónico de estado de versión → variante de badge.
 * Flujo: Draft → InReview → Approved → Published (Rejected/Superseded fuera de línea).
 */
export const VERSION_STATUS_BADGE: Record<VersionStatus, BadgeVariant> = {
  Draft: 'badge-neutral',
  InReview: 'badge-warning',
  Approved: 'badge-info',
  Published: 'badge-success',
  Rejected: 'badge-error',
  Superseded: 'badge-neutral',
};

/** Mapeo canónico de estado de localidad → variante de badge (fallback si no hay color del servidor). */
export const LOCALITY_STATUS_BADGE: Record<LocalityStatusCode, BadgeVariant> = {
  ACTIVA: 'badge-success',
  INACTIVA: 'badge-neutral',
  SUSPENDIDA_TEMP: 'badge-warning',
  EN_APERTURA: 'badge-info',
  EN_CIERRE: 'badge-error',
};

export function versionStatusBadge(status: VersionStatus): BadgeVariant {
  return VERSION_STATUS_BADGE[status] ?? 'badge-neutral';
}

export function versionStatusLabel(status: VersionStatus): string {
  return VERSION_STATUS_LABELS[status] ?? status;
}

export function localityStatusBadge(code: LocalityStatusCode): BadgeVariant {
  return LOCALITY_STATUS_BADGE[code] ?? 'badge-neutral';
}
