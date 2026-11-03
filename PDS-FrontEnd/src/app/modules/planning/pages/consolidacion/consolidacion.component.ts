import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { PageShellComponent } from '@public-intelligence/ui-core';
import { ConsolidationService } from '../../../../services/planning/consolidation.service';
import { RequirementStoreService } from '../../../../services/planning/requirement-store.service';
import { PlanningCycleStoreService } from '../../../../services/planning/planning-cycle-store.service';
import {
  ExpenseObjectsBoardPagedDto,
  ProductsBoardPagedDto,
  ConsolidatorAreasBoardPagedDto,
  ConsolidatorGroupDto,
  RequirementDto,
} from '../../data/planning-api.models';

@Component({
  selector: 'app-consolidacion',
  standalone: true,
  imports: [CommonModule, FormsModule, PageShellComponent, RouterLink, DecimalPipe],
  template: `
    <app-page-shell
      title="Consolidación Institucional y PACC"
      description="Tablero Institucional de Consolidación por Área, Objeto del Gasto (Techos Presupuestarios) y Demanda Agrupada"
    >
      <div class="space-y-6">
        <!-- View Toggle & Export Header -->
        <div class="card p-4 flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div class="segmented">
            <button
              type="button"
              (click)="cambiarTab('objeto')"
              class="segmented-item"
              [class.segmented-item-active]="activeTab() === 'objeto'"
            >
              <i class="bi bi-pie-chart text-xs"></i>
              Por Objeto del Gasto y Techos
            </button>
            <button
              type="button"
              (click)="cambiarTab('product')"
              class="segmented-item"
              [class.segmented-item-active]="activeTab() === 'product'"
            >
              <i class="bi bi-box-seam text-xs"></i>
              Demanda Agrupada por Producto
            </button>
            <button
              type="button"
              (click)="cambiarTab('area')"
              class="segmented-item"
              [class.segmented-item-active]="activeTab() === 'area'"
            >
              <i class="bi bi-diagram-3 text-xs"></i>
              Resumen por Área Consolidadora
            </button>
          </div>

          <div class="flex items-center gap-2">
            <button
              type="button"
              class="btn-primary btn-sm flex items-center gap-1.5 shadow-sm"
              [disabled]="store.descargando()"
              (click)="descargarPacc()"
            >
              <i class="bi bi-file-earmark-excel"></i>
              Exportar PACC Matriz 17 Columnas
            </button>
          </div>
        </div>

        <!-- TAB 1: Expense Object Groups & Techos Presupuestarios -->
        <div *ngIf="activeTab() === 'objeto'" class="space-y-6">
          <div *ngIf="loadingObjeto()" class="card p-12 text-center text-ink-muted">
            <span class="inline-block size-8 animate-spin rounded-full border-3 border-brand border-t-transparent mb-3"></span>
            <p class="font-medium text-ink">Cargando Agrupación por Objeto del Gasto y Techos Presupuestarios...</p>
          </div>

          <div *ngIf="!loadingObjeto() && expenseObjectsBoard() as board" class="space-y-6">
            <!-- Metrics Summary Cards -->
            <div class="grid grid-cols-2 md:grid-cols-4 gap-4">
              <div class="stat-card">
                <div class="flex items-center justify-between text-ink-subtle">
                  <span class="stat-label">Objetos del Gasto</span>
                  <i class="bi bi-folder2-open text-base"></i>
                </div>
                <div class="stat-value mt-1">{{ board.totalCount }}</div>
              </div>
              <div class="stat-card">
                <div class="flex items-center justify-between text-ink-subtle">
                  <span class="stat-label">Requerimientos Totales</span>
                  <i class="bi bi-list-check text-base"></i>
                </div>
                <div class="stat-value mt-1">{{ board.totalRequirements }}</div>
              </div>
              <div class="stat-card">
                <div class="flex items-center justify-between text-brand-500">
                  <span class="stat-label">Monto Total Solicitado</span>
                  <i class="bi bi-cash-stack text-base"></i>
                </div>
                <div class="stat-value text-brand mt-1">RD$ {{ board.totalAmount | number: '1.2-2' }}</div>
              </div>
              <div class="stat-card">
                <div class="flex items-center justify-between text-success-text">
                  <span class="stat-label">Monto Aprobado</span>
                  <i class="bi bi-check-circle text-base"></i>
                </div>
                <div class="stat-value text-success-text mt-1">RD$ {{ board.totalApprovedAmount | number: '1.2-2' }}</div>
              </div>
            </div>

            <!-- Search Filter Bar -->
            <div class="card p-4 flex items-center justify-between gap-4">
              <div class="relative flex-1 max-w-md">
                <input
                  type="text"
                  class="field-control pl-9 pr-8 text-sm"
                  placeholder="Buscar por código u objeto del gasto..."
                  [(ngModel)]="searchObjeto"
                  (keyup.enter)="buscarObjeto()"
                />
                <i class="bi bi-search absolute left-3 top-2.5 text-ink-subtle text-sm"></i>
              </div>
              <button type="button" class="btn-secondary btn-sm" (click)="buscarObjeto()">
                Buscar
              </button>
            </div>

            <!-- Expense Objects Group Cards -->
            <div class="space-y-4">
              @for (eg of board.expenseObjectGroups; track eg.expenseObjectCode) {
                <div class="card p-5 space-y-4">
                  <!-- Header Card with Techo Presupuestario Status -->
                  <div class="flex flex-col md:flex-row md:items-center justify-between gap-4 border-b border-border pb-4">
                    <div>
                      <div class="flex items-center gap-2">
                        <span class="px-2 py-0.5 rounded text-xs font-mono font-bold bg-brand-50 text-brand border border-brand-200">
                          {{ eg.expenseObjectCode }}
                        </span>
                        <h3 class="!font-poppins !text-base font-semibold text-ink-heading">
                          {{ eg.expenseObjectName }}
                        </h3>
                      </div>
                      <p class="mt-1 text-xs text-ink-muted">
                        {{ eg.productGroups.length }} artículo(s) · {{ eg.requirementCount }} requerimiento(s) de {{ eg.requestingAreaCount }} área(s)
                      </p>
                    </div>

                    <!-- Techo Presupuestario Health Indicator -->
                    <div class="bg-surface-sunken p-3 rounded-lg border border-border flex flex-col gap-1.5 min-w-[18rem]">
                      <div class="flex items-center justify-between text-xs font-semibold">
                        <span class="text-ink-muted">Techo Presupuestario:</span>
                        <span class="tabular-nums font-bold text-ink">
                          {{ eg.ceilingCurrentAmount ? ('RD$ ' + (eg.ceilingCurrentAmount | number: '1.2-2')) : 'Sin Techo Registrado' }}
                        </span>
                      </div>

                      @if (eg.ceilingCurrentAmount) {
                        <div class="w-full bg-slate-200 rounded-full h-2 overflow-hidden">
                          <div
                            class="h-2 rounded-full transition-all"
                            [ngClass]="eg.totalAmount > eg.ceilingCurrentAmount ? 'bg-error' : (eg.totalAmount / eg.ceilingCurrentAmount) > 0.85 ? 'bg-warning' : 'bg-success'"
                            [style.width.%]="calcPercentage(eg.totalAmount, eg.ceilingCurrentAmount)"
                          ></div>
                        </div>

                        <div class="flex items-center justify-between text-[11px]">
                          <span class="text-ink-subtle">Solicitado: RD$ {{ eg.totalAmount | number: '1.2-2' }}</span>
                          <span
                            class="font-semibold"
                            [ngClass]="eg.totalAmount > eg.ceilingCurrentAmount ? 'text-error-text' : 'text-success-text'"
                          >
                            {{ eg.totalAmount > eg.ceilingCurrentAmount ? 'Excede Techo' : 'Disponible: RD$ ' + ((eg.ceilingCurrentAmount - eg.totalAmount) | number: '1.2-2') }}
                          </span>
                        </div>
                      }
                    </div>
                  </div>

                  <!-- Articles under this Expense Object -->
                  <div class="overflow-x-auto">
                    <table class="w-full text-left border-collapse">
                      <thead>
                        <tr class="border-b border-border bg-surface-sunken text-xs font-semibold uppercase text-ink-muted">
                          <th class="py-2 px-3">Artículo Maestro</th>
                          <th class="py-2 px-3">Código / UNSPSC</th>
                          <th class="py-2 px-3 text-center">Cant. Solicitada</th>
                          <th class="py-2 px-3 text-center">Requerimientos</th>
                          <th class="py-2 px-3 text-right">Monto Estimado</th>
                          <th class="py-2 px-3 text-right">Monto Aprobado</th>
                          <th class="py-2 px-3 text-center">Acción</th>
                        </tr>
                      </thead>
                      <tbody class="divide-y divide-border text-xs">
                        @for (p of eg.productGroups; track p.masterProductCode) {
                          <tr class="hover:bg-surface-sunken/60 transition-colors">
                            <td class="py-2 px-3 font-medium text-ink">
                              {{ p.masterProductName }}
                            </td>
                            <td class="py-2 px-3 font-mono text-ink-muted">
                              {{ p.masterProductCode }} / UNSPSC: {{ p.unspscCode || 'N/A' }}
                            </td>
                            <td class="py-2 px-3 text-center font-bold text-ink">
                              {{ p.totalQuantity | number }}
                            </td>
                            <td class="py-2 px-3 text-center">
                              <span class="badge-neutral">
                                {{ p.requirementCount }}
                              </span>
                            </td>
                            <td class="py-2 px-3 text-right font-semibold text-ink">
                              RD$ {{ p.totalAmount | number: '1.2-2' }}
                            </td>
                            <td class="py-2 px-3 text-right font-bold text-success-text">
                              RD$ {{ p.totalApprovedAmount | number: '1.2-2' }}
                            </td>
                            <td class="py-2 px-3 text-center">
                              <a
                                routerLink="/requerimientos"
                                class="btn-secondary btn-sm !text-[11px] !no-underline inline-flex items-center gap-1"
                              >
                                <i class="bi bi-lightning-charge"></i> Gestionar
                              </a>
                            </td>
                          </tr>
                        }
                      </tbody>
                    </table>
                  </div>
                </div>
              } @empty {
                <div class="card p-8 text-center text-ink-muted">
                  No se encontraron Objetos del Gasto registrados para los criterios especificados.
                </div>
              }
            </div>

            <!-- Pagination Bar Tab 1 -->
            <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pt-4 border-t border-border">
              <div class="flex items-center gap-2 text-xs text-ink-muted">
                <span>Por página:</span>
                <select
                  class="field-control !h-8 !py-0 !px-2 !w-20 text-xs"
                  [ngModel]="pageSizeObjeto()"
                  (ngModelChange)="cambiarPageSizeObjeto($event)"
                >
                  <option [value]="20">20</option>
                  <option [value]="40">40</option>
                  <option [value]="80">80</option>
                </select>
                <span>Total: {{ board.totalCount }} objetos</span>
              </div>

              <div class="flex items-center gap-2">
                <button
                  type="button"
                  class="btn-secondary btn-sm"
                  [disabled]="pageObjeto() <= 1 || loadingObjeto()"
                  (click)="anteriorObjeto()"
                >
                  <i class="bi bi-chevron-left"></i> Anterior
                </button>
                <span class="text-xs font-semibold text-ink px-2">
                  Página {{ pageObjeto() }} de {{ board.totalPages || 1 }}
                </span>
                <button
                  type="button"
                  class="btn-secondary btn-sm"
                  [disabled]="pageObjeto() >= board.totalPages || loadingObjeto()"
                  (click)="siguienteObjeto()"
                >
                  Siguiente <i class="bi bi-chevron-right"></i>
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- TAB 2: Product Demand Board -->
        <div *ngIf="activeTab() === 'product'" class="space-y-6">
          <div *ngIf="loadingProduct()" class="card p-12 text-center text-ink-muted">
            <span class="inline-block size-8 animate-spin rounded-full border-3 border-brand border-t-transparent mb-3"></span>
            <p class="font-medium text-ink">Cargando Consolidación de Demanda Agrupada por Producto...</p>
          </div>

          <div *ngIf="!loadingProduct() && productsBoard() as board" class="space-y-6">
            <!-- Metrics Summary Cards -->
            <div class="grid grid-cols-2 md:grid-cols-4 gap-4">
              <div class="stat-card">
                <div class="flex items-center justify-between text-ink-subtle">
                  <span class="stat-label">Productos Registrados</span>
                  <i class="bi bi-box-seam text-base"></i>
                </div>
                <div class="stat-value mt-1">{{ board.totalCount }}</div>
              </div>
              <div class="stat-card">
                <div class="flex items-center justify-between text-ink-subtle">
                  <span class="stat-label">Requerimientos Totales</span>
                  <i class="bi bi-layers text-base"></i>
                </div>
                <div class="stat-value mt-1">{{ board.totalRequirements }}</div>
              </div>
              <div class="stat-card">
                <div class="flex items-center justify-between text-brand-500">
                  <span class="stat-label">Monto Total Solicitado</span>
                  <i class="bi bi-cash-stack text-base"></i>
                </div>
                <div class="stat-value text-brand mt-1">RD$ {{ board.totalAmount | number: '1.2-2' }}</div>
              </div>
              <div class="stat-card">
                <div class="flex items-center justify-between text-success-text">
                  <span class="stat-label">Monto Aprobado</span>
                  <i class="bi bi-check-circle text-base"></i>
                </div>
                <div class="stat-value text-success-text mt-1">RD$ {{ board.totalApprovedAmount | number: '1.2-2' }}</div>
              </div>
            </div>

            <!-- Search Filter Bar -->
            <div class="card p-4 flex items-center justify-between gap-4">
              <div class="relative flex-1 max-w-md">
                <input
                  type="text"
                  class="field-control pl-9 pr-8 text-sm"
                  placeholder="Buscar producto por nombre o código UNSPSC..."
                  [(ngModel)]="searchProduct"
                  (keyup.enter)="buscarProduct()"
                />
                <i class="bi bi-search absolute left-3 top-2.5 text-ink-subtle text-sm"></i>
              </div>
              <button type="button" class="btn-secondary btn-sm" (click)="buscarProduct()">
                Buscar
              </button>
            </div>

            <!-- Product Groups Table -->
            <div class="card overflow-hidden">
              <div class="overflow-x-auto">
                <table class="w-full text-left border-collapse">
                  <thead>
                    <tr class="border-b border-border bg-surface-sunken text-xs font-semibold uppercase text-ink-muted">
                      <th class="py-3 px-4">Artículo / Producto Maestro</th>
                      <th class="py-3 px-4">UNSPSC / Obj. Gasto</th>
                      <th class="py-3 px-4 text-center">Cant. Total</th>
                      <th class="py-3 px-4 text-center">Requerimientos</th>
                      <th class="py-3 px-4 text-center">Áreas Solicitantes</th>
                      <th class="py-3 px-4 text-right">Monto Estimado</th>
                      <th class="py-3 px-4 text-right">Monto Aprobado</th>
                      <th class="py-3 px-4 text-center">Acción</th>
                    </tr>
                  </thead>
                  <tbody class="divide-y divide-border text-sm">
                    <tr *ngFor="let g of board.productGroups" class="hover:bg-surface-sunken/60 transition-colors">
                      <td class="py-3 px-4 font-medium text-ink">
                        <div>{{ g.masterProductName }}</div>
                        <div class="text-xs font-mono text-brand">{{ g.masterProductCode }}</div>
                      </td>
                      <td class="py-3 px-4 text-xs font-mono text-ink-muted">
                        <div>UNSPSC: {{ g.unspscCode || 'N/A' }}</div>
                        <div class="flex items-center gap-1 mt-0.5">
                          <span>Obj: {{ g.expenseObjectCode || 'N/A' }}</span>
                          @if (g.ceilingCurrentAmount) {
                            <span
                              class="badge text-[10px] font-bold"
                              [ngClass]="g.totalAmount > g.ceilingCurrentAmount ? 'badge-error' : 'badge-success'"
                              title="Techo Presupuestario del Objeto"
                            >
                              Techo: RD$ {{ g.ceilingCurrentAmount | number: '1.0-0' }}
                            </span>
                          }
                        </div>
                      </td>
                      <td class="py-3 px-4 text-center font-bold text-ink">
                        {{ g.totalQuantity | number }}
                      </td>
                      <td class="py-3 px-4 text-center">
                        <span class="badge-neutral">
                          {{ g.requirementCount }}
                        </span>
                      </td>
                      <td class="py-3 px-4 text-center">
                        <span class="badge-info">
                          {{ g.requestingAreaCount }} áreas
                        </span>
                      </td>
                      <td class="py-3 px-4 text-right font-semibold text-ink">
                        RD$ {{ g.totalAmount | number: '1.2-2' }}
                      </td>
                      <td class="py-3 px-4 text-right font-bold text-success-text">
                        RD$ {{ g.totalApprovedAmount | number: '1.2-2' }}
                      </td>
                      <td class="py-3 px-4 text-center">
                        <a
                          routerLink="/requerimientos"
                          class="btn-secondary btn-sm !text-xs !no-underline inline-flex items-center gap-1"
                        >
                          <i class="bi bi-lightning-charge"></i> Gestionar
                        </a>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>

            <!-- Pagination Bar Tab 2 -->
            <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pt-4 border-t border-border">
              <div class="flex items-center gap-2 text-xs text-ink-muted">
                <span>Por página:</span>
                <select
                  class="field-control !h-8 !py-0 !px-2 !w-20 text-xs"
                  [ngModel]="pageSizeProduct()"
                  (ngModelChange)="cambiarPageSizeProduct($event)"
                >
                  <option [value]="20">20</option>
                  <option [value]="40">40</option>
                  <option [value]="80">80</option>
                </select>
                <span>Total: {{ board.totalCount }} productos</span>
              </div>

              <div class="flex items-center gap-2">
                <button
                  type="button"
                  class="btn-secondary btn-sm"
                  [disabled]="pageProduct() <= 1 || loadingProduct()"
                  (click)="anteriorProduct()"
                >
                  <i class="bi bi-chevron-left"></i> Anterior
                </button>
                <span class="text-xs font-semibold text-ink px-2">
                  Página {{ pageProduct() }} de {{ board.totalPages || 1 }}
                </span>
                <button
                  type="button"
                  class="btn-secondary btn-sm"
                  [disabled]="pageProduct() >= board.totalPages || loadingProduct()"
                  (click)="siguienteProduct()"
                >
                  Siguiente <i class="bi bi-chevron-right"></i>
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- TAB 3: Area Summary Table -->
        <div *ngIf="activeTab() === 'area'" class="space-y-6">
          <div *ngIf="loadingArea()" class="card p-12 text-center text-ink-muted">
            <span class="inline-block size-8 animate-spin rounded-full border-3 border-brand border-t-transparent mb-3"></span>
            <p class="font-medium text-ink">Cargando Resumen y Distribución por Área Consolidadora...</p>
          </div>

          <div *ngIf="!loadingArea() && consolidatorAreasBoard() as board" class="space-y-6">
            <!-- Metrics Summary Cards -->
            <div class="grid grid-cols-2 md:grid-cols-4 gap-4">
              <div class="stat-card">
                <div class="flex items-center justify-between text-ink-subtle">
                  <span class="stat-label">Áreas Consolidadoras</span>
                  <i class="bi bi-building text-base"></i>
                </div>
                <div class="stat-value mt-1">{{ board.totalCount }}</div>
              </div>
              <div class="stat-card">
                <div class="flex items-center justify-between text-ink-subtle">
                  <span class="stat-label">Requerimientos Ruteados</span>
                  <i class="bi bi-diagram-3 text-base"></i>
                </div>
                <div class="stat-value mt-1">{{ getBoardTotalRequirements(board) }}</div>
              </div>
              <div class="stat-card">
                <div class="flex items-center justify-between text-brand-500">
                  <span class="stat-label">Monto Total Estimado</span>
                  <i class="bi bi-cash-stack text-base"></i>
                </div>
                <div class="stat-value text-brand mt-1">RD$ {{ getBoardTotalAmount(board) | number: '1.2-2' }}</div>
              </div>
              <div class="stat-card">
                <div class="flex items-center justify-between text-success-text">
                  <span class="stat-label">Monto Total Aprobado</span>
                  <i class="bi bi-check-circle text-base"></i>
                </div>
                <div class="stat-value text-success-text mt-1">RD$ {{ board.totalApprovedAmount | number: '1.2-2' }}</div>
              </div>
            </div>

            <!-- Search Filter Bar -->
            <div class="card p-4 flex items-center justify-between gap-4">
              <div class="relative flex-1 max-w-md">
                <input
                  type="text"
                  class="field-control pl-9 pr-8 text-sm"
                  placeholder="Buscar área consolidadora..."
                  [(ngModel)]="searchArea"
                  (keyup.enter)="buscarArea()"
                />
                <i class="bi bi-search absolute left-3 top-2.5 text-ink-subtle text-sm"></i>
              </div>
              <button type="button" class="btn-secondary btn-sm" (click)="buscarArea()">
                Buscar
              </button>
            </div>

            <!-- Area Summary Table Card -->
            <div class="card overflow-hidden">
              <div class="p-4 border-b border-border flex flex-col md:flex-row md:items-center justify-between gap-3 bg-surface-raised">
                <div>
                  <h3 class="!font-poppins !text-base font-semibold text-ink-heading">Distribución por Área Consolidadora</h3>
                  <p class="text-xs text-ink-muted">Ciclo POA {{ board.cycleName }} — Desglose consolidado de la demanda por unidad responsable</p>
                </div>
                <span class="badge-info text-xs">
                  {{ board.consolidatorGroups.length }} Unidades Consolidadoras
                </span>
              </div>

              <div class="overflow-x-auto">
                <table class="w-full text-left border-collapse">
                  <thead>
                    <tr class="border-b border-border bg-surface-sunken text-xs font-semibold uppercase text-ink-muted">
                      <th class="py-3 px-4">Área Consolidadora</th>
                      <th class="py-3 px-4 text-center">Requerimientos</th>
                      <th class="py-3 px-4">Proporción de Demanda</th>
                      <th class="py-3 px-4 text-right">Monto Estimado</th>
                      <th class="py-3 px-4 text-right">Monto Aprobado</th>
                      <th class="py-3 px-4 text-center">Acción</th>
                    </tr>
                  </thead>
                  <tbody class="divide-y divide-border text-sm">
                    @for (g of board.consolidatorGroups; track g.consolidatorAreaId) {
                      <tr class="hover:bg-surface-sunken/60 transition-colors">
                        <td class="py-3 px-4 font-medium text-ink">
                          <div class="flex items-center gap-2.5">
                            <div class="size-8 rounded-md bg-brand-50 text-brand flex items-center justify-center shrink-0 text-sm">
                              <i class="bi bi-building"></i>
                            </div>
                            <div>
                              <div class="font-semibold text-ink-heading">{{ g.consolidatorAreaName }}</div>
                              <div class="text-xs text-ink-muted font-mono">ID: {{ g.consolidatorAreaId }}</div>
                            </div>
                          </div>
                        </td>
                        <td class="py-3 px-4 text-center">
                          <span class="badge-neutral">
                            <i class="bi bi-layers mr-1 text-[10px]"></i>
                            {{ g.requirementCount }} reqs.
                          </span>
                        </td>
                        <td class="py-3 px-4 min-w-[12rem]">
                          <div class="space-y-1">
                            <div class="flex items-center justify-between text-xs">
                              <span class="text-ink-muted font-medium">Proporción</span>
                              <span class="font-bold text-ink">{{ calcPercentage(g.totalAmount, getBoardTotalAmount(board)) }}%</span>
                            </div>
                            <div class="w-full bg-surface-sunken rounded-full h-2 overflow-hidden border border-border">
                              <div
                                class="h-2 rounded-full bg-brand transition-all"
                                [style.width.%]="calcPercentage(g.totalAmount, getBoardTotalAmount(board))"
                              ></div>
                            </div>
                          </div>
                        </td>
                        <td class="py-3 px-4 text-right font-semibold text-ink tabular-nums">
                          RD$ {{ g.totalAmount | number: '1.2-2' }}
                        </td>
                        <td class="py-3 px-4 text-right font-bold text-success-text tabular-nums">
                          RD$ {{ getAreaApprovedAmount(g) | number: '1.2-2' }}
                        </td>
                        <td class="py-3 px-4 text-center">
                          <a
                            routerLink="/requerimientos"
                            class="btn-secondary btn-sm !text-xs !no-underline inline-flex items-center gap-1"
                            title="Ver requerimientos asignados a esta área"
                          >
                            <i class="bi bi-box-arrow-up-right"></i> Ver en Bandeja
                          </a>
                        </td>
                      </tr>
                    } @empty {
                      <tr>
                        <td colspan="6" class="p-8 text-center text-sm text-ink-muted">
                          No se encontraron áreas consolidadoras registradas para los criterios especificados.
                        </td>
                      </tr>
                    }
                  </tbody>
                  <tfoot *ngIf="board.consolidatorGroups.length > 0" class="border-t-2 border-border bg-surface-sunken font-semibold text-xs text-ink">
                    <tr>
                      <td class="py-3 px-4 uppercase text-ink-muted font-bold">Total General</td>
                      <td class="py-3 px-4 text-center font-bold">
                        {{ getBoardTotalRequirements(board) }} reqs.
                      </td>
                      <td class="py-3 px-4">
                        <span class="font-bold text-brand">100% de Demanda</span>
                      </td>
                      <td class="py-3 px-4 text-right font-bold text-brand text-sm tabular-nums">
                        RD$ {{ getBoardTotalAmount(board) | number: '1.2-2' }}
                      </td>
                      <td class="py-3 px-4 text-right font-bold text-success-text text-sm tabular-nums">
                        RD$ {{ board.totalApprovedAmount | number: '1.2-2' }}
                      </td>
                      <td class="py-3 px-4 text-center"></td>
                    </tr>
                  </tfoot>
                </table>
              </div>
            </div>

            <!-- Pagination Bar Tab 3 -->
            <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pt-4 border-t border-border">
              <div class="flex items-center gap-2 text-xs text-ink-muted">
                <span>Por página:</span>
                <select
                  class="field-control !h-8 !py-0 !px-2 !w-20 text-xs"
                  [ngModel]="pageSizeArea()"
                  (ngModelChange)="cambiarPageSizeArea($event)"
                >
                  <option [value]="20">20</option>
                  <option [value]="40">40</option>
                  <option [value]="80">80</option>
                </select>
                <span>Total: {{ board.totalCount }} áreas</span>
              </div>

              <div class="flex items-center gap-2">
                <button
                  type="button"
                  class="btn-secondary btn-sm"
                  [disabled]="pageArea() <= 1 || loadingArea()"
                  (click)="anteriorArea()"
                >
                  <i class="bi bi-chevron-left"></i> Anterior
                </button>
                <span class="text-xs font-semibold text-ink px-2">
                  Página {{ pageArea() }} de {{ board.totalPages || 1 }}
                </span>
                <button
                  type="button"
                  class="btn-secondary btn-sm"
                  [disabled]="pageArea() >= board.totalPages || loadingArea()"
                  (click)="siguienteArea()"
                >
                  Siguiente <i class="bi bi-chevron-right"></i>
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </app-page-shell>
  `,
})
export class ConsolidacionComponent implements OnInit {
  protected readonly store = inject(RequirementStoreService);
  private readonly consolidationService = inject(ConsolidationService);
  private readonly cicloStore = inject(PlanningCycleStoreService);

  activeTab = signal<'objeto' | 'product' | 'area'>('objeto');

  // Tab 1 state
  expenseObjectsBoard = signal<ExpenseObjectsBoardPagedDto | null>(null);
  loadingObjeto = signal(false);
  pageObjeto = signal(1);
  pageSizeObjeto = signal(40);
  searchObjeto = '';

  // Tab 2 state
  productsBoard = signal<ProductsBoardPagedDto | null>(null);
  loadingProduct = signal(false);
  pageProduct = signal(1);
  pageSizeProduct = signal(40);
  searchProduct = '';

  // Tab 3 state
  consolidatorAreasBoard = signal<ConsolidatorAreasBoardPagedDto | null>(null);
  loadingArea = signal(false);
  pageArea = signal(1);
  pageSizeArea = signal(40);
  searchArea = '';

  async ngOnInit(): Promise<void> {
    await this.cicloStore.cargarCiclos();
    const cycleId = this.cicloStore.cicloActivoId();
    if (!cycleId) return;

    // Load only active tab on init
    this.cargarObjeto();
  }

  cambiarTab(tab: 'objeto' | 'product' | 'area'): void {
    this.activeTab.set(tab);

    if (tab === 'objeto' && !this.expenseObjectsBoard()) {
      this.cargarObjeto();
    } else if (tab === 'product' && !this.productsBoard()) {
      this.cargarProduct();
    } else if (tab === 'area' && !this.consolidatorAreasBoard()) {
      this.cargarArea();
    }
  }

  cargarObjeto(): void {
    const cycleId = this.cicloStore.cicloActivoId();
    if (!cycleId) return;

    this.loadingObjeto.set(true);
    this.consolidationService
      .getExpenseObjectsBoard(cycleId, this.pageObjeto(), this.pageSizeObjeto(), this.searchObjeto)
      .subscribe({
        next: (board) => {
          this.expenseObjectsBoard.set(board);
          this.loadingObjeto.set(false);
        },
        error: () => this.loadingObjeto.set(false),
      });
  }

  buscarObjeto(): void {
    this.pageObjeto.set(1);
    this.cargarObjeto();
  }

  cambiarPageSizeObjeto(size: number): void {
    this.pageSizeObjeto.set(Number(size));
    this.pageObjeto.set(1);
    this.cargarObjeto();
  }

  anteriorObjeto(): void {
    if (this.pageObjeto() > 1) {
      this.pageObjeto.update((p) => p - 1);
      this.cargarObjeto();
    }
  }

  siguienteObjeto(): void {
    const totalPages = this.expenseObjectsBoard()?.totalPages || 1;
    if (this.pageObjeto() < totalPages) {
      this.pageObjeto.update((p) => p + 1);
      this.cargarObjeto();
    }
  }

  cargarProduct(): void {
    const cycleId = this.cicloStore.cicloActivoId();
    if (!cycleId) return;

    this.loadingProduct.set(true);
    this.consolidationService
      .getProductsBoardPaged(cycleId, undefined, this.pageProduct(), this.pageSizeProduct(), this.searchProduct)
      .subscribe({
        next: (board) => {
          this.productsBoard.set(board);
          this.loadingProduct.set(false);
        },
        error: () => this.loadingProduct.set(false),
      });
  }

  buscarProduct(): void {
    this.pageProduct.set(1);
    this.cargarProduct();
  }

  cambiarPageSizeProduct(size: number): void {
    this.pageSizeProduct.set(Number(size));
    this.pageProduct.set(1);
    this.cargarProduct();
  }

  anteriorProduct(): void {
    if (this.pageProduct() > 1) {
      this.pageProduct.update((p) => p - 1);
      this.cargarProduct();
    }
  }

  siguienteProduct(): void {
    const totalPages = this.productsBoard()?.totalPages || 1;
    if (this.pageProduct() < totalPages) {
      this.pageProduct.update((p) => p + 1);
      this.cargarProduct();
    }
  }

  cargarArea(): void {
    const cycleId = this.cicloStore.cicloActivoId();
    if (!cycleId) return;

    this.loadingArea.set(true);
    this.consolidationService
      .getConsolidatorAreasBoard(cycleId, this.pageArea(), this.pageSizeArea(), this.searchArea)
      .subscribe({
        next: (board) => {
          this.consolidatorAreasBoard.set(board);
          this.loadingArea.set(false);
        },
        error: () => this.loadingArea.set(false),
      });
  }

  buscarArea(): void {
    this.pageArea.set(1);
    this.cargarArea();
  }

  cambiarPageSizeArea(size: number): void {
    this.pageSizeArea.set(Number(size));
    this.pageArea.set(1);
    this.cargarArea();
  }

  anteriorArea(): void {
    if (this.pageArea() > 1) {
      this.pageArea.update((p) => p - 1);
      this.cargarArea();
    }
  }

  siguienteArea(): void {
    const totalPages = this.consolidatorAreasBoard()?.totalPages || 1;
    if (this.pageArea() < totalPages) {
      this.pageArea.update((p) => p + 1);
      this.cargarArea();
    }
  }

  calcPercentage(amount: number, total: number | null | undefined): number {
    if (!total || total <= 0) return 0;
    return Math.min(100, Math.round((amount / total) * 100));
  }

  getBoardTotalAmount(board: ConsolidatorAreasBoardPagedDto): number {
    return board.consolidatorGroups?.reduce((sum, g) => sum + g.totalAmount, 0) ?? 0;
  }

  getBoardTotalRequirements(board: ConsolidatorAreasBoardPagedDto): number {
    return board.consolidatorGroups?.reduce((sum, g) => sum + g.requirementCount, 0) ?? 0;
  }

  getAreaApprovedAmount(group: ConsolidatorGroupDto): number {
    if (!group.requirements || group.requirements.length === 0) return 0;
    return group.requirements
      .filter((r: RequirementDto) => r.status === 'Approved' || r.status === 'Reduced')
      .reduce((sum: number, r: RequirementDto) => sum + (r.approvedAmount ?? r.calculatedTotal ?? r.quantity * r.estimatedUnitPrice), 0);
  }

  protected async descargarPacc(): Promise<void> {
    const cycleId = this.cicloStore.cicloActivoId();
    if (!cycleId) return;
    const anio = this.cicloStore.cicloActivo()?.year ?? '';
    await this.store.descargarPacc(cycleId, `PACC_${anio}.xlsx`);
  }
}
