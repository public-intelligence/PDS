/**
 * Calendario laboral de una localidad (SPEC-LOC-CAL-01 §11). La especificación no fija la forma de
 * `GET …/calendario` ni de `…/calendario/estado`: `CalendarioLocalidad` y `EstadoCalendario` son la
 * forma acordada por el frontend y deben confirmarse con el backend (brechas 1 y 2).
 */

/** Intervalo laboral `[inicio, fin)` en hora local (`HH:mm`); `fin` admite `24:00` (RF-LO-24). */
export interface IntervaloLaboral {
  inicio: string;
  fin: string;
}

export type DiaSemana = 'mon' | 'tue' | 'wed' | 'thu' | 'fri' | 'sat' | 'sun';
export const DIAS_SEMANA: readonly DiaSemana[] = ['mon', 'tue', 'wed', 'thu', 'fri', 'sat', 'sun'];

export type JornadaSemanal = Record<DiaSemana, IntervaloLaboral[]>;

/** Versión del horario semanal (RF-LO-25). */
export interface JornadaVersion {
  id: string;
  vigenteDesde: string;
  dias: JornadaSemanal;
  motivo?: string | null;
  creadoPor?: string | null;
}
export interface JornadaInput {
  vigenteDesde: string;
  motivo?: string;
  dias: JornadaSemanal;
}

/** Suscripción a un calendario de feriados (RF-LO-30). */
export interface SuscripcionCalendario {
  calendarioFeriadosId: string;
  codigo: string;
  nombre: string;
  vigenteDesde?: string | null;
  vigenteHasta?: string | null;
}
export interface SuscripcionInput {
  calendarioFeriadosId: string;
  vigenteDesde?: string | null;
  vigenteHasta?: string | null;
}

export type TipoExcepcion = 'Cierre' | 'JornadaEspecial' | 'DiaLaborableAdicional';
export type CategoriaExcepcion =
  | 'DecisionAdministrativa'
  | 'FeriadoLocal'
  | 'DueloOficial'
  | 'Emergencia'
  | 'EventoInstitucional'
  | 'Otro'
  // Solo las genera el sistema (RF-LO-32).
  | 'Suspension'
  | 'CierreDefinitivo';
export type OrigenExcepcion = 'PaisAnfitrion' | 'PaisPropio' | 'Institucional';
export type CanalCambio = 'PdsUi' | 'ApiExterna' | 'Sistema' | 'Migracion';

/** Día no laborable o excepción de la localidad (RF-LO-31/32). */
export interface ExcepcionCalendario {
  id: string;
  fechaInicio: string;
  /** Nula en las excepciones de sistema sin fin (suspensión abierta, cierre). */
  fechaFin?: string | null;
  tipo: TipoExcepcion;
  categoria: CategoriaExcepcion;
  origen?: OrigenExcepcion | null;
  motivo: string;
  referenciaNormativa?: string | null;
  instrumentoLegalId?: string | null;
  intervalos?: IntervaloLaboral[] | null;
  esSistema: boolean;
  registradoPor?: string | null;
  canal?: CanalCambio | null;
}
export interface ExcepcionInput {
  fechaInicio: string;
  fechaFin: string;
  tipo: TipoExcepcion;
  categoria: CategoriaExcepcion;
  origen: OrigenExcepcion;
  motivo: string;
  referenciaNormativa?: string;
  instrumentoLegalId?: string;
  intervalos: IntervaloLaboral[] | null;
}

/** Definición del calendario: `GET /localidades/{id}/calendario`. */
export interface CalendarioLocalidad {
  localidadId: string;
  zonaHoraria: string;
  calendarioVersion: number;
  /** El usuario es gestor de calendario de la localidad (RF-LO-38). */
  puedeGestionar?: boolean;
  /** `Calendario:VentanaRetroactivaDias` (RF-LO-33). */
  ventanaRetroactivaDias?: number;
  jornadas: JornadaVersion[];
  suscripciones: SuscripcionCalendario[];
}

export type FuenteDia = 'Jornada' | 'Feriado' | 'Excepcion' | 'CicloVida';

export interface MotivoDia {
  tipo: string;
  nombre: string;
  calendarioFeriadosId?: string;
  excepcionId?: string;
}

/** Día resuelto del calendario efectivo (RF-LO-34, §11.3.5). */
export interface DiaEfectivo {
  fecha: string;
  diaSemana: DiaSemana;
  laborable: boolean;
  intervalos: IntervaloLaboral[];
  fuente: FuenteDia;
  motivos?: MotivoDia[];
}
export interface CalendarioEfectivo {
  localidadId: string;
  zonaHoraria: string;
  calendarioVersion: number;
  desde: string;
  hasta: string;
  dias: DiaEfectivo[];
}

/** `GET …/calendario/estado`: si la localidad atiende en el instante consultado. */
export interface EstadoCalendario {
  laborable: boolean;
  intervaloActual?: IntervaloLaboral | null;
  proximoInstanteLaborable?: string | null;
}

/** Zona horaria vigente de la localidad (RF-LO-22, §11.3.1). */
export interface ZonaHorariaActual {
  localidadId: string;
  zonaHoraria: string;
  instante: string;
  fechaHoraLocal: string;
  fechaLocal: string;
  desplazamientoUtc: string;
  desplazamientoMinutos: number;
  esHorarioVerano: boolean;
  abreviatura: string;
  proximaTransicion?: {
    instante: string;
    desplazamientoUtc: string;
    esHorarioVerano: boolean;
  } | null;
}

/** Localidad cuyo calendario puede mantener el usuario (RF-LO-40, §11.3.8). */
export interface LocalidadGestionable {
  id: string;
  codigo: string;
  nombreOficial: string;
  pais?: string | null;
  ciudad?: string | null;
  zonaHoraria?: string | null;
  rol: 'Admin' | 'Gestor';
}
