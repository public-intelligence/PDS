/**
 * Identificadores y metadatos de cada reporte. Única fuente de verdad para títulos, rutas (segmento
 * bajo /reportes) y enlaces de drill-through desde el dashboard.
 */
export type ReportKey =
  | 'authorized-posts'
  | 'staffing-complement'
  | 'estimated-cost'
  | 'comparative'
  | 'structural-changes';

export interface ReportMeta {
  title: string;
  route: string;
  description: string;
}

export const REPORT_META: Record<ReportKey, ReportMeta> = {
  'authorized-posts': {
    title: 'Plazas autorizadas',
    route: 'puestos-autorizados',
    description: 'Plazas autorizadas por unidad, nivel jerárquico o categoría salarial.',
  },
  'staffing-complement': {
    title: 'Dotación definida',
    route: 'dotacion',
    description: 'Cargos distintos y plazas autorizadas por unidad y nivel (definición, no ocupación).',
  },
  'estimated-cost': {
    title: 'Costo estimado',
    route: 'costo-estimado',
    description: 'Costo estimado de la plantilla (salario × plazas autorizadas).',
  },
  comparative: {
    title: 'Comparativo entre períodos',
    route: 'comparativo',
    description: 'Variaciones de unidades, cargos, plazas y costo entre dos contextos.',
  },
  'structural-changes': {
    title: 'Cambios estructurales',
    route: 'cambios-estructurales',
    description: 'Cambios aprobados en un período (trazabilidad del flujo de aprobación).',
  },
};
