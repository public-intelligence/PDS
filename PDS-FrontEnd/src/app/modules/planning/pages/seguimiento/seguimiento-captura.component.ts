import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';
import { PlanningStoreService } from '../../data/planning-store.service';

/**
 * Captura de avance del corte: físico, financiero, comentario, evidencias y
 * riesgos, con el historial de reportes anteriores a la vista.
 */
@Component({
  selector: 'app-seguimiento-captura',
  imports: [RouterLink, FormsModule],
  template: `
    @let reg = registro();
    @if (!reg) {
      <div class="p-6"><p class="text-sm text-ink-muted">Registro no encontrado.</p></div>
    } @else {
      <div class="mx-auto max-w-3xl p-6">
        <a routerLink="/seguimiento" class="!text-sm !text-ink-muted !no-underline hover:!underline">← Seguimiento</a>
        <h1 class="!font-poppins mt-2 !text-2xl font-semibold text-ink">Reportar avance del corte</h1>
        <p class="mt-1 !text-sm text-ink-muted">{{ reg.actividad }}</p>

        <div class="card mt-4 p-5">
          <dl class="grid grid-cols-2 gap-3 !text-sm lg:grid-cols-3">
            <div><dt class="text-xs font-medium text-ink-subtle uppercase">Área</dt><dd class="text-ink">{{ store.area(reg.areaId)?.nombre }}</dd></div>
            <div><dt class="text-xs font-medium text-ink-subtle uppercase">Meta del período</dt><dd class="text-ink">{{ reg.metaPeriodo }}</dd></div>
            <div><dt class="text-xs font-medium text-ink-subtle uppercase">Fecha de corte</dt><dd class="text-ink">{{ reg.corte }}</dd></div>
          </dl>
        </div>

        @if (guardado()) {
          <div class="card mt-4 border-2 border-success-border p-6 text-center">
            <p class="!font-poppins !text-xl font-semibold text-success-text">Avance reportado</p>
            <p class="mt-2 !text-sm text-ink-muted">El corte quedó registrado con su evidencia y trazabilidad.</p>
            <a routerLink="/seguimiento" class="btn-primary mt-4 inline-flex !no-underline">Volver al seguimiento</a>
          </div>
        } @else {
          <div class="card mt-4 p-5">
            <h2 class="!font-poppins !text-base font-semibold text-ink">Avance del período</h2>
            <div class="mt-3 grid grid-cols-2 gap-4">
              <label class="field-label">Avance físico (%)
                <input type="number" min="0" max="100" class="field-control mt-1" [(ngModel)]="fisico" />
              </label>
              <label class="field-label">Avance financiero (%)
                <input type="number" min="0" max="100" class="field-control mt-1" [(ngModel)]="financiero" />
              </label>
            </div>
            @if (desviacion() > 10) {
              <p class="mt-3 rounded-lg bg-warning-bg px-3 py-2 !text-sm text-warning-text ring-1 ring-warning-border">
                La ejecución financiera supera la física por {{ desviacion() }} puntos. Explique el
                desvío y registre medidas correctivas.
              </p>
            }
            <label class="field-label mt-4 block">Comentario explicativo
              <textarea class="mt-1 !h-24 w-full rounded-lg border border-border-strong px-3 py-2 !text-sm" [(ngModel)]="comentario" placeholder="Qué se logró, qué quedó pendiente y por qué"></textarea>
            </label>
            <label class="field-label mt-3 block">Riesgos / medidas correctivas (opcional)
              <textarea class="mt-1 !h-16 w-full rounded-lg border border-border-strong px-3 py-2 !text-sm" [(ngModel)]="riesgo"></textarea>
            </label>

            <p class="field-label mt-4">Evidencias</p>
            <div
              class="mt-1 flex flex-col items-center justify-center gap-1 rounded-xl border-2 border-dashed border-border-strong px-4 py-6 text-center"
            >
              <p class="!text-sm text-ink-muted">Arrastre aquí sus archivos o</p>
              <button type="button" class="btn-secondary btn-sm" (click)="adjuntarSimulado()">Seleccionar archivo</button>
              <p class="!text-xs text-ink-subtle">(Demo: se adjunta un archivo de ejemplo)</p>
            </div>
            @if (evidencias().length) {
              <ul class="mt-2 flex flex-wrap gap-2">
                @for (e of evidencias(); track e) {
                  <li class="badge-info">📎 {{ e }}</li>
                }
              </ul>
            }

            <div class="mt-5 flex justify-end gap-2 border-t border-border pt-4">
              <a routerLink="/seguimiento" class="btn-ghost !no-underline">Cancelar</a>
              <button type="button" class="btn-primary" [disabled]="!comentario.trim()" (click)="guardar()">Guardar reporte</button>
            </div>
          </div>
        }

        <div class="card mt-4 p-5">
          <h2 class="!font-poppins !text-base font-semibold text-ink">Reportes anteriores</h2>
          <ul class="mt-3 flex flex-col gap-3">
            @for (h of reg.historial; track h.corte) {
              <li class="rounded-lg bg-surface-sunken px-3 py-2.5">
                <p class="!text-sm text-ink">
                  <strong>Corte {{ h.corte }}:</strong> físico {{ h.avanceFisico }}% · financiero {{ h.avanceFinanciero }}%
                </p>
                <p class="mt-0.5 !text-xs text-ink-muted">{{ h.comentario }}</p>
              </li>
            } @empty {
              <li class="!text-sm text-ink-muted">Primer reporte de esta actividad.</li>
            }
          </ul>
        </div>
      </div>
    }
  `,
})
export class SeguimientoCapturaComponent {
  protected readonly store = inject(PlanningStoreService);
  private readonly route = inject(ActivatedRoute);

  protected fisico = 0;
  protected financiero = 0;
  protected comentario = '';
  protected riesgo = '';
  protected readonly evidencias = signal<string[]>([]);
  protected readonly guardado = signal(false);

  private readonly regId = toSignal(this.route.paramMap.pipe(map((p) => p.get('registroId') ?? '')), {
    initialValue: '',
  });

  protected readonly registro = computed(() =>
    this.store.seguimiento().find((r) => r.id === this.regId()),
  );

  protected desviacion(): number {
    return this.financiero - this.fisico;
  }

  protected adjuntarSimulado(): void {
    const n = this.evidencias().length + 1;
    this.evidencias.update((e) => [...e, `evidencia-corte-jun-${n}.pdf`]);
  }

  protected guardar(): void {
    this.store.reportarAvance(this.regId(), {
      avanceFisico: this.fisico,
      avanceFinanciero: this.financiero,
      comentario: this.comentario.trim(),
      riesgo: this.riesgo.trim() || undefined,
      evidencia: this.evidencias()[0],
    });
    this.guardado.set(true);
  }
}
