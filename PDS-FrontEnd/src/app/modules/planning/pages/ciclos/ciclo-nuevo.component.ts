import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { PageShellComponent } from '@public-intelligence/ui-core';
import { WizardStepsComponent } from '../../components/wizard-steps.component';
import { SummaryListComponent, SummaryRow } from '../../components/summary-list.component';
import { PlanningCycleService } from '../../../../services/planning/planning-cycle.service';
import { PlanningTemplateService } from '../../../../services/planning/planning-template.service';
import { EligibleAreaDto, PlanningTemplateDto } from '../../data/planning-api.models';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../../environments/environment';

const PASOS = [
  'Datos del ciclo',
  'Alcance organizacional',
  'Plantilla',
  'Fechas e hitos',
  'Revisión y publicación',
];

interface StrategicPlanItem {
  id: string;
  name: string;
  status: string;
}

@Component({
  selector: 'app-ciclo-nuevo',
  standalone: true,
  imports: [PageShellComponent, FormsModule, WizardStepsComponent, SummaryListComponent],
  template: `
    <app-page-shell
      title="Crear ciclo de planificación"
      description="Las decisiones de este asistente definen el comportamiento del sistema durante todo el ciclo"
    >
      <div class="mx-auto max-w-3xl">
        <button type="button" class="btn-ghost btn-sm mb-3 -ml-2" (click)="atras()">← Volver</button>

        @if (errorMsg()) {
          <div class="slide-in-top mb-4 rounded-xl border border-danger-border bg-danger-bg px-4 py-3 text-sm text-danger-text">
            {{ errorMsg() }}
            <button class="btn-ghost btn-sm ml-2" (click)="errorMsg.set(null)">✕</button>
          </div>
        }

        <div class="card p-6">
          <app-wizard-steps [pasos]="pasos" [actual]="paso()" />
          <hr class="my-4 border-border" />

          @switch (paso()) {
            @case (0) {
              <h2 class="!font-poppins !text-xl font-semibold text-ink">Defina el ciclo</h2>
              <p class="mt-1 !text-sm text-ink-muted">Nombre del artefacto anual y período que cubrirá.</p>
              <div class="mt-4 flex max-w-md flex-col gap-4">
                <label class="field-label">Nombre del ciclo
                  <input class="field-control mt-1" [(ngModel)]="nombre" placeholder="Ej.: POA 2028" />
                </label>
                <label class="field-label">Año
                  <input type="number" class="field-control mt-1" [(ngModel)]="year" min="2024" max="2050" />
                </label>
                <label class="field-label">Año de fin (opcional, para ciclos plurianuales)
                  <input type="number" class="field-control mt-1" [(ngModel)]="endYear" placeholder="Dejar en blanco si es anual" />
                </label>
                
                <label class="field-label">Plan Estratégico (PEI)
                  <select class="field-control mt-1" [(ngModel)]="selectedStrategicPlanId">
                    @for (pei of strategicPlans(); track pei.id) {
                      <option [value]="pei.id">
                        {{ pei.name }} ({{ pei.status === 'Current' ? 'Vigente' : pei.status }})
                      </option>
                    }
                  </select>
                </label>
              </div>
            }
            @case (1) {
              <h2 class="!font-poppins !text-xl font-semibold text-ink">¿Qué áreas participan?</h2>
              <p class="mt-1 !text-sm text-ink-muted">
                Participan las unidades del organigrama habilitadas en la regla de configuración. Cada una recibirá su plan para formular.
              </p>
              @if (loadingAreas()) {
                <p class="mt-4 text-sm text-ink-muted">Cargando áreas de la estructura...</p>
              } @else if (eligibleAreas().length === 0) {
                <p class="mt-4 text-sm text-ink-muted">No se encontraron áreas habilitadas según la regla configurada.</p>
              } @else {
                <div class="mt-4 flex justify-between items-center mb-2">
                  <span class="text-xs text-ink-subtle">{{ selectedAreaIds().length }} de {{ eligibleAreas().length }} áreas seleccionadas</span>
                  <button type="button" class="btn-ghost btn-xs text-brand" (click)="toggleAllAreas()">
                    {{ selectedAreaIds().length === eligibleAreas().length ? 'Desmarcar todas' : 'Seleccionar todas' }}
                  </button>
                </div>
                <ul class="grid grid-cols-1 gap-2 sm:grid-cols-2 max-h-80 overflow-y-auto pr-1">
                  @for (a of eligibleAreas(); track a.areaId) {
                    <li>
                      <label class="!flex items-center gap-2 rounded-lg border border-border px-3 py-2 !text-sm !font-normal text-ink hover:bg-surface cursor-pointer">
                        <input
                          type="checkbox"
                          class="!size-4 accent-brand"
                          [checked]="selectedAreaIds().includes(a.areaId)"
                          (change)="toggleArea(a.areaId)"
                        />
                        <span class="block truncate font-medium">{{ a.areaName }}</span>
                      </label>
                    </li>
                  }
                </ul>
              }
            }
            @case (2) {
              <h2 class="!font-poppins !text-xl font-semibold text-ink">Plantilla del plan anual</h2>
              <p class="mt-1 !text-sm text-ink-muted">
                La plantilla define las columnas del plan anual que usarán las áreas.
              </p>
              @if (loadingTemplates()) {
                <p class="mt-4 text-sm text-ink-muted">Cargando plantillas activas...</p>
              } @else if (templates().length === 0) {
                <p class="mt-4 text-sm text-danger">No hay plantillas registradas. Debe crear una plantilla en Configuración.</p>
              } @else {
                <div class="mt-4 flex flex-col gap-2">
                  @for (t of templates(); track t.id) {
                    <label 
                      class="!flex items-start gap-2 rounded-lg border px-3 py-2.5 !text-sm !font-normal cursor-pointer" 
                      [class]="selectedTemplateId() === t.id ? 'border-brand bg-brand-50' : 'border-border hover:bg-surface'"
                    >
                      <input 
                        type="radio" 
                        name="template" 
                        class="mt-0.5 accent-brand" 
                        [checked]="selectedTemplateId() === t.id" 
                        (change)="selectedTemplateId.set(t.id)" 
                      />
                      <span>
                        <span class="block font-medium text-ink">{{ t.name }} (v{{ t.version }})</span>
                        <span class="block text-xs text-ink-muted">{{ t.description || 'Sin descripción' }} · {{ t.columnCount }} columnas</span>
                        @if (t.isActive) {
                          <span class="badge-success mt-1 inline-block">Plantilla activa</span>
                        }
                      </span>
                    </label>
                  }
                </div>
              }
            }
            @case (3) {
              <h2 class="!font-poppins !text-xl font-semibold text-ink">Calendario del ciclo</h2>
              <p class="mt-1 !text-sm text-ink-muted">
                Establezca los hitos del proceso. El hito <strong>Límite de formulación</strong> define la fecha límite de entrega de los planes.
              </p>
              <div class="mt-4 flex flex-col gap-3">
                @for (h of hitos(); track $index; let i = $index) {
                  <div class="grid grid-cols-1 sm:grid-cols-12 gap-2 items-center p-3 rounded-lg border border-border bg-surface">
                    <div class="sm:col-span-5">
                      <label class="field-label text-xs">Nombre del hito
                        <input class="field-control mt-1" [(ngModel)]="h.name" />
                      </label>
                    </div>
                    <div class="sm:col-span-4">
                      <label class="field-label text-xs">Tipo de hito
                        <select class="field-control mt-1" [(ngModel)]="h.kind">
                          <option value="FormulationDeadline">Límite de formulación</option>
                          <option value="Review">Revisión de planificación</option>
                          <option value="Approval">Aprobación directiva</option>
                          <option value="Other">Otro hito</option>
                        </select>
                      </label>
                    </div>
                    <div class="sm:col-span-3">
                      <label class="field-label text-xs">Fecha
                        <input type="date" class="field-control mt-1" [(ngModel)]="h.date" />
                      </label>
                    </div>
                  </div>
                }
              </div>
              <button type="button" class="btn-ghost btn-sm mt-3" (click)="agregarHito()">+ Agregar hito</button>
            }
            @case (4) {
              <h2 class="!font-poppins !text-xl font-semibold text-ink">Revise antes de publicar</h2>
              <p class="mt-1 !text-sm text-ink-muted">
                Al publicar el ciclo, las áreas seleccionadas recibirán automáticamente su plan anual (POA) en estado «Sin iniciar».
              </p>
              <div class="mt-4">
                <app-summary-list [filas]="resumen()" (cambiar)="irAPaso($event)" />
              </div>
              <div class="mt-4 rounded-lg bg-warning-bg px-3 py-2.5 text-sm text-warning-text ring-1 ring-warning-border">
                El ciclo se creará y publicará. Se generará 1 plan anual por cada área alcanzada.
              </div>
            }
          }

          <div class="mt-6 flex items-center justify-between border-t border-border pt-4">
            <button type="button" class="btn-ghost" (click)="atras()">Atrás</button>
            @if (paso() < 4) {
              <button 
                type="button" 
                class="btn-primary" 
                (click)="continuar()" 
                [disabled]="!puedoAvanzar()"
              >
                Continuar
              </button>
            } @else {
              <div class="flex gap-2">
                <button 
                  type="button" 
                  class="btn-ghost" 
                  (click)="guardarBorrador()" 
                  [disabled]="isSubmitting()"
                >
                  Guardar como borrador
                </button>
                <button 
                  type="button" 
                  class="btn-primary" 
                  (click)="guardarYPublicar()" 
                  [disabled]="isSubmitting()"
                >
                  {{ isSubmitting() ? 'Procesando...' : 'Publicar ciclo' }}
                </button>
              </div>
            }
          </div>
        </div>
      </div>
    </app-page-shell>
  `,
})
export class CicloNuevoComponent implements OnInit {
  private readonly cycleService = inject(PlanningCycleService);
  private readonly templateService = inject(PlanningTemplateService);
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  protected readonly pasos = PASOS;
  protected readonly paso = signal(0);
  errorMsg = signal<string | null>(null);
  isSubmitting = signal(false);

  // Data sources
  eligibleAreas = signal<EligibleAreaDto[]>([]);
  loadingAreas = signal(true);

  templates = signal<PlanningTemplateDto[]>([]);
  loadingTemplates = signal(true);

  strategicPlans = signal<StrategicPlanItem[]>([]);

  // Form State
  nombre = 'POA ' + (new Date().getFullYear() + 1);
  year = new Date().getFullYear() + 1;
  endYear: number | null = null;

  selectedStrategicPlanId = signal<string>('');
  selectedTemplateId = signal<string>('');
  selectedAreaIds = signal<string[]>([]);

  hitos = signal<{ name: string; date: string; kind: string }[]>([
    { name: 'Formulación por áreas', date: `${new Date().getFullYear() + 1}-08-15`, kind: 'FormulationDeadline' },
    { name: 'Revisión de planificación', date: `${new Date().getFullYear() + 1}-09-15`, kind: 'Review' },
    { name: 'Aprobación directiva', date: `${new Date().getFullYear() + 1}-11-15`, kind: 'Approval' },
  ]);

  ngOnInit() {
    // Load eligible areas
    this.cycleService.getEligibleAreas().subscribe({
      next: (areas) => {
        this.eligibleAreas.set(areas);
        this.selectedAreaIds.set(areas.map((a) => a.areaId));
        this.loadingAreas.set(false);
      },
      error: () => this.loadingAreas.set(false),
    });

    // Load templates
    this.templateService.list().subscribe({
      next: (data) => {
        this.templates.set(data);
        const active = data.find((t) => t.isActive) || data[0];
        if (active) this.selectedTemplateId.set(active.id);
        this.loadingTemplates.set(false);
      },
      error: () => this.loadingTemplates.set(false),
    });

    // Load strategic plans
    this.http.get<StrategicPlanItem[]>(`${environment.apiUrl}strategic-plans`).subscribe({
      next: (plans) => {
        this.strategicPlans.set(plans);
        const current = plans.find((p) => p.status === 'Current') || plans[0];
        if (current) this.selectedStrategicPlanId.set(current.id);
      },
      error: (err) => console.error('Error al cargar PEI:', err),
    });
  }

  puedoAvanzar(): boolean {
    const p = this.paso();
    if (p === 0) return !!this.nombre.trim() && !!this.year && !!this.selectedStrategicPlanId();
    if (p === 1) return this.selectedAreaIds().length > 0;
    if (p === 2) return !!this.selectedTemplateId();
    if (p === 3) return this.hitos().length > 0 && this.hitos().some((h) => h.kind === 'FormulationDeadline');
    return true;
  }

  protected readonly resumen = computed<SummaryRow[]>(() => {
    const selectedPei = this.strategicPlans().find((p) => p.id === this.selectedStrategicPlanId());
    const selectedTpl = this.templates().find((t) => t.id === this.selectedTemplateId());
    const deadlineHito = this.hitos().find((h) => h.kind === 'FormulationDeadline');

    return [
      { etiqueta: 'Nombre', valor: this.nombre, cambiarRef: '0' },
      { etiqueta: 'Año', valor: `${this.year}${this.endYear ? '–' + this.endYear : ''}`, cambiarRef: '0' },
      { etiqueta: 'PEI vinculado', valor: selectedPei ? selectedPei.name : 'No seleccionado', cambiarRef: '0' },
      { etiqueta: 'Áreas alcanzadas', valor: `${this.selectedAreaIds().length} áreas participantes`, cambiarRef: '1' },
      { etiqueta: 'Plantilla', valor: selectedTpl ? selectedTpl.name : 'No seleccionada', cambiarRef: '2' },
      {
        etiqueta: 'Límite de entrega',
        valor: deadlineHito ? `${deadlineHito.name} (${deadlineHito.date})` : 'No definido',
        cambiarRef: '3',
      },
    ];
  });

  toggleArea(id: string): void {
    this.selectedAreaIds.update((ids) => (ids.includes(id) ? ids.filter((x) => x !== id) : [...ids, id]));
  }

  toggleAllAreas(): void {
    if (this.selectedAreaIds().length === this.eligibleAreas().length) {
      this.selectedAreaIds.set([]);
    } else {
      this.selectedAreaIds.set(this.eligibleAreas().map((a) => a.areaId));
    }
  }

  agregarHito(): void {
    this.hitos.update((hs) => [...hs, { name: 'Nuevo hito', date: `${this.year}-10-01`, kind: 'Other' }]);
  }

  continuar(): void {
    this.paso.update((p) => Math.min(p + 1, 4));
  }

  atras(): void {
    if (this.paso() === 0) {
      this.router.navigate(['/ciclos']);
    } else {
      this.paso.update((p) => p - 1);
    }
  }

  irAPaso(ref: string): void {
    this.paso.set(Number(ref));
  }

  guardarBorrador(): void {
    this.errorMsg.set(null);
    this.isSubmitting.set(true);

    const dto = {
      name: this.nombre.trim(),
      year: this.year,
      endYear: this.endYear || null,
      strategicPlanId: this.selectedStrategicPlanId(),
      templateId: this.selectedTemplateId(),
      milestones: this.hitos(),
      areaIds: this.selectedAreaIds(),
    };

    this.cycleService.createDraft(dto).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.router.navigate(['/ciclos']);
      },
      error: (err) => {
        this.isSubmitting.set(false);
        const detail = err?.error?.title || err?.error?.errors?.year?.[0] || 'Error al guardar el borrador del ciclo.';
        this.errorMsg.set(detail);
      },
    });
  }

  guardarYPublicar(): void {
    this.errorMsg.set(null);
    this.isSubmitting.set(true);

    const dto = {
      name: this.nombre.trim(),
      year: this.year,
      endYear: this.endYear || null,
      strategicPlanId: this.selectedStrategicPlanId(),
      templateId: this.selectedTemplateId(),
      milestones: this.hitos(),
      areaIds: this.selectedAreaIds(),
    };

    this.cycleService.createDraft(dto).subscribe({
      next: (created) => {
        this.cycleService.publish(created.id).subscribe({
          next: () => {
            this.isSubmitting.set(false);
            this.router.navigate(['/ciclos'], { queryParams: { publicado: this.nombre } });
          },
          error: (err) => {
            this.isSubmitting.set(false);
            const detail = err?.error?.title || err?.error?.errors?.[0] || 'El ciclo fue creado como borrador, pero falló al publicar.';
            this.errorMsg.set(detail);
          },
        });
      },
      error: (err) => {
        this.isSubmitting.set(false);
        const detail = err?.error?.title || err?.error?.errors?.year?.[0] || 'Error al crear el ciclo.';
        this.errorMsg.set(detail);
      },
    });
  }
}
