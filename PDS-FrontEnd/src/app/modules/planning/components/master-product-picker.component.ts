import { Component, OnInit, computed, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MasterProductService } from '../../../services/planning/master-product.service';
import { MasterProductSearchItemDto } from '../data/planning-api.models';

/**
 * Buscador de artículos de la Maestra para la pantalla operativa.
 *
 * El usuario busca por nombre o sinónimo; nunca escribe un código. El endpoint
 * `master-products/search` solo devuelve artículos con área consolidadora resuelta,
 * así que lo que aparece aquí siempre se puede requerir. Para lo que no aparece
 * existe el flujo de gobernanza «No encuentro lo que necesito».
 */
@Component({
  selector: 'app-master-product-picker',
  imports: [FormsModule],
  template: `
    <div class="relative">
      <label class="field-label">{{ etiqueta() }}
        <input
          type="text"
          class="field-control mt-1"
          [placeholder]="placeholder()"
          [ngModel]="texto()"
          (ngModelChange)="alEscribir($event)"
          (keydown)="alTeclear($event)"
          (focus)="abierto.set(true)"
          autocomplete="off"
          role="combobox"
          [attr.aria-expanded]="abierto()"
          aria-autocomplete="list"
        />
      </label>

      @if (seleccionado(); as sel) {
        <div class="mt-2 rounded-lg bg-surface-sunken px-3 py-2">
          <div class="flex items-start justify-between gap-2">
            <div>
              <p class="!text-sm font-semibold text-ink">{{ sel.name }}</p>
              <p class="!text-xs text-ink-subtle">{{ sel.code }}{{ sel.itemType ? ' · ' + sel.itemType : '' }}</p>
            </div>
            <button type="button" class="btn-ghost btn-sm" (click)="limpiar()">Cambiar</button>
          </div>

          <!-- Bloque derivado: informativo, nunca editable. -->
          <dl class="mt-2 grid grid-cols-1 gap-x-4 gap-y-1 border-t border-border pt-2 !text-xs text-ink-muted sm:grid-cols-2">
            <div><dt class="inline font-medium">UNSPSC:</dt> <dd class="inline">{{ sel.unspscCode || '—' }}</dd></div>
            <div><dt class="inline font-medium">Objeto del gasto:</dt> <dd class="inline">{{ sel.expenseObjectCode || 'sin asignar' }}</dd></div>
            <div><dt class="inline font-medium">Cuenta contable:</dt> <dd class="inline">{{ sel.accountingAccountCode || 'sin asignar' }}</dd></div>
            <div><dt class="inline font-medium">Precio de referencia:</dt> <dd class="inline">{{ sel.referencePrice ? ('RD$ ' + sel.referencePrice) : 'sin precio' }}</dd></div>
            <div class="sm:col-span-2">
              <dt class="inline font-medium">Área consolidadora:</dt>
              <dd class="inline">{{ sel.consolidatorAreaName || '—' }}</dd>
            </div>
          </dl>
        </div>
      }

      @if (abierto() && !seleccionado()) {
        <div class="absolute z-20 mt-1 max-h-72 w-full overflow-auto rounded-lg border border-border bg-surface shadow-lg">
          @if (buscando()) {
            <p class="px-3 py-3 !text-sm text-ink-muted">Buscando…</p>
          } @else if (resultados().length === 0 && texto().trim().length >= 2) {
            <div class="px-3 py-3">
              <p class="!text-sm text-ink-muted">Sin resultados para «{{ texto() }}».</p>
              <button type="button" class="btn-ghost btn-sm !px-0 text-brand" (click)="abrirSolicitud()">
                No encuentro lo que necesito
              </button>
            </div>
          } @else if (texto().trim().length < 2) {
            <p class="px-3 py-3 !text-sm text-ink-muted">Escriba al menos 2 caracteres.</p>
          } @else {
            <ul role="listbox">
              @for (item of resultados(); track item.id; let i = $index) {
                <li
                  role="option"
                  [attr.aria-selected]="i === indiceActivo()"
                  class="cursor-pointer px-3 py-2 hover:bg-surface-sunken"
                  [class.bg-surface-sunken]="i === indiceActivo()"
                  (click)="seleccionar(item)"
                  (mouseenter)="indiceActivo.set(i)"
                >
                  <p class="!text-sm font-medium text-ink">{{ item.name }}</p>
                  <p class="!text-xs text-ink-subtle">
                    {{ item.code }}
                    @if (item.consolidatorAreaName) { · lo gestiona {{ item.consolidatorAreaName }} }
                  </p>
                </li>
              }
            </ul>
            <button type="button" class="btn-ghost btn-sm w-full border-t border-border !text-xs" (click)="abrirSolicitud()">
              No encuentro lo que necesito
            </button>
          }
        </div>
      }

      <!-- Solicitud de artículo nuevo: no crea requerimiento, va a la cola de Planificación. -->
      @if (solicitudAbierta()) {
        <div class="mt-3 rounded-lg border border-border bg-surface p-3">
          <p class="!text-sm font-semibold text-ink">Solicitar un artículo nuevo</p>
          <p class="mt-0.5 !text-xs text-ink-muted">
            Planificación revisa la solicitud, le asigna códigos y área consolidadora, y lo habilita
            para todas las áreas. Esto no crea el requerimiento.
          </p>
          <div class="mt-2 grid grid-cols-1 gap-2 sm:grid-cols-2">
            <label class="field-label">Nombre propuesto
              <input class="field-control mt-1" [(ngModel)]="solicitudNombre" placeholder="Ej. Monitor curvo 34 pulgadas" />
            </label>
            <label class="field-label">Tipo
              <select class="field-control mt-1" [(ngModel)]="solicitudTipo">
                <option value="Bien">Bien</option>
                <option value="Servicio">Servicio</option>
                <option value="Obra">Obra</option>
              </select>
            </label>
            <label class="field-label sm:col-span-2">Justificación
              <input class="field-control mt-1" [(ngModel)]="solicitudJustificacion" placeholder="Para qué se necesita y por qué no sirve un artículo existente" />
            </label>
          </div>
          @if (solicitudMensaje()) {
            <p class="mt-2 rounded-lg bg-success-bg px-3 py-1.5 !text-xs text-success-text">{{ solicitudMensaje() }}</p>
          }
          <div class="mt-2 flex gap-2">
            <button type="button" class="btn-primary btn-sm" [disabled]="!solicitudNombre.trim() || enviandoSolicitud()" (click)="enviarSolicitud()">
              {{ enviandoSolicitud() ? 'Enviando…' : 'Enviar solicitud' }}
            </button>
            <button type="button" class="btn-ghost btn-sm" (click)="solicitudAbierta.set(false)">Cancelar</button>
          </div>
        </div>
      }
    </div>
  `,
})
export class MasterProductPickerComponent implements OnInit {
  private readonly masterProductService = inject(MasterProductService);

  readonly etiqueta = input('Artículo');
  readonly placeholder = input('Busque por nombre: monitor, cemento, capacitación…');
  /** Preselección al editar un requerimiento existente. */
  readonly valorInicial = input<MasterProductSearchItemDto | null>(null);

  readonly seleccion = output<MasterProductSearchItemDto | null>();

  protected readonly texto = signal('');
  protected readonly resultados = signal<MasterProductSearchItemDto[]>([]);
  protected readonly seleccionado = signal<MasterProductSearchItemDto | null>(null);
  protected readonly buscando = signal(false);
  protected readonly abierto = signal(false);
  protected readonly indiceActivo = signal(0);

  protected readonly solicitudAbierta = signal(false);
  protected readonly enviandoSolicitud = signal(false);
  protected readonly solicitudMensaje = signal('');
  protected solicitudNombre = '';
  protected solicitudTipo = 'Bien';
  protected solicitudJustificacion = '';

  private readonly consulta$ = new Subject<string>();

  constructor() {
    this.consulta$
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        switchMap((q) => this.masterProductService.search(q)),
        takeUntilDestroyed()
      )
      .subscribe({
        next: (items) => {
          this.resultados.set(items ?? []);
          this.indiceActivo.set(0);
          this.buscando.set(false);
        },
        error: () => {
          this.resultados.set([]);
          this.buscando.set(false);
        },
      });
  }

  ngOnInit(): void {
    const inicial = this.valorInicial();
    if (inicial) {
      this.seleccionado.set(inicial);
      this.texto.set(inicial.name);
    }
  }

  protected alEscribir(valor: string): void {
    this.texto.set(valor);
    this.seleccionado.set(null);
    this.seleccion.emit(null);
    this.abierto.set(true);
    this.solicitudNombre = valor;

    if (valor.trim().length < 2) {
      this.resultados.set([]);
      return;
    }
    this.buscando.set(true);
    this.consulta$.next(valor.trim());
  }

  protected alTeclear(event: KeyboardEvent): void {
    if (!this.abierto() || this.resultados().length === 0) return;

    if (event.key === 'ArrowDown') {
      event.preventDefault();
      this.indiceActivo.update((i) => Math.min(i + 1, this.resultados().length - 1));
    } else if (event.key === 'ArrowUp') {
      event.preventDefault();
      this.indiceActivo.update((i) => Math.max(i - 1, 0));
    } else if (event.key === 'Enter') {
      event.preventDefault();
      const item = this.resultados()[this.indiceActivo()];
      if (item) this.seleccionar(item);
    } else if (event.key === 'Escape') {
      this.abierto.set(false);
    }
  }

  protected seleccionar(item: MasterProductSearchItemDto): void {
    this.seleccionado.set(item);
    this.texto.set(item.name);
    this.abierto.set(false);
    this.solicitudAbierta.set(false);
    this.seleccion.emit(item);
  }

  protected limpiar(): void {
    this.seleccionado.set(null);
    this.texto.set('');
    this.resultados.set([]);
    this.abierto.set(true);
    this.seleccion.emit(null);
  }

  protected abrirSolicitud(): void {
    this.solicitudNombre = this.texto().trim();
    this.solicitudJustificacion = '';
    this.solicitudMensaje.set('');
    this.solicitudAbierta.set(true);
    this.abierto.set(false);
  }

  protected enviarSolicitud(): void {
    this.enviandoSolicitud.set(true);
    this.masterProductService
      .requestProduct({
        name: this.solicitudNombre.trim(),
        itemType: this.solicitudTipo,
        description: this.solicitudJustificacion.trim() || undefined,
      })
      .subscribe({
        next: () => {
          this.enviandoSolicitud.set(false);
          this.solicitudMensaje.set('Solicitud enviada. Planificación la revisará y habilitará el artículo.');
          this.solicitudNombre = '';
          this.solicitudJustificacion = '';
        },
        error: () => {
          this.enviandoSolicitud.set(false);
          this.solicitudMensaje.set('No se pudo enviar la solicitud. Intente de nuevo.');
        },
      });
  }
}
