import { Component, OnInit, inject, ChangeDetectorRef, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PageShellComponent } from '@public-intelligence/ui-core';
import { MasterProductService, PagedResponse } from '../../../../services/planning/master-product.service';
import { RegulatoryCatalogService } from '../../../../services/planning/regulatory-catalog.service';
import { MasterDataImportService } from '../../../../services/planning/master-data-import.service';
import {
  MasterProductDto,
  MasterProductDetailDto,
  MasterProductMappingSummaryDto,
  MasterProductState,
  ExpenseObjectDto,
  AccountingAccountDto,
  ConsolidatorAreaProfileDto,
  MasterDataImportResultDto
} from '../../data/planning-api.models';

@Component({
  selector: 'app-maestra-articulos',
  standalone: true,
  imports: [CommonModule, FormsModule, PageShellComponent],
  template: `
    <app-page-shell
      title="Maestra Institucional de Productos y Servicios"
      description="Catálogo maestro de bienes, servicios y obras con mapeos regulatorios (UNSPSC, Objeto del Gasto, Plan de Cuentas, Área Consolidadora)"
    >
      <div class="space-y-6">
        <!-- Top Action Bar & Modals Triggers -->
        <div class="card p-4 flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div class="flex items-center gap-2">
            <span class="p-2 rounded-lg bg-brand-50 text-brand">
              <i class="bi bi-box-seam text-lg"></i>
            </span>
            <span class="text-xs font-medium text-ink-muted">
              Gobernanza centralizada de artículos e integración con clasificadores regulatorios
            </span>
          </div>

          <div class="flex items-center gap-3">
            <button
              type="button"
              (click)="openImportModal()"
              class="btn-primary flex items-center gap-2"
            >
              <i class="bi bi-file-earmark-excel"></i>
              Importar Maestra Excel
            </button>
            <button
              type="button"
              (click)="openBulkRatifyModal()"
              class="btn-secondary flex items-center gap-2"
            >
              <i class="bi bi-diagram-3"></i>
              Ruteo Masivo por UNSPSC
            </button>
          </div>
        </div>

        <!-- Mapping Summary Metric Cards -->
        @if (summary) {
          <div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-4">
            <div class="card p-4">
              <div class="text-xs text-ink-muted font-medium">Total Artículos</div>
              <div class="text-2xl font-bold text-ink mt-1">{{ summary.totalProducts | number }}</div>
            </div>

            <div class="card p-4">
              <div class="text-xs text-ink-muted font-medium">Utilizables en el POA</div>
              <div class="text-2xl font-bold text-emerald-600 mt-1">
                {{ summary.activeProducts | number }}
              </div>
            </div>

            <div class="card p-4">
              <div class="text-xs text-ink-muted font-medium">Sin Obj. Gasto</div>
              <div class="text-2xl font-bold mt-1" [ngClass]="summary.missingExpenseObject > 0 ? 'text-amber-600' : 'text-ink'">
                {{ summary.missingExpenseObject | number }}
              </div>
            </div>

            <div class="card p-4">
              <div class="text-xs text-ink-muted font-medium">Sin Cta. Contable</div>
              <div class="text-2xl font-bold mt-1" [ngClass]="summary.missingAccountingAccount > 0 ? 'text-amber-600' : 'text-ink'">
                {{ summary.missingAccountingAccount | number }}
              </div>
            </div>

            <div class="card p-4">
              <div class="text-xs text-ink-muted font-medium">Sin Área Consolidadora</div>
              <div class="text-2xl font-bold mt-1" [ngClass]="summary.missingConsolidatorArea > 0 ? 'text-brand' : 'text-ink'">
                {{ summary.missingConsolidatorArea | number }}
              </div>
            </div>

            <div class="card p-4">
              <div class="text-xs text-ink-muted font-medium">En Revisión</div>
              <div class="text-2xl font-bold mt-1" [ngClass]="summary.requiresReviewCount > 0 ? 'text-rose-600' : 'text-ink'">
                {{ summary.requiresReviewCount | number }}
              </div>
            </div>
          </div>
        }

        <!-- Filters & Search Controls -->
        <div class="card p-4 space-y-4">
          <div class="grid grid-cols-1 md:grid-cols-4 gap-4">
            <!-- Search -->
            <div class="md:col-span-2 relative">
              <input
                type="text"
                [(ngModel)]="filterQuery"
                (ngModelChange)="onFilterChange()"
                placeholder="Buscar por código, nombre o alias..."
                class="field-control w-full"
              />
            </div>

            <!-- State Filter -->
            <div>
              <select
                [(ngModel)]="filterState"
                (change)="onFilterChange()"
                class="field-control w-full font-select"
              >
                <option value="">-- Todos los Estados --</option>
                <option value="Active">Activos (Utilizables)</option>
                <option value="Pending">Pendientes de definición</option>
                <option value="Requested">Solicitados por unidades</option>
                <option value="Disabled">Deshabilitados</option>
              </select>
            </div>

            <!-- Consolidator Area Filter -->
            <div>
              <select
                [(ngModel)]="filterConsolidatorAreaId"
                (change)="onFilterChange()"
                class="field-control w-full font-select"
              >
                <option value="">-- Área Consolidadora --</option>
                @for (area of consolidatorProfiles; track area.organizationalStructureId) {
                  <option [value]="area.organizationalStructureId">
                    {{ area.areaName }}
                  </option>
                }
              </select>
            </div>
          </div>

          <div class="flex flex-wrap items-center justify-between gap-4 pt-3 border-t border-border">
            <div class="flex items-center gap-6 text-sm text-ink-muted">
              <label class="flex items-center gap-2 cursor-pointer">
                <input
                  type="checkbox"
                  [(ngModel)]="filterMissingMapping"
                  (change)="onFilterChange()"
                  class="!size-4 accent-brand"
                />
                Faltan Mapeos Regulatorios
              </label>

              <label class="flex items-center gap-2 cursor-pointer">
                <input
                  type="checkbox"
                  [(ngModel)]="filterRequiresReview"
                  (change)="onFilterChange()"
                  class="!size-4 accent-brand"
                />
                Requieren Revisión de Ruteo
              </label>
            </div>

            <div class="flex items-center gap-2">
              <input
                type="text"
                [(ngModel)]="filterUnspscPrefix"
                (ngModelChange)="onFilterChange()"
                placeholder="Prefijo UNSPSC (ej: 4321)"
                class="field-control !w-44 !py-1 !text-xs"
              />
              <button
                type="button"
                (click)="resetFilters()"
                class="btn-outline btn-sm"
              >
                Limpiar Filtros
              </button>
            </div>
          </div>
        </div>

        <!-- Table Container -->
        <div class="card overflow-hidden">
          <div class="overflow-x-auto min-h-[380px]">
            <table class="w-full text-left border-collapse">
              <thead>
                <tr class="bg-surface-subtle text-xs font-semibold text-ink-muted border-b border-border">
                  <th class="py-3.5 px-4">Artículo</th>
                  <th class="py-3.5 px-4">UNSPSC</th>
                  <th class="py-3.5 px-4">Objeto del Gasto</th>
                  <th class="py-3.5 px-4">Cuenta Contable</th>
                  <th class="py-3.5 px-4">Área Consolidadora</th>
                  <th class="py-3.5 px-4">Precio Ref.</th>
                  <th class="py-3.5 px-4">Estado</th>
                  <th class="py-3.5 px-4 text-center w-16">Acciones</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-border text-sm">
                @if (isLoading) {
                  <tr class="text-center py-8">
                    <td colspan="8" class="py-8 text-ink-muted">
                      <div class="flex items-center justify-center gap-2">
                        <span class="inline-block h-4 w-4 animate-spin rounded-full border-2 border-brand border-t-transparent"></span>
                        Cargando catálogo maestro de productos...
                      </div>
                    </td>
                  </tr>
                } @else if (!pagedResult?.value || pagedResult?.value?.length === 0) {
                  <tr class="text-center">
                    <td colspan="8" class="py-8 text-ink-muted">
                      No se encontraron artículos con los filtros especificados.
                    </td>
                  </tr>
                } @else {
                  @for (item of pagedResult?.value; track item.id) {
                    <tr class="hover:bg-surface-subtle transition-colors">
                      <!-- Product Code & Name -->
                      <td class="py-3.5 px-4">
                        <div class="font-medium text-ink max-w-[280px]" [title]="item.name">{{ item.name }}</div>
                        <div class="flex items-center gap-2 mt-0.5">
                          <span class="text-xs font-mono text-brand font-semibold">{{ item.code }}</span>
                          @if (item.itemType) {
                            <span class="text-[10px] uppercase tracking-wider px-1.5 py-0.5 bg-surface-subtle border border-border text-ink-muted rounded font-semibold">
                              {{ item.itemType }}
                            </span>
                          }
                        </div>
                      </td>

                      <!-- UNSPSC Mapping -->
                      <td class="py-3.5 px-4">
                        @if (item.unspscCode) {
                          <div class="text-xs">
                            <span class="font-mono font-semibold text-ink">{{ item.unspscCode }}</span>
                            <div class="text-[11px] text-ink-muted truncate max-w-[170px]" [title]="item.unspscName">{{ item.unspscName }}</div>
                          </div>
                        } @else {
                          <span class="inline-flex items-center gap-1 text-xs text-amber-700 bg-amber-50 px-2 py-0.5 rounded font-medium border border-amber-200">
                            Sin UNSPSC
                          </span>
                        }
                      </td>

                      <!-- Expense Object -->
                      <td class="py-3.5 px-4">
                        @if (item.expenseObjectCode) {
                          <div class="text-xs">
                            <span class="font-mono font-semibold text-ink">{{ item.expenseObjectCode }}</span>
                            <div class="text-[11px] text-ink-muted truncate max-w-[170px]" [title]="item.expenseObjectName">{{ item.expenseObjectName }}</div>
                          </div>
                        } @else {
                          <span class="inline-flex items-center gap-1 text-xs text-amber-700 bg-amber-50 px-2 py-0.5 rounded font-medium border border-amber-200">
                            Sin Obj. Gasto
                          </span>
                        }
                      </td>

                      <!-- Accounting Account -->
                      <td class="py-3.5 px-4">
                        @if (item.accountingAccountCode) {
                          <div class="text-xs">
                            <span class="font-mono font-semibold text-ink">{{ item.accountingAccountCode }}</span>
                            <div class="text-[11px] text-ink-muted truncate max-w-[170px]" [title]="item.accountingAccountName">{{ item.accountingAccountName }}</div>
                            @if (item.mastersoftAccountCode) {
                              <div class="text-[10px] text-indigo-700 font-mono font-bold mt-0.5" [title]="'MasterSoft: ' + item.mastersoftAccountCode + ' - ' + (item.mastersoftAccountName || '')">
                                🔗 MS: {{ item.mastersoftAccountCode }}
                              </div>
                            }
                          </div>
                        } @else {
                          <span class="inline-flex items-center gap-1 text-xs text-ink-subtle">
                            Sin Cta. Contable
                          </span>
                        }
                      </td>

                      <!-- Consolidator Area -->
                      <td class="py-3.5 px-4">
                        @if (item.consolidatorAreaName) {
                          <div class="flex flex-col gap-1">
                            <span class="font-medium text-ink text-xs truncate max-w-[170px]" [title]="item.consolidatorAreaName">{{ item.consolidatorAreaName }}</span>
                            <div class="flex items-center gap-1">
                              <span
                                class="text-[10px] px-1.5 py-0.5 rounded font-semibold border"
                                [ngClass]="{
                                  'bg-brand-50 border-brand-200 text-brand': item.consolidatorAreaSource === 'Manual',
                                  'bg-sky-50 border-sky-200 text-sky-700': item.consolidatorAreaSource === 'Rule',
                                  'bg-slate-100 border-slate-200 text-slate-700': item.consolidatorAreaSource === 'Imported'
                                }"
                              >
                                {{ item.consolidatorAreaSource }}
                              </span>
                              @if (item.requiresReview) {
                                <span class="text-[10px] text-rose-600 font-semibold flex items-center gap-0.5">
                                  Revisión
                                </span>
                              }
                            </div>
                          </div>
                        } @else {
                          <span class="text-xs text-rose-600 font-semibold">
                            Sin Ruteo
                          </span>
                        }
                      </td>

                      <!-- Reference Price -->
                      <td class="py-3.5 px-4">
                        @if (item.referencePrice) {
                          <div class="text-xs font-semibold text-emerald-700 font-mono">
                            RD$ {{ item.referencePrice | number:'1.2-2' }}
                          </div>
                          @if (item.measureUnit) {
                            <div class="text-[10px] text-ink-muted">por {{ item.measureUnit }}</div>
                          }
                        } @else {
                          <span class="text-xs text-ink-subtle font-italic">Sin precio</span>
                        }
                      </td>

                      <!-- State -->
                      <td class="py-3.5 px-4">
                        <span
                          class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold"
                          [ngClass]="{
                            'bg-emerald-50 text-emerald-800 border border-emerald-200': item.state === 'Active',
                            'bg-amber-50 text-amber-800 border border-amber-200': item.state === 'Pending',
                            'bg-slate-100 text-slate-700 border border-slate-200': item.state === 'Disabled'
                          }"
                        >
                          <span class="w-1.5 h-1.5 rounded-full" [ngClass]="{
                            'bg-emerald-500': item.state === 'Active',
                            'bg-amber-500': item.state === 'Pending',
                            'bg-slate-400': item.state === 'Disabled'
                          }"></span>
                          {{ etiquetaEstado(item.state) }}
                        </span>
                      </td>

                      <!-- Kebab Menu Actions -->
                      <td class="py-3.5 px-4 text-center relative">
                        <button
                          type="button"
                          (click)="toggleMenu(item.id, $event)"
                          class="p-1.5 rounded-lg text-ink-muted hover:text-ink hover:bg-slate-100 transition-colors inline-flex items-center justify-center"
                          title="Opciones de Artículo"
                        >
                          <i class="bi bi-three-dots-vertical text-base"></i>
                        </button>

                        <!-- Kebab Popup Menu -->
                        @if (activeMenuProductId === item.id) {
                          <div
                            class="absolute right-4 top-10 z-30 w-48 bg-white rounded-xl shadow-xl border border-border py-1 text-left text-xs animate-in fade-in zoom-in-95 duration-100"
                          >
                            <button
                              type="button"
                              (click)="openEditMappingModal(item); closeMenu()"
                              class="w-full px-3 py-2 text-ink hover:bg-slate-50 flex items-center gap-2 transition-colors font-medium"
                            >
                              <i class="bi bi-pencil text-brand"></i>
                              Editar Artículo y Mapeo
                            </button>
                            <div class="my-1 border-t border-border"></div>
                            <button
                              type="button"
                              (click)="confirmDelete(item); closeMenu()"
                              class="w-full px-3 py-2 text-rose-600 hover:bg-rose-50 flex items-center gap-2 transition-colors font-medium"
                            >
                              <i class="bi bi-trash"></i>
                              Eliminar Artículo
                            </button>
                          </div>
                        }
                      </td>
                    </tr>
                  }
                }
              </tbody>
            </table>
          </div>

          <!-- Pagination Bar -->
          @if (pagedResult && pagedResult.totalPages > 1) {
            <div class="px-4 py-3 bg-surface-subtle border-t border-border flex items-center justify-between text-xs">
              <div class="text-ink-muted">
                Mostrando página <span class="font-semibold text-ink">{{ pagedResult.pageNumber }}</span> de <span class="font-semibold text-ink">{{ pagedResult.totalPages }}</span> ({{ pagedResult.totalRecords | number }} registros)
              </div>
              <div class="flex items-center gap-2">
                <button
                  type="button"
                  [disabled]="pagedResult.pageNumber <= 1"
                  (click)="changePage(pagedResult.pageNumber - 1)"
                  class="btn-outline btn-sm disabled:opacity-50"
                >
                  Anterior
                </button>
                <button
                  type="button"
                  [disabled]="pagedResult.pageNumber >= pagedResult.totalPages"
                  (click)="changePage(pagedResult.pageNumber + 1)"
                  class="btn-outline btn-sm disabled:opacity-50"
                >
                  Siguiente
                </button>
              </div>
            </div>
          }
        </div>
      </div>
    </app-page-shell>

    <!-- Side Interface (Drawer Panel for Editing) -->
    @if (isEditModalOpen) {
      <div class="fixed inset-0 z-50 overflow-hidden bg-slate-900/40 backdrop-blur-sm flex justify-end">
        <div
          class="w-full max-w-xl bg-white h-full shadow-2xl border-l border-border flex flex-col justify-between animate-in slide-in-from-right duration-200"
        >
          <!-- Side Header -->
          <div class="p-6 border-b border-border bg-surface-subtle flex items-start justify-between">
            <div>
              <div class="flex items-center gap-2">
                <span class="px-2 py-0.5 bg-brand-50 text-brand text-xs font-mono font-bold rounded">
                  {{ editingProductDetail?.code }}
                </span>
                <span class="text-xs text-ink-muted uppercase tracking-wider font-semibold">
                  {{ editingProductDetail?.itemType || 'Artículo' }}
                </span>
              </div>
              <h3 class="text-base font-bold text-ink mt-1.5 leading-snug">
                {{ editingProductDetail?.name }}
              </h3>
            </div>
            <button
              type="button"
              (click)="isEditModalOpen = false"
              class="p-2 rounded-lg text-ink-subtle hover:text-ink hover:bg-slate-200 transition-colors"
            >
              <i class="bi bi-x-lg text-lg"></i>
            </button>
          </div>

          <!-- Side Body (Scrollable) -->
          <div class="p-6 overflow-y-auto space-y-6 flex-1 text-sm">
            <!-- Information & Extraction Notes Box -->
            @if (editingNotes || editingProductDetail?.priceSource) {
              <div class="p-4 bg-slate-50 border border-slate-200 rounded-xl space-y-2">
                <div class="flex items-center gap-2 text-xs font-bold text-slate-800 uppercase tracking-wider">
                  <i class="bi bi-info-circle text-brand text-sm"></i>
                  Información de Extracción u Origen
                </div>
                @if (editingNotes) {
                  <p class="text-xs text-slate-700 leading-relaxed font-medium">
                    {{ editingNotes }}
                  </p>
                }
                @if (editingProductDetail?.priceSource) {
                  <div class="text-[11px] text-slate-500 font-mono">
                    Proceso de Referencia: <span class="font-bold text-slate-700">{{ editingProductDetail?.priceSource }}</span>
                  </div>
                }
              </div>
            }

            <!-- General Classification & Price -->
            <div class="space-y-4">
              <h4 class="text-xs font-bold uppercase tracking-wider text-ink-muted border-b border-border pb-1">
                Clasificación General y Precio
              </h4>

              <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label class="block text-xs font-semibold text-ink mb-1">Tipo de Bien / Servicio</label>
                  <select [(ngModel)]="editingItemType" class="field-control w-full font-select">
                    <option value="Goods">Goods (Bienes)</option>
                    <option value="Services">Services (Servicios)</option>
                    <option value="Works">Works (Obras)</option>
                  </select>
                </div>

                <div>
                  <label class="block text-xs font-semibold text-ink mb-1">Estado de Gobernanza</label>
                  <select [(ngModel)]="editingState" class="field-control w-full font-select">
                    <option value="Active">Utilizable (Activo)</option>
                    <option value="Pending">Pendiente de definición</option>
                    <option value="Disabled">Deshabilitado</option>
                  </select>
                </div>
              </div>

              <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label class="block text-xs font-semibold text-ink mb-1">Precio Referencial (RD$)</label>
                  <input
                    type="number"
                    [(ngModel)]="editingReferencePrice"
                    placeholder="0.00"
                    class="field-control w-full font-mono"
                  />
                </div>

                <div>
                  <label class="block text-xs font-semibold text-ink mb-1">Unidad de Medida</label>
                  <input
                    type="text"
                    [(ngModel)]="editingMeasureUnit"
                    placeholder="Ej. UD, KG, M2"
                    class="field-control w-full"
                  />
                </div>
              </div>
            </div>

            <!-- Regulatory Classifiers -->
            <div class="space-y-4">
              <h4 class="text-xs font-bold uppercase tracking-wider text-ink-muted border-b border-border pb-1">
                Clasificadores Regulatorios
              </h4>

              <!-- Expense Object -->
              <div>
                <label class="block text-xs font-semibold text-ink mb-1">Objeto del Gasto (DIGEPRES)</label>
                <select [(ngModel)]="editingExpenseObjectId" class="field-control w-full font-select">
                  <option [value]="null">-- Seleccionar Objeto del Gasto --</option>
                  @for (obj of expenseObjects; track obj.id) {
                    <option [value]="obj.id">
                      {{ obj.code }} - {{ obj.name }}
                    </option>
                  }
                </select>
              </div>

              <!-- Accounting Account -->
              <div>
                <label class="block text-xs font-semibold text-ink mb-1">Cuenta Contable (DIGECOB)</label>
                <select [(ngModel)]="editingAccountingAccountId" class="field-control w-full font-select">
                  <option [value]="null">-- Seleccionar Cuenta Contable --</option>
                  @for (acc of accountingAccounts; track acc.id) {
                    <option [value]="acc.id">
                      {{ acc.code }} - {{ acc.name }}
                    </option>
                  }
                </select>
              </div>
            </div>

            <!-- Routing & Consolidator Area -->
            <div class="space-y-4">
              <h4 class="text-xs font-bold uppercase tracking-wider text-ink-muted border-b border-border pb-1">
                Ruteo y Área Consolidadora
              </h4>

              <div>
                <label class="block text-xs font-semibold text-ink mb-1">Área Consolidadora Asignada</label>
                <select [(ngModel)]="editingConsolidatorAreaId" class="field-control w-full font-select">
                  <option [value]="null">-- Resolver por Reglas Automatizadas --</option>
                  @for (area of consolidatorProfiles; track area.organizationalStructureId) {
                    <option [value]="area.organizationalStructureId">
                      {{ area.areaName }}
                    </option>
                  }
                </select>
              </div>

              <!-- Explanation box for Ratify -->
              <div class="p-3 bg-brand-50/70 border border-brand-200 rounded-xl space-y-1.5">
                <label class="flex items-start gap-2 cursor-pointer">
                  <input type="checkbox" [(ngModel)]="editingRatifyArea" class="!size-4 accent-brand mt-0.5" />
                  <div>
                    <span class="text-xs font-bold text-brand">Ratificar asignación manual del Área Consolidadora</span>
                    <p class="text-[11px] text-ink-muted leading-tight mt-0.5">
                      Al activar esta casilla, la fuente se fija en <b>'Manual'</b>. Esto bloquea el ruteo y evita que el motor de reglas automáticas sobrescriba esta área en futuras ejecuciones.
                    </p>
                  </div>
                </label>
              </div>
            </div>
          </div>

          <!-- Side Footer (Sticky Bottom) -->
          <div class="p-4 border-t border-border bg-surface-subtle flex justify-end gap-3">
            <button type="button" (click)="isEditModalOpen = false" class="btn-outline">
              Cancelar
            </button>
            <button type="button" (click)="saveMapping()" class="btn-primary flex items-center gap-2">
              <i class="bi bi-check-circle"></i>
              Guardar Cambios
            </button>
          </div>
        </div>
      </div>
    }

    <!-- Delete Confirmation Modal -->
    @if (isDeleteModalOpen && itemToDelete) {
      <div class="fixed inset-0 z-50 bg-slate-900/50 backdrop-blur-sm flex items-center justify-center p-4">
        <div class="bg-white rounded-2xl max-w-md w-full p-6 shadow-2xl border border-border space-y-5 animate-in fade-in zoom-in-95 duration-100">
          <div class="flex items-center gap-3 text-rose-600">
            <div class="p-2.5 rounded-full bg-rose-50 border border-rose-200">
              <i class="bi bi-exclamation-triangle text-xl"></i>
            </div>
            <div>
              <h3 class="text-base font-bold text-ink">Confirmar Eliminación</h3>
              <p class="text-xs text-ink-muted">Esta acción moverá el artículo a estado eliminado.</p>
            </div>
          </div>

          <div class="p-3 bg-slate-50 rounded-xl border border-border text-xs text-ink space-y-1">
            <div class="font-bold text-ink">{{ itemToDelete.name }}</div>
            <div class="font-mono text-brand text-[11px]">{{ itemToDelete.code }}</div>
          </div>

          <div class="flex justify-end gap-3 pt-2 border-t border-border">
            <button type="button" (click)="isDeleteModalOpen = false" class="btn-outline">
              Cancelar
            </button>
            <button type="button" (click)="executeDelete()" class="btn-primary !bg-rose-600 hover:!bg-rose-700 !border-rose-600 flex items-center gap-1.5">
              <i class="bi bi-trash"></i>
              Eliminar Artículo
            </button>
          </div>
        </div>
      </div>
    }

    <!-- Bulk Ratify Modal -->
    @if (isBulkRatifyModalOpen) {
      <div class="fixed inset-0 z-50 bg-slate-900/50 backdrop-blur-sm flex items-center justify-center p-4">
        <div class="bg-white rounded-2xl max-w-md w-full p-6 shadow-2xl border border-border space-y-6">
          <div class="flex items-center justify-between border-b border-border pb-4">
            <h3 class="text-lg font-bold text-ink">Ruteo Masivo por UNSPSC</h3>
            <button type="button" (click)="isBulkRatifyModalOpen = false" class="text-ink-subtle hover:text-ink text-lg font-bold">
              ✕
            </button>
          </div>

          <p class="text-xs text-ink-muted">
            Asigna y ratifica masivamente un Área Consolidadora a todos los artículos cuyo código UNSPSC comience por el prefijo indicado (ej: 4321).
          </p>

          <div class="space-y-4 text-sm">
            <div>
              <label class="block text-xs font-semibold text-ink mb-1">Prefijo UNSPSC (Clase o Familia)</label>
              <input
                type="text"
                [(ngModel)]="bulkUnspscPrefix"
                placeholder="Ej. 4321 o 432119"
                class="field-control w-full font-mono"
              />
            </div>

            <div>
              <label class="block text-xs font-semibold text-ink mb-1">Área Consolidadora Destino</label>
              <select [(ngModel)]="bulkConsolidatorAreaId" class="field-control w-full font-select">
                <option value="">-- Seleccionar Área Consolidadora --</option>
                @for (area of consolidatorProfiles; track area.organizationalStructureId) {
                  <option [value]="area.organizationalStructureId">
                    {{ area.areaName }}
                  </option>
                }
              </select>
            </div>
          </div>

          <div class="flex justify-end gap-3 pt-4 border-t border-border">
            <button type="button" (click)="isBulkRatifyModalOpen = false" class="btn-outline">
              Cancelar
            </button>
            <button type="button" (click)="executeBulkRatify()" class="btn-primary">
              Ejecutar Ruteo Masivo
            </button>
          </div>
        </div>
      </div>
    }

    <!-- Import Excel Modal -->
    @if (isImportModalOpen) {
      <div class="fixed inset-0 z-50 bg-slate-900/50 backdrop-blur-sm flex items-center justify-center p-4">
        <div class="bg-white rounded-2xl max-w-lg w-full p-6 shadow-2xl border border-border space-y-6">
          <div class="flex items-center justify-between border-b border-border pb-4">
            <h3 class="text-lg font-bold text-ink">Importar Maestra Excel Institucional</h3>
            <button type="button" (click)="isImportModalOpen = false" class="text-ink-subtle hover:text-ink text-lg font-bold">
              ✕
            </button>
          </div>

          <div class="space-y-4 text-sm">
            <p class="text-xs text-ink-muted">
              Sube el archivo Excel oficial de la Maestra Institucional. Puedes seleccionar <b>Validación Dry-Run</b> para verificar inconsistencias sin guardar en la base de datos.
            </p>

            <div>
              <input type="file" (change)="onFileSelected($event)" accept=".xlsx, .xls" class="field-control w-full text-xs" />
            </div>

            <label class="flex items-center gap-2 cursor-pointer text-xs font-medium text-ink">
              <input type="checkbox" [(ngModel)]="importDryRun" class="!size-4 accent-brand" />
              Solo Validar (Dry-Run de Prueba)
            </label>

            <div class="flex gap-2">
              <button type="button" (click)="executeRepositoryImport()" [disabled]="isImporting" class="btn-outline w-full text-xs">
                Cargar Archivo Predeterminado del Servidor
              </button>
            </div>

            @if (importResult) {
              <div class="p-3 bg-surface-subtle rounded-lg border border-border text-xs space-y-2">
                <div class="font-bold" [ngClass]="importResult.errorMessages.length > 0 ? 'text-rose-600' : 'text-emerald-700'">
                  {{ importResult.isDryRun ? '[Modo Simulación] ' : '' }} Procesadas {{ importResult.processedRows }} filas.
                </div>
                <div>Nuevos Artículos: <b>{{ importResult.newProductsCount }}</b> | Actualizados: <b>{{ importResult.updatedProductsCount }}</b></div>
                @if (importResult.warningMessages.length > 0) {
                  <div class="text-amber-700">
                    <b>Advertencias:</b>
                    <ul class="list-disc pl-4 max-h-24 overflow-y-auto mt-1">
                      @for (w of importResult.warningMessages; track w) {
                        <li>{{ w }}</li>
                      }
                    </ul>
                  </div>
                }
              </div>
            }
          </div>

          <div class="flex justify-end gap-3 pt-4 border-t border-border">
            <button type="button" (click)="isImportModalOpen = false" class="btn-outline">
              Cerrar
            </button>
            <button type="button" (click)="executeFileImport()" [disabled]="!selectedFile || isImporting" class="btn-primary disabled:opacity-50">
              {{ isImporting ? 'Procesando...' : 'Importar Archivo' }}
            </button>
          </div>
        </div>
      </div>
    }
  `
})
export class ConfigMaestraArticulosComponent implements OnInit {
  /** Etiquetas en español de los estados que persiste el backend. */
  etiquetaEstado(estado: MasterProductState): string {
    return {
      Active: 'Utilizable',
      Pending: 'Pendiente de definición',
      Requested: 'Solicitado',
      Disabled: 'Deshabilitado',
    }[estado] || estado;
  }

  private masterProductService = inject(MasterProductService);
  private regulatoryService = inject(RegulatoryCatalogService);
  private importService = inject(MasterDataImportService);
  private cdr = inject(ChangeDetectorRef);

  summary: MasterProductMappingSummaryDto | null = null;
  pagedResult: PagedResponse<MasterProductDto> | null = null;
  consolidatorProfiles: ConsolidatorAreaProfileDto[] = [];
  expenseObjects: ExpenseObjectDto[] = [];
  accountingAccounts: AccountingAccountDto[] = [];

  isLoading = false;
  currentPage = 1;
  pageSize = 20;

  filterQuery = '';
  filterState = '';
  filterConsolidatorAreaId = '';
  filterMissingMapping = false;
  filterRequiresReview = false;
  filterUnspscPrefix = '';

  // Kebab Menu State
  activeMenuProductId: string | null = null;

  // Edit Drawer State
  isEditModalOpen = false;
  editingProductDetail: MasterProductDetailDto | null = null;
  editingItemType = 'Goods';
  editingExpenseObjectId: string | null = null;
  editingAccountingAccountId: string | null = null;
  editingConsolidatorAreaId: string | null = null;
  editingRatifyArea = false;
  editingState: MasterProductState = 'Active';
  editingReferencePrice: number | null = null;
  editingMeasureUnit = '';
  editingNotes = '';

  // Delete Modal State
  isDeleteModalOpen = false;
  itemToDelete: MasterProductDto | null = null;

  // Bulk Ratify Modal State
  isBulkRatifyModalOpen = false;
  bulkUnspscPrefix = '';
  bulkConsolidatorAreaId = '';

  // Import Modal State
  isImportModalOpen = false;
  selectedFile: File | null = null;
  importDryRun = false;
  isImporting = false;
  importResult: MasterDataImportResultDto | null = null;

  @HostListener('document:click')
  onDocumentClick(): void {
    this.activeMenuProductId = null;
  }

  ngOnInit(): void {
    this.loadProducts();
    this.loadSummary();
    this.loadConsolidatorProfiles();
  }

  toggleMenu(productId: string, event: Event): void {
    event.stopPropagation();
    this.activeMenuProductId = this.activeMenuProductId === productId ? null : productId;
  }

  closeMenu(): void {
    this.activeMenuProductId = null;
  }

  loadSummary(): void {
    this.masterProductService.getMappingSummary().subscribe({
      next: s => this.summary = s,
      error: () => {}
    });
  }

  loadProducts(): void {
    this.isLoading = true;
    this.masterProductService
      .list({
        q: this.filterQuery || undefined,
        state: this.filterState || undefined,
        consolidatorAreaId: this.filterConsolidatorAreaId || undefined,
        missingMapping: this.filterMissingMapping || undefined,
        requiresReview: this.filterRequiresReview || undefined,
        unspscPrefix: this.filterUnspscPrefix || undefined,
        page: this.currentPage,
        pageSize: this.pageSize
      })
      .subscribe({
        next: res => {
          this.pagedResult = res;
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
      error: () => {
        this.consolidatorProfiles = [
          { id: '1', areaCode: 'DTI', organicGroup: 'Sustantivo', areaName: 'Dirección de Tecnologías de la Información (DTI)', organizationalStructureId: 'area-dti', mandateSummary: 'Consolidación de Tecnología', consolidatorKind: 'Tecnología' },
          { id: '2', areaCode: 'DAF', organicGroup: 'Sustantivo', areaName: 'Dirección Administrativa y Financiera', organizationalStructureId: 'area-admin', mandateSummary: 'Consolidación Administrativa', consolidatorKind: 'Insumos' },
          { id: '3', areaCode: 'DSG', organicGroup: 'Sustantivo', areaName: 'Departamento de Servicios Generales', organizationalStructureId: 'area-serv', mandateSummary: 'Consolidación de Inmuebles', consolidatorKind: 'Servicios' },
          { id: '4', areaCode: 'DTR', organicGroup: 'Sustantivo', areaName: 'División de Transportes y Flotas', organizationalStructureId: 'area-trans', mandateSummary: 'Consolidación de Vehículos', consolidatorKind: 'Transporte' }
        ];
      }
    });
  }

  loadExpenseObjects(): void {
    this.regulatoryService.listExpenseObjects(undefined, true, 1, 500).subscribe({
      next: res => this.expenseObjects = res.value,
      error: () => {}
    });
  }

  loadAccountingAccounts(): void {
    this.regulatoryService.listAccountingAccounts(undefined, true, 1, 500).subscribe({
      next: res => this.accountingAccounts = res.value,
      error: () => {}
    });
  }

  onFilterChange(): void {
    this.currentPage = 1;
    this.loadProducts();
  }

  resetFilters(): void {
    this.filterQuery = '';
    this.filterState = '';
    this.filterConsolidatorAreaId = '';
    this.filterMissingMapping = false;
    this.filterRequiresReview = false;
    this.filterUnspscPrefix = '';
    this.currentPage = 1;
    this.loadProducts();
  }

  changePage(page: number): void {
    this.currentPage = page;
    this.loadProducts();
  }

  openEditMappingModal(item: MasterProductDto): void {
    if (this.expenseObjects.length === 0) this.loadExpenseObjects();
    if (this.accountingAccounts.length === 0) this.loadAccountingAccounts();

    this.masterProductService.getById(item.id).subscribe({
      next: detail => {
        this.editingProductDetail = detail;
        this.editingItemType = detail.itemType || 'Goods';
        this.editingExpenseObjectId = detail.expenseObjectId;
        this.editingAccountingAccountId = detail.accountingAccountId;
        this.editingConsolidatorAreaId = detail.consolidatorAreaId;
        this.editingRatifyArea = detail.consolidatorAreaSource === 'Manual';
        this.editingState = detail.state || 'Active';
        this.editingReferencePrice = detail.referencePrice ?? null;
        this.editingMeasureUnit = detail.measureUnit || '';
        this.editingNotes = detail.notes || '';
        this.isEditModalOpen = true;
      }
    });
  }

  saveMapping(): void {
    if (!this.editingProductDetail) return;
    this.masterProductService
      .updateMapping(this.editingProductDetail.id, {
        itemType: this.editingItemType,
        expenseObjectId: this.editingExpenseObjectId || undefined,
        accountingAccountId: this.editingAccountingAccountId || undefined,
        consolidatorAreaId: this.editingConsolidatorAreaId || undefined,
        ratifyConsolidatorArea: this.editingRatifyArea,
        state: this.editingState,
        referencePrice: this.editingReferencePrice ?? undefined,
        measureUnit: this.editingMeasureUnit || undefined,
        notes: this.editingNotes || undefined
      })
      .subscribe({
        next: () => {
          this.isEditModalOpen = false;
          this.loadSummary();
          this.loadProducts();
        },
        error: () => {
          this.isEditModalOpen = false;
          this.loadSummary();
          this.loadProducts();
        }
      });
  }

  confirmDelete(item: MasterProductDto): void {
    this.itemToDelete = item;
    this.isDeleteModalOpen = true;
  }

  executeDelete(): void {
    if (!this.itemToDelete) return;
    this.masterProductService.deleteProduct(this.itemToDelete.id).subscribe({
      next: () => {
        this.isDeleteModalOpen = false;
        this.itemToDelete = null;
        this.loadSummary();
        this.loadProducts();
      },
      error: () => {
        this.isDeleteModalOpen = false;
        this.itemToDelete = null;
        this.loadSummary();
        this.loadProducts();
      }
    });
  }

  openBulkRatifyModal(): void {
    this.bulkUnspscPrefix = '';
    this.bulkConsolidatorAreaId = '';
    this.isBulkRatifyModalOpen = true;
  }

  executeBulkRatify(): void {
    if (!this.bulkUnspscPrefix || !this.bulkConsolidatorAreaId) return;
    this.masterProductService
      .bulkRatify({
        unspscPrefix: this.bulkUnspscPrefix.trim(),
        consolidatorAreaId: this.bulkConsolidatorAreaId
      })
      .subscribe({
        next: () => {
          this.isBulkRatifyModalOpen = false;
          this.loadSummary();
          this.loadProducts();
        },
        error: () => {
          this.isBulkRatifyModalOpen = false;
          this.loadSummary();
          this.loadProducts();
        }
      });
  }

  openImportModal(): void {
    this.selectedFile = null;
    this.importDryRun = false;
    this.importResult = null;
    this.isImportModalOpen = true;
  }

  onFileSelected(event: any): void {
    const file = event.target.files[0];
    if (file) this.selectedFile = file;
  }

  executeFileImport(): void {
    if (!this.selectedFile) return;
    this.isImporting = true;
    this.importService.importFile(this.selectedFile, this.importDryRun).subscribe({
      next: res => {
        this.importResult = res;
        this.isImporting = false;
        this.loadSummary();
        this.loadProducts();
      },
      error: () => this.isImporting = false
    });
  }

  executeRepositoryImport(): void {
    this.isImporting = true;
    this.importService.importFromRepository(this.importDryRun).subscribe({
      next: res => {
        this.importResult = res;
        this.isImporting = false;
        this.loadSummary();
        this.loadProducts();
      },
      error: () => this.isImporting = false
    });
  }
}
