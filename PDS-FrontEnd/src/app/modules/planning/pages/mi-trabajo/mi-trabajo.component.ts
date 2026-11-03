import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PageShellComponent } from '@public-intelligence/ui-core';
import { PlanningStoreService } from '../../data/planning-store.service';

interface ItemTrabajo {
  tipo: 'Plan anual' | 'Requerimiento' | 'Seguimiento';
  nombre: string;
  area: string;
  periodo: string;
  fechaLimite: string;
  estado: string;
  ruta: string;
}

const TABS = ['Todos', 'Pendiente', 'Observado', 'Listo para revisar', 'Aprobado'] as const;
type Tab = (typeof TABS)[number];

/**
 * Bandeja personal: sustituye el seguimiento informal por colas claras de
 * trabajo según la persona activa. Pestañas de estado + tabla filtrable.
 */
@Component({
  selector: 'app-mi-trabajo',
  imports: [PageShellComponent, RouterLink],
  template: `
    <app-page-shell
      title="Mi trabajo"
      [description]="'Tareas y expedientes asignados a ' + store.persona().nombre + ' (' + store.areaActual().corta + ')'"
    >
      <div class="card flex min-h-[60dvh] flex-col p-0">
        <!-- Toolbar -->
        <div class="flex flex-wrap items-center gap-3 border-b border-border px-4 py-3">
          <div class="segmented">
            @for (t of tabs; track t) {
              <button
                type="button"
                class="segmented-item"
                [class.segmented-item-active]="tab() === t"
                (click)="tab.set(t)"
              >
                {{ t }}
                <span class="rounded-full bg-surface-sunken px-1.5 text-[10px] font-bold tabular-nums">
                  {{ contar(t) }}
                </span>
              </button>
            }
          </div>
          <div class="ml-auto flex items-center gap-2">
            <input
              type="search"
              placeholder="Buscar…"
              class="!h-9 w-56"
              [value]="busqueda()"
              (input)="busqueda.set($any($event.target).value)"
            />
            <button type="button" class="btn-secondary btn-sm" (click)="exportado.set(true)">
              Exportar Excel
            </button>
          </div>
        </div>

        @if (exportado()) {
          <div class="mx-4 mt-3 rounded-lg bg-info-bg px-3 py-2 text-sm text-info-text ring-1 ring-info-border">
            (Demo) Se descargaría un Excel con {{ filtrados().length }} registros de su bandeja.
          </div>
        }

        <!-- Tabla -->
        <div class="custom-scrollbar min-h-0 flex-1 overflow-auto">
          <table class="w-full text-sm">
            <thead>
              <tr>
                @for (h of ['Tipo', 'Nombre', 'Área', 'Período', 'Fecha límite', 'Estado', '']; track h) {
                  <th
                    class="sticky top-0 z-10 border-b border-border bg-surface-sunken px-4 py-2 text-left text-xs font-semibold tracking-wide whitespace-nowrap text-ink-muted uppercase"
                  >
                    {{ h }}
                  </th>
                }
              </tr>
            </thead>
            <tbody>
              @for (item of filtrados(); track item.ruta + item.nombre) {
                <tr class="group hover:bg-surface">
                  <td class="border-b border-border px-4 py-2.5 whitespace-nowrap">
                    <span class="badge-neutral">{{ item.tipo }}</span>
                  </td>
                  <td class="max-w-md border-b border-border px-4 py-2.5">
                    <a [routerLink]="item.ruta" class="!text-sm font-medium">{{ item.nombre }}</a>
                  </td>
                  <td class="border-b border-border px-4 py-2.5 whitespace-nowrap text-ink-muted">{{ item.area }}</td>
                  <td class="border-b border-border px-4 py-2.5 whitespace-nowrap text-ink-muted">{{ item.periodo }}</td>
                  <td class="border-b border-border px-4 py-2.5 whitespace-nowrap text-ink-muted">{{ item.fechaLimite }}</td>
                  <td class="border-b border-border px-4 py-2.5 whitespace-nowrap">
                    <span [class]="badge(item.estado)">{{ item.estado }}</span>
                  </td>
                  <td class="border-b border-border px-4 py-2.5 text-right whitespace-nowrap">
                    <a [routerLink]="item.ruta" class="btn-ghost btn-sm !no-underline opacity-0 group-hover:opacity-100">Abrir</a>
                  </td>
                </tr>
              } @empty {
                <tr>
                  <td colspan="7" class="px-4 py-10 text-center text-sm text-ink-muted">
                    No hay elementos en esta bandeja.
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      </div>
    </app-page-shell>
  `,
})
export class MiTrabajoComponent {
  protected readonly store = inject(PlanningStoreService);
  protected readonly tabs = TABS;
  protected readonly tab = signal<Tab>('Todos');
  protected readonly busqueda = signal('');
  protected readonly exportado = signal(false);

  /** Bandeja según persona: planes propios o de todas las áreas, requerimientos y cortes. */
  protected readonly items = computed<ItemTrabajo[]>(() => {
    const persona = this.store.personaId();
    const areaId = this.store.persona().areaId;
    const out: ItemTrabajo[] = [];

    const planes =
      persona === 'planificacion' || persona === 'presupuesto' || persona === 'mae'
        ? this.store.planes()
        : this.store.planes().filter((p) => p.areaId === areaId);
    for (const p of planes) {
      out.push({
        tipo: 'Plan anual',
        nombre: p.nombre,
        area: this.store.area(p.areaId)?.corta ?? '',
        periodo: '2027',
        fechaLimite: p.fechaLimite,
        estado: p.estado,
        // Un plan en ajustes abre directamente la reprogramación guiada (ajuste 12).
        ruta: p.estado === 'En ajustes' ? '/plan-anual/' + p.id + '/ajustes' : '/plan-anual/' + p.id,
      });
    }

    const reqs = this.store.requerimientos().filter((r) => {
      if (r.estado === 'Fusionado') return false;
      if (persona === 'transversal') return this.store.consolidadoraDeCategoria(r.categoriaId)?.id === areaId;
      if (persona === 'sustantiva') return r.origenAreaId === areaId;
      return persona === 'planificacion';
    });
    for (const r of reqs) {
      out.push({
        tipo: 'Requerimiento',
        nombre: `${r.codigo} · ${r.descripcion}`,
        area: this.store.area(r.origenAreaId)?.corta ?? '',
        periodo: '2027',
        fechaLimite: 'Trimestre ' + r.trimestre,
        estado: r.estado,
        ruta: '/requerimientos/' + r.id,
      });
    }

    const cortes = this.store
      .seguimiento()
      .filter((s) => (persona === 'sustantiva' ? s.areaId === areaId : persona === 'planificacion'));
    for (const s of cortes) {
      out.push({
        tipo: 'Seguimiento',
        nombre: s.actividad,
        area: this.store.area(s.areaId)?.corta ?? '',
        periodo: 'Corte jun-2026',
        fechaLimite: s.corte,
        estado: s.estado === 'Pendiente' ? 'Pendiente' : 'Reportado',
        ruta: '/seguimiento/' + s.id,
      });
    }
    return out;
  });

  protected readonly filtrados = computed(() => {
    const q = this.busqueda().toLowerCase();
    return this.items().filter((i) => {
      const matchTab =
        this.tab() === 'Todos' ||
        (this.tab() === 'Pendiente' &&
          ['Borrador', 'Sin iniciar', 'Pendiente', 'Nuevo', 'En revisión', 'Devuelto', 'En ajustes'].includes(i.estado)) ||
        (this.tab() === 'Observado' && i.estado === 'Observado') ||
        (this.tab() === 'Listo para revisar' && i.estado === 'Listo para revisión') ||
        (this.tab() === 'Aprobado' && ['Aprobado', 'Reportado'].includes(i.estado));
      const matchQ = !q || i.nombre.toLowerCase().includes(q) || i.area.toLowerCase().includes(q);
      return matchTab && matchQ;
    });
  });

  protected contar(tab: Tab): number {
    // Conteo sin alterar el estado: replica el filtro por pestaña sobre items()
    const match = (i: ItemTrabajo) =>
      tab === 'Todos' ||
      (tab === 'Pendiente' &&
        ['Borrador', 'Sin iniciar', 'Pendiente', 'Nuevo', 'En revisión', 'Devuelto', 'En ajustes'].includes(i.estado)) ||
      (tab === 'Observado' && i.estado === 'Observado') ||
      (tab === 'Listo para revisar' && i.estado === 'Listo para revisión') ||
      (tab === 'Aprobado' && ['Aprobado', 'Reportado'].includes(i.estado));
    return this.items().filter(match).length;
  }

  protected badge(estado: string): string {
    switch (estado) {
      case 'Aprobado':
      case 'Reportado':
        return 'badge-success';
      case 'Observado':
      case 'Devuelto':
        return 'badge-error';
      case 'Listo para revisión':
      case 'En revisión':
        return 'badge-info';
      case 'Sin iniciar':
        return 'badge-neutral';
      default:
        return 'badge-warning';
    }
  }
}
