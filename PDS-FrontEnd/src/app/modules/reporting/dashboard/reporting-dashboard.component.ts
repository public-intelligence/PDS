import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { LayoutService } from '@public-intelligence/ui-core';
import { ReportingContextSelectorComponent } from '../../../components/reporting/reporting-context-selector/reporting-context-selector.component';
import { ChartComponent } from '../../../components/reporting/chart/chart.component';
import { DashboardService } from '../../../services/reporting/dashboard.service';
import { NotificationService } from '../../../services/notification/notification.service';
import { ReportingDashboard, QUALITY_ALERT_REPORT } from '../../../models/reporting/reporting-dashboard.model';
import { ReportingContext, contextToParams } from '../../../models/reporting/reporting-context.model';
import { REPORT_META, ReportKey } from '../../../models/reporting/report-key.model';
import { ChartDatum } from '../../../models/reporting/chart.model';
import { money, num } from '../../../utils/reporting-format';

// RF-RE-07: dashboard de indicadores definicionales con drill-through a los reportes.
@Component({
  selector: 'app-reporting-dashboard',
  imports: [RouterLink, ReportingContextSelectorComponent, ChartComponent],
  template: `
    <div class="flex flex-col gap-4 p-6">
      <div class="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 class="text-xl font-semibold text-primary-blue">Reportes e Indicadores</h1>
          <p class="text-xs text-slate-500">Indicadores definicionales de la estructura publicada.</p>
        </div>
      </div>

      <div class="rounded-xl border border-slate-100 bg-white p-4 shadow-sm">
        <app-reporting-context-selector (contextChange)="onContextChange($event)" />
      </div>

      @if (loading()) {
        <div class="py-10 text-center text-sm text-slate-400">Cargando indicadores…</div>
      } @else if (dashboard(); as d) {
        <!-- KPIs -->
        <div class="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <a class="block rounded-xl border border-slate-100 bg-white p-4 shadow-sm hover:ring-1 hover:ring-primary-blue/30"
             [routerLink]="link('authorized-posts')" [queryParams]="ctxParams()">
            <p class="text-xs text-slate-500">Unidades activas</p>
            <p class="mt-1 text-2xl font-bold text-slate-900">{{ fmt(d.totalActiveUnits) }}</p>
            <p class="text-xs text-slate-400">{{ d.activeUnitsByLevel.length }} niveles jerárquicos</p>
          </a>
          <a class="block rounded-xl border border-slate-100 bg-white p-4 shadow-sm hover:ring-1 hover:ring-primary-blue/30"
             [routerLink]="link('authorized-posts')" [queryParams]="ctxParams()">
            <p class="text-xs text-slate-500">Cargos definidos</p>
            <p class="mt-1 text-2xl font-bold text-slate-900">{{ fmt(d.totalDefinedPositions) }}</p>
            <p class="text-xs text-slate-400">{{ fmt(d.totalAuthorizedPosts) }} plazas autorizadas</p>
          </a>
          <a class="block rounded-xl border border-slate-100 bg-white p-4 shadow-sm hover:ring-1 hover:ring-primary-blue/30"
             [routerLink]="link('estimated-cost')" [queryParams]="ctxParams()">
            <div class="flex items-center justify-between">
              <p class="text-xs text-slate-500">Costo total</p>
              <span class="rounded-full bg-amber-50 px-2 py-0.5 text-[10px] font-bold text-amber-700">ESTIMADO</span>
            </div>
            <p class="mt-1 text-2xl font-bold text-slate-900">{{ cost(d.estimatedTotalCost) }}</p>
            <p class="text-xs text-slate-400">Base: punto medio del rango</p>
          </a>
          <div class="rounded-xl border border-slate-100 bg-white p-4 shadow-sm">
            <p class="text-xs text-slate-500">Solicitudes pendientes</p>
            <p class="mt-1 text-2xl font-bold text-slate-900">{{ fmt(d.pendingApprovalCount) }}</p>
            <p class="text-xs text-slate-400">Versiones en revisión o aprobadas</p>
          </div>
        </div>

        <!-- Cambios recientes + alertas -->
        <div class="grid grid-cols-1 gap-4 lg:grid-cols-2">
          <div class="rounded-xl border border-slate-100 bg-white p-4 shadow-sm">
            <h2 class="mb-2 text-sm font-semibold text-slate-700">Cambios estructurales (último período)</h2>
            <div class="grid grid-cols-4 gap-2 text-center">
              <div><p class="text-lg font-bold text-emerald-600">{{ fmt(d.recentStructuralChanges.createdUnits) }}</p><p class="text-xs text-slate-500">Creadas</p></div>
              <div><p class="text-lg font-bold text-blue-600">{{ fmt(d.recentStructuralChanges.modifiedUnits) }}</p><p class="text-xs text-slate-500">Modificadas</p></div>
              <div><p class="text-lg font-bold text-amber-600">{{ fmt(d.recentStructuralChanges.relocatedUnits) }}</p><p class="text-xs text-slate-500">Reubicadas</p></div>
              <div><p class="text-lg font-bold text-red-600">{{ fmt(d.recentStructuralChanges.deletedUnits) }}</p><p class="text-xs text-slate-500">Eliminadas</p></div>
            </div>
            <a class="mt-3 inline-block text-xs font-semibold text-primary-blue hover:underline"
               [routerLink]="link('structural-changes')" [queryParams]="ctxParams()">Ver cambios estructurales →</a>
          </div>

          <div class="rounded-xl border border-slate-100 bg-white p-4 shadow-sm">
            <h2 class="mb-2 text-sm font-semibold text-slate-700">Alertas de calidad definicional</h2>
            @if (!d.qualityAlerts.length) {
              <p class="text-xs text-emerald-600">Sin alertas: la estructura cumple los criterios definicionales.</p>
            } @else {
              <ul class="flex flex-col gap-2">
                @for (a of d.qualityAlerts; track a.code) {
                  <li class="flex items-center justify-between gap-2 rounded-lg bg-amber-50 px-3 py-1.5">
                    <span class="text-xs text-amber-800">{{ a.message }}</span>
                    <a class="text-xs font-bold text-amber-900"
                       [routerLink]="link(alertReport(a.code))" [queryParams]="ctxParams()">{{ a.count }} →</a>
                  </li>
                }
              </ul>
            }
          </div>
        </div>

        <!-- Distribuciones -->
        <div class="grid grid-cols-1 gap-4 lg:grid-cols-3">
          <div class="rounded-xl border border-slate-100 bg-white p-4 shadow-sm">
            <app-chart type="donut" title="Plazas por grupo ocupacional" [data]="byGroup()" />
          </div>
          <div class="rounded-xl border border-slate-100 bg-white p-4 shadow-sm">
            <app-chart type="donut" title="Plazas por categoría salarial" [data]="bySalary()" />
          </div>
          <div class="rounded-xl border border-slate-100 bg-white p-4 shadow-sm">
            <app-chart type="bar" title="Plazas por nivel jerárquico" [data]="byLevel()" />
          </div>
        </div>
      } @else {
        <div class="py-10 text-center text-sm text-slate-400">
          No hay una versión publicada para el contexto seleccionado.
        </div>
      }
    </div>
  `,
})
export class ReportingDashboardComponent implements OnInit {
  private dashboardService = inject(DashboardService);
  private layoutService = inject(LayoutService);
  private notification = inject(NotificationService);

  protected loading = signal(true);
  protected dashboard = signal<ReportingDashboard | null>(null);
  private context = signal<ReportingContext>({});

  protected ctxParams = computed(() => contextToParams(this.context()));
  protected byGroup = computed<ChartDatum[]>(() =>
    (this.dashboard()?.authorizedPostsByOccupationalGroup ?? []).map((c) => ({ label: c.label, value: c.authorizedPosts })),
  );
  protected bySalary = computed<ChartDatum[]>(() =>
    (this.dashboard()?.authorizedPostsBySalaryCategory ?? []).map((c) => ({ label: c.label, value: c.authorizedPosts })),
  );
  protected byLevel = computed<ChartDatum[]>(() =>
    (this.dashboard()?.authorizedPostsByLevel ?? []).map((l) => ({ label: l.label, value: l.authorizedPosts })),
  );

  ngOnInit(): void {
    this.layoutService.setPageTitle('Reportes e Indicadores');
    this.layoutService.breadcrumbs.set([{ label: 'Reportes', url: '/reportes' }]);
    this.load();
  }

  protected onContextChange(ctx: ReportingContext): void {
    this.context.set(ctx);
    this.load();
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    try {
      const data = await firstValueFrom(this.dashboardService.getDashboard(this.context()));
      this.dashboard.set(data);
    } catch {
      this.dashboard.set(null);
      this.notification.error('No se pudieron cargar los indicadores.');
    } finally {
      this.loading.set(false);
    }
  }

  protected link(key: ReportKey): string[] {
    return ['/reportes', REPORT_META[key].route];
  }

  protected alertReport(code: string): ReportKey {
    return QUALITY_ALERT_REPORT[code] ?? 'authorized-posts';
  }

  protected fmt = num;
  protected cost = money;
}
