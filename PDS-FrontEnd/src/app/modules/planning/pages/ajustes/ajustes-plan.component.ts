import { Component, computed, inject, signal, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';
import { PoaStoreService } from '../../../../services/planning/poa-store.service';
import { RequirementStoreService } from '../../../../services/planning/requirement-store.service';

@Component({
  selector: 'app-ajustes-plan',
  imports: [RouterLink, FormsModule],
  template: `
    @let plan = planActual();
    @if (poaStore.cargando() || !poaStore.cargado()) {
      <div class="card flex flex-col items-center justify-center p-12 text-center text-ink-muted min-h-[350px] m-6">
        <span class="inline-block size-8 animate-spin rounded-full border-4 border-brand border-t-transparent mb-3"></span>
        <p class="text-base font-semibold text-ink">Cargando plan y reprogramación...</p>
        <p class="text-xs text-ink-muted mt-1">Por favor espere mientras se obtienen los datos del servidor.</p>
      </div>
    } @else if (!plan) {
      <div class="card mx-auto my-12 max-w-md p-8 text-center m-6">
        <p class="text-base font-semibold text-ink">Plan no encontrado</p>
        <p class="mt-1 text-xs text-ink-muted">El plan solicitado no existe o no tiene permisos para acceder a él.</p>
        <a routerLink="/plan-anual" class="btn-primary btn-sm mt-4 inline-flex !no-underline">← Volver al listado de planes</a>
      </div>
    } @else {
      <div class="mx-auto max-w-5xl p-6">
        <a [routerLink]="'/plan-anual/' + plan.id" class="!text-sm !text-ink-muted !no-underline hover:!underline">← {{ plan.nombre }}</a>
        <h1 class="!font-poppins mt-1 !text-2xl font-semibold text-ink">Ajustes y reprogramación</h1>
        <p class="mt-1 !text-sm text-ink-muted">
          Estado del plan: <span class="badge-warning">{{ plan.estado }}</span>.
          Revise las decisiones de la consolidadora y reprograme lo necesario.
        </p>

        <div class="card mt-4 p-5">
          <h2 class="!font-poppins !text-base font-semibold text-ink">Requerimientos del área</h2>
          <ul class="mt-3 flex flex-col gap-2">
            @for (r of reqStore.requerimientos(); track r.id) {
              <li class="flex items-center justify-between rounded-lg bg-surface-sunken px-3 py-2 text-sm">
                <div>
                  <span class="font-medium text-ink">{{ r.code }}</span> · {{ r.description }}
                </div>
                <span class="badge-neutral">{{ r.status }}</span>
              </li>
            } @empty {
              <li class="text-sm text-ink-muted">Sin requerimientos registrados.</li>
            }
          </ul>
        </div>

        <div class="mt-4 flex items-center gap-3">
          <button type="button" class="btn-primary" (click)="someter()">
            Guardar y reenviar plan
          </button>
          @if (mensaje()) {
            <span class="!text-sm text-success-text">{{ mensaje() }}</span>
          }
        </div>
      </div>
    }
  `,
})
export class AjustesPlanComponent implements OnInit {
  protected readonly poaStore = inject(PoaStoreService);
  protected readonly reqStore = inject(RequirementStoreService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly mensaje = signal('');

  private readonly planId = toSignal(this.route.paramMap.pipe(map((p) => p.get('planId') ?? '')), {
    initialValue: '',
  });

  protected readonly planActual = computed(() => this.poaStore.plan(this.planId()));

  async ngOnInit(): Promise<void> {
    const id = this.planId();
    if (id) {
      await this.poaStore.cargarWorkspace(id);
      await this.reqStore.cargarPorOrigen('', id);
    }
  }

  protected async someter(): Promise<void> {
    const id = this.planId();
    if (!id) return;
    this.mensaje.set('Plan reenviado a revisión.');
    this.router.navigate(['/plan-anual', id]);
  }
}
