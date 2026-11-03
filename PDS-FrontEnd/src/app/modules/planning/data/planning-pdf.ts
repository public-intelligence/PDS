/**
 * Construcción de los reportes POA y PACC en PDF (ajuste 16), reusando el
 * `PdfService` institucional (pdfmake). Recibe filas ya resueltas a texto para
 * no acoplarse al store: la pantalla arma los datos y este módulo el documento.
 */
import { Content, TDocumentDefinitions } from 'pdfmake/interfaces';

export interface EncabezadoReporte {
  institucion: string;
  titulo: string;
  ciclo: string;
  version: string;
  /** Línea de sello (usuario, cargo, fecha) o «Pendiente de aprobación». */
  sello: string;
}

/** Fila del POA (una por producto). */
export interface FilaPoaPdf {
  codigo: string;
  origen: string;
  alineacion: string;
  producto: string;
  unidades: string;
  meta: string;
  responsable: string;
  municipios: string;
  tareas: string;
}

/** Fila del PACC (una por requerimiento). */
export interface FilaPaccPdf {
  codigo: string;
  rubro: string;
  codigoPresupuestario: string;
  codigoUnspsc: string;
  requiriente: string;
  consolidadora: string;
  cantidad: string;
  unidad: string;
  precio: string;
  monto: string;
  trimestre: string;
  estado: string;
}

const GRIS = '#f1f5f9';
const AZUL = '#1e3a5f';

function encabezado(enc: EncabezadoReporte): Content[] {
  return [
    { text: enc.institucion, style: 'inst' },
    { text: enc.titulo, style: 'titulo' },
    {
      columns: [
        { text: `Ciclo: ${enc.ciclo}`, style: 'meta' },
        { text: `Versión: ${enc.version}`, style: 'meta', alignment: 'center' as const },
        { text: enc.sello, style: 'meta', alignment: 'right' as const },
      ],
      margin: [0, 2, 0, 8] as [number, number, number, number],
    },
  ];
}

function th(text: string): Content {
  return { text, style: 'th' };
}

export function construirPoaPdf(enc: EncabezadoReporte, filas: FilaPoaPdf[]): TDocumentDefinitions {
  const body: Content[][] = [
    [th('Código'), th('Origen'), th('Alineación / Producto'), th('Unidad de medida'), th('Meta'), th('Responsable'), th('Municipios'), th('Tareas')],
    ...filas.map((f) => [
      { text: f.codigo, style: 'td' },
      { text: f.origen, style: 'td' },
      { text: `${f.alineacion}\n${f.producto}`, style: 'td' },
      { text: f.unidades, style: 'td' },
      { text: f.meta, style: 'td', alignment: 'right' as const },
      { text: f.responsable, style: 'td' },
      { text: f.municipios, style: 'td' },
      { text: f.tareas, style: 'td' },
    ]),
  ];
  return {
    pageOrientation: 'landscape',
    pageSize: 'A4',
    pageMargins: [24, 24, 24, 32],
    content: [
      ...encabezado(enc),
      {
        table: { headerRows: 1, widths: [40, 44, '*', 90, 34, 70, 80, 110], body },
        layout: {
          fillColor: (rowIndex: number) => (rowIndex === 0 ? AZUL : rowIndex % 2 === 0 ? GRIS : null),
          hLineColor: () => '#cbd5e1',
          vLineColor: () => '#cbd5e1',
        },
      },
      { text: `${filas.length} producto(s) · Generado desde el sistema de planificación (mockup).`, style: 'pie', margin: [0, 8, 0, 0] },
    ],
    styles: estilos(),
    defaultStyle: { fontSize: 8 },
  };
}

export function construirPaccPdf(enc: EncabezadoReporte, filas: FilaPaccPdf[]): TDocumentDefinitions {
  const body: Content[][] = [
    [th('Código'), th('Rubro'), th('Cód. presup.'), th('UNSPSC'), th('Requiriente'), th('Consolidadora'), th('Cant.'), th('Unidad'), th('Precio'), th('Monto'), th('Trim.'), th('Estado')],
    ...filas.map((f) => [
      { text: f.codigo, style: 'td' },
      { text: f.rubro, style: 'td' },
      { text: f.codigoPresupuestario, style: 'td' },
      { text: f.codigoUnspsc, style: 'td' },
      { text: f.requiriente, style: 'td' },
      { text: f.consolidadora, style: 'td' },
      { text: f.cantidad, style: 'td', alignment: 'right' as const },
      { text: f.unidad, style: 'td' },
      { text: f.precio, style: 'td', alignment: 'right' as const },
      { text: f.monto, style: 'td', alignment: 'right' as const },
      { text: f.trimestre, style: 'td' },
      { text: f.estado, style: 'td' },
    ]),
  ];
  return {
    pageOrientation: 'landscape',
    pageSize: 'A4',
    pageMargins: [20, 24, 20, 32],
    content: [
      ...encabezado(enc),
      {
        table: { headerRows: 1, widths: [46, 70, 58, 42, 62, 62, 26, 44, 46, 52, 24, 46], body },
        layout: {
          fillColor: (rowIndex: number) => (rowIndex === 0 ? AZUL : rowIndex % 2 === 0 ? GRIS : null),
          hLineColor: () => '#cbd5e1',
          vLineColor: () => '#cbd5e1',
        },
      },
      { text: `${filas.length} requerimiento(s) · Generado desde el sistema de planificación (mockup).`, style: 'pie', margin: [0, 8, 0, 0] },
    ],
    styles: estilos(),
    defaultStyle: { fontSize: 7.5 },
  };
}

function estilos() {
  return {
    inst: { fontSize: 9, bold: true, color: AZUL },
    titulo: { fontSize: 14, bold: true, margin: [0, 2, 0, 2] as [number, number, number, number] },
    meta: { fontSize: 8, color: '#475569' },
    th: { fontSize: 8, bold: true, color: '#ffffff', margin: [1, 2, 1, 2] as [number, number, number, number] },
    td: { fontSize: 7.5, margin: [1, 1, 1, 1] as [number, number, number, number] },
    pie: { fontSize: 7, italics: true, color: '#64748b' },
  };
}
