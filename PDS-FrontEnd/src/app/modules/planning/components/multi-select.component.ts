import { Component, computed, input, output, signal } from '@angular/core';

export interface MultiOption {
  value: string;
  label: string;
  /** Texto secundario opcional (p. ej. tipo de área). */
  detalle?: string;
}

let multiSelectSeq = 0;

/** Alto del buscador + barra «Todas/Limpiar» que van sobre la lista. */
const ALTO_CABECERA_PANEL = 104;
/** Bajo este espacio libre, abrir hacia abajo deja el panel inservible. */
const ALTO_PANEL_MINIMO = 220;
const ALTO_LISTA_MINIMO = 140;

/**
 * Dropdown de selección múltiple con búsqueda, usado para catálogos que
 * vienen del organigrama o de un maestro (áreas involucradas, municipios de
 * impacto). Muestra un resumen en el disparador y chips removibles debajo.
 *
 * Nota: los `<label>` internos usan `!flex` porque el CSS del sistema de
 * diseño (@public-intelligence/ui-core) define `label { display: inline-block }` sin capa,
 * lo que gana sobre las utilidades de Tailwind.
 */
@Component({
  selector: 'app-multi-select',
  template: `
    <div class="relative flex flex-col gap-0.5">
      @if (etiqueta()) {
        <label class="field-label" [attr.for]="id">{{ etiqueta() }}</label>
      }

      <button
        #trigger
        type="button"
        [id]="id"
        (click)="toggle(trigger)"
        (keydown.escape)="cerrar()"
        [attr.aria-expanded]="abierto()"
        aria-haspopup="listbox"
        [class]="clasesTrigger()"
      >
        <span class="min-w-0 flex-1 truncate" [class]="seleccionados().length ? 'text-ink' : 'text-ink-subtle'">
          {{ resumen() }}
        </span>
        <svg
          class="size-4 shrink-0 text-ink-subtle transition-transform duration-150"
          [class.rotate-180]="abierto()"
          viewBox="0 0 16 16"
          fill="currentColor"
          aria-hidden="true"
        >
          <path
            fill-rule="evenodd"
            d="M4.22 6.22a.75.75 0 0 1 1.06 0L8 8.94l2.72-2.72a.75.75 0 1 1 1.06 1.06l-3.25 3.25a.75.75 0 0 1-1.06 0L4.22 7.28a.75.75 0 0 1 0-1.06z"
          />
        </svg>
      </button>

      @if (abierto()) {
        <!-- Capa para cerrar al hacer clic fuera -->
        <div class="fixed inset-0 z-40" role="presentation" (click)="cerrar()"></div>

        <div
          class="absolute right-0 left-0 z-50 overflow-hidden rounded-lg border border-border bg-surface-raised shadow-lg"
          [class.bottom-full]="haciaArriba()"
          [class.mb-1]="haciaArriba()"
          [class.top-full]="!haciaArriba()"
          [class.mt-1]="!haciaArriba()"
        >
          <div class="border-b border-border p-2">
            <input
              type="text"
              [placeholder]="placeholderBusqueda()"
              [value]="busqueda()"
              (input)="busqueda.set($any($event.target).value)"
              (click)="$event.stopPropagation()"
              (keydown.escape)="cerrar()"
              class="field-control !h-9"
            />
          </div>

          <div class="flex items-center justify-between border-b border-border px-3 py-1.5">
            <span class="!text-xs text-ink-subtle">{{ seleccionados().length }} de {{ opciones().length }} seleccionadas</span>
            <span class="flex gap-1">
              <button type="button" class="btn-ghost btn-sm" (click)="seleccionarTodo()">Todas</button>
              <button type="button" class="btn-ghost btn-sm" (click)="limpiar()">Limpiar</button>
            </span>
          </div>

          <ul
            class="custom-scrollbar overflow-y-auto py-1"
            [style.maxHeight.px]="maxLista()"
            role="listbox"
            [attr.aria-multiselectable]="true"
          >
            @for (op of filtradas(); track op.value) {
              <li>
                <label
                  class="!flex cursor-pointer items-center gap-2 px-3 py-2 !text-sm !font-normal text-ink transition-colors hover:bg-surface-sunken"
                  [class.bg-brand-50]="seleccionados().includes(op.value)"
                >
                  <input
                    type="checkbox"
                    class="!size-4 accent-brand"
                    [checked]="seleccionados().includes(op.value)"
                    (change)="alternar(op.value)"
                  />
                  <span class="min-w-0">
                    <span class="block truncate">{{ op.label }}</span>
                    @if (op.detalle) {
                      <span class="block truncate !text-xs text-ink-subtle">{{ op.detalle }}</span>
                    }
                  </span>
                </label>
              </li>
            } @empty {
              <li class="px-3 py-2 !text-sm text-ink-subtle">Sin resultados</li>
            }
          </ul>
        </div>
      }

      <!-- Chips de lo seleccionado -->
      @if (seleccionados().length) {
        <div class="mt-1.5 flex flex-wrap gap-1">
          @for (v of seleccionados(); track v) {
            <span class="badge-info">
              {{ etiquetaDe(v) }}
              <button
                type="button"
                class="btn-icon !size-4 !text-info-text"
                [attr.aria-label]="'Quitar ' + etiquetaDe(v)"
                (click)="alternar(v)"
              >
                ✕
              </button>
            </span>
          }
        </div>
      }
    </div>
  `,
})
export class MultiSelectComponent {
  readonly etiqueta = input('');
  readonly opciones = input.required<MultiOption[]>();
  readonly seleccionados = input.required<string[]>();
  readonly placeholder = input('Seleccione una o más opciones');
  readonly placeholderBusqueda = input('Buscar…');

  readonly cambio = output<string[]>();

  protected readonly id = `multi-select-${++multiSelectSeq}`;
  protected readonly abierto = signal(false);
  protected readonly busqueda = signal('');
  /** El panel abre hacia arriba cuando no cabe debajo del disparador. */
  protected readonly haciaArriba = signal(false);
  protected readonly maxLista = signal(224);

  protected readonly filtradas = computed(() => {
    const q = this.busqueda().toLowerCase().trim();
    if (!q) return this.opciones();
    return this.opciones().filter((o) => o.label.toLowerCase().includes(q));
  });

  protected readonly resumen = computed(() => {
    const sel = this.seleccionados();
    if (!sel.length) return this.placeholder();
    if (sel.length <= 2) return sel.map((v) => this.etiquetaDe(v)).join(', ');
    return `${sel.length} seleccionadas`;
  });

  protected readonly clasesTrigger = computed(() => {
    const base =
      '!flex h-10 w-full cursor-pointer items-center justify-between gap-2 !rounded-lg border !px-3 text-left !text-sm !font-normal !normal-case shadow-none outline-none transition-colors ';
    return this.abierto()
      ? `${base} border-brand !bg-surface-raised ring-2 ring-brand-200`
      : `${base} border-border-strong !bg-surface-raised hover:border-brand-300`;
  });

  protected etiquetaDe(value: string): string {
    return this.opciones().find((o) => o.value === value)?.label ?? value;
  }

  protected toggle(trigger: HTMLElement): void {
    if (this.abierto()) {
      this.cerrar();
      return;
    }
    this.ubicar(trigger);
    this.abierto.set(true);
  }

  /**
   * El panel vive dentro de contenedores con scroll (drawer, formularios
   * largos): si se abriera siempre hacia abajo, quedaría recortado y sin
   * alcance cuando el campo está al final de la pantalla. Se decide el lado
   * con más espacio y se limita la altura de la lista a ese espacio.
   */
  private ubicar(trigger: HTMLElement): void {
    const caja = trigger.getBoundingClientRect();
    const margen = 16;
    const espacioAbajo = window.innerHeight - caja.bottom - margen;
    const espacioArriba = caja.top - margen;
    const arriba = espacioAbajo < ALTO_PANEL_MINIMO && espacioArriba > espacioAbajo;
    this.haciaArriba.set(arriba);
    const disponible = (arriba ? espacioArriba : espacioAbajo) - ALTO_CABECERA_PANEL;
    this.maxLista.set(Math.max(ALTO_LISTA_MINIMO, Math.min(224, disponible)));
  }

  protected cerrar(): void {
    this.abierto.set(false);
    this.busqueda.set('');
  }

  protected alternar(value: string): void {
    const sel = this.seleccionados();
    this.cambio.emit(sel.includes(value) ? sel.filter((v) => v !== value) : [...sel, value]);
  }

  protected seleccionarTodo(): void {
    this.cambio.emit(this.filtradas().map((o) => o.value));
  }

  protected limpiar(): void {
    this.cambio.emit([]);
  }
}
