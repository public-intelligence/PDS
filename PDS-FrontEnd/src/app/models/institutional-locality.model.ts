import { AuditLog } from './audit-log.model';
import { CanalCambio } from './locality-calendar.model';

export type LocalityStatusCode =
  | 'ACTIVA'
  | 'INACTIVA'
  | 'SUSPENDIDA_TEMP'
  | 'EN_APERTURA'
  | 'EN_CIERRE';

/** Fila ligera para la lista (RF-LO-15). */
export interface LocalityListItem {
  id: string;
  codigo: string;
  nombreOficial: string;
  tipoLocalidadId: string;
  tipoNombre?: string | null;
  estadoLocalidadId: string;
  estadoNombre?: string | null;
  estadoColor?: string | null;
  provinciaId?: string | null;
  provinciaNombre?: string | null;
  municipioId?: string | null;
  municipioNombre?: string | null;
  latitud?: number | null;
  longitud?: number | null;
  // RF-LO-19
  paisId?: string | null;
  paisCodigo?: string | null;
  paisNombre?: string | null;
  ciudad?: string | null;
  zonaHoraria?: string | null;
  estadoCodigo?: string | null;
  /** Códigos de las funciones de servicio exterior (RF-LO-45). */
  funciones?: string[] | null;
}

/** Filtros de búsqueda + paginación, compartidos por lista y mapa (RF-LO-15/16). */
export interface LocalityListQuery {
  nombre?: string;
  codigo?: string;
  tipoId?: string;
  estadoId?: string;
  naturalezaId?: string;
  regionId?: string;
  provinciaId?: string;
  municipioId?: string;
  unidadId?: string;
  servicioId?: string;
  instrumentoLegalId?: string;
  // RF-LO-19
  paisId?: string;
  paisCodigo?: string;
  estadoCodigo?: string;
  funcionId?: string;
  funcionCodigo?: string;
  pageNumber?: number;
  pageSize?: number;
}

/** Punto del mapa (RF-LO-16). */
export interface MapPoint {
  id: string;
  nombreOficial: string;
  codigo: string;
  latitud: number;
  longitud: number;
  tipoLocalidadId: string;
  tipoNombre?: string | null;
  tipoColor?: string | null;
  estadoLocalidadId: string;
  estadoNombre?: string | null;
  estadoColor?: string | null;
  unidadResponsableId?: string | null;
  unidadResponsableNombre?: string | null;
  direccionCompleta?: string | null;
  paisNombre?: string | null;
  ciudad?: string | null;
  zonaHoraria?: string | null;
}

// ── Sub-elementos (lectura) ──
export interface LocalityServiceLink {
  id: string;
  servicioId: string;
  servicioNombre?: string | null;
  habilitado: boolean;
  fechaHabilitacion?: string | null;
}
export interface LocalityResponsibleLink {
  id: string;
  responsableId: string;
  responsableNombre?: string | null;
  rol?: string | null;
  principal: boolean;
  fechaAsignacion?: string | null;
  /** Puede mantener el calendario laboral de la localidad (RF-LO-38). */
  gestionaCalendario?: boolean;
}
export interface LocalityDocument {
  id: string;
  tipoDocumento: string;
  descripcion?: string | null;
  url: string;
  fechaRegistro: string;
  registradoPor?: string | null;
}
export interface LocalityUnitPresence {
  id: string;
  unidadOrganizativaId: string;
  unidadNombre?: string | null;
  notas?: string | null;
}
export interface LocalityCoverage {
  id: string;
  regionId?: string | null;
  regionNombre?: string | null;
  provinciaId?: string | null;
  provinciaNombre?: string | null;
  municipioId?: string | null;
  municipioNombre?: string | null;
  distritoMunicipalId?: string | null;
  distritoMunicipalNombre?: string | null;
}
export interface LocalityLegalInstrumentLink {
  id: string;
  instrumentoLegalId: string;
  proposito?: string | null;
  numero?: string | null;
  tipo?: string | null;
  fecha?: string | null;
  documentUrl?: string | null;
}

/** Detalle consolidado (RF-LO-14). */
export interface LocalityDetail {
  id: string;
  codigo: string;
  nombreOficial: string;
  nombreCorto?: string | null;
  sigla?: string | null;
  tipoLocalidadId: string;
  tipoNombre?: string | null;
  naturalezaAdministrativaId: string;
  naturalezaNombre?: string | null;
  estadoLocalidadId: string;
  estadoNombre?: string | null;
  estadoColor?: string | null;
  unidadResponsableId?: string | null;
  unidadResponsableNombre?: string | null;
  unidadVinculadaId?: string | null;
  unidadVinculadaNombre?: string | null;
  localidadPadreId?: string | null;
  localidadPadreNombre?: string | null;
  paisId: string;
  paisCodigo?: string | null;
  paisNombre?: string | null;
  regionId?: string | null;
  regionNombre?: string | null;
  provinciaId?: string | null;
  provinciaNombre?: string | null;
  municipioId?: string | null;
  municipioNombre?: string | null;
  distritoMunicipalId?: string | null;
  distritoMunicipalNombre?: string | null;
  /** Solo localidades en el exterior (RF-LO-18). */
  ciudad?: string | null;
  subdivisionAdministrativa?: string | null;
  sectorBarrioParaje?: string | null;
  direccionCompleta: string;
  edificioPisoLocalReferenciaInterna?: string | null;
  codigoPostal?: string | null;
  /** Identificador IANA, p. ej. `America/Santo_Domingo` (RF-LO-20). */
  zonaHoraria?: string | null;
  latitud?: number | null;
  longitud?: number | null;
  notasAcceso?: string | null;
  horarioRegular?: string | null;
  observaciones?: string | null;
  fechaApertura?: string | null;
  suspensionMotivo?: string | null;
  suspensionFechaInicio?: string | null;
  suspensionFechaFin?: string | null;
  localidadAlternaId?: string | null;
  localidadAlternaNombre?: string | null;
  cierreFecha?: string | null;
  cierreMotivo?: string | null;
  localidadSucesoraId?: string | null;
  localidadSucesoraNombre?: string | null;
  serviciosAfectados?: string | null;
  justificacionDuplicado?: string | null;
  createdAt: string;
  updatedAt: string;
  servicios: LocalityServiceLink[];
  responsables: LocalityResponsibleLink[];
  documentos: LocalityDocument[];
  unidadesPresencia: LocalityUnitPresence[];
  coberturas: LocalityCoverage[];
  instrumentosLegales: LocalityLegalInstrumentLink[];
  historial: LocalityAudit[];
  /** Funciones de servicio exterior (RF-LO-45). */
  funciones?: LocalityFunction[] | null;
  /** Plantilla de horario y calendario de respaldo de la organización (RF-LO-36). */
  esLocalidadCalendarioPredeterminada?: boolean;
  calendarioVersion?: number;
}

export interface LocalityFunction {
  id: string;
  codigo: string;
  nombre: string;
}

/** Fila de bitácora (RF-LO-14). */
export interface LocalityAudit {
  id: string;
  accion: AuditLog['action'];
  motivo?: string | null;
  usuario?: string | null;
  fecha: string;
  instrumentoLegalId?: string | null;
  detalle?: string | null;
  /** Origen del cambio (RF-LO-44). */
  canal?: CanalCambio | null;
}

// ── Sub-elementos (entrada anidada) ──
export interface LocalityServiceInput {
  servicioId: string;
  habilitado?: boolean;
  fechaHabilitacion?: string | null;
}
export interface LocalityResponsibleInput {
  responsableId: string;
  rol?: string | null;
  principal?: boolean;
  fechaAsignacion?: string | null;
  gestionaCalendario?: boolean;
}
export interface LocalityDocumentInput {
  tipoDocumento: string;
  descripcion?: string | null;
  url: string;
}
export interface LocalityUnitPresenceInput {
  unidadOrganizativaId: string;
  notas?: string | null;
}
export interface LocalityCoverageInput {
  regionId?: string | null;
  provinciaId?: string | null;
  municipioId?: string | null;
  distritoMunicipalId?: string | null;
}
export interface LocalityLegalInstrumentInput {
  instrumentoLegalId: string;
  proposito?: string | null;
}

/** Cuerpo de crear/editar (RF-LO-01/06/13). */
export interface LocalityUpsert {
  codigo?: string;
  nombreOficial?: string;
  nombreCorto?: string | null;
  sigla?: string | null;
  tipoLocalidadId?: string;
  naturalezaAdministrativaId?: string;
  estadoLocalidadId?: string | null;
  unidadResponsableId?: string | null;
  unidadVinculadaId?: string | null;
  localidadPadreId?: string | null;
  paisId?: string;
  regionId?: string | null;
  provinciaId?: string | null;
  municipioId?: string | null;
  distritoMunicipalId?: string | null;
  /** Requerida en el exterior, donde no aplica la cascada dominicana (RF-LO-18). */
  ciudad?: string | null;
  subdivisionAdministrativa?: string | null;
  sectorBarrioParaje?: string | null;
  direccionCompleta?: string;
  edificioPisoLocalReferenciaInterna?: string | null;
  codigoPostal?: string | null;
  /** Requerida por el API; cambiarla exige `localities:calendar:manage` (RF-LO-20). */
  zonaHoraria?: string;
  latitud?: number | null;
  longitud?: number | null;
  notasAcceso?: string | null;
  horarioRegular?: string | null;
  observaciones?: string | null;
  fechaApertura?: string | null;
  /** RF-LO-45. */
  funcionIds?: string[];
  // RF-LO-06 (solo edición)
  motivo?: string;
  instrumentoLegalId?: string | null;
  // RF-LO-13
  override?: boolean;
  justificacionDuplicado?: string | null;
  // Colecciones hijas
  servicios?: LocalityServiceInput[];
  responsables?: LocalityResponsibleInput[];
  documentos?: LocalityDocumentInput[];
  unidadesPresencia?: LocalityUnitPresenceInput[];
  coberturas?: LocalityCoverageInput[];
  instrumentosLegales?: LocalityLegalInstrumentInput[];
}

export interface SuspendLocalityBody {
  motivo: string;
  fechaInicio?: string | null;
  fechaFin?: string | null;
  instrumentoLegalId?: string | null;
  localidadAlternaId?: string | null;
}
export interface ReactivateLocalityBody {
  motivo?: string | null;
}
export interface CloseLocalityBody {
  motivo: string;
  cierreFecha?: string | null;
  instrumentoLegalId?: string | null;
  localidadSucesoraId?: string | null;
  serviciosAfectados?: string | null;
}

export interface DuplicateWarning {
  campo: string;
  localidadId: string;
  codigo: string;
  nombreOficial: string;
  motivo: string;
}
export interface DuplicateCheckResult {
  hasWarnings: boolean;
  warnings: DuplicateWarning[];
}
export interface DuplicateCheckQuery {
  codigo?: string;
  nombreOficial?: string;
  direccionCompleta?: string;
  latitud?: number | null;
  longitud?: number | null;
  provinciaId?: string | null;
  municipioId?: string | null;
  /** Regla `paisCiudadTipo` de las localidades en el exterior (RF-LO-18). */
  paisId?: string | null;
  ciudad?: string | null;
  tipoLocalidadId?: string | null;
  excludeId?: string | null;
}
