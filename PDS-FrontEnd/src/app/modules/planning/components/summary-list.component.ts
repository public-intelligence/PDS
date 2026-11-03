import { Component, input, output } from '@angular/core';

export interface SummaryRow {
  etiqueta: string;
  valor: string;
  /** Referencia que la página usa para navegar al editar ("Cambiar"). */
  cambiarRef?: string;
}

/**
 * Patrón GOV.UK "Summary list" (check your answers): filas etiqueta/valor con
 * enlace «Cambiar» por fila. Se usa antes de someter o aprobar.
 */
@Component({
  selector: 'app-summary-list',
  template: `
    <dl class="divide-y divide-border">
      @for (fila of filas(); track fila.etiqueta) {
        <div class="flex items-start gap-4 py-2.5">
          <dt class="w-44 shrink-0 text-sm font-medium text-ink-muted">{{ fila.etiqueta }}</dt>
          <dd class="min-w-0 flex-1 text-sm whitespace-pre-line text-ink">{{ fila.valor || '—' }}</dd>
          @if (fila.cambiarRef) {
            <dd class="shrink-0">
              <a
                href="#"
                (click)="$event.preventDefault(); cambiar.emit(fila.cambiarRef!)"
                class="!text-sm"
                >Cambiar<span class="sr-only"> {{ fila.etiqueta }}</span></a
              >
            </dd>
          }
        </div>
      }
    </dl>
  `,
})
export class SummaryListComponent {
  readonly filas = input.required<SummaryRow[]>();
  readonly cambiar = output<string>();
}
