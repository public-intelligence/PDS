import { Component, input } from '@angular/core';

export interface GroupedHeader {
  label: string;
  numeric?: boolean;
}

export interface GroupedBlock {
  label: string;
  rows: (string | null)[][];
  subtotal: (string | null)[];
}

/**
 * Renderizador genérico de reportes agrupados con subtotales y total general. El TableComponent base
 * no soporta agrupación/subtotales, por lo que este componente es dedicado (y el TableComponent
 * permanece intacto). Las celdas llegan ya formateadas por la pantalla.
 */
@Component({
  selector: 'app-grouped-report-table',
  template: `
    @if (isLoading()) {
      <div class="flex items-center justify-center py-10 text-sm text-slate-400">Cargando…</div>
    } @else if (!groups().length) {
      <div class="flex items-center justify-center py-10 text-sm text-slate-400">{{ noDataDescription() }}</div>
    } @else {
      <table class="w-full border-collapse text-sm">
        <thead>
          <tr class="bg-primary-blue text-white">
            @for (h of headers(); track h.label) {
              <th class="px-3 py-2 font-semibold" [class.text-right]="h.numeric" [class.text-left]="!h.numeric">
                {{ h.label }}
              </th>
            }
          </tr>
        </thead>
        <tbody>
          @for (group of groups(); track group.label) {
            <tr class="bg-slate-50">
              <td class="px-3 py-1.5 font-semibold text-slate-700" [attr.colspan]="headers().length">
                {{ group.label }}
              </td>
            </tr>
            @for (row of group.rows; track $index) {
              <tr class="border-t border-slate-100">
                @for (cell of row; track $index) {
                  <td class="px-3 py-1.5 text-slate-700" [class.text-right]="headers()[$index]?.numeric">
                    {{ cell }}
                  </td>
                }
              </tr>
            }
            <tr class="border-t border-slate-200 font-medium text-slate-800">
              @for (cell of group.subtotal; track $index) {
                <td class="px-3 py-1.5" [class.text-right]="headers()[$index]?.numeric">{{ cell }}</td>
              }
            </tr>
          }
          <tr class="border-t-2 border-primary-blue/30 bg-blue-50 font-bold text-slate-900">
            @for (cell of grandTotal(); track $index) {
              <td class="px-3 py-2" [class.text-right]="headers()[$index]?.numeric">{{ cell }}</td>
            }
          </tr>
        </tbody>
      </table>
    }
  `,
})
export class GroupedReportTableComponent {
  readonly headers = input<GroupedHeader[]>([]);
  readonly groups = input<GroupedBlock[]>([]);
  readonly grandTotal = input<(string | null)[]>([]);
  readonly isLoading = input(false);
  readonly noDataDescription = input('No hay datos para mostrar para el contexto seleccionado.');
}
