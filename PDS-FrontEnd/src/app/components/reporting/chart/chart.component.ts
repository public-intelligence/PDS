import { Component, computed, input } from '@angular/core';
import { ChartDatum, ChartType } from '../../../models/reporting/chart.model';

const PALETTE = [
  '#003876', '#1a579a', '#2e8bc0', '#5fb0d9', '#88c4e3',
  '#2ecc71', '#ebc342', '#ee2a24', '#9b59b6', '#16a085',
];

/**
 * Envoltorio de gráfico intercambiable (RF-RE-07). Implementación nativa: donut en SVG y barras en
 * CSS, sin dependencias externas (consistente con el organigrama SVG). La superficie pública es
 * agnóstica del motor, de modo que puede sustituirse por Chart.js sin tocar el dashboard.
 */
@Component({
  selector: 'app-chart',
  template: `
    <div class="flex flex-col gap-3">
      @if (title()) {
        <h3 class="text-sm font-semibold text-slate-700">{{ title() }}</h3>
      }

      @if (!colored().length) {
        <p class="text-xs text-slate-400">Sin datos para el contexto seleccionado.</p>
      } @else if (type() === 'donut') {
        <div class="flex flex-wrap items-center gap-4">
          <svg viewBox="0 0 180 180" [style.height.px]="height()" [style.width.px]="height()">
            @for (arc of arcs(); track arc.label) {
              <path [attr.d]="arc.path" [attr.fill]="arc.color">
                <title>{{ arc.label }}: {{ format(arc.value) }} ({{ arc.pct }}%)</title>
              </path>
            }
            <text x="90" y="86" text-anchor="middle" class="fill-slate-500" font-size="10">Total</text>
            <text x="90" y="102" text-anchor="middle" class="fill-slate-800" font-size="16" font-weight="700">
              {{ format(total()) }}
            </text>
          </svg>
          @if (showLegend()) {
            <ul class="flex flex-col gap-1 text-xs">
              @for (arc of arcs(); track arc.label) {
                <li class="flex items-center gap-2">
                  <span class="inline-block h-3 w-3 rounded-sm" [style.background]="arc.color"></span>
                  <span class="text-slate-600">{{ arc.label }}</span>
                  <span class="font-medium text-slate-800">{{ format(arc.value) }} ({{ arc.pct }}%)</span>
                </li>
              }
            </ul>
          }
        </div>
      } @else {
        <ul class="flex flex-col gap-2">
          @for (bar of bars(); track bar.label) {
            <li class="flex items-center gap-3 text-xs">
              <span class="w-40 shrink-0 truncate text-slate-600" [title]="bar.label">{{ bar.label }}</span>
              <span class="relative h-4 flex-1 overflow-hidden rounded bg-slate-100">
                <span class="absolute inset-y-0 left-0 rounded" [style.width.%]="bar.width" [style.background]="bar.color"></span>
              </span>
              <span class="w-16 shrink-0 text-right font-medium text-slate-800">{{ format(bar.value) }}</span>
            </li>
          }
        </ul>
      }
    </div>
  `,
})
export class ChartComponent {
  readonly type = input<ChartType>('bar');
  readonly data = input<ChartDatum[]>([]);
  readonly title = input('');
  readonly height = input(180);
  readonly showLegend = input(true);

  protected colored = computed(() =>
    this.data().map((d, i) => ({ ...d, color: d.color ?? PALETTE[i % PALETTE.length] })),
  );

  protected total = computed(() => this.colored().reduce((sum, d) => sum + d.value, 0));
  private maxValue = computed(() => Math.max(1, ...this.colored().map((d) => d.value)));

  protected bars = computed(() =>
    this.colored().map((d) => ({ ...d, width: (d.value / this.maxValue()) * 100 })),
  );

  protected arcs = computed(() => {
    const data = this.colored();
    const total = this.total() || 1;
    let angle = -Math.PI / 2;
    return data.map((d) => {
      const start = angle;
      const end = angle + (d.value / total) * 2 * Math.PI;
      angle = end;
      return { ...d, path: this.arcPath(start, end), pct: Math.round((d.value / total) * 100) };
    });
  });

  private arcPath(start: number, end: number): string {
    const cx = 90, cy = 90, rOuter = 82, rInner = 50;
    // Un solo segmento que cubre el 100% no puede dibujarse como arco; se aproxima a círculo completo.
    const sweep = end - start >= 2 * Math.PI - 0.001 ? 2 * Math.PI - 0.001 : end - start;
    const e = start + sweep;
    const large = sweep > Math.PI ? 1 : 0;
    const x1 = cx + rOuter * Math.cos(start), y1 = cy + rOuter * Math.sin(start);
    const x2 = cx + rOuter * Math.cos(e), y2 = cy + rOuter * Math.sin(e);
    const x3 = cx + rInner * Math.cos(e), y3 = cy + rInner * Math.sin(e);
    const x4 = cx + rInner * Math.cos(start), y4 = cy + rInner * Math.sin(start);
    return `M ${x1} ${y1} A ${rOuter} ${rOuter} 0 ${large} 1 ${x2} ${y2} L ${x3} ${y3} A ${rInner} ${rInner} 0 ${large} 0 ${x4} ${y4} Z`;
  }

  protected format(value: number): string {
    return value.toLocaleString('es-DO');
  }
}
