import { AfterViewInit, Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';
import { PoaStoreService } from '../../../../services/planning/poa-store.service';
import { RequirementStoreService } from '../../../../services/planning/requirement-store.service';
import { ImportResult } from '../../../../services/planning/pei-store.service';
import { PlanGridComponent } from '../../components/plan-grid.component';
import { PlanningDrawerComponent } from '../../components/planning-drawer.component';
import { MultiSelectComponent, MultiOption } from '../../components/multi-select.component';
import { MasterProductPickerComponent } from '../../components/master-product-picker.component';
import {
  OrigenProducto,
  ProductoPoa,
  TareaPoa,
  Trimestre,
  trimestreDeTarea,
} from '../../data/planning.models';
import { MasterProductSearchItemDto, RequirementDto } from '../../data/planning-api.models';
import {
  FUENTES_FINANCIAMIENTO,
  MESES_CORTOS,
  montoDe,
  formatRD,
} from '../../data/planning.mock-data';

type FiltroFilas = 'Todas' | 'Incompletas' | 'Con apoyo transversal';
type TabDrawer =
  | 'General'
  | 'Unidades y metas'
  | 'Tareas y cronograma'
  | 'Requerimientos'
  | 'Historial';

/**
 * Workspace del plan anual (formato oficial de 13 columnas): grid ancho con
 * columnas fijadas + drawer de detalle por producto. Los montos no viven aquí:
 * se capturan en los requerimientos (formato PACC) desde la pestaña
 * correspondiente del drawer.
 */
@Component({
  selector: 'app-plan-workspace',
  imports: [
    RouterLink,
    FormsModule,
    PlanGridComponent,
    PlanningDrawerComponent,
    MultiSelectComponent,
    MasterProductPickerComponent,
  ],
  template: `
    @let plan = planActual();
    @if (store.cargando() || !store.cargado()) {
      <div
        class="card flex flex-col items-center justify-center p-12 text-center text-ink-muted min-h-[350px] m-6"
      >
        <span
          class="inline-block size-8 animate-spin rounded-full border-4 border-brand border-t-transparent mb-3"
        ></span>
        <p class="text-base font-semibold text-ink">Cargando expediente del plan...</p>
        <p class="text-xs text-ink-muted mt-1">
          Por favor espere mientras se obtienen los datos del servidor.
        </p>
      </div>
    } @else if (!plan) {
      <div class="card mx-auto my-12 max-w-md p-8 text-center m-6">
        <p class="text-base font-semibold text-ink">Plan no encontrado</p>
        <p class="mt-1 text-xs text-ink-muted">
          El plan solicitado no existe o no tiene permisos para acceder a él.
        </p>
        <a routerLink="/plan-anual" class="btn-primary btn-sm mt-4 inline-flex !no-underline"
          >← Volver al listado de planes</a
        >
      </div>
    } @else {
      @let area = store.area(plan.areaId);
      <div class="flex h-full flex-col gap-3 p-6">
        <!-- Cabecera fija del expediente -->
        <div class="card p-4">
          <div class="flex flex-wrap items-start justify-between gap-3">
            <div class="min-w-0">
              <a
                routerLink="/plan-anual"
                class="!text-xs !text-ink-muted !no-underline hover:!underline"
                >← Plan anual</a
              >
              <h1 class="!font-poppins mt-0.5 !text-xl font-semibold text-ink">
                {{ plan.nombre }}
              </h1>
              <p class="mt-0.5 flex flex-wrap items-center gap-2 !text-xs text-ink-muted">
                <span>{{ area?.nombre }}</span> · <span>Período 2027</span> ·
                <span>Fecha límite: {{ plan.fechaLimite }}</span> ·
                <span class="badge-neutral">v{{ plan.version }}</span>
                <span [class]="badgeEstado(plan.estado)">{{ plan.estado }}</span>
              </p>
            </div>
            <div class="flex shrink-0 items-center gap-2">
              @if (plan.estado === 'En ajustes') {
                <a
                  [routerLink]="'/plan-anual/' + plan.id + '/ajustes'"
                  class="btn-primary !no-underline"
                >
                  Reprogramar y reenviar
                </a>
              }
              @if (puedeDevolver()) {
                <button
                  type="button"
                  class="btn-secondary"
                  (click)="devolviendo.set(!devolviendo())"
                >
                  Devolver con observaciones
                </button>
              }
              @if (editable()) {
                <a
                  [routerLink]="'/plan-anual/' + plan.id + '/revisar'"
                  class="btn-primary !no-underline"
                >
                  Revisar y someter
                </a>
              }
            </div>
          </div>

          <!-- Barra de progreso (completitud del formato) -->
          <div class="mt-3 flex items-center gap-3">
            <div class="h-2 flex-1 overflow-hidden rounded-full bg-surface-sunken">
              <div
                class="h-full rounded-full bg-brand transition-all"
                [style.width.%]="progreso()"
              ></div>
            </div>
            <span class="!text-xs whitespace-nowrap tabular-nums text-ink-muted">
              {{ filasCompletas() }} de {{ plan.rows.length }} productos completos · requerimientos
              del área: {{ fmt(montoRequerimientos()) }}
            </span>
          </div>

          @if (plan.estado === 'Observado' && plan.observacionGeneral) {
            <div
              class="mt-3 rounded-lg bg-error-bg px-3 py-2 text-sm text-error-text ring-1 ring-error-border"
            >
              <strong>Observación de Planificación:</strong> {{ plan.observacionGeneral }}
            </div>
          }
          @if (devolviendo()) {
            <div class="mt-3 flex flex-col gap-2 rounded-lg bg-surface-sunken p-3">
              <label class="field-label" for="obs">Observación para el área</label>
              <textarea
                id="obs"
                class="!h-20 w-full rounded-lg border border-border-strong bg-white px-3 py-2 text-sm"
                [(ngModel)]="textoObservacion"
              ></textarea>
              <div class="flex justify-end gap-2">
                <button type="button" class="btn-ghost btn-sm" (click)="devolviendo.set(false)">
                  Cancelar
                </button>
                <button
                  type="button"
                  class="btn-danger btn-sm"
                  [disabled]="!textoObservacion.trim()"
                  (click)="devolver()"
                >
                  Devolver plan
                </button>
              </div>
            </div>
          }
        </div>

        <!-- Toolbar de trabajo -->
        <div class="card flex flex-wrap items-center justify-between gap-3 px-4 py-3">
          <div class="flex flex-wrap items-center gap-2.5">
            <div class="relative min-w-[240px]">
              <input
                type="search"
                placeholder="Buscar producto…"
                class="!h-9 w-full rounded-lg border border-border bg-surface pl-3 pr-3 text-sm focus:border-brand"
                [ngModel]="busqueda()"
                (ngModelChange)="busqueda.set($event)"
              />
            </div>
            <select
              class="!h-9 w-auto rounded-lg border border-border bg-surface px-3 text-sm font-medium text-ink focus:border-brand"
              [ngModel]="filtroFilas()"
              (ngModelChange)="filtroFilas.set($event)"
            >
              <option value="Todas">Todos los productos</option>
              <option value="Incompletas">Solo incompletos</option>
              <option value="Con apoyo transversal">Con apoyo transversal</option>
            </select>
          </div>

          <div class="flex shrink-0 items-center gap-2">
            @if (editable()) {
              <button
                type="button"
                class="btn-secondary btn-sm flex items-center gap-1.5"
                [disabled]="store.descargando()"
                (click)="descargarPlantilla()"
              >
                @if (store.descargando()) {
                  <span
                    class="inline-block size-3.5 animate-spin rounded-full border-2 border-brand border-t-transparent"
                  ></span>
                }
                Descargar plantilla
              </button>
              <button
                type="button"
                class="btn-secondary btn-sm flex items-center gap-1.5"
                (click)="panelImportar.set(!panelImportar())"
              >
                Importar Excel
              </button>
            }
            <button
              type="button"
              class="btn-secondary btn-sm flex items-center gap-1.5"
              [disabled]="store.descargando()"
              (click)="exportar()"
            >
              @if (store.descargando()) {
                <span
                  class="inline-block size-3.5 animate-spin rounded-full border-2 border-brand border-t-transparent"
                ></span>
              }
              Exportar
            </button>
            @if (editable()) {
              <button type="button" class="btn-primary btn-sm" (click)="agregarFila()">
                + Agregar producto
              </button>
            }
          </div>
        </div>

        @if (mensajeArchivo()) {
          <div
            class="rounded-lg bg-success-bg px-3 py-2 text-sm text-success-text ring-1 ring-success-border"
          >
            {{ mensajeArchivo() }}
            <button type="button" class="btn-ghost btn-sm ml-2" (click)="mensajeArchivo.set('')">
              Cerrar
            </button>
          </div>
        }

        <!-- Asistente de carga desde la plantilla -->
        @if (panelImportar()) {
          <div class="card flex flex-col gap-3 p-4">
            <div class="flex items-start justify-between gap-3">
              <div>
                <h2 class="!font-poppins !text-base font-semibold text-ink">
                  Cargar productos desde la plantilla
                </h2>
                <p class="mt-1 !text-sm text-ink-muted">
                  La plantilla es un Excel (.xlsx) con una fila por
                  <strong>unidad de medida</strong>: las filas con el mismo código se agrupan en un
                  producto. Dentro de una celda, las listas se separan con «|». El servidor valida
                  todo el archivo antes de aplicar.
                </p>
              </div>
              <button
                type="button"
                class="btn-icon shrink-0"
                (click)="cerrarImportacion()"
                aria-label="Cerrar"
              >
                ✕
              </button>
            </div>

            <div class="flex flex-wrap items-center gap-2">
              <button
                type="button"
                class="btn-secondary btn-sm flex items-center gap-1.5"
                [disabled]="store.descargando()"
                (click)="descargarPlantilla()"
              >
                @if (store.descargando()) {
                  <span
                    class="inline-block h-3.5 w-3.5 animate-spin rounded-full border-2 border-brand border-t-transparent"
                  ></span>
                }
                Descargar plantilla vacía
              </button>
              <button
                type="button"
                class="btn-secondary btn-sm flex items-center gap-1.5"
                [disabled]="store.descargando()"
                (click)="exportar()"
              >
                @if (store.descargando()) {
                  <span
                    class="inline-block h-3.5 w-3.5 animate-spin rounded-full border-2 border-brand border-t-transparent"
                  ></span>
                }
                Descargar el plan actual
              </button>
              <input
                type="file"
                accept=".xlsx"
                class="!h-9 w-auto !py-1.5 !text-sm"
                (change)="archivoSeleccionado($event)"
              />
              @if (validandoArchivo()) {
                <span class="!text-sm text-ink-muted">Validando archivo…</span>
              }
            </div>

            <div class="flex flex-wrap items-center gap-3">
              <label class="field-label !flex items-center gap-2">
                <input
                  type="radio"
                  name="modoImport"
                  class="!size-4 accent-brand"
                  value="actualizar"
                  [ngModel]="modoImport"
                  (ngModelChange)="setModoImport($event)"
                />
                Actualizar por código (conserva lo que no viene en el archivo)
              </label>
              <label class="field-label !flex items-center gap-2">
                <input
                  type="radio"
                  name="modoImport"
                  class="!size-4 accent-brand"
                  value="reemplazar"
                  [ngModel]="modoImport"
                  (ngModelChange)="setModoImport($event)"
                />
                Reemplazar todo el plan
              </label>
            </div>

            @let previa = previsualizacion();
            @if (previa) {
              <div class="rounded-lg bg-surface-sunken px-3 py-2.5">
                @if (previa.errors.length) {
                  <p class="!text-sm font-semibold text-error-text">
                    {{ previa.errors.length }} error(es) que impiden la carga:
                  </p>
                  <ul class="mt-1 flex flex-col gap-0.5">
                    @for (e of previa.errors.slice(0, 12); track $index) {
                      <li class="!text-sm text-error-text">
                        Fila {{ e.row }}
                        @if (e.column) {
                          · {{ e.column }}
                        }
                        : {{ e.message }}
                      </li>
                    }
                  </ul>
                  @if (previa.errors.length > 12) {
                    <p class="!text-xs text-error-text">…y {{ previa.errors.length - 12 }} más.</p>
                  }
                } @else if (previa.summary; as s) {
                  <p class="!text-sm font-semibold text-success-text">
                    Archivo válido: {{ s.added }} producto(s) por agregar · {{ s.updated }} por
                    actualizar · {{ s.unchanged }} conservado(s) · {{ s.removed }} por eliminar.
                  </p>
                }
                @for (a of previa.warnings; track $index) {
                  <p class="mt-1 !text-sm text-warning-text">⚠ {{ a }}</p>
                }
              </div>

              <button
                type="button"
                class="btn-primary btn-sm self-start"
                [disabled]="!!previa.errors.length || aplicandoImport()"
                (click)="aplicarImportacion()"
              >
                {{ aplicandoImport() ? 'Aplicando…' : 'Aplicar carga' }}
              </button>
            }
          </div>
        }

        <!-- Contenido principal -->
        <div class="card min-h-0 flex-1 overflow-hidden p-0 border border-border shadow-xs">
          <app-plan-grid
            [planId]="plan.id"
            [rows]="filasVisibles()"
            [editable]="editable()"
            [resaltarErrores]="resaltarErrores()"
            (abrirFila)="abrirDrawer($event)"
          />
        </div>

        <!-- Panel de incidencias -->
        @if (incidencias().length) {
          <div class="card p-4 border border-error-border bg-error-bg/30">
            <button
              type="button"
              class="!flex w-full items-center justify-between !rounded-lg !bg-transparent !p-0 !normal-case text-left"
              (click)="incidenciasAbiertas.set(!incidenciasAbiertas())"
            >
              <div class="flex items-center gap-2">
                <span
                  class="flex size-6 items-center justify-center rounded-full bg-error-bg text-error-text font-bold text-xs"
                  >!</span
                >
                <span class="!text-sm font-semibold text-error-text"
                  >{{ incidencias().length }} producto(s) con información faltante</span
                >
              </div>
              <span class="btn-ghost btn-sm text-error-text font-medium">{{
                incidenciasAbiertas() ? 'Ocultar ▴' : 'Ver detalle ▾'
              }}</span>
            </button>
            @if (incidenciasAbiertas()) {
              <ul class="mt-3 flex flex-col gap-1.5 border-t border-error-border/40 pt-3">
                @for (inc of incidencias(); track inc.row.id) {
                  <li>
                    <button
                      type="button"
                      class="!m-0 !rounded-lg !bg-surface-raised/80 px-3 py-1.5 text-left !text-xs !font-medium !normal-case !text-error-text hover:bg-surface-raised transition-colors border border-error-border/30 w-full flex items-center justify-between"
                      (click)="enfocarFila(inc.row.id)"
                    >
                      <span
                        ><strong class="font-semibold">{{ inc.row.codigo }}:</strong>
                        {{ inc.issues.join(' · ') }}</span
                      >
                      <span class="text-[11px] underline shrink-0 ml-2">Editar producto →</span>
                    </button>
                  </li>
                }
              </ul>
            }
          </div>
        }
      </div>

      <!-- Drawer de detalle del producto -->
      <app-planning-drawer
        [abierto]="!!filaAbierta()"
        [titulo]="filaAbierta()?.codigo + ' · ' + (borrador()?.nombreProducto || 'Nuevo producto')"
        [subtitulo]="area?.nombre ?? ''"
        (cerrar)="cerrarDrawer()"
      >
        @if (borrador(); as b) {
          <div class="segmented mb-4 w-full flex-wrap">
            @for (t of tabsDrawer; track t) {
              <button
                type="button"
                class="segmented-item justify-center"
                [class.segmented-item-active]="tabDrawer() === t"
                (click)="tabDrawer.set(t)"
              >
                {{ t }}
              </button>
            }
          </div>

          @switch (tabDrawer()) {
            @case ('General') {
              <div class="flex flex-col gap-3.5">
                <!-- Ajuste 2: ¿el producto responde al PEI o es operativo? -->
                <p class="!text-xs font-semibold tracking-wide text-ink-subtle uppercase">
                  ¿Este producto responde al PEI?
                </p>
                <div class="segmented w-full">
                  <button
                    type="button"
                    class="segmented-item justify-center"
                    [class.segmented-item-active]="b.origen === 'pei'"
                    (click)="setOrigen('pei')"
                  >
                    Sí, alinear al PEI
                  </button>
                  <button
                    type="button"
                    class="segmented-item justify-center"
                    [class.segmented-item-active]="b.origen === 'operativo'"
                    (click)="setOrigen('operativo')"
                  >
                    No aplica (actividad operativa)
                  </button>
                </div>

                @if (b.origen === 'operativo') {
                  <div
                    class="rounded-lg bg-info-bg px-3 py-2 !text-xs text-info-text ring-1 ring-info-border"
                  >
                    Producto operativo: no se alinea al PEI. Disponible para todas las áreas.
                    Indique la justificación de por qué se incluye en el POA.
                  </div>
                  <label class="field-label"
                    >Justificación operativa
                    <textarea
                      class="field-control mt-1 !h-20"
                      [ngModel]="b.justificacionOperativa ?? ''"
                      (ngModelChange)="setJustificacion($event)"
                      placeholder="Ej.: actividad de soporte requerida por normativa, no vinculada a un producto estratégico."
                    ></textarea>
                  </label>
                } @else {
                  <p class="!text-xs font-semibold tracking-wide text-ink-subtle uppercase">
                    Alineación con el plan estratégico
                  </p>
                  <label class="field-label"
                    >Foco estratégico
                    <select
                      class="field-control mt-1"
                      [ngModel]="b.focoId"
                      (ngModelChange)="setFoco($event)"
                    >
                      <option [ngValue]="null">— Seleccione —</option>
                      @for (f of store.pei().focos; track f.id) {
                        <option [ngValue]="f.id">{{ f.nombre }}</option>
                      }
                    </select>
                  </label>
                  <label class="field-label"
                    >Objetivo estratégico
                    <select
                      class="field-control mt-1"
                      [ngModel]="b.objetivoId"
                      (ngModelChange)="setObjetivo($event)"
                      [disabled]="!b.focoId"
                    >
                      <option [ngValue]="null">— Seleccione —</option>
                      @for (o of objetivosDisponibles(); track o.id) {
                        <option [ngValue]="o.id">{{ o.codigo }} — {{ o.nombre }}</option>
                      }
                    </select>
                  </label>

                  <p class="mt-1 !text-xs font-semibold tracking-wide text-ink-subtle uppercase">
                    Producto
                  </p>
                  <label class="field-label"
                    >Producto estratégico del PEI (opcional)
                    <select
                      class="field-control mt-1"
                      [ngModel]="b.productoEstrategicoId"
                      (ngModelChange)="seleccionarProductoPei($event)"
                      [disabled]="!b.objetivoId"
                    >
                      <option [ngValue]="null">— Sin producto específico del PEI —</option>
                      @for (p of productosDisponibles(); track p.id) {
                        <option [ngValue]="p.id">{{ p.nombre }}</option>
                      }
                    </select>
                  </label>
                  @if (autocompletado()) {
                    <p
                      class="rounded-lg bg-info-bg px-3 py-2 !text-xs text-info-text ring-1 ring-info-border"
                    >
                      Se completaron unidades de medida, metas del año, responsable y áreas
                      involucradas desde el PEI. Puede ajustarlas.
                    </p>
                  }
                }
                <label class="field-label"
                  >Nombre del producto
                  <input class="field-control mt-1" [(ngModel)]="b.nombreProducto" />
                </label>

                <app-multi-select
                  etiqueta="Municipios de impacto"
                  placeholder="Seleccione uno o más municipios"
                  placeholderBusqueda="Buscar municipio…"
                  [opciones]="opcionesMunicipios()"
                  [seleccionados]="b.municipiosImpacto"
                  (cambio)="setMunicipios($event)"
                />

                <app-multi-select
                  etiqueta="Áreas involucradas (organigrama vigente)"
                  placeholder="Seleccione una o más áreas"
                  placeholderBusqueda="Buscar área…"
                  [opciones]="opcionesAreas()"
                  [seleccionados]="b.areasInvolucradasIds"
                  (cambio)="setAreasInvolucradas($event)"
                />

                @for (col of store.columnasPersonalizadas(); track col.id) {
                  <label class="field-label" [for]="'custom-' + col.id">{{
                    col.nombreVisible
                  }}</label>
                  <div class="-mt-2">
                    @if (col.tipo === 'seleccion' && col.opciones?.length) {
                      <select
                        [id]="'custom-' + col.id"
                        class="field-control mt-1"
                        [ngModel]="b.custom[col.nombreTecnico] ?? ''"
                        (ngModelChange)="b.custom[col.nombreTecnico] = $event"
                      >
                        <option value="">—</option>
                        @for (op of col.opciones; track op) {
                          <option [value]="op">{{ op }}</option>
                        }
                      </select>
                    } @else {
                      <input
                        [id]="'custom-' + col.id"
                        class="field-control mt-1"
                        [ngModel]="b.custom[col.nombreTecnico] ?? ''"
                        (ngModelChange)="b.custom[col.nombreTecnico] = $event"
                      />
                    }
                  </div>
                }
              </div>
            }
            @case ('Unidades y metas') {
              <div class="flex flex-col gap-3">
                @for (u of b.unidades; track u.id; let i = $index) {
                  <div class="rounded-lg border border-border p-3">
                    <div class="flex items-start justify-between gap-2">
                      <label class="field-label flex-1"
                        >Unidad de medida
                        <input class="field-control mt-1" [(ngModel)]="u.nombre" />
                      </label>
                      <button
                        type="button"
                        class="btn-icon mt-5 !text-error-text"
                        title="Eliminar unidad"
                        (click)="eliminarUnidad(i)"
                      >
                        ✕
                      </button>
                    </div>
                    <div class="mt-2 grid grid-cols-2 gap-3">
                      <label class="field-label"
                        >Meta anual
                        <input
                          type="number"
                          class="field-control mt-1 text-right"
                          [(ngModel)]="u.metaValor"
                        />
                      </label>
                      <label class="field-label"
                        >Tipo de medición
                        <select class="field-control mt-1" [(ngModel)]="u.metaTipo">
                          <option value="absoluto">Valor absoluto</option>
                          <option value="porcentaje">Porcentaje</option>
                        </select>
                      </label>
                    </div>
                    <label class="field-label mt-2 block"
                      >Medios de verificación
                      <input
                        class="field-control mt-1"
                        [(ngModel)]="u.mediosVerificacion"
                        placeholder="Ej.: actas, reportes del sistema, listas de asistencia"
                      />
                    </label>
                    <label class="field-label mt-2 block"
                      >Responsable (área del organigrama)
                      <select class="field-control mt-1" [(ngModel)]="u.responsableAreaId">
                        <option value="">— Seleccione —</option>
                        @for (a of areasSeleccionables(); track a.id) {
                          <option [value]="a.id">{{ a.nombre }}</option>
                        }
                      </select>
                    </label>
                    <p class="field-label mt-2">Programación de metas por trimestre</p>
                    <div class="grid grid-cols-4 gap-2">
                      <label class="field-label"
                        >T1
                        <input
                          type="number"
                          class="field-control mt-1 !px-2 text-right"
                          [(ngModel)]="u.programacion.T1"
                        />
                      </label>
                      <label class="field-label"
                        >T2
                        <input
                          type="number"
                          class="field-control mt-1 !px-2 text-right"
                          [(ngModel)]="u.programacion.T2"
                        />
                      </label>
                      <label class="field-label"
                        >T3
                        <input
                          type="number"
                          class="field-control mt-1 !px-2 text-right"
                          [(ngModel)]="u.programacion.T3"
                        />
                      </label>
                      <label class="field-label"
                        >T4
                        <input
                          type="number"
                          class="field-control mt-1 !px-2 text-right"
                          [(ngModel)]="u.programacion.T4"
                        />
                      </label>
                    </div>
                    @if (avisoProgramacion(u)) {
                      <p
                        class="mt-2 rounded-lg bg-warning-bg px-3 py-1.5 !text-xs text-warning-text ring-1 ring-warning-border"
                      >
                        {{ avisoProgramacion(u) }}
                      </p>
                    }
                  </div>
                }
                <button
                  type="button"
                  class="btn-secondary btn-sm self-start"
                  (click)="agregarUnidad()"
                >
                  + Agregar unidad de medida
                </button>
              </div>
            }
            @case ('Tareas y cronograma') {
              <div class="flex flex-col gap-3">
                <div class="flex items-center justify-between gap-2">
                  <p class="!text-xs text-ink-muted">
                    Las tareas no requieren medición: orientan la ejecución. El cronograma genera
                    alertas de vencimiento en el seguimiento y es el origen de los requerimientos de
                    apoyo.
                  </p>
                  <span class="badge-neutral shrink-0">{{ b.tareas.length }} tarea(s)</span>
                </div>
                <p class="rounded-lg bg-surface-sunken px-3 py-1.5 !text-xs text-ink-muted">
                  Registre tantas tareas como necesite: el sistema no impone un máximo por producto.
                </p>
                @if (avisoTarea()) {
                  <p
                    class="rounded-lg bg-error-bg px-3 py-2 !text-sm text-error-text ring-1 ring-error-border"
                  >
                    {{ avisoTarea() }}
                  </p>
                }
                @for (t of b.tareas; track t.id; let i = $index) {
                  @let reqsTarea = reqsDeTarea(t.id);
                  <div class="rounded-lg border border-border p-3">
                    <div class="flex items-start justify-between gap-2">
                      <label class="field-label flex-1"
                        >Tarea
                        <input class="field-control mt-1" [(ngModel)]="t.nombre" />
                      </label>
                      <button
                        type="button"
                        class="btn-icon mt-5 !text-error-text"
                        title="Eliminar tarea"
                        (click)="eliminarTarea(i)"
                      >
                        ✕
                      </button>
                    </div>
                    @if (reqsTarea.length) {
                      <button
                        type="button"
                        class="btn-ghost btn-sm !mt-1 !px-0 !normal-case"
                        (click)="tabDrawer.set('Requerimientos')"
                      >
                        <span class="badge-info"
                          >{{ reqsTarea.length }} requerimiento(s) de apoyo</span
                        >
                      </button>
                    }
                    <!-- Ajuste 4: ejecución continua durante todo el año. -->
                    <label
                      class="!flex mt-2 cursor-pointer items-center gap-2 !text-sm !font-normal text-ink"
                    >
                      <input
                        type="checkbox"
                        class="!size-4 accent-brand"
                        [checked]="t.continua"
                        (change)="toggleContinua(t.id)"
                      />
                      Ejecución continua durante todo el año
                    </label>
                    @if (t.continua) {
                      <p
                        class="mt-2 rounded-lg bg-info-bg px-3 py-1.5 !text-xs text-info-text ring-1 ring-info-border"
                      >
                        Tarea permanente: se ejecuta todos los meses y no genera alertas de
                        vencimiento por mes.
                      </p>
                    } @else {
                      <p class="field-label mt-2">Meses de ejecución</p>
                      <div class="flex flex-wrap gap-1.5">
                        @for (mes of meses; track $index; let mi = $index) {
                          <!-- El modificador important en flex es necesario: el sistema de diseño
                               declara los label como inline-block sin capa y gana sobre Tailwind. -->
                          <label
                            class="!flex size-8 cursor-pointer items-center justify-center rounded-lg border !text-xs !font-bold"
                            [class]="
                              t.meses.includes(mi)
                                ? 'border-brand bg-brand-50 text-brand'
                                : 'border-border text-ink-subtle hover:bg-surface'
                            "
                          >
                            <input
                              type="checkbox"
                              class="sr-only"
                              [checked]="t.meses.includes(mi)"
                              (change)="toggleMesTarea(t.id, mi)"
                            />
                            {{ mes }}
                          </label>
                        }
                      </div>
                    }
                  </div>
                }
                <button
                  type="button"
                  class="btn-secondary btn-sm self-start"
                  (click)="agregarTarea()"
                >
                  + Agregar tarea
                </button>
              </div>
            }
            @case ('Requerimientos') {
              <div class="flex flex-col gap-3">
                @let tareasReq = tareasSolicitables();

                <!-- 1. Lo ya solicitado, agrupado por la tarea que lo originó -->
                @for (g of requerimientosPorTarea(); track g.tareaId) {
                  <p class="!text-xs font-semibold tracking-wide text-ink-subtle uppercase">
                    {{ g.nombre }}
                  </p>
                  @for (r of g.items; track r.id) {
                    <div
                      class="rounded-lg border p-3"
                      [class]="
                        editandoReqId() === r.id
                          ? 'border-brand ring-2 ring-brand-200'
                          : 'border-border'
                      "
                    >
                      <p class="!text-sm font-medium text-ink">
                        {{ r.code }} · {{ r.masterProductName }}
                      </p>
                      <p class="!text-xs text-ink-subtle">{{ r.description }}</p>
                      <p class="mt-0.5 !text-xs text-ink-muted">
                        {{ r.quantity }} {{ r.unitOfMeasure }}
                        @if (r.estimatedUnitPrice > 0) {
                          × {{ fmt(r.estimatedUnitPrice) }} =
                          <strong class="tabular-nums">{{ fmt(r.calculatedTotal) }}</strong>
                        }
                        · {{ r.quarter }}
                        @if (r.fundingSource) {
                          · {{ r.fundingSource }}
                        }
                        @if (!r.priceConfirmed && r.estimatedUnitPrice > 0) {
                          <span class="text-warning-text"> · precio de referencia</span>
                        }
                      </p>
                      <!-- Códigos regulatorios: derivados, solo informativos. -->
                      <p class="mt-0.5 !text-xs text-ink-subtle">
                        UNSPSC {{ r.unspscCode || '—' }} · objeto {{ r.expenseObjectCode || '—' }} ·
                        cuenta {{ r.accountingAccountCode || '—' }}
                      </p>
                      @if (r.appliedRule) {
                        <p class="mt-0.5 !text-xs text-ink-muted">{{ r.appliedRule }}</p>
                      }
                      <div class="mt-1.5 flex flex-wrap items-center gap-2">
                        <span [class]="badgeEstadoReq(r.status)">{{
                          etiquetaEstadoReq(r.status)
                        }}</span>
                        <span class="!text-xs text-ink-subtle">→ {{ r.consolidatorAreaName }}</span>
                        @if (editable()) {
                          @if (r.status === 'New' || r.status === 'Returned') {
                            <button
                              type="button"
                              class="btn-ghost btn-sm"
                              (click)="editarRequerimiento(r)"
                            >
                              Editar
                            </button>
                            <button
                              type="button"
                              class="btn-ghost btn-sm !text-error-text"
                              (click)="eliminandoReqId.set(r.id)"
                            >
                              Eliminar
                            </button>
                          } @else {
                            <span class="!text-xs text-ink-subtle">
                              Ya está en manos del área consolidadora: pida su devolución para
                              modificarlo.
                            </span>
                          }
                        }
                      </div>
                      @if (eliminandoReqId() === r.id) {
                        <div class="mt-2 rounded-lg bg-error-bg px-3 py-2 ring-1 ring-error-border">
                          <p class="!text-sm text-error-text">
                            ¿Eliminar {{ r.code }}?
                            @if (r.calculatedTotal > 0) {
                              Se libera {{ fmt(r.calculatedTotal) }} del techo del área.
                            }
                            Queda registrado en la bitácora.
                          </p>
                          <div class="mt-2 flex gap-2">
                            <button
                              type="button"
                              class="btn-ghost btn-sm"
                              (click)="eliminandoReqId.set('')"
                            >
                              Cancelar
                            </button>
                            <button
                              type="button"
                              class="btn-danger btn-sm"
                              (click)="eliminarRequerimiento(r)"
                            >
                              Sí, eliminar
                            </button>
                          </div>
                        </div>
                      }
                    </div>
                  }
                } @empty {
                  <p class="!text-sm text-ink-muted">
                    Este producto todavía no tiene requerimientos de apoyo.
                  </p>
                }

                <!-- 2. Alta o edición: el apoyo se pide desde una tarea -->
                @if (editable()) {
                  @if (!tareasReq.length) {
                    <div class="rounded-lg bg-warning-bg px-3 py-3 ring-1 ring-warning-border">
                      <p class="!text-sm font-semibold text-warning-text">
                        Este producto todavía no tiene tareas
                      </p>
                      <p class="mt-1 !text-sm text-warning-text">
                        El apoyo transversal se solicita desde la tarea que lo necesita, no desde el
                        producto: es la tarea la que define en qué meses hace falta. Agregue al
                        menos una tarea con nombre para poder crear requerimientos.
                      </p>
                      <button
                        type="button"
                        class="btn-secondary btn-sm mt-2"
                        (click)="tabDrawer.set('Tareas y cronograma')"
                      >
                        Ir a «Tareas y cronograma»
                      </button>
                    </div>
                  } @else {
                    <p class="mt-2 !text-xs font-semibold tracking-wide text-ink-subtle uppercase">
                      {{
                        editandoReqId()
                          ? 'Editar ' + codigoEditando()
                          : 'Nuevo requerimiento (formato PACC)'
                      }}
                    </p>
                    <label class="field-label"
                      >Tarea que necesita el apoyo
                      <select
                        class="field-control mt-1"
                        [ngModel]="nuevoReq.tareaId"
                        (ngModelChange)="seleccionarTareaReq($event)"
                      >
                        <option value="">— Seleccione la tarea —</option>
                        @for (t of tareasReq; track t.id) {
                          <option [value]="t.id">{{ t.nombre }}{{ mesesDeTarea(t) }}</option>
                        }
                      </select>
                    </label>
                    @if (!nuevoReq.tareaId) {
                      <p class="!text-xs text-ink-muted">
                        Elija la tarea para continuar: de ella se toma el trimestre en que se
                        necesita.
                      </p>
                    }
                  }
                }

                @if (editable() && nuevoReq.tareaId) {
                  <!-- El usuario busca el artículo por nombre; los códigos los deriva el sistema. -->
                  <app-master-product-picker
                    etiqueta="¿Qué necesita?"
                    [valorInicial]="articuloSeleccionado()"
                    (seleccion)="seleccionarArticulo($event)"
                  />

                  @if (articuloSeleccionado(); as art) {
                    <div class="grid grid-cols-2 gap-3">
                      <label class="field-label"
                        >Cantidad
                        <input
                          type="number"
                          min="1"
                          class="field-control mt-1 text-right"
                          [(ngModel)]="nuevoReq.cantidad"
                        />
                      </label>
                      <label class="field-label"
                        >Unidad de medida
                        <input
                          class="field-control mt-1"
                          [(ngModel)]="nuevoReq.unidadMedida"
                          placeholder="Unidades, galones, resmas…"
                        />
                      </label>
                      <label class="field-label"
                        >Trimestre
                        <select class="field-control mt-1" [(ngModel)]="nuevoReq.trimestre">
                          @for (t of trimestres; track t) {
                            <option [value]="t">{{ t }}</option>
                          }
                        </select>
                      </label>
                      <label class="field-label"
                        >Fuente de financiamiento
                        <select
                          class="field-control mt-1"
                          [(ngModel)]="nuevoReq.fuenteFinanciamiento"
                        >
                          @for (f of fuentes; track f) {
                            <option [value]="f">{{ f }}</option>
                          }
                        </select>
                      </label>
                    </div>

                    <label class="field-label"
                      >Justificación
                      <input
                        class="field-control mt-1"
                        [(ngModel)]="nuevoReq.descripcion"
                        [placeholder]="nombreTareaReq()"
                      />
                    </label>

                    @if (avisoTrimestre()) {
                      <p
                        class="rounded-lg bg-warning-bg px-3 py-1.5 !text-xs text-warning-text ring-1 ring-warning-border"
                      >
                        {{ avisoTrimestre() }}
                      </p>
                    }

                    @if (art.referencePrice) {
                      <p class="rounded-lg bg-surface-sunken px-3 py-2 !text-sm text-ink">
                        Monto de referencia:
                        <strong class="tabular-nums">{{
                          fmt(nuevoReq.cantidad * art.referencePrice)
                        }}</strong>
                        <span class="mt-0.5 block !text-xs text-ink-muted">
                          Precio del catálogo institucional. El área consolidadora lo confirma o lo
                          ajusta al revisar; hasta entonces el techo lo muestra como estimado.
                        </span>
                      </p>
                    } @else {
                      <p
                        class="rounded-lg bg-info-bg px-3 py-2 !text-sm text-info-text ring-1 ring-info-border"
                      >
                        Este artículo todavía no tiene precio de referencia institucional. El área
                        consolidadora lo estimará al revisar el requerimiento.
                      </p>
                    }

                    <p
                      class="rounded-lg bg-info-bg px-3 py-2 !text-xs text-info-text ring-1 ring-info-border"
                    >
                      Lo gestiona <strong>{{ art.consolidatorAreaName }}</strong
                      >. El requerimiento quedará enlazado a la tarea «{{ nombreTareaReq() }}» de
                      este producto.
                    </p>

                    @if (errorReq()) {
                      <p class="rounded-lg bg-danger-bg px-3 py-2 !text-sm text-danger-text">
                        {{ errorReq() }}
                      </p>
                    }

                    <div class="flex flex-wrap gap-2">
                      <button
                        type="button"
                        class="btn-accent btn-sm"
                        [disabled]="!puedeGuardarReq() || guardandoReq()"
                        (click)="guardarRequerimiento()"
                      >
                        {{
                          editandoReqId() ? 'Guardar cambios' : 'Crear requerimiento transversal'
                        }}
                      </button>
                      @if (editandoReqId()) {
                        <button
                          type="button"
                          class="btn-ghost btn-sm"
                          (click)="cancelarEdicionReq()"
                        >
                          Cancelar edición
                        </button>
                      }
                    </div>
                  }
                }

                @if (reqCreado()) {
                  <p
                    class="rounded-lg bg-success-bg px-3 py-2 !text-sm text-success-text ring-1 ring-success-border"
                  >
                    {{ reqCreado() }}
                  </p>
                }
              </div>
            }
            @case ('Historial') {
              <ul class="flex flex-col gap-3">
                @for (ev of historialFila(); track ev.id) {
                  <li class="flex gap-3">
                    <span class="mt-1.5 size-1.5 shrink-0 rounded-full bg-brand-300"></span>
                    <span>
                      <span class="block !text-sm text-ink"
                        >{{ ev.accion }} — {{ ev.persona }}</span
                      >
                      <span class="block !text-xs text-ink-muted">{{ ev.detalle }}</span>
                      <span class="block !text-[11px] text-ink-subtle">{{ ev.fecha }}</span>
                    </span>
                  </li>
                } @empty {
                  <li class="!text-sm text-ink-muted">
                    Sin eventos registrados para este producto.
                  </li>
                }
              </ul>
            }
          }
        }

        <div slot="footer" class="flex w-full items-center justify-between gap-2">
          @if (editable()) {
            <button type="button" class="btn-danger btn-sm" (click)="eliminarFila()">
              Eliminar producto
            </button>
            <div class="flex gap-2">
              <button type="button" class="btn-ghost" (click)="cerrarDrawer()">Cancelar</button>
              <button type="button" class="btn-secondary" (click)="guardar(true)">
                Guardar y siguiente
              </button>
              <button type="button" class="btn-primary" (click)="guardar(false)">
                Guardar cambios
              </button>
            </div>
          } @else {
            <span></span>
            <button type="button" class="btn-secondary" (click)="cerrarDrawer()">Cerrar</button>
          }
        </div>
      </app-planning-drawer>
    }
  `,
})
export class PlanWorkspaceComponent implements AfterViewInit {
  protected readonly store = inject(PoaStoreService);
  protected readonly reqStore = inject(RequirementStoreService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly taskOption = signal<Record<string, string>[]>([]);

  protected readonly fmt = formatRD;
  protected readonly monto = montoDe;
  protected readonly meses = MESES_CORTOS;
  protected readonly fuentes = FUENTES_FINANCIAMIENTO;
  protected readonly trimestres: Trimestre[] = ['T1', 'T2', 'T3', 'T4'];
  protected readonly tabsDrawer: TabDrawer[] = [
    'General',
    'Unidades y metas',
    'Tareas y cronograma',
    'Requerimientos',
    'Historial',
  ];
  protected readonly areasSeleccionables = computed(() =>
    this.store.areas.filter((a) => !a.esRaiz),
  );

  /** Catálogos para los dropdowns de selección múltiple (organigrama y territorio reales). */
  protected readonly opcionesAreas = computed<MultiOption[]>(() =>
    this.areasSeleccionables().map((a) => ({ value: a.id, label: a.nombre })),
  );
  protected readonly opcionesMunicipios = computed<MultiOption[]>(() =>
    this.store.municipios().map((m) => ({ value: m, label: m })),
  );

  private readonly planId = toSignal(this.route.paramMap.pipe(map((p) => p.get('planId') ?? '')), {
    initialValue: '',
  });

  constructor() {
    // Carga catálogos + expediente completo del plan (backend real, M5).
    void this.store.cargar().then(() => {
      const id = this.planId();
      if (id) void this.store.cargarWorkspace(id);
    });
  }
  protected readonly resaltarErrores = toSignal(
    this.route.queryParamMap.pipe(map((q) => q.get('errores') === '1')),
    { initialValue: false },
  );

  protected readonly planActual = computed(() => this.store.plan(this.planId()));

  protected readonly busqueda = signal('');
  protected readonly filtroFilas = signal<FiltroFilas>('Todas');
  protected readonly incidenciasAbiertas = signal(true);
  // Carga y descarga de la plantilla (Excel real vía backend, M6)
  protected readonly panelImportar = signal(false);
  protected readonly previsualizacion = signal<ImportResult | null>(null);
  protected readonly validandoArchivo = signal(false);
  protected readonly aplicandoImport = signal(false);
  protected readonly mensajeArchivo = signal('');
  protected archivoImport: File | null = null;
  protected modoImport: 'actualizar' | 'reemplazar' = 'actualizar';
  protected readonly devolviendo = signal(false);
  protected textoObservacion = '';

  // Drawer
  protected readonly filaAbierta = signal<ProductoPoa | null>(null);
  protected readonly borrador = signal<ProductoPoa | null>(null);
  protected readonly tabDrawer = signal<TabDrawer>('General');
  protected readonly autocompletado = signal(false);
  protected readonly reqCreado = signal('');
  protected readonly avisoTarea = signal('');
  /** Requerimiento en edición; vacío = el formulario da de alta uno nuevo. */
  protected readonly editandoReqId = signal('');
  /** Requerimiento con confirmación de borrado abierta. */
  protected readonly eliminandoReqId = signal('');
  protected nuevoReq = this.reqVacio();

  /** Artículo elegido en el buscador; de él salen unidad, precio y todos los códigos. */
  protected readonly articuloSeleccionado = signal<MasterProductSearchItemDto | null>(null);
  protected readonly guardandoReq = signal(false);
  protected readonly errorReq = signal('');

  private reqVacio() {
    return {
      tareaId: '',
      descripcion: '',
      cantidad: 1,
      unidadMedida: 'Unidad',
      trimestre: 'T1' as Trimestre,
      fuenteFinanciamiento: FUENTES_FINANCIAMIENTO[0],
    };
  }

  protected readonly editable = computed(() => {
    const plan = this.planActual();
    if (!plan) return false;
    const esPropio = plan.areaId === this.store.persona().areaId;
    const estadoEditable = ['Sin iniciar', 'Borrador', 'Observado'].includes(plan.estado);
    // El servidor tiene la última palabra (poa:edit + área propia, o admin); aquí solo se
    // decide qué UI mostrar: el área dueña o quien revisa (Planificación).
    return estadoEditable && this.store.puedeEditar() && (esPropio || this.store.puedeRevisar());
  });

  protected readonly puedeDevolver = computed(
    () => this.store.puedeRevisar() && this.planActual()?.estado === 'Listo para revisión',
  );

  protected readonly filasVisibles = computed(() => {
    const plan = this.planActual();
    if (!plan) return [];
    const q = this.busqueda().toLowerCase();
    return plan.rows.filter((r) => {
      const matchQ =
        !q || r.nombreProducto.toLowerCase().includes(q) || r.codigo.toLowerCase().includes(q);
      const f = this.filtroFilas();
      const tieneReqs = this.reqStore
        .requerimientos()
        .some((x) => x.poaProductId === r.id && x.status !== 'Merged');
      const matchF =
        f === 'Todas' ||
        (f === 'Incompletas' && this.store.rowIssues(r).length > 0) ||
        (f === 'Con apoyo transversal' && tieneReqs);
      return matchQ && matchF;
    });
  });

  protected readonly filasCompletas = computed(() => {
    const plan = this.planActual();
    if (!plan) return 0;
    return plan.rows.filter((r) => !this.store.rowIssues(r).length).length;
  });

  protected readonly progreso = computed(() => {
    const plan = this.planActual();
    if (!plan?.rows.length) return 0;
    return Math.round((this.filasCompletas() / plan.rows.length) * 100);
  });

  /** Monto comprometido del área = suma de sus requerimientos vigentes. */
  protected readonly montoRequerimientos = computed(() => {
    return this.reqStore.montoTotalSolicitado();
  });

  protected readonly incidencias = computed(() => {
    const plan = this.planActual();
    if (!plan) return [];
    return plan.rows
      .map((row) => ({ row, issues: this.store.rowIssues(row) }))
      .filter((x) => x.issues.length);
  });

  // Cascada de alineación en el drawer
  protected readonly objetivosDisponibles = computed(() => {
    const b = this.borrador();
    return this.store.foco(b?.focoId)?.objetivos ?? [];
  });

  protected readonly productosDisponibles = computed(() => {
    const b = this.borrador();
    return this.store.objetivo(b?.focoId, b?.objetivoId)?.productos ?? [];
  });

  protected readonly requerimientosDeFila = computed<RequirementDto[]>(() => {
    const fila = this.filaAbierta();
    if (!fila) return [];
    const taskIds = new Set((fila.tareas ?? []).map((t) => t.id));
    return this.reqStore
      .requerimientos()
      .filter((r) => taskIds.has(r.poaTaskId) && r.status !== 'Merged');
  });

  protected readonly historialFila = computed<
    { id: string; accion: string; persona: string; detalle: string; fecha: string }[]
  >(() => []);

  /**
   * El artículo trae la unidad sugerida; el precio y los códigos regulatorios no se
   * teclean nunca: el servidor los copia de la Maestra al crear el requerimiento.
   */
  protected seleccionarArticulo(articulo: MasterProductSearchItemDto | null): void {
    this.articuloSeleccionado.set(articulo);
    this.errorReq.set('');
    if (articulo?.suggestedUnit) {
      this.nuevoReq.unidadMedida = articulo.suggestedUnit;
    }
  }

  protected puedeGuardarReq(): boolean {
    return (
      !!this.articuloSeleccionado() &&
      !!this.nuevoReq.tareaId &&
      this.nuevoReq.cantidad > 0 &&
      !!this.nuevoReq.unidadMedida.trim()
    );
  }

  // --- Origen del apoyo: la tarea, no el producto ---------------------------
  /**
   * Tareas con nombre del producto abierto. Son métodos (no computed) porque el
   * nombre de la tarea se edita in-place con ngModel y debe releerse en cada
   * ciclo de detección de cambios.
   */
  protected tareasSolicitables(): TareaPoa[] {
    return (this.borrador()?.tareas ?? []).filter((t) => t.nombre.trim());
  }

  protected nombreTareaReq(): string {
    return this.tareasSolicitables().find((t) => t.id === this.nuevoReq.tareaId)?.nombre ?? '';
  }

  protected mesesDeTarea(t: TareaPoa): string {
    if (t.continua) return ' — ejecución continua todo el año';
    const tri = trimestreDeTarea(t);
    return tri ? ` — inicia en ${tri}` : ' — sin meses programados';
  }

  /** Al elegir la tarea se propone el trimestre desde su cronograma. */
  protected seleccionarTareaReq(tareaId: string): void {
    this.nuevoReq.tareaId = tareaId;
    const tarea = this.tareasSolicitables().find((t) => t.id === tareaId);
    const tri = tarea ? trimestreDeTarea(tarea) : null;
    if (tri) this.nuevoReq.trimestre = tri;
    this.reqCreado.set('');
  }

  /** Avisa cuando el trimestre pedido no coincide con el inicio de la tarea. */
  protected avisoTrimestre(): string {
    const tarea = this.tareasSolicitables().find((t) => t.id === this.nuevoReq.tareaId);
    if (!tarea) return '';
    if (tarea.continua) {
      return 'La tarea es continua (todo el año): elija a mano el trimestre en que se necesita el apoyo.';
    }
    const tri = trimestreDeTarea(tarea);
    if (!tri) {
      return 'La tarea no tiene meses programados: defina su cronograma para justificar el trimestre.';
    }
    return tri === this.nuevoReq.trimestre
      ? ''
      : `La tarea «${tarea.nombre}» inicia en ${tri}. Verifique el trimestre del requerimiento.`;
  }

  protected async taskByProductId(): Promise<Record<string, string>[]> {
    try {
      const tasks = await this.reqStore.cargarTareasPorProducto(this.filaAbierta()?.id ?? '');
      const options = tasks.map((t) => ({ value: t.id, label: t.name || 'Tarea sin nombre' }));
      this.taskOption.set(options);
      return this.taskOption();
    } catch {
      return [];
    }
  }

  protected etiquetaEstadoReq(estado: string): string {
    const mapa: Record<string, string> = {
      New: 'Nuevo',
      InReview: 'En revisión',
      Approved: 'Aprobado',
      Reduced: 'Reducido',
      Returned: 'Devuelto',
      Merged: 'Fusionado',
      Rescheduled: 'Reprogramado',
    };
    return mapa[estado] ?? estado;
  }

  protected badgeEstadoReq(estado: string): string {
    if (estado === 'Approved') return 'badge-success';
    if (estado === 'Returned') return 'badge-error';
    if (estado === 'Merged' || estado === 'Rescheduled') return 'badge-neutral';
    return 'badge-info';
  }

  /** Requerimientos del producto agrupados por la tarea que los originó. */
  protected reqsDeTarea(tareaId: string) {
    return this.reqStore.requerimientos().filter((r) => r.poaTaskId === tareaId);
  }

  protected requerimientosPorTarea(): {
    tareaId: string;
    nombre: string;
    items: RequirementDto[];
  }[] {
    const reqs = this.requerimientosDeFila();
    if (!reqs.length) return [];
    const tareas = this.borrador()?.tareas ?? [];
    const grupos = tareas
      .map((t) => ({
        tareaId: t.id,
        nombre: t.nombre || 'Tarea sin nombre',
        items: reqs.filter((r) => r.poaTaskId === t.id),
      }))
      .filter((g) => g.items.length);
    const huerfanos = reqs.filter((r) => !tareas.some((t) => t.id === r.poaTaskId));
    if (huerfanos.length) {
      grupos.push({ tareaId: '—', nombre: 'Sin tarea vigente', items: huerfanos });
    }
    return grupos;
  }

  ngAfterViewInit(): void {
    // Enfoca la fila indicada por query param (?fila=row-id) al llegar desde
    // el error summary de la pantalla de revisión.
    const fila = this.route.snapshot.queryParamMap.get('fila');
    if (fila) {
      setTimeout(() => this.enfocarFila(fila), 150);
    }
  }

  protected badgeEstado(estado: string): string {
    switch (estado) {
      case 'Aprobado':
        return 'badge-success';
      case 'Observado':
        return 'badge-error';
      case 'Listo para revisión':
        return 'badge-info';
      case 'Sin iniciar':
        return 'badge-neutral';
      default:
        return 'badge-warning';
    }
  }

  protected enfocarFila(rowId: string): void {
    document
      .getElementById('fila-' + rowId)
      ?.scrollIntoView({ behavior: 'smooth', block: 'center' });
  }

  protected async agregarFila(): Promise<void> {
    const row = await this.store.addRow(this.planId());
    if (row) this.abrirDrawer(row);
  }

  // --- Plantilla Excel: descarga y carga (backend real, M6) -----------------
  protected exportar(): void {
    const plan = this.planActual();
    if (!plan) return;
    void this.store.exportarPlan(plan.id);
    this.mensajeArchivo.set(
      'Se descargó el plan en Excel. Edítelo y vuelva a cargarlo desde «Importar Excel».',
    );
  }

  protected descargarPlantilla(): void {
    void this.store.descargarPlantilla(this.planId());
    this.mensajeArchivo.set(
      'Se descargó la plantilla vacía con las columnas del ciclo (incluidas las personalizadas).',
    );
  }

  protected async archivoSeleccionado(ev: Event): Promise<void> {
    const archivo = (ev.target as HTMLInputElement).files?.[0] ?? null;
    this.archivoImport = archivo;
    this.previsualizacion.set(null);
    if (!archivo) return;
    await this.validarArchivo();
  }

  protected setModoImport(modo: 'actualizar' | 'reemplazar'): void {
    this.modoImport = modo;
    // El resumen de la previsualización depende del modo: se revalida.
    if (this.archivoImport) void this.validarArchivo();
  }

  /** Validación previa en el servidor (dryRun): nada se aplica hasta confirmar. */
  private async validarArchivo(): Promise<void> {
    if (!this.archivoImport) return;
    this.validandoArchivo.set(true);
    try {
      this.previsualizacion.set(
        await this.store.importar(this.planId(), this.archivoImport, this.modoImport, true),
      );
    } finally {
      this.validandoArchivo.set(false);
    }
  }

  protected async aplicarImportacion(): Promise<void> {
    const previa = this.previsualizacion();
    if (!previa || previa.errors.length || !this.archivoImport) return;
    this.aplicandoImport.set(true);
    try {
      const resultado = await this.store.importar(
        this.planId(),
        this.archivoImport,
        this.modoImport,
        false,
      );
      if (!resultado.applied) {
        this.previsualizacion.set(resultado);
        return;
      }
      const s = resultado.summary;
      this.cerrarImportacion();
      this.mensajeArchivo.set(
        s
          ? `Carga aplicada: ${s.added} producto(s) agregado(s), ${s.updated} actualizado(s), ${s.unchanged} conservado(s).`
          : 'Carga aplicada.',
      );
    } finally {
      this.aplicandoImport.set(false);
    }
  }

  protected cerrarImportacion(): void {
    this.panelImportar.set(false);
    this.previsualizacion.set(null);
    this.archivoImport = null;
  }

  protected abrirDrawer(row: ProductoPoa): void {
    this.filaAbierta.set(row);
    this.borrador.set(structuredClone(row));
    this.tabDrawer.set('General');
    this.autocompletado.set(false);
    this.reqCreado.set('');
    this.avisoTarea.set('');
    this.editandoReqId.set('');
    this.eliminandoReqId.set('');
    this.nuevoReq = this.reqVacio();
  }

  protected cerrarDrawer(): void {
    this.filaAbierta.set(null);
    this.borrador.set(null);
  }

  // --- origen del producto (ajuste 2) --------------------------------------
  /** Alterna entre producto del PEI y actividad operativa no vinculada. */
  protected setOrigen(origen: OrigenProducto): void {
    const b = this.borrador();
    if (!b) return;
    b.origen = origen;
    if (origen === 'operativo') {
      b.focoId = null;
      b.objetivoId = null;
      b.productoEstrategicoId = null;
      this.autocompletado.set(false);
    } else {
      b.justificacionOperativa = '';
    }
    this.borrador.set({ ...b });
  }

  protected setJustificacion(texto: string): void {
    const b = this.borrador();
    if (!b) return;
    b.justificacionOperativa = texto;
    this.borrador.set({ ...b });
  }

  // --- alineación en cascada ----------------------------------------------
  protected setFoco(focoId: string | null): void {
    const b = this.borrador();
    if (!b) return;
    b.focoId = focoId;
    b.objetivoId = null;
    b.productoEstrategicoId = null;
    this.borrador.set({ ...b });
  }

  protected setObjetivo(objetivoId: string | null): void {
    const b = this.borrador();
    if (!b) return;
    b.objetivoId = objetivoId;
    b.productoEstrategicoId = null;
    this.borrador.set({ ...b });
  }

  /**
   * Spec col. 4 del POA: al seleccionar un producto estratégico, el sistema
   * completa unidad de medida, meta del año, responsable y área involucrada.
   */
  protected seleccionarProductoPei(productoId: string | null): void {
    const b = this.borrador();
    if (!b) return;
    b.productoEstrategicoId = productoId;
    if (productoId) {
      const info = this.store.productoEstrategico(productoId);
      if (info) {
        b.nombreProducto = info.producto.nombre;
        b.areasInvolucradasIds = [...info.producto.areasInvolucradasIds];

        const cicloPeriodo = this.store.cicloActivo().periodo;
        const yearMatch = cicloPeriodo.match(/\d{4}/);
        const anio = yearMatch ? Number(yearMatch[0]) : new Date().getFullYear();
        const claveAnio = `a${anio}`;

        b.unidades = info.producto.unidades.map((u) => {
          const valorAnio = u.metasAnuales[claveAnio as keyof typeof u.metasAnuales];
          const primerValorConDato = Object.values(u.metasAnuales).find(
            (v) => typeof v === 'number' && v > 0,
          );
          const metaValor = valorAnio && valorAnio > 0 ? valorAnio : (primerValorConDato ?? 0);
          return {
            id: `upoa-${Math.random().toString(36).slice(2, 8)}`,
            nombre: u.nombre,
            metaValor,
            metaTipo: u.nombre.toLowerCase().includes('porcentaje') ? 'porcentaje' : 'absoluto',
            mediosVerificacion: '',
            responsableAreaId: u.responsableAreaId,
            programacion: { T1: 0, T2: 0, T3: 0, T4: 0 },
          };
        });
        this.autocompletado.set(true);
      }
    }
    this.borrador.set({ ...b });
  }

  // --- colecciones del borrador --------------------------------------------
  protected setMunicipios(valores: string[]): void {
    const b = this.borrador();
    if (!b) return;
    b.municipiosImpacto = valores;
    this.borrador.set({ ...b });
  }

  protected setAreasInvolucradas(valores: string[]): void {
    const b = this.borrador();
    if (!b) return;
    b.areasInvolucradasIds = valores;
    this.borrador.set({ ...b });
  }

  protected agregarUnidad(): void {
    const b = this.borrador();
    if (!b) return;
    b.unidades = [
      ...b.unidades,
      {
        id: `upoa-${Math.random().toString(36).slice(2, 8)}`,
        nombre: '',
        metaValor: 0,
        metaTipo: 'absoluto',
        mediosVerificacion: '',
        responsableAreaId: this.planActual()?.areaId ?? '',
        programacion: { T1: 0, T2: 0, T3: 0, T4: 0 },
      },
    ];
    this.borrador.set({ ...b });
  }

  protected eliminarUnidad(i: number): void {
    const b = this.borrador();
    if (!b) return;
    b.unidades = b.unidades.filter((_, idx) => idx !== i);
    this.borrador.set({ ...b });
  }

  protected avisoProgramacion(u: ProductoPoa['unidades'][number]): string {
    if (!u.metaValor) return '';
    const suma = u.programacion.T1 + u.programacion.T2 + u.programacion.T3 + u.programacion.T4;
    if (u.metaTipo === 'absoluto' && suma !== u.metaValor) {
      return `La suma de los trimestres (${suma}) no coincide con la meta anual (${u.metaValor}).`;
    }
    if (u.metaTipo === 'porcentaje' && u.programacion.T4 !== u.metaValor) {
      return `En medición porcentual, el T4 debe alcanzar la meta anual (${u.metaValor}%).`;
    }
    return '';
  }

  protected agregarTarea(): void {
    const b = this.borrador();
    if (!b) return;
    b.tareas = [
      ...b.tareas,
      {
        id: `ta-${Math.random().toString(36).slice(2, 8)}`,
        nombre: '',
        meses: [],
        continua: false,
      },
    ];
    this.borrador.set({ ...b });
  }

  /** Ajuste 4: alterna una tarea entre cronograma mensual y ejecución continua. */
  protected toggleContinua(tareaId: string): void {
    const b = this.borrador();
    if (!b) return;
    b.tareas = b.tareas.map((t) =>
      t.id !== tareaId ? t : { ...t, continua: !t.continua, meses: !t.continua ? [] : t.meses },
    );
    this.borrador.set({ ...b });
  }

  /**
   * No se elimina una tarea con requerimientos vivos: dejaría el apoyo sin
   * origen y sin fecha que lo justifique.
   */
  protected eliminarTarea(i: number): void {
    const b = this.borrador();
    if (!b) return;
    const tarea = b.tareas[i];
    const reqs = this.reqStore.requerimientos().filter((r) => r.poaTaskId === tarea.id);
    if (reqs.length) {
      this.avisoTarea.set(
        `No se puede eliminar «${tarea.nombre || 'tarea sin nombre'}»: tiene ${reqs.length} requerimiento(s) de apoyo (${reqs
          .map((r) => r.code)
          .join(', ')}). Cancélelos o reasígnelos antes de eliminarla.`,
      );
      return;
    }
    this.avisoTarea.set('');
    b.tareas = b.tareas.filter((_, idx) => idx !== i);
    this.borrador.set({ ...b });
  }

  protected toggleMesTarea(tareaId: string, mes: number): void {
    const b = this.borrador();
    if (!b) return;
    b.tareas = b.tareas.map((t) =>
      t.id !== tareaId
        ? t
        : {
            ...t,
            meses: t.meses.includes(mes)
              ? t.meses.filter((m) => m !== mes)
              : [...t.meses, mes].sort((a, z) => a - z),
          },
    );
    this.borrador.set({ ...b });
  }

  // --- persistencia ---------------------------------------------------------
  protected async guardar(siguiente: boolean): Promise<void> {
    const b = this.borrador();
    const fila = this.filaAbierta();
    if (!b || !fila) return;
    const { id, ...resto } = b;
    const filaFresca = await this.store.updateRow(this.planId(), id, structuredClone(resto));
    if (filaFresca) {
      this.filaAbierta.set(filaFresca);
      this.borrador.set(structuredClone(filaFresca));
    }
    if (siguiente) {
      const tabActualIndex = this.tabsDrawer.indexOf(this.tabDrawer());
      if (tabActualIndex >= 0 && tabActualIndex < this.tabsDrawer.length - 1) {
        this.tabDrawer.set(this.tabsDrawer[tabActualIndex + 1]);
        return;
      }
    }
    this.cerrarDrawer();
  }

  protected eliminarFila(): void {
    const fila = this.filaAbierta();
    if (!fila) return;
    this.store.deleteRow(this.planId(), fila.id);
    this.cerrarDrawer();
  }

  /** El backend habla Q1..Q4; la UI del POA, T1..T4. */
  private aQuarter(t: Trimestre): string {
    return t.replace('T', 'Q');
  }

  private aTrimestre(q: string): Trimestre {
    const t = q.replace('Q', 'T');
    return (['T1', 'T2', 'T3', 'T4'].includes(t) ? t : 'T1') as Trimestre;
  }

  /** Carga un requerimiento existente en el formulario de la pestaña. */
  protected editarRequerimiento(req: RequirementDto): void {
    this.editandoReqId.set(req.id);
    this.eliminandoReqId.set('');
    this.reqCreado.set('');
    this.errorReq.set('');
    this.nuevoReq = {
      tareaId: req.poaTaskId,
      descripcion: req.description,
      cantidad: req.quantity,
      unidadMedida: req.unitOfMeasure,
      trimestre: this.aTrimestre(req.quarter),
      fuenteFinanciamiento: req.fundingSource ?? FUENTES_FINANCIAMIENTO[0],
    };
    // Se reconstruye el artículo desde los snapshots del requerimiento: basta para
    // mostrarlo y para reenviarlo, sin volver a pegarle al buscador.
    this.articuloSeleccionado.set({
      id: req.masterProductId,
      code: req.masterProductCode ?? '',
      name: req.masterProductName,
      itemType: req.itemType,
      unspscCode: req.unspscCode,
      unspscName: req.unspscName,
      expenseObjectCode: req.expenseObjectCode,
      accountingAccountCode: req.accountingAccountCode,
      consolidatorAreaId: req.consolidatorAreaId,
      consolidatorAreaName: req.consolidatorAreaName,
      suggestedUnit: req.unitOfMeasure,
      referencePrice: req.estimatedUnitPrice || null,
    });
  }

  protected codigoEditando(): string {
    return this.reqStore.requerimientos().find((r) => r.id === this.editandoReqId())?.code ?? '';
  }

  protected cancelarEdicionReq(): void {
    this.editandoReqId.set('');
    this.nuevoReq = this.reqVacio();
    this.articuloSeleccionado.set(null);
    this.errorReq.set('');
  }

  protected async eliminarRequerimiento(req: RequirementDto): Promise<void> {
    await this.reqStore.eliminarRequerimiento(req.id);
    this.eliminandoReqId.set('');
    if (this.editandoReqId() === req.id) this.cancelarEdicionReq();
    this.reqCreado.set(`${req.code} eliminado.`);
  }

  /** Alta o edición según el estado del formulario. */
  protected async guardarRequerimiento(): Promise<void> {
    const fila = this.filaAbierta();
    const b = this.borrador();
    const articulo = this.articuloSeleccionado();
    if (!fila || !b || !articulo || !this.nuevoReq.tareaId) return;

    const tareaSeleccionada = this.nuevoReq.tareaId;
    const nombreTarea = this.nombreTareaReq();
    const { id, ...resto } = b;

    this.guardandoReq.set(true);
    this.errorReq.set('');
    try {
      // El borrador puede traer una tarea recién agregada con id local (no GUID):
      // se guarda primero el producto para que el backend le asigne su id real.
      const filaFresca = await this.store.updateRow(this.planId(), id, structuredClone(resto));
      if (filaFresca) {
        this.filaAbierta.set(filaFresca);
        this.borrador.set(structuredClone(filaFresca));
      }
      const tareaId =
        filaFresca?.tareas.find((t) => t.id === tareaSeleccionada || t.nombre === nombreTarea)
          ?.id ?? tareaSeleccionada;
      this.nuevoReq.tareaId = tareaId;

      // El precio no se envía: el servidor toma el de referencia del artículo salvo que
      // la consolidadora ya lo haya confirmado.
      const comun = {
        description: this.nuevoReq.descripcion.trim() || nombreTarea,
        quantity: this.nuevoReq.cantidad,
        unitOfMeasure: this.nuevoReq.unidadMedida.trim(),
        quarter: this.aQuarter(this.nuevoReq.trimestre),
        fundingSource: this.nuevoReq.fuenteFinanciamiento,
        priority: 'Medium',
      };

      if (this.editandoReqId()) {
        await this.reqStore.actualizarRequerimiento(this.editandoReqId(), {
          masterProductId: articulo.id,
          estimatedUnitPrice: articulo.referencePrice ?? 0,
          ...comun,
        });
        this.cancelarEdicionReq();
        this.reqCreado.set('Requerimiento actualizado.');
        return;
      }

      const created = await this.reqStore.crearRequerimiento({
        poaTaskId: tareaId,
        masterProductId: articulo.id,
        ...comun,
      });

      this.nuevoReq = this.reqVacio();
      this.articuloSeleccionado.set(null);
      this.seleccionarTareaReq(tareaId);
      this.reqCreado.set(
        `${created.code} creado desde la tarea «${nombreTarea}» y enrutado a ${created.consolidatorAreaName}.`,
      );
    } catch (err: any) {
      const serverMsg =
        err?.error?.detail ||
        err?.error?.title ||
        (err?.error?.errors ? Object.values(err.error.errors).flat().join(' ') : null);
      this.errorReq.set(
        serverMsg ||
          'No se pudo guardar el requerimiento. Verifique que el artículo siga habilitado y que la tarea pertenezca a su área.',
      );
    } finally {
      this.guardandoReq.set(false);
    }
  }

  protected devolver(): void {
    this.store.devolverPlan(this.planId(), this.textoObservacion.trim());
    this.textoObservacion = '';
    this.devolviendo.set(false);
  }
}
