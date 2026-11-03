import { Component, inject, input, output, signal } from '@angular/core';
import { PoaStoreService } from '../../../services/planning/poa-store.service';
import { ProductoPoa } from '../data/planning.models';
import { MESES_CORTOS } from '../data/planning.mock-data';

/**
 * Grid del plan anual con las 13 columnas del formato oficial del POA:
 * alineación estratégica (3 niveles), producto, unidades de medida, meta,
 * municipios de impacto, medios de verificación, tareas, responsable (área),
 * áreas involucradas, cronograma mensual y programación trimestral.
 * Sin columna de montos: los montos viven en los requerimientos (PACC).
 * Columnas de identificación fijadas a la izquierda; el detalle se edita en
 * el drawer (doble clic en Producto permite renombrar inline).
 */
@Component({
  selector: 'app-plan-grid',
  template: `
    <div class="custom-scrollbar h-full overflow-auto">
      <table class="w-full min-w-[2100px] border-separate border-spacing-0 text-sm">
        <thead>
          <tr>
            <th [class]="th + ' sticky left-0 z-30 w-20 min-w-20'">Código</th>
            <th [class]="th + ' sticky left-20 z-30 w-64 min-w-64'">Producto</th>
            <th [class]="th + ' sticky z-30 w-28 min-w-28 left-[21rem]'">Estado</th>
            <th [class]="th">Foco estratégico</th>
            <th [class]="th">Objetivo estratégico</th>
            <th [class]="th">Unidad de medida</th>
            <th [class]="th">Meta</th>
            <th [class]="th">Municipios de impacto</th>
            <th [class]="th">Medios de verificación</th>
            <th [class]="th">Tareas</th>
            <th [class]="th">Responsable (área)</th>
            <th [class]="th">Áreas involucradas</th>
            <th [class]="th">Cronograma</th>
            <th [class]="th">Programación de metas</th>
            @for (col of store.columnasPersonalizadas(); track col.id) {
              <th [class]="th">
                {{ col.nombreVisible }}
                <span class="ml-1 rounded bg-accent-100 px-1 text-[9px] font-bold text-accent-700 uppercase"
                  title="Columna agregada por configuración">config</span>
              </th>
            }
            <th [class]="th"></th>
          </tr>
        </thead>
        <tbody>
          @for (row of rows(); track row.id) {
            @let issues = store.rowIssues(row);
            <tr [id]="'fila-' + row.id" class="group" [class.bg-error-bg]="resaltarErrores() && issues.length">
              <td [class]="tdSticky + ' left-0 w-20 min-w-20 font-medium'">
                <span class="flex items-center gap-1">
                  @if (issues.length) {
                    <span class="size-1.5 shrink-0 rounded-full bg-error" [title]="issues.join(' · ')"></span>
                  }
                  {{ row.codigo }}
                </span>
              </td>
              <td
                [class]="tdSticky + ' left-20 w-64 min-w-64' + (editable() ? ' cursor-cell' : '')"
                (dblclick)="editable() && iniciarEdicionNombre(row)"
                [title]="editable() ? 'Doble clic para renombrar · clic para abrir el detalle' : ''"
              >
                @if (editandoNombre() === row.id) {
                  <input
                    class="!h-8 w-full !rounded !px-1.5 !text-sm"
                    [value]="row.nombreProducto"
                    (blur)="confirmarNombre(row, $event)"
                    (keydown.enter)="confirmarNombre(row, $event)"
                    (keydown.escape)="editandoNombre.set(null)"
                    data-cell-editor
                  />
                } @else {
                  <button
                    type="button"
                    class="!m-0 w-full truncate !rounded-none !bg-transparent !p-0 text-left !text-sm !font-normal !normal-case !text-brand hover:underline"
                    [title]="row.nombreProducto || '(sin producto)'"
                    (click)="abrirFila.emit(row)"
                  >
                    {{ row.nombreProducto || '(sin producto)' }}
                  </button>
                  @if (row.origen === 'operativo') {
                    <span class="badge-warning mt-0.5 !text-[10px]">Operativo</span>
                  } @else {
                    <span class="badge-info mt-0.5 !text-[10px]">Del PEI</span>
                  }
                }
              </td>
              <td [class]="tdSticky + ' w-28 min-w-28 left-[21rem]'">
                @if (issues.length) {
                  <span class="badge-error" [title]="issues.join(' · ')">Incompleto</span>
                } @else {
                  <span class="badge-success">Completo</span>
                }
              </td>
              <td [class]="td + ' max-w-52'">
                <span class="block truncate" [title]="store.foco(row.focoId)?.nombre">
                  {{ store.foco(row.focoId)?.nombre ?? '—' }}
                </span>
              </td>
              <td [class]="td + ' max-w-52'">
                @let obj = store.objetivo(row.focoId, row.objetivoId);
                @if (obj) {
                  <span class="badge-info" [title]="obj.nombre">{{ obj.codigo }}</span>
                } @else {
                  <span class="badge-error">Sin alinear</span>
                }
              </td>
              <td [class]="td + ' max-w-56'">
                @for (u of row.unidades; track u.id) {
                  <span class="block truncate" [title]="u.nombre">{{ u.nombre || '(sin nombre)' }}</span>
                } @empty {
                  <span class="text-error-text">Sin unidades</span>
                }
              </td>
              <td [class]="td + ' text-right'">
                @for (u of row.unidades; track u.id) {
                  <span class="block tabular-nums" [class.text-error-text]="!u.metaValor">
                    {{ u.metaValor || '—' }}{{ u.metaTipo === 'porcentaje' ? '%' : '' }}
                  </span>
                }
              </td>
              <td [class]="td + ' max-w-48'">
                <span class="flex flex-wrap gap-1">
                  @for (m of row.municipiosImpacto.slice(0, 2); track m) {
                    <span class="badge-neutral">{{ m }}</span>
                  }
                  @if (row.municipiosImpacto.length > 2) {
                    <span class="badge-neutral" [title]="row.municipiosImpacto.join(', ')">+{{ row.municipiosImpacto.length - 2 }}</span>
                  }
                  @if (!row.municipiosImpacto.length) {
                    <span class="text-error-text">Pendiente</span>
                  }
                </span>
              </td>
              <td [class]="td + ' max-w-52'">
                @for (u of row.unidades; track u.id) {
                  <span class="block truncate" [title]="u.mediosVerificacion" [class.text-error-text]="!u.mediosVerificacion">
                    {{ u.mediosVerificacion || 'Pendiente' }}
                  </span>
                }
              </td>
              <td [class]="td + ' max-w-48'">
                @if (row.tareas.length) {
                  <span class="block truncate" [title]="nombresTareas(row)">
                    {{ row.tareas.length }} tarea(s) · {{ row.tareas[0].nombre }}
                  </span>
                } @else {
                  <span class="text-ink-subtle">—</span>
                }
              </td>
              <td [class]="td">
                @for (u of row.unidades; track u.id) {
                  <span class="block" [class.text-error-text]="!u.responsableAreaId">
                    {{ store.area(u.responsableAreaId)?.corta ?? 'Sin responsable' }}
                  </span>
                }
              </td>
              <td [class]="td + ' max-w-44'">
                <span class="flex flex-wrap gap-1">
                  @for (aid of row.areasInvolucradasIds; track aid) {
                    <span class="badge-neutral">{{ store.area(aid)?.corta }}</span>
                  } @empty {
                    <span class="text-ink-subtle">—</span>
                  }
                </span>
              </td>
              <td [class]="td">
                <span class="flex gap-0.5" [title]="'Meses con tareas programadas'">
                  @for (mes of meses; track $index; let mi = $index) {
                    <span class="flex flex-col items-center">
                      <span class="text-[8px] leading-none text-ink-subtle">{{ mes }}</span>
                      <span
                        class="mt-0.5 size-1.5 rounded-full"
                        [class]="mesActivo(row, mi) ? 'bg-brand' : 'bg-surface-sunken'"
                      ></span>
                    </span>
                  }
                </span>
              </td>
              <td [class]="td">
                @for (u of row.unidades; track u.id) {
                  <span class="block tabular-nums whitespace-nowrap">
                    {{ u.programacion.T1 }} / {{ u.programacion.T2 }} / {{ u.programacion.T3 }} / {{ u.programacion.T4 }}
                  </span>
                }
              </td>
              @for (col of store.columnasPersonalizadas(); track col.id) {
                <td
                  [class]="td + (editable() ? ' cursor-cell' : '')"
                  (dblclick)="editable() && iniciarEdicionCustom(row, col.nombreTecnico)"
                >
                  @if (esEditandoCustom(row.id, col.nombreTecnico)) {
                    @if (col.tipo === 'seleccion' && col.opciones?.length) {
                      <select
                        class="!h-8 w-full min-w-32 !rounded !px-1.5 !text-sm"
                        [value]="row.custom[col.nombreTecnico] ?? ''"
                        (change)="confirmarCustom(row, $event)"
                        (blur)="cancelarCustom()"
                        data-cell-editor
                      >
                        <option value="">—</option>
                        @for (op of col.opciones; track op) {
                          <option [value]="op">{{ op }}</option>
                        }
                      </select>
                    } @else {
                      <input
                        class="!h-8 w-full min-w-28 !rounded !px-1.5 !text-sm"
                        [value]="row.custom[col.nombreTecnico] ?? ''"
                        (blur)="confirmarCustom(row, $event)"
                        (keydown.enter)="confirmarCustom(row, $event)"
                        (keydown.escape)="cancelarCustom()"
                        data-cell-editor
                      />
                    }
                  } @else {
                    <span [class.text-error-text]="col.obligatoria && !row.custom[col.nombreTecnico]">
                      {{ row.custom[col.nombreTecnico] || (col.obligatoria ? 'Pendiente' : '—') }}
                    </span>
                  }
                </td>
              }
              <td [class]="td + ' text-right'">
                <button type="button" class="btn-ghost btn-sm opacity-0 group-hover:opacity-100" (click)="abrirFila.emit(row)">
                  Abrir
                </button>
              </td>
            </tr>
          } @empty {
            <tr>
              <td colspan="20" class="px-4 py-10 text-center text-sm text-ink-muted">
                Este plan aún no tiene productos. Use «Agregar producto» o importe desde Excel.
              </td>
            </tr>
          }
        </tbody>
      </table>
    </div>
  `,
})
export class PlanGridComponent {
  protected readonly store = inject(PoaStoreService);

  readonly planId = input.required<string>();
  readonly rows = input.required<ProductoPoa[]>();
  readonly editable = input(false);
  /** Cuando viene de "Revisar", resalta en rojo las filas con errores. */
  readonly resaltarErrores = input(false);

  readonly abrirFila = output<ProductoPoa>();

  protected readonly meses = MESES_CORTOS;
  protected readonly editandoNombre = signal<string | null>(null);
  protected readonly editandoCustom = signal<{ rowId: string; campo: string } | null>(null);

  // Clases compartidas (evita repetición en el template)
  protected readonly th =
    'border-b border-border bg-surface-sunken px-3 py-2 text-left text-xs font-semibold whitespace-nowrap text-ink-muted uppercase tracking-wide';
  protected readonly td = 'border-b border-border bg-surface-raised px-3 py-2 align-top text-ink';
  protected readonly tdSticky =
    'sticky z-20 border-b border-border bg-surface-raised px-3 py-2 align-top text-ink group-hover:bg-surface';

  protected mesActivo(row: ProductoPoa, mes: number): boolean {
    return row.tareas.some((t) => t.meses.includes(mes));
  }

  protected nombresTareas(row: ProductoPoa): string {
    return row.tareas.map((t) => t.nombre).join(' · ');
  }

  protected iniciarEdicionNombre(row: ProductoPoa): void {
    this.editandoNombre.set(row.id);
    setTimeout(() => document.querySelector<HTMLElement>('[data-cell-editor]')?.focus());
  }

  protected confirmarNombre(row: ProductoPoa, ev: Event): void {
    if (this.editandoNombre() !== row.id) return;
    const valor = (ev.target as HTMLInputElement).value;
    void this.store.updateRow(this.planId(), row.id, { nombreProducto: valor });
    this.editandoNombre.set(null);
  }

  protected esEditandoCustom(rowId: string, campo: string): boolean {
    const e = this.editandoCustom();
    return !!e && e.rowId === rowId && e.campo === campo;
  }

  protected iniciarEdicionCustom(row: ProductoPoa, campo: string): void {
    this.editandoCustom.set({ rowId: row.id, campo });
    setTimeout(() => document.querySelector<HTMLElement>('[data-cell-editor]')?.focus());
  }

  protected cancelarCustom(): void {
    this.editandoCustom.set(null);
  }

  protected confirmarCustom(row: ProductoPoa, ev: Event): void {
    const e = this.editandoCustom();
    if (!e) return;
    const valor = (ev.target as HTMLInputElement | HTMLSelectElement).value;
    this.store.updateRowCustom(this.planId(), row.id, e.campo, valor);
    this.editandoCustom.set(null);
  }
}
