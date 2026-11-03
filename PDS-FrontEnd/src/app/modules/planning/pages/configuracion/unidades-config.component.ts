import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PageShellComponent } from '@public-intelligence/ui-core';
import { PlanningConfigService, PlanningAreaConfigDto } from '../../../../services/planning/planning-config.service';
import { NotificationService } from '../../../../services/notification/notification.service';

@Component({
  selector: 'app-config-unidades',
  imports: [PageShellComponent, FormsModule],
  template: `
    <app-page-shell
      title="Unidades participantes en PEI y POA"
      description="Define hasta qué nivel jerárquico del organigrama participan las unidades organizacionales en la formulación del PEI y POA"
    >
      @if (configService.cargando()) {
        <div class="card p-8 text-center text-ink-muted">Cargando reglas y guardrails de unidades…</div>
      } @else {
        @let cfg = configService.config();
        <div class="flex flex-col gap-6">
          
          <!-- Tarjeta de configuración del nivel -->
          <div class="card p-6">
            <h2 class="!font-poppins text-lg font-semibold text-ink">Profundidad de unidades participantes</h2>
            <p class="mt-1 text-sm text-ink-muted">
              Por norma institucional, el PEI y el POA son formulados habitualmente por el <strong>Despacho</strong>, 
              <strong>Dependencias de despacho</strong> y <strong>Direcciones</strong> (hasta el 2.º nivel desde la máxima autoridad).
            </p>

            <div class="mt-5 grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
              @for (opc of opcionesNivel; track opc.val) {
                <div
                  class="relative flex cursor-pointer flex-col rounded-xl border p-4 transition-all"
                  [class]="
                    selectedLevel() === opc.val
                      ? 'border-brand bg-brand-50/50 ring-2 ring-brand'
                      : 'border-border bg-surface hover:border-brand-300'
                  "
                  (click)="seleccionarNivel(opc.val)"
                >
                  <div class="flex items-center justify-between">
                    <span class="font-bold text-ink">{{ opc.titulo }}</span>
                    @if (opc.recomendado) {
                      <span class="rounded-full bg-brand-100 px-2 py-0.5 text-[10px] font-bold text-brand uppercase">Recomendado</span>
                    }
                  </div>
                  <p class="mt-2 text-xs text-ink-muted leading-relaxed">{{ opc.descripcion }}</p>
                </div>
              }
            </div>

            <div class="mt-6 flex items-center justify-between border-t border-border pt-4">
              <span class="text-xs font-semibold text-ink-subtle uppercase">
                Configuración seleccionada: {{ obtenerTextoNivel(selectedLevel()) }}
              </span>
              <button
                type="button"
                class="btn-primary flex items-center gap-2"
                [disabled]="configService.guardando()"
                (click)="guardarConfiguracion()"
              >
                @if (configService.guardando()) {
                  <span class="inline-block h-4 w-4 animate-spin rounded-full border-2 border-white border-t-transparent"></span>
                }
                Guardar cambios
              </button>
            </div>
          </div>

          <!-- Vista previa de unidades y su estado -->
          @if (cfg) {
            <div class="card p-6">
              <div class="flex flex-wrap items-center justify-between gap-3 border-b border-border pb-3">
                <div>
                  <h3 class="!font-poppins text-base font-semibold text-ink">Vista previa de unidades en el catálogo</h3>
                  <p class="mt-0.5 text-xs text-ink-muted">
                    Total: {{ cfg.qualifyingAreas.length }} unidades · Habilitadas: {{ contarHabilitadas() }}
                  </p>
                </div>
                <input
                  type="search"
                  placeholder="Buscar unidad…"
                  class="field-control !h-9 !w-64 text-xs"
                  [(ngModel)]="filtroBusqueda"
                />
              </div>

              <div class="mt-4 overflow-x-auto">
                <table class="w-full text-left text-sm">
                  <thead>
                    <tr class="border-b border-border text-xs font-semibold text-ink-subtle uppercase">
                      <th class="py-2.5 px-3">Código</th>
                      <th class="py-2.5 px-3">Unidad Organizacional</th>
                      <th class="py-2.5 px-3">Unidad Padre</th>
                      <th class="py-2.5 px-3">Nivel Jerárquico</th>
                      <th class="py-2.5 px-3 text-center">¿Participa en PEI / POA?</th>
                    </tr>
                  </thead>
                  <tbody class="divide-y divide-border">
                    @for (area of areasFiltradas(cfg.qualifyingAreas); track area.id) {
                      <tr class="hover:bg-surface-sunken/50 transition-colors">
                        <td class="py-2.5 px-3 font-mono text-xs font-semibold text-ink-muted">{{ area.code }}</td>
                        <td class="py-2.5 px-3 font-medium text-ink">{{ area.name }}</td>
                        <td class="py-2.5 px-3 text-xs text-ink-muted">{{ area.parentName || '— (Máxima Autoridad)' }}</td>
                        <td class="py-2.5 px-3">
                          <span class="rounded bg-surface-sunken px-2 py-0.5 text-xs font-medium text-ink-muted">
                            Nivel {{ area.depthLevel }}
                          </span>
                        </td>
                        <td class="py-2.5 px-3 text-center">
                          <label class="inline-flex items-center gap-2 cursor-pointer select-none">
                            <input
                              type="checkbox"
                              class="h-4 w-4 rounded border-border text-brand focus:ring-brand accent-brand cursor-pointer"
                              [checked]="isAreaSelected(area.id)"
                              (change)="toggleAreaSelection(area.id, $event)"
                            />
                            <span
                              class="text-xs font-semibold px-2 py-0.5 rounded-full"
                              [class]="isAreaSelected(area.id) ? 'bg-success-bg text-success-text ring-1 ring-success-border' : 'bg-surface-sunken text-ink-subtle ring-1 ring-border'"
                            >
                              {{ isAreaSelected(area.id) ? 'Participa' : 'Excluida' }}
                            </span>
                          </label>
                        </td>
                      </tr>
                    }
                  </tbody>
                </table>
              </div>
            </div>
          }
        </div>
      }
    </app-page-shell>
  `,
})
export class UnidadesConfigComponent implements OnInit {
  protected readonly configService = inject(PlanningConfigService);
  protected readonly notification = inject(NotificationService);

  protected selectedLevel = signal<number>(2);
  protected selectedAreaIds = signal<Set<string>>(new Set());
  protected filtroBusqueda = '';

  protected readonly opcionesNivel = [
    {
      val: 0,
      titulo: 'Nivel 0 (Solo Despacho)',
      descripcion: 'Únicamente el Despacho o Máxima Autoridad.',
      recomendado: false,
    },
    {
      val: 1,
      titulo: 'Nivel 1 (Hasta dependencias de despacho)',
      descripcion: 'Despacho, Viceministerios y dependencias de despacho.',
      recomendado: false,
    },
    {
      val: 2,
      titulo: 'Nivel 2 (Hasta Direcciones)',
      descripcion: 'Despacho, Viceministerios y Direcciones. Estándar oficial institucional.',
      recomendado: true,
    },
    {
      val: 3,
      titulo: 'Nivel 3 (Hasta Departamentos)',
      descripcion: 'Incluye Departamentos por debajo de las Direcciones.',
      recomendado: false,
    },
    {
      val: 99,
      titulo: 'Sin Restricción (Todas)',
      descripcion: 'Habilita la totalidad de unidades del organigrama (incluyendo divisiones y secciones).',
      recomendado: false,
    },
    {
      val: -1,
      titulo: 'Selección Personalizada',
      descripcion: 'Permite seleccionar o desmarcar unidades organizacionales de forma individual según requerimiento.',
      recomendado: false,
    },
  ];

  async ngOnInit(): Promise<void> {
    const res = await this.configService.cargarConfiguracion();
    if (res) {
      this.selectedLevel.set(res.maxLevel);
      const initialIds = new Set<string>();
      res.qualifyingAreas.forEach((area) => {
        if (area.qualifiesForPlanning) {
          initialIds.add(area.id);
        }
      });
      this.selectedAreaIds.set(initialIds);
    }
  }

  protected isAreaSelected(areaId: string): boolean {
    return this.selectedAreaIds().has(areaId);
  }

  protected toggleAreaSelection(areaId: string, event: Event): void {
    const target = event.target as HTMLInputElement;
    const isChecked = target ? target.checked : false;

    const newSet = new Set(this.selectedAreaIds());
    if (isChecked) {
      newSet.add(areaId);
    } else {
      newSet.delete(areaId);
    }
    this.selectedAreaIds.set(newSet);
    this.evaluarNivelSeleccionado();
  }

  protected seleccionarNivel(val: number): void {
    const cfg = this.configService.config();
    if (!cfg) return;

    if (val === -1) {
      this.selectedLevel.set(-1);
      return;
    }

    const newSet = new Set<string>();
    cfg.qualifyingAreas.forEach((area) => {
      if (val === 99 || area.depthLevel <= val) {
        newSet.add(area.id);
      }
    });

    this.selectedAreaIds.set(newSet);
    this.selectedLevel.set(val);
  }

  protected evaluarNivelSeleccionado(): void {
    const cfg = this.configService.config();
    if (!cfg) return;

    const currentSet = this.selectedAreaIds();
    const allAreas = cfg.qualifyingAreas;

    const standardLevels = [0, 1, 2, 3, 99];
    for (const level of standardLevels) {
      const matchesLevel = allAreas.every((area) => {
        const shouldBeIncluded = level === 99 ? true : area.depthLevel <= level;
        return currentSet.has(area.id) === shouldBeIncluded;
      });

      if (matchesLevel) {
        this.selectedLevel.set(level);
        return;
      }
    }

    this.selectedLevel.set(-1);
  }

  protected obtenerTextoNivel(val: number): string {
    const opc = this.opcionesNivel.find((o) => o.val === val);
    return opc ? opc.titulo : `Nivel ${val}`;
  }

  protected contarHabilitadas(): number {
    return this.selectedAreaIds().size;
  }

  protected areasFiltradas(items: PlanningAreaConfigDto['qualifyingAreas']) {
    if (!this.filtroBusqueda.trim()) return items;
    const term = this.filtroBusqueda.toLowerCase();
    return items.filter(
      (a) =>
        a.name.toLowerCase().includes(term) ||
        a.code.toLowerCase().includes(term) ||
        (a.parentName && a.parentName.toLowerCase().includes(term))
    );
  }

  protected async guardarConfiguracion(): Promise<void> {
    const level = this.selectedLevel();
    const customIds = level === -1 ? Array.from(this.selectedAreaIds()) : undefined;
    const res = await this.configService.actualizarGuardrailNivel(level, customIds);
    if (res) {
      this.notification.success('Regla de guardrail actualizada correctamente.');
    } else {
      this.notification.error('No se pudo guardar la configuración.');
    }
  }
}
