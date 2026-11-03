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
import { AuthorizedPostsReport, REPORT_GROUPING_LABELS, ReportGrouping } from '../../../models/reporting/reporting-reports.model';
import { ExportFormat, ReportingContext } from '../../../models/reporting/reporting-context.model';
import { num } from '../../../utils/reporting-format';

// RF-RE-01: plazas autorizadas, agrupable por unidad/nivel/categoría salarial/grupo ocupacional.
@Component({
  selector: 'app-authorized-posts-report',
  imports: [ReportShellComponent, ReportingContextSelectorComponent, ReportExportControlComponent, GroupedReportTableComponent],
  template: `
    <app-report-shell title="Reporte de plazas autorizadas">
      <app-report-export-control slot="export" [busy]="exporting()" (export)="onExport($event)" />

      <app-reporting-context-selector slot="context" [value]="context()" (contextChange)="onContextChange($event)" />

      <div slot="filters" class="flex flex-wrap items-end gap-3">
        <label class="flex flex-col gap-1 text-xs text-slate-600">
          <span>Agrupar por</span>
          <select class="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-blue-500"
                  [value]="groupBy()" (change)="onGroupByChange($any($event.target).value)">
            @for (g of groupings; track g) {
              <option [value]="g">{{ groupingLabels[g] }}</option>
            }
          </select>
        </label>
      </div>

      <app-grouped-report-table slot="table"
        [headers]="headers" [groups]="groups()" [grandTotal]="grandTotal()" [isLoading]="loading()" />
    </app-report-shell>
  `,
})
export class AuthorizedPostsReportComponent implements OnInit {
  private reporting = inject(ReportingService);
  private downloader = inject(ReportDownloadService);
  private layoutService = inject(LayoutService);
  private notification = inject(NotificationService);
  private route = inject(ActivatedRoute);

  protected readonly groupings: ReportGrouping[] = ['Unit', 'HierarchyLevel', 'SalaryCategory', 'OccupationalGroup'];
  protected readonly groupingLabels = REPORT_GROUPING_LABELS;
  protected readonly headers: GroupedHeader[] = [
    { label: 'Código' }, { label: 'Unidad' }, { label: 'Tipo de área' }, { label: 'Nivel', numeric: true },
    { label: 'Cargo' }, { label: 'Grupo ocupacional' }, { label: 'Grado', numeric: true }, { label: 'Plazas autorizadas', numeric: true },
  ];

  protected loading = signal(true);
  protected exporting = signal(false);
  protected context = signal<ReportingContext>({});
  protected groupBy = signal<ReportGrouping>('Unit');
  private report = signal<AuthorizedPostsReport | null>(null);

  protected groups = computed<GroupedBlock[]>(() => {
    const r = this.report();
    if (!r) return [];
    return r.subtotals.map((s) => ({
      label: s.groupLabel,
      subtotal: ['Subtotal: ' + s.groupLabel, '', '', '', num(s.distinctPositionCount) + ' cargos', '', '', num(s.authorizedPosts)],
      rows: r.rows
        .filter((row) => row.groupKey === s.groupKey)
        .map((row) => [
          row.unitCode ?? '', row.unitName ?? '', row.areaTypeName ?? '', num(row.level),
          row.positionName ?? '', row.occupationalGroupName ?? '', num(row.grade), num(row.authorizedPosts),
        ]),
    }));
  });

  protected grandTotal = computed<(string | null)[]>(() => {
    const gt = this.report()?.grandTotal;
    if (!gt) return [];
    return ['Total general', '', '', '', num(gt.distinctPositionCount) + ' cargos', '', '', num(gt.authorizedPosts)];
  });

  ngOnInit(): void {
    this.layoutService.setPageTitle('Plazas autorizadas');
    this.layoutService.breadcrumbs.set([
      { label: 'Reportes', url: '/reportes' },
      { label: 'Plazas autorizadas', url: '/reportes/puestos-autorizados' },
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

  protected onGroupByChange(value: string): void {
    this.groupBy.set(value as ReportGrouping);
    this.load();
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    try {
      const data = await firstValueFrom(this.reporting.authorizedPosts(this.context(), this.groupBy()));
      this.report.set(data);
    } catch (e) {
      this.report.set(null);
      this.notification.fromHttpError(e, 'No se pudo cargar el reporte.');
    } finally {
      this.loading.set(false);
    }
  }

  protected async onExport(format: ExportFormat): Promise<void> {
    this.exporting.set(true);
    await this.downloader.download(
      this.reporting.exportAuthorizedPosts(this.context(), this.groupBy(), format),
      `plazas-autorizadas.${format}`,
    );
    this.exporting.set(false);
  }
}
