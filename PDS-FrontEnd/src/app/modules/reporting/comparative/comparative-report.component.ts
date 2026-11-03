import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { LayoutService } from '@public-intelligence/ui-core';
import { ReportingContextSelectorComponent } from '../../../components/reporting/reporting-context-selector/reporting-context-selector.component';
import { ReportExportControlComponent } from '../../../components/reporting/report-export-control/report-export-control.component';
import { ReportingService, CompareOptions } from '../../../services/reporting/reporting.service';
import { ReportDownloadService } from '../../../services/reporting/report-download.service';
import { NotificationService } from '../../../services/notification/notification.service';
import { ComparativeReport, SALARY_BASIS_LABELS, SalaryBasis } from '../../../models/reporting/reporting-reports.model';
import { ExportFormat, ReportingContext } from '../../../models/reporting/reporting-context.model';
import { money, num, signed } from '../../../utils/reporting-format';

const CHANGE_LABELS: Record<string, string> = {
  Created: 'Creada', Deleted: 'Eliminada', Modified: 'Modificada', Relocated: 'Reubicada',
  Added: 'Agregado', Removed: 'Removido', PostsChanged: 'Plazas modificadas',
};

// RF-RE-04: comparativo entre dos contextos. Reutiliza la comparación de versiones (RF-VH-03).
@Component({
  selector: 'app-comparative-report',
  imports: [ReportingContextSelectorComponent, ReportExportControlComponent],
  template: `
    <div class="flex flex-col gap-4 p-6">
      <div class="flex flex-wrap items-center justify-between gap-4">
        <h1 class="text-xl font-semibold text-primary-blue">Reporte comparativo entre períodos</h1>
        <app-report-export-control [disabled]="!report()" [busy]="exporting()" (export)="onExport($event)" />
      </div>

      <div class="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <div class="rounded-xl border border-slate-100 bg-white p-4 shadow-sm">
          <p class="mb-2 text-xs font-semibold text-slate-600">Contexto origen (antes)</p>
          <app-reporting-context-selector (contextChange)="onFromChange($event)" />
        </div>
        <div class="rounded-xl border border-slate-100 bg-white p-4 shadow-sm">
          <p class="mb-2 text-xs font-semibold text-slate-600">Contexto destino (después)</p>
          <app-reporting-context-selector (contextChange)="onToChange($event)" />
        </div>
      </div>

      <div class="flex flex-wrap items-end gap-3 rounded-xl border border-slate-100 bg-white p-4 shadow-sm">
        <label class="flex flex-col gap-1 text-xs text-slate-600">
          <span>Base salarial (costo)</span>
          <select class="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-blue-500"
                  [value]="basis()" (change)="onBasisChange($any($event.target).value)">
            @for (b of bases; track b) { <option [value]="b">{{ basisLabels[b] }}</option> }
          </select>
        </label>
        <p class="text-xs text-slate-500">Seleccione una versión o fecha en cada lado para comparar.</p>
      </div>

      @if (loading()) {
        <div class="py-10 text-center text-sm text-slate-400">Comparando…</div>
      } @else if (report(); as r) {
        <div class="grid grid-cols-2 gap-4 md:grid-cols-4">
          <div class="rounded-xl border border-slate-100 bg-white p-4 text-center shadow-sm">
            <p class="text-2xl font-bold text-emerald-600">{{ num(r.unitComparison.summary.createdCount) }}</p>
            <p class="text-xs text-slate-500">Unidades creadas</p>
          </div>
          <div class="rounded-xl border border-slate-100 bg-white p-4 text-center shadow-sm">
            <p class="text-2xl font-bold text-red-600">{{ num(r.unitComparison.summary.deletedCount) }}</p>
            <p class="text-xs text-slate-500">Unidades eliminadas</p>
          </div>
          <div class="rounded-xl border border-slate-100 bg-white p-4 text-center shadow-sm">
            <p class="text-2xl font-bold text-slate-800">{{ signed(r.positionDeltas.authorizedPostsDelta) }}</p>
            <p class="text-xs text-slate-500">Δ Plazas autorizadas</p>
          </div>
          <div class="rounded-xl border border-slate-100 bg-white p-4 text-center shadow-sm">
            <p class="text-xl font-bold text-amber-700">{{ money(r.costDelta.costDelta) }}</p>
            <p class="text-xs text-slate-500">Δ Costo estimado</p>
          </div>
        </div>

        <div class="rounded-xl border border-slate-100 bg-white p-4 shadow-sm">
          <h2 class="mb-2 text-sm font-semibold text-slate-700">Cambios en unidades</h2>
          @if (!r.unitComparison.unitChanges.length) {
            <p class="text-xs text-slate-400">Sin cambios de unidades.</p>
          } @else {
            <table class="w-full border-collapse text-sm">
              <thead><tr class="bg-primary-blue text-white">
                <th class="px-3 py-2 text-left font-semibold">Unidad</th>
                <th class="px-3 py-2 text-left font-semibold">Cambio</th>
                <th class="px-3 py-2 text-left font-semibold">Antes</th>
                <th class="px-3 py-2 text-left font-semibold">Después</th>
              </tr></thead>
              <tbody>
                @for (u of r.unitComparison.unitChanges; track u.sourceUnitId) {
                  <tr class="border-t border-slate-100">
                    <td class="px-3 py-1.5">{{ (u.after?.code || u.before?.code) }} — {{ (u.after?.name || u.before?.name) }}</td>
                    <td class="px-3 py-1.5">{{ changeLabels(u.changeType) }}</td>
                    <td class="px-3 py-1.5 text-slate-500">{{ u.before?.name || '—' }}</td>
                    <td class="px-3 py-1.5 text-slate-500">{{ u.after?.name || '—' }}</td>
                  </tr>
                }
              </tbody>
            </table>
          }
        </div>

        <div class="rounded-xl border border-slate-100 bg-white p-4 shadow-sm">
          <h2 class="mb-2 text-sm font-semibold text-slate-700">Cambios en cargos y plazas</h2>
          @if (!r.positionDeltas.changes.length) {
            <p class="text-xs text-slate-400">Sin cambios de cargos.</p>
          } @else {
            <table class="w-full border-collapse text-sm">
              <thead><tr class="bg-primary-blue text-white">
                <th class="px-3 py-2 text-left font-semibold">Cargo</th>
                <th class="px-3 py-2 text-left font-semibold">Tipo</th>
                <th class="px-3 py-2 text-right font-semibold">Antes</th>
                <th class="px-3 py-2 text-right font-semibold">Después</th>
                <th class="px-3 py-2 text-right font-semibold">Δ</th>
              </tr></thead>
              <tbody>
                @for (d of r.positionDeltas.changes; track d.positionId) {
                  <tr class="border-t border-slate-100">
                    <td class="px-3 py-1.5">{{ d.positionName }}</td>
                    <td class="px-3 py-1.5">{{ label(d.changeType) }}</td>
                    <td class="px-3 py-1.5 text-right">{{ num(d.fromAuthorizedPosts) }}</td>
                    <td class="px-3 py-1.5 text-right">{{ num(d.toAuthorizedPosts) }}</td>
                    <td class="px-3 py-1.5 text-right font-medium">{{ signed(d.authorizedPostsDelta) }}</td>
                  </tr>
                }
              </tbody>
            </table>
          }
        </div>
      } @else {
        <div class="py-10 text-center text-sm text-slate-400">
          Seleccione dos contextos (versión o fecha) para generar la comparación.
        </div>
      }

      <p class="text-xs italic text-slate-400">
        El costo es estimado (salario × plazas autorizadas). Ocupación fuera de alcance por diseño.
      </p>
    </div>
  `,
})
export class ComparativeReportComponent implements OnInit {
  private reporting = inject(ReportingService);
  private downloader = inject(ReportDownloadService);
  private layoutService = inject(LayoutService);
  private notification = inject(NotificationService);

  protected readonly bases: SalaryBasis[] = ['Min', 'Midpoint', 'Max'];
  protected readonly basisLabels = SALARY_BASIS_LABELS;

  protected loading = signal(false);
  protected exporting = signal(false);
  protected report = signal<ComparativeReport | null>(null);
  private fromCtx = signal<ReportingContext>({});
  private toCtx = signal<ReportingContext>({});
  protected basis = signal<SalaryBasis>('Midpoint');

  protected num = num;
  protected money = money;
  protected signed = signed;

  ngOnInit(): void {
    this.layoutService.setPageTitle('Comparativo entre períodos');
    this.layoutService.breadcrumbs.set([
      { label: 'Reportes', url: '/reportes' },
      { label: 'Comparativo', url: '/reportes/comparativo' },
    ]);
  }

  protected onFromChange(ctx: ReportingContext): void {
    this.fromCtx.set(ctx);
    this.load();
  }

  protected onToChange(ctx: ReportingContext): void {
    this.toCtx.set(ctx);
    this.load();
  }

  protected onBasisChange(value: string): void {
    this.basis.set(value as SalaryBasis);
    this.load();
  }

  private hasBothSides(): boolean {
    const f = this.fromCtx();
    const t = this.toCtx();
    return (!!f.versionId || !!f.asOfDate) && (!!t.versionId || !!t.asOfDate);
  }

  private options(): CompareOptions {
    const f = this.fromCtx();
    const t = this.toCtx();
    return {
      fromVersionId: f.versionId,
      fromDate: f.asOfDate,
      toVersionId: t.versionId,
      toDate: t.asOfDate,
      basis: this.basis(),
    };
  }

  private async load(): Promise<void> {
    if (!this.hasBothSides()) {
      this.report.set(null);
      return;
    }
    this.loading.set(true);
    try {
      const data = await firstValueFrom(this.reporting.comparative(this.options()));
      this.report.set(data);
    } catch (e) {
      this.report.set(null);
      this.notification.fromHttpError(e, 'No se pudo comparar los contextos.');
    } finally {
      this.loading.set(false);
    }
  }

  protected label(type: string): string {
    return CHANGE_LABELS[type] ?? type;
  }

  protected changeLabels(types: string[]): string {
    return types.map((t) => this.label(t)).join(', ');
  }

  protected async onExport(format: ExportFormat): Promise<void> {
    if (!this.hasBothSides()) return;
    this.exporting.set(true);
    await this.downloader.download(
      this.reporting.exportComparative(this.options(), format),
      `comparativo.${format}`,
    );
    this.exporting.set(false);
  }
}
