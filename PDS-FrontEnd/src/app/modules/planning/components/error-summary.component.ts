import { Component, input, output } from '@angular/core';

export interface ErrorSummaryItem {
  /** Identificador (p. ej. id de fila) que la página usa para enfocar el error. */
  ref: string;
  mensaje: string;
}

/**
 * Patrón GOV.UK "Error summary": caja al inicio de la página con la lista de
 * errores enlazados al campo/fila problemática. Siempre visible cuando exista
 * al menos un error de validación.
 */
@Component({
  selector: 'app-error-summary',
  template: `
    @if (errores().length) {
      <div
        class="slide-in-top rounded-xl border-2 border-error bg-error-bg p-4"
        role="alert"
        aria-labelledby="error-summary-title"
      >
        <h2 id="error-summary-title" class="!font-poppins !text-base font-semibold text-error-text">
          {{ titulo() }}
        </h2>
        <ul class="mt-2 flex flex-col gap-1">
          @for (e of errores(); track e.ref + e.mensaje) {
            <li>
              <a
                href="#"
                (click)="$event.preventDefault(); irA.emit(e.ref)"
                class="!text-sm !text-error-text underline hover:!text-error"
                >{{ e.mensaje }}</a
              >
            </li>
          }
        </ul>
      </div>
    }
  `,
})
export class ErrorSummaryComponent {
  readonly titulo = input('Hay un problema con la información registrada');
  readonly errores = input.required<ErrorSummaryItem[]>();
  readonly irA = output<string>();
}
