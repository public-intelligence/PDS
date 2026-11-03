import { inject, Injectable } from '@angular/core';
import { Content, TDocumentDefinitions } from 'pdfmake/interfaces';
import { Position } from '../../models/position.model';
import { PdfService } from './pdf.service';

const PAGE_WIDTH = 451; // A4 width (595) minus 2 × 72pt margins
const FONT = 11;

@Injectable({
  providedIn: 'root',
})
export class PositionPdfBuilder {

  private pdf = inject(PdfService);

  build(position: Position): TDocumentDefinitions {
    return {
      pageSize: 'A4',
      pageMargins: [72, 72, 72, 72],
      defaultStyle: { fontSize: FONT, lineHeight: 1.3 },
      content: [
        ...this.header(),
        ...this.titleAndCode(position),
        ...this.section('3.  Naturaleza del trabajo:', position.natureOfWork),
        ...this.listSection('4. Tareas Típicas:', position.typicalTasks),
        ...this.minimumRequirements(position),
        ...this.listSection('6. Características de la clase de cargo:', position.characteristics),
        ...this.section('7.  Período probatorio:', position.probatoryPeriod),
        ...this.scoreAndGrade(position),
      ],
    };
  }

  private header(): Content[] {
    return [
      this.pdf.line(PAGE_WIDTH, 1.5),
      {
        text: 'MANUAL DE DESCRIPCION DE PUESTOS',
        bold: true,
        fontSize: FONT,
        alignment: 'center',
        margin: [0, 5, 0, 5],
      },
      this.pdf.line(PAGE_WIDTH, 0.5),
      this.br(),
    ];
  }

  private titleAndCode(position: Position): Content[] {
    return [
      {
        columns: [
          {
            width: '65%',
            stack: [
              { text: '1.  Título:', bold: true },
              this.br(4),
              { text: position.name, alignment: 'justify' },
            ],
          },
          {
            width: '35%',
            stack: [
              { text: '2. Código:', bold: true },
              this.br(4),
              { text: position.code ?? '—' },
            ],
          },
        ],
      },
      this.br(),
    ];
  }

  private section(title: string, value?: string): Content[] {
    if (!value) return [];
    return [
      { text: title, bold: true },
      this.br(4),
      { text: value, alignment: 'justify' },
      this.br(),
    ];
  }

  private listSection(title: string, value?: string): Content[] {
    const items = this.pdf.splitItems(value);
    if (!items.length) return [];
    return [
      { text: title, bold: true },
      {
        ul: items,
        alignment: 'justify',
        margin: [0, 2, 0, 0],
      },
      this.br(),
    ];
  }

  private minimumRequirements(position: Position): Content[] {
    const sections: Content[] = [
      { text: '5. Requisitos Mínimos Exigidos:', bold: true },
      this.br(),
      { text: '5.1 Educación y Experiencia:', bold: true },
      this.br(4),
      {
        text: [
          position.requiredEducation ?? '—',
          position.yearsOfExperienceRequired
            ? ` más ${position.yearsOfExperienceRequired} años de experiencia en labores similares`
            : '',
        ],
        alignment: 'justify',
      },
      this.br(),
    ];

    const competencies = this.pdf.splitItems(position.competencies);
    if (competencies.length) {
      sections.push(
        { text: '5.2 Conocimientos, Habilidades y Destrezas Requeridas:', bold: true },
        { ul: competencies, alignment: 'justify', margin: [0, 2, 0, 0] },
        this.br(),
      );
    }

    const otherReqs = this.pdf.splitItems(position.otherDesirableRequirements);
    if (otherReqs.length) {
      sections.push(
        { text: '5.3 Otros Requisitos Deseables:', bold: true },
        { ul: otherReqs, alignment: 'justify', margin: [0, 2, 0, 0] },
        this.br(),
      );
    }

    return sections;
  }

  private scoreAndGrade(position: Position): Content[] {
    return [
      {
        columns: [
          {
            width: '50%',
            stack: [
              { text: '8. Puntuación:', bold: true },
              this.br(4),
              { text: position.punctuation ?? '—' },
            ],
          },
          {
            width: '50%',
            stack: [
              { text: '9.Grado:', bold: true },
              this.br(4),
              { text: position.grade ?? '—' },
            ],
          },
        ],
      },
    ];
  }

  private br(size = FONT): Content {
    return { text: ' ', fontSize: size, margin: [0, 0, 0, 0] };
  }
}
