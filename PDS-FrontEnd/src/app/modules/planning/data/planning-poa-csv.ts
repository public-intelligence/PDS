/**
 * Plantilla del POA en CSV (UTF-8 con BOM y separador `;`): Excel la abre y la
 * guarda con doble clic, sin depender de una librería .xlsx.
 *
 * Formato plano de una fila por UNIDAD DE MEDIDA, que es como se trabaja el
 * formato oficial de 13 columnas en hoja de cálculo: las filas del mismo
 * `Código` se agrupan en un solo producto. Las listas dentro de una celda
 * (municipios, áreas involucradas, tareas) se separan con `|`.
 *
 * Los catálogos se escriben con NOMBRES, no con ids, para que el archivo sea
 * legible y editable por las áreas; la importación los resuelve contra el PEI,
 * el organigrama vigente y el catálogo territorial, y reporta fila por fila lo
 * que no resuelve.
 */
import {
  Area,
  ColumnaConfig,
  Pei,
  Plan,
  ProductoPoa,
  TareaPoa,
  TipoMedicion,
  UnidadMedidaPoa,
} from './planning.models';

const SEP = ';';
/** Separador de listas dentro de una celda. */
const SEP_LISTA = '|';
const EOL = '\r\n';

export interface CtxCsv {
  pei: Pei;
  areas: Area[];
  /** Columnas configurables visibles del ciclo (se anexan al final). */
  columnas: ColumnaConfig[];
  municipiosValidos: string[];
}

export interface ErrorCsv {
  /** Número de fila tal como se ve en Excel (1 = encabezados). */
  fila: number;
  mensaje: string;
}

export interface ResultadoImport {
  productos: ProductoPoa[];
  errores: ErrorCsv[];
  avisos: string[];
}

const COLUMNAS_BASE = [
  'Código',
  'Origen del producto',
  'Foco estratégico',
  'Objetivo estratégico',
  'Producto estratégico (PEI)',
  'Producto',
  'Justificación operativa',
  'Unidad de medida',
  'Meta anual',
  'Tipo de medición',
  'T1',
  'T2',
  'T3',
  'T4',
  'Medios de verificación',
  'Área responsable',
  'Municipios de impacto',
  'Áreas involucradas',
  'Tareas',
] as const;

/** Encabezados del archivo, con las columnas configurables al final. */
function encabezados(ctx: CtxCsv): string[] {
  return [...COLUMNAS_BASE, ...ctx.columnas.map((c) => c.nombreVisible)];
}

// ---------------------------------------------------------------------------
// Escritura
// ---------------------------------------------------------------------------

function celda(valor: string | number | null | undefined): string {
  const s = valor === null || valor === undefined ? '' : String(valor);
  return /["\r\n;]/.test(s) ? `"${s.replace(/"/g, '""')}"` : s;
}

function linea(valores: (string | number | null | undefined)[]): string {
  return valores.map(celda).join(SEP) + EOL;
}

/**
 * «Ejecutar operativos (meses: 2,3,4)» — meses en base 1 para el usuario. Una
 * tarea continua (ajuste 4) se escribe como «Nombre (continua)».
 */
function tareasACelda(tareas: TareaPoa[]): string {
  return tareas
    .filter((t) => t.nombre.trim())
    .map((t) =>
      t.continua ? `${t.nombre} (continua)` : `${t.nombre} (meses: ${t.meses.map((m) => m + 1).join(',')})`,
    )
    .join(SEP_LISTA);
}

function filasDeProducto(row: ProductoPoa, ctx: CtxCsv): (string | number)[][] {
  const foco = ctx.pei.focos.find((f) => f.id === row.focoId);
  const objetivo = foco?.objetivos.find((o) => o.id === row.objetivoId);
  const productoPei = objetivo?.productos.find((p) => p.id === row.productoEstrategicoId);
  const areas = row.areasInvolucradasIds
    .map((id) => ctx.areas.find((a) => a.id === id)?.nombre ?? id)
    .join(SEP_LISTA);
  const custom = ctx.columnas.map((c) => row.custom[c.nombreTecnico] ?? '');

  const comunes = [
    row.codigo,
    row.origen === 'operativo' ? 'Operativo' : 'PEI',
    foco?.nombre ?? '',
    objetivo?.nombre ?? '',
    productoPei?.nombre ?? '',
    row.nombreProducto,
    row.justificacionOperativa ?? '',
  ];
  const cola = [
    row.municipiosImpacto.join(SEP_LISTA),
    areas,
    tareasACelda(row.tareas),
    ...custom,
  ];

  if (!row.unidades.length) {
    return [[...comunes, '', '', 'absoluto', '', '', '', '', '', '', ...cola]];
  }
  return row.unidades.map((u) => [
    ...comunes,
    u.nombre,
    u.metaValor,
    u.metaTipo,
    u.programacion.T1,
    u.programacion.T2,
    u.programacion.T3,
    u.programacion.T4,
    u.mediosVerificacion,
    ctx.areas.find((a) => a.id === u.responsableAreaId)?.nombre ?? '',
    ...cola,
  ]);
}

/** Contenido descargable del plan completo. */
export function planACsv(plan: Plan, ctx: CtxCsv): string {
  let out = `sep=${SEP}${EOL}`;
  out += linea([...encabezados(ctx)]);
  for (const row of plan.rows) {
    for (const fila of filasDeProducto(row, ctx)) out += linea(fila);
  }
  return out;
}

/** POA institucional: todas las áreas en un solo archivo (ajuste 16). */
export function planesACsv(planes: Plan[], ctx: CtxCsv): string {
  let out = `sep=${SEP}${EOL}`;
  out += linea(['Área', ...encabezados(ctx)]);
  for (const plan of planes) {
    const area = ctx.areas.find((a) => a.id === plan.areaId)?.nombre ?? plan.areaId;
    for (const row of plan.rows) {
      for (const fila of filasDeProducto(row, ctx)) out += linea([area, ...fila]);
    }
  }
  return out;
}

/** Plantilla vacía con una fila de ejemplo tomada del PEI vigente. */
export function plantillaCsv(ctx: CtxCsv): string {
  let out = `sep=${SEP}${EOL}`;
  out += linea([...encabezados(ctx)]);

  const foco = ctx.pei.focos[0];
  const objetivo = foco?.objetivos[0];
  const producto = objetivo?.productos[0];
  const area = ctx.areas[0];
  out += linea([
    'EJ-01',
    'PEI',
    foco?.nombre ?? '',
    objetivo?.nombre ?? '',
    producto?.nombre ?? '',
    'Escriba aquí el nombre del producto',
    '',
    'Nombre de la unidad de medida',
    12,
    'absoluto',
    3,
    3,
    3,
    3,
    'Actas, reportes del sistema, listas de asistencia',
    area?.nombre ?? '',
    ctx.municipiosValidos.slice(0, 2).join(SEP_LISTA),
    ctx.areas[1]?.nombre ?? '',
    `Primera tarea (meses: 1,2)${SEP_LISTA}Tarea permanente (continua)`,
    ...ctx.columnas.map(() => ''),
  ]);
  return out;
}

/**
 * Reporte del PACC en CSV (ajuste 16): una fila por requerimiento con los
 * códigos del catálogo maestro. Las filas ya vienen resueltas a texto.
 */
export interface FilaPaccCsv {
  codigo: string;
  rubro: string;
  codigoPresupuestario: string;
  codigoUnspsc: string;
  requiriente: string;
  consolidadora: string;
  cantidad: number;
  unidad: string;
  precio: number;
  monto: number;
  trimestre: string;
  fuente: string;
  estado: string;
}

const COLUMNAS_PACC = [
  'Código',
  'Rubro',
  'Código presupuestario',
  'Código UNSPSC',
  'Área requiriente',
  'Área consolidadora',
  'Cantidad',
  'Unidad de medida',
  'Precio estimado',
  'Monto total',
  'Trimestre',
  'Fuente de financiamiento',
  'Estado',
] as const;

export function paccACsv(filas: FilaPaccCsv[]): string {
  let out = `sep=${SEP}${EOL}`;
  out += linea([...COLUMNAS_PACC]);
  for (const f of filas) {
    out += linea([
      f.codigo,
      f.rubro,
      f.codigoPresupuestario,
      f.codigoUnspsc,
      f.requiriente,
      f.consolidadora,
      f.cantidad,
      f.unidad,
      f.precio,
      f.monto,
      f.trimestre,
      f.fuente,
      f.estado,
    ]);
  }
  return out;
}

/** Descarga en el navegador con BOM para que Excel respete los acentos. */
export function descargarCsv(nombreArchivo: string, contenido: string): void {
  const blob = new Blob(['﻿' + contenido], { type: 'text/csv;charset=utf-8;' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = nombreArchivo;
  a.click();
  URL.revokeObjectURL(url);
}

// ---------------------------------------------------------------------------
// Lectura
// ---------------------------------------------------------------------------

/** Parser CSV con soporte de comillas, saltos de línea y `,` o `;`. */
export function parsearCsv(texto: string): string[][] {
  let t = texto.replace(/^\uFEFF/, '');
  const primeraLinea = t.slice(0, t.indexOf('\n') === -1 ? t.length : t.indexOf('\n'));
  if (/^sep=/i.test(primeraLinea)) t = t.slice(primeraLinea.length).replace(/^\r?\n/, '');

  const cabecera = t.slice(0, t.indexOf('\n') === -1 ? t.length : t.indexOf('\n'));
  const sep = cabecera.includes(';') ? ';' : ',';

  const filas: string[][] = [];
  let fila: string[] = [];
  let campo = '';
  let enComillas = false;

  for (let i = 0; i < t.length; i++) {
    const c = t[i];
    if (enComillas) {
      if (c === '"') {
        if (t[i + 1] === '"') {
          campo += '"';
          i++;
        } else {
          enComillas = false;
        }
      } else {
        campo += c;
      }
      continue;
    }
    if (c === '"') {
      enComillas = true;
    } else if (c === sep) {
      fila.push(campo);
      campo = '';
    } else if (c === '\n') {
      fila.push(campo.replace(/\r$/, ''));
      filas.push(fila);
      fila = [];
      campo = '';
    } else {
      campo += c;
    }
  }
  if (campo || fila.length) {
    fila.push(campo.replace(/\r$/, ''));
    filas.push(fila);
  }
  return filas.filter((f) => f.some((c) => c.trim()));
}

/** Rango de marcas diacríticas combinantes (U+0300–U+036F) que deja NFD. */
const MARCAS_DIACRITICAS = /[̀-ͯ]/g;

/** Compara nombres de catálogo sin distinguir mayúsculas ni acentos. */
const norm = (s: string): string => s.trim().toLowerCase().normalize('NFD').replace(MARCAS_DIACRITICAS, '');

function lista(contenido: string): string[] {
  return contenido
    .split(SEP_LISTA)
    .map((s) => s.trim())
    .filter(Boolean);
}

/** Inverso de `tareasACelda`. Devuelve las tareas y los meses fuera de rango. */
function celdaATareas(contenido: string, nuevoId: () => string): { tareas: TareaPoa[]; malos: string[] } {
  const malos: string[] = [];
  const tareas = lista(contenido).map((texto) => {
    // «Nombre (continua)» — tarea permanente durante todo el año (ajuste 4).
    const cont = texto.match(/^(.*?)\s*\(\s*continua\s*\)\s*$/i);
    if (cont) {
      return { id: nuevoId(), nombre: cont[1].trim(), meses: [], continua: true };
    }
    const m = texto.match(/^(.*?)\s*\((?:meses:)?\s*([\d,\s]*)\)\s*$/i);
    const nombre = (m ? m[1] : texto).trim();
    const meses = (m?.[2] ?? '')
      .split(',')
      .map((x) => Number(x.trim()))
      .filter((n) => !Number.isNaN(n))
      .map((n) => n - 1);
    if (meses.some((n) => n < 0 || n > 11)) malos.push(nombre);
    return {
      id: nuevoId(),
      nombre,
      meses: [...new Set(meses.filter((n) => n >= 0 && n <= 11))].sort((a, b) => a - b),
      continua: false,
    };
  });
  return { tareas, malos };
}

function numero(valor: string): number {
  const n = Number(String(valor).replace(/[^\d.,-]/g, '').replace(',', '.'));
  return Number.isNaN(n) ? 0 : n;
}

/**
 * Reconstruye los productos del POA desde el archivo. `planActual` permite
 * conservar los ids de productos y tareas que ya existen (mismo código, mismo
 * nombre de tarea), para no romper el enlace con los requerimientos.
 */
export function csvAProductos(texto: string, ctx: CtxCsv, planActual?: Plan): ResultadoImport {
  const errores: ErrorCsv[] = [];
  const avisos: string[] = [];
  const filas = parsearCsv(texto);

  if (filas.length < 2) {
    return { productos: [], errores: [{ fila: 1, mensaje: 'El archivo no tiene filas de datos.' }], avisos };
  }

  const cabecera = filas[0].map((h) => norm(h));
  const esperados = encabezados(ctx).map(norm);
  const faltantes = esperados.filter((h) => !cabecera.includes(h));
  if (faltantes.length) {
    errores.push({
      fila: 1,
      mensaje: `Faltan columnas de la plantilla: ${faltantes.join(', ')}. Descargue la plantilla vigente.`,
    });
    return { productos: [], errores, avisos };
  }
  const col = (nombre: string): number => cabecera.indexOf(norm(nombre));

  let seq = 0;
  const nuevoId = (prefijo: string) => () => `${prefijo}-imp-${++seq}`;

  // Agrupa por código: cada grupo es un producto con N unidades de medida
  const grupos = new Map<string, { filas: string[][]; numeros: number[] }>();
  for (let i = 1; i < filas.length; i++) {
    const f = filas[i];
    const codigo = (f[col('Código')] ?? '').trim();
    if (!codigo) {
      errores.push({ fila: i + 1, mensaje: 'Falta el código del producto.' });
      continue;
    }
    const g = grupos.get(codigo) ?? { filas: [], numeros: [] };
    g.filas.push(f);
    g.numeros.push(i + 1);
    grupos.set(codigo, g);
  }

  const productos: ProductoPoa[] = [];

  for (const [codigo, grupo] of grupos) {
    const primera = grupo.filas[0];
    const nFila = grupo.numeros[0];
    const valor = (nombre: string, f = primera) => (f[col(nombre)] ?? '').trim();
    const previo = planActual?.rows.find((r) => r.codigo === codigo);

    // --- alineación estratégica (nombres → ids del PEI vigente) -------------
    const nombreFoco = valor('Foco estratégico');
    const foco = nombreFoco ? ctx.pei.focos.find((f) => norm(f.nombre) === norm(nombreFoco)) : undefined;
    if (nombreFoco && !foco) {
      errores.push({ fila: nFila, mensaje: `El foco «${nombreFoco}» no existe en el PEI vigente.` });
    }
    const nombreObjetivo = valor('Objetivo estratégico');
    const objetivo = nombreObjetivo
      ? foco?.objetivos.find((o) => norm(o.nombre) === norm(nombreObjetivo))
      : undefined;
    if (nombreObjetivo && !objetivo) {
      errores.push({
        fila: nFila,
        mensaje: `El objetivo «${nombreObjetivo}» no pertenece al foco indicado.`,
      });
    }
    const nombreProdPei = valor('Producto estratégico (PEI)');
    const prodPei = nombreProdPei
      ? objetivo?.productos.find((p) => norm(p.nombre) === norm(nombreProdPei))
      : undefined;
    if (nombreProdPei && !prodPei) {
      errores.push({
        fila: nFila,
        mensaje: `El producto estratégico «${nombreProdPei}» no pertenece al objetivo indicado.`,
      });
    }

    // Origen del producto (ajuste 2): PEI u Operativo. Por compatibilidad, si la
    // celda viene vacía se infiere «Operativo» cuando no hay foco/objetivo.
    const origenTexto = norm(valor('Origen del producto'));
    const origen: 'pei' | 'operativo' =
      origenTexto === 'operativo' ? 'operativo' : origenTexto === 'pei' ? 'pei' : foco && objetivo ? 'pei' : 'operativo';
    const justificacionOperativa = valor('Justificación operativa') || undefined;
    if (origen === 'operativo' && !justificacionOperativa) {
      errores.push({
        fila: nFila,
        mensaje: `El producto operativo ${codigo} debe indicar su justificación.`,
      });
    }

    // --- unidades de medida (una por fila del grupo) ------------------------
    const unidades: UnidadMedidaPoa[] = [];
    grupo.filas.forEach((f, idx) => {
      const nf = grupo.numeros[idx];
      const nombreUnidad = valor('Unidad de medida', f);
      if (!nombreUnidad) return;
      const tipoTexto = norm(valor('Tipo de medición', f)) || 'absoluto';
      if (!['absoluto', 'porcentaje'].includes(tipoTexto)) {
        errores.push({
          fila: nf,
          mensaje: `Tipo de medición «${valor('Tipo de medición', f)}»: use «absoluto» o «porcentaje».`,
        });
      }
      const nombreArea = valor('Área responsable', f);
      const area = nombreArea ? ctx.areas.find((a) => norm(a.nombre) === norm(nombreArea)) : undefined;
      if (nombreArea && !area) {
        errores.push({ fila: nf, mensaje: `El área responsable «${nombreArea}» no existe en el organigrama vigente.` });
      }
      const previaUnidad = previo?.unidades.find((u) => norm(u.nombre) === norm(nombreUnidad));
      unidades.push({
        id: previaUnidad?.id ?? nuevoId('upoa')(),
        nombre: nombreUnidad,
        metaValor: numero(valor('Meta anual', f)),
        metaTipo: (tipoTexto === 'porcentaje' ? 'porcentaje' : 'absoluto') as TipoMedicion,
        mediosVerificacion: valor('Medios de verificación', f),
        responsableAreaId: area?.id ?? '',
        programacion: {
          T1: numero(valor('T1', f)),
          T2: numero(valor('T2', f)),
          T3: numero(valor('T3', f)),
          T4: numero(valor('T4', f)),
        },
      });
    });

    // --- municipios y áreas involucradas ------------------------------------
    const municipios = lista(valor('Municipios de impacto'));
    const municipiosMalos = municipios.filter(
      (m) => !ctx.municipiosValidos.some((v) => norm(v) === norm(m)),
    );
    if (municipiosMalos.length) {
      errores.push({
        fila: nFila,
        mensaje: `Municipios que no están en el catálogo: ${municipiosMalos.join(', ')}.`,
      });
    }
    const municipiosOk = municipios
      .map((m) => ctx.municipiosValidos.find((v) => norm(v) === norm(m)))
      .filter((m): m is string => !!m);

    const nombresAreas = lista(valor('Áreas involucradas'));
    const areasMalas = nombresAreas.filter((n) => !ctx.areas.some((a) => norm(a.nombre) === norm(n)));
    if (areasMalas.length) {
      errores.push({ fila: nFila, mensaje: `Áreas involucradas no reconocidas: ${areasMalas.join(', ')}.` });
    }
    const areasIds = nombresAreas
      .map((n) => ctx.areas.find((a) => norm(a.nombre) === norm(n))?.id)
      .filter((id): id is string => !!id);

    // --- tareas: conservan su id si el nombre no cambió ---------------------
    const { tareas, malos } = celdaATareas(valor('Tareas'), nuevoId('ta'));
    if (malos.length) {
      errores.push({
        fila: nFila,
        mensaje: `Meses fuera de rango (use 1 a 12) en: ${malos.join(', ')}.`,
      });
    }
    const tareasConId = tareas.map((t) => {
      const previa = previo?.tareas.find((x) => norm(x.nombre) === norm(t.nombre));
      return previa ? { ...t, id: previa.id } : t;
    });

    const custom: Record<string, string> = { ...(previo?.custom ?? {}) };
    for (const c of ctx.columnas) custom[c.nombreTecnico] = valor(c.nombreVisible);

    const nombreProducto = valor('Producto');
    if (!nombreProducto) {
      errores.push({ fila: nFila, mensaje: `El producto ${codigo} no tiene nombre.` });
    }
    // Las filas del mismo código son unidades de medida del mismo producto:
    // si el nombre difiere entre ellas, manda el de la primera fila.
    if (grupo.filas.some((f) => valor('Producto', f) && valor('Producto', f) !== nombreProducto)) {
      avisos.push(
        `${codigo}: las filas tienen nombres de producto distintos; se usó «${nombreProducto}» (fila ${nFila}).`,
      );
    }

    productos.push({
      id: previo?.id ?? nuevoId('row')(),
      codigo,
      origen,
      focoId: origen === 'operativo' ? null : foco?.id ?? null,
      objetivoId: origen === 'operativo' ? null : objetivo?.id ?? null,
      productoEstrategicoId: origen === 'operativo' ? null : prodPei?.id ?? null,
      justificacionOperativa,
      nombreProducto,
      unidades,
      municipiosImpacto: municipiosOk,
      tareas: tareasConId,
      areasInvolucradasIds: areasIds,
      observacion: previo?.observacion,
      custom,
    });
  }

  if (!errores.length && !productos.length) {
    errores.push({ fila: 1, mensaje: 'El archivo no contiene productos.' });
  }

  return { productos, errores, avisos };
}
