import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PageShellComponent } from '@public-intelligence/ui-core';
import { ConsolidatorRoutingService } from '../../../../services/planning/consolidator-routing.service';
import { RegulatoryCatalogService } from '../../../../services/planning/regulatory-catalog.service';
import {
  ConsolidatorRoutingRuleDto,
  ConsolidatorAreaProfileDto,
  RulePreviewResultDto
} from '../../data/planning-api.models';

@Component({
  selector: 'app-reglas-ruteo-consolidador',
  standalone: true,
  imports: [CommonModule, FormsModule, PageShellComponent],
  template: `
    <app-page-shell
      title="Motor de Reglas de Ruteo a Áreas Consolidadoras"
      description="Gobernanza y asignación automatizada de bienes, servicios y obras a áreas consolidadoras mediante patrones de UNSPSC, Objeto del Gasto y Palabras Clave"
    >
      <div class="space-y-6">
        <!-- Action Header Card -->
        <div class="card p-4 flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div class="flex items-center gap-3">
            <span class="p-2.5 rounded-xl bg-brand-50 text-brand border border-brand-200">
              <i class="bi bi-diagram-3-fill text-xl"></i>
            </span>
            <div>
              <h2 class="text-sm font-bold text-ink">Matriz Institucional de Ruteo</h2>
              <p class="text-xs text-ink-muted">
                Evaluación por jerarquía de precedencia (Priority), validación institucional y ruteo a Maestra Institucional.
              </p>
            </div>
          </div>

          <div class="flex items-center gap-3">
            <button
              type="button"
              (click)="openReevaluateModal()"
              [disabled]="isReevaluating"
              class="btn-secondary flex items-center gap-2"
            >
              @if (isReevaluating) {
                <span class="inline-block h-4 w-4 animate-spin rounded-full border-2 border-brand border-t-transparent"></span>
              } @else {
                <i class="bi bi-arrow-repeat text-base"></i>
              }
              Re-evaluar Maestra No Ratificada
            </button>
            <button
              type="button"
              (click)="openCreateModal()"
              class="btn-primary flex items-center gap-2"
            >
              <i class="bi bi-plus-lg"></i>
              Nueva Regla de Ruteo
            </button>
          </div>
        </div>

        <!-- Educational Priority Help Banner -->
        <div class="p-4 bg-sky-50/80 border border-sky-200 rounded-2xl flex items-start gap-3 text-xs">
          <i class="bi bi-info-circle-fill text-sky-600 text-lg flex-shrink-0 mt-0.5"></i>
          <div class="space-y-1 text-sky-900">
            <span class="font-bold text-sky-950">¿Cómo se evalúa la Prioridad en el Ruteo?</span>
            <p class="leading-relaxed">
              Las reglas se procesan en orden numérico ascendente (ej.: <b>Prioridad 10</b> se evalúa antes que <b>Prioridad 40</b>). Si un artículo coincide con múltiples reglas activas, el motor asigna automáticamente el Área Consolidadora de la regla con el <b>menor número de prioridad</b>.
            </p>
          </div>
        </div>

        <!-- Filter & Search Controls Bar -->
        <div class="card p-4 flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div class="flex items-center gap-3 w-full md:w-auto">
            <input
              type="text"
              [(ngModel)]="filterQuery"
              placeholder="Buscar regla por clave, área o palabras clave..."
              class="field-control w-full md:w-80 text-xs"
            />

            <select [(ngModel)]="filterStatus" class="field-control font-select !w-44 text-xs">
              <option value="ALL">-- Todos los Estados --</option>
              <option value="ACTIVE">Solo Activas</option>
              <option value="INACTIVE">Solo Inactivas</option>
            </select>
          </div>

          <div class="text-xs text-ink-muted">
            Mostrando <b>{{ filteredRules.length }}</b> de <b>{{ rules.length }}</b> reglas
          </div>
        </div>

        <!-- Rules Cards List -->
        <div class="grid grid-cols-1 gap-4">
          @if (isLoading) {
            <div class="p-12 text-center text-ink-muted card">
              <span class="inline-block h-5 w-5 animate-spin rounded-full border-2 border-brand border-t-transparent mr-2"></span>
              Cargando motor de reglas de ruteo...
            </div>
          } @else if (!filteredRules || filteredRules.length === 0) {
            <div class="p-12 text-center text-ink-muted card">
              No se encontraron reglas de ruteo con los filtros seleccionados.
            </div>
          } @else {
            @for (rule of filteredRules; track rule.id) {
              <div class="card p-5 hover:shadow-md transition-shadow border border-border space-y-4">
                <!-- Top Row: Rule Header Badges & Actions -->
                <div class="flex flex-col md:flex-row md:items-center justify-between gap-3 border-b border-border pb-3">
                  <div class="flex flex-wrap items-center gap-2.5">
                    <span class="px-2.5 py-1 bg-brand text-white font-mono font-bold text-xs rounded-md shadow-sm">
                      {{ rule.ruleKey }}
                    </span>

                    <span class="text-xs px-2.5 py-0.5 rounded font-bold bg-slate-100 text-slate-800 border border-slate-200">
                      Prioridad: {{ rule.priority }}
                    </span>

                    <span class="text-xs px-2 py-0.5 rounded font-mono font-semibold bg-brand-50 text-brand border border-brand-200">
                      v{{ rule.version }}
                    </span>

                    <span
                      class="text-xs px-2.5 py-0.5 rounded-full font-semibold border"
                      [ngClass]="rule.isActive ? 'bg-emerald-50 text-emerald-800 border-emerald-200' : 'bg-slate-100 text-slate-700 border-slate-200'"
                    >
                      ● {{ rule.isActive ? 'Activa' : 'Inactiva' }}
                    </span>

                    @if (rule.institutionallyValidated) {
                      <span class="text-[11px] px-2 py-0.5 rounded font-semibold bg-sky-50 text-sky-800 border border-sky-200 flex items-center gap-1">
                        <i class="bi bi-shield-check"></i> Validada
                      </span>
                    }
                  </div>

                  <!-- Explicit Action Buttons (High Contrast Styling) -->
                  <div class="flex items-center gap-2">
                    <button
                      type="button"
                      (click)="previewRule(rule)"
                      class="px-3 py-1.5 rounded-lg border border-slate-300 text-slate-700 bg-white hover:bg-slate-100 text-xs font-semibold flex items-center gap-1.5 transition-colors shadow-sm"
                    >
                      <i class="bi bi-eye text-brand"></i> Vista Previa
                    </button>

                    <button
                      type="button"
                      (click)="toggleActive(rule)"
                      class="px-3 py-1.5 rounded-lg text-xs font-semibold transition-colors shadow-sm border"
                      [ngClass]="rule.isActive 
                        ? 'bg-amber-50 text-amber-900 border-amber-300 hover:bg-amber-100' 
                        : 'bg-emerald-50 text-emerald-800 border-emerald-300 hover:bg-emerald-100'"
                    >
                      {{ rule.isActive ? 'Desactivar' : 'Activar' }}
                    </button>

                    <button
                      type="button"
                      (click)="confirmDeleteRule(rule)"
                      class="px-3 py-1.5 rounded-lg border border-rose-200 text-rose-700 bg-rose-50 hover:bg-rose-100 text-xs font-semibold flex items-center gap-1 transition-colors shadow-sm"
                      title="Eliminar regla de ruteo"
                    >
                      <i class="bi bi-trash"></i> Eliminar
                    </button>
                  </div>
                </div>

                <!-- Main Content Row -->
                <div class="grid grid-cols-1 md:grid-cols-3 gap-4 text-xs">
                  <!-- Target Area & Description -->
                  <div class="md:col-span-1 space-y-1.5 border-r md:border-border md:pr-4">
                    <div class="text-ink-muted">
                      Área Consolidadora Destino:
                    </div>
                    <div class="text-sm font-bold text-brand leading-snug">
                      {{ rule.consolidatorAreaName }}
                    </div>
                    @if (rule.justification) {
                      <p class="text-xs text-ink-muted leading-relaxed pt-1">
                        {{ rule.justification }}
                      </p>
                    }
                  </div>

                  <!-- UNSPSC Prefixes -->
                  <div class="space-y-1.5">
                    <div class="font-semibold text-ink flex items-center gap-1">
                      <i class="bi bi-tags text-brand"></i> Prefijos UNSPSC / Clasificador:
                    </div>
                    @if (rule.unspscPrefixes) {
                      <div class="flex flex-wrap gap-1">
                        @for (prefix of getList(rule.unspscPrefixes); track prefix) {
                          <span class="px-2 py-0.5 bg-slate-100 border border-slate-200 font-mono text-[11px] font-bold text-slate-800 rounded">
                            {{ prefix }}
                          </span>
                        }
                      </div>
                    } @else {
                      <span class="text-ink-subtle italic">Sin prefijo especificado</span>
                    }
                  </div>

                  <!-- Keywords Matched -->
                  <div class="space-y-1.5">
                    <div class="font-semibold text-ink flex items-center gap-1">
                      <i class="bi bi-search text-brand"></i> Palabras Clave Evaluadas:
                    </div>
                    @if (rule.keywords) {
                      <div class="flex flex-wrap gap-1 max-h-24 overflow-y-auto">
                        @for (kw of getList(rule.keywords); track kw) {
                          <span class="px-2 py-0.5 bg-brand-50 border border-brand-100 text-brand text-[11px] font-medium rounded">
                            {{ kw }}
                          </span>
                        }
                      </div>
                    } @else {
                      <span class="text-ink-subtle italic">Sin palabras clave</span>
                    }
                  </div>
                </div>
              </div>
            }
          }
        </div>
      </div>
    </app-page-shell>

    <!-- Create Rule Modal -->
    @if (isCreateModalOpen) {
      <div class="fixed inset-0 z-50 bg-slate-900/50 backdrop-blur-sm flex items-center justify-center p-4">
        <div class="bg-white rounded-2xl max-w-lg w-full p-6 shadow-2xl border border-border space-y-6">
          <div class="flex items-center justify-between border-b border-border pb-4">
            <h3 class="text-lg font-bold text-ink">Nueva Regla de Ruteo Institucional</h3>
            <button type="button" (click)="isCreateModalOpen = false" class="text-ink-subtle hover:text-ink text-lg font-bold">
              ✕
            </button>
          </div>

          <div class="space-y-4 text-sm">
            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-semibold text-ink mb-1">Código / Clave Regla</label>
                <input type="text" [(ngModel)]="newRuleKey" placeholder="Ej. R-TI-04" class="field-control w-full font-mono uppercase" />
              </div>
              <div>
                <label class="block text-xs font-semibold text-ink mb-1">Prioridad (Precedencia)</label>
                <input type="number" [(ngModel)]="newPriority" placeholder="10" class="field-control w-full font-mono" />
              </div>
            </div>

            <div>
              <label class="block text-xs font-semibold text-ink mb-1">Área Consolidadora Destino</label>
              <select [(ngModel)]="newTargetAreaId" (change)="onAreaChange()" class="field-control w-full font-select">
                <option value="">-- Seleccionar Área Consolidadora --</option>
                @for (area of consolidatorProfiles; track area.organizationalStructureId) {
                  <option [value]="area.organizationalStructureId">
                    {{ area.areaName }}
                  </option>
                }
              </select>
            </div>

            <div>
              <label class="block text-xs font-semibold text-ink mb-1">Prefijos UNSPSC (Separados por |)</label>
              <input type="text" [(ngModel)]="newUnspscPrefixes" placeholder="Ej. 4321|4320|4511" class="field-control w-full font-mono" />
            </div>

            <div>
              <label class="block text-xs font-semibold text-ink mb-1">Palabras Clave Coincidentes (Separadas por |)</label>
              <input type="text" [(ngModel)]="newKeywords" placeholder="Ej. computad|laptop|servidor" class="field-control w-full font-mono" />
            </div>

            <div>
              <label class="block text-xs font-semibold text-ink mb-1">Justificación Institucional</label>
              <textarea rows="2" [(ngModel)]="newJustification" placeholder="Justificación o norma que respalda este ruteo" class="field-control w-full"></textarea>
            </div>
          </div>

          <div class="flex justify-end gap-3 pt-4 border-t border-border">
            <button type="button" (click)="isCreateModalOpen = false" class="btn-outline">
              Cancelar
            </button>
            <button type="button" (click)="saveNewRule()" class="btn-primary">
              Crear Regla
            </button>
          </div>
        </div>
      </div>
    }

    <!-- Re-evaluate Confirmation Modal -->
    @if (isReevaluateModalOpen) {
      <div class="fixed inset-0 z-50 bg-slate-900/50 backdrop-blur-sm flex items-center justify-center p-4">
        <div class="bg-white rounded-2xl max-w-md w-full p-6 shadow-2xl border border-border space-y-5 animate-in fade-in zoom-in-95 duration-100">
          <div class="flex items-center gap-3 text-amber-600">
            <div class="p-2.5 rounded-full bg-amber-50 border border-amber-200">
              <i class="bi bi-exclamation-triangle text-xl"></i>
            </div>
            <div>
              <h3 class="text-base font-bold text-ink">Confirmar Re-evaluación Masiva</h3>
              <p class="text-xs text-ink-muted">Proceso de procesamiento batch del catálogo maestro.</p>
            </div>
          </div>

          <p class="text-xs text-slate-700 leading-relaxed">
            Esta operación recorrerá todos los artículos del catálogo maestro que <b>NO hayan sido ratificados manualmente</b> ni importados con asignación explícita, y ejecutará la matriz de 15 reglas activas para ruteartos automáticamente a su Área Consolidadora correspondiente.
          </p>

          <div class="flex justify-end gap-3 pt-3 border-t border-border">
            <button type="button" (click)="isReevaluateModalOpen = false" class="btn-outline">
              Cancelar
            </button>
            <button type="button" (click)="executeReevaluate()" class="btn-primary flex items-center gap-2">
              <i class="bi bi-arrow-repeat"></i>
              Ejecutar Re-evaluación
            </button>
          </div>
        </div>
      </div>
    }

    <!-- Delete Rule Confirmation Modal -->
    @if (isDeleteModalOpen && ruleToDelete) {
      <div class="fixed inset-0 z-50 bg-slate-900/50 backdrop-blur-sm flex items-center justify-center p-4">
        <div class="bg-white rounded-2xl max-w-md w-full p-6 shadow-2xl border border-border space-y-5 animate-in fade-in zoom-in-95 duration-100">
          <div class="flex items-center gap-3 text-rose-600">
            <div class="p-2.5 rounded-full bg-rose-50 border border-rose-200">
              <i class="bi bi-trash text-xl"></i>
            </div>
            <div>
              <h3 class="text-base font-bold text-ink">Confirmar Eliminación de Regla</h3>
              <p class="text-xs text-ink-muted">Esta regla será desactivada y removida del motor.</p>
            </div>
          </div>

          <div class="p-3 bg-slate-50 rounded-xl border border-border text-xs space-y-1">
            <div class="font-bold text-brand font-mono">{{ ruleToDelete.ruleKey }}</div>
            <div class="font-semibold text-ink">{{ ruleToDelete.consolidatorAreaName }}</div>
          </div>

          <div class="flex justify-end gap-3 pt-2 border-t border-border">
            <button type="button" (click)="isDeleteModalOpen = false" class="btn-outline">
              Cancelar
            </button>
            <button type="button" (click)="executeDeleteRule()" class="btn-primary !bg-rose-600 hover:!bg-rose-700 !border-rose-600 flex items-center gap-1.5">
              <i class="bi bi-trash"></i>
              Eliminar Regla
            </button>
          </div>
        </div>
      </div>
    }

    <!-- Preview Modal -->
    @if (isPreviewModalOpen) {
      <div class="fixed inset-0 z-50 bg-slate-900/50 backdrop-blur-sm flex items-center justify-center p-4">
        <div class="bg-white rounded-2xl max-w-2xl w-full p-6 shadow-2xl border border-border space-y-5 animate-in fade-in zoom-in-95 duration-100">
          <div class="flex items-center justify-between border-b border-border pb-3">
            <div>
              <h3 class="text-base font-bold text-ink">Vista Previa de Artículos Ruteados</h3>
              <span class="text-xs font-mono text-brand font-bold">{{ selectedRuleForPreview?.ruleKey }} - {{ selectedRuleForPreview?.consolidatorAreaName }}</span>
            </div>
            <button type="button" (click)="isPreviewModalOpen = false" class="text-ink-subtle hover:text-ink text-lg font-bold">
              ✕
            </button>
          </div>

          @if (isPreviewLoading) {
            <div class="p-8 text-center text-ink-muted">
              <span class="inline-block h-5 w-5 animate-spin rounded-full border-2 border-brand border-t-transparent mr-2"></span>
              Evaluando artículos coincidentes...
            </div>
          } @else if (previewResult) {
            <div class="space-y-3">
              <div class="flex items-center justify-between p-3 bg-brand-50 rounded-xl text-xs text-brand border border-brand-200">
                <div>Total Artículos Coincidentes en BD: <b class="text-brand text-sm">{{ previewResult.matchedProductsCount | number }}</b></div>
                <div class="text-[11px] text-ink-muted">Muestra de los primeros {{ previewResult.sampleMatchedProducts.length }} resultados</div>
              </div>

              <div class="max-h-80 overflow-y-auto space-y-2 pr-1">
                @for (item of previewResult.sampleMatchedProducts; track item.id) {
                  <div class="p-3 bg-surface-subtle rounded-xl border border-border text-xs flex justify-between items-center hover:bg-slate-100 transition-colors">
                    <div>
                      <div class="font-bold text-ink">{{ item.name }}</div>
                      <div class="text-[11px] text-ink-muted font-mono mt-0.5">
                        <span class="text-brand font-semibold">{{ item.code }}</span> | UNSPSC: {{ item.unspscCode || 'N/A' }} | Obj: {{ item.expenseObjectCode || 'N/A' }}
                      </div>
                    </div>
                    <span class="px-2 py-0.5 rounded font-semibold text-[10px] bg-emerald-50 text-emerald-800 border border-emerald-200">
                      Coincide
                    </span>
                  </div>
                }
              </div>
            </div>
          }

          <div class="flex justify-end pt-3 border-t border-border">
            <button type="button" (click)="isPreviewModalOpen = false" class="btn-outline">
              Cerrar
            </button>
          </div>
        </div>
      </div>
    }
  `
})
export class ConfigReglasRuteoConsolidadorComponent implements OnInit {
  private routingService = inject(ConsolidatorRoutingService);
  private regulatoryService = inject(RegulatoryCatalogService);
  private cdr = inject(ChangeDetectorRef);

  rules: ConsolidatorRoutingRuleDto[] = [];
  consolidatorProfiles: ConsolidatorAreaProfileDto[] = [];
  isLoading = false;
  isReevaluating = false;

  filterQuery = '';
  filterStatus = 'ALL';

  // Create Modal
  isCreateModalOpen = false;
  newRuleKey = '';
  newPriority = 10;
  newTargetAreaId = '';
  newTargetAreaName = '';
  newUnspscPrefixes = '';
  newKeywords = '';
  newJustification = '';

  // Re-evaluate Confirmation Modal
  isReevaluateModalOpen = false;

  // Delete Rule Modal
  isDeleteModalOpen = false;
  ruleToDelete: ConsolidatorRoutingRuleDto | null = null;

  // Preview Modal
  isPreviewModalOpen = false;
  isPreviewLoading = false;
  selectedRuleForPreview: ConsolidatorRoutingRuleDto | null = null;
  previewResult: RulePreviewResultDto | null = null;

  ngOnInit(): void {
    this.loadConsolidatorProfiles();
    this.loadRules();
  }

  get filteredRules(): ConsolidatorRoutingRuleDto[] {
    return this.rules.filter(r => {
      const matchesQuery = !this.filterQuery ||
        r.ruleKey.toLowerCase().includes(this.filterQuery.toLowerCase()) ||
        r.consolidatorAreaName.toLowerCase().includes(this.filterQuery.toLowerCase()) ||
        (r.keywords && r.keywords.toLowerCase().includes(this.filterQuery.toLowerCase())) ||
        (r.unspscPrefixes && r.unspscPrefixes.includes(this.filterQuery));

      const matchesStatus = this.filterStatus === 'ALL' ||
        (this.filterStatus === 'ACTIVE' && r.isActive) ||
        (this.filterStatus === 'INACTIVE' && !r.isActive);

      return matchesQuery && matchesStatus;
    });
  }

  getList(str: string | null): string[] {
    if (!str) return [];
    return str.split('|').map(s => s.trim()).filter(s => s.length > 0);
  }

  loadRules(): void {
    this.isLoading = true;
    this.routingService.list().subscribe({
      next: res => {
        this.rules = res;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  loadConsolidatorProfiles(): void {
    this.regulatoryService.listConsolidatorAreaProfiles().subscribe({
      next: p => this.consolidatorProfiles = p,
      error: () => {}
    });
  }

  onAreaChange(): void {
    const profile = this.consolidatorProfiles.find(p => p.organizationalStructureId === this.newTargetAreaId);
    if (profile) this.newTargetAreaName = profile.areaName;
  }

  openCreateModal(): void {
    this.newRuleKey = '';
    this.newPriority = 10;
    this.newTargetAreaId = '';
    this.newTargetAreaName = '';
    this.newUnspscPrefixes = '';
    this.newKeywords = '';
    this.newJustification = '';
    this.isCreateModalOpen = true;
    this.cdr.detectChanges();
  }

  saveNewRule(): void {
    if (!this.newRuleKey || !this.newTargetAreaId || !this.newTargetAreaName) return;

    const trimmedKey = this.newRuleKey.trim().toUpperCase();

    // Prevent duplicate rule key
    if (this.rules.some(r => r.ruleKey.toUpperCase() === trimmedKey)) {
      alert(`Ya existe una regla de ruteo con la clave '${trimmedKey}'. Especifica una clave única.`);
      return;
    }

    this.routingService
      .create({
        ruleKey: trimmedKey,
        priority: this.newPriority,
        consolidatorAreaId: this.newTargetAreaId,
        consolidatorAreaName: this.newTargetAreaName,
        unspscPrefixes: this.newUnspscPrefixes.trim() || undefined,
        keywords: this.newKeywords.trim() || undefined,
        justification: this.newJustification.trim() || undefined,
        institutionallyValidated: true,
        baseConfidence: 85
      })
      .subscribe({
        next: () => {
          this.isCreateModalOpen = false;
          this.loadRules();
        },
        error: () => {
          this.isCreateModalOpen = false;
          this.loadRules();
        }
      });
  }

  toggleActive(rule: ConsolidatorRoutingRuleDto): void {
    this.routingService.setActive(rule.id, !rule.isActive).subscribe({
      next: () => this.loadRules(),
      error: () => {
        rule.isActive = !rule.isActive;
        this.cdr.detectChanges();
      }
    });
  }

  confirmDeleteRule(rule: ConsolidatorRoutingRuleDto): void {
    this.ruleToDelete = rule;
    this.isDeleteModalOpen = true;
    this.cdr.detectChanges();
  }

  executeDeleteRule(): void {
    if (!this.ruleToDelete) return;
    this.routingService.deleteRule(this.ruleToDelete.id).subscribe({
      next: () => {
        this.isDeleteModalOpen = false;
        this.ruleToDelete = null;
        this.loadRules();
      },
      error: () => {
        this.isDeleteModalOpen = false;
        this.ruleToDelete = null;
        this.loadRules();
      }
    });
  }

  previewRule(rule: ConsolidatorRoutingRuleDto): void {
    this.selectedRuleForPreview = rule;
    this.isPreviewLoading = true;
    this.previewResult = null;
    this.isPreviewModalOpen = true;
    this.cdr.detectChanges();

    this.routingService.preview(rule.id, 20).subscribe({
      next: res => {
        this.previewResult = res;
        this.isPreviewLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isPreviewLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  openReevaluateModal(): void {
    this.isReevaluateModalOpen = true;
    this.cdr.detectChanges();
  }

  executeReevaluate(): void {
    this.isReevaluateModalOpen = false;
    this.isReevaluating = true;
    this.cdr.detectChanges();

    this.routingService.reevaluate(true).subscribe({
      next: res => {
        alert(`Re-evaluación completada exitosamente: ${res.updatedCount} artículos ruteados.`);
        this.isReevaluating = false;
        this.cdr.detectChanges();
      },
      error: () => {
        alert('Re-evaluación ejecutada.');
        this.isReevaluating = false;
        this.cdr.detectChanges();
      }
    });
  }
}
