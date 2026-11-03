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
import { StaffingComplementReport } from '../../../models/reporting/reporting-reports.model';
import { ExportFormat, ReportingContext } from '../../../models/reporting/reporting-context.model';
import { num } from '../../../utils/reporting-format';

// RF-RE-02: dotación definida (cargos distintos + plazas autorizadas), consolidada por jerarquía.
@Component({
  selector: 'app-staffing-complement-report',
  imports: [ReportShellComponent, ReportingContextSelectorComponent, ReportExportControlComponent, GroupedReportTableComponent],
  template: `
    <app-report-shell title="Reporte de dotación definida (plantilla)">
      <app-report-export-control slot="export" [busy]="exporting()" (export)="onExport($event)" />
      <app-reporting-context-selector slot="context" [value]="context()" (contextChange)="onContextChange($event)" />
      <p slot="filters" class="text-xs text-slate-500">
        Cargos definidos y plazas autorizadas por unidad y nivel jerárquico, con figuras propias y
        consolidadas (unidad + descendientes). No representa ocupación ni personal real.
      </p>
      <app-grouped-report-table slot="table"
        [headers]="headers" [groups]="groups()" [grandTotal]="grandTotal()" [isLoading]="loading()" />
    </app-report-shell>
  `,
})
export class StaffingComplementReportComponent implements OnInit {
  private reporting = inject(ReportingService);
  private downloader = inject(ReportDownloadService);
  private layoutService = inject(LayoutService);
  private notification = inject(NotificationService);
  private route = inject(ActivatedRoute);

  protected readonly headers: GroupedHeader[] = [
    { label: 'Código' }, { label: 'Unidad' }, { label: 'Tipo de área' }, { label: 'Nivel', numeric: true },
    { label: 'Cargos propios', numeric: true }, { label: 'Plazas propias', numeric: true },
    { label: 'Cargos consolidados', numeric: true }, { label: 'Plazas consolidadas', numeric: true },
    { label: 'Supervisor' },
  ];

  protected loading = signal(true);
  protected exporting = signal(false);
  protected context = signal<ReportingContext>({});
  private report = signal<StaffingComplementReport | null>(null);

  // Agrupa las unidades por nivel; el subtotal de cada grupo es la fila "por nivel" del backend.
  protected groups = computed<GroupedBlock[]>(() => {
    const r = this.report();
    if (!r) return [];
    return r.byLevel.map((lvl) => ({
      label: lvl.groupLabel,
      subtotal: ['Subtotal ' + lvl.groupLabel, '', '', '', num(lvl.distinctPositionCount), num(lvl.authorizedPosts), '', '', ''],
      rows: r.units
        .filter((u) => `L${u.level}` === lvl.groupKey)
        .map((u) => [
          u.code, u.name, u.areaTypeName ?? '', num(u.level),
          num(u.ownDistinctPositions), num(u.ownAuthorizedPosts),
          num(u.consolidatedDistinctPositions), num(u.consolidatedAuthorizedPosts),
          u.hasSupervisorDefined ? 'Sí' : 'No',
        ]),
    }));
  });

  protected grandTotal = computed<(string | null)[]>(() => {
    const gt = this.report()?.grandTotal;
    if (!gt) return [];
    return ['Total general', '', '', '', num(gt.distinctPositionCount), num(gt.authorizedPosts), '', '', ''];
  });

  ngOnInit(): void {
    this.layoutService.setPageTitle('Dotación definida');
    this.layoutService.breadcrumbs.set([
      { label: 'Reportes', url: '/reportes' },
      { label: 'Dotación definida', url: '/reportes/dotacion' },
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

  private async load(): Promise<void> {
    this.loading.set(true);
    try {
      const data = await firstValueFrom(this.reporting.staffingComplement(this.context()));
      this.report.set(data);
    } catch (e) {
      this.report.set(null);
      this.notification.fromHttpError(e, 'No se pudo cargar la dotación.');
    } finally {
      this.loading.set(false);
    }
  }

  protected async onExport(format: ExportFormat): Promise<void> {
    this.exporting.set(true);
    await this.downloader.download(
      this.reporting.exportStaffingComplement(this.context(), format),
      `dotacion.${format}`,
    );
    this.exporting.set(false);
  }
}
