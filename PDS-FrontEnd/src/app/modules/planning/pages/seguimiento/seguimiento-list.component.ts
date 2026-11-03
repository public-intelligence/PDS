import { Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PageShellComponent } from '@public-intelligence/ui-core';
import { PlanningStoreService } from '../../data/planning-store.service';

/**
 * Seguimiento y ejecución: registros pendientes de actualizar en el corte
 * vigente, con semáforo de desviación física vs financiera.
 */
@Component({
  selector: 'app-seguimiento-list',
  imports: [PageShellComponent, RouterLink],
  template: `
    <app-page-shell
      title="Seguimiento y ejecución"
      description="Corte al 30 de junio de 2026 (POA 2026 en ejecución) · reporte avance físico, financiero y evidencias"
    >
      <div class="mb-4 grid grid-cols-2 gap-3 lg:grid-cols-4">
        <div class="stat-card">
          <p class="stat-label">Registros del corte</p>
          <p class="stat-value">{{ registros().length }}</p>
        </div>
        <div class="stat-card">
          <p class="stat-label">Pendientes</p>
          <p class="stat-value text-warning-text">{{ pendientes() }}</p>
        </div>
        <div class="stat-card">
          <p class="stat-label">Reportados</p>
          <p class="stat-value text-success-text">{{ registros().length - pendientes() }}</p>
        </div>
        <div class="stat-card">
          <p class="stat-label">Con desviación</p>
          <p class="stat-value text-error-text">{{ conDesviacion() }}</p>
        </div>
      </div>

      <div class="card p-0">
        <table class="w-full text-sm">
          <thead>
            <tr>
              @for (h of ['Actividad', 'Área', 'Meta del período', 'Avance físico', 'Avance financiero', 'Desviación', 'Evidencia', 'Estado', ''];
                track h) {
                <th class="border-b border-border bg-surface-sunken px-4 py-2 text-left text-xs font-semibold tracking-wide whitespace-nowrap text-ink-muted uppercase">{{ h }}</th>
              }
            </tr>
          </thead>
          <tbody>
            @for (r of registros(); track r.id) {
              <tr class="group hover:bg-surface">
                <td class="max-w-md border-b border-border px-4 py-2.5">
                  <a [routerLink]="'/seguimiento/' + r.id" class="!text-sm font-medium">{{ r.actividad }}</a>
                </td>
                <td class="border-b border-border px-4 py-2.5 whitespace-nowrap text-ink-muted">{{ store.area(r.areaId)?.corta }}</td>
                <td class="border-b border-border px-4 py-2.5 whitespace-nowrap text-ink-muted">{{ r.metaPeriodo }}</td>
                <td class="border-b border-border px-4 py-2.5 whitespace-nowrap tabular-nums">
                  {{ r.avanceFisico !== null ? r.avanceFisico + '%' : '—' }}
                </td>
                <td class="border-b border-border px-4 py-2.5 whitespace-nowrap tabular-nums">
                  {{ r.avanceFinanciero !== null ? r.avanceFinanciero + '%' : '—' }}
                </td>
                <td class="border-b border-border px-4 py-2.5 whitespace-nowrap">
                  @if (r.avanceFisico !== null && r.avanceFinanciero !== null) {
                    @let desv = r.avanceFinanciero - r.avanceFisico;
                    <span [class]="desv > 10 ? 'badge-error' : desv > 5 ? 'badge-warning' : 'badge-success'">
                      {{ desv > 0 ? '+' : '' }}{{ desv }} pts
                    </span>
                  } @else {
                    <span class="text-ink-subtle">—</span>
                  }
                </td>
                <td class="border-b border-border px-4 py-2.5 whitespace-nowrap">
                  @if (r.evidencias.length) {
                    <span class="badge-success">{{ r.evidencias.length }} adjunta(s)</span>
                  } @else {
                    <span class="badge-warning">Faltante</span>
                  }
                </td>
                <td class="border-b border-border px-4 py-2.5 whitespace-nowrap">
                  <span [class]="r.estado === 'Reportado' ? 'badge-success' : 'badge-warning'">{{ r.estado }}</span>
                </td>
                <td class="border-b border-border px-4 py-2.5 text-right whitespace-nowrap">
                  <a [routerLink]="'/seguimiento/' + r.id" class="btn-ghost btn-sm !no-underline opacity-0 group-hover:opacity-100">
                    {{ r.estado === 'Pendiente' ? 'Reportar' : 'Ver' }}
                  </a>
                </td>
              </tr>
            } @empty {
              <tr><td colspan="9" class="px-4 py-10 text-center text-sm text-ink-muted">Sin registros para su área en este corte.</td></tr>
            }
          </tbody>
        </table>
      </div>
    </app-page-shell>
  `,
})
export class SeguimientoListComponent {
  protected readonly store = inject(PlanningStoreService);

  protected readonly registros = computed(() => {
    const persona = this.store.personaId();
    const areaId = this.store.persona().areaId;
    return this.store
      .seguimiento()
      .filter((r) => (persona === 'sustantiva' || persona === 'transversal' ? r.areaId === areaId : true));
  });

  protected readonly pendientes = computed(
    () => this.registros().filter((r) => r.estado === 'Pendiente').length,
  );

  protected readonly conDesviacion = computed(
    () =>
      this.registros().filter(
        (r) =>
          r.avanceFisico !== null &&
          r.avanceFinanciero !== null &&
          r.avanceFinanciero - r.avanceFisico > 10,
      ).length,
  );
}
