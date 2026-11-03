import { SelectOption } from '../components/shared/custom-select/custom-select';
import { AlcanceFeriado, AmbitoCalendario } from '../models/holiday-calendar.model';
import { CategoriaFuncion } from '../models/locality-catalog.model';
import {
  CanalCambio,
  CategoriaExcepcion,
  DiaEfectivo,
  OrigenExcepcion,
  TipoExcepcion,
} from '../models/locality-calendar.model';

/** Textos en español de las enumeraciones del calendario (§9.5, §12). */

export const TIPO_EXCEPCION_LABELS: Record<TipoExcepcion, string> = {
  Cierre: 'Cierre',
  JornadaEspecial: 'Jornada especial',
  DiaLaborableAdicional: 'Día laborable adicional',
};

export const TIPO_EXCEPCION_HINTS: Record<TipoExcepcion, string> = {
  Cierre: 'No se trabaja',
  JornadaEspecial: 'Se trabaja con otro horario',
  DiaLaborableAdicional: 'Se trabaja un día normalmente libre',
};

export const CATEGORIA_EXCEPCION_LABELS: Record<CategoriaExcepcion, string> = {
  DecisionAdministrativa: 'Decisión administrativa',
  FeriadoLocal: 'Feriado local',
  DueloOficial: 'Duelo oficial',
  Emergencia: 'Emergencia',
  EventoInstitucional: 'Evento institucional',
  Otro: 'Otro',
  Suspension: 'Suspensión',
  CierreDefinitivo: 'Cierre definitivo',
};

/** Categorías que puede elegir un usuario; las de sistema quedan fuera (RF-LO-32). */
const CATEGORIAS_DE_USUARIO: CategoriaExcepcion[] = [
  'DecisionAdministrativa',
  'FeriadoLocal',
  'DueloOficial',
  'Emergencia',
  'EventoInstitucional',
  'Otro',
];

export const ORIGEN_EXCEPCION_LABELS: Record<OrigenExcepcion, string> = {
  PaisAnfitrion: 'País anfitrión',
  PaisPropio: 'República Dominicana',
  Institucional: 'Institucional',
};

export const CANAL_LABELS: Record<CanalCambio, string> = {
  PdsUi: 'PDS',
  ApiExterna: 'Intranet / API externa',
  Sistema: 'Sistema',
  Migracion: 'Migración',
};

export const AMBITO_LABELS: Record<AmbitoCalendario, string> = {
  Nacional: 'Nacional',
  Subnacional: 'Subnacional',
  Local: 'Local',
  Institucional: 'Institucional',
  OrganismoInternacional: 'Organismo internacional',
};

export const ALCANCE_LABELS: Record<AlcanceFeriado, string> = {
  DiaCompleto: 'Día completo',
  JornadaReducida: 'Jornada reducida',
};

export const CATEGORIA_FUNCION_LABELS: Record<CategoriaFuncion, string> = {
  Diplomatica: 'Diplomática',
  Consular: 'Consular',
  Multilateral: 'Multilateral',
};

function toOptions<T extends string>(labels: Record<T, string>, keys?: T[]): SelectOption[] {
  return (keys ?? (Object.keys(labels) as T[])).map((value) => ({ value, label: labels[value] }));
}

export const CATEGORIA_EXCEPCION_OPTIONS = toOptions(CATEGORIA_EXCEPCION_LABELS, CATEGORIAS_DE_USUARIO);
export const ORIGEN_EXCEPCION_OPTIONS = toOptions(ORIGEN_EXCEPCION_LABELS);
export const AMBITO_OPTIONS = toOptions(AMBITO_LABELS);
export const CATEGORIA_FUNCION_OPTIONS = toOptions(CATEGORIA_FUNCION_LABELS);

/** Estado de un día en la vista anual (UI-05 §5). */
export type DayKind = 'laborable' | 'noLaborable' | 'feriado' | 'excepcion' | 'cicloVida';

export const DAY_KIND_LABELS: Record<DayKind, string> = {
  laborable: 'Laborable',
  noLaborable: 'No laborable (horario)',
  feriado: 'Feriado',
  excepcion: 'Excepción',
  cicloVida: 'Suspensión o cierre',
};

export const DAY_KINDS = Object.keys(DAY_KIND_LABELS) as DayKind[];

export function dayKind(day: Pick<DiaEfectivo, 'fuente' | 'laborable'>): DayKind {
  switch (day.fuente) {
    case 'CicloVida':
      return 'cicloVida';
    case 'Excepcion':
      return 'excepcion';
    case 'Feriado':
      return 'feriado';
    default:
      return day.laborable ? 'laborable' : 'noLaborable';
  }
}
