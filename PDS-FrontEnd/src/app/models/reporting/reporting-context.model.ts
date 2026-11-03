import { StructureVersionSummary } from '../structure-version.model';

/**
 * Contexto de reportería: versión publicada (por id) o fecha de corte. Ambos vacíos ⇒ versión vigente.
 * Es el selector compartido por el dashboard y todos los reportes (RF-RE-01..07).
 */
export interface ReportingContext {
  versionId?: string;
  asOfDate?: string; // yyyy-MM-dd
}

export type ReportingContextKind = 'current' | 'version' | 'asOf';

export type ExportFormat = 'pdf' | 'xlsx' | 'csv';

export const EXPORT_FORMAT_LABELS: Record<ExportFormat, string> = {
  pdf: 'PDF',
  xlsx: 'Excel (.xlsx)',
  csv: 'CSV',
};

export const EXPORT_FORMAT_EXT: Record<ExportFormat, string> = {
  pdf: 'pdf',
  xlsx: 'xlsx',
  csv: 'csv',
};

/** Convierte el contexto a parámetros de query (versionId XOR asOfDate). */
export function contextToParams(ctx: ReportingContext): Record<string, string> {
  const params: Record<string, string> = {};
  if (ctx.versionId) params['versionId'] = ctx.versionId;
  else if (ctx.asOfDate) params['asOfDate'] = ctx.asOfDate;
  return params;
}

/** Etiqueta legible del contexto para mostrar en tarjetas y encabezados. */
export function contextLabel(ctx: ReportingContext, versions: StructureVersionSummary[]): string {
  if (ctx.versionId) {
    const v = versions.find((x) => x.id === ctx.versionId);
    if (v) return v.number != null ? `Versión v.${v.number}` : v.name;
    return 'Versión publicada';
  }
  if (ctx.asOfDate) return `Al ${ctx.asOfDate}`;
  return 'Versión vigente';
}

/** Sufijo corto del contexto para nombres de archivo de respaldo. */
export function contextSuffix(ctx: ReportingContext, versions: StructureVersionSummary[]): string {
  if (ctx.versionId) {
    const v = versions.find((x) => x.id === ctx.versionId);
    return v?.number != null ? `v${v.number}` : 'version';
  }
  if (ctx.asOfDate) return `al-${ctx.asOfDate.replace(/-/g, '')}`;
  return 'vigente';
}
