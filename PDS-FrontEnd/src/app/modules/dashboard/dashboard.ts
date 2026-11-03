import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { PageShellComponent } from '@public-intelligence/ui-core';
import { ChartComponent } from '../../components/reporting/chart/chart.component';
import { DashboardService } from '../../services/reporting/dashboard.service';
import { NotificationService } from '../../services/notification/notification.service';
import { ReportingDashboard } from '../../models/reporting/reporting-dashboard.model';
import { ChartDatum } from '../../models/reporting/chart.model';
import { money, num } from '../../utils/reporting-format';

interface StatCard {
  label: string;
  value: string;
  description: string;
  iconPath: string;
  color: string;
  badge?: string;
}

@Component({
  selector: 'app-dashboard',
  imports: [PageShellComponent, ChartComponent],
  templateUrl: './dashboard.html',
})
export class Dashboard implements OnInit {
  private dashboardService = inject(DashboardService);
  private notification = inject(NotificationService);

  protected loading = signal(true);
  protected dashboard = signal<ReportingDashboard | null>(null);

  /** KPIs definicionales del contexto vigente (RF-RE-07). */
  protected stats = computed<StatCard[]>(() => {
    const d = this.dashboard();
    if (!d) return [];
    return [
      {
        label: 'Unidades Activas',
        value: num(d.totalActiveUnits),
        description: `${num(d.activeUnitsByLevel.length)} niveles jerárquicos`,
        iconPath:
          'M2.25 21h19.5m-18-18v18m10.5-18v18m6-13.5V21M6.75 6.75h.75m-.75 3h.75m-.75 3h.75m3-6h.75m-.75 3h.75m-.75 3h.75M6.75 21v-3.375c0-.621.504-1.125 1.125-1.125h2.25c.621 0 1.125.504 1.125 1.125V21M3 3h12m-.75 4.5H21m-3.75 3.75h.008v.008h-.008v-.008zm0 3h.008v.008h-.008v-.008zm0 3h.008v.008h-.008v-.008z',
        color: 'text-brand bg-brand-50',
      },
      {
        label: 'Cargos Definidos',
        value: num(d.totalDefinedPositions),
        description: `${num(d.totalAuthorizedPosts)} plazas autorizadas`,
        iconPath:
          'M20.25 14.15v4.25c0 1.094-.787 2.036-1.872 2.18-2.087.277-4.216.42-6.378.42s-4.291-.143-6.378-.42c-1.085-.144-1.872-1.086-1.872-2.18v-4.25m16.5 0a2.18 2.18 0 00.75-1.661V8.706c0-1.081-.768-2.015-1.837-2.175a48.114 48.114 0 00-3.413-.387m4.5 8.006c-.194.165-.42.295-.673.38A23.978 23.978 0 0112 15.75c-2.648 0-5.195-.429-7.577-1.22a2.016 2.016 0 01-.673-.38m0 0A2.18 2.18 0 013 12.489V8.706c0-1.081.768-2.015 1.837-2.175a48.111 48.111 0 013.413-.387m7.5 0V5.25A2.25 2.25 0 0013.5 3h-3a2.25 2.25 0 00-2.25 2.25v.894m7.5 0a48.667 48.667 0 00-7.5 0M12 12.75h.008v.008H12v-.008z',
        color: 'text-success-text bg-success-bg',
      },
      {
        label: 'Costo Total Estimado',
        value: money(d.estimatedTotalCost),
        description: 'Base: punto medio del rango salarial',
        badge: 'ESTIMADO',
        iconPath:
          'M12 6v12m-3-2.818l.879.659c1.171.879 3.07.879 4.242 0 1.172-.879 1.172-2.303 0-3.182C13.536 12.219 12.768 12 12 12c-.725 0-1.45-.22-2.003-.659-1.106-.879-1.106-2.303 0-3.182s2.9-.879 4.006 0l.415.33M21 12a9 9 0 11-18 0 9 9 0 0118 0z',
        color: 'text-accent-700 bg-accent-50',
      },
      {
        label: 'Aprobaciones Pendientes',
        value: num(d.pendingApprovalCount),
        description: 'Versiones en revisión o aprobadas',
        iconPath: 'M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z',
        color: 'text-info-text bg-info-bg',
      },
    ];
  });

  protected byGroup = computed<ChartDatum[]>(() =>
    (this.dashboard()?.authorizedPostsByOccupationalGroup ?? []).map((c) => ({
      label: c.label,
      value: c.authorizedPosts,
    })),
  );
  protected bySalary = computed<ChartDatum[]>(() =>
    (this.dashboard()?.authorizedPostsBySalaryCategory ?? []).map((c) => ({
      label: c.label,
      value: c.authorizedPosts,
    })),
  );
  protected byLevel = computed<ChartDatum[]>(() =>
    (this.dashboard()?.authorizedPostsByLevel ?? []).map((l) => ({
      label: l.label,
      value: l.authorizedPosts,
    })),
  );

  ngOnInit(): void {
    this.load();
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    try {
      const data = await firstValueFrom(this.dashboardService.getDashboard({}));
      this.dashboard.set(data);
    } catch {
      this.dashboard.set(null);
      this.notification.error('No se pudieron cargar los indicadores.');
    } finally {
      this.loading.set(false);
    }
  }

  protected fmt = num;
}
