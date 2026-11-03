import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';
import { DatePipe } from '@angular/common';
import { PageShellComponent } from '@public-intelligence/ui-core';
import { PlanningCycleService } from '../../../../services/planning/planning-cycle.service';
import { PlanningCycleDto } from '../../data/planning-api.models';

/**
 * Gestión de ciclos de planificación (anuales o plurianuales) conectados al backend M4.
 */
@Component({
  selector: 'app-ciclos-list',
  standalone: true,
  imports: [PageShellComponent, RouterLink, DatePipe],
  template: `
    <app-page-shell
      title="Ciclos de planificación"
      description="Cada ciclo define calendario, plantillas y reglas del proceso de formulación"
    >
      @if (publicado()) {
        <div
          class="slide-in-top mb-4 rounded-xl border border-success-border bg-success-bg px-4 py-3"
          role="status"
        >
          <p class="!text-sm font-medium text-success-text">
            Ciclo «{{ publicado() }}» publicado. Las áreas incluidas ya pueden iniciar su formulación.
          </p>
        </div>
      }

      @if (errorMsg()) {
        <div class="slide-in-top mb-4 rounded-xl border border-danger-border bg-danger-bg px-4 py-3 text-sm text-danger-text">
          {{ errorMsg() }}
          <button class="btn-ghost btn-sm ml-2" (click)="errorMsg.set(null)">✕</button>
        </div>
      }

      <div class="mb-4 flex items-center justify-between">
        <h2 class="!font-poppins !text-lg font-semibold text-ink">Ciclos registrados</h2>
        <a routerLink="/ciclos/nuevo" class="btn-primary !no-underline">+ Crear ciclo</a>
      </div>

      @if (isLoading()) {
        <div class="card p-8 text-center text-ink-muted">
          <span class="animate-spin inline-block w-5 h-5 border-2 border-brand border-t-transparent rounded-full mr-2"></span>
          Cargando ciclos de planificación...
        </div>
      } @else if (ciclos().length === 0) {
        <div class="card p-8 text-center text-ink-muted border border-dashed border-border">
          No hay ciclos de planificación registrados. Haga clic en «Crear ciclo» para iniciar.
        </div>
      } @else {
        <div class="grid grid-cols-1 gap-4 lg:grid-cols-2">
          @for (c of ciclos(); track c.id) {
            <div class="card p-5 flex flex-col justify-between">
              <div>
                <div class="flex items-start justify-between gap-3">
                  <div>
                    <h3 class="!font-poppins !text-lg font-semibold text-ink">{{ c.name }}</h3>
                    <p class="!text-sm text-ink-muted">
                      Año {{ c.year }}{{ c.endYear ? '–' + c.endYear : '' }} · {{ c.templateName || 'Sin plantilla' }}
                    </p>
                  </div>
                  <span [class]="c.status === 'Published' ? 'badge-success' : c.status === 'Draft' ? 'badge-warning' : 'badge-neutral'">
                    {{ c.status === 'Published' ? 'Publicado' : c.status === 'Draft' ? 'Borrador' : 'Cerrado' }}
                  </span>
                </div>

                <div class="mt-4 flex flex-col gap-1 text-sm text-ink-subtle">
                  <p class="!text-xs text-ink-muted">
                    PEI: <strong class="text-ink">{{ c.strategicPlanName || 'No vinculado' }}</strong>
                  </p>
                  <p class="!text-xs text-ink-muted">
                    {{ c.areaCount }} áreas alcanzadas
                  </p>
                </div>

                <!-- Plan Counts by Status -->
                @if (c.status === 'Published' && getPlanCountsKeys(c.planCountsByStatus).length > 0) {
                  <div class="mt-3 border-t border-border pt-2">
                    <p class="!text-xs font-semibold text-ink-subtle uppercase mb-1">Estado de planes (POA)</p>
                    <div class="flex flex-wrap gap-1.5">
                      @for (key of getPlanCountsKeys(c.planCountsByStatus); track key) {
                        <span class="badge-neutral text-xs">
                          {{ key }}: {{ c.planCountsByStatus[key] }}
                        </span>
                      }
                    </div>
                  </div>
                }
              </div>

              <div class="mt-4 flex items-center justify-between border-t border-border pt-3">
                <span class="text-xs text-ink-muted">
                  {{ c.publishedAt ? ('Publicado: ' + (c.publishedAt | date:'dd/MM/yyyy')) : 'En preparación' }}
                </span>

                <div class="flex gap-2">
                  @if (c.status === 'Draft') {
                    <button class="btn-primary btn-sm" (click)="publicarCiclo(c)" [disabled]="isActionLoading()">
                      Publicar
                    </button>
                  } @else if (c.status === 'Published') {
                    <button class="btn-ghost btn-sm text-danger" (click)="cerrarCiclo(c)" [disabled]="isActionLoading()">
                      Cerrar ciclo
                    </button>
                  }
                </div>
              </div>
            </div>
          }
        </div>
      }
    </app-page-shell>
  `,
})
export class CiclosListComponent {
  private readonly cycleService = inject(PlanningCycleService);
  private readonly route = inject(ActivatedRoute);

  ciclos = signal<PlanningCycleDto[]>([]);
  isLoading = signal(true);
  isActionLoading = signal(false);
  errorMsg = signal<string | null>(null);

  protected readonly publicado = toSignal(
    this.route.queryParamMap.pipe(map((p) => p.get('publicado'))),
    { initialValue: null },
  );

  constructor() {
    this.loadCiclos();
  }

  loadCiclos() {
    this.isLoading.set(true);
    this.cycleService.list().subscribe({
      next: (data) => {
        this.ciclos.set(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.errorMsg.set('Error al cargar la lista de ciclos de planificación.');
        this.isLoading.set(false);
        console.error(err);
      },
    });
  }

  publicarCiclo(c: PlanningCycleDto) {
    if (!confirm(`¿Está seguro de publicar el ciclo «${c.name}»? Esta acción creará los planes anuales de todas las áreas alcanzadas.`)) return;

    this.isActionLoading.set(true);
    this.errorMsg.set(null);
    this.cycleService.publish(c.id).subscribe({
      next: () => {
        this.isActionLoading.set(false);
        this.loadCiclos();
      },
      error: (err) => {
        this.isActionLoading.set(false);
        const detail = err?.error?.title || err?.error?.errors?.[0] || 'Error al publicar el ciclo.';
        this.errorMsg.set(detail);
        console.error(err);
      },
    });
  }

  cerrarCiclo(c: PlanningCycleDto) {
    if (!confirm(`¿Está seguro de cerrar el ciclo «${c.name}»? Una vez cerrado, no se permitirán más modificaciones en los planes de este ciclo.`)) return;

    this.isActionLoading.set(true);
    this.errorMsg.set(null);
    this.cycleService.close(c.id).subscribe({
      next: () => {
        this.isActionLoading.set(false);
        this.loadCiclos();
      },
      error: (err) => {
        this.isActionLoading.set(false);
        this.errorMsg.set('Error al cerrar el ciclo.');
        console.error(err);
      },
    });
  }

  getPlanCountsKeys(counts: Record<string, number>): string[] {
    return counts ? Object.keys(counts) : [];
  }
}
