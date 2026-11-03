import { Component, input } from '@angular/core';

/**
 * Cascarón reutilizable de reportes: título + control de exportación, selector de contexto, panel de
 * filtros y tabla. Incluye la nota fija de alcance (ocupación fuera de alcance por diseño).
 */
@Component({
  selector: 'app-report-shell',
  template: `
    <div class="flex flex-col gap-4 p-6">
      <div class="flex flex-wrap items-center justify-between gap-4">
        <h1 class="text-xl font-semibold text-primary-blue">{{ title() }}</h1>
        <ng-content select="[slot=export]" />
      </div>

      <div class="rounded-xl border border-slate-100 bg-white p-4 shadow-sm">
        <ng-content select="[slot=context]" />
      </div>

      <div class="rounded-xl border border-slate-100 bg-white p-4 shadow-sm">
        <ng-content select="[slot=filters]" />
      </div>

      <div class="overflow-auto rounded-xl border border-slate-100 bg-white p-2 shadow-sm">
        <ng-content select="[slot=table]" />
      </div>

      <p class="text-xs italic text-slate-400">
        PDS es un sistema definicional: define unidades, cargos y plazas autorizadas. Las cifras de
        ocupación (plazas ocupadas/vacantes, % de ocupación) están fuera de alcance por diseño.
      </p>
    </div>
  `,
})
export class ReportShellComponent {
  readonly title = input('');
}
