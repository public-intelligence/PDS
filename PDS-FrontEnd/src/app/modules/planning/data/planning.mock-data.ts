/**
 * Datos simulados (demo) del módulo de Planificación — iteración 3
 * (ajustes de stakeholders). PEI con estructura oficial de 4 niveles
 * (Foco → Objetivo → Producto → Unidades de medida), POA sin montos,
 * requerimientos en formato PACC y techos por gran rubro institucional.
 * Todo es estático y se restaura con "Reiniciar demo".
 */

import {
  Area,
  ArticuloServicio,
  CategoriaTransversal,
  Ciclo,
  ColumnaConfig,
  DecisionPriorizacion,
  EjecutorGasto,
  EventoAuditoria,
  FamiliaServicio,
  Pei,
  Persona,
  Plan,
  RegistroSeguimiento,
  ReglaRuteo,
  Requerimiento,
  RubroCatalogo,
  TechoRubro,
} from './planning.models';

// ---------------------------------------------------------------------------
// Personas de la demo
// ---------------------------------------------------------------------------

export const PERSONAS: Persona[] = [
  {
    id: 'planificacion',
    nombre: 'María Fernández',
    cargo: 'Directora de Planificación y Desarrollo',
    areaId: 'dir-planificacion',
    iniciales: 'MF',
  },
  {
    id: 'sustantiva',
    nombre: 'José Herrera',
    cargo: 'Director de Tramitación y Registro',
    areaId: 'dir-tramitacion',
    iniciales: 'JH',
  },
  {
    id: 'transversal',
    nombre: 'Carmen Rosario',
    cargo: 'Directora Administrativa y Financiera',
    areaId: 'dir-admin',
    iniciales: 'CR',
  },
  {
    id: 'presupuesto',
    nombre: 'Luis Peña',
    cargo: 'Encargado de la División de Presupuesto',
    areaId: 'dir-admin',
    iniciales: 'LP',
  },
  {
    id: 'mae',
    nombre: 'Dra. Altagracia Medina',
    cargo: 'Directora General (MAE)',
    areaId: 'despacho',
    iniciales: 'AM',
  },
];

// ---------------------------------------------------------------------------
// Áreas (organigrama vigente de la demo)
// ---------------------------------------------------------------------------

/**
 * Organigrama vigente en dos niveles bajo el despacho (nivel 0): las
 * direcciones dependen del despacho (nivel 1) y los departamentos de apoyo
 * cuelgan de su dirección (nivel 2). El nivel 1 es el que formula plan propio;
 * los departamentos consolidan dentro del plan de su dirección.
 */
export const AREAS: Area[] = [
  {
    id: 'despacho',
    nombre: 'Despacho de la Dirección General',
    corta: 'Despacho',
    tipo: 'rectora',
    responsable: 'Dra. Altagracia Medina',
    padreId: null,
  },
  {
    id: 'dir-planificacion',
    nombre: 'Dirección de Planificación y Desarrollo',
    corta: 'Planificación',
    tipo: 'rectora',
    responsable: 'María Fernández',
    padreId: 'despacho',
  },
  {
    id: 'dir-tramitacion',
    nombre: 'Dirección de Tramitación y Registro',
    corta: 'Tramitación',
    tipo: 'sustantiva',
    responsable: 'José Herrera',
    padreId: 'despacho',
  },
  {
    id: 'dir-juridica',
    nombre: 'Dirección Jurídica',
    corta: 'Jurídica',
    tipo: 'sustantiva',
    responsable: 'Rafael Guzmán',
    padreId: 'despacho',
  },
  {
    id: 'dir-fiscalizacion',
    nombre: 'Dirección de Fiscalización Territorial',
    corta: 'Fiscalización',
    tipo: 'sustantiva',
    responsable: 'Ana Delgado',
    padreId: 'despacho',
  },
  {
    id: 'dir-rrhh',
    nombre: 'Dirección de Recursos Humanos',
    corta: 'RR. HH.',
    tipo: 'sustantiva',
    responsable: 'Pedro Sánchez',
    padreId: 'despacho',
  },
  {
    id: 'dir-admin',
    nombre: 'Dirección Administrativa y Financiera',
    corta: 'Administrativa',
    tipo: 'transversal',
    responsable: 'Carmen Rosario',
    padreId: 'despacho',
  },
  {
    id: 'dpto-protocolo',
    nombre: 'Departamento de Protocolo y Eventos',
    corta: 'Protocolo',
    tipo: 'transversal',
    responsable: 'Laura Jiménez',
    padreId: 'dir-admin',
  },
  {
    id: 'dpto-tic',
    nombre: 'Departamento de Tecnologías de la Información',
    corta: 'TIC',
    tipo: 'transversal',
    responsable: 'Miguel Torres',
    padreId: 'dir-planificacion',
  },
  {
    id: 'dpto-compras',
    nombre: 'Departamento de Compras y Contrataciones',
    corta: 'Compras',
    tipo: 'transversal',
    responsable: 'Yudelka Castillo',
    padreId: 'dir-admin',
  },
  {
    // Ajuste 7: sexta área consolidadora (publicidad, difusión, impresos).
    id: 'dpto-comunicaciones',
    nombre: 'Departamento de Comunicaciones',
    corta: 'Comunicaciones',
    tipo: 'transversal',
    responsable: 'Patria Reynoso',
    padreId: 'despacho',
  },
];

// ---------------------------------------------------------------------------
// Categorías transversales y reglas de ruteo
// ---------------------------------------------------------------------------

/**
 * Ajuste 5: cada categoría de apoyo dice claramente qué área consolida
 * (`areaDestinoId`), para que el área usuaria elija la necesidad sin saber de
 * antemano qué dependencia la atiende. Ajuste 7: 6 áreas consolidadoras.
 */
export const CATEGORIAS: CategoriaTransversal[] = [
  {
    id: 'transporte',
    nombre: 'Transporte y combustible',
    areaDestinoId: 'dir-admin',
    ejemplos: 'Vehículos, combustible, mantenimiento vehicular, transporte de personal',
  },
  {
    id: 'viaticos',
    nombre: 'Viáticos y gastos de viaje',
    areaDestinoId: 'dir-admin',
    ejemplos: 'Viáticos nacionales, alojamiento, dietas de jornadas territoriales',
  },
  {
    id: 'mobiliario-equipos',
    nombre: 'Mobiliario, planta física y suministros',
    areaDestinoId: 'dir-admin',
    ejemplos: 'Escritorios, sillas, climatización, papelería y suministros',
  },
  {
    id: 'eventos-audiovisual',
    nombre: 'Eventos y soporte audiovisual',
    areaDestinoId: 'dpto-protocolo',
    ejemplos: 'Montaje de actos, sonido, proyección, refrigerios',
  },
  {
    id: 'soporte-tic',
    nombre: 'Tecnología y equipamiento TIC',
    areaDestinoId: 'dpto-tic',
    ejemplos: 'Computadoras, licencias, redes, UPS y baterías de respaldo',
  },
  {
    id: 'capacitacion',
    nombre: 'Capacitación y formación',
    areaDestinoId: 'dir-rrhh',
    ejemplos: 'Cursos, talleres, certificaciones, formación por competencias',
  },
  {
    id: 'comunicaciones',
    nombre: 'Publicidad, impresos y difusión',
    areaDestinoId: 'dpto-comunicaciones',
    ejemplos: 'Campañas, publicidad, brochures, publicaciones institucionales',
  },
  {
    id: 'compras-bienes',
    nombre: 'Compras, contrataciones y asistencia técnica',
    areaDestinoId: 'dpto-compras',
    ejemplos: 'Bienes programables, servicios contratables, consultorías',
  },
];

/**
 * Ajuste 6: las reglas referencian rubros del catálogo por id (no texto libre).
 * Los rubros disponibles al crear un requerimiento se filtran por la categoría
 * elegida y su área consolidadora.
 */
export const REGLAS_RUTEO: ReglaRuteo[] = [
  {
    id: 'rr-1',
    categoriaId: 'transporte',
    rubroIds: ['rub-vehiculos', 'rub-combustible', 'rub-transporte-personal', 'rub-mantenimiento-veh'],
    areaDestinoId: 'dir-admin',
    version: 3,
    activa: true,
    autor: 'María Fernández',
    fecha: '2026-05-14',
  },
  {
    id: 'rr-2',
    categoriaId: 'viaticos',
    rubroIds: ['rub-viaticos'],
    areaDestinoId: 'dir-admin',
    version: 1,
    activa: true,
    autor: 'María Fernández',
    fecha: '2026-05-14',
  },
  {
    id: 'rr-3',
    categoriaId: 'mobiliario-equipos',
    rubroIds: ['rub-mobiliario', 'rub-climatizacion', 'rub-papeleria'],
    areaDestinoId: 'dir-admin',
    version: 2,
    activa: true,
    autor: 'María Fernández',
    fecha: '2026-05-14',
  },
  {
    id: 'rr-4',
    categoriaId: 'eventos-audiovisual',
    rubroIds: ['rub-montaje', 'rub-audiovisual', 'rub-refrigerio'],
    areaDestinoId: 'dpto-protocolo',
    version: 1,
    activa: true,
    autor: 'María Fernández',
    fecha: '2026-05-20',
  },
  {
    id: 'rr-5',
    categoriaId: 'soporte-tic',
    rubroIds: ['rub-computo', 'rub-licencias', 'rub-conectividad', 'rub-equipamiento-tec'],
    areaDestinoId: 'dpto-tic',
    version: 4,
    activa: true,
    autor: 'Miguel Torres',
    fecha: '2026-06-02',
  },
  {
    id: 'rr-6',
    categoriaId: 'capacitacion',
    rubroIds: ['rub-capacitacion'],
    areaDestinoId: 'dir-rrhh',
    version: 1,
    activa: true,
    autor: 'Pedro Sánchez',
    fecha: '2026-06-05',
  },
  {
    id: 'rr-7',
    categoriaId: 'comunicaciones',
    rubroIds: ['rub-publicidad', 'rub-impresos'],
    areaDestinoId: 'dpto-comunicaciones',
    version: 1,
    activa: true,
    autor: 'Patria Reynoso',
    fecha: '2026-06-08',
  },
  {
    id: 'rr-8',
    categoriaId: 'compras-bienes',
    rubroIds: ['rub-bienes', 'rub-servicios', 'rub-asistencia'],
    areaDestinoId: 'dpto-compras',
    version: 1,
    activa: true,
    autor: 'Yudelka Castillo',
    fecha: '2026-06-10',
  },
];

// ---------------------------------------------------------------------------
// Ciclos
// ---------------------------------------------------------------------------

export const CICLOS: Ciclo[] = [
  {
    id: 'ciclo-2026',
    nombre: 'POA 2026',
    periodo: '2026',
    estado: 'Publicado',
    usaPeiVigente: true,
    areaIds: AREAS.filter((a) => a.id !== 'despacho').map((a) => a.id),
    hitos: [
      { nombre: 'Formulación', fecha: '2025-08-15' },
      { nombre: 'Consolidación', fecha: '2025-10-01' },
      { nombre: 'Ajustes y priorización', fecha: '2025-10-20' },
      { nombre: 'Aprobación directiva', fecha: '2025-11-15' },
      { nombre: 'Inicio de ejecución', fecha: '2026-01-05' },
    ],
    plantilla: 'Plantilla POA institucional v3',
    fechaPublicacion: '2025-08-01',
  },
  {
    id: 'ciclo-2027',
    nombre: 'POA 2027',
    periodo: '2027',
    estado: 'Publicado',
    usaPeiVigente: true,
    areaIds: AREAS.filter((a) => a.id !== 'despacho').map((a) => a.id),
    hitos: [
      { nombre: 'Formulación por áreas', fecha: '2026-08-14' },
      { nombre: 'Revisión de planificación', fecha: '2026-09-11' },
      { nombre: 'Consolidación institucional', fecha: '2026-10-02' },
      { nombre: 'Validación presupuestaria', fecha: '2026-10-23' },
      { nombre: 'Ajustes y priorización por las áreas', fecha: '2026-11-06' },
      { nombre: 'Aprobación directiva', fecha: '2026-11-20' },
    ],
    plantilla: 'Plantilla POA institucional v4',
    fechaPublicacion: '2026-06-30',
  },
];

/** Ciclo activo de formulación en la demo. */
export const CICLO_ACTIVO_ID = 'ciclo-2027';

// ---------------------------------------------------------------------------
// PEI 2025-2028 — Foco → Objetivo → Actividad → Producto → Unidades
// ---------------------------------------------------------------------------

/**
 * Año de referencia para la inmutabilidad del PEI: las metas de años ya
 * transcurridos (<= este año) no se pueden editar ni eliminar en una enmienda.
 */
export const ANIO_ACTUAL = new Date().getFullYear();

/** Versión vigente del PEI (la que consume el POA para alinear). */
export const PEI: Pei = {
  id: 'pei-v2',
  nombre: 'Plan Estratégico Institucional 2025-2028',
  periodo: '2025-2028',
  version: 'v2.0',
  estado: 'Vigente',
  motivo:
    'Incorporación del foco de transparencia y ajuste de metas 2027-2028 tras la evaluación anual 2025.',
  creadaPor: 'María Fernández',
  fechaCreacion: '2026-01-20',
  aprobadoPor: 'Dra. Altagracia Medina (MAE)',
  fechaAprobacion: '2026-02-14',
  cambios: [
    {
      fecha: '2026-01-22',
      autor: 'María Fernández',
      tipo: 'Alta',
      nivel: 'Foco',
      descripcion: 'Se incorpora el foco «Transparencia, integridad y rendición de cuentas».',
    },
    {
      fecha: '2026-01-28',
      autor: 'María Fernández',
      tipo: 'Edición',
      nivel: 'Unidad de medida',
      descripcion: 'Meta 2027 de «Servicios digitalizados en producción» elevada de 9 a 12.',
    },
    {
      fecha: '2026-02-03',
      autor: 'Miguel Torres',
      tipo: 'Alta',
      nivel: 'Producto',
      descripcion: 'Se agrega «Procesos rediseñados y automatizados» al objetivo OE 2.2.',
    },
    {
      fecha: '2026-07-21',
      autor: 'María Fernández',
      tipo: 'Edición',
      nivel: 'Objetivo',
      descripcion:
        'Ajuste metodológico: se elimina el nivel «Actividad estratégica». Los productos ahora cuelgan directamente del objetivo (foco → objetivo → producto → unidad de medida).',
    },
  ],
  focos: [
    {
      id: 'foco-1',
      nombre: 'Servicios institucionales oportunos y de calidad',
      alineacionEnd: 'END 2030 — Eje 1: Estado social democrático de derecho',
      objetivos: [
        {
          id: 'obj-1-1',
          codigo: 'OE 1.1',
          nombre: 'Reducir los tiempos de tramitación de los servicios misionales',
          productos: [
            {
              id: 'prod-1-1-1-1',
              nombre: 'Expedientes tramitados dentro del plazo normativo',
              unidades: [
                {
                  id: 'um-1',
                  nombre: 'Porcentaje de expedientes en plazo',
                  metasAnuales: { a2025: 80, a2026: 85, a2027: 90, a2028: 95 },
                  responsableAreaId: 'dir-tramitacion',
                },
              ],
              areasInvolucradasIds: ['dpto-tic', 'dir-juridica'],
            },
            {
              id: 'prod-1-1-1-2',
              nombre: 'Servicios digitalizados de punta a punta',
              unidades: [
                {
                  id: 'um-2',
                  nombre: 'Servicios digitalizados en producción',
                  metasAnuales: { a2025: 3, a2026: 6, a2027: 12, a2028: 16 },
                  responsableAreaId: 'dir-tramitacion',
                },
              ],
              areasInvolucradasIds: ['dpto-tic'],
            },
            {
              id: 'prod-1-1-2-1',
              nombre: 'Trámites estandarizados y simplificados',
              unidades: [
                {
                  id: 'um-3',
                  nombre: 'Trámites estandarizados',
                  metasAnuales: { a2025: 5, a2026: 10, a2027: 15, a2028: 20 },
                  responsableAreaId: 'dir-tramitacion',
                },
              ],
              areasInvolucradasIds: ['dir-juridica', 'dir-planificacion'],
            },
          ],
        },
        {
          id: 'obj-1-2',
          codigo: 'OE 1.2',
          nombre: 'Fortalecer la fiscalización territorial preventiva',
          productos: [
            {
              id: 'prod-1-2-1-1',
              nombre: 'Municipios con inspección anual realizada',
              unidades: [
                {
                  id: 'um-4',
                  nombre: 'Municipios inspeccionados',
                  metasAnuales: { a2025: 20, a2026: 28, a2027: 32, a2028: 40 },
                  responsableAreaId: 'dir-fiscalizacion',
                },
              ],
              areasInvolucradasIds: ['dir-admin'],
            },
          ],
        },
      ],
    },
    {
      id: 'foco-2',
      nombre: 'Fortalecimiento institucional y del talento humano',
      alineacionEnd: 'END 2030 — Eje 1: Administración pública eficiente',
      objetivos: [
        {
          id: 'obj-2-1',
          codigo: 'OE 2.1',
          nombre: 'Profesionalizar la gestión del talento humano',
          productos: [
            {
              id: 'prod-2-1-1-1',
              nombre: 'Plan anual de formación ejecutado',
              unidades: [
                {
                  id: 'um-5',
                  nombre: 'Porcentaje de cumplimiento del plan de capacitación',
                  metasAnuales: { a2025: 75, a2026: 80, a2027: 85, a2028: 90 },
                  responsableAreaId: 'dir-rrhh',
                },
              ],
              areasInvolucradasIds: ['dpto-protocolo'],
            },
          ],
        },
        {
          id: 'obj-2-2',
          codigo: 'OE 2.2',
          nombre: 'Modernizar los procesos internos y su soporte tecnológico',
          productos: [
            {
              id: 'prod-2-2-1-1',
              nombre: 'Procesos rediseñados y automatizados',
              unidades: [
                {
                  id: 'um-6',
                  nombre: 'Procesos automatizados',
                  metasAnuales: { a2025: 2, a2026: 4, a2027: 8, a2028: 10 },
                  responsableAreaId: 'dpto-tic',
                },
              ],
              areasInvolucradasIds: ['dir-planificacion'],
            },
          ],
        },
      ],
    },
    {
      id: 'foco-3',
      nombre: 'Transparencia, integridad y rendición de cuentas',
      alineacionEnd: 'END 2030 — Eje 1: Gobierno transparente',
      objetivos: [
        {
          id: 'obj-3-1',
          codigo: 'OE 3.1',
          nombre: 'Garantizar el acceso oportuno a la información institucional',
          productos: [
            {
              id: 'prod-3-1-1-1',
              nombre: 'Portal de transparencia actualizado',
              unidades: [
                {
                  id: 'um-7',
                  nombre: 'Porcentaje de cumplimiento mensual del portal',
                  metasAnuales: { a2025: 100, a2026: 100, a2027: 100, a2028: 100 },
                  responsableAreaId: 'dir-juridica',
                },
              ],
              areasInvolucradasIds: ['dpto-tic', 'dir-planificacion'],
            },
          ],
        },
      ],
    },
  ],
};

/**
 * Versión original aprobada en 2025, hoy histórica y congelada. Se conserva
 * completa para poder consultar con qué metas se formularon los POA 2025-2026.
 */
export const PEI_V1: Pei = {
  id: 'pei-v1',
  nombre: 'Plan Estratégico Institucional 2025-2028',
  periodo: '2025-2028',
  version: 'v1.0',
  estado: 'Histórico',
  motivo: 'Versión original del PEI aprobada al inicio del período.',
  creadaPor: 'María Fernández',
  fechaCreacion: '2025-02-10',
  aprobadoPor: 'Dra. Altagracia Medina (MAE)',
  fechaAprobacion: '2025-03-21',
  cambios: [],
  focos: [
    {
      id: 'foco-1',
      nombre: 'Servicios institucionales oportunos y de calidad',
      alineacionEnd: 'END 2030 — Eje 1: Estado social democrático de derecho',
      objetivos: [
        {
          id: 'obj-1-1',
          codigo: 'OE 1.1',
          nombre: 'Reducir los tiempos de tramitación de los servicios misionales',
          productos: [
            {
              id: 'prod-1-1-1-1',
              nombre: 'Expedientes tramitados dentro del plazo normativo',
              unidades: [
                {
                  id: 'um-1',
                  nombre: 'Porcentaje de expedientes en plazo',
                  metasAnuales: { a2025: 80, a2026: 85, a2027: 88, a2028: 92 },
                  responsableAreaId: 'dir-tramitacion',
                },
              ],
              areasInvolucradasIds: ['dpto-tic', 'dir-juridica'],
            },
            {
              id: 'prod-1-1-1-2',
              nombre: 'Servicios digitalizados de punta a punta',
              unidades: [
                {
                  id: 'um-2',
                  nombre: 'Servicios digitalizados en producción',
                  // Metas originales: la enmienda v2.0 elevó 2027 a 12
                  metasAnuales: { a2025: 3, a2026: 6, a2027: 9, a2028: 14 },
                  responsableAreaId: 'dir-tramitacion',
                },
              ],
              areasInvolucradasIds: ['dpto-tic'],
            },
            {
              id: 'prod-1-1-2-1',
              nombre: 'Trámites estandarizados y simplificados',
              unidades: [
                {
                  id: 'um-3',
                  nombre: 'Trámites estandarizados',
                  metasAnuales: { a2025: 5, a2026: 10, a2027: 15, a2028: 20 },
                  responsableAreaId: 'dir-tramitacion',
                },
              ],
              areasInvolucradasIds: ['dir-juridica', 'dir-planificacion'],
            },
          ],
        },
        {
          id: 'obj-1-2',
          codigo: 'OE 1.2',
          nombre: 'Fortalecer la fiscalización territorial preventiva',
          productos: [
            {
              id: 'prod-1-2-1-1',
              nombre: 'Municipios con inspección anual realizada',
              unidades: [
                {
                  id: 'um-4',
                  nombre: 'Municipios inspeccionados',
                  metasAnuales: { a2025: 20, a2026: 28, a2027: 32, a2028: 40 },
                  responsableAreaId: 'dir-fiscalizacion',
                },
              ],
              areasInvolucradasIds: ['dir-admin'],
            },
          ],
        },
      ],
    },
    {
      id: 'foco-2',
      nombre: 'Fortalecimiento institucional y del talento humano',
      alineacionEnd: 'END 2030 — Eje 1: Administración pública eficiente',
      objetivos: [
        {
          id: 'obj-2-1',
          codigo: 'OE 2.1',
          nombre: 'Profesionalizar la gestión del talento humano',
          productos: [
            {
              id: 'prod-2-1-1-1',
              nombre: 'Plan anual de formación ejecutado',
              unidades: [
                {
                  id: 'um-5',
                  nombre: 'Porcentaje de cumplimiento del plan de capacitación',
                  metasAnuales: { a2025: 75, a2026: 80, a2027: 85, a2028: 90 },
                  responsableAreaId: 'dir-rrhh',
                },
              ],
              areasInvolucradasIds: ['dpto-protocolo'],
            },
          ],
        },
      ],
    },
  ],
};

/** Historial completo: la vigente primero, luego las históricas. */
export const PEI_VERSIONES: Pei[] = [PEI, PEI_V1];

// ---------------------------------------------------------------------------
// Configuración de columnas del plan anual
// ---------------------------------------------------------------------------

export const COLUMNAS_CONFIG: ColumnaConfig[] = [
  {
    id: 'col-producto',
    nombreVisible: 'Producto',
    nombreTecnico: 'nombreProducto',
    tipo: 'texto',
    obligatoria: true,
    visible: true,
    origen: 'base',
    version: 1,
    autor: 'Sistema',
    fecha: '2026-06-30',
  },
  {
    id: 'col-unidad',
    nombreVisible: 'Unidad de medida',
    nombreTecnico: 'unidadMedida',
    tipo: 'texto',
    obligatoria: true,
    visible: true,
    origen: 'base',
    version: 1,
    autor: 'Sistema',
    fecha: '2026-06-30',
  },
  {
    id: 'col-meta',
    nombreVisible: 'Meta',
    nombreTecnico: 'meta',
    tipo: 'numero',
    obligatoria: true,
    visible: true,
    origen: 'base',
    version: 1,
    autor: 'Sistema',
    fecha: '2026-06-30',
  },
  {
    id: 'col-municipios',
    nombreVisible: 'Municipios de impacto',
    nombreTecnico: 'municipiosImpacto',
    tipo: 'seleccion',
    obligatoria: true,
    visible: true,
    origen: 'base',
    version: 1,
    autor: 'Sistema',
    fecha: '2026-06-30',
  },
  {
    id: 'col-responsable',
    nombreVisible: 'Responsable (área)',
    nombreTecnico: 'responsable',
    tipo: 'seleccion',
    obligatoria: true,
    visible: true,
    origen: 'base',
    version: 1,
    autor: 'Sistema',
    fecha: '2026-06-30',
  },
];

// ---------------------------------------------------------------------------
// Planes anuales del ciclo 2027 (POA de 13 columnas, sin montos)
// ---------------------------------------------------------------------------

export const PLANES: Plan[] = [
  {
    id: 'plan-tramitacion',
    areaId: 'dir-tramitacion',
    cicloId: 'ciclo-2027',
    nombre: 'Plan anual 2027 — Dirección de Tramitación y Registro',
    estado: 'Borrador',
    version: 1,
    fechaLimite: '2026-08-14',
    rows: [
      {
        id: 'row-tr-1',
        codigo: 'TR-01',
        origen: 'pei',
        focoId: 'foco-1',
        objetivoId: 'obj-1-1',
        productoEstrategicoId: 'prod-1-1-1-1',
        nombreProducto: 'Expedientes tramitados dentro del plazo normativo',
        unidades: [
          {
            id: 'upoa-tr-1',
            nombre: 'Porcentaje de expedientes en plazo',
            metaValor: 90,
            metaTipo: 'porcentaje',
            mediosVerificacion: 'Reporte trimestral del sistema de tramitación',
            responsableAreaId: 'dir-tramitacion',
            programacion: { T1: 90, T2: 90, T3: 90, T4: 90 },
          },
        ],
        municipiosImpacto: ['Distrito Nacional', 'Santo Domingo Este'],
        tareas: [
          { id: 'ta-tr-1', nombre: 'Habilitar ventanilla única de recepción', meses: [0, 1, 2], continua: false },
          { id: 'ta-tr-2', nombre: 'Monitoreo mensual de plazos por servicio', meses: [], continua: true },
        ],
        areasInvolucradasIds: ['dpto-tic', 'dir-juridica'],
        custom: {},
      },
      {
        id: 'row-tr-2',
        codigo: 'TR-02',
        origen: 'pei',
        focoId: 'foco-1',
        objetivoId: 'obj-1-1',
        productoEstrategicoId: 'prod-1-1-2-1',
        nombreProducto: 'Trámites estandarizados y simplificados',
        unidades: [
          {
            id: 'upoa-tr-2',
            nombre: 'Trámites estandarizados',
            metaValor: 10,
            metaTipo: 'absoluto',
            mediosVerificacion: 'Resoluciones publicadas y fichas de trámite actualizadas',
            responsableAreaId: 'dir-tramitacion',
            programacion: { T1: 4, T2: 3, T3: 3, T4: 0 },
          },
        ],
        municipiosImpacto: ['Distrito Nacional'],
        tareas: [
          { id: 'ta-tr-3', nombre: 'Levantar requisitos de los 10 trámites de mayor volumen', meses: [0, 1], continua: false },
          { id: 'ta-tr-4', nombre: 'Publicar fichas estandarizadas', meses: [3, 6, 8], continua: false },
        ],
        areasInvolucradasIds: ['dir-juridica'],
        custom: {},
      },
      {
        id: 'row-tr-3',
        codigo: 'TR-03',
        origen: 'pei',
        focoId: 'foco-1',
        objetivoId: 'obj-1-1',
        productoEstrategicoId: 'prod-1-1-1-2',
        nombreProducto: 'Servicio de certificaciones de registro digitalizado',
        unidades: [
          {
            id: 'upoa-tr-5',
            nombre: 'Servicio digitalizado en producción',
            metaValor: 1,
            metaTipo: 'absoluto',
            mediosVerificacion: 'Acta de puesta en producción',
            // Falta responsable (demo de fila incompleta)
            responsableAreaId: '',
            programacion: { T1: 0, T2: 0, T3: 1, T4: 0 },
          },
        ],
        municipiosImpacto: ['Distrito Nacional', 'Santiago de los Caballeros'],
        tareas: [{ id: 'ta-tr-5', nombre: 'Desarrollo e integración con pasarela de pagos', meses: [1, 2, 3, 4, 5], continua: false }],
        areasInvolucradasIds: ['dpto-tic'],
        custom: {},
      },
      {
        id: 'row-tr-4',
        codigo: 'TR-04',
        origen: 'pei',
        focoId: 'foco-1',
        objetivoId: 'obj-1-1',
        productoEstrategicoId: null,
        nombreProducto: 'Jornadas de socialización del portal de servicios',
        unidades: [
          {
            id: 'upoa-tr-6',
            nombre: 'Jornadas realizadas',
            metaValor: 6,
            metaTipo: 'absoluto',
            mediosVerificacion: 'Listas de asistencia y memoria fotográfica',
            responsableAreaId: 'dir-tramitacion',
            programacion: { T1: 0, T2: 2, T3: 2, T4: 2 },
          },
        ],
        // Falta municipios (demo de fila incompleta)
        municipiosImpacto: [],
        // Sin tareas: demo del bloqueo de requerimientos (no hay a qué tarea colgarlos)
        tareas: [],
        areasInvolucradasIds: ['dpto-protocolo'],
        custom: {},
      },
      {
        id: 'row-tr-5',
        codigo: 'TR-05',
        origen: 'pei',
        focoId: 'foco-1',
        objetivoId: 'obj-1-1',
        productoEstrategicoId: null,
        nombreProducto: 'Mensajería interinstitucional de expedientes fortalecida',
        unidades: [
          {
            id: 'upoa-tr-7',
            nombre: 'Rutas de mensajería operando',
            metaValor: 3,
            metaTipo: 'absoluto',
            mediosVerificacion: 'Bitácoras de ruta',
            responsableAreaId: 'dir-tramitacion',
            programacion: { T1: 1, T2: 1, T3: 1, T4: 0 },
          },
        ],
        municipiosImpacto: ['Distrito Nacional', 'Santo Domingo Este', 'Santo Domingo Norte'],
        tareas: [{ id: 'ta-tr-7', nombre: 'Adquirir y rotular vehículo de mensajería', meses: [0, 1], continua: false }],
        areasInvolucradasIds: ['dir-admin'],
        custom: {},
      },
      {
        id: 'row-tr-6',
        codigo: 'TR-06',
        // Ajuste 2: producto OPERATIVO, no vinculado al PEI. No exige alineación
        // estratégica, pero sí una justificación.
        origen: 'operativo',
        focoId: null,
        objetivoId: null,
        productoEstrategicoId: null,
        justificacionOperativa:
          'Actividad operativa de soporte: reorganización del archivo físico para cumplir la normativa de gestión documental. No responde a un producto del PEI.',
        nombreProducto: 'Archivo físico de expedientes históricos reorganizado',
        unidades: [
          {
            id: 'upoa-tr-8',
            nombre: 'Porcentaje del archivo reorganizado',
            metaValor: 100,
            metaTipo: 'porcentaje',
            mediosVerificacion: 'Inventario documental y actas de descarte',
            responsableAreaId: 'dir-tramitacion',
            programacion: { T1: 0, T2: 0, T3: 50, T4: 100 },
          },
        ],
        municipiosImpacto: ['Distrito Nacional'],
        tareas: [{ id: 'ta-tr-8', nombre: 'Acondicionar depósito y mobiliario del archivo', meses: [6, 7], continua: false }],
        areasInvolucradasIds: ['dir-admin'],
        custom: {},
      },
    ],
  },
  {
    id: 'plan-juridica',
    areaId: 'dir-juridica',
    cicloId: 'ciclo-2027',
    nombre: 'Plan anual 2027 — Dirección Jurídica',
    estado: 'Listo para revisión',
    version: 2,
    fechaLimite: '2026-08-14',
    rows: [
      {
        id: 'row-ju-1',
        codigo: 'JU-01',
        origen: 'pei',
        focoId: 'foco-1',
        objetivoId: 'obj-1-1',
        productoEstrategicoId: null,
        nombreProducto: 'Opiniones legales emitidas en plazo',
        unidades: [
          {
            id: 'upoa-ju-1',
            nombre: 'Porcentaje de opiniones emitidas en 10 días o menos',
            metaValor: 92,
            metaTipo: 'porcentaje',
            mediosVerificacion: 'Matriz de control de opiniones',
            responsableAreaId: 'dir-juridica',
            programacion: { T1: 92, T2: 92, T3: 92, T4: 92 },
          },
        ],
        municipiosImpacto: ['Distrito Nacional'],
        tareas: [
          { id: 'ta-ju-1', nombre: 'Depurar plantillas de opiniones recurrentes', meses: [0, 1], continua: false },
          { id: 'ta-ju-5', nombre: 'Consultar jurisprudencia actualizada para cada opinión', meses: [], continua: true },
        ],
        areasInvolucradasIds: ['dir-tramitacion'],
        custom: {},
      },
      {
        id: 'row-ju-2',
        codigo: 'JU-02',
        origen: 'pei',
        focoId: 'foco-3',
        objetivoId: 'obj-3-1',
        productoEstrategicoId: 'prod-3-1-1-1',
        nombreProducto: 'Portal de transparencia actualizado',
        unidades: [
          {
            id: 'upoa-ju-2',
            nombre: 'Porcentaje de cumplimiento mensual del portal',
            metaValor: 100,
            metaTipo: 'porcentaje',
            mediosVerificacion: 'Certificación mensual de la DIGEIG',
            responsableAreaId: 'dir-juridica',
            programacion: { T1: 100, T2: 100, T3: 100, T4: 100 },
          },
        ],
        municipiosImpacto: ['Distrito Nacional'],
        tareas: [{ id: 'ta-ju-2', nombre: 'Cargar matrices mensuales en el portal', meses: [], continua: true }],
        areasInvolucradasIds: ['dpto-tic', 'dir-planificacion'],
        custom: {},
      },
      {
        id: 'row-ju-3',
        codigo: 'JU-03',
        origen: 'pei',
        focoId: 'foco-2',
        objetivoId: 'obj-2-1',
        productoEstrategicoId: null,
        nombreProducto: 'Talleres regionales sobre el nuevo marco de contrataciones (Ley 47-25)',
        unidades: [
          {
            id: 'upoa-ju-3',
            nombre: 'Talleres realizados',
            metaValor: 3,
            metaTipo: 'absoluto',
            mediosVerificacion: 'Listas de asistencia y relatorías',
            responsableAreaId: 'dir-juridica',
            programacion: { T1: 0, T2: 1, T3: 2, T4: 0 },
          },
        ],
        municipiosImpacto: ['Santiago de los Caballeros', 'San Juan de la Maguana', 'Higüey'],
        tareas: [
          { id: 'ta-ju-3', nombre: 'Preparar material didáctico', meses: [2, 3], continua: false },
          { id: 'ta-ju-4', nombre: 'Ejecutar los talleres regionales', meses: [4, 7, 8], continua: false },
        ],
        areasInvolucradasIds: ['dpto-protocolo', 'dir-rrhh'],
        custom: {},
      },
    ],
  },
  {
    id: 'plan-fiscalizacion',
    areaId: 'dir-fiscalizacion',
    cicloId: 'ciclo-2027',
    nombre: 'Plan anual 2027 — Dirección de Fiscalización Territorial',
    estado: 'Observado',
    version: 1,
    fechaLimite: '2026-08-14',
    observacionGeneral:
      'Los requerimientos del área exceden el techo presupuestario asignado. Priorizar municipios o ajustar cantidades.',
    rows: [
      {
        id: 'row-fi-1',
        codigo: 'FI-01',
        origen: 'pei',
        focoId: 'foco-1',
        objetivoId: 'obj-1-2',
        productoEstrategicoId: 'prod-1-2-1-1',
        nombreProducto: 'Municipios con inspección anual realizada (región Norte)',
        unidades: [
          {
            id: 'upoa-fi-1',
            nombre: 'Municipios inspeccionados',
            metaValor: 18,
            metaTipo: 'absoluto',
            mediosVerificacion: 'Actas de inspección firmadas',
            responsableAreaId: 'dir-fiscalizacion',
            programacion: { T1: 6, T2: 6, T3: 6, T4: 0 },
          },
        ],
        municipiosImpacto: ['Santiago de los Caballeros', 'La Vega', 'Puerto Plata'],
        tareas: [
          { id: 'ta-fi-1', nombre: 'Programar rutas de operativos', meses: [0], continua: false },
          { id: 'ta-fi-2', nombre: 'Ejecutar operativos región Norte', meses: [1, 2, 3, 4, 5, 6, 7, 8], continua: false },
        ],
        areasInvolucradasIds: ['dir-admin'],
        custom: {},
      },
      {
        id: 'row-fi-2',
        codigo: 'FI-02',
        origen: 'pei',
        focoId: 'foco-1',
        objetivoId: 'obj-1-2',
        productoEstrategicoId: 'prod-1-2-1-1',
        nombreProducto: 'Municipios con inspección anual realizada (región Sur)',
        unidades: [
          {
            id: 'upoa-fi-2',
            nombre: 'Municipios inspeccionados',
            metaValor: 14,
            metaTipo: 'absoluto',
            mediosVerificacion: 'Actas de inspección firmadas',
            responsableAreaId: 'dir-fiscalizacion',
            programacion: { T1: 0, T2: 5, T3: 5, T4: 4 },
          },
        ],
        municipiosImpacto: ['Azua de Compostela', 'San Juan de la Maguana', 'San Cristóbal'],
        tareas: [{ id: 'ta-fi-3', nombre: 'Ejecutar operativos región Sur', meses: [3, 4, 5, 6, 7, 8, 9, 10], continua: false }],
        areasInvolucradasIds: ['dir-admin'],
        custom: {},
      },
      {
        id: 'row-fi-3',
        codigo: 'FI-03',
        origen: 'pei',
        focoId: 'foco-2',
        objetivoId: 'obj-2-2',
        productoEstrategicoId: null,
        nombreProducto: 'Inspectores equipados con herramientas digitales de campo',
        unidades: [
          {
            id: 'upoa-fi-3',
            nombre: 'Inspectores equipados',
            metaValor: 25,
            metaTipo: 'absoluto',
            mediosVerificacion: 'Actas de entrega de equipos',
            responsableAreaId: 'dir-fiscalizacion',
            programacion: { T1: 25, T2: 0, T3: 0, T4: 0 },
          },
        ],
        municipiosImpacto: ['Distrito Nacional'],
        tareas: [
          { id: 'ta-fi-4', nombre: 'Levantar especificaciones técnicas con TIC', meses: [0], continua: false },
          { id: 'ta-fi-5', nombre: 'Entregar y configurar los equipos de campo', meses: [1, 2], continua: false },
        ],
        areasInvolucradasIds: ['dpto-tic'],
        custom: {},
      },
    ],
  },
  {
    id: 'plan-rrhh',
    areaId: 'dir-rrhh',
    cicloId: 'ciclo-2027',
    nombre: 'Plan anual 2027 — Dirección de Recursos Humanos',
    estado: 'Borrador',
    version: 1,
    fechaLimite: '2026-08-14',
    rows: [
      {
        id: 'row-rh-1',
        codigo: 'RH-01',
        origen: 'pei',
        focoId: 'foco-2',
        objetivoId: 'obj-2-1',
        productoEstrategicoId: 'prod-2-1-1-1',
        nombreProducto: 'Plan anual de formación ejecutado',
        unidades: [
          {
            id: 'upoa-rh-1',
            nombre: 'Porcentaje de cumplimiento del plan de capacitación',
            metaValor: 85,
            metaTipo: 'porcentaje',
            mediosVerificacion: 'Listas de participantes y evaluaciones',
            responsableAreaId: 'dir-rrhh',
            programacion: { T1: 20, T2: 45, T3: 70, T4: 85 },
          },
        ],
        municipiosImpacto: ['Distrito Nacional'],
        tareas: [
          { id: 'ta-rh-1', nombre: 'Ejecutar acciones formativas del calendario', meses: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10], continua: false },
          { id: 'ta-rh-3', nombre: 'Acondicionar el salón de capacitaciones', meses: [0], continua: false },
        ],
        areasInvolucradasIds: ['dpto-protocolo'],
        custom: {},
      },
      {
        id: 'row-rh-2',
        codigo: 'RH-02',
        origen: 'pei',
        focoId: 'foco-2',
        objetivoId: 'obj-2-1',
        productoEstrategicoId: null,
        nombreProducto: 'Estudio de clima organizacional y plan de mejora',
        unidades: [
          {
            id: 'upoa-rh-2',
            nombre: 'Estudio aplicado y plan formulado',
            metaValor: 1,
            metaTipo: 'absoluto',
            mediosVerificacion: 'Informe de resultados y plan de mejora aprobado',
            responsableAreaId: 'dir-rrhh',
            programacion: { T1: 0, T2: 1, T3: 0, T4: 0 },
          },
        ],
        municipiosImpacto: ['Distrito Nacional'],
        tareas: [{ id: 'ta-rh-2', nombre: 'Aplicar encuesta institucional', meses: [3], continua: false }],
        areasInvolucradasIds: ['dir-planificacion'],
        custom: {},
      },
    ],
  },
  {
    id: 'plan-planificacion',
    areaId: 'dir-planificacion',
    cicloId: 'ciclo-2027',
    nombre: 'Plan anual 2027 — Dirección de Planificación y Desarrollo',
    estado: 'Borrador',
    version: 1,
    fechaLimite: '2026-08-14',
    rows: [
      {
        id: 'row-pl-1',
        codigo: 'PL-01',
        origen: 'pei',
        focoId: 'foco-2',
        objetivoId: 'obj-2-2',
        productoEstrategicoId: null,
        nombreProducto: 'Guía metodológica de formulación del POA actualizada',
        unidades: [
          {
            id: 'upoa-pl-1',
            nombre: 'Guía actualizada y socializada',
            metaValor: 1,
            metaTipo: 'absoluto',
            mediosVerificacion: 'Documento aprobado y registro de socialización',
            responsableAreaId: 'dir-planificacion',
            programacion: { T1: 1, T2: 0, T3: 0, T4: 0 },
          },
        ],
        municipiosImpacto: ['Distrito Nacional'],
        tareas: [{ id: 'ta-pl-1', nombre: 'Socializar la guía con todas las áreas', meses: [1, 2], continua: false }],
        areasInvolucradasIds: ['dir-rrhh'],
        custom: {},
      },
      {
        id: 'row-pl-2',
        codigo: 'PL-02',
        origen: 'pei',
        focoId: 'foco-2',
        objetivoId: 'obj-2-2',
        productoEstrategicoId: null,
        nombreProducto: 'Informes trimestrales de seguimiento del POA',
        unidades: [
          {
            id: 'upoa-pl-2',
            nombre: 'Informes de corte publicados',
            metaValor: 4,
            metaTipo: 'absoluto',
            mediosVerificacion: 'Informes publicados en la intranet',
            responsableAreaId: 'dir-planificacion',
            programacion: { T1: 1, T2: 1, T3: 1, T4: 1 },
          },
        ],
        municipiosImpacto: ['Distrito Nacional'],
        tareas: [{ id: 'ta-pl-2', nombre: 'Consolidar cortes trimestrales', meses: [2, 5, 8, 11], continua: false }],
        areasInvolucradasIds: [],
        custom: {},
      },
    ],
  },
  {
    id: 'plan-admin',
    areaId: 'dir-admin',
    cicloId: 'ciclo-2027',
    nombre: 'Plan anual 2027 — Dirección Administrativa y Financiera',
    estado: 'Sin iniciar',
    version: 1,
    fechaLimite: '2026-08-21',
    rows: [],
  },
  {
    id: 'plan-protocolo',
    areaId: 'dpto-protocolo',
    cicloId: 'ciclo-2027',
    nombre: 'Plan anual 2027 — Departamento de Protocolo y Eventos',
    estado: 'Sin iniciar',
    version: 1,
    fechaLimite: '2026-08-21',
    rows: [],
  },
  {
    id: 'plan-tic',
    areaId: 'dpto-tic',
    cicloId: 'ciclo-2027',
    nombre: 'Plan anual 2027 — Departamento de Tecnologías de la Información',
    estado: 'Sin iniciar',
    version: 1,
    fechaLimite: '2026-08-21',
    rows: [],
  },
];

// ---------------------------------------------------------------------------
// Requerimientos transversales (formato PACC)
// ---------------------------------------------------------------------------

export const REQUERIMIENTOS: Requerimiento[] = [
  {
    id: 'req-1',
    codigo: 'RQ-2027-001',
    origenAreaId: 'dir-tramitacion',
    planId: 'plan-tramitacion',
    rowId: 'row-tr-5',
    tareaId: 'ta-tr-7',
    categoriaId: 'transporte',
    rubro: 'Vehículo',
    rubroId: 'rub-vehiculos',
    articuloId: 'art-1',
    descripcion: 'Vehículo tipo panel para mensajería interinstitucional de expedientes',
    cantidad: 1,
    unidadMedida: 'Unidad',
    precioEstimado: 1_450_000,
    trimestre: 'T1',
    fuenteFinanciamiento: 'Presupuesto nacional',
    prioridad: 'Alta',
    estado: 'Nuevo',
    reglaAplicada: 'Regla rr-1 v3: rubro «Vehículo» → Dirección Administrativa y Financiera',
    comentarios: [],
  },
  {
    id: 'req-2',
    codigo: 'RQ-2027-002',
    origenAreaId: 'dir-tramitacion',
    planId: 'plan-tramitacion',
    rowId: 'row-tr-6',
    tareaId: 'ta-tr-8',
    categoriaId: 'mobiliario-equipos',
    rubro: 'Mobiliario de oficina',
    rubroId: 'rub-mobiliario',
    articuloId: 'art-9',
    descripcion: 'Estantería metálica y archivos móviles para reorganización del archivo histórico',
    cantidad: 12,
    unidadMedida: 'Unidades',
    precioEstimado: 21_500,
    trimestre: 'T3',
    fuenteFinanciamiento: 'Fondos propios',
    prioridad: 'Media',
    estado: 'Nuevo',
    reglaAplicada: 'Regla rr-3 v2: rubro «Mobiliario de oficina» → Dirección Administrativa y Financiera',
    comentarios: [],
  },
  {
    id: 'req-3',
    codigo: 'RQ-2027-003',
    origenAreaId: 'dir-fiscalizacion',
    planId: 'plan-fiscalizacion',
    rowId: 'row-fi-1',
    tareaId: 'ta-fi-2',
    categoriaId: 'transporte',
    rubro: 'Combustible',
    rubroId: 'rub-combustible',
    articuloId: 'art-4',
    descripcion: 'Combustible para operativos de inspección región Norte',
    cantidad: 800,
    unidadMedida: 'Galones',
    precioEstimado: 600,
    trimestre: 'T1',
    fuenteFinanciamiento: 'Presupuesto nacional',
    prioridad: 'Alta',
    estado: 'En revisión',
    reglaAplicada: 'Regla rr-1 v3: rubro «Combustible» → Dirección Administrativa y Financiera',
    comentarios: [
      {
        autor: 'Carmen Rosario',
        area: 'Administrativa',
        fecha: '2026-07-10',
        texto: 'Favor indicar la distribución mensual de galones y los vehículos asignados.',
      },
    ],
  },
  {
    id: 'req-4',
    codigo: 'RQ-2027-004',
    origenAreaId: 'dir-fiscalizacion',
    planId: 'plan-fiscalizacion',
    rowId: 'row-fi-2',
    tareaId: 'ta-fi-3',
    categoriaId: 'transporte',
    rubro: 'Combustible',
    rubroId: 'rub-combustible',
    articuloId: 'art-4',
    descripcion: 'Combustible para operativos de inspección región Sur',
    cantidad: 700,
    unidadMedida: 'Galones',
    precioEstimado: 600,
    trimestre: 'T2',
    fuenteFinanciamiento: 'Presupuesto nacional',
    prioridad: 'Alta',
    estado: 'En revisión',
    reglaAplicada: 'Regla rr-1 v3: rubro «Combustible» → Dirección Administrativa y Financiera',
    comentarios: [],
  },
  {
    id: 'req-5',
    codigo: 'RQ-2027-005',
    origenAreaId: 'dir-fiscalizacion',
    planId: 'plan-fiscalizacion',
    rowId: 'row-fi-1',
    tareaId: 'ta-fi-2',
    categoriaId: 'transporte',
    rubro: 'Vehículo',
    rubroId: 'rub-vehiculos',
    descripcion: 'Camionetas 4x4 para operativos de inspección territorial',
    cantidad: 3,
    unidadMedida: 'Unidades',
    precioEstimado: 1_900_000,
    trimestre: 'T1',
    fuenteFinanciamiento: 'Presupuesto nacional',
    prioridad: 'Alta',
    estado: 'Nuevo',
    reglaAplicada: 'Regla rr-1 v3: rubro «Vehículo» → Dirección Administrativa y Financiera',
    comentarios: [],
  },
  {
    id: 'req-6',
    codigo: 'RQ-2027-006',
    origenAreaId: 'dir-rrhh',
    planId: 'plan-rrhh',
    rowId: 'row-rh-1',
    tareaId: 'ta-rh-1',
    categoriaId: 'eventos-audiovisual',
    rubro: 'Montaje de eventos',
    rubroId: 'rub-montaje',
    articuloId: 'art-11',
    descripcion: 'Salones, sonido y refrigerio para 12 jornadas del plan de capacitación',
    cantidad: 12,
    unidadMedida: 'Jornadas',
    precioEstimado: 29_000,
    // Protocolo ya validó el precio al aprobarlo
    precioConfirmado: true,
    trimestre: 'T1',
    fuenteFinanciamiento: 'Fondos propios',
    prioridad: 'Media',
    estado: 'Aprobado',
    reglaAplicada: 'Regla rr-4 v1: rubro «Montaje de eventos» → Departamento de Protocolo y Eventos',
    comentarios: [
      {
        autor: 'Laura Jiménez',
        area: 'Protocolo',
        fecha: '2026-07-08',
        texto: 'Aprobado. Se consolidará con el calendario institucional de eventos.',
      },
    ],
  },
  {
    id: 'req-7',
    codigo: 'RQ-2027-007',
    origenAreaId: 'dir-juridica',
    planId: 'plan-juridica',
    rowId: 'row-ju-3',
    tareaId: 'ta-ju-4',
    categoriaId: 'eventos-audiovisual',
    rubro: 'Audiovisual',
    rubroId: 'rub-audiovisual',
    descripcion: 'Proyección y grabación para talleres regionales de la Ley 47-25',
    cantidad: 3,
    unidadMedida: 'Servicios',
    // Rubro sin costo: Protocolo lo cubre con su equipo propio (demo del modo
    // «capacidad instalada»: sin monto, sin fuente, con fecha requerida).
    precioEstimado: 0,
    trimestre: 'T2',
    fechaRequerida: '2027-05-12',
    fuenteFinanciamiento: '',
    prioridad: 'Media',
    estado: 'Nuevo',
    reglaAplicada: 'Regla rr-4 v1: rubro «Audiovisual» → Departamento de Protocolo y Eventos',
    comentarios: [],
  },
  {
    id: 'req-8',
    codigo: 'RQ-2027-008',
    origenAreaId: 'dir-tramitacion',
    planId: 'plan-tramitacion',
    rowId: 'row-tr-1',
    tareaId: 'ta-tr-1',
    categoriaId: 'soporte-tic',
    rubro: 'Equipos de cómputo',
    rubroId: 'rub-computo',
    articuloId: 'art-15',
    descripcion: 'Estaciones de trabajo y escáneres para la ventanilla única',
    cantidad: 8,
    unidadMedida: 'Estaciones',
    precioEstimado: 65_000,
    trimestre: 'T1',
    fuenteFinanciamiento: 'Presupuesto nacional',
    prioridad: 'Alta',
    estado: 'En revisión',
    reglaAplicada: 'Regla rr-5 v4: rubro «Equipos de cómputo» → Departamento de TIC',
    comentarios: [],
  },
  {
    id: 'req-9',
    codigo: 'RQ-2027-009',
    origenAreaId: 'dir-fiscalizacion',
    planId: 'plan-fiscalizacion',
    rowId: 'row-fi-3',
    tareaId: 'ta-fi-5',
    categoriaId: 'soporte-tic',
    rubro: 'Equipos de cómputo',
    rubroId: 'rub-computo',
    articuloId: 'art-16',
    descripcion: 'Tabletas con conectividad para levantamiento de inspecciones en campo',
    cantidad: 25,
    unidadMedida: 'Unidades',
    precioEstimado: 31_200,
    trimestre: 'T1',
    fuenteFinanciamiento: 'Presupuesto nacional',
    prioridad: 'Alta',
    estado: 'Nuevo',
    reglaAplicada: 'Regla rr-5 v4: rubro «Equipos de cómputo» → Departamento de TIC',
    comentarios: [],
  },
  {
    id: 'req-10',
    codigo: 'RQ-2027-010',
    origenAreaId: 'dir-rrhh',
    planId: 'plan-rrhh',
    rowId: 'row-rh-1',
    tareaId: 'ta-rh-3',
    categoriaId: 'mobiliario-equipos',
    rubro: 'Equipos de climatización',
    rubroId: 'rub-climatizacion',
    articuloId: 'art-10',
    descripcion: 'Acondicionador de aire para el salón de capacitaciones',
    cantidad: 1,
    unidadMedida: 'Unidad',
    precioEstimado: 145_000,
    trimestre: 'T1',
    fuenteFinanciamiento: 'Fondos propios',
    prioridad: 'Baja',
    estado: 'Devuelto',
    reglaAplicada: 'Regla rr-3 v2: rubro «Equipos de climatización» → Dirección Administrativa y Financiera',
    comentarios: [
      {
        autor: 'Carmen Rosario',
        area: 'Administrativa',
        fecha: '2026-07-12',
        texto: 'Devuelto: especificar capacidad (BTU) y si sustituye un equipo existente.',
      },
    ],
  },
  {
    id: 'req-11',
    codigo: 'RQ-2027-011',
    origenAreaId: 'dir-juridica',
    planId: 'plan-juridica',
    rowId: 'row-ju-1',
    tareaId: 'ta-ju-5',
    categoriaId: 'compras-bienes',
    rubro: 'Servicios contratables',
    rubroId: 'rub-servicios',
    descripcion: 'Suscripción anual a base de datos de jurisprudencia',
    cantidad: 1,
    unidadMedida: 'Suscripción anual',
    precioEstimado: 220_000,
    trimestre: 'T1',
    fuenteFinanciamiento: 'Fondos propios',
    prioridad: 'Media',
    estado: 'Nuevo',
    reglaAplicada: 'Regla rr-8 v1: rubro «Servicios contratables» → Departamento de Compras',
    comentarios: [],
  },
];

/**
 * Bitácora de decisiones de priorización (ajuste 14). Seed de una devolución ya
 * registrada para que la trazabilidad no arranque vacía en la demo.
 */
export const DECISIONES_PRIORIZACION: DecisionPriorizacion[] = [
  {
    id: 'dec-1',
    requerimientoId: 'req-10',
    codigoRequerimiento: 'RQ-2027-010',
    rubroId: 'rub-climatizacion',
    tipo: 'Devolución',
    montoSolicitado: 145_000,
    montoAprobado: 0,
    motivo: 'Falta especificar capacidad (BTU) y si sustituye un equipo existente.',
    decidePor: 'Carmen Rosario',
    areaDecideId: 'dir-admin',
    fecha: '2026-07-12',
    nivel: 'consolidadora',
  },
];

/**
 * Monto total de un requerimiento. Si el área consolidadora lo redujo por techo
 * (ajuste 14), manda el monto priorizado; si no, es cantidad × precio estimado.
 */
export function montoDe(
  req: Pick<Requerimiento, 'cantidad' | 'precioEstimado' | 'montoAprobado'>,
): number {
  if (req.montoAprobado !== undefined && req.montoAprobado !== null) return req.montoAprobado;
  return (req.cantidad || 0) * (req.precioEstimado || 0);
}

// ---------------------------------------------------------------------------
// Techos presupuestarios (ajuste 10: por GRAN RUBRO institucional, no por área)
// ---------------------------------------------------------------------------

const techo = (
  rubroId: string,
  montoInicial: number,
  fuente: string,
  clasificador: string,
  ajustes: TechoRubro['ajustes'] = [],
): TechoRubro => ({ rubroId, montoInicial, ajustes, fuente, clasificador });

export const TECHOS_RUBRO: TechoRubro[] = [
  techo('rub-vehiculos', 5_500_000, 'Presupuesto nacional', '2.6 — Bienes muebles'),
  // Combustible queda por debajo de la demanda: demo de exceso por rubro
  techo('rub-combustible', 900_000, 'Presupuesto nacional', '2.3 — Materiales y suministros', [
    { fecha: '2026-07-14', motivo: 'Reducción por reprogramación institucional', monto: -100_000 },
  ]),
  techo('rub-mantenimiento-veh', 700_000, 'Fondos propios', '2.2 — Servicios no personales'),
  techo('rub-viaticos', 1_200_000, 'Presupuesto nacional', '2.2 — Servicios no personales'),
  techo('rub-mobiliario', 800_000, 'Presupuesto nacional', '2.6 — Bienes muebles'),
  techo('rub-climatizacion', 400_000, 'Fondos propios', '2.6 — Bienes muebles'),
  techo('rub-papeleria', 500_000, 'Fondos propios', '2.3 — Materiales y suministros'),
  techo('rub-montaje', 900_000, 'Presupuesto nacional', '2.2 — Servicios no personales', [
    { fecha: '2026-07-05', motivo: 'Refuerzo para actos institucionales', monto: 150_000 },
  ]),
  techo('rub-refrigerio', 300_000, 'Fondos propios', '2.2 — Servicios no personales'),
  techo('rub-computo', 2_400_000, 'Presupuesto nacional', '2.6 — Bienes muebles'),
  techo('rub-licencias', 900_000, 'Presupuesto nacional', '2.2 — Servicios no personales'),
  techo('rub-conectividad', 700_000, 'Presupuesto nacional', '2.2 — Servicios no personales'),
  techo('rub-equipamiento-tec', 600_000, 'Presupuesto nacional', '2.6 — Bienes muebles'),
  techo('rub-capacitacion', 1_500_000, 'Presupuesto nacional', '2.2 — Servicios no personales'),
  techo('rub-publicidad', 1_000_000, 'Presupuesto nacional', '2.2 — Servicios no personales'),
  techo('rub-impresos', 600_000, 'Fondos propios', '2.2 — Servicios no personales'),
  techo('rub-bienes', 1_800_000, 'Presupuesto nacional', '2.3 — Materiales y suministros'),
  techo('rub-servicios', 2_000_000, 'Fondos propios', '2.2 — Servicios no personales'),
  techo('rub-asistencia', 1_500_000, 'Fondos propios', '2.2 — Servicios no personales'),
];

// ---------------------------------------------------------------------------
// Seguimiento (ciclo 2026 en ejecución, corte 30 de junio de 2026)
// ---------------------------------------------------------------------------

export const REGISTROS_SEGUIMIENTO: RegistroSeguimiento[] = [
  {
    id: 'seg-1',
    planId: 'plan-2026-tramitacion',
    rowId: 'row-2026-tr-1',
    actividad: 'Expedientes tramitados dentro del plazo normativo (POA 2026)',
    areaId: 'dir-tramitacion',
    metaPeriodo: '85% de expedientes en plazo',
    corte: '2026-06-30',
    avanceFisico: null,
    avanceFinanciero: null,
    evidencias: [],
    estado: 'Pendiente',
    historial: [
      {
        corte: '2026-03-31',
        avanceFisico: 82,
        avanceFinanciero: 24,
        comentario: 'Avance dentro de lo esperado. Pico estacional en marzo.',
      },
    ],
  },
  {
    id: 'seg-2',
    planId: 'plan-2026-tramitacion',
    rowId: 'row-2026-tr-2',
    actividad: 'Digitalización del servicio de constancias (POA 2026)',
    areaId: 'dir-tramitacion',
    metaPeriodo: 'Piloto en producción',
    corte: '2026-06-30',
    avanceFisico: null,
    avanceFinanciero: null,
    evidencias: [],
    estado: 'Pendiente',
    historial: [
      {
        corte: '2026-03-31',
        avanceFisico: 40,
        avanceFinanciero: 35,
        comentario: 'Desarrollo en curso; pruebas internas iniciadas.',
      },
    ],
  },
  {
    id: 'seg-3',
    planId: 'plan-2026-fiscalizacion',
    rowId: 'row-2026-fi-1',
    actividad: 'Operativos de inspección territorial primer semestre (POA 2026)',
    areaId: 'dir-fiscalizacion',
    metaPeriodo: '15 municipios inspeccionados',
    corte: '2026-06-30',
    avanceFisico: 60,
    avanceFinanciero: 72,
    comentario: 'Se cubrieron 9 de 15 municipios. Retraso por disponibilidad de vehículos.',
    evidencias: ['informe-operativos-s1.pdf'],
    riesgo: 'Desviación financiera sobre lo físico: revisar costos de transporte.',
    estado: 'Reportado',
    historial: [
      {
        corte: '2026-03-31',
        avanceFisico: 33,
        avanceFinanciero: 30,
        comentario: 'Primeros 5 municipios completados.',
      },
    ],
  },
  {
    id: 'seg-4',
    planId: 'plan-2026-rrhh',
    rowId: 'row-2026-rh-1',
    actividad: 'Plan anual de formación por competencias (POA 2026)',
    areaId: 'dir-rrhh',
    metaPeriodo: '50% del plan ejecutado',
    corte: '2026-06-30',
    avanceFisico: 48,
    avanceFinanciero: 41,
    comentario: '12 de 25 acciones formativas completadas.',
    evidencias: ['listado-participantes-jun.xlsx'],
    estado: 'Reportado',
    historial: [
      {
        corte: '2026-03-31',
        avanceFisico: 22,
        avanceFinanciero: 18,
        comentario: 'Arranque según calendario.',
      },
    ],
  },
  {
    id: 'seg-5',
    planId: 'plan-2026-juridica',
    rowId: 'row-2026-ju-1',
    actividad: 'Opiniones legales emitidas en plazo (POA 2026)',
    areaId: 'dir-juridica',
    metaPeriodo: '90% en plazo',
    corte: '2026-06-30',
    avanceFisico: null,
    avanceFinanciero: null,
    evidencias: [],
    estado: 'Pendiente',
    historial: [
      {
        corte: '2026-03-31',
        avanceFisico: 91,
        avanceFinanciero: 20,
        comentario: 'Cumplimiento sostenido.',
      },
    ],
  },
  {
    id: 'seg-6',
    planId: 'plan-2026-planificacion',
    rowId: 'row-2026-pl-1',
    actividad: 'Informes trimestrales de seguimiento del POA 2026',
    areaId: 'dir-planificacion',
    metaPeriodo: 'Informe T2 publicado',
    corte: '2026-06-30',
    avanceFisico: null,
    avanceFinanciero: null,
    evidencias: [],
    estado: 'Pendiente',
    historial: [
      {
        corte: '2026-03-31',
        avanceFisico: 100,
        avanceFinanciero: 25,
        comentario: 'Informe T1 publicado en fecha.',
      },
    ],
  },
];

// ---------------------------------------------------------------------------
// Actividad reciente
// ---------------------------------------------------------------------------

export const ACTIVIDAD_INICIAL: EventoAuditoria[] = [
  {
    id: 'ev-1',
    fecha: '2026-07-17 10:42',
    persona: 'María Fernández',
    modulo: 'Plan anual',
    accion: 'Devolución con observaciones',
    detalle: 'Plan de Fiscalización Territorial devuelto: sus requerimientos exceden el techo del área.',
  },
  {
    id: 'ev-2',
    fecha: '2026-07-16 15:20',
    persona: 'Rafael Guzmán',
    modulo: 'Plan anual',
    accion: 'Sometimiento a revisión',
    detalle: 'Plan de la Dirección Jurídica sometido a revisión (v2).',
  },
  {
    id: 'ev-3',
    fecha: '2026-07-12 09:05',
    persona: 'Carmen Rosario',
    modulo: 'Requerimientos',
    accion: 'Devolución de requerimiento',
    detalle: 'RQ-2027-010 devuelto a RR. HH.: falta especificación técnica.',
  },
  {
    id: 'ev-4',
    fecha: '2026-07-10 11:30',
    persona: 'Carmen Rosario',
    modulo: 'Requerimientos',
    accion: 'Solicitud de información',
    detalle: 'Comentario en RQ-2027-003: distribución mensual de galones.',
  },
  {
    id: 'ev-5',
    fecha: '2026-07-08 14:12',
    persona: 'Laura Jiménez',
    modulo: 'Requerimientos',
    accion: 'Aprobación',
    detalle: 'RQ-2027-006 aprobado por Protocolo y Eventos.',
  },
  {
    id: 'ev-6',
    fecha: '2026-07-02 16:44',
    persona: 'Luis Peña',
    modulo: 'Configuración',
    accion: 'Actualización de techos',
    detalle: 'Techos por rubro del ciclo 2027 cargados para 4 áreas.',
  },
  {
    id: 'ev-7',
    fecha: '2026-06-30 09:00',
    persona: 'María Fernández',
    modulo: 'Ciclos',
    accion: 'Publicación de ciclo',
    detalle: 'Ciclo POA 2027 publicado. Formulación abierta hasta el 14/08/2026.',
  },
];

// ---------------------------------------------------------------------------
// Catálogo jerárquico: Área transversal → Familia → Rubro → Artículo
// ---------------------------------------------------------------------------

/** Ajuste 7: cada familia se agrupa bajo una de las 6 áreas consolidadoras. */
export const FAMILIAS_SERVICIO: FamiliaServicio[] = [
  { id: 'fam-transporte', nombre: 'Transporte y combustible', areaReceptoraId: 'dir-admin' },
  { id: 'fam-viaticos', nombre: 'Viáticos y gastos de viaje', areaReceptoraId: 'dir-admin' },
  { id: 'fam-mobiliario', nombre: 'Mobiliario, planta física y suministros', areaReceptoraId: 'dir-admin' },
  { id: 'fam-eventos', nombre: 'Eventos y protocolo', areaReceptoraId: 'dpto-protocolo' },
  { id: 'fam-tic', nombre: 'Tecnología', areaReceptoraId: 'dpto-tic' },
  { id: 'fam-capacitacion', nombre: 'Capacitación y formación', areaReceptoraId: 'dir-rrhh' },
  { id: 'fam-comunicaciones', nombre: 'Comunicaciones y publicaciones', areaReceptoraId: 'dpto-comunicaciones' },
  { id: 'fam-contrataciones', nombre: 'Contrataciones', areaReceptoraId: 'dpto-compras' },
];

/**
 * Catálogo maestro depurado (ajuste 8). Cada rubro trae los códigos del catálogo
 * maestro (ajuste 9: presupuestario, UNSPSC de Compras, inventario) y `ejecutor`
 * dice quién contrata/ejecuta el gasto —sin efecto sobre los techos, que ahora
 * son por rubro institucional (ajuste 10)—.
 */
interface RubroOpts {
  codigoPresupuestario: string;
  codigoUnspsc?: string;
  codigoInventario?: string;
  unidadMedidaSugerida: string;
  camposRequeridos: string[];
  evidencias: string[];
  anticipacionDias: number;
  fusionable: boolean;
  generaCompra: boolean;
  llevaCosto: boolean;
  ejecutor: EjecutorGasto;
  habilitadoInstitucional?: boolean;
}

function rubro(id: string, familiaId: string, nombre: string, o: RubroOpts): RubroCatalogo {
  return {
    id,
    familiaId,
    nombre,
    codigoPresupuestario: o.codigoPresupuestario,
    codigoUnspsc: o.codigoUnspsc,
    codigoInventario: o.codigoInventario,
    unidadMedidaSugerida: o.unidadMedidaSugerida,
    camposRequeridos: o.camposRequeridos,
    evidencias: o.evidencias,
    anticipacionDias: o.anticipacionDias,
    fusionable: o.fusionable,
    generaCompra: o.generaCompra,
    habilitadoInstitucional: o.habilitadoInstitucional ?? true,
    llevaCosto: o.llevaCosto,
    ejecutor: o.ejecutor,
  };
}

export const RUBROS_CATALOGO: RubroCatalogo[] = [
  // --- Transporte y combustible (Administrativa) ---------------------------
  rubro('rub-vehiculos', 'fam-transporte', 'Vehículo', {
    codigoPresupuestario: '2.6.1 — Equipo de transporte',
    codigoUnspsc: '25101500',
    codigoInventario: 'INV-TRA-VEH',
    unidadMedidaSugerida: 'Unidades',
    camposRequeridos: ['Tipo de vehículo', 'Uso previsto', 'Justificación técnica'],
    evidencias: ['Ficha técnica', 'Cotización referencial'],
    anticipacionDias: 90, fusionable: true, generaCompra: true, llevaCosto: true, ejecutor: 'requiriente',
  }),
  rubro('rub-combustible', 'fam-transporte', 'Combustible', {
    codigoPresupuestario: '2.3.1 — Combustibles y lubricantes',
    codigoUnspsc: '15101500',
    unidadMedidaSugerida: 'Galones',
    camposRequeridos: ['Distribución mensual', 'Vehículos asignados'],
    evidencias: ['Bitácora de ruta'],
    anticipacionDias: 30, fusionable: true, generaCompra: true, llevaCosto: true, ejecutor: 'consolidadora',
  }),
  rubro('rub-transporte-personal', 'fam-transporte', 'Transporte de personal', {
    codigoPresupuestario: '2.2.4 — Servicios de transporte',
    unidadMedidaSugerida: 'Servicios',
    camposRequeridos: ['Ruta', 'Cantidad de pasajeros'],
    evidencias: ['Programación de viajes'],
    // Se resuelve con la flotilla institucional: consume agenda, no presupuesto
    anticipacionDias: 15, fusionable: true, generaCompra: false, llevaCosto: false, ejecutor: 'consolidadora',
  }),
  rubro('rub-mantenimiento-veh', 'fam-transporte', 'Mantenimiento vehicular', {
    // Caso de la demo (ajuste 6): «batería para vehículo» se enruta aquí →
    // Administrativa, mientras que las baterías de respaldo van a TIC.
    codigoPresupuestario: '2.2.7 — Mantenimiento de equipo de transporte',
    codigoUnspsc: '78180100',
    codigoInventario: 'INV-TRA-MAN',
    unidadMedidaSugerida: 'Servicios',
    camposRequeridos: ['Vehículo (placa)', 'Tipo de mantenimiento o repuesto'],
    evidencias: ['Cotización del taller'],
    anticipacionDias: 20, fusionable: true, generaCompra: true, llevaCosto: true, ejecutor: 'consolidadora',
  }),
  // --- Viáticos (Administrativa) -------------------------------------------
  rubro('rub-viaticos', 'fam-viaticos', 'Viáticos y gastos de viaje', {
    codigoPresupuestario: '2.2.5 — Viáticos',
    unidadMedidaSugerida: 'Comisiones',
    camposRequeridos: ['Destino', 'Cantidad de personas', 'Días'],
    evidencias: ['Programación de la comisión'],
    anticipacionDias: 15, fusionable: false, generaCompra: false, llevaCosto: true, ejecutor: 'requiriente',
  }),
  // --- Mobiliario, planta física y suministros (Administrativa) ------------
  rubro('rub-mobiliario', 'fam-mobiliario', 'Mobiliario de oficina', {
    codigoPresupuestario: '2.6.2 — Mobiliario y equipo de oficina',
    codigoUnspsc: '56101500',
    codigoInventario: 'INV-MOB-OFI',
    unidadMedidaSugerida: 'Unidades',
    camposRequeridos: ['Ubicación de destino', 'Cantidad por tipo'],
    evidencias: ['Croquis o foto del área'],
    anticipacionDias: 60, fusionable: true, generaCompra: true, llevaCosto: true, ejecutor: 'requiriente',
  }),
  rubro('rub-climatizacion', 'fam-mobiliario', 'Equipos de climatización', {
    codigoPresupuestario: '2.6.3 — Equipo de planta física',
    codigoUnspsc: '40101700',
    codigoInventario: 'INV-MOB-CLI',
    unidadMedidaSugerida: 'Unidades',
    camposRequeridos: ['Capacidad (BTU)', '¿Sustituye equipo existente?'],
    evidencias: ['Informe técnico de mantenimiento'],
    anticipacionDias: 60, fusionable: false, generaCompra: true, llevaCosto: true, ejecutor: 'requiriente',
  }),
  rubro('rub-papeleria', 'fam-mobiliario', 'Papelería y suministros', {
    codigoPresupuestario: '2.3.3 — Productos de papel y cartón',
    codigoUnspsc: '14111500',
    unidadMedidaSugerida: 'Lotes',
    camposRequeridos: ['Detalle de insumos'],
    evidencias: ['Consumo histórico'],
    anticipacionDias: 20, fusionable: true, generaCompra: true, llevaCosto: true, ejecutor: 'consolidadora',
  }),
  // --- Eventos y protocolo (Protocolo) -------------------------------------
  rubro('rub-montaje', 'fam-eventos', 'Montaje de eventos', {
    codigoPresupuestario: '2.2.8 — Servicios de eventos',
    codigoUnspsc: '80141600',
    unidadMedidaSugerida: 'Jornadas',
    camposRequeridos: ['Fecha tentativa', 'Cantidad de participantes', 'Lugar'],
    evidencias: ['Agenda del evento', 'Lista de participantes'],
    // No lo compra el área requiriente, pero cuesta: lo contrata la consolidadora
    anticipacionDias: 45, fusionable: true, generaCompra: false, llevaCosto: true, ejecutor: 'consolidadora',
  }),
  rubro('rub-audiovisual', 'fam-eventos', 'Audiovisual', {
    codigoPresupuestario: '2.2.8 — Servicios de eventos',
    unidadMedidaSugerida: 'Servicios',
    camposRequeridos: ['Tipo de soporte requerido'],
    evidencias: ['Requerimiento técnico'],
    // Protocolo lo cubre con su propio equipo de sonido y proyección
    anticipacionDias: 30, fusionable: true, generaCompra: false, llevaCosto: false, ejecutor: 'consolidadora',
  }),
  rubro('rub-refrigerio', 'fam-eventos', 'Refrigerio', {
    codigoPresupuestario: '2.2.8 — Servicios de eventos',
    unidadMedidaSugerida: 'Raciones',
    camposRequeridos: ['Cantidad de personas', 'Fecha'],
    evidencias: ['Lista de participantes'],
    anticipacionDias: 15, fusionable: true, generaCompra: false, llevaCosto: true, ejecutor: 'consolidadora',
  }),
  // --- Tecnología (TIC) ----------------------------------------------------
  rubro('rub-computo', 'fam-tic', 'Equipos de cómputo', {
    codigoPresupuestario: '2.6.4 — Equipo de cómputo',
    codigoUnspsc: '43211500',
    codigoInventario: 'INV-TIC-COM',
    unidadMedidaSugerida: 'Unidades',
    camposRequeridos: ['Perfil de uso', 'Especificación mínima'],
    evidencias: ['Aval técnico de TIC'],
    anticipacionDias: 90, fusionable: true, generaCompra: true, llevaCosto: true, ejecutor: 'requiriente',
  }),
  rubro('rub-licencias', 'fam-tic', 'Licencias de software', {
    codigoPresupuestario: '2.2.9 — Servicios informáticos',
    codigoUnspsc: '43230000',
    unidadMedidaSugerida: 'Licencias',
    camposRequeridos: ['Producto', 'Cantidad de usuarios'],
    evidencias: ['Aval técnico de TIC'],
    anticipacionDias: 60, fusionable: true, generaCompra: true, llevaCosto: true, ejecutor: 'consolidadora',
  }),
  rubro('rub-conectividad', 'fam-tic', 'Conectividad', {
    codigoPresupuestario: '2.2.9 — Servicios informáticos',
    unidadMedidaSugerida: 'Enlaces',
    camposRequeridos: ['Localidad', 'Ancho de banda'],
    evidencias: ['Levantamiento de sitio'],
    anticipacionDias: 90, fusionable: false, generaCompra: true, llevaCosto: true, ejecutor: 'consolidadora',
  }),
  rubro('rub-equipamiento-tec', 'fam-tic', 'Equipamiento tecnológico', {
    // Contraparte del caso «batería»: UPS y baterías de respaldo → TIC.
    codigoPresupuestario: '2.6.4 — Equipo de cómputo',
    codigoUnspsc: '39121000',
    codigoInventario: 'INV-TIC-EQU',
    unidadMedidaSugerida: 'Unidades',
    camposRequeridos: ['Equipo a respaldar', 'Especificación (VA / capacidad)'],
    evidencias: ['Aval técnico de TIC'],
    anticipacionDias: 60, fusionable: true, generaCompra: true, llevaCosto: true, ejecutor: 'requiriente',
  }),
  // --- Capacitación y formación (RR. HH.) ----------------------------------
  rubro('rub-capacitacion', 'fam-capacitacion', 'Capacitación y formación', {
    codigoPresupuestario: '2.2.6 — Capacitación',
    codigoUnspsc: '86132000',
    unidadMedidaSugerida: 'Cupos',
    camposRequeridos: ['Tema', 'Cantidad de participantes', 'Modalidad'],
    evidencias: ['Programa del curso'],
    anticipacionDias: 45, fusionable: true, generaCompra: true, llevaCosto: true, ejecutor: 'consolidadora',
  }),
  // --- Comunicaciones y publicaciones (Comunicaciones) ---------------------
  rubro('rub-publicidad', 'fam-comunicaciones', 'Publicidad y difusión', {
    codigoPresupuestario: '2.2.2 — Publicidad y propaganda',
    codigoUnspsc: '82101500',
    unidadMedidaSugerida: 'Campañas',
    camposRequeridos: ['Objetivo de la campaña', 'Medios', 'Vigencia'],
    evidencias: ['Brief de comunicación'],
    anticipacionDias: 45, fusionable: true, generaCompra: true, llevaCosto: true, ejecutor: 'consolidadora',
  }),
  rubro('rub-impresos', 'fam-comunicaciones', 'Impresos y publicaciones', {
    codigoPresupuestario: '2.2.1 — Impresión y encuadernación',
    codigoUnspsc: '82121500',
    unidadMedidaSugerida: 'Unidades',
    camposRequeridos: ['Tipo de pieza', 'Tiraje', 'Especificación'],
    evidencias: ['Arte aprobado'],
    anticipacionDias: 30, fusionable: true, generaCompra: true, llevaCosto: true, ejecutor: 'consolidadora',
  }),
  // --- Contrataciones (Compras) --------------------------------------------
  rubro('rub-bienes', 'fam-contrataciones', 'Bienes programables', {
    codigoPresupuestario: '2.3 — Materiales y suministros',
    unidadMedidaSugerida: 'Unidades',
    camposRequeridos: ['Especificación técnica'],
    evidencias: ['Ficha técnica'],
    anticipacionDias: 90, fusionable: true, generaCompra: true, llevaCosto: true, ejecutor: 'requiriente',
  }),
  rubro('rub-servicios', 'fam-contrataciones', 'Servicios contratables', {
    codigoPresupuestario: '2.2 — Servicios no personales',
    unidadMedidaSugerida: 'Contratos',
    camposRequeridos: ['Alcance del servicio', 'Duración'],
    evidencias: ['Términos de referencia'],
    anticipacionDias: 90, fusionable: false, generaCompra: true, llevaCosto: true, ejecutor: 'requiriente',
  }),
  rubro('rub-asistencia', 'fam-contrataciones', 'Asistencia técnica y consultorías', {
    codigoPresupuestario: '2.2.9 — Consultorías',
    codigoUnspsc: '80101500',
    unidadMedidaSugerida: 'Contratos',
    camposRequeridos: ['Objeto de la consultoría', 'Productos esperados'],
    evidencias: ['Términos de referencia'],
    anticipacionDias: 90, fusionable: false, generaCompra: true, llevaCosto: true, ejecutor: 'requiriente',
  }),
];

const art = (
  id: string,
  rubroId: string,
  nombre: string,
  unidadMedida: string,
  precioReferencia: number,
  extra: { codigoUnspsc?: string; habilitadoInstitucional?: boolean } = {},
): ArticuloServicio => ({
  id,
  rubroId,
  nombre,
  unidadMedida,
  precioReferencia,
  codigoUnspsc: extra.codigoUnspsc,
  habilitadoInstitucional: extra.habilitadoInstitucional ?? true,
});

export const ARTICULOS_SERVICIO: ArticuloServicio[] = [
  art('art-1', 'rub-vehiculos', 'Adquisición de vehículo', 'Unidades', 1_450_000, { codigoUnspsc: '25101500' }),
  art('art-2', 'rub-vehiculos', 'Alquiler de vehículo', 'Meses', 45_000),
  art('art-3', 'rub-mantenimiento-veh', 'Mantenimiento de vehículo', 'Servicios', 18_000),
  // Caso de la demo: la batería para vehículo la consolida Administrativa
  art('art-26', 'rub-mantenimiento-veh', 'Batería para vehículo', 'Unidades', 9_800, { codigoUnspsc: '26111700' }),
  art('art-4', 'rub-combustible', 'Gasoil', 'Galones', 600),
  art('art-5', 'rub-combustible', 'Gasolina premium', 'Galones', 680),
  art('art-6', 'rub-transporte-personal', 'Autobús para jornada territorial', 'Servicios', 32_000),
  art('art-27', 'rub-viaticos', 'Viático nacional (día/persona)', 'Comisiones', 3_500),
  art('art-7', 'rub-mobiliario', 'Escritorio ejecutivo', 'Unidades', 24_000),
  art('art-8', 'rub-mobiliario', 'Silla ergonómica', 'Unidades', 12_500),
  art('art-9', 'rub-mobiliario', 'Estantería metálica de archivo', 'Unidades', 21_500),
  art('art-10', 'rub-climatizacion', 'Acondicionador de aire 24,000 BTU', 'Unidades', 145_000),
  art('art-28', 'rub-papeleria', 'Papelería institucional', 'Resmas', 950),
  art('art-29', 'rub-papeleria', 'Insumos de limpieza', 'Lotes', 15_000),
  art('art-11', 'rub-montaje', 'Salón con montaje y refrigerio', 'Jornadas', 29_000),
  art('art-12', 'rub-montaje', 'Logística de acto institucional', 'Eventos', 85_000),
  art('art-13', 'rub-audiovisual', 'Sonido y proyección', 'Servicios', 40_000),
  art('art-14', 'rub-audiovisual', 'Grabación y edición', 'Servicios', 35_000),
  art('art-15', 'rub-computo', 'Estación de trabajo', 'Unidades', 65_000, { codigoUnspsc: '43211507' }),
  art('art-16', 'rub-computo', 'Tableta de campo con conectividad', 'Unidades', 31_200),
  art('art-17', 'rub-computo', 'Escáner de producción', 'Unidades', 48_000),
  // Contraparte del caso «batería»: la batería de respaldo la consolida TIC
  art('art-30', 'rub-equipamiento-tec', 'Batería de respaldo / UPS 1500 VA', 'Unidades', 14_500, { codigoUnspsc: '39121011' }),
  art('art-31', 'rub-equipamiento-tec', 'Banco de baterías para servidor', 'Unidades', 62_000),
  art('art-18', 'rub-licencias', 'Suite ofimática (anual)', 'Licencias', 9_500),
  art('art-19', 'rub-licencias', 'Antivirus corporativo', 'Licencias', 3_200),
  art('art-20', 'rub-conectividad', 'Enlace dedicado de oficina regional', 'Enlaces', 28_000),
  art('art-32', 'rub-capacitacion', 'Diplomado en gestión pública', 'Cupos', 18_000),
  art('art-33', 'rub-capacitacion', 'Taller de competencias (jornada)', 'Cupos', 4_200),
  art('art-34', 'rub-publicidad', 'Campaña de difusión institucional', 'Campañas', 320_000),
  art('art-35', 'rub-impresos', 'Brochure institucional', 'Unidades', 85),
  art('art-36', 'rub-impresos', 'Memoria anual impresa', 'Unidades', 480),
  art('art-21', 'rub-bienes', 'Papelería institucional', 'Resmas', 950),
  art('art-22', 'rub-bienes', 'Insumos de limpieza', 'Lotes', 15_000),
  art('art-23', 'rub-servicios', 'Suscripción a base de datos jurídica', 'Contratos', 220_000),
  art('art-24', 'rub-servicios', 'Servicio de vigilancia', 'Contratos', 620_000),
  art('art-25', 'rub-asistencia', 'Consultoría de procesos', 'Contratos', 480_000),
];

// ---------------------------------------------------------------------------
// Utilidades compartidas
// ---------------------------------------------------------------------------

export const MUNICIPIOS = [
  'Distrito Nacional',
  'Santo Domingo Este',
  'Santo Domingo Norte',
  'Santiago de los Caballeros',
  'La Vega',
  'San Cristóbal',
  'Azua de Compostela',
  'Puerto Plata',
  'Higüey',
  'San Juan de la Maguana',
];

export const PROVINCIAS = [
  'Distrito Nacional',
  'Santo Domingo',
  'Santiago',
  'La Vega',
  'San Cristóbal',
  'Azua',
  'Puerto Plata',
  'La Altagracia',
  'San Juan',
];

export const FUENTES_FINANCIAMIENTO = ['Presupuesto nacional', 'Fondos propios', 'Cooperación'];

export const MESES_CORTOS = ['E', 'F', 'M', 'A', 'M', 'J', 'J', 'A', 'S', 'O', 'N', 'D'];

/** Nombres de mes para explicar el cronograma de una tarea fuera del grid. */
export const MESES_LARGOS = [
  'enero',
  'febrero',
  'marzo',
  'abril',
  'mayo',
  'junio',
  'julio',
  'agosto',
  'septiembre',
  'octubre',
  'noviembre',
  'diciembre',
];

export function formatRD(monto: number): string {
  return 'RD$ ' + monto.toLocaleString('es-DO');
}
