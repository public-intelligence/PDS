import { Component, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PageShellComponent } from '@public-intelligence/ui-core';
import { PlanningDrawerComponent } from '../../components/planning-drawer.component';
import { MultiSelectComponent, MultiOption } from '../../components/multi-select.component';
import { MetasAnuales, Pei, ProductoEstrategico, UnidadMedidaPei } from '../../data/planning.models';
import { ImportResult, PeiStoreService } from '../../../../services/planning/pei-store.service';
import {
  PLANNING_PERMISSIONS,
  PlanningContextService,
} from '../../../../services/planning/planning-context.service';

/** Nodo que se está agregando/editando inline en el árbol. */
interface EdicionInline {
  modo: 'agregar' | 'editar';
  tipo: 'foco' | 'objetivo' | 'producto';
  focoId?: string;
  objetivoId?: string;
  nodoId?: string;
}

/**
 * Expediente del PEI con la estructura oficial de 4 niveles (foco → objetivo →
 * producto → unidad de medida) y GOBERNANZA DE VERSIONES contra el backend real
 * (M3): enmiendas justificadas aprobadas por la MAE, metas de años transcurridos
 * inmutables, descontinuación en vez de borrado, y carga/descarga Excel.
 */
@Component({
  selector: 'app-pei',
  imports: [PageShellComponent, FormsModule, PlanningDrawerComponent, MultiSelectComponent],
  template: `
    @if (store.cargando() || !store.cargado()) {
      <app-page-shell title="Plan estratégico institucional" description="Cargando información…">
        <div class="card flex flex-col items-center justify-center p-12 text-center text-ink-muted min-h-[350px]">
          <span class="inline-block size-8 animate-spin rounded-full border-4 border-brand border-t-transparent mb-3"></span>
          <p class="text-base font-semibold text-ink">Cargando Plan Estratégico Institucional...</p>
          <p class="text-xs text-ink-muted mt-1">Por favor espere mientras se obtienen los datos del servidor.</p>
        </div>
      </app-page-shell>
    } @else if (!versionMostrada()) {
      <!-- Estado vacío: la organización aún no tiene PEI -->
      <app-page-shell title="Plan estratégico institucional" description="Sin PEI registrado">
        @if (store.error()) {
          <p class="mb-4 rounded-lg bg-error-bg px-3 py-2 text-sm text-error-text ring-1 ring-error-border">{{ store.error() }}</p>
        }
        <div class="card mx-auto max-w-xl p-6">
          <h2 class="!font-poppins !text-lg font-semibold text-ink">Crear el PEI institucional</h2>
          <p class="mt-1 !text-sm text-ink-muted">
            Aún no hay un plan estratégico registrado. Cree la primera versión (quedará en
            elaboración) y luego cargue el contenido desde Excel o edítelo en pantalla.
          </p>
          @if (esPlanificacion()) {
            <label class="field-label mt-4 block">Nombre del plan
              <input class="field-control mt-1" [(ngModel)]="nuevoNombre" placeholder="Plan Estratégico Institucional (PEI) 2025-2028" />
            </label>
            <div class="mt-3 grid grid-cols-2 gap-3">
              <label class="field-label">Año inicial
                <input type="number" class="field-control mt-1" [(ngModel)]="nuevoInicio" />
              </label>
              <label class="field-label">Año final
                <input type="number" class="field-control mt-1" [(ngModel)]="nuevoFin" />
              </label>
            </div>
            <div class="mt-5 flex justify-end">
              <button type="button" class="btn-primary" [disabled]="!nuevoNombre.trim() || nuevoFin < nuevoInicio" (click)="crearPeiInicial()">
                Crear PEI
              </button>
            </div>
          } @else {
            <p class="mt-4 rounded-lg bg-surface-sunken px-3 py-2 !text-sm text-ink-muted">
              El área de Planificación es la responsable de crear y cargar el PEI.
            </p>
          }
        </div>
      </app-page-shell>
    } @else {
      @let pei = versionMostrada()!;
      @let editable = esBorrador() && esPlanificacion();
      <app-page-shell title="Plan estratégico institucional" [description]="pei.nombre">
        @if (aviso()) {
          <p class="slide-in-top mb-4 rounded-lg bg-success-bg px-3 py-2 text-sm text-success-text ring-1 ring-success-border">
            {{ aviso() }}
          </p>
        }
        @if (store.error()) {
          <p class="mb-4 rounded-lg bg-error-bg px-3 py-2 text-sm text-error-text ring-1 ring-error-border">{{ store.error() }}</p>
        }

        <!-- Selector de PEI y Selector de versión -->
        <div class="card mb-4 p-4">
          <div class="flex flex-wrap items-center justify-between gap-3">
            <div class="min-w-0 flex flex-wrap items-center gap-4">
              @if (peisPorPeriodo().length > 1) {
                <div>
                  <p class="!text-xs font-semibold tracking-wide text-ink-subtle uppercase">Plan Estratégico (PEI)</p>
                  <select
                    class="field-control mt-1 !w-auto !py-1 !text-sm font-semibold text-ink"
                    [ngModel]="peiPlanActual()?.periodo"
                    (ngModelChange)="periodoSeleccionado.set($event); versionId.set('')"
                  >
                    @for (p of peisPorPeriodo(); track p.periodo) {
                      <option [value]="p.periodo">
                        {{ p.nombre }} ({{ p.periodo }}) · {{ p.estado }}
                      </option>
                    }
                  </select>
                </div>
              }

              <div>
                <p class="!text-xs font-semibold tracking-wide text-ink-subtle uppercase">Versión</p>
                <div class="mt-1 flex flex-wrap items-center gap-1.5">
                  @for (v of versionesPlanActual(); track v.id) {
                    <button
                      type="button"
                      class="!rounded-lg border !px-3 !py-1.5 !text-sm !font-medium !normal-case transition-colors"
                      [class]="
                        v.id === pei.id
                          ? 'border-brand !bg-brand-50 !text-brand'
                          : 'border-border !bg-surface-raised !text-ink-muted hover:!bg-surface'
                      "
                      (click)="versionId.set(v.id)"
                    >
                      {{ v.version }}
                      <span [class]="badgeVersion(v.estado)" class="ml-1 !text-[10px]">{{ v.estado }}</span>
                    </button>
                  }
                </div>
              </div>
            </div>

            <div class="flex shrink-0 flex-wrap gap-2">
              <button type="button" class="btn-ghost btn-sm flex items-center gap-1.5" [disabled]="store.descargando()" (click)="store.exportar(pei.id)">
                @if (store.descargando()) {
                  <span class="inline-block h-3.5 w-3.5 animate-spin rounded-full border-2 border-brand border-t-transparent"></span>
                }
                Exportar Excel
              </button>
              @if (editable) {
                <button type="button" class="btn-ghost btn-sm flex items-center gap-1.5" [disabled]="store.descargando()" (click)="store.descargarPlantilla(pei.id)">
                  @if (store.descargando()) {
                    <span class="inline-block h-3.5 w-3.5 animate-spin rounded-full border-2 border-brand border-t-transparent"></span>
                  }
                  Plantilla PEI
                </button>
                <button type="button" class="btn-secondary btn-sm" (click)="abrirImportar()">Importar Excel</button>
              }
              @if (esPlanificacion()) {
                @if (!store.peiBorrador()) {
                  <button type="button" class="btn-outline btn-sm" (click)="abrirNuevoPeiModal()">+ Nuevo PEI</button>
                  <button type="button" class="btn-primary btn-sm" (click)="abrirEnmienda()">Nueva enmienda</button>
                }
              }
              @if (esBorrador() && esPlanificacion()) {
                <button type="button" class="btn-ghost" (click)="confirmarDescarte.set(true)">Descartar enmienda</button>
              }
              @if (esBorrador() && esMae()) {
                <button type="button" class="btn-primary" (click)="confirmarAprobacion.set(true)">Aprobar enmienda</button>
              }
            </div>
          </div>

          <!-- Contexto de la versión mostrada -->
          <dl class="mt-3 grid grid-cols-2 gap-3 border-t border-border pt-3 !text-sm lg:grid-cols-4">
            <div>
              <dt class="!text-xs font-medium text-ink-subtle uppercase">Estado</dt>
              <dd><span [class]="badgeVersion(pei.estado)">{{ pei.estado }}</span></dd>
            </div>
            <div>
              <dt class="!text-xs font-medium text-ink-subtle uppercase">Período</dt>
              <dd class="text-ink">{{ pei.periodo }}</dd>
            </div>
            <div>
              <dt class="!text-xs font-medium text-ink-subtle uppercase">
                {{ pei.estado === 'En elaboración' ? 'Creada por' : 'Aprobada por' }}
              </dt>
              <dd class="text-ink">
                {{ pei.estado === 'En elaboración' ? pei.creadaPor : pei.aprobadoPor || '—' }}
              </dd>
            </div>
            <div>
              <dt class="!text-xs font-medium text-ink-subtle uppercase">Fecha</dt>
              <dd class="text-ink">
                {{ pei.estado === 'En elaboración' ? pei.fechaCreacion : pei.fechaAprobacion || '—' }}
              </dd>
            </div>
          </dl>

          @if (pei.motivo) {
            <div class="mt-3 rounded-lg bg-surface-sunken px-3 py-2">
              <p class="!text-xs font-semibold tracking-wide text-ink-subtle uppercase">Justificación de esta versión</p>
              <p class="mt-0.5 !text-sm text-ink">{{ pei.motivo }}</p>
            </div>
          }
        </div>

        <!-- Avisos de modo -->
        @if (pei.estado === 'Histórico') {
          <div class="mb-4 rounded-lg bg-surface-sunken px-4 py-3 ring-1 ring-border">
            <p class="!text-sm font-medium text-ink">Versión histórica — sólo lectura</p>
            <p class="mt-0.5 !text-xs text-ink-muted">
              Se conserva congelada para poder consultar con qué metas se formularon los planes de su vigencia.
            </p>
          </div>
        } @else if (pei.estado === 'Vigente') {
          <div class="mb-4 rounded-lg bg-info-bg px-4 py-3 ring-1 ring-info-border">
            <p class="!text-sm font-medium text-info-text">Versión vigente — no se edita directamente</p>
            <p class="mt-0.5 !text-xs text-ink-muted">
              Es la que consume el POA para alinear. Para modificarla hay que abrir una enmienda
              justificada; los cambios sólo aplican cuando la MAE la aprueba.
            </p>
            @if (store.peiBorrador(); as b) {
              <p class="mt-2 !text-sm text-info-text">
                Hay una enmienda <strong>{{ b.version }}</strong> en elaboración pendiente de aprobación.
                <button type="button" class="btn-ghost btn-sm !text-info-text" (click)="versionId.set(b.id)">Verla</button>
              </p>
            }
          </div>
        } @else if (editable) {
          <div class="mb-4 rounded-lg bg-warning-bg px-4 py-3 ring-1 ring-warning-border">
            <p class="!text-sm font-medium text-warning-text">Enmienda {{ pei.version }} en elaboración</p>
            <p class="mt-0.5 !text-xs text-ink-muted">
              Las metas de {{ store.anioActual }} y años anteriores están bloqueadas por ser ejecución
              registrada. Los elementos con historia no se eliminan: se descontinúan desde un año futuro.
            </p>
          </div>
        } @else if (esBorrador()) {
          <div class="mb-4 rounded-lg bg-warning-bg px-4 py-3 ring-1 ring-warning-border">
            <p class="!text-sm font-medium text-warning-text">Enmienda pendiente de su aprobación</p>
            <p class="mt-0.5 !text-xs text-ink-muted">
              Revise los cambios propuestos y apruebe para que sustituya a la versión vigente.
            </p>
          </div>
        }

        <!-- Registro de cambios de la versión -->
        @if (pei.cambios.length) {
          <div class="card mb-4 overflow-hidden border border-border">
            <div
              class="flex items-center justify-between gap-3 px-5 py-3.5 cursor-pointer bg-surface-raised hover:bg-surface-sunken/60 transition-colors select-none"
              (click)="verCambios.set(!verCambios())"
            >
              <div class="flex items-center gap-3 min-w-0">
                <span
                  class="flex size-7 shrink-0 items-center justify-center rounded-lg bg-brand-50 text-brand transition-transform duration-200"
                  [class.rotate-90]="verCambios()"
                >
                  <svg class="size-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.5">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M9 5l7 7-7 7" />
                  </svg>
                </span>
                <span class="!text-sm font-semibold text-ink">Registro de cambios ({{ pei.cambios.length }})</span>
              </div>
              <button
                type="button"
                class="btn-secondary btn-sm flex items-center gap-1.5 font-medium shrink-0"
                (click)="$event.stopPropagation(); verCambios.set(!verCambios())"
              >
                <span>{{ verCambios() ? 'Plegar' : 'Desplegar' }}</span>
                <svg
                  class="size-3.5 transition-transform duration-200"
                  [class.rotate-180]="verCambios()"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                  stroke-width="2.5"
                >
                  <path stroke-linecap="round" stroke-linejoin="round" d="M19 9l-7 7-7-7" />
                </svg>
              </button>
            </div>
            @if (verCambios()) {
              <div class="border-t border-border p-4 bg-surface-raised">
                <ul class="flex flex-col gap-2">
                  @for (c of pei.cambios; track c.fecha + c.descripcion) {
                    <li class="flex gap-2 rounded-lg bg-surface-sunken px-3 py-2">
                      <span [class]="badgeCambio(c.tipo)" class="h-fit shrink-0">{{ c.tipo }}</span>
                      <span class="min-w-0">
                        <span class="block !text-sm text-ink">{{ c.descripcion }}</span>
                        <span class="block !text-xs text-ink-subtle">{{ c.nivel }} · {{ c.autor }} · {{ c.fecha }}</span>
                      </span>
                    </li>
                  }
                </ul>
              </div>
            }
          </div>
        }

        <div class="mb-3 flex items-center justify-between">
          <h2 class="!font-poppins !text-lg font-semibold text-ink">Estructura estratégica</h2>
          @if (editable) {
            <button type="button" class="btn-primary btn-sm" (click)="iniciarAgregar('foco')">+ Agregar foco</button>
          }
        </div>

        @if (esFormInline('foco', undefined)) {
          <div class="card mb-3 flex items-center gap-2 p-3">
            <input class="!h-9 flex-1" placeholder="Nombre del nuevo foco estratégico" [(ngModel)]="textoInline" />
            <button type="button" class="btn-primary btn-sm" [disabled]="!textoInline.trim()" (click)="guardarInline()">Guardar</button>
            <button type="button" class="btn-ghost btn-sm" (click)="cancelarInline()">Cancelar</button>
          </div>
        }

        <div class="flex flex-col gap-3">
          @for (foco of pei.focos; track foco.id; let fi = $index) {
            @let isOpen = abiertos().includes(foco.id);
            <div class="card overflow-hidden transition-shadow hover:shadow-sm">
              <!-- Foco Header (Acordeón) -->
              <div
                class="flex items-center justify-between gap-3 px-5 py-4 cursor-pointer select-none transition-colors"
                [class]="isOpen ? 'bg-brand-50/30 hover:bg-brand-50/50 border-b border-border' : 'bg-surface-raised hover:bg-surface-sunken/70'"
                (click)="toggle(foco.id)"
              >
                <div class="flex min-w-0 flex-1 items-center gap-3">
                  <span
                    class="flex size-8 shrink-0 items-center justify-center rounded-lg border transition-transform duration-200"
                    [class]="isOpen ? 'bg-brand text-white border-brand rotate-90' : 'bg-brand-50 text-brand border-brand-200'"
                  >
                    <svg class="size-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.5">
                      <path stroke-linecap="round" stroke-linejoin="round" d="M9 5l7 7-7 7" />
                    </svg>
                  </span>
                  <div class="min-w-0">
                    @if (esFormInline('foco', foco.id)) {
                      <input class="!h-9 w-full" [(ngModel)]="textoInline" (click)="$event.stopPropagation()" />
                    } @else {
                      <span class="block truncate !font-poppins !text-base font-semibold text-ink">
                        Foco {{ fi + 1 }}: {{ foco.nombre }}
                      </span>
                      @if (foco.alineacionEnd) {
                        <span class="block truncate !text-xs text-ink-muted">{{ foco.alineacionEnd }}</span>
                      }
                    }
                  </div>
                </div>

                <div class="flex shrink-0 items-center gap-2">
                  @if (editable) {
                    <span class="flex items-center gap-1 border-r border-border pr-2">
                      @if (esFormInline('foco', foco.id)) {
                        <button type="button" class="btn-primary btn-sm" (click)="$event.stopPropagation(); guardarInline()">Guardar</button>
                        <button type="button" class="btn-ghost btn-sm" (click)="$event.stopPropagation(); cancelarInline()">Cancelar</button>
                      } @else {
                        <button type="button" class="btn-ghost btn-sm" (click)="$event.stopPropagation(); iniciarEditar('foco', foco.nombre, { focoId: foco.id })">Editar</button>
                        <button type="button" class="btn-ghost btn-sm !text-error-text" (click)="$event.stopPropagation(); borrarFoco(foco.id)">Eliminar</button>
                      }
                    </span>
                  }

                  <!-- Botón explícito a la derecha para desplegar/plegar el acordeón -->
                  <button
                    type="button"
                    class="btn-secondary btn-sm flex items-center gap-1.5 font-medium shadow-2xs"
                    (click)="$event.stopPropagation(); toggle(foco.id)"
                  >
                    <span>{{ isOpen ? 'Plegar' : 'Desplegar' }}</span>
                    <svg
                      class="size-3.5 transition-transform duration-200"
                      [class.rotate-180]="isOpen"
                      fill="none"
                      viewBox="0 0 24 24"
                      stroke="currentColor"
                      stroke-width="2.5"
                    >
                      <path stroke-linecap="round" stroke-linejoin="round" d="M19 9l-7 7-7-7" />
                    </svg>
                  </button>
                </div>
              </div>

              @if (isOpen) {
                <div class="px-5 py-4">
                  @for (obj of foco.objetivos; track obj.id) {
                    <div class="mb-5 last:mb-1">
                      <!-- Objetivo -->
                      <div class="flex items-center justify-between gap-2">
                        @if (esFormInline('objetivo', obj.id)) {
                          <input class="!h-9 flex-1" [(ngModel)]="textoInline" />
                          <button type="button" class="btn-primary btn-sm" (click)="guardarInline()">Guardar</button>
                          <button type="button" class="btn-ghost btn-sm" (click)="cancelarInline()">Cancelar</button>
                        } @else {
                          <p class="!text-sm font-semibold text-ink">
                            <span class="badge-info mr-2">{{ obj.codigo }}</span>{{ obj.nombre }}
                          </p>
                          @if (editable) {
                            <span class="flex shrink-0 gap-1">
                              <button type="button" class="btn-ghost btn-sm" (click)="iniciarEditar('objetivo', obj.nombre, { focoId: foco.id, objetivoId: obj.id })">Editar</button>
                              <button type="button" class="btn-ghost btn-sm !text-error-text" (click)="borrarObjetivo(foco.id, obj.id)">Eliminar</button>
                            </span>
                          }
                        }
                      </div>

                      <div class="mt-2 ml-3 border-l-2 border-brand-100 pl-3">
                        @if (obj.productos.length) {
                          <div class="custom-scrollbar mt-2 overflow-x-auto">
                            <table class="w-full min-w-[960px] text-sm">
                              <thead>
                                <tr>
                                  <th [class]="th">Producto estratégico</th>
                                  <th [class]="th">Unidad de medida</th>
                                  @for (a of anios(); track a.anio) {
                                    <th [class]="th + ' text-right'">
                                      {{ a.anio }}
                                      @if (!a.editable) {
                                        <span title="Año ejecutado: meta inmutable">🔒</span>
                                      }
                                    </th>
                                  }
                                  <th [class]="th + ' text-right'">Total</th>
                                  <th [class]="th">Responsable (área)</th>
                                  <th [class]="th">Áreas involucradas</th>
                                  <th [class]="th"></th>
                                </tr>
                              </thead>
                              <tbody>
                                @for (prod of obj.productos; track prod.id) {
                                  @for (u of prod.unidades; track u.id; let ui = $index) {
                                    <tr class="hover:bg-surface" [class.opacity-60]="prod.descontinuadoDesde || u.descontinuadoDesde">
                                      @if (ui === 0) {
                                        <td class="border-b border-border px-3 py-2 align-top font-medium text-ink" [attr.rowspan]="prod.unidades.length || 1">
                                          {{ prod.nombre }}
                                          @if (prod.descontinuadoDesde) {
                                            <span class="badge-neutral mt-1 block w-fit" [title]="prod.motivoDescontinuacion ?? ''">
                                              Descontinuado desde {{ prod.descontinuadoDesde }}
                                            </span>
                                          }
                                        </td>
                                      }
                                      <td class="border-b border-border px-3 py-2 text-ink">
                                        {{ u.nombre }}
                                        @if (u.descontinuadoDesde && !prod.descontinuadoDesde) {
                                          <span class="badge-neutral ml-1 !text-[10px]" [title]="u.motivoDescontinuacion ?? ''">
                                            desde {{ u.descontinuadoDesde }}
                                          </span>
                                        }
                                      </td>
                                      @for (a of anios(); track a.anio) {
                                        <td class="border-b border-border px-3 py-2 text-right tabular-nums"
                                          [class.text-ink-subtle]="!a.editable"
                                          [title]="!a.editable ? 'Ejecución registrada: no editable' : ''">
                                          {{ u.metasAnuales[a.clave] ?? 0 }}
                                        </td>
                                      }
                                      <td class="border-b border-border px-3 py-2 text-right font-semibold tabular-nums">{{ totalMetas(u.metasAnuales) }}</td>
                                      <td class="border-b border-border px-3 py-2 whitespace-nowrap text-ink-muted">{{ store.area(u.responsableAreaId)?.corta ?? '—' }}</td>
                                      @if (ui === 0) {
                                        <td class="border-b border-border px-3 py-2 align-top" [attr.rowspan]="prod.unidades.length || 1">
                                          <span class="flex flex-wrap gap-1">
                                            @for (aid of prod.areasInvolucradasIds; track aid) {
                                              <span class="badge-neutral">{{ store.area(aid)?.corta ?? '—' }}</span>
                                            } @empty {
                                              <span class="text-ink-subtle">—</span>
                                            }
                                          </span>
                                        </td>
                                        <td class="border-b border-border px-3 py-2 text-right align-top whitespace-nowrap" [attr.rowspan]="prod.unidades.length || 1">
                                          @if (editable) {
                                            <button type="button" class="btn-ghost btn-sm" (click)="abrirProducto(prod)">Editar</button>
                                            @if (store.productoTieneHistoria(prod)) {
                                              @if (!prod.descontinuadoDesde) {
                                                <button type="button" class="btn-ghost btn-sm !text-warning-text" (click)="abrirDescontinuar(prod)">Descontinuar</button>
                                              }
                                            } @else {
                                              <button type="button" class="btn-ghost btn-sm !text-error-text" (click)="borrarProducto(prod.id)">Eliminar</button>
                                            }
                                          }
                                        </td>
                                      }
                                    </tr>
                                  } @empty {
                                    <tr class="hover:bg-surface">
                                      <td class="border-b border-border px-3 py-2 font-medium text-ink">{{ prod.nombre }}</td>
                                      <td class="border-b border-border px-3 py-2 text-warning-text" [attr.colspan]="anios().length + 2">Sin unidades de medida — agregue al menos una</td>
                                      <td class="border-b border-border px-3 py-2"></td>
                                      <td class="border-b border-border px-3 py-2 text-right whitespace-nowrap">
                                        @if (editable) {
                                          <button type="button" class="btn-ghost btn-sm" (click)="abrirProducto(prod)">Editar</button>
                                          <button type="button" class="btn-ghost btn-sm !text-error-text" (click)="borrarProducto(prod.id)">Eliminar</button>
                                        }
                                      </td>
                                    </tr>
                                  }
                                }
                              </tbody>
                            </table>
                          </div>
                        }

                        @if (editable) {
                          @if (esFormInline('producto', obj.id)) {
                            <div class="mt-2 flex items-center gap-2">
                              <input class="!h-9 flex-1" placeholder="Nombre del nuevo producto estratégico" [(ngModel)]="textoInline" />
                              <button type="button" class="btn-primary btn-sm" [disabled]="!textoInline.trim()" (click)="guardarInline()">Guardar</button>
                              <button type="button" class="btn-ghost btn-sm" (click)="cancelarInline()">Cancelar</button>
                            </div>
                          } @else {
                            <button type="button" class="btn-ghost btn-sm mt-1" (click)="iniciarAgregar('producto', { focoId: foco.id, objetivoId: obj.id })">
                              + Agregar producto
                            </button>
                          }
                        }
                      </div>
                    </div>
                  }

                  @if (editable) {
                    @if (esFormInline('objetivo', 'nuevo-' + foco.id)) {
                      <div class="mt-2 flex items-center gap-2">
                        <input class="!h-9 flex-1" placeholder="Nombre del nuevo objetivo estratégico" [(ngModel)]="textoInline" />
                        <button type="button" class="btn-primary btn-sm" [disabled]="!textoInline.trim()" (click)="guardarInline()">Guardar</button>
                        <button type="button" class="btn-ghost btn-sm" (click)="cancelarInline()">Cancelar</button>
                      </div>
                    } @else {
                      <button type="button" class="btn-ghost btn-sm" (click)="iniciarAgregar('objetivo', { focoId: foco.id })">+ Agregar objetivo</button>
                    }
                  }
                </div>
              }
            </div>
          }
        </div>
      </app-page-shell>
    }

    <!-- Modal: importar Excel -->
    @if (importando()) {
      <div class="fade-in fixed inset-0 z-50 grid place-items-center bg-black/40 p-4" role="dialog" aria-modal="true">
        <div class="card scale-in w-full max-w-2xl p-6">
          <h2 class="!font-poppins !text-lg font-semibold text-ink">Importar el PEI desde Excel</h2>
          <p class="mt-1 !text-sm text-ink-muted">
            Suba la plantilla llenada. Primero se valida todo el archivo: si hay errores, no se
            aplica nada. Las metas de años transcurridos son inmutables.
          </p>

          <div class="mt-4 flex flex-wrap items-center gap-3">
            <input type="file" accept=".xlsx" (change)="seleccionarArchivo($event)" />
            <label class="flex items-center gap-1 !text-sm text-ink">
              <input type="radio" name="modoImport" value="actualizar" [(ngModel)]="modoImport" /> Actualizar
            </label>
            <label class="flex items-center gap-1 !text-sm text-ink">
              <input type="radio" name="modoImport" value="reemplazar" [(ngModel)]="modoImport" /> Reemplazar
            </label>
          </div>

          @if (resultadoImport(); as r) {
            <div class="mt-4 max-h-64 overflow-y-auto rounded-lg bg-surface-sunken p-3">
              @if (r.errors.length) {
                <p class="!text-sm font-semibold text-error-text">{{ r.errors.length }} error(es) — corrija el archivo y vuelva a subirlo</p>
                <ul class="mt-1 flex flex-col gap-1">
                  @for (e of r.errors.slice(0, 12); track $index) {
                    <li class="!text-xs text-error-text">Fila {{ e.row }}@if (e.column) { · {{ e.column }} }: {{ e.message }}</li>
                  }
                  @if (r.errors.length > 12) {
                    <li class="!text-xs text-ink-muted">… y {{ r.errors.length - 12 }} más.</li>
                  }
                </ul>
              } @else {
                <p class="!text-sm font-semibold text-success-text">
                  Archivo válido.
                  @if (r.summary; as s) {
                    {{ s.added }} por agregar · {{ s.updated }} por actualizar · {{ s.unchanged }} sin cambios · {{ s.removed }} por eliminar.
                  }
                </p>
              }
              @for (w of r.warnings; track $index) {
                <p class="mt-1 !text-xs text-warning-text">⚠ {{ w }}</p>
              }
            </div>
          }

          <div class="mt-5 flex justify-end gap-2">
            <button type="button" class="btn-ghost" (click)="cerrarImportar()">Cancelar</button>
            <button
              type="button"
              class="btn-primary"
              [disabled]="!archivoImport || !resultadoImport() || resultadoImport()!.errors.length > 0 || aplicandoImport()"
              (click)="aplicarImport()"
            >
              {{ aplicandoImport() ? 'Aplicando…' : 'Aplicar carga' }}
            </button>
          </div>
        </div>
      </div>
    }

    <!-- Modal: abrir enmienda (exige justificación) -->
    @if (abriendoEnmienda()) {
      <div class="fade-in fixed inset-0 z-50 grid place-items-center bg-black/40 p-4" role="dialog" aria-modal="true">
        <div class="card scale-in w-full max-w-lg p-6">
          <h2 class="!font-poppins !text-lg font-semibold text-ink">Abrir una enmienda al PEI</h2>
          <p class="mt-1 !text-sm text-ink-muted">
            Modificar el plan estratégico es un acto excepcional. La justificación queda en el
            expediente y acompaña a la versión para siempre.
          </p>
          <label class="field-label mt-4 block">Justificación de la enmienda
            <textarea
              class="mt-1 !h-24 w-full rounded-lg border border-border-strong px-3 py-2 !text-sm"
              [(ngModel)]="motivoEnmienda"
              placeholder="Ej.: ajuste de metas por reasignación presupuestaria aprobada por el Consejo"
            ></textarea>
          </label>
          <p class="mt-2 rounded-lg bg-surface-sunken px-3 py-2 !text-xs text-ink-muted">
            Se creará la versión {{ proximaVersion() }} como copia de la vigente. Las metas de
            {{ store.anioActual }} y años anteriores quedarán bloqueadas.
          </p>
          <div class="mt-5 flex justify-end gap-2">
            <button type="button" class="btn-ghost" (click)="abriendoEnmienda.set(false)">Cancelar</button>
            <button type="button" class="btn-primary" [disabled]="motivoEnmienda.trim().length < 15" (click)="crearEnmienda()">
              Abrir enmienda
            </button>
          </div>
        </div>
      </div>
    }

    <!-- Modal: descontinuar producto -->
    @if (descontinuando(); as prod) {
      <div class="fade-in fixed inset-0 z-50 grid place-items-center bg-black/40 p-4" role="dialog" aria-modal="true">
        <div class="card scale-in w-full max-w-lg p-6">
          <h2 class="!font-poppins !text-lg font-semibold text-ink">Descontinuar «{{ prod.nombre }}»</h2>
          <p class="mt-1 !text-sm text-ink-muted">
            Este producto ya comprometió metas en años transcurridos, así que no puede eliminarse:
            su historia debe seguir siendo consultable. Deja de medirse a partir del año que indique.
          </p>
          <div class="mt-4 grid grid-cols-2 gap-3">
            <label class="field-label">Descontinuar desde
              <select class="field-control mt-1" [(ngModel)]="anioDescontinuacion">
                @for (a of aniosFuturos(); track a) {
                  <option [ngValue]="a">{{ a }}</option>
                }
              </select>
            </label>
          </div>
          <label class="field-label mt-3 block">Motivo
            <textarea class="mt-1 !h-20 w-full rounded-lg border border-border-strong px-3 py-2 !text-sm" [(ngModel)]="motivoDescontinuacion"></textarea>
          </label>
          <div class="mt-5 flex justify-end gap-2">
            <button type="button" class="btn-ghost" (click)="descontinuando.set(null)">Cancelar</button>
            <button type="button" class="btn-accent" [disabled]="!motivoDescontinuacion.trim()" (click)="confirmarDescontinuar()">
              Descontinuar
            </button>
          </div>
        </div>
      </div>
    }

    <!-- Confirmaciones de descarte y aprobación -->
    @if (confirmarDescarte()) {
      <div class="fade-in fixed inset-0 z-50 grid place-items-center bg-black/40 p-4" role="dialog" aria-modal="true">
        <div class="card scale-in w-full max-w-md p-6">
          <h2 class="!font-poppins !text-lg font-semibold text-ink">Descartar la enmienda</h2>
          <p class="mt-2 !text-sm text-ink-muted">
            Se perderán los cambios propuestos en {{ store.peiBorrador()?.version }}. La versión
            vigente no se altera.
          </p>
          <div class="mt-5 flex justify-end gap-2">
            <button type="button" class="btn-ghost" (click)="confirmarDescarte.set(false)">Cancelar</button>
            <button type="button" class="btn-danger" (click)="descartar()">Descartar</button>
          </div>
        </div>
      </div>
    }
    @if (confirmarAprobacion()) {
      <div class="fade-in fixed inset-0 z-50 grid place-items-center bg-black/40 p-4" role="dialog" aria-modal="true">
        <div class="card scale-in w-full max-w-md p-6">
          <h2 class="!font-poppins !text-lg font-semibold text-ink">Aprobar la enmienda</h2>
          <p class="mt-2 !text-sm text-ink-muted">
            {{ store.peiBorrador()?.version }} pasará a ser la versión vigente y el POA alineará
            contra ella. La versión anterior quedará histórica y congelada.
          </p>
          <div class="mt-5 flex justify-end gap-2">
            <button type="button" class="btn-ghost" (click)="confirmarAprobacion.set(false)">Cancelar</button>
            <button type="button" class="btn-primary" (click)="aprobar()">Aprobar versión</button>
          </div>
        </div>
      </div>
    }

    <!-- Drawer de edición del producto -->
    <app-planning-drawer
      [abierto]="!!productoAbierto()"
      titulo="Editar producto estratégico"
      [subtitulo]="borradorNombre"
      (cerrar)="cerrarProducto()"
    >
      @if (productoAbierto()) {
        <div class="flex flex-col gap-4">
          <label class="field-label">Producto estratégico
            <input class="field-control mt-1" [(ngModel)]="borradorNombre" />
          </label>

          <app-multi-select
            etiqueta="Áreas involucradas (organigrama vigente)"
            placeholder="Seleccione una o más áreas"
            placeholderBusqueda="Buscar área…"
            [opciones]="opcionesAreas()"
            [seleccionados]="borradorAreas()"
            (cambio)="borradorAreas.set($event)"
          />

          <div>
            <p class="field-label">Unidades de medida y metas anuales</p>
            <p class="mb-2 !text-xs text-ink-muted">
              Las metas de {{ store.anioActual }} y años anteriores están bloqueadas: corresponden a
              ejecución ya registrada.
            </p>
            <div class="flex flex-col gap-3">
              @for (u of borradorUnidades(); track $index; let i = $index) {
                <div class="rounded-lg border border-border p-3">
                  <div class="flex items-start justify-between gap-2">
                    <label class="field-label flex-1">Unidad de medida
                      <input class="field-control mt-1" [(ngModel)]="u.nombre" />
                    </label>
                    @if (store.unidadTieneHistoria(u)) {
                      <span class="badge-neutral mt-6 shrink-0" title="Tiene metas ejecutadas: no se puede eliminar">Con historia</span>
                    } @else {
                      <button type="button" class="btn-icon mt-5 !text-error-text" title="Eliminar unidad" (click)="eliminarUnidadBorrador(i)">✕</button>
                    }
                  </div>
                  <div class="mt-2 grid grid-cols-4 gap-2">
                    @for (a of anios(); track a.anio) {
                      <label class="field-label">
                        {{ a.anio }} @if (!a.editable) {<span title="Ejecución registrada">🔒</span>}
                        <input
                          type="number"
                          class="field-control mt-1 !px-2 text-right"
                          [ngModel]="u.metasAnuales[a.clave] ?? 0"
                          (ngModelChange)="setMeta(u, a.clave, $event, a.editable)"
                          [disabled]="!a.editable"
                          [class.field-control-disabled]="!a.editable"
                        />
                      </label>
                    }
                  </div>
                  <p class="mt-1 !text-xs text-ink-muted">Total del período: <strong class="tabular-nums">{{ totalMetas(u.metasAnuales) }}</strong></p>
                  <label class="field-label mt-2 block">Responsable (área del organigrama)
                    <select class="field-control mt-1" [(ngModel)]="u.responsableAreaId">
                      <option value="">— Seleccione —</option>
                      @for (a of areasSeleccionables(); track a.id) {
                        <option [value]="a.id">{{ a.nombre }}</option>
                      }
                    </select>
                  </label>
                </div>
              }
              <button type="button" class="btn-secondary btn-sm self-start" (click)="agregarUnidadBorrador()">+ Agregar unidad de medida</button>
            </div>
          </div>
        </div>
      }
      <div slot="footer" class="flex w-full items-center justify-end gap-2">
        <button type="button" class="btn-ghost" (click)="cerrarProducto()">Cancelar</button>
        <button type="button" class="btn-primary" (click)="guardarProducto()">Guardar cambios</button>
      </div>
    </app-planning-drawer>

    <!-- Modal para crear un nuevo PEI -->
    @if (mostrarCrearModal()) {
      <div class="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-xs p-4">
        <div class="card w-full max-w-md p-6 slide-in-top">
          <h2 class="!font-poppins !text-lg font-semibold text-ink">Crear nuevo PEI</h2>
          <p class="mt-1 !text-xs text-ink-muted">Registra un nuevo Plan Estratégico Institucional para un período multianual.</p>
          
          <label class="field-label mt-4 block">Nombre del PEI
            <input class="field-control mt-1" [(ngModel)]="nuevoNombreModal" placeholder="Ej.: Plan Estratégico Institucional 2029-2032" />
          </label>
          <div class="mt-3 grid grid-cols-2 gap-3">
            <label class="field-label">Año inicial
              <input type="number" class="field-control mt-1" [(ngModel)]="nuevoInicioModal" />
            </label>
            <label class="field-label">Año final
              <input type="number" class="field-control mt-1" [(ngModel)]="nuevoFinModal" />
            </label>
          </div>
          <div class="mt-6 flex justify-end gap-2">
            <button type="button" class="btn-ghost" (click)="mostrarCrearModal.set(false)">Cancelar</button>
            <button type="button" class="btn-primary" [disabled]="!nuevoNombreModal.trim() || nuevoFinModal < nuevoInicioModal || creandoPei()" (click)="crearNuevoPeiDesdeModal()">
              {{ creandoPei() ? 'Creando…' : 'Crear PEI' }}
            </button>
          </div>
        </div>
      </div>
    }
  `,
})
export class PeiComponent {
  protected readonly store = inject(PeiStoreService);
  private readonly contexto = inject(PlanningContextService);

  protected readonly th =
    'border-b border-border bg-surface-sunken px-3 py-1.5 text-left text-xs font-semibold whitespace-nowrap text-ink-muted uppercase';

  protected readonly abiertos = signal<string[]>([]);
  protected readonly aviso = signal('');
  protected readonly verCambios = signal(false);

  /**
   * Versión elegida explícitamente. Vacío = automático: se muestra la enmienda
   * en curso si la hay (es lo accionable) y, si no, la vigente.
   */
  protected readonly versionId = signal<string>('');

  // Estado vacío (primer PEI)
  protected nuevoNombre = '';
  protected nuevoInicio = new Date().getFullYear();
  protected nuevoFin = new Date().getFullYear() + 3;

  // Flujo de enmienda
  protected readonly abriendoEnmienda = signal(false);
  protected readonly confirmarDescarte = signal(false);
  protected readonly confirmarAprobacion = signal(false);
  protected motivoEnmienda = '';

  // Importación Excel
  protected readonly importando = signal(false);
  protected readonly resultadoImport = signal<ImportResult | null>(null);
  protected readonly aplicandoImport = signal(false);
  protected archivoImport: File | null = null;
  protected modoImport: 'actualizar' | 'reemplazar' = 'actualizar';

  // Descontinuación
  protected readonly descontinuando = signal<ProductoEstrategico | null>(null);
  protected anioDescontinuacion = this.store.anioActual + 1;
  protected motivoDescontinuacion = '';

  // Formularios inline del árbol
  protected readonly edicion = signal<EdicionInline | null>(null);
  protected textoInline = '';

  // Drawer de producto
  protected readonly productoAbierto = signal<ProductoEstrategico | null>(null);
  protected borradorNombre = '';
  protected readonly borradorAreas = signal<string[]>([]);
  protected readonly borradorUnidades = signal<UnidadMedidaPei[]>([]);

  protected readonly areasSeleccionables = computed(() =>
    this.store.areas().filter((a) => !a.esRaiz),
  );
  protected readonly opcionesAreas = computed<MultiOption[]>(() =>
    this.areasSeleccionables().map((a) => ({ value: a.id, label: a.nombre })),
  );

  // Modal de nuevo PEI
  protected readonly mostrarCrearModal = signal(false);
  protected nuevoNombreModal = '';
  protected nuevoInicioModal = new Date().getFullYear();
  protected nuevoFinModal = new Date().getFullYear() + 3;
  protected readonly creandoPei = signal(false);

  // Selector de PEI por período
  protected readonly periodoSeleccionado = signal<string | null>(null);

  protected readonly peisPorPeriodo = computed(() => {
    const versiones = this.store.peiVersiones();
    const map = new Map<string, Pei[]>();
    for (const v of versiones) {
      const list = map.get(v.periodo) ?? [];
      list.push(v);
      map.set(v.periodo, list);
    }
    const result: { periodo: string; nombre: string; estado: string; versiones: Pei[] }[] = [];
    for (const [periodo, vers] of map.entries()) {
      const vigente = vers.find((v) => v.estado === 'Vigente');
      const borrador = vers.find((v) => v.estado === 'En elaboración');
      const estado = vigente ? 'Vigente' : (borrador ? 'En elaboración' : 'Histórico');
      const nombre = vers[0]?.nombre ?? `PEI ${periodo}`;
      result.push({ periodo, nombre, estado, versiones: vers });
    }
    return result;
  });

  protected readonly peiPlanActual = computed(() => {
    const list = this.peisPorPeriodo();
    const sel = this.periodoSeleccionado();
    if (sel) {
      const found = list.find((p) => p.periodo === sel);
      if (found) return found;
    }
    return list.find((p) => p.estado === 'Vigente') ?? list[0];
  });

  protected readonly versionesPlanActual = computed(() => {
    return this.peiPlanActual()?.versiones ?? [];
  });

  /** La versión mostrada; si la seleccionada desapareció, cae al automático. */
  protected readonly versionMostrada = computed<Pei | undefined>(() => {
    const list = this.versionesPlanActual();
    const id = this.versionId();
    if (id) {
      const encontrada = list.find((v) => v.id === id);
      if (encontrada) return encontrada;
    }
    return list.find((v) => v.estado === 'Vigente') ?? list.find((v) => v.estado === 'En elaboración') ?? list[0];
  });

  /** Años del período de la versión mostrada (dinámicos, M3). */
  protected readonly anios = computed(() => this.store.aniosDe(this.versionMostrada()));

  protected readonly esBorrador = computed(() => this.versionMostrada()?.estado === 'En elaboración');
  protected readonly esPlanificacion = computed(() => {
    this.contexto.context();
    return this.contexto.hasPermission(PLANNING_PERMISSIONS.peiEdit);
  });
  protected readonly esMae = computed(() => {
    this.contexto.context();
    return this.contexto.hasPermission(PLANNING_PERMISSIONS.peiApprove);
  });

  protected readonly proximaVersion = computed(() => {
    const mayor = Number(this.store.pei()?.version.replace(/^v/, '').split('.')[0]) || 1;
    return `v${mayor + 1}.0`;
  });

  private versionIdInicializada = '';

  constructor() {
    void this.contexto.load();
    void this.store.cargar();
    effect(() => {
      const pei = this.versionMostrada();
      if (pei && pei.id !== this.versionIdInicializada) {
        this.versionIdInicializada = pei.id;
        this.abiertos.set(pei.focos.map((f) => f.id));
      }
    });
  }

  protected abrirNuevoPeiModal(): void {
    const nextYear = new Date().getFullYear() + 1;
    this.nuevoNombreModal = `Plan Estratégico Institucional (PEI) ${nextYear}-${nextYear + 3}`;
    this.nuevoInicioModal = nextYear;
    this.nuevoFinModal = nextYear + 3;
    this.mostrarCrearModal.set(true);
  }

  protected async crearNuevoPeiDesdeModal(): Promise<void> {
    if (!this.nuevoNombreModal.trim()) return;
    this.creandoPei.set(true);
    const created = await this.store.crearPeiInicial(
      this.nuevoNombreModal.trim(),
      this.nuevoInicioModal,
      this.nuevoFinModal,
    );
    this.creandoPei.set(false);
    if (created) {
      this.mostrarCrearModal.set(false);
      this.periodoSeleccionado.set(created.periodo);
      this.versionId.set(created.id);
    }
  }

  protected aniosFuturos(): number[] {
    return this.anios().filter((a) => a.editable).map((a) => a.anio);
  }

  protected totalMetas(m: MetasAnuales): number {
    return Object.values(m).reduce((total, valor) => total + (valor || 0), 0);
  }

  protected badgeVersion(estado: string): string {
    switch (estado) {
      case 'Vigente':
        return 'badge-success';
      case 'En elaboración':
        return 'badge-warning';
      default:
        return 'badge-neutral';
    }
  }

  protected badgeCambio(tipo: string): string {
    switch (tipo) {
      case 'Alta':
        return 'badge-success';
      case 'Descontinuación':
        return 'badge-warning';
      default:
        return 'badge-info';
    }
  }

  protected toggle(id: string): void {
    this.abiertos.update((ids) => (ids.includes(id) ? ids.filter((x) => x !== id) : [...ids, id]));
  }

  // --- estado vacío ---------------------------------------------------------
  protected async crearPeiInicial(): Promise<void> {
    const creado = await this.store.crearPeiInicial(
      this.nuevoNombre.trim(),
      this.nuevoInicio,
      this.nuevoFin,
    );
    if (creado) {
      this.versionId.set(creado.id);
      this.aviso.set('PEI creado en elaboración. Cargue el contenido desde Excel o edítelo aquí.');
    }
  }

  // --- flujo de enmienda ----------------------------------------------------
  protected abrirEnmienda(): void {
    this.motivoEnmienda = '';
    this.abriendoEnmienda.set(true);
  }

  protected async crearEnmienda(): Promise<void> {
    const borrador = await this.store.iniciarEnmienda(this.motivoEnmienda.trim());
    this.abriendoEnmienda.set(false);
    if (borrador) {
      this.versionId.set(borrador.id);
      this.aviso.set(
        `Enmienda ${borrador.version} abierta. Edite lo que corresponda y envíela a la MAE para su aprobación.`,
      );
    }
  }

  protected async descartar(): Promise<void> {
    await this.store.descartarEnmienda();
    this.confirmarDescarte.set(false);
    this.versionId.set(this.store.pei()?.id ?? '');
    this.aviso.set('Enmienda descartada. La versión vigente no fue alterada.');
  }

  protected async aprobar(): Promise<void> {
    await this.store.aprobarEnmienda();
    this.confirmarAprobacion.set(false);
    this.versionId.set(this.store.pei()?.id ?? '');
    this.aviso.set('Enmienda aprobada: ya es la versión vigente y el POA alinea contra ella.');
  }

  // --- importación Excel ----------------------------------------------------
  protected abrirImportar(): void {
    this.archivoImport = null;
    this.resultadoImport.set(null);
    this.modoImport = 'actualizar';
    this.importando.set(true);
  }

  protected cerrarImportar(): void {
    this.importando.set(false);
  }

  protected async seleccionarArchivo(event: Event): Promise<void> {
    const archivo = (event.target as HTMLInputElement).files?.[0] ?? null;
    this.archivoImport = archivo;
    this.resultadoImport.set(null);
    if (!archivo) return;

    const planId = this.versionMostrada()?.id;
    if (!planId) return;
    // Validación previa (dryRun): nada se aplica hasta confirmar.
    this.resultadoImport.set(await this.store.importar(planId, archivo, this.modoImport, true));
  }

  protected async aplicarImport(): Promise<void> {
    const planId = this.versionMostrada()?.id;
    if (!planId || !this.archivoImport) return;
    this.aplicandoImport.set(true);
    try {
      const resultado = await this.store.importar(planId, this.archivoImport, this.modoImport, false);
      this.resultadoImport.set(resultado);
      if (resultado.applied) {
        this.importando.set(false);
        this.aviso.set('Importación aplicada: el contenido del PEI fue actualizado.');
      }
    } finally {
      this.aplicandoImport.set(false);
    }
  }

  // --- formularios inline ---------------------------------------------------
  protected esFormInline(tipo: EdicionInline['tipo'], nodoId: string | undefined): boolean {
    const e = this.edicion();
    if (!e || e.tipo !== tipo) return false;
    if (e.modo === 'editar') return e.nodoId === nodoId;
    if (tipo === 'foco') return nodoId === undefined;
    if (tipo === 'objetivo') return nodoId === 'nuevo-' + e.focoId;
    // Alta de producto: el formulario cuelga directamente del objetivo.
    return nodoId === e.objetivoId;
  }

  protected iniciarAgregar(
    tipo: EdicionInline['tipo'],
    ids: { focoId?: string; objetivoId?: string } = {},
  ): void {
    this.textoInline = '';
    this.edicion.set({ modo: 'agregar', tipo, ...ids });
  }

  protected iniciarEditar(
    tipo: EdicionInline['tipo'],
    valorActual: string,
    ids: { focoId?: string; objetivoId?: string },
  ): void {
    this.textoInline = valorActual;
    const nodoId = tipo === 'foco' ? ids.focoId : ids.objetivoId;
    this.edicion.set({ modo: 'editar', tipo, nodoId, ...ids });
  }

  protected cancelarInline(): void {
    this.edicion.set(null);
    this.textoInline = '';
  }

  protected guardarInline(): void {
    const e = this.edicion();
    const texto = this.textoInline.trim();
    if (!e || !texto) return;
    if (e.modo === 'agregar') {
      if (e.tipo === 'foco') this.store.agregarFoco(texto, 'Alineación END 2030 por definir');
      if (e.tipo === 'objetivo') this.store.agregarObjetivo(e.focoId!, texto);
      if (e.tipo === 'producto') this.store.agregarProducto(e.focoId!, e.objetivoId!, texto);
    } else {
      if (e.tipo === 'foco') this.store.editarFoco(e.focoId!, texto);
      if (e.tipo === 'objetivo') this.store.editarObjetivo(e.focoId!, e.objetivoId!, texto);
    }
    this.cancelarInline();
  }

  // --- eliminaciones con guarda de historia ---------------------------------
  private avisoNoEliminable(que: string): void {
    this.aviso.set(
      `No se puede eliminar ${que}: tiene metas comprometidas en años ya transcurridos. Descontinúelo desde un año futuro para conservar la historia.`,
    );
  }

  protected borrarFoco(focoId: string): void {
    if (!this.store.eliminarFoco(focoId)) this.avisoNoEliminable('este foco');
  }

  protected borrarObjetivo(focoId: string, objetivoId: string): void {
    if (!this.store.eliminarObjetivo(focoId, objetivoId)) this.avisoNoEliminable('este objetivo');
  }

  protected borrarProducto(productoId: string): void {
    if (!this.store.eliminarProducto(productoId)) this.avisoNoEliminable('este producto');
  }

  // --- descontinuación ------------------------------------------------------
  protected abrirDescontinuar(prod: ProductoEstrategico): void {
    this.descontinuando.set(prod);
    this.anioDescontinuacion = this.aniosFuturos()[0] ?? this.store.anioActual + 1;
    this.motivoDescontinuacion = '';
  }

  protected confirmarDescontinuar(): void {
    const prod = this.descontinuando();
    if (!prod) return;
    this.store.descontinuarProducto(prod.id, this.anioDescontinuacion, this.motivoDescontinuacion.trim());
    this.aviso.set(
      `«${prod.nombre}» se descontinúa desde ${this.anioDescontinuacion}. Sus metas anteriores siguen visibles.`,
    );
    this.descontinuando.set(null);
  }

  // --- drawer de producto ---------------------------------------------------
  protected abrirProducto(prod: ProductoEstrategico): void {
    this.productoAbierto.set(prod);
    this.borradorNombre = prod.nombre;
    this.borradorAreas.set([...prod.areasInvolucradasIds]);
    this.borradorUnidades.set(structuredClone(prod.unidades));
  }

  protected cerrarProducto(): void {
    this.productoAbierto.set(null);
  }

  /** Ignora cualquier intento de escribir sobre un año ya ejecutado. */
  protected setMeta(u: UnidadMedidaPei, clave: string, valor: number, editable: boolean): void {
    if (!editable) return;
    (u.metasAnuales as unknown as Record<string, number>)[clave] = Number(valor) || 0;
  }

  protected agregarUnidadBorrador(): void {
    const metas: MetasAnuales = {};
    for (const a of this.anios()) metas[a.clave] = 0;
    this.borradorUnidades.update((us) => [
      ...us,
      { id: '', nombre: '', metasAnuales: metas, responsableAreaId: '' },
    ]);
  }

  protected eliminarUnidadBorrador(i: number): void {
    this.borradorUnidades.update((us) => us.filter((_, idx) => idx !== i));
  }

  protected guardarProducto(): void {
    const prod = this.productoAbierto();
    if (!prod) return;
    this.store.editarProducto(prod.id, {
      nombre: this.borradorNombre.trim() || prod.nombre,
      areasInvolucradasIds: this.borradorAreas(),
    });
    this.store.reemplazarUnidadesPei(prod.id, this.borradorUnidades());
    this.cerrarProducto();
  }
}
