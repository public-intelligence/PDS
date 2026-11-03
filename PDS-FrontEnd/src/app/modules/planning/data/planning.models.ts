/**
 * MOCKUP DE PLANIFICACIÓN (demo para validación con stakeholders).
 * Modelos del dominio simulado, alineados a los formatos institucionales
 * vigentes de PEI, POA y PACC. Sin backend.
 *
 * Convenciones de la iteración 2 (feedback de revisión):
 * - Responsable = ÁREA del organigrama vigente (no persona).
 * - Los montos viven SOLO en los requerimientos (formato PACC).
 * - Techos presupuestarios por área y por rubro.
 */

// ---------------------------------------------------------------------------
// Personas (puntos de vista de la demo)
// ---------------------------------------------------------------------------

export type PersonaId = 'planificacion' | 'sustantiva' | 'transversal' | 'presupuesto' | 'mae';

export interface Persona {
  id: PersonaId;
  nombre: string;
  cargo: string;
  areaId: string;
  iniciales: string;
}

// ---------------------------------------------------------------------------
// Estructura / catálogos (el "organigrama vigente" de la demo)
// ---------------------------------------------------------------------------

export type TipoArea = 'sustantiva' | 'transversal' | 'rectora';

export interface Area {
  id: string;
  nombre: string;
  corta: string;
  tipo: TipoArea;
  responsable: string;
  /**
   * Unidad organizativa de la que depende en el organigrama vigente.
   * `null` sólo en la raíz (el despacho, nivel 0); las áreas con `padreId`
   * apuntando a la raíz son el nivel 1 y son las que formulan plan propio.
   */
  padreId: string | null;
}

export interface CategoriaTransversal {
  id: string;
  nombre: string;
  /** Área consolidadora: la que recibe los requerimientos de esta categoría. */
  areaDestinoId: string;
  ejemplos: string;
}

export interface ReglaRuteo {
  id: string;
  categoriaId: string;
  /**
   * Ajuste 6: la regla referencia rubros del catálogo maestro por id, no por
   * texto libre. Así el catálogo es la única fuente de la nomenclatura y los
   * rubros disponibles se filtran por categoría y área consolidadora.
   */
  rubroIds: string[];
  areaDestinoId: string;
  version: number;
  activa: boolean;
  autor: string;
  fecha: string;
}

// ---------------------------------------------------------------------------
// Ciclo de planificación
// ---------------------------------------------------------------------------

export type CicloEstado = 'Borrador' | 'Publicado' | 'Cerrado';

export interface HitoCiclo {
  nombre: string;
  fecha: string;
}

export interface Ciclo {
  id: string;
  nombre: string;
  periodo: string;
  estado: CicloEstado;
  usaPeiVigente: boolean;
  areaIds: string[];
  hitos: HitoCiclo[];
  plantilla: string;
  fechaPublicacion?: string;
}

// ---------------------------------------------------------------------------
// PEI — formato oficial (ajuste 1 del feedback de stakeholders):
// Foco → Objetivo → Producto estratégico → Unidad de medida
// (metas anuales + responsable) + áreas involucradas.
// La «Actividad estratégica» se eliminó de la metodología: era un nivel
// intermedio redundante entre el objetivo y el producto.
// ---------------------------------------------------------------------------

/**
 * Metas por año con claves `a{año}` (ej. a2025). Desde M3 el período del PEI es
 * dinámico (PeriodStartYear..PeriodEndYear del backend); el mock sigue usando
 * 2025-2028. Usar `clavesDeMetas()`/`aniosDeMetas()` para iterar.
 */
export type MetasAnuales = Record<`a${number}`, number>;

/** Claves presentes en un mapa de metas, ordenadas por año. */
export function clavesDeMetas(m: MetasAnuales): `a${number}`[] {
  return (Object.keys(m) as `a${number}`[]).sort();
}

/** Construye la clave de metas de un año. */
export function claveDeAnio(anio: number): `a${number}` {
  return `a${anio}`;
}

export interface UnidadMedidaPei {
  id: string;
  nombre: string;
  metasAnuales: MetasAnuales;
  /** Área responsable del logro (organigrama vigente). */
  responsableAreaId: string;
  /**
   * Año a partir del cual la unidad deja de medirse. Nunca se elimina un
   * elemento con historia: se descontinúa, para que los años ya ejecutados
   * sigan siendo consultables y auditables.
   */
  descontinuadoDesde?: number;
  motivoDescontinuacion?: string;
}

export interface ProductoEstrategico {
  id: string;
  nombre: string;
  unidades: UnidadMedidaPei[];
  /** Áreas que apoyan la ejecución del producto (1 o más). */
  areasInvolucradasIds: string[];
  /** Ver `UnidadMedidaPei.descontinuadoDesde`. */
  descontinuadoDesde?: number;
  motivoDescontinuacion?: string;
}

export interface ObjetivoEstrategico {
  id: string;
  codigo: string;
  nombre: string;
  /**
   * Productos estratégicos del objetivo. Antes colgaban de una «Actividad
   * estratégica» intermedia, eliminada de la metodología (ajuste 1).
   */
  productos: ProductoEstrategico[];
}

export interface FocoEstrategico {
  id: string;
  nombre: string;
  /** Alineación con la Estrategia Nacional de Desarrollo 2030. */
  alineacionEnd: string;
  objetivos: ObjetivoEstrategico[];
}

/**
 * Estado de una versión del PEI. Sólo puede haber una `Vigente` y, como
 * máximo, una `En elaboración` (la enmienda en curso).
 */
export type PeiEstadoVersion = 'En elaboración' | 'Vigente' | 'Histórico';

/** Entrada del registro de cambios de una enmienda. */
export interface CambioPei {
  fecha: string;
  autor: string;
  tipo: 'Alta' | 'Edición' | 'Descontinuación';
  nivel: 'Foco' | 'Objetivo' | 'Producto' | 'Unidad de medida';
  descripcion: string;
}

/**
 * Una VERSIÓN del PEI. El plan estratégico no se edita en caliente: cada
 * modificación exige abrir una enmienda justificada, que nace como
 * «En elaboración» y sólo sustituye a la vigente cuando la MAE la aprueba.
 * Las versiones históricas quedan congeladas para siempre.
 */
export interface Pei {
  id: string;
  nombre: string;
  periodo: string;
  version: string;
  estado: PeiEstadoVersion;
  /** Justificación de la enmienda. Obligatoria salvo en la versión original. */
  motivo: string;
  creadaPor: string;
  fechaCreacion: string;
  aprobadoPor: string;
  fechaAprobacion: string;
  focos: FocoEstrategico[];
  /** Qué cambió respecto de la versión anterior. */
  cambios: CambioPei[];
}

// ---------------------------------------------------------------------------
// POA — formato oficial de 13 columnas. Sin columna de montos: los montos
// viven en los requerimientos (formato PACC).
// ---------------------------------------------------------------------------

export type Trimestre = 'T1' | 'T2' | 'T3' | 'T4';
export type TipoMedicion = 'absoluto' | 'porcentaje';

export interface ProgramacionTrimestral {
  T1: number;
  T2: number;
  T3: number;
  T4: number;
}

export interface UnidadMedidaPoa {
  id: string;
  nombre: string;
  metaValor: number;
  metaTipo: TipoMedicion;
  mediosVerificacion: string;
  /** Área responsable (organigrama vigente). */
  responsableAreaId: string;
  /** Desagregación trimestral de la meta anual. */
  programacion: ProgramacionTrimestral;
}

export interface TareaPoa {
  id: string;
  nombre: string;
  /** Meses de ejecución (0 = enero … 11 = diciembre). Vacío cuando es continua. */
  meses: number[];
  /**
   * Ajuste 4: tarea que se ejecuta de forma continua durante todo el año. No
   * tiene meses puntuales ni genera alertas de vencimiento por mes; su
   * cronograma es «permanente».
   */
  continua: boolean;
}

/**
 * Trimestre que corresponde al primer mes programado de una tarea. Una tarea
 * continua no arranca en un trimestre concreto: devuelve null y el trimestre
 * del requerimiento debe elegirse a mano.
 */
export function trimestreDeTarea(tarea: Pick<TareaPoa, 'meses' | 'continua'>): Trimestre | null {
  if (tarea.continua || !tarea.meses.length) return null;
  const primero = Math.min(...tarea.meses);
  return (['T1', 'T2', 'T3', 'T4'] as const)[Math.floor(primero / 3)];
}

/**
 * Origen del producto del POA (ajuste 2 del feedback):
 * - 'pei': responde al plan estratégico y se alinea con foco/objetivo/producto.
 * - 'operativo': actividad operativa propia del área, no vinculada al PEI. No
 *   exige alineación estratégica, pero sí una justificación.
 */
export type OrigenProducto = 'pei' | 'operativo';

export interface ProductoPoa {
  id: string;
  codigo: string;
  origen: OrigenProducto;
  // Alineación con el plan estratégico (cascada de 2 niveles: foco → objetivo).
  // Todos en null cuando el producto es operativo.
  focoId: string | null;
  objetivoId: string | null;
  /** Producto estratégico del PEI; null cuando el producto es operativo. */
  productoEstrategicoId: string | null;
  nombreProducto: string;
  /** Justificación del producto operativo (obligatoria cuando origen = 'operativo'). */
  justificacionOperativa?: string;
  unidades: UnidadMedidaPoa[];
  municipiosImpacto: string[];
  tareas: TareaPoa[];
  areasInvolucradasIds: string[];
  observacion?: string;
  /** Valores de columnas configurables (clave = nombreTecnico de ColumnaConfig). */
  custom: Record<string, string>;
}

export type PlanEstado =
  | 'Sin iniciar'
  | 'Borrador'
  | 'Listo para revisión'
  | 'Observado'
  | 'En ajustes'
  | 'Aprobado'
  | 'En ejecución'
  | 'Cerrado';

export interface Plan {
  id: string;
  areaId: string;
  cicloId: string;
  nombre: string;
  estado: PlanEstado;
  version: number;
  fechaLimite: string;
  rows: ProductoPoa[];
  observacionGeneral?: string;
}

// ---------------------------------------------------------------------------
// Configuración funcional (constructor de columnas)
// ---------------------------------------------------------------------------

export type TipoColumna = 'texto' | 'numero' | 'moneda' | 'seleccion';

export interface ColumnaConfig {
  id: string;
  nombreVisible: string;
  nombreTecnico: string;
  tipo: TipoColumna;
  obligatoria: boolean;
  visible: boolean;
  origen: 'base' | 'personalizada';
  opciones?: string[];
  version: number;
  autor: string;
  fecha: string;
}

// ---------------------------------------------------------------------------
// Requerimientos transversales — formato PACC:
// Cantidad · Unidad de medida · Detalle · Precio estimado · Monto total ·
// Trimestre · Área consolidadora · Área requiriente.
//
// Regla de origen (iteración 3): todo requerimiento nace de una TAREA concreta
// del POA, no del producto, porque el producto es el nivel macro y quien
// consume el apoyo es la tarea. Por eso plan/producto/tarea son obligatorios:
// un producto sin tareas no puede generar requerimientos.
// ---------------------------------------------------------------------------

export type RequerimientoEstado =
  | 'Nuevo'
  | 'En revisión'
  | 'Aprobado'
  | 'Reducido'
  | 'Devuelto'
  | 'Fusionado'
  | 'Reprogramado';

export interface ComentarioReq {
  autor: string;
  area: string;
  fecha: string;
  texto: string;
}

export interface Requerimiento {
  id: string;
  codigo: string;
  /** Área requiriente (origen de la solicitud). */
  origenAreaId: string;
  planId: string;
  /** Producto del POA (nivel macro) al que pertenece la tarea. */
  rowId: string;
  /** Tarea que necesita el apoyo. Obligatoria: sin tarea no hay requerimiento. */
  tareaId: string;
  categoriaId: string;
  rubro: string;
  /** Rubro del catálogo: resuelve la regla de imputación presupuestaria. */
  rubroId?: string;
  /** Artículo del catálogo del que salió el precio de referencia. */
  articuloId?: string;
  /** Detalle del requerimiento. */
  descripcion: string;
  cantidad: number;
  /** Unidad de medida del bien/servicio (galones, unidades, resmas…). */
  unidadMedida: string;
  /**
   * Precio unitario (RD$). Sale del precio de referencia del artículo; el área
   * requiriente no lo teclea. Es 0 en los rubros que no llevan costo.
   */
  precioEstimado: number;
  /**
   * `true` cuando el área consolidadora ya validó el precio. Mientras sea
   * falso, el monto es referencial y así debe leerse en los techos.
   */
  precioConfirmado?: boolean;
  /** Trimestre en que se necesita. */
  trimestre: Trimestre;
  /** Fecha concreta requerida; es lo que sustituye al monto en los apoyos sin costo. */
  fechaRequerida?: string;
  /** Sin costo no hay fuente que asignar: queda vacía. */
  fuenteFinanciamiento: string;
  prioridad: 'Alta' | 'Media' | 'Baja';
  estado: RequerimientoEstado;
  /**
   * Ajuste 14: monto que el área consolidadora priorizó cuando redujo el
   * requerimiento por techo insuficiente. `undefined` = se aprobó completo.
   */
  montoAprobado?: number;
  reglaAplicada: string;
  comentarios: ComentarioReq[];
  /** Id del requerimiento consolidado cuando se fusiona. */
  fusionadoEnId?: string;
}

// ---------------------------------------------------------------------------
// Catálogo jerárquico de rubros y servicios transversales
// Área transversal → Familia de servicio → Rubro → Artículo o servicio
// ---------------------------------------------------------------------------

/**
 * Ajuste 10: quién ejecuta o contrata el gasto. YA NO define de qué techo se
 * descuenta (los techos ahora son por rubro institucional, no por área), sólo
 * el rol operativo:
 * - 'requiriente': el bien queda en el área que lo pidió (equipo, mobiliario).
 * - 'consolidadora': lo ejecuta o contrata el área consolidadora (combustible,
 *   montaje de eventos, licencias corporativas).
 */
export type EjecutorGasto = 'requiriente' | 'consolidadora';

export interface FamiliaServicio {
  id: string;
  nombre: string;
  /** Área transversal que recibe y consolida esta familia. */
  areaReceptoraId: string;
}

export interface RubroCatalogo {
  id: string;
  familiaId: string;
  nombre: string;
  /** Ajuste 9: código presupuestario (antes «clasificador de gasto»). */
  codigoPresupuestario: string;
  /** Ajuste 9: código UNSPSC de Compras y Contrataciones, cuando aplica. */
  codigoUnspsc?: string;
  /** Ajuste 9: código del catálogo de inventario, cuando aplica. */
  codigoInventario?: string;
  unidadMedidaSugerida: string;
  camposRequeridos: string[];
  evidencias: string[];
  /** Días mínimos de anticipación para solicitarlo. */
  anticipacionDias: number;
  fusionable: boolean;
  generaCompra: boolean;
  /**
   * Ajuste 8: `false` deja el rubro fuera del catálogo depurado que ven las
   * áreas (se conserva para el histórico, pero no se puede seleccionar).
   */
  habilitadoInstitucional: boolean;
  /**
   * `false` = el área transversal lo atiende con capacidad instalada (salón
   * propio, flotilla, equipo audiovisual del departamento). Esos apoyos no
   * llevan monto ni fuente y no consumen techo: consumen agenda. Ojo: no es lo
   * mismo que `generaCompra`, porque hay gastos que sí cuestan aunque el área
   * requiriente no sea la que contrata (combustible, refrigerio).
   */
  llevaCosto: boolean;
  ejecutor: EjecutorGasto;
}

export interface ArticuloServicio {
  id: string;
  rubroId: string;
  nombre: string;
  unidadMedida: string;
  precioReferencia: number;
  /** Ajuste 9: el UNSPSC concreto aplica al artículo, no sólo al rubro. */
  codigoUnspsc?: string;
  /** Ajuste 8: artículo disponible para las áreas en el catálogo depurado. */
  habilitadoInstitucional: boolean;
}

// ---------------------------------------------------------------------------
// Techos presupuestarios (ajuste 10: por GRAN RUBRO institucional, no por área)
// ---------------------------------------------------------------------------

export interface AjusteTecho {
  fecha: string;
  motivo: string;
  /** Positivo = incremento, negativo = reducción. */
  monto: number;
}

/**
 * Techo de un gran rubro para toda la institución. El consumo por área deja de
 * ser un techo configurable y pasa a ser un reporte derivado (ver EstadoTecho).
 */
export interface TechoRubro {
  rubroId: string;
  montoInicial: number;
  ajustes: AjusteTecho[];
  fuente: string;
  clasificador: string;
}

/** Estados financieros derivados del techo de un rubro institucional. */
export interface EstadoTecho {
  rubroId: string;
  nombre: string;
  inicial: number;
  ajustes: number;
  vigente: number;
  reservado: number;
  planificado: number;
  comprometido: number;
  /** Parte del monto que sigue siendo precio de referencia sin confirmar. */
  porConfirmar: number;
  ejecutado: number;
  disponible: number;
  saldoProyectado: number;
  fuente: string;
  clasificador: string;
}

// ---------------------------------------------------------------------------
// Seguimiento y ejecución
// ---------------------------------------------------------------------------

export interface AvanceHistorial {
  corte: string;
  avanceFisico: number;
  avanceFinanciero: number;
  comentario: string;
}

export interface RegistroSeguimiento {
  id: string;
  planId: string;
  rowId: string;
  actividad: string;
  areaId: string;
  metaPeriodo: string;
  corte: string;
  avanceFisico: number | null;
  avanceFinanciero: number | null;
  comentario?: string;
  evidencias: string[];
  riesgo?: string;
  estado: 'Pendiente' | 'Reportado';
  historial: AvanceHistorial[];
}

// ---------------------------------------------------------------------------
// Trazabilidad (actividad reciente / historial)
// ---------------------------------------------------------------------------

export interface EventoAuditoria {
  id: string;
  fecha: string;
  persona: string;
  modulo: string;
  accion: string;
  detalle: string;
}

// ---------------------------------------------------------------------------
// Decisiones de priorización (ajuste 14)
// ---------------------------------------------------------------------------

export type TipoDecision = 'Reducción' | 'Devolución' | 'Reprogramación' | 'Priorización';

/**
 * Cuando los requerimientos superan el techo disponible queda registrada la
 * decisión del área consolidadora o de la autoridad: qué se redujo, devolvió,
 * reprogramó o priorizó, con su motivo y su rastro.
 */
export interface DecisionPriorizacion {
  id: string;
  requerimientoId: string;
  codigoRequerimiento: string;
  rubroId?: string;
  tipo: TipoDecision;
  montoSolicitado: number;
  montoAprobado: number;
  motivo: string;
  decidePor: string;
  areaDecideId: string;
  fecha: string;
  nivel: 'consolidadora' | 'autoridad';
}

// ---------------------------------------------------------------------------
// Aprobación institucional (ajuste 15: POA y PACC por separado)
// ---------------------------------------------------------------------------

export type ArtefactoAprobacion = 'POA' | 'PACC';
export type NivelAprobador = 'MAE' | 'Nivel ejecutivo';

export interface SelloAprobacion {
  artefacto: ArtefactoAprobacion;
  usuario: string;
  cargo: string;
  nivel: NivelAprobador;
  fecha: string;
  version: string;
}
