import { IntervaloLaboral } from './locality-calendar.model';

/**
 * Calendarios de feriados compartidos (RF-LO-27 a 29). Los contadores de la lista, `suscriptores`
 * y `puedeGestionar` no figuran en §11.3: son la forma acordada por el frontend para UI-09/UI-10.
 */

export type AmbitoCalendario =
  | 'Nacional'
  | 'Subnacional'
  | 'Local'
  | 'Institucional'
  | 'OrganismoInternacional';

export type AlcanceFeriado = 'DiaCompleto' | 'JornadaReducida';

export interface SuscriptorCalendario {
  localidadId: string;
  codigo: string;
  nombreOficial: string;
  paisNombre?: string | null;
  vigenteDesde?: string | null;
  vigenteHasta?: string | null;
}

export interface CalendarioFeriados {
  id: string;
  codigo: string;
  nombre: string;
  descripcion?: string | null;
  paisId?: string | null;
  paisNombre?: string | null;
  ambito: AmbitoCalendario;
  localidadGestoraId?: string | null;
  localidadGestoraNombre?: string | null;
  activo: boolean;
  localidadesSuscritas?: number;
  feriadosAnioActual?: number;
  feriadosAnioSiguiente?: number;
  /** El usuario gestiona el calendario de la localidad gestora (RF-LO-38). */
  puedeGestionar?: boolean;
  ventanaRetroactivaDias?: number;
  suscriptores?: SuscriptorCalendario[];
}

export interface CalendarioFeriadosUpsert {
  codigo?: string;
  nombre?: string;
  descripcion?: string | null;
  paisId?: string | null;
  ambito?: AmbitoCalendario;
  localidadGestoraId?: string | null;
  activo?: boolean;
}

export interface Feriado {
  id: string;
  fecha: string;
  nombre: string;
  alcance: AlcanceFeriado;
  intervalos?: IntervaloLaboral[] | null;
  referenciaNormativa?: string | null;
  notas?: string | null;
}

export interface FeriadoInput {
  fecha: string;
  nombre: string;
  alcance: AlcanceFeriado;
  intervalos: IntervaloLaboral[] | null;
  referenciaNormativa?: string;
  notas?: string;
}

export type ModoImportacion = 'agregar' | 'reemplazarAnio';

export interface ImportacionError {
  linea: number;
  error: string;
}

/** Resultado de `…/dias/importar` (RF-LO-29). */
export interface ImportacionResultado {
  creados: number;
  actualizados: number;
  omitidos: number;
  errores: ImportacionError[];
}
