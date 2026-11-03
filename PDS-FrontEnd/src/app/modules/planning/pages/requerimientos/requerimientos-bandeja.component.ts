import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { PageShellComponent } from '@public-intelligence/ui-core';
import { RequirementStoreService } from '../../../../services/planning/requirement-store.service';
import { PlanningContextService } from '../../../../services/planning/planning-context.service';
import { PlanningCycleStoreService } from '../../../../services/planning/planning-cycle-store.service';
import { NotificationService } from '../../../../services/notification/notification.service';

/** Vistas de la pantalla. La consolidadora ve su demanda entrante, no la que ella pide. */
type Vista = 'origen' | 'consolidadora' | 'articulo';

@Component({
  selector: 'app-requerimientos-bandeja',
  imports: [PageShellComponent, RouterLink, DecimalPipe, FormsModule],
  template: `
    <app-page-shell title="Requerimientos transversales" [description]="descripcion()">
      <div class="mb-4 rounded-lg bg-surface-sunken px-3 py-2 text-sm text-ink-muted">
        Un requerimiento es una solicitud de apoyo de un área a otra (no solo compras): transporte,
        eventos, soporte TIC, mobiliario o contrataciones. Cuando implica adquisición, el área
        consolidadora lo convierte en insumo del <strong>pre-PACC</strong> institucional.
      </div>

      <div class="card mt-4 flex min-h-[50dvh] flex-col p-0">
        <div class="flex flex-wrap items-center gap-3 border-b border-border px-4 py-3">
          <div class="segmented">
            <button type="button" class="segmented-item" [class.segmented-item-active]="vista() === 'origen'" (click)="verOrigen()">
              Mis requerimientos
            </button>
            <button type="button" class="segmented-item" [class.segmented-item-active]="vista() === 'consolidadora'" (click)="verConsolidadora()">
              Bandeja consolidadora
            </button>
            <button type="button" class="segmented-item" [class.segmented-item-active]="vista() === 'articulo'" (click)="verPorArticulo()">
              Tablero por artículo
            </button>
          </div>

          @if (vista() === 'consolidadora' && seleccion().length > 1) {
            <button type="button" class="btn-accent btn-sm" (click)="abrirFusion()">
              Fusionar {{ seleccion().length }} seleccionados
            </button>
          }
          @if (mensaje()) {
            <span class="rounded-lg bg-success-bg px-3 py-1.5 text-sm text-success-text ring-1 ring-success-border">{{ mensaje() }}</span>
          }
        </div>

        <!-- Diálogo de fusión: el backend exige una descripción para el consolidado. -->
        @if (fusionAbierta()) {
          <div class="slide-in-top border-b border-border bg-surface-sunken px-4 py-3">
            <p class="!text-sm font-semibold text-ink">Fusionar {{ seleccion().length }} requerimientos</p>
            <p class="mt-0.5 !text-xs text-ink-muted">
              Se creará un requerimiento consolidado en revisión y los originales quedarán enlazados como
              fusionados. Solo se pueden fusionar requerimientos del mismo artículo, ciclo y unidad de medida.
            </p>
            <div class="mt-2 flex flex-wrap items-end gap-2">
              <label class="field-label flex-1 min-w-[16rem]">Descripción del consolidado
                <input class="field-control mt-1" [(ngModel)]="fusionDescripcion" placeholder="Ej. Monitores 27 pulgadas — consolidado institucional" />
              </label>
              <label class="field-label flex-1 min-w-[16rem]">Motivo (opcional)
                <input class="field-control mt-1" [(ngModel)]="fusionMotivo" placeholder="Ej. Economía de escala" />
              </label>
              <button type="button" class="btn-primary btn-sm" [disabled]="!fusionDescripcion.trim() || ocupado()" (click)="fusionar()">
                {{ ocupado() ? 'Fusionando…' : 'Fusionar' }}
              </button>
              <button type="button" class="btn-ghost btn-sm" (click)="fusionAbierta.set(false)">Cancelar</button>
            </div>
          </div>
        }

        @if (error()) {
          <p class="border-b border-border bg-danger-bg px-4 py-2 text-sm text-danger-text">{{ error() }}</p>
        }

        @if (vista() === 'articulo') {
          <div class="custom-scrollbar min-h-0 flex-1 overflow-auto p-4">
            @if (store.cargando()) {
              <p class="py-10 text-center text-sm text-ink-muted">Cargando tablero por artículo…</p>
            } @else if (store.tableroPorArticulo(); as board) {
              <div class="mb-4 flex items-center justify-between">
                <div>
                  <h3 class="!font-poppins text-base font-semibold text-ink">Demanda agrupada por artículo</h3>
                  <p class="text-xs text-ink-muted">
                    {{ board.totalProducts }} artículos · {{ board.totalRequirements }} requerimientos ·
                    RD$ {{ board.totalAmount | number: '1.2-2' }}
                  </p>
                </div>
              </div>
              <table class="w-full text-sm">
                <thead>
                  <tr class="border-b border-border bg-surface-sunken text-left">
                    <th class="px-3 py-2 text-xs font-semibold uppercase text-ink-muted">Artículo</th>
                    <th class="px-3 py-2 text-xs font-semibold uppercase text-ink-muted">UNSPSC / Objeto</th>
                    <th class="px-3 py-2 text-right text-xs font-semibold uppercase text-ink-muted">Cantidad</th>
                    <th class="px-3 py-2 text-right text-xs font-semibold uppercase text-ink-muted">Reqs.</th>
                    <th class="px-3 py-2 text-right text-xs font-semibold uppercase text-ink-muted">Áreas</th>
                    <th class="px-3 py-2 text-right text-xs font-semibold uppercase text-ink-muted">Monto</th>
                  </tr>
                </thead>
                <tbody>
                  @for (g of board.productGroups; track g.masterProductId) {
                    <tr class="border-b border-border last:border-b-0 hover:bg-surface-sunken">
                      <td class="px-3 py-2.5">
                        <span class="font-medium text-ink">{{ g.masterProductName }}</span>
                        <span class="block !text-xs text-ink-subtle">{{ g.masterProductCode }}</span>
                      </td>
                      <td class="px-3 py-2.5 !text-xs text-ink-muted">
                        {{ g.unspscCode || '—' }} · {{ g.expenseObjectCode || 'sin objeto' }}
                      </td>
                      <td class="px-3 py-2.5 text-right tabular-nums">{{ g.totalQuantity | number }}</td>
                      <td class="px-3 py-2.5 text-right tabular-nums">{{ g.requirementCount }}</td>
                      <td class="px-3 py-2.5 text-right tabular-nums">{{ g.requestingAreaCount }}</td>
                      <td class="px-3 py-2.5 text-right font-semibold tabular-nums">RD$ {{ g.totalAmount | number: '1.2-2' }}</td>
                    </tr>
                  } @empty {
                    <tr><td colspan="6" class="px-4 py-10 text-center text-sm text-ink-muted">Sin demanda registrada en este ciclo.</td></tr>
                  }
                </tbody>
              </table>
            } @else {
              <p class="py-10 text-center text-sm text-ink-muted">Sin datos de consolidación disponibles.</p>
            }
          </div>
        } @else {
          <div class="custom-scrollbar min-h-0 flex-1 overflow-auto">
            <table class="w-full min-w-[1200px] text-sm">
              <thead>
                <tr>
                  <th class="w-10 border-b border-border bg-surface-sunken px-3 py-2"></th>
                  @for (h of columnas(); track h) {
                    <th class="sticky top-0 z-10 border-b border-border bg-surface-sunken px-3 py-2 text-left text-xs font-semibold tracking-wide whitespace-nowrap text-ink-muted uppercase">{{ h }}</th>
                  }
                </tr>
              </thead>
              <tbody>
                @if (store.cargando()) {
                  <tr>
                    <td [attr.colspan]="columnas().length + 1" class="px-4 py-16 text-center text-sm text-ink-muted">
                      <span class="inline-block size-7 animate-spin rounded-full border-3 border-brand border-t-transparent mb-2"></span>
                      <p class="font-medium text-ink">Cargando requerimientos transversales...</p>
                    </td>
                  </tr>
                } @else {
                  @for (r of store.requerimientos(); track r.id) {
                    <tr class="group hover:bg-surface">
                      <td class="border-b border-border px-3 py-2.5">
                        @if (vista() === 'consolidadora' && r.status !== 'Merged') {
                          <input type="checkbox" class="!size-4 accent-brand" [checked]="seleccion().includes(r.id)" (change)="toggleSeleccion(r.id)" />
                        }
                      </td>
                      <td class="border-b border-border px-3 py-2.5 font-medium whitespace-nowrap">{{ r.code }}</td>
                      <td class="border-b border-border px-3 py-2.5 whitespace-nowrap text-ink-muted">
                        {{ vista() === 'consolidadora' ? r.originAreaName : r.consolidatorAreaName }}
                      </td>
                      <td class="max-w-sm border-b border-border px-3 py-2.5">
                        <a [routerLink]="'/requerimientos/' + r.id" class="!text-sm font-medium text-brand hover:underline">{{ r.masterProductName }}</a>
                        <span class="block truncate !text-xs text-ink-subtle">{{ r.description }}</span>
                        @if (r.appliedRule) {
                          <span class="block truncate !text-xs text-ink-subtle" [title]="r.appliedRule">{{ r.appliedRule }}</span>
                        }
                      </td>
                      <td class="border-b border-border px-3 py-2.5 !text-xs whitespace-nowrap text-ink-muted">
                        {{ r.unspscCode || '—' }}
                        <span class="block">{{ r.expenseObjectCode || 'sin objeto' }}</span>
                      </td>
                      <td class="border-b border-border px-3 py-2.5 text-right whitespace-nowrap tabular-nums">{{ r.quantity }}</td>
                      <td class="border-b border-border px-3 py-2.5 whitespace-nowrap text-ink-muted">{{ r.unitOfMeasure }}</td>
                      <td class="border-b border-border px-3 py-2.5 text-right whitespace-nowrap tabular-nums">
                        RD$ {{ r.estimatedUnitPrice | number: '1.2-2' }}
                        @if (!r.priceConfirmed) { <span class="badge-warning ml-1 !text-[10px]">estimado</span> }
                      </td>
                      <td class="border-b border-border px-3 py-2.5 text-right font-semibold whitespace-nowrap tabular-nums">
                        RD$ {{ r.calculatedTotal | number: '1.2-2' }}
                      </td>
                      <td class="border-b border-border px-3 py-2.5 whitespace-nowrap"><span class="badge-neutral">{{ r.quarter }}</span></td>
                      <td class="border-b border-border px-3 py-2.5 whitespace-nowrap">
                        <span [class]="badge(r.status)">{{ etiquetaEstado(r.status) }}</span>
                      </td>
                      <td class="border-b border-border px-3 py-2.5 text-right whitespace-nowrap">
                        @if (vista() === 'consolidadora' && puedeDecidir(r.status)) {
                          <button type="button" class="btn-ghost btn-sm" [disabled]="ocupado()" (click)="confirmarPrecio(r.id, r.estimatedUnitPrice)">Precio</button>
                          <button type="button" class="btn-ghost btn-sm text-success" [disabled]="ocupado()" (click)="decidir(r.id, 'Approve')">Aprobar</button>
                          <button type="button" class="btn-ghost btn-sm" [disabled]="ocupado()" (click)="reducir(r.id, r.calculatedTotal)">Reducir</button>
                          <button type="button" class="btn-ghost btn-sm text-danger" [disabled]="ocupado()" (click)="decidir(r.id, 'Return')">Devolver</button>
                          <button type="button" class="btn-ghost btn-sm" [disabled]="ocupado()" (click)="decidir(r.id, 'Reschedule')">Reprogramar</button>
                        } @else {
                          <a [routerLink]="'/requerimientos/' + r.id" class="btn-ghost btn-sm !no-underline opacity-0 group-hover:opacity-100">Abrir</a>
                        }
                      </td>
                    </tr>
                  } @empty {
                    <tr><td [attr.colspan]="columnas().length + 1" class="px-4 py-10 text-center text-sm text-ink-muted">No hay requerimientos en esta vista.</td></tr>
                  }
                }
              </tbody>
            </table>
          </div>
        }
      </div>
    </app-page-shell>
  `,
})
export class RequerimientosBandejaComponent implements OnInit {
  protected readonly store = inject(RequirementStoreService);
  protected readonly planningContext = inject(PlanningContextService);
  private readonly cicloStore = inject(PlanningCycleStoreService);
  private readonly notification = inject(NotificationService);

  protected readonly vista = signal<Vista>('origen');
  protected readonly seleccion = signal<string[]>([]);
  protected readonly mensaje = signal('');
  protected readonly error = signal('');
  protected readonly ocupado = signal(false);
  protected readonly fusionAbierta = signal(false);

  protected fusionDescripcion = '';
  protected fusionMotivo = '';

  protected readonly descripcion = computed(() => {
    const ctx = this.planningContext.context();
    return ctx ? `Área activa: ${ctx.planAreaName ?? ctx.unitAreaName ?? 'Sin área'}` : 'Cargando requerimientos...';
  });

  protected readonly columnas = computed(() => [
    'Código',
    this.vista() === 'consolidadora' ? 'Área requiriente' : 'Consolidador',
    'Artículo',
    'UNSPSC / Objeto',
    'Cantidad',
    'Unidad',
    'Precio est.',
    'Monto total',
    'Trimestre',
    'Estado',
    '',
  ]);

  async ngOnInit(): Promise<void> {
    await this.cicloStore.cargarCiclos();
    await this.verOrigen();
  }

  private areaId(): string {
    const ctx = this.planningContext.context();
    return ctx?.planAreaId ?? ctx?.unitAreaId ?? '';
  }

  protected async verOrigen(): Promise<void> {
    this.vista.set('origen');
    this.seleccion.set([]);
    const areaId = this.areaId();
    if (!areaId) return;
    await this.store.cargarPorOrigen(this.cicloStore.cicloActivoId(), areaId);
  }

  protected async verConsolidadora(): Promise<void> {
    this.vista.set('consolidadora');
    this.seleccion.set([]);
    const areaId = this.areaId();
    if (!areaId) return;
    await this.store.cargarPorConsolidador(this.cicloStore.cicloActivoId(), areaId);
  }

  protected async verPorArticulo(): Promise<void> {
    this.vista.set('articulo');
    await this.store.cargarTableroPorArticulo(this.cicloStore.cicloActivoId());
  }

  protected toggleSeleccion(id: string): void {
    this.seleccion.update((ids) => (ids.includes(id) ? ids.filter((x) => x !== id) : [...ids, id]));
  }

  protected abrirFusion(): void {
    this.fusionDescripcion = '';
    this.fusionMotivo = '';
    this.error.set('');
    this.fusionAbierta.set(true);
  }

  protected async fusionar(): Promise<void> {
    await this.ejecutar(async () => {
      const consolidado = await this.store.fusionar({
        requirementIds: this.seleccion(),
        newDescription: this.fusionDescripcion.trim(),
        reason: this.fusionMotivo.trim() || null,
      });
      this.fusionAbierta.set(false);
      this.seleccion.set([]);
      this.notificar(`Requerimientos fusionados en ${consolidado.code}.`);
    });
  }

  protected async confirmarPrecio(id: string, precioActual: number): Promise<void> {
    const entrada = window.prompt('Precio unitario confirmado (RD$):', String(precioActual));
    if (entrada === null) return;
    const price = Number(entrada);
    if (!Number.isFinite(price) || price <= 0) {
      this.error.set('El precio debe ser un número mayor que cero.');
      return;
    }
    await this.ejecutar(async () => {
      await this.store.confirmarPrecio(id, { price });
      this.notificar('Precio confirmado.');
    });
  }

  protected async reducir(id: string, montoActual: number): Promise<void> {
    const entrada = window.prompt('Monto aprobado (RD$):', String(montoActual));
    if (entrada === null) return;
    const approvedAmount = Number(entrada);
    if (!Number.isFinite(approvedAmount) || approvedAmount <= 0) {
      this.error.set('El monto aprobado debe ser un número mayor que cero.');
      return;
    }
    const reason = window.prompt('Motivo de la reducción:') ?? '';
    if (!reason.trim()) {
      this.error.set('La reducción requiere un motivo.');
      return;
    }
    await this.ejecutar(async () => {
      await this.store.reducir(id, { approvedAmount, reason: reason.trim() });
      this.notificar('Requerimiento reducido.');
    });
  }

  protected async decidir(id: string, action: 'Approve' | 'Return' | 'Reschedule'): Promise<void> {
    const etiquetas = { Approve: 'aprobación', Return: 'devolución', Reschedule: 'reprogramación' };
    const comment = window.prompt(`Comentario de la ${etiquetas[action]}:`) ?? '';
    if (!comment.trim()) {
      this.error.set('La decisión requiere un comentario para la bitácora.');
      return;
    }

    let quarter: string | null = null;
    if (action === 'Reschedule') {
      quarter = window.prompt('Nuevo trimestre (Q1, Q2, Q3 o Q4):');
      if (!quarter || !['Q1', 'Q2', 'Q3', 'Q4'].includes(quarter)) {
        this.error.set('El trimestre debe ser Q1, Q2, Q3 o Q4.');
        return;
      }
    }

    await this.ejecutar(async () => {
      await this.store.decidir(id, { action, comment: comment.trim(), quarter });
      this.notificar('Decisión registrada.');
    });
  }

  private async ejecutar(accion: () => Promise<void>): Promise<void> {
    this.ocupado.set(true);
    this.error.set('');
    try {
      await accion();
    } catch {
      this.error.set('No se pudo completar la operación. Verifique el estado del requerimiento e intente de nuevo.');
    } finally {
      this.ocupado.set(false);
    }
  }

  private notificar(texto: string): void {
    this.mensaje.set(texto);
    this.notification.success(texto);
    setTimeout(() => this.mensaje.set(''), 4000);
  }

  protected puedeDecidir(estado: string): boolean {
    return estado === 'New' || estado === 'InReview';
  }

  protected etiquetaEstado(estado: string): string {
    const mapa: Record<string, string> = {
      New: 'Nuevo',
      InReview: 'En revisión',
      Approved: 'Aprobado',
      Reduced: 'Reducido',
      Returned: 'Devuelto',
      Merged: 'Fusionado',
      Rescheduled: 'Reprogramado',
    };
    return mapa[estado] ?? estado;
  }

  protected badge(estado: string): string {
    switch (estado) {
      case 'Approved':
        return 'badge-success';
      case 'Returned':
        return 'badge-error';
      case 'InReview':
        return 'badge-info';
      case 'Merged':
      case 'Rescheduled':
        return 'badge-neutral';
      default:
        return 'badge-warning';
    }
  }
}
