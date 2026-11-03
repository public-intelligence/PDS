import { Injectable } from '@angular/core';
import pdfMake from 'pdfmake/build/pdfmake';
import pdfFonts from 'pdfmake/build/vfs_fonts';
import { Content, StyleDictionary, TDocumentDefinitions } from 'pdfmake/interfaces';

(pdfMake as unknown as { vfs: unknown }).vfs = (pdfFonts as unknown as { vfs: unknown }).vfs;

@Injectable({
  providedIn: 'root',
})
export class PdfService {

  readonly baseStyles: StyleDictionary = {
    documentTitle: { fontSize: 12, bold: true, alignment: 'center' },
    sectionTitle: { fontSize: 10, bold: true, margin: [0, 14, 0, 4] },
    subSectionTitle: { fontSize: 10, bold: true, margin: [0, 8, 0, 4] },
    body: { fontSize: 10, alignment: 'justify', lineHeight: 1.4 },
  };

  download(docDefinition: TDocumentDefinitions, filename: string): void {
    pdfMake.createPdf(docDefinition).download(`${filename}.pdf`);
  }

  open(docDefinition: TDocumentDefinitions): void {
    pdfMake.createPdf(docDefinition).open();
  }

  line(width = 475, lineWidth = 0.5, margin?: [number, number, number, number]): Content {
    return { canvas: [{ type: 'line', x1: 0, y1: 0, x2: width, y2: 0, lineWidth }], margin };
  }

  splitItems(value?: string): string[] {
    return (value ?? '').split('\x1F').map((s) => s.trim()).filter(Boolean);
  }
}
