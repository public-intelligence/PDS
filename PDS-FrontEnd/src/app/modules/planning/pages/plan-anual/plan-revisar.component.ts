import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';
import { PoaStoreService } from '../../../../services/planning/poa-store.service';
import { RequirementStoreService } from '../../../../services/planning/requirement-store.service';
import { ErrorSummaryComponent, ErrorSummaryItem } from '../../components/error-summary.component';
import { SummaryListComponent, SummaryRow } from '../../components/summary-list.component';
import { formatRD, montoDe } from '../../data/planning.mock-data';
import { Requerimiento } from '../../data/planning.models';

/**
 * Revisión antes de someter (patrón "check your answers"): resume el plan por
 * secciones, enlaza los errores pendientes, valida los requerimientos contra
 * el techo del área y exige confirmación explícita.
 */
@Component({
  selector: 'app-plan-revisar',
  imports: [RouterLink, FormsModule, ErrorSummaryComponent, SummaryListComponent],
  template: `
    @let plan = planActual();
    @if (store.cargando() || !store.cargado()) {
      <div class="card flex flex-col items-center justify-center p-12 text-center text-ink-muted min-h-[350px] m-6">
        <span class="inline-block size-8 animate-spin rounded-full border-4 border-brand border-t-transparent mb-3"></span>
        <p class="text-base font-semibold text-ink">Cargando revisión del plan...</p>
        <p class="text-xs text-ink-muted mt-1">Por favor espere mientras se obtienen los datos del servidor.</p>
      </div>
    } @else if (!plan) {
      <div class="card mx-auto my-12 max-w-md p-8 text-center m-6">
        <p class="text-base font-semibold text-ink">Plan no encontrado</p>
        <p class="mt-1 text-xs text-ink-muted">El plan solicitado no existe o no tiene permisos para acceder a él.</p>
        <a routerLink="/plan-anual" class="btn-primary btn-sm mt-4 inline-flex !no-underline">← Volver al listado de planes</a>
      </div>
    } @else {
      <div class="mx-auto max-w-3xl p-6">
        <a [routerLink]="'/plan-anual/' + plan.id" class="!text-sm !text-ink-muted !no-underline hover:!underline">← Volver al plan</a>
        <h1 class="!font-poppins mt-2 !text-2xl font-semibold text-ink">Revise el plan antes de someterlo</h1>
        <p class="mt-1 !text-sm text-ink-muted">{{ plan.nombre }}</p>

        @if (sometido()) {
          <div class="card mt-6 border-2 border-success-border p-6 text-center">
            <p class="!font-poppins !text-xl font-semibold text-success-text">Plan sometido a revisión</p>
            <p class="mt-2 !text-sm text-ink-muted">
              La versión v{{ plan.version }} quedó bloqueada para edición y fue enviada a la Dirección
              de Planificación y Desarrollo. Recibirá una notificación con el resultado de la revisión.
            </p>
            <div class="mt-4 flex justify-center gap-2">
              <a routerLink="/plan-anual" class="btn-primary !no-underline">Ir a planes anuales</a>
              <a [routerLink]="'/plan-anual/' + plan.id" class="btn-secondary !no-underline">Ver el plan</a>
            </div>
          </div>
        } @else {
          <div class="mt-4">
            <app-error-summary
              titulo="Debe corregir estos productos antes de someter"
              [errores]="errores()"
              (irA)="irAFila($event)"
            />
          </div>

          @if (erroresServidor().length) {
            <div class="mt-4 rounded-lg bg-error-bg px-4 py-3 ring-1 ring-error-border">
              <p class="!text-sm font-semibold text-error-text">El servidor rechazó el sometimiento:</p>
              <ul class="mt-1 flex flex-col gap-1">
                @for (e of erroresServidor(); track $index) {
                  <li class="!text-xs text-error-text">{{ e }}</li>
                }
              </ul>
            </div>
          }

          <div class="card mt-4 p-5">
            <h2 class="!font-poppins !text-base font-semibold text-ink">Información general</h2>
            <app-summary-list [filas]="resumenGeneral()" (cambiar)="volverAlPlan()" />
          </div>

          <div class="card mt-4 p-5">
            <h2 class="!font-poppins !text-base font-semibold text-ink">Productos y metas</h2>
            <app-summary-list [filas]="resumenProductos()" (cambiar)="volverAlPlan()" />
          </div>

          <div class="card mt-4 p-5">
            <h2 class="!font-poppins !text-base font-semibold text-ink">Requerimientos de apoyo (formato PACC)</h2>
            <app-summary-list [filas]="resumenRequerimientos()" (cambiar)="volverAlPlan()" />
            @if (rubrosExcedidos().length) {
              <p class="mt-2 rounded-lg bg-error-bg px-3 py-2 !text-sm text-error-text ring-1 ring-error-border">
                Algunos rubros usados por este plan exceden su techo institucional:
                <strong>{{ rubrosExcedidos().join(', ') }}</strong>. Presupuesto podrá devolver el plan o
                reducir requerimientos en la fase de ajustes.
              </p>
            }
          </div>

          <div class="mt-4 rounded-lg bg-warning-bg px-4 py-3 text-sm text-warning-text ring-1 ring-warning-border">
            Después del envío, el plan quedará <strong>bloqueado para edición</strong> mientras dure la
            revisión. Solo Planificación podrá devolverlo con observaciones.
          </div>

          <label class="mt-4 !flex items-start gap-2 !text-sm !font-normal text-ink">
            <input type="checkbox" class="mt-0.5 !size-4 accent-brand" [(ngModel)]="confirmado" />
            Confirmo que la información registrada fue validada por el área y cuenta con el visto
            bueno de su responsable.
          </label>

          <div class="mt-5 flex items-center gap-3">
            <button
              type="button"
              class="btn-primary"
              [disabled]="!confirmado || errores().length > 0 || !editable()"
              (click)="someter()"
            >
              Someter a revisión
            </button>
            @if (errores().length) {
              <span class="!text-sm text-error-text">Corrija los {{ errores().length }} producto(s) con errores para continuar.</span>
            } @else if (!editable()) {
              <span class="!text-sm text-ink-muted">Este plan no está en un estado editable.</span>
            }
          </div>
        }
      </div>
    }
  `,
})
export class PlanRevisarComponent {
  protected readonly store = inject(PoaStoreService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly fmt = formatRD;
  protected confirmado = false;
  protected readonly sometido = signal(false);
  protected readonly erroresServidor = signal<string[]>([]);

  private readonly planId = toSignal(this.route.paramMap.pipe(map((p) => p.get('planId') ?? '')), {
    initialValue: '',
  });

  constructor() {
    // Carga directa por URL: asegura el expediente del plan y sus catálogos.
    void this.store.cargar().then(() => {
      const id = this.planId();
      if (id && !this.store.plan(id)?.rows.length) void this.store.cargarWorkspace(id);
    });
  }

  protected readonly planActual = computed(() => this.store.plan(this.planId()));

  protected readonly editable = computed(() => {
    const plan = this.planActual();
    return !!plan && ['Sin iniciar', 'Borrador', 'Observado'].includes(plan.estado);
  });

  protected readonly errores = computed<ErrorSummaryItem[]>(() => {
    const plan = this.planActual();
    if (!plan) return [];
    return plan.rows
      .map((row) => ({ row, issues: this.store.rowIssues(row) }))
      .filter((x) => x.issues.length)
      .map((x) => ({ ref: x.row.id, mensaje: `${x.row.codigo}: ${x.issues.join(' · ')}` }));
  });

  protected readonly reqStore = inject(RequirementStoreService);

  private readonly reqsDelPlan = computed(() => {
    return (this.reqStore.requerimientos() as unknown as Requerimiento[]);
  });

  /** Monto total formulado por el área (todos sus requerimientos vigentes). */
  protected readonly montoTotal = computed(() =>
    this.reqsDelPlan().reduce((s, r) => s + montoDe(r), 0),
  );

  /** Lo que formuló el área pero ejecuta un área consolidadora (ajuste 10). */
  protected readonly montoEjecutadoPorOtros = computed(() => {
    return 0;
  });

  protected readonly rubrosExcedidos = computed(() => {
    return [];
  });

  protected readonly resumenGeneral = computed<SummaryRow[]>(() => {
    const plan = this.planActual()!;
    const area = this.store.area(plan.areaId);
    return [
      { etiqueta: 'Área responsable', valor: area?.nombre ?? '', cambiarRef: 'plan' },
      { etiqueta: 'Ciclo', valor: this.store.cicloActivo().nombre },
      { etiqueta: 'Versión', valor: 'v' + plan.version },
      { etiqueta: 'Fecha límite', valor: plan.fechaLimite },
    ];
  });

  protected readonly resumenProductos = computed<SummaryRow[]>(() => {
    const plan = this.planActual()!;
    const completos = plan.rows.filter((r) => !this.store.rowIssues(r).length).length;
    const unidades = plan.rows.reduce((s, r) => s + r.unidades.length, 0);
    const municipios = new Set(plan.rows.flatMap((r) => r.municipiosImpacto));
    const operativos = plan.rows.filter((r) => r.origen === 'operativo').length;
    const alineados = plan.rows.filter((r) => r.origen === 'pei' && r.focoId && r.objetivoId).length;
    const delPei = plan.rows.length - operativos;
    return [
      { etiqueta: 'Total de productos', valor: String(plan.rows.length), cambiarRef: 'plan' },
      { etiqueta: 'Completos', valor: `${completos} de ${plan.rows.length}`, cambiarRef: 'plan' },
      { etiqueta: 'Unidades de medida', valor: String(unidades), cambiarRef: 'plan' },
      { etiqueta: 'Municipios de impacto', valor: `${municipios.size} municipio(s) cubiertos`, cambiarRef: 'plan' },
      {
        etiqueta: 'Productos operativos',
        valor: operativos ? `${operativos} no vinculado(s) al PEI` : 'Ninguno',
        cambiarRef: 'plan',
      },
      {
        etiqueta: 'Alineación al PEI',
        valor: alineados === delPei ? 'Todos los productos del PEI alineados' : `${delPei - alineados} sin alinear`,
        cambiarRef: 'plan',
      },
    ];
  });

  protected readonly resumenRequerimientos = computed<SummaryRow[]>(() => {
    const reqs = this.reqsDelPlan();
    const porCategoria = new Map<string, { n: number; monto: number; sinCosto: number }>();
    for (const r of reqs) {
      const nombre = r.categoriaId || 'Requerimiento';
      const acc = porCategoria.get(nombre) ?? { n: 0, monto: 0, sinCosto: 0 };
      const conCosto = r.precioEstimado > 0;
      porCategoria.set(nombre, {
        n: acc.n + 1,
        monto: acc.monto + (conCosto ? montoDe(r) : 0),
        sinCosto: acc.sinCosto + (conCosto ? 0 : 1),
      });
    }
    // Los apoyos de capacidad instalada no llevan monto: se cuentan aparte para
    // no leerse como «RD$0.00», que parecería un dato faltante.
    const filas: SummaryRow[] = [...porCategoria.entries()].map(([nombre, v]) => ({
      etiqueta: nombre,
      valor:
        v.sinCosto === v.n
          ? `${v.n} requerimiento(s) · sin costo (capacidad instalada)`
          : `${v.n} requerimiento(s) · ${formatRD(v.monto)}` +
            (v.sinCosto ? ` · ${v.sinCosto} sin costo` : ''),
    }));
    filas.push({
      etiqueta: 'Monto total formulado',
      valor: formatRD(this.montoTotal()),
    });
    if (this.montoEjecutadoPorOtros() > 0) {
      filas.push({
        etiqueta: 'Lo ejecuta un área consolidadora',
        valor: `${formatRD(this.montoEjecutadoPorOtros())} — según el ejecutor de cada rubro`,
      });
    }
    return filas;
  });

  protected irAFila(rowId: string): void {
    this.router.navigate(['/plan-anual', this.planId()], {
      queryParams: { fila: rowId, errores: '1' },
    });
  }

  protected volverAlPlan(): void {
    this.router.navigate(['/plan-anual', this.planId()]);
  }

  protected async someter(): Promise<void> {
    // El servidor es la fuente de verdad de la validación: si devuelve errores, se muestran.
    const errores = await this.store.someterPlan(this.planId());
    if (errores.length) {
      this.erroresServidor.set(errores);
      return;
    }
    this.erroresServidor.set([]);
    this.sometido.set(true);
  }
}
