import { Component, computed, effect, inject, signal, OnInit } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';
import { RequirementStoreService } from '../../../../services/planning/requirement-store.service';
import { PlanningContextService } from '../../../../services/planning/planning-context.service';
import { RequirementDetailDto } from '../../data/planning-api.models';

@Component({
  selector: 'app-requerimiento-detalle',
  imports: [RouterLink, FormsModule, DecimalPipe],
  template: `
    @let req = store.requerimientoSeleccionado();
    @if (store.cargando()) {
      <div class="card flex flex-col items-center justify-center p-12 text-center text-ink-muted min-h-[350px] m-6">
        <span class="inline-block size-8 animate-spin rounded-full border-4 border-brand border-t-transparent mb-3"></span>
        <p class="text-base font-semibold text-ink">Cargando requerimiento...</p>
        <p class="text-xs text-ink-muted mt-1">Por favor espere mientras se obtienen los datos del servidor.</p>
      </div>
    } @else if (!req) {
      <div class="card mx-auto my-12 max-w-md p-8 text-center m-6">
        <p class="text-base font-semibold text-ink">Requerimiento no encontrado</p>
        <p class="mt-1 text-xs text-ink-muted">El requerimiento solicitado no existe o no tiene permisos para acceder a él.</p>
        <a routerLink="/requerimientos" class="btn-primary btn-sm mt-4 inline-flex !no-underline">← Volver a la bandeja</a>
      </div>
    } @else {
      <div class="mx-auto max-w-4xl p-6">
        <a routerLink="/requerimientos" class="!text-sm !text-ink-muted !no-underline hover:!underline">← Requerimientos</a>
        <div class="mt-2 flex flex-wrap items-start justify-between gap-3">
          <div>
            <h1 class="!font-poppins !text-2xl font-semibold text-ink">{{ req.code }}</h1>
            <p class="mt-1 !text-sm text-ink-muted">{{ req.description }}</p>
          </div>
          <span [class]="badge(req.status)" class="!text-sm">{{ req.status }}</span>
        </div>

        <div class="mt-4 grid grid-cols-1 gap-4 lg:grid-cols-3">
          <!-- Columna principal -->
          <div class="flex flex-col gap-4 lg:col-span-2">
            <div class="card p-5">
              <h2 class="!font-poppins !text-base font-semibold text-ink">Datos del requerimiento (formato PACC)</h2>
              <dl class="mt-3 grid grid-cols-2 gap-3 !text-sm lg:grid-cols-3">
                <div><dt class="text-xs font-medium text-ink-subtle uppercase">Cantidad</dt><dd class="tabular-nums text-ink">{{ req.quantity }}</dd></div>
                <div><dt class="text-xs font-medium text-ink-subtle uppercase">Unidad de medida</dt><dd class="text-ink">{{ req.unitOfMeasure }}</dd></div>
                <div>
                  <dt class="text-xs font-medium text-ink-subtle uppercase">Precio unitario</dt>
                  <dd class="tabular-nums text-ink">RD$ {{ req.estimatedUnitPrice | number: '1.2-2' }}</dd>
                </div>
                <div><dt class="text-xs font-medium text-ink-subtle uppercase">Monto total</dt><dd class="font-semibold tabular-nums text-ink">RD$ {{ (req.calculatedTotal ?? (req.quantity * req.estimatedUnitPrice)) | number: '1.2-2' }}</dd></div>
                <div><dt class="text-xs font-medium text-ink-subtle uppercase">Trimestre</dt><dd class="text-ink">{{ req.quarter }}</dd></div>
                <div><dt class="text-xs font-medium text-ink-subtle uppercase">Área requiriente</dt><dd class="text-ink">{{ req.originAreaName }}</dd></div>
                <div><dt class="text-xs font-medium text-ink-subtle uppercase">Área consolidadora</dt><dd class="text-ink">{{ req.consolidatorAreaName }}</dd></div>
              </dl>

              @if (req.appliedRule) {
                <div class="mt-4 rounded-lg bg-surface-sunken px-3 py-2.5">
                  <p class="!text-xs font-semibold tracking-wide text-ink-subtle uppercase">Regla de ruteo aplicada</p>
                  <p class="mt-0.5 !text-sm text-ink">{{ req.appliedRule }}</p>
                </div>
              }
            </div>

            <!-- Comentarios -->
            <div class="card p-5">
              <h2 class="!font-poppins !text-base font-semibold text-ink">Comentarios entre áreas</h2>
              <ul class="mt-3 flex flex-col gap-3">
                @for (c of req.comments; track c.id) {
                  <li class="rounded-lg bg-surface-sunken px-3 py-2.5">
                    <p class="!text-sm text-ink">{{ c.text }}</p>
                    <p class="mt-1 !text-xs text-ink-subtle">{{ c.userName ?? 'Usuario' }} ({{ c.areaName ?? 'Área' }}) · {{ c.date }}</p>
                  </li>
                } @empty {
                  <li class="!text-sm text-ink-muted">Sin comentarios todavía.</li>
                }
              </ul>
              <div class="mt-3 flex gap-2">
                <input class="!h-10 flex-1" placeholder="Escribir un comentario…" [(ngModel)]="comentario" (keydown.enter)="comentar()" />
                <button type="button" class="btn-secondary" [disabled]="!comentario.trim()" (click)="comentar()">Comentar</button>
              </div>
            </div>
          </div>

          <!-- Panel de decisión -->
          <div class="flex flex-col gap-4">
            <div class="card p-5">
              <h2 class="!font-poppins !text-base font-semibold text-ink">Decisión</h2>
              <div class="mt-3 flex flex-col gap-2">
                <button type="button" class="btn-primary w-full" (click)="decidir('Approve')">Aprobar</button>
                <button type="button" class="btn-danger w-full" (click)="devolviendo.set(!devolviendo())">Devolver</button>
              </div>
              @if (devolviendo()) {
                <div class="mt-3 flex flex-col gap-2">
                  <textarea class="!h-20 w-full rounded-lg border border-border-strong px-3 py-2 !text-sm" placeholder="Motivo de la devolución" [(ngModel)]="motivoDevolucion"></textarea>
                  <button type="button" class="btn-danger btn-sm" [disabled]="!motivoDevolucion.trim()" (click)="confirmarDevolucion()">Confirmar devolución</button>
                </div>
              }
              @if (decidido()) {
                <p class="mt-3 rounded-lg bg-success-bg px-3 py-2 !text-sm text-success-text ring-1 ring-success-border">{{ decidido() }}</p>
              }
            </div>
          </div>
        </div>
      </div>
    }
  `,
})
export class RequerimientoDetalleComponent implements OnInit {
  protected readonly store = inject(RequirementStoreService);
  protected readonly planningContext = inject(PlanningContextService);
  private readonly route = inject(ActivatedRoute);

  protected comentario = '';
  protected motivoDevolucion = '';
  protected readonly devolviendo = signal(false);
  protected readonly decidido = signal('');

  private readonly reqId = toSignal(this.route.paramMap.pipe(map((p) => p.get('reqId') ?? '')), {
    initialValue: '',
  });

  async ngOnInit(): Promise<void> {
    const id = this.reqId();
    if (id) {
      await this.store.cargarDetalle(id);
    }
  }

  protected async comentar(): Promise<void> {
    const id = this.reqId();
    if (!id || !this.comentario.trim()) return;
    await this.store.agregarComentario(id, this.comentario.trim());
    this.comentario = '';
  }

  /**
   * El backend solo acepta Approve | Return | Reschedule y exige un comentario para
   * la bitácora. Fusionar y eliminar son operaciones aparte (`/merge`, `DELETE`).
   */
  protected async decidir(action: 'Approve' | 'Return' | 'Reschedule'): Promise<void> {
    const id = this.reqId();
    if (!id) return;
    await this.store.decidir(id, { action, comment: 'Decisión desde el detalle del requerimiento' });
    this.decidido.set('Requerimiento actualizado.');
  }

  protected async confirmarDevolucion(): Promise<void> {
    const id = this.reqId();
    if (!id || !this.motivoDevolucion.trim()) return;
    await this.store.decidir(id, { action: 'Return', comment: this.motivoDevolucion.trim() });
    this.decidido.set('Requerimiento devuelto al área requiriente.');
    this.devolviendo.set(false);
    this.motivoDevolucion = '';
  }

  protected badge(estado: string): string {
    switch (estado) {
      case 'Approved':
      case 'Aprobado':
        return 'badge-success';
      case 'Returned':
      case 'Devuelto':
        return 'badge-error';
      case 'InReview':
      case 'En revisión':
        return 'badge-info';
      default:
        return 'badge-warning';
    }
  }
}
