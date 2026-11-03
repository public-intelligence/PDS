import { Component, computed, input } from '@angular/core';

/**
 * Indicador de pasos del asistente de ciclos (una decisión por pantalla).
 * Muestra pasos completados, actual y pendientes.
 */
@Component({
  selector: 'app-wizard-steps',
  template: `
    <ol class="flex flex-wrap items-center gap-x-1 gap-y-2">
      @for (paso of pasos(); track paso; let i = $index) {
        <li class="flex items-center gap-1">
          <span
            class="grid size-6 shrink-0 place-items-center rounded-full text-xs font-bold"
            [class]="
              i < actual()
                ? 'bg-success text-white'
                : i === actual()
                  ? 'bg-brand text-white'
                  : 'bg-surface-sunken text-ink-subtle'
            "
          >
            @if (i < actual()) {
              ✓
            } @else {
              {{ i + 1 }}
            }
          </span>
          <span
            class="text-xs"
            [class]="i === actual() ? 'font-semibold text-ink' : 'text-ink-muted'"
            >{{ paso }}</span
          >
          @if (i < pasos().length - 1) {
            <span class="mx-1 h-px w-4 bg-border-strong"></span>
          }
        </li>
      }
    </ol>
    <p class="mt-1 text-xs text-ink-subtle">Paso {{ actual() + 1 }} de {{ total() }}</p>
  `,
})
export class WizardStepsComponent {
  readonly pasos = input.required<string[]>();
  readonly actual = input.required<number>();
  protected readonly total = computed(() => this.pasos().length);
}
