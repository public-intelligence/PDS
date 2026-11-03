import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PageShellComponent } from '@public-intelligence/ui-core';
import { PlanningTemplateService } from '../../../../services/planning/planning-template.service';
import {
  PlanningTemplateDto,
  PlanningTemplateDetailDto,
  PlanningTemplateColumnDto,
} from '../../data/planning-api.models';

/**
 * Gestión de plantillas del plan anual y sus columnas configurables.
 * Conectada al backend M2 (POST/GET/PUT planning-templates).
 */
@Component({
  selector: 'app-config-plantillas',
  imports: [PageShellComponent, FormsModule],
  template: `
    <app-page-shell
      title="Plantillas del plan anual"
      description="Columnas y validaciones de la capa anual operativa · cada cambio genera versión y rastro de auditoría"
    >
      @if (confirmacion()) {
        <div class="slide-in-top mb-4 rounded-xl border border-success-border bg-success-bg px-4 py-3" role="status">
          <p class="!text-sm font-medium text-success-text">{{ confirmacion() }}</p>
        </div>
      }

      @if (error()) {
        <div class="slide-in-top mb-4 rounded-xl border border-error-border bg-error-bg px-4 py-3" role="alert">
          <p class="!text-sm font-medium text-error-text">{{ error() }}</p>
        </div>
      }

      <!-- Selector de plantilla + crear nueva -->
      <div class="mb-4 flex flex-wrap items-center justify-between gap-3">
        <div class="flex items-center gap-2">
          <label class="field-label !mb-0">Plantilla:</label>
          <select class="field-control !w-auto" [(ngModel)]="selectedTemplateId" (ngModelChange)="onTemplateSelected($event)">
            <option value="">— Seleccione —</option>
            @for (t of templates(); track t.id) {
              <option [value]="t.id">{{ t.name }} · v{{ t.version }} {{ t.isActive ? '(activa)' : '' }}</option>
            }
          </select>
        </div>
        <button type="button" class="btn-primary" (click)="showCreateForm.set(!showCreateForm())">
          {{ showCreateForm() ? 'Cancelar' : '+ Nueva plantilla' }}
        </button>
      </div>

      <!-- Formulario crear plantilla -->
      @if (showCreateForm()) {
        <div class="card slide-in-top mb-4 p-5">
          <h2 class="!font-poppins !text-base font-semibold text-ink">Crear plantilla</h2>
          <div class="mt-3 grid grid-cols-1 gap-3 lg:grid-cols-2">
            <label class="field-label">Nombre
              <input class="field-control mt-1" [(ngModel)]="newTemplateName" placeholder="Ej.: Formulario de Cadena de Valor" />
            </label>
            <label class="field-label">Descripción (opcional)
              <input class="field-control mt-1" [(ngModel)]="newTemplateDescription" placeholder="Plantilla para el ciclo 2027" />
            </label>
          </div>
          <div class="mt-4 flex flex-wrap items-center justify-between gap-3 border-t border-border pt-3">
            <label class="!flex items-center gap-2 text-xs font-medium text-ink cursor-pointer">
              <input type="checkbox" class="accent-brand size-4" [(ngModel)]="seedOnCreate" />
              Poblar automáticamente con las 13 columnas estándar del sistema (Recomendado)
            </label>
            <div class="flex items-center gap-2">
              <button type="button" class="btn-ghost" (click)="showCreateForm.set(false)">Cancelar</button>
              <button type="button" class="btn-primary" [disabled]="!newTemplateName.trim() || saving()" (click)="crearPlantilla()">
                {{ saving() ? 'Guardando…' : (seedOnCreate ? '⚡ Crear y cargar 13 columnas' : 'Crear plantilla') }}
              </button>
            </div>
          </div>
        </div>
      }

      @if (loading()) {
        <div class="flex items-center justify-center py-12">
          <div class="size-8 animate-spin rounded-full border-4 border-brand border-t-transparent"></div>
        </div>
      }

      @if (detail()) {
        <div class="grid grid-cols-1 gap-6 lg:grid-cols-12">
          <!-- Tabla de columnas -->
          <div class="card p-0 lg:col-span-8 xl:col-span-9">
            <div class="flex flex-wrap items-center justify-between gap-2 border-b border-border px-4 py-3">
              <div>
                <h2 class="!font-poppins !text-base font-semibold text-ink">Columnas de la plantilla</h2>
                <p class="!text-xs text-ink-muted">{{ detail()!.name }} · v{{ detail()!.version }}</p>
              </div>
              <div class="flex flex-wrap items-center gap-2">
                <button type="button" class="btn-outline btn-sm" [disabled]="saving()" (click)="poblarColumnasEstandar()">
                  ⚡ Cargar 13 columnas estándar
                </button>
                <button type="button" class="btn-ghost btn-sm !text-error hover:!bg-error-bg" [disabled]="saving()" (click)="eliminarPlantilla()">
                  Eliminar plantilla
                </button>
              </div>
            </div>
            <div class="overflow-x-auto">
              <table class="w-full text-sm">
                <thead>
                  <tr>
                    @for (h of ['Columna', 'Nombre técnico', 'Tipo', 'Obligatoria', 'Origen', 'Versión', 'Visible', 'Acciones']; track h) {
                      <th class="border-b border-border bg-surface-sunken px-4 py-2 text-left text-xs font-semibold tracking-wide whitespace-nowrap text-ink-muted uppercase">{{ h }}</th>
                    }
                  </tr>
                </thead>
                <tbody>
                  @for (c of detail()!.columns; track c.id) {
                    <tr class="hover:bg-surface">
                      <td class="border-b border-border px-4 py-2.5 font-medium whitespace-nowrap">{{ c.visibleName }}</td>
                      <td class="border-b border-border px-4 py-2.5 font-mono text-xs text-ink-muted whitespace-nowrap">{{ c.technicalName }}</td>
                      <td class="border-b border-border px-4 py-2.5 text-ink-muted whitespace-nowrap">{{ c.type }}</td>
                      <td class="border-b border-border px-4 py-2.5 whitespace-nowrap">
                        <span [class]="c.required ? 'badge-warning' : 'badge-neutral'">{{ c.required ? 'Sí' : 'No' }}</span>
                      </td>
                      <td class="border-b border-border px-4 py-2.5 whitespace-nowrap">
                        <span [class]="c.origin === 'Base' ? 'badge-neutral' : 'badge-info'">{{ c.origin }}</span>
                      </td>
                      <td class="border-b border-border px-4 py-2.5 whitespace-nowrap text-ink-muted text-xs">
                        v{{ c.version }} · {{ c.createdByName || 'Sistema' }}
                      </td>
                      <td class="border-b border-border px-4 py-2.5 whitespace-nowrap">
                        <span [class]="c.visible ? 'text-success-text font-medium' : 'text-ink-subtle'">
                          {{ c.visible ? 'Sí' : 'No' }}
                        </span>
                      </td>
                      <td class="border-b border-border px-4 py-2.5 whitespace-nowrap">
                        <div class="flex items-center gap-1.5">
                          @if (c.origin === 'Custom') {
                            <button type="button" class="btn-ghost btn-xs" [disabled]="saving()"
                              (click)="toggleColumnaVisible(c)">
                              {{ c.visible ? 'Ocultar' : 'Mostrar' }}
                            </button>
                          }
                          <button type="button" class="btn-ghost btn-xs !text-error hover:!bg-error-bg" [disabled]="saving()"
                            (click)="borrarColumna(c)">
                            Borrar
                          </button>
                        </div>
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
            @if (!detail()!.columns.length) {
              <p class="px-4 py-6 text-center !text-sm text-ink-muted">Sin columnas configuradas todavía.</p>
            }
          </div>

          <!-- Formulario de nueva columna -->
          <div class="card h-fit p-5 lg:col-span-4 xl:col-span-3">
            <h2 class="!font-poppins !text-base font-semibold text-ink">Agregar columna</h2>
            <p class="mt-1 !text-xs text-ink-muted">
              La columna aparece de inmediato en los grids del ciclo activo, sin cambios de código.
            </p>
            <div class="mt-4 flex flex-col gap-3">
              <label class="field-label">Nombre visible
                <input class="field-control mt-1" [(ngModel)]="nombreVisible" placeholder="Ej.: Municipio" />
              </label>
              <label class="field-label">Nombre técnico
                <input class="field-control mt-1 font-mono" [value]="nombreTecnico()" readonly />
              </label>
              <label class="field-label">Tipo de campo
                <select class="field-control mt-1" [(ngModel)]="tipo">
                  <option value="Text">Texto</option>
                  <option value="Number">Número</option>
                  <option value="Currency">Moneda</option>
                  <option value="Select">Selección (lista)</option>
                </select>
              </label>
              @if (tipo === 'Select') {
                <label class="field-label">Opciones (una por línea)
                  <textarea class="mt-1 !h-28 w-full rounded-lg border border-border-strong px-3 py-2 font-mono !text-xs" [(ngModel)]="opcionesTexto"></textarea>
                </label>
              }
              <label class="!flex items-center gap-2 !text-sm !font-normal text-ink">
                <input type="checkbox" class="!size-4 accent-brand" [(ngModel)]="obligatoria" />
                Obligatoria para someter el plan
              </label>
              <button type="button" class="btn-primary" [disabled]="!nombreVisible.trim() || saving()" (click)="agregarColumna()">
                {{ saving() ? 'Guardando…' : 'Agregar columna' }}
              </button>
              <p class="!text-xs text-ink-subtle">
                Se registrará como versión nueva con autor y fecha, y podrá revertirse desde el
                historial de configuración.
              </p>
            </div>
          </div>
        </div>
      }
    </app-page-shell>
  `,
})
export class ConfigPlantillasComponent implements OnInit {
  private readonly service = inject(PlanningTemplateService);

  protected readonly templates = signal<PlanningTemplateDto[]>([]);
  protected readonly detail = signal<PlanningTemplateDetailDto | null>(null);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly confirmacion = signal('');
  protected readonly error = signal('');
  protected readonly showCreateForm = signal(false);

  protected selectedTemplateId = '';
  protected newTemplateName = '';
  protected newTemplateDescription = '';

  // New column form
  protected nombreVisible = '';
  protected tipo = 'Text';
  protected obligatoria = false;
  protected opcionesTexto = '';

  protected readonly nombreTecnico = computed(() =>
    this.nombreVisible
      .trim()
      .toLowerCase()
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .replace(/[^a-z0-9]+/g, '_')
      .replace(/^_+|_+$/g, ''),
  );

  ngOnInit(): void {
    this.loadTemplates();
  }

  private loadTemplates(): void {
    this.loading.set(true);
    this.service.list().subscribe({
      next: (list) => {
        this.templates.set(list);
        // Auto-select the active template
        const active = list.find((t) => t.isActive);
        if (active) {
          this.selectedTemplateId = active.id;
          this.loadDetail(active.id);
        } else if (list.length) {
          this.selectedTemplateId = list[0].id;
          this.loadDetail(list[0].id);
        } else {
          this.loading.set(false);
        }
      },
      error: () => {
        this.error.set('No se pudieron cargar las plantillas.');
        this.loading.set(false);
      },
    });
  }

  private loadDetail(id: string): void {
    this.loading.set(true);
    this.service.getById(id).subscribe({
      next: (d) => {
        this.detail.set(d);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('No se pudo cargar el detalle de la plantilla.');
        this.loading.set(false);
      },
    });
  }

  protected onTemplateSelected(id: string): void {
    if (id) {
      this.detail.set(null);
      this.loadDetail(id);
    }
  }

  protected seedOnCreate = true;

  protected crearPlantilla(): void {
    if (!this.newTemplateName.trim()) return;
    this.saving.set(true);
    this.clearMessages();
    this.service.create({ name: this.newTemplateName.trim(), description: this.newTemplateDescription.trim() || undefined }).subscribe({
      next: (created) => {
        if (this.seedOnCreate) {
          this.service.seedStandardColumns(created.id).subscribe({
            next: (seeded) => {
              this.confirmacion.set(`Plantilla «${created.name}» creada y poblada con las 13 columnas estándar.`);
              this.showCreateForm.set(false);
              this.newTemplateName = '';
              this.newTemplateDescription = '';
              this.saving.set(false);
              this.selectedTemplateId = seeded.id;
              this.detail.set(seeded);
              this.loadTemplates();
            },
            error: () => {
              this.confirmacion.set(`Plantilla «${created.name}» creada (no se pudieron poblar las columnas automáticamente).`);
              this.showCreateForm.set(false);
              this.newTemplateName = '';
              this.newTemplateDescription = '';
              this.saving.set(false);
              this.loadTemplates();
            },
          });
        } else {
          this.confirmacion.set(`Plantilla «${created.name}» creada.`);
          this.showCreateForm.set(false);
          this.newTemplateName = '';
          this.newTemplateDescription = '';
          this.saving.set(false);
          this.loadTemplates();
        }
      },
      error: () => {
        this.error.set('Error al crear la plantilla.');
        this.saving.set(false);
      },
    });
  }

  protected eliminarPlantilla(): void {
    const d = this.detail();
    if (!d) return;
    if (!confirm(`¿Estás seguro de que deseas eliminar la plantilla «${d.name}»?\n\nLos planes POA existentes que utilicen esta plantilla conservarán todos sus datos intactos.`)) return;
    this.saving.set(true);
    this.clearMessages();
    this.service.deleteTemplate(d.id).subscribe({
      next: () => {
        this.confirmacion.set(`Plantilla «${d.name}» eliminada.`);
        this.selectedTemplateId = '';
        this.detail.set(null);
        this.saving.set(false);
        this.loadTemplates();
      },
      error: (err) => {
        this.error.set(err.error?.message || 'Error al eliminar la plantilla.');
        this.saving.set(false);
      },
    });
  }

  protected poblarColumnasEstandar(): void {
    const d = this.detail();
    if (!d) return;
    this.saving.set(true);
    this.clearMessages();
    this.service.seedStandardColumns(d.id).subscribe({
      next: (updated) => {
        this.detail.set(updated);
        this.confirmacion.set('Se poblaron exitosamente las 13 columnas estándar en la plantilla.');
        this.saving.set(false);
      },
      error: (err) => {
        this.error.set(err.error?.message || 'Error al poblar las columnas estándar.');
        this.saving.set(false);
      },
    });
  }

  protected agregarColumna(): void {
    const d = this.detail();
    if (!d) return;
    this.saving.set(true);
    this.clearMessages();

    const opciones = this.tipo === 'Select'
      ? this.opcionesTexto.split('\n').map((o) => o.trim()).filter(Boolean)
      : undefined;

    this.service.addColumn(d.id, {
      visibleName: this.nombreVisible.trim(),
      technicalName: this.nombreTecnico(),
      type: this.tipo,
      required: this.obligatoria,
      visible: true,
      origin: 'Custom',
      optionsJson: opciones ? JSON.stringify(opciones) : undefined,
      order: d.columns.length,
    }).subscribe({
      next: () => {
        const nameAdded = this.nombreVisible.trim();
        this.nombreVisible = '';
        this.opcionesTexto = '';
        this.obligatoria = false;
        this.tipo = 'Text';
        this.service.getById(d.id).subscribe((refreshed) => {
          this.detail.set(refreshed);
          this.confirmacion.set(`Columna «${nameAdded}» agregada.`);
          this.saving.set(false);
          this.loadTemplates();
        });
      },
      error: () => {
        this.error.set('Error al agregar la columna.');
        this.saving.set(false);
      },
    });
  }

  protected toggleColumnaVisible(c: PlanningTemplateColumnDto): void {
    const d = this.detail();
    if (!d) return;
    this.saving.set(true);
    this.clearMessages();
    this.service.updateColumn(d.id, c.id, {
      visibleName: c.visibleName,
      type: c.type,
      required: c.required,
      visible: !c.visible,
      optionsJson: c.optionsJson ?? undefined,
      order: c.order,
    }).subscribe({
      next: () => {
        this.service.getById(d.id).subscribe((refreshed) => {
          this.detail.set(refreshed);
          this.saving.set(false);
        });
      },
      error: () => {
        this.error.set('Error al cambiar la visibilidad.');
        this.saving.set(false);
      },
    });
  }

  protected borrarColumna(c: PlanningTemplateColumnDto): void {
    const d = this.detail();
    if (!d) return;
    if (!confirm(`¿Estás seguro de que deseas eliminar la columna «${c.visibleName}»?`)) return;
    this.saving.set(true);
    this.clearMessages();
    this.service.deleteColumn(d.id, c.id).subscribe({
      next: (updated) => {
        this.detail.set(updated);
        this.confirmacion.set(`Columna «${c.visibleName}» eliminada.`);
        this.saving.set(false);
        this.loadTemplates();
      },
      error: () => {
        this.error.set('Error al eliminar la columna.');
        this.saving.set(false);
      },
    });
  }

  private clearMessages(): void {
    this.confirmacion.set('');
    this.error.set('');
  }
}
