/** Catálogos de clasificación y gestión de localidades (RF-LO-01/14/15). */

export interface TipoLocalidad {
  id: string;
  codigo: string;
  nombre: string;
  descripcion?: string | null;
  color?: string | null;
}
export interface TipoLocalidadUpsert {
  codigo?: string;
  nombre?: string;
  descripcion?: string | null;
  color?: string | null;
}

export interface NaturalezaAdministrativa {
  id: string;
  codigo: string;
  nombre: string;
  descripcion?: string | null;
}
export interface NaturalezaAdministrativaUpsert {
  codigo?: string;
  nombre?: string;
  descripcion?: string | null;
}

export interface EstadoLocalidad {
  id: string;
  codigo: string;
  nombre: string;
  descripcion?: string | null;
  color?: string | null;
  esTerminal: boolean;
}
export interface EstadoLocalidadUpsert {
  codigo?: string;
  nombre?: string;
  descripcion?: string | null;
  color?: string | null;
  esTerminal?: boolean;
}

export interface Servicio {
  id: string;
  codigo?: string | null;
  nombre: string;
  descripcion?: string | null;
}
export interface ServicioUpsert {
  codigo?: string | null;
  nombre?: string;
  descripcion?: string | null;
}

export interface Responsable {
  id: string;
  nombre: string;
  cargo?: string | null;
  email?: string | null;
  telefono?: string | null;
  /** `sub` del usuario GobID; solo lo cambian los administradores de calendario (RF-LO-37). */
  gobidUserId?: string | null;
}
export interface ResponsableUpsert {
  nombre?: string;
  cargo?: string | null;
  email?: string | null;
  telefono?: string | null;
  gobidUserId?: string | null;
}

/** Zona horaria IANA del catálogo (RF-LO-21). */
export interface ZonaHoraria {
  id: string;
  comentario?: string | null;
  /** Desplazamiento UTC vigente, p. ej. `-04:00`. */
  desplazamientoActual: string;
  esHorarioVeranoActual?: boolean;
}

export type CategoriaFuncion = 'Diplomatica' | 'Consular' | 'Multilateral';

/** Función de servicio exterior de una localidad (RF-LO-45). */
export interface FuncionLocalidad {
  id: string;
  codigo: string;
  nombre: string;
  categoria: CategoriaFuncion;
  descripcion?: string | null;
}
export interface FuncionLocalidadUpsert {
  codigo?: string;
  nombre?: string;
  categoria?: CategoriaFuncion;
  descripcion?: string | null;
}
