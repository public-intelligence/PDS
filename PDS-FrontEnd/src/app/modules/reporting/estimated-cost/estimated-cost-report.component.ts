import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { LayoutService } from '@public-intelligence/ui-core';
import { ReportShellComponent } from '../../../components/reporting/report-shell/report-shell.component';
import { ReportingContextSelectorComponent } from '../../../components/reporting/reporting-context-selector/reporting-context-selector.component';
import { ReportExportControlComponent } from '../../../components/reporting/report-export-control/report-export-control.component';
import { GroupedReportTableComponent, GroupedBlock, GroupedHeader } from '../../../components/reporting/grouped-report-table/grouped-report-table.component';
import { ReportingService } from '../../../services/reporting/reporting.service';
import { ReportDownloadService } from '../../../services/reporting/report-download.service';
import { NotificationService } from '../../../services/notification/notification.service';
import {
  EstimatedCostReport,
  REPORT_GROUPING_LABELS,
  ReportGrouping,
  SALARY_BASIS_LABELS,
  SalaryBasis,
} from '../../../models/reporting/reporting-reports.model';
import { ExportFormat, ReportingContext } from '../../../models/reporting/reporting-context.model';
import { money, num } from '../../../utils/reporting-format';

// RF-RE-03: costo estimado (salario × plazas autorizadas). Siempre etiquetado como estimación.
@Component({
  selector: 'app-estimated-cost-report',
  imports: [ReportShellComponent, ReportingContextSelectorComponent, ReportExportControlComponent, GroupedReportTableComponent],
  template: `
    <app-report-shell title="Reporte de costo estimado de la plantilla">
      <app-report-export-control slot="export" [busy]="exporting()" (export)="onExport($event)" />

      <app-reporting-context-selector slot="context" [value]="context()" (contextChange)="onContextChange($event)" />

      <div slot="filters" class="flex flex-col gap-3">
        <div class="flex flex-wrap items-end gap-3">
          <label class="flex flex-col gap-1 text-xs text-slate-600">
            <span>Base salarial</span>
            <select class="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-blue-500"
                    [value]="basis()" (change)="onBasisChange($any($event.target).value)">
              @for (b of bases; track b) { <option [value]="b">{{ basisLabels[b] }}</option> }
            </select>
          </label>
          <label class="flex flex-col gap-1 text-xs text-slate-600">
            <span>Agrupar por</span>
            <select class="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-blue-500"
                    [value]="groupBy()" (change)="onGroupByChange($any($event.target).value)">
              @for (g of groupings; track g) { <option [value]="g">{{ groupingLabels[g] }}</option> }
            </select>
          </label>
        </div>
        <p class="rounded-lg bg-amber-50 px-3 py-2 text-xs text-amber-800">
          <strong>Estimación</strong> derivada de plazas autorizadas — no es nómina real.
          @if (report(); as r) {
            @if (r.unresolvedPositionCount > 0) {
              Cargos sin salario utilizable (excluidos): {{ r.unresolvedPositionCount }}.
            }
          }
        </p>
      </div>

      <app-grouped-report-table slot="table"
        [headers]="headers" [groups]="groups()" [grandTotal]="grandTotal()" [isLoading]="loading()" />
    </app-report-shell>
  `,
})
export class EstimatedCostReportComponent implements OnInit {
  private reporting = inject(ReportingService);
  private downloader = inject(ReportDownloadService);
  private layoutService = inject(LayoutService);
  private notification = inject(NotificationService);
  private route = inject(ActivatedRoute);

  protected readonly groupings: ReportGrouping[] = ['Unit', 'HierarchyLevel', 'SalaryCategory', 'OccupationalGroup'];
  protected readonly bases: SalaryBasis[] = ['Min', 'Midpoint', 'Max'];
  protected readonly groupingLabels = REPORT_GROUPING_LABELS;
  protected readonly basisLabels = SALARY_BASIS_LABELS;
  protected readonly headers: GroupedHeader[] = [
    { label: 'Código' }, { label: 'Unidad' }, { label: 'Nivel', numeric: true }, { label: 'Cargo' },
    { label: 'Grupo ocupacional' }, { label: 'Grado', numeric: true }, { label: 'Plazas', numeric: true },
    { label: 'Salario unitario (estimado)', numeric: true }, { label: 'Costo estimado', numeric: true },
  ];

  protected loading = signal(true);
  protected exporting = signal(false);
  protected context = signal<ReportingContext>({});
  protected basis = signal<SalaryBasis>('Midpoint');
  protected groupBy = signal<ReportGrouping>('Unit');
  protected report = signal<EstimatedCostReport | null>(null);

  protected groups = computed<GroupedBlock[]>(() => {
    const r = this.report();
    if (!r) return [];
    return r.subtotals.map((s) => ({
      label: s.groupLabel,
      subtotal: ['Subtotal: ' + s.groupLabel, '', '', '', '', '', num(s.authorizedPosts), '', money(s.estimatedCost)],
      rows: r.rows
        .filter((row) => row.groupKey === s.groupKey)
        .map((row) => [
          row.unitCode, row.unitName, num(row.level), row.positionName,
          row.occupationalGroupName ?? '', num(row.grade), num(row.authorizedPosts),
          money(row.unitSalary), money(row.lineCost),
        ]),
    }));
  });

  protected grandTotal = computed<(string | null)[]>(() => {
    const gt = this.report()?.grandTotal;
    if (!gt) return [];
    return ['Total general (estimado)', '', '', '', '', '', num(gt.authorizedPosts), '', money(gt.estimatedCost)];
  });

  ngOnInit(): void {
    this.layoutService.setPageTitle('Costo estimado');
    this.layoutService.breadcrumbs.set([
      { label: 'Reportes', url: '/reportes' },
      { label: 'Costo estimado', url: '/reportes/costo-estimado' },
    ]);
    this.context.set(this.seedContext());
    this.load();
  }

  private seedContext(): ReportingContext {
    const qp = this.route.snapshot.queryParamMap;
    const versionId = qp.get('versionId');
    const asOfDate = qp.get('asOfDate');
    if (versionId) return { versionId };
    if (asOfDate) return { asOfDate };
    return {};
  }

  protected onContextChange(ctx: ReportingContext): void {
    this.context.set(ctx);
    this.load();
  }

  protected onBasisChange(value: string): void {
    this.basis.set(value as SalaryBasis);
    this.load();
  }

  protected onGroupByChange(value: string): void {
    this.groupBy.set(value as ReportGrouping);
    this.load();
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    try {
      const data = await firstValueFrom(this.reporting.estimatedCost(this.context(), this.basis(), this.groupBy()));
      this.report.set(data);
    } catch (e) {
      this.report.set(null);
      this.notification.fromHttpError(e, 'No se pudo cargar el reporte de costo.');
    } finally {
      this.loading.set(false);
    }
  }

  protected async onExport(format: ExportFormat): Promise<void> {
    this.exporting.set(true);
    await this.downloader.download(
      this.reporting.exportEstimatedCost(this.context(), this.basis(), this.groupBy(), format),
      `costo-estimado.${format}`,
    );
    this.exporting.set(false);
  }
}
