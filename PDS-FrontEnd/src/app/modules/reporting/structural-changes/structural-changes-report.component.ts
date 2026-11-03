import { Component, OnInit, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { LayoutService } from '@public-intelligence/ui-core';
import { ReportExportControlComponent } from '../../../components/reporting/report-export-control/report-export-control.component';
import { ReportingService } from '../../../services/reporting/reporting.service';
import { ReportDownloadService } from '../../../services/reporting/report-download.service';
import { NotificationService } from '../../../services/notification/notification.service';
import { StructuralChangesFilter, StructuralChangesReport } from '../../../models/reporting/reporting-reports.model';
import { ExportFormat } from '../../../models/reporting/reporting-context.model';
import { LEGAL_INSTRUMENT_TYPE_LABELS, VERSION_STATUS_LABELS } from '../../../models/structure-version.model';
import { num, signed } from '../../../utils/reporting-format';

// RF-RE-05: cambios estructurales aprobados en un período (trazabilidad del flujo de aprobación).
@Component({
  selector: 'app-structural-changes-report',
  imports: [ReportExportControlComponent],
  template: `
    <div class="flex flex-col gap-4 p-6">
      <div class="flex flex-wrap items-center justify-between gap-4">
        <h1 class="text-xl font-semibold text-primary-blue">Cambios estructurales aprobados</h1>
        <app-report-export-control [disabled]="!report()" [busy]="exporting()" (export)="onExport($event)" />
      </div>

      <div class="flex flex-wrap items-end gap-3 rounded-xl border border-slate-100 bg-white p-4 shadow-sm">
        <label class="flex flex-col gap-1 text-xs text-slate-600">
          <span>Desde</span>
          <input type="date" class="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-blue-500"
                 [value]="from()" (change)="onFromChange($any($event.target).value)" />
        </label>
        <label class="flex flex-col gap-1 text-xs text-slate-600">
          <span>Hasta</span>
          <input type="date" class="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-blue-500"
                 [value]="to()" (change)="onToChange($any($event.target).value)" />
        </label>
        <label class="flex flex-col gap-1 text-xs text-slate-600">
          <span>Tipo de cambio</span>
          <select class="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-blue-500"
                  [value]="changeType()" (change)="onChangeTypeChange($any($event.target).value)">
            <option value="">Todos</option>
            <option value="Created">Unidades creadas</option>
            <option value="Modified">Unidades modificadas</option>
            <option value="Relocated">Unidades reubicadas</option>
            <option value="Deleted">Unidades eliminadas</option>
          </select>
        </label>
      </div>

      <div class="overflow-auto rounded-xl border border-slate-100 bg-white p-2 shadow-sm">
        @if (loading()) {
          <div class="py-10 text-center text-sm text-slate-400">Cargando…</div>
        } @else if (report(); as r) {
          @if (r.entries.length) {
            <table class="w-full border-collapse text-sm">
              <thead><tr class="bg-primary-blue text-white">
                <th class="px-3 py-2 text-left font-semibold">Versión</th>
                <th class="px-3 py-2 text-left font-semibold">Estado</th>
                <th class="px-3 py-2 text-left font-semibold">Vigencia desde</th>
                <th class="px-3 py-2 text-left font-semibold">Instrumento legal</th>
                <th class="px-3 py-2 text-left font-semibold">Aprobó</th>
                <th class="px-3 py-2 text-left font-semibold">Publicó</th>
                <th class="px-3 py-2 text-left font-semibold">Unidades (C/M/R/E)</th>
                <th class="px-3 py-2 text-left font-semibold">Cargos (+/−/Δ)</th>
              </tr></thead>
              <tbody>
                @for (e of r.entries; track e.versionId) {
                  <tr class="border-t border-slate-100">
                    <td class="px-3 py-1.5">{{ e.number != null ? 'v.' + e.number : '—' }} · {{ e.name }}</td>
                    <td class="px-3 py-1.5">{{ statusLabel(e.status) }}</td>
                    <td class="px-3 py-1.5">{{ e.effectiveFrom || '—' }}</td>
                    <td class="px-3 py-1.5">{{ legalLabel(e) }}</td>
                    <td class="px-3 py-1.5">{{ e.approvedByName || '—' }}</td>
                    <td class="px-3 py-1.5">{{ e.publishedByName || '—' }}</td>
                    <td class="px-3 py-1.5">{{ unitsCell(e) }}</td>
                    <td class="px-3 py-1.5">{{ positionsCell(e) }}</td>
                  </tr>
                }
              </tbody>
            </table>
          } @else {
            <div class="py-10 text-center text-sm text-slate-400">No hay cambios aprobados en el período.</div>
          }
        } @else {
          <div class="py-10 text-center text-sm text-slate-400">No hay cambios aprobados en el período.</div>
        }
      </div>

      <p class="text-xs italic text-slate-400">
        Cada versión publicada en el período es un cambio estructural aprobado. Ocupación fuera de alcance por diseño.
      </p>
    </div>
  `,
})
export class StructuralChangesReportComponent implements OnInit {
  private reporting = inject(ReportingService);
  private downloader = inject(ReportDownloadService);
  private layoutService = inject(LayoutService);
  private notification = inject(NotificationService);

  protected loading = signal(true);
  protected exporting = signal(false);
  protected report = signal<StructuralChangesReport | null>(null);

  protected from = signal(this.defaultFrom());
  protected to = signal(this.today());
  protected changeType = signal('');

  ngOnInit(): void {
    this.layoutService.setPageTitle('Cambios estructurales');
    this.layoutService.breadcrumbs.set([
      { label: 'Reportes', url: '/reportes' },
      { label: 'Cambios estructurales', url: '/reportes/cambios-estructurales' },
    ]);
    this.load();
  }

  protected onFromChange(value: string): void { this.from.set(value); this.load(); }
  protected onToChange(value: string): void { this.to.set(value); this.load(); }
  protected onChangeTypeChange(value: string): void { this.changeType.set(value); this.load(); }

  private filter(): StructuralChangesFilter {
    return {
      from: this.from(),
      to: this.to(),
      changeType: this.changeType() || undefined,
    };
  }

  private async load(): Promise<void> {
    if (!this.from() || !this.to()) return;
    this.loading.set(true);
    try {
      const data = await firstValueFrom(this.reporting.structuralChanges(this.filter()));
      this.report.set(data);
    } catch (e) {
      this.report.set(null);
      this.notification.fromHttpError(e, 'No se pudo cargar el reporte de cambios.');
    } finally {
      this.loading.set(false);
    }
  }

  protected async onExport(format: ExportFormat): Promise<void> {
    this.exporting.set(true);
    await this.downloader.download(
      this.reporting.exportStructuralChanges(this.filter(), format),
      `cambios-estructurales.${format}`,
    );
    this.exporting.set(false);
  }

  protected statusLabel(status: StructuralChangesReport['entries'][number]['status']): string {
    return VERSION_STATUS_LABELS[status];
  }

  protected legalLabel(e: StructuralChangesReport['entries'][number]): string {
    return e.legalInstrument ? `${LEGAL_INSTRUMENT_TYPE_LABELS[e.legalInstrument.type]} ${e.legalInstrument.number}` : '—';
  }

  protected unitsCell(e: StructuralChangesReport['entries'][number]): string {
    const u = e.unitChangeSummary;
    return u ? `${u.createdCount}/${u.modifiedCount}/${u.relocatedCount}/${u.deletedCount}` : '—';
  }

  protected positionsCell(e: StructuralChangesReport['entries'][number]): string {
    const p = e.positionDeltaSummary;
    return p ? `+${num(p.addedCount)}/−${num(p.removedCount)}/${signed(p.authorizedPostsDelta)}` : '—';
  }

  private today(): string {
    return new Date().toISOString().slice(0, 10);
  }

  private defaultFrom(): string {
    const d = new Date();
    d.setFullYear(d.getFullYear() - 1);
    return d.toISOString().slice(0, 10);
  }
}
