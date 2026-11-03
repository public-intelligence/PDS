import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PageShellComponent } from '@public-intelligence/ui-core';
import { PoaStoreService } from '../../../../services/planning/poa-store.service';

const FILTROS = ['Todos', 'Faltantes', 'En borrador', 'Observados', 'Listos para revisar'] as const;
type Filtro = (typeof FILTROS)[number];

/**
 * Planes anuales por ciclo (M5, contra backend real). Navegación por listados: el selector
 * de ciclo permite consultar POA de ciclos pasados (solo lectura si el ciclo no está
 * publicado). El alcance depende de los permisos reales: quien tiene planning:poa:review ve
 * todas las áreas; el resto solo su propio plan.
 */
@Component({
  selector: 'app-planes-list',
  imports: [PageShellComponent, RouterLink],
  template: `
    <app-page-shell title="Plan anual" [description]="descripcion()">
      @if (store.error()) {
        <p class="mb-4 rounded-lg bg-error-bg px-3 py-2 text-sm text-error-text ring-1 ring-error-border">{{ store.error() }}</p>
      }

      <!-- Selector de ciclo (consultar el pasado) -->
      <div class="mb-4 flex flex-wrap items-center gap-3">
        <label class="field-label !mb-0 flex items-center gap-2">Ciclo
          <select
            class="field-control !h-9 !w-auto"
            [value]="store.cicloActivo().id"
            (change)="cambiarCiclo($event)"
          >
            @for (c of store.ciclos(); track c.id) {
              <option [value]="c.id">{{ c.nombre }} ({{ c.periodo }}) — {{ c.estado }}</option>
            }
          </select>
        </label>
        @if (store.cicloActivo().estado === 'Cerrado') {
          <span class="badge-neutral">Ciclo cerrado — solo consulta</span>
        }
        @if (puedeRevisar()) {
          <button type="button" class="btn-ghost btn-sm" (click)="store.exportarCiclo(store.cicloActivo().id)">
            Exportar POA institucional
          </button>
        }
      </div>

      @if (veTodo()) {
        <div class="segmented mb-4">
          @for (f of filtros; track f) {
            <button
              type="button"
              class="segmented-item"
              [class.segmented-item-active]="filtro() === f"
              (click)="filtro.set(f)"
            >
              {{ f }}
              <span class="rounded-full bg-surface-sunken px-1.5 text-[10px] font-bold tabular-nums">{{ contar(f) }}</span>
            </button>
          }
        </div>
      } @else {
        <div class="mb-4 rounded-lg bg-info-bg px-3 py-2 text-sm text-info-text ring-1 ring-info-border">
          Viendo únicamente el plan de {{ store.areaActual().nombre }} (alcance de su rol).
        </div>
      }

      @if (store.cargando() || !store.cargado()) {
        <div class="card flex flex-col items-center justify-center p-12 text-center text-ink-muted min-h-[300px]">
          <span class="inline-block size-8 animate-spin rounded-full border-4 border-brand border-t-transparent mb-3"></span>
          <p class="text-base font-semibold text-ink">Cargando planes anuales...</p>
          <p class="text-xs text-ink-muted mt-1">Obteniendo los planes correspondientes al ciclo seleccionado.</p>
        </div>
      } @else {
        <div class="grid grid-cols-1 gap-4 lg:grid-cols-2 xl:grid-cols-3">
          @for (p of visibles(); track p.id) {
            @let issues = store.conteoIncompletos(p.id);
            <a
              [routerLink]="'/plan-anual/' + p.id"
              class="card block p-5 !no-underline transition-shadow hover:shadow-md"
              [class.ring-2]="p.areaId === store.persona().areaId"
              [class.ring-brand-300]="p.areaId === store.persona().areaId"
            >
              <div class="flex items-start justify-between gap-2">
                <p class="!text-base font-semibold text-ink">{{ store.area(p.areaId)?.nombre ?? p.nombre }}</p>
                <span [class]="badge(p.estado)" class="shrink-0">{{ p.estado }}</span>
              </div>
              @if (p.areaId === store.persona().areaId) {
                <span class="badge-info mt-1">Mi área</span>
              }

              <div class="mt-3 flex items-center gap-3">
                <div class="h-2 flex-1 overflow-hidden rounded-full bg-surface-sunken">
                  <div class="h-full rounded-full bg-brand" [style.width.%]="progreso(p.id)"></div>
                </div>
                <span class="!text-xs tabular-nums text-ink-muted">{{ progreso(p.id) }}%</span>
              </div>

              <dl class="mt-3 grid grid-cols-2 gap-2 !text-xs text-ink-muted">
                <div>
                  <dt class="font-medium text-ink-subtle uppercase">Productos</dt>
                  <dd class="text-sm text-ink">
                    {{ store.conteoProductos(p.id) }}
                    @if (issues) {<span class="text-error-text">({{ issues }} incompletos)</span>}
                  </dd>
                </div>
                <div>
                  <dt class="font-medium text-ink-subtle uppercase">Fecha límite</dt>
                  <dd class="text-sm text-ink">{{ p.fechaLimite || '—' }}</dd>
                </div>
              </dl>

              @if (p.observacionGeneral) {
                <p class="mt-3 rounded-lg bg-error-bg px-3 py-2 !text-xs text-error-text ring-1 ring-error-border">
                  {{ p.observacionGeneral }}
                </p>
              }
            </a>
          } @empty {
            <div class="col-span-full card p-8 text-center text-ink-muted border border-dashed border-border">
              <p class="!text-sm text-ink-muted">No hay planes en este filtro.</p>
            </div>
          }
        </div>
      }
    </app-page-shell>
  `,
})
export class PlanesListComponent {
  protected readonly store = inject(PoaStoreService);
  protected readonly filtros = FILTROS;
  protected readonly filtro = signal<Filtro>('Todos');

  constructor() {
    void this.store.cargar();
  }

  /** Con planning:poa:review (o admin) se ven todas las áreas; el resto solo la propia. */
  protected readonly veTodo = computed(() => this.store.puedeRevisar());

  protected puedeRevisar(): boolean {
    return this.store.puedeRevisar();
  }

  protected readonly descripcion = computed(() => {
    const ciclo = this.store.cicloActivo();
    return this.veTodo()
      ? `Formulación del ${ciclo.nombre} (${ciclo.periodo})`
      : `Formulación del ${ciclo.nombre} · ${this.store.areaActual().nombre}`;
  });

  protected cambiarCiclo(event: Event): void {
    void this.store.seleccionarCiclo((event.target as HTMLSelectElement).value);
  }

  protected readonly visibles = computed(() => {
    const areaId = this.store.persona().areaId;
    if (!this.veTodo()) {
      return this.store.planes().filter((p) => p.areaId === areaId);
    }
    let planes = [...this.store.planes()];
    planes.sort((a, b) => Number(b.areaId === areaId) - Number(a.areaId === areaId));
    const f = this.filtro();
    planes = planes.filter(
      (p) =>
        f === 'Todos' ||
        (f === 'Faltantes' && p.estado === 'Sin iniciar') ||
        (f === 'En borrador' && p.estado === 'Borrador') ||
        (f === 'Observados' && p.estado === 'Observado') ||
        (f === 'Listos para revisar' && p.estado === 'Listo para revisión'),
    );
    return planes;
  });

  protected contar(f: Filtro): number {
    return this.store
      .planes()
      .filter(
        (p) =>
          f === 'Todos' ||
          (f === 'Faltantes' && p.estado === 'Sin iniciar') ||
          (f === 'En borrador' && p.estado === 'Borrador') ||
          (f === 'Observados' && p.estado === 'Observado') ||
          (f === 'Listos para revisar' && p.estado === 'Listo para revisión'),
      ).length;
  }

  protected progreso(planId: string): number {
    const total = this.store.conteoProductos(planId);
    if (!total) return 0;
    const completos = total - this.store.conteoIncompletos(planId);
    return Math.round((completos / total) * 100);
  }

  protected badge(estado: string): string {
    switch (estado) {
      case 'Aprobado':
        return 'badge-success';
      case 'Observado':
        return 'badge-error';
      case 'Listo para revisión':
        return 'badge-info';
      case 'Sin iniciar':
        return 'badge-neutral';
      default:
        return 'badge-warning';
    }
  }
}
