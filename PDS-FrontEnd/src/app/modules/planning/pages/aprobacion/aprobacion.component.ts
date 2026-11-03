import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { PageShellComponent } from '@public-intelligence/ui-core';
import { PlanningStoreService } from '../../data/planning-store.service';
import { formatRD, montoDe } from '../../data/planning.mock-data';
import { ArtefactoAprobacion, NivelAprobador } from '../../data/planning.models';
import { SummaryListComponent, SummaryRow } from '../../components/summary-list.component';

/**
 * Vista ejecutiva de aprobación (ajuste 15): el POA y el PACC se aprueban por
 * separado, con su propio sello (usuario, cargo, nivel, fecha, versión). La
 * aprobación puede recaer en la MAE o en el nivel ejecutivo que corresponda; el
 * PACC exige que el POA ya esté aprobado.
 */
@Component({
  selector: 'app-aprobacion',
  imports: [PageShellComponent, RouterLink, FormsModule, SummaryListComponent],
  template: `
    <app-page-shell
      title="Aprobación institucional"
      [description]="'Expediente de aprobación del ' + store.cicloActivo().nombre + ' · POA y PACC'"
    >
      <!-- Resumen ejecutivo -->
      <div class="grid grid-cols-2 gap-3 lg:grid-cols-4">
        <div class="stat-card">
          <p class="stat-label">Áreas con plan sometido</p>
          <p class="stat-value">{{ sometidos() }} / {{ store.planes().length }}</p>
        </div>
        <div class="stat-card">
          <p class="stat-label">Monto consolidado (PACC)</p>
          <p class="stat-value">{{ fmt(costoTotal()) }}</p>
          <p class="!text-xs text-ink-muted">Techo institucional: {{ fmt(techoTotal()) }}</p>
        </div>
        <div class="stat-card">
          <p class="stat-label">Requerimientos aprobados</p>
          <p class="stat-value">{{ reqsAprobados() }}</p>
        </div>
        <div class="stat-card">
          <p class="stat-label">Validación financiera</p>
          @if (store.aprobacionFinanciera() === 'aprobada') {
            <span class="badge-success mt-1 w-fit">Completada</span>
          } @else {
            <span class="badge-warning mt-1 w-fit">Pendiente</span>
          }
        </div>
      </div>

      <div class="mt-4 grid grid-cols-1 gap-4 lg:grid-cols-3">
        <div class="lg:col-span-2">
          <div class="card p-5">
            <h2 class="!font-poppins !text-base font-semibold text-ink">Expediente de aprobación</h2>
            <app-summary-list [filas]="expediente()" />
            <div class="mt-3 flex gap-2">
              <button type="button" class="btn-secondary btn-sm" (click)="descarga.set(true)">Descargar expediente</button>
              <a routerLink="/reportes-pds" class="btn-ghost btn-sm !no-underline">Generar POA y PACC (PDF/Excel)</a>
            </div>
            @if (descarga()) {
              <p class="mt-2 rounded-lg bg-info-bg px-3 py-2 !text-sm text-info-text ring-1 ring-info-border">
                (Demo) Genere el POA y el PACC en PDF y Excel desde <a routerLink="/reportes-pds" class="!text-sm">Reportes PDS</a>.
              </p>
            }
          </div>

          <div class="card mt-4 p-5">
            <h2 class="!font-poppins !text-base font-semibold text-ink">Pendientes críticos</h2>
            <ul class="mt-2 flex flex-col gap-2">
              @for (p of pendientesCriticos(); track p) {
                <li class="rounded-lg bg-warning-bg px-3 py-2 !text-sm text-warning-text ring-1 ring-warning-border">{{ p }}</li>
              } @empty {
                <li class="rounded-lg bg-success-bg px-3 py-2 !text-sm text-success-text ring-1 ring-success-border">
                  Sin pendientes críticos: el expediente está completo.
                </li>
              }
            </ul>
          </div>
        </div>

        <!-- Panel de aprobación: POA y PACC por separado -->
        <div class="flex h-fit flex-col gap-4">
          @for (art of artefactos; track art.id) {
            <div class="card p-5">
              <h2 class="!font-poppins !text-base font-semibold text-ink">Aprobar {{ art.id }}</h2>
              <p class="!text-xs text-ink-muted">{{ art.detalle }}</p>

              @if (store.selloDe(art.id); as sello) {
                <div class="mt-3 rounded-xl border-2 border-success-border bg-success-bg p-4">
                  <p class="!font-poppins !text-base font-semibold text-success-text">{{ art.id }} aprobado ✓</p>
                  <dl class="mt-2 !text-sm text-ink">
                    <div class="flex justify-between gap-2 py-0.5"><dt class="text-ink-muted">Versión</dt><dd class="text-right font-medium">{{ sello.version }}</dd></div>
                    <div class="flex justify-between gap-2 py-0.5"><dt class="text-ink-muted">Aprobado por</dt><dd class="text-right font-medium">{{ sello.usuario }}</dd></div>
                    <div class="flex justify-between gap-2 py-0.5"><dt class="text-ink-muted">Nivel</dt><dd class="text-right">{{ sello.nivel }}</dd></div>
                    <div class="flex justify-between gap-2 py-0.5"><dt class="text-ink-muted">Fecha y hora</dt><dd class="text-right tabular-nums">{{ sello.fecha }}</dd></div>
                  </dl>
                </div>
              } @else if (!esAprobador()) {
                <p class="mt-2 !text-sm text-ink-muted">
                  La aprobación corresponde a la Máxima Autoridad Ejecutiva o al nivel ejecutivo delegado.
                  Cambie a la persona <strong>Dra. Altagracia Medina (MAE)</strong> en el selector de la demo.
                </p>
              } @else if (store.aprobacionFinanciera() !== 'aprobada') {
                <p class="mt-2 rounded-lg bg-warning-bg px-3 py-2 !text-sm text-warning-text ring-1 ring-warning-border">
                  Aún falta la validación financiera de Presupuesto.
                </p>
              } @else if (art.id === 'PACC' && !store.selloDe('POA')) {
                <p class="mt-2 rounded-lg bg-warning-bg px-3 py-2 !text-sm text-warning-text ring-1 ring-warning-border">
                  Apruebe primero el POA: el PACC se aprueba después del plan.
                </p>
              } @else {
                <label class="field-label mt-3">Nivel aprobador
                  <select class="field-control mt-1" [ngModel]="nivel[art.id]" (ngModelChange)="setNivel(art.id, $event)">
                    <option value="MAE">Máxima Autoridad Ejecutiva</option>
                    <option value="Nivel ejecutivo">Nivel ejecutivo delegado</option>
                  </select>
                </label>
                <label class="mt-3 !flex items-start gap-2 !text-sm !font-normal text-ink">
                  <input type="checkbox" class="mt-0.5 !size-4 accent-brand" [ngModel]="confirmado[art.id]" (ngModelChange)="setConfirmado(art.id, $event)" />
                  He revisado el expediente y apruebo el {{ art.id }} consolidado del {{ store.cicloActivo().nombre }}.
                </label>
                <button type="button" class="btn-primary mt-3 w-full" [disabled]="!confirmado[art.id]" (click)="aprobar(art.id)">
                  Aprobar {{ art.id }}
                </button>
              }
            </div>
          }
        </div>
      </div>
    </app-page-shell>
  `,
})
export class AprobacionComponent {
  protected readonly store = inject(PlanningStoreService);
  protected readonly fmt = formatRD;
  protected readonly descarga = signal(false);

  protected readonly artefactos: { id: ArtefactoAprobacion; detalle: string }[] = [
    { id: 'POA', detalle: 'Plan Operativo Anual consolidado.' },
    { id: 'PACC', detalle: 'Plan Anual de Compras y Contrataciones. Requiere el POA aprobado.' },
  ];

  protected confirmado: Record<ArtefactoAprobacion, boolean> = { POA: false, PACC: false };
  protected nivel: Record<ArtefactoAprobacion, NivelAprobador> = { POA: 'MAE', PACC: 'MAE' };

  protected readonly esAprobador = computed(() => this.store.personaId() === 'mae');

  protected setConfirmado(art: ArtefactoAprobacion, v: boolean): void {
    this.confirmado = { ...this.confirmado, [art]: v };
  }

  protected setNivel(art: ArtefactoAprobacion, v: NivelAprobador): void {
    this.nivel = { ...this.nivel, [art]: v };
  }

  protected aprobar(art: ArtefactoAprobacion): void {
    this.store.aprobarArtefacto(art, this.nivel[art]);
  }

  protected readonly sometidos = computed(
    () =>
      this.store.planes().filter((p) => ['Listo para revisión', 'En ajustes', 'Aprobado'].includes(p.estado)).length,
  );

  /** Monto consolidado = suma de los requerimientos vigentes (formato PACC). */
  protected readonly costoTotal = computed(() =>
    this.store.requerimientosVigentes().reduce((s, r) => s + montoDe(r), 0),
  );

  /** Techo institucional = suma de los techos por rubro vigentes. */
  protected readonly techoTotal = computed(() =>
    this.store.techosRubro().reduce((s, t) => s + this.store.techoRubro(t.rubroId), 0),
  );

  protected readonly reqsAprobados = computed(
    () => this.store.requerimientos().filter((r) => ['Aprobado', 'Reducido'].includes(r.estado)).length,
  );

  protected readonly expediente = computed<SummaryRow[]>(() => [
    { etiqueta: 'Ciclo', valor: this.store.cicloActivo().nombre },
    { etiqueta: 'PEI de referencia', valor: this.store.pei().nombre + ' (' + this.store.pei().version + ')' },
    { etiqueta: 'Planes de área', valor: `${this.sometidos()} sometidos de ${this.store.planes().length}` },
    { etiqueta: 'Requerimientos consolidados (PACC)', valor: this.fmt(this.costoTotal()) },
    {
      etiqueta: 'Validación financiera',
      valor: this.store.aprobacionFinanciera() === 'aprobada' ? 'Completada (División de Presupuesto)' : 'Pendiente',
    },
    { etiqueta: 'Reglas de ruteo activas', valor: `${this.store.reglasRuteo().filter((r) => r.activa).length} reglas` },
  ]);

  protected readonly pendientesCriticos = computed(() => {
    const out: string[] = [];
    const sinIniciar = this.store.planes().filter((p) => p.estado === 'Sin iniciar');
    if (sinIniciar.length) {
      out.push(
        `${sinIniciar.length} área(s) sin iniciar su plan: ` +
          sinIniciar.map((p) => this.store.area(p.areaId)?.corta).join(', '),
      );
    }
    const observados = this.store.planes().filter((p) => p.estado === 'Observado').length;
    if (observados) out.push(`${observados} plan(es) devueltos con observaciones sin corregir.`);
    const enAjustes = this.store.planes().filter((p) => p.estado === 'En ajustes').length;
    if (enAjustes) out.push(`${enAjustes} plan(es) en fase de ajustes por las áreas.`);
    if (this.store.aprobacionFinanciera() !== 'aprobada') {
      out.push('Validación financiera de Presupuesto pendiente.');
    }
    return out;
  });
}
