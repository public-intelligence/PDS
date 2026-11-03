import { Component, input, output } from '@angular/core';
import { ExportFormat } from '../../../models/reporting/reporting-context.model';

/** Control de exportación PDF/Excel/CSV (RF-RE-06). Emite el formato; la pantalla hace la descarga. */
@Component({
  selector: 'app-report-export-control',
  template: `
    <div class="flex items-center gap-2">
      <span class="text-xs text-slate-500">Exportar:</span>
      @for (f of formats; track f.value) {
        <button
          type="button"
          class="!rounded-lg !bg-white !px-3 !py-1.5 !text-xs !font-semibold !text-primary-blue ring-1 ring-primary-blue/30 hover:!bg-blue-50 disabled:opacity-50"
          [disabled]="disabled() || busy()"
          (click)="export.emit(f.value)"
        >
          {{ f.label }}
        </button>
      }
      @if (busy()) {
        <span class="text-xs text-slate-400">Generando…</span>
      }
    </div>
  `,
})
export class ReportExportControlComponent {
  readonly disabled = input(false);
  readonly busy = input(false);
  readonly export = output<ExportFormat>();

  protected readonly formats: { value: ExportFormat; label: string }[] = [
    { value: 'pdf', label: 'PDF' },
    { value: 'xlsx', label: 'Excel' },
    { value: 'csv', label: 'CSV' },
  ];
}
