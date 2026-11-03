import { CommonModule } from '@angular/common';
import { Component, ElementRef, OnInit, ViewChild, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { PageShellComponent } from '@public-intelligence/ui-core';
import { BudgetCeilingService } from '../../../../services/planning/budget-ceiling.service';
import { RegulatoryCatalogService } from '../../../../services/planning/regulatory-catalog.service';
import { FundingSourceService } from '../../../../services/planning/funding-source.service';
import {
  AreaConsumptionReportDto,
  BudgetAdjustmentDto,
  BudgetCeilingDetailDto,
  BudgetCeilingDto,
  BudgetImpactPreviewDto,
  BudgetMonitoringDto,
  CreateBudgetAdjustmentDto,
  CreateBudgetCeilingDto,
  CrossExecutionReportDto,
  ExpenseObjectDto,
  FundingSourceDto,
} from '../../data/planning-api.models';
import { formatRD } from '../../data/planning.mock-data';

@Component({
  selector: 'app-config-techos',
  standalone: true,
  imports: [CommonModule, PageShellComponent, FormsModule, RouterLink],
  template: `
    <app-page-shell
      title="Techos presupuestarios"
      description="Configuración y gobernanza de techos presupuestarios por Auxiliar del Objeto del Gasto (DIGEPRES) · PACC / POA"
    >
      <!-- Selector de Secciones del Módulo (PRD Sección 4) -->
      <div class="segmented mb-4 flex flex-wrap gap-1">
        <button
          type="button"
          class="segmented-item"
          [class.segmented-item-active]="vista() === 'configuracion'"
          (click)="cambiarVista('configuracion')"
        >
          Configuración de techos
        </button>
        <button
          type="button"
          class="segmented-item"
          [class.segmented-item-active]="vista() === 'monitoreo'"
          (click)="cambiarVista('monitoreo')"
        >
          Monitoreo presupuestario
        </button>
        <button
          type="button"
          class="segmented-item"
          [class.segmented-item-active]="vista() === 'area'"
          (click)="cambiarVista('area')"
        >
          Consumo por área
        </button>
        <button
          type="button"
          class="segmented-item"
          [class.segmented-item-active]="vista() === 'cruzada'"
          (click)="cambiarVista('cruzada')"
        >
          Ejecución cruzada
        </button>
      </div>

      <!-- Alertas de notificación -->
      @if (mensajeExito()) {
        <p class="slide-in-top mb-3 rounded-lg bg-success-bg px-3 py-2 text-sm text-success-text ring-1 ring-success-border">
          {{ mensajeExito() }}
        </p>
      }
      @if (mensajeError()) {
        <p class="slide-in-top mb-3 rounded-lg bg-error-bg px-3 py-2 text-sm text-error-text ring-1 ring-error-border">
          {{ mensajeError() }}
        </p>
      }

      <!-- ════════════════════ SECCIÓN 1: CONFIGURACIÓN DE TECHOS (PRD Sección 5) ════════════════════ -->
      @if (vista() === 'configuracion') {
        <!-- Barra de Filtros y Acciones -->
        <div class="mb-4 flex flex-wrap items-center justify-between gap-3 rounded-lg border border-border bg-surface p-3 shadow-xs">
          <div class="flex flex-wrap items-center gap-3 flex-1 min-w-[300px]">
            <label class="flex items-center gap-1.5 text-xs font-semibold text-ink-muted">
              Año Fiscal:
              <select
                class="field-control !h-8 !py-1 text-xs"
                [ngModel]="filtroAnio()"
                (ngModelChange)="filtroAnio.set($event); cargarTechos()"
              >
                <option [value]="2025">2025</option>
                <option [value]="2026">2026</option>
                <option [value]="2027">2027</option>
              </select>
            </label>

            <label class="flex items-center gap-1.5 text-xs font-semibold text-ink-muted">
              Fuente:
              <select
                class="field-control !h-8 !py-1 text-xs"
                [ngModel]="filtroFuente()"
                (ngModelChange)="filtroFuente.set($event); cargarTechos()"
              >
                <option value="">Todas las fuentes</option>
                <option value="Fuente 10 - Recursos del Tesoro">Fuente 10 - Recursos del Tesoro</option>
                <option value="Fuente 20 - Crédito Externo">Fuente 20 - Crédito Externo</option>
                <option value="Fuente 30 - Ingresos Propios">Fuente 30 - Ingresos Propios</option>
              </select>
            </label>

            <label class="flex items-center gap-1.5 text-xs font-semibold text-ink-muted">
              Estado:
              <select
                class="field-control !h-8 !py-1 text-xs"
                [ngModel]="filtroEstado()"
                (ngModelChange)="filtroEstado.set($event); cargarTechos()"
              >
                <option value="">Todos los estados</option>
                <option value="Green">Green (Dentro del techo)</option>
                <option value="Yellow">Yellow (Cerca del techo)</option>
                <option value="Red">Red (Excedido)</option>
              </select>
            </label>

            <div class="relative flex-1 min-w-[200px]">
              <input
                type="text"
                class="field-control !h-8 w-full pl-8 text-xs"
                placeholder="Buscar por código auxiliar o descripción..."
                [ngModel]="filtroBusqueda()"
                (ngModelChange)="filtroBusqueda.set($event); cargarTechos()"
              />
              <span class="absolute left-2.5 top-2 text-ink-subtle">🔍</span>
            </div>
          </div>

          <button type="button" class="btn-primary btn-sm flex items-center gap-1.5" (click)="abrirModalCrear()">
            <span>➕</span> Configurar nuevo techo
          </button>
        </div>

        <!-- Tabla de Techos por Auxiliar del Objeto del Gasto -->
        <div class="card p-0 shadow-xs">
          <div class="custom-scrollbar overflow-x-auto">
            <table class="w-full min-w-[1180px] text-sm">
              <thead>
                <tr>
                  @for (h of ['Auxiliar (DIGEPRES)', 'Fuente / Programa', 'Vigente', 'Reservado', 'Planificado', 'Comprometido', 'Ejecutado', 'Disponible', 'Estado', 'Acciones']; track h) {
                    <th class="border-b border-border bg-surface-sunken px-3 py-2.5 text-left text-xs font-semibold tracking-wide whitespace-nowrap text-ink-muted uppercase">
                      {{ h }}
                    </th>
                  }
                </tr>
              </thead>
              <tbody>
                @for (t of techos(); track t.id) {
                  <tr class="hover:bg-surface-sunken/40 transition-colors" [class.bg-error-bg/30]="t.status === 'Red'">
                    <td class="border-b border-border px-3 py-2.5">
                      <div class="flex items-center gap-2">
                        <p class="font-semibold text-ink whitespace-nowrap">{{ t.expenseObjectCode }}</p>
                        <span class="inline-flex items-center rounded-sm bg-slate-100 px-1.5 py-0.5 text-[10px] font-bold text-slate-700 border border-slate-300">
                          Nivel {{ t.expenseObjectLevel || (t.expenseObjectCode.split('.').length) }}
                        </span>
                      </div>
                      <p class="!text-xs text-ink-muted line-clamp-1 max-w-[260px]" [title]="t.expenseObjectName">
                        {{ t.expenseObjectName }}
                      </p>
                    </td>
                    <td class="border-b border-border px-3 py-2.5">
                      <p class="text-xs text-ink whitespace-nowrap">{{ t.fundingSource }}</p>
                      <p class="!text-[11px] text-ink-subtle whitespace-nowrap">{{ t.budgetProgram || 'Sin programa' }}</p>
                    </td>
                    <td class="border-b border-border px-3 py-2.5 text-right font-semibold whitespace-nowrap tabular-nums text-ink">
                      {{ fmt(t.currentAmount) }}
                    </td>
                    <td class="border-b border-border px-3 py-2.5 text-right whitespace-nowrap tabular-nums text-warning-text">
                      {{ fmt(t.reservedAmount) }}
                    </td>
                    <td class="border-b border-border px-3 py-2.5 text-right whitespace-nowrap tabular-nums text-info-text">
                      {{ fmt(t.plannedAmount) }}
                    </td>
                    <td class="border-b border-border px-3 py-2.5 text-right whitespace-nowrap tabular-nums text-ink font-medium">
                      {{ fmt(t.committedAmount) }}
                    </td>
                    <td class="border-b border-border px-3 py-2.5 text-right whitespace-nowrap tabular-nums text-ink-muted">
                      {{ fmt(t.executedAmount) }}
                    </td>
                    <td
                      class="border-b border-border px-3 py-2.5 text-right font-bold whitespace-nowrap tabular-nums"
                      [class.text-error-text]="t.availableAmount < 0"
                      [class.text-success-text]="t.availableAmount >= 0"
                    >
                      {{ fmt(t.availableAmount) }}
                    </td>
                    <td class="border-b border-border px-3 py-2.5 whitespace-nowrap">
                      @if (t.status === 'Red') {
                        <span class="badge-error">Excedido</span>
                      } @else if (t.status === 'Yellow') {
                        <span class="badge-warning">Cerca del techo ({{ t.utilizationPercentage }}%)</span>
                      } @else {
                        <span class="badge-success">Dentro del techo</span>
                      }
                    </td>
                    <td class="border-b border-border px-3 py-2.5 text-right whitespace-nowrap">
                      <div class="flex items-center justify-end gap-1.5">
                        <button type="button" class="btn-ghost btn-sm" (click)="verDetalle(t.id)" title="Ver detalle y ciclo">
                          👁️ Detalle
                        </button>
                        <button type="button" class="btn-ghost btn-sm text-primary" (click)="abrirModalAjuste(t)" title="Registrar ajuste (+/-)">
                          ⚖️ Ajustar
                        </button>
                      </div>
                    </td>
                  </tr>
                } @empty {
                  <tr>
                    <td colspan="10" class="px-4 py-8 text-center text-sm text-ink-muted">
                      No hay techos presupuestarios configurados para los filtros seleccionados.
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </div>
      }

      <!-- ════════════════════ SECCIÓN 2: MONITOREO PRESUPUESTARIO (PRD Sección 8) ════════════════════ -->
      @if (vista() === 'monitoreo') {
        <div class="card p-0 shadow-xs">
          <div class="border-b border-border bg-surface-sunken px-4 py-3">
            <h3 class="font-semibold text-ink text-sm">Visión Financiera Global y Utilización de Techos</h3>
            <p class="text-xs text-ink-muted">Barra de porcentaje de consumo acumulado (Reservado + Planificado + Comprometido) frente al techo vigente.</p>
          </div>
          <div class="custom-scrollbar overflow-x-auto">
            <table class="w-full text-sm">
              <thead>
                <tr>
                  @for (h of ['Auxiliar', 'Fuente', 'Inicial', 'Ajustes', 'Vigente', 'Reservado', 'Planificado', 'Comprometido', 'Ejecutado', 'Disponible', 'Utilización']; track h) {
                    <th class="border-b border-border bg-surface-sunken px-3 py-2.5 text-left text-xs font-semibold tracking-wide whitespace-nowrap text-ink-muted uppercase">
                      {{ h }}
                    </th>
                  }
                </tr>
              </thead>
              <tbody>
                @for (m of reporteMonitoreo(); track m.id) {
                  <tr class="hover:bg-surface-sunken/40">
                    <td class="border-b border-border px-3 py-2.5 font-medium whitespace-nowrap">
                      {{ m.expenseObjectCode }} <span class="text-xs text-ink-muted">({{ m.expenseObjectName }})</span>
                    </td>
                    <td class="border-b border-border px-3 py-2.5 text-xs whitespace-nowrap text-ink-muted">{{ m.fundingSource }}</td>
                    <td class="border-b border-border px-3 py-2.5 text-right tabular-nums whitespace-nowrap">{{ fmt(m.initialAmount) }}</td>
                    <td class="border-b border-border px-3 py-2.5 text-right tabular-nums whitespace-nowrap text-ink-muted">
                      {{ m.totalAdjustments === 0 ? '—' : (m.totalAdjustments > 0 ? '+' : '') + fmt(m.totalAdjustments) }}
                    </td>
                    <td class="border-b border-border px-3 py-2.5 text-right font-semibold tabular-nums whitespace-nowrap">{{ fmt(m.currentAmount) }}</td>
                    <td class="border-b border-border px-3 py-2.5 text-right tabular-nums whitespace-nowrap text-warning-text">{{ fmt(m.reservedAmount) }}</td>
                    <td class="border-b border-border px-3 py-2.5 text-right tabular-nums whitespace-nowrap text-info-text">{{ fmt(m.plannedAmount) }}</td>
                    <td class="border-b border-border px-3 py-2.5 text-right tabular-nums whitespace-nowrap font-medium">{{ fmt(m.committedAmount) }}</td>
                    <td class="border-b border-border px-3 py-2.5 text-right tabular-nums whitespace-nowrap text-ink-muted">{{ fmt(m.executedAmount) }}</td>
                    <td class="border-b border-border px-3 py-2.5 text-right font-bold tabular-nums whitespace-nowrap" [class.text-error-text]="m.availableAmount < 0">
                      {{ fmt(m.availableAmount) }}
                    </td>
                    <td class="border-b border-border px-3 py-2.5 whitespace-nowrap min-w-[160px]">
                      <div class="flex items-center gap-2">
                        <div class="h-3.5 flex-1 rounded-full bg-surface-sunken overflow-hidden ring-1 ring-border">
                          <div
                            class="h-full rounded-full transition-all duration-500"
                            [style.width.%]="mathMin(m.utilizationPercentage, 100)"
                            [class.bg-success]="m.utilizationPercentage < 80"
                            [class.bg-warning]="m.utilizationPercentage >= 80 && m.utilizationPercentage <= 100"
                            [class.bg-error]="m.utilizationPercentage > 100"
                          ></div>
                        </div>
                        <span class="text-xs font-semibold tabular-nums text-ink">{{ m.utilizationPercentage }}%</span>
                      </div>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </div>
      }

      <!-- ════════════════════ SECCIÓN 3: CONSUMO POR ÁREA (PRD Sección 9) ════════════════════ -->
      @if (vista() === 'area') {
        <div class="mb-3 rounded-lg bg-info-bg px-3 py-2 text-sm text-info-text ring-1 ring-info-border">
          Los techos se gobiernan por Auxiliar del Objeto del Gasto. Esta vista presenta el consumo <strong>analítico</strong> por unidad organizacional.
        </div>
        <div class="card p-0 shadow-xs">
          <table class="w-full text-sm">
            <thead>
              <tr>
                @for (h of ['Área Organizacional', 'Requerimientos', 'Reservado', 'Planificado', 'Comprometido', 'Ejecutado', 'Consumo Total']; track h) {
                  <th class="border-b border-border bg-surface-sunken px-4 py-2.5 text-left text-xs font-semibold tracking-wide whitespace-nowrap text-ink-muted uppercase">
                    {{ h }}
                  </th>
                }
              </tr>
            </thead>
            <tbody>
              @for (a of reporteConsumoArea(); track a.areaId) {
                <tr class="hover:bg-surface-sunken/40">
                  <td class="border-b border-border px-4 py-2.5 font-medium whitespace-nowrap text-ink">{{ a.areaName }}</td>
                  <td class="border-b border-border px-4 py-2.5 text-right whitespace-nowrap tabular-nums text-ink-muted">{{ a.requirementCount }}</td>
                  <td class="border-b border-border px-4 py-2.5 text-right whitespace-nowrap tabular-nums text-warning-text">{{ fmt(a.reservedAmount) }}</td>
                  <td class="border-b border-border px-4 py-2.5 text-right whitespace-nowrap tabular-nums text-info-text">{{ fmt(a.plannedAmount) }}</td>
                  <td class="border-b border-border px-4 py-2.5 text-right whitespace-nowrap tabular-nums font-medium">{{ fmt(a.committedAmount) }}</td>
                  <td class="border-b border-border px-4 py-2.5 text-right whitespace-nowrap tabular-nums text-ink-muted">{{ fmt(a.executedAmount) }}</td>
                  <td class="border-b border-border px-4 py-2.5 text-right whitespace-nowrap tabular-nums font-bold text-ink">{{ fmt(a.totalConsumption) }}</td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }

      <!-- ════════════════════ SECCIÓN 4: EJECUCIÓN CRUZADA (PRD Sección 10) ════════════════════ -->
      @if (vista() === 'cruzada') {
        <div class="mb-3 rounded-lg bg-info-bg px-3 py-2 text-sm text-info-text ring-1 ring-info-border">
          Identifica la contratación y ejecución operativa realizada por <strong>áreas consolidadoras</strong> a cuenta de las áreas formuladoras.
        </div>
        <div class="flex flex-col gap-4">
          @for (grupo of reporteEjecucionCruzada(); track grupo.consolidatorAreaId + '-' + grupo.originAreaId) {
            <div class="card p-0 shadow-xs">
              <div class="flex items-center justify-between border-b border-border px-4 py-3 bg-surface-sunken">
                <div>
                  <h3 class="text-sm font-semibold text-ink">
                    Área Consolidadora: <span class="text-primary">{{ grupo.consolidatorAreaName }}</span>
                  </h3>
                  <p class="text-xs text-ink-muted">Ejecuta {{ grupo.requirementCount }} requerimiento(s) para: <strong>{{ grupo.originAreaName }}</strong></p>
                </div>
                <span class="text-sm font-bold tabular-nums text-ink">{{ fmt(grupo.totalAmount) }}</span>
              </div>
              <table class="w-full text-sm">
                <thead>
                  <tr>
                    @for (h of ['Código', 'Artículo', 'Cantidad', 'Monto Aprobado', 'Estado']; track h) {
                      <th class="border-b border-border px-4 py-2 text-left text-xs font-semibold text-ink-muted uppercase">{{ h }}</th>
                    }
                  </tr>
                </thead>
                <tbody>
                  @for (r of grupo.requirements; track r.id) {
                    <tr class="hover:bg-surface-sunken/30">
                      <td class="border-b border-border px-4 py-2 text-xs font-mono text-ink-muted">{{ r.code }}</td>
                      <td class="border-b border-border px-4 py-2 font-medium text-ink">{{ r.masterProductName }}</td>
                      <td class="border-b border-border px-4 py-2 text-right tabular-nums">{{ r.quantity }} {{ r.unitOfMeasure }}</td>
                      <td class="border-b border-border px-4 py-2 text-right tabular-nums font-semibold">{{ fmt(r.approvedAmount || (r.quantity * r.estimatedUnitPrice)) }}</td>
                      <td class="border-b border-border px-4 py-2 whitespace-nowrap">
                        <span class="badge-secondary">{{ r.status }}</span>
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          } @empty {
            <p class="text-sm text-ink-muted p-4 card">No se registraron requerimientos con ejecución cruzada entre áreas.</p>
          }
        </div>
      }

      <!-- ════════════════════ MODAL: CREAR TECHO PRESUPUESTARIO ════════════════════ -->
      @if (modalCrearAbierto()) {
        <div class="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4 backdrop-blur-xs">
          <div class="card max-w-xl w-full p-6 shadow-xl slide-in-top">
            <h2 class="text-lg font-bold text-ink mb-1">Configurar Nuevo Techo Presupuestario</h2>
            <p class="text-xs text-ink-muted mb-4">Asigne el límite presupuestario a cualquier nivel del Clasificador del Gasto (DIGEPRES).</p>

            <div class="flex flex-col gap-3">
              <!-- Selector del Objeto del Gasto por Nivel -->
              <div class="flex flex-col gap-2">
                <label class="field-label flex items-center justify-between">
                  <span>Clasificador del Gasto (DIGEPRES)</span>
                  <span class="text-[11px] text-ink-muted">
                    {{ auxiliaresFiltrados().length }} disponible(s)
                  </span>
                </label>

                <!-- Filtros Rápidos por Nivel Jerárquico -->
                <div class="flex flex-wrap items-center gap-1.5 bg-slate-50 p-1.5 rounded-lg border border-border">
                  <button
                    type="button"
                    class="px-2.5 py-1 text-[11px] font-semibold rounded-md transition-colors"
                    [class.bg-blue-600]="filtroNivelModal() === 0"
                    [class.text-white]="filtroNivelModal() === 0"
                    [class.bg-slate-200/60]="filtroNivelModal() !== 0"
                    [class.text-slate-700]="filtroNivelModal() !== 0"
                    [class.hover:bg-slate-200]="filtroNivelModal() !== 0"
                    (click)="filtroNivelModal.set(0)"
                  >
                    Todos
                  </button>
                  <button
                    type="button"
                    class="px-2 py-1 text-[11px] font-semibold rounded-md transition-colors"
                    [class.bg-blue-600]="filtroNivelModal() === 1"
                    [class.text-white]="filtroNivelModal() === 1"
                    [class.text-blue-700]="filtroNivelModal() !== 1"
                    [class.bg-blue-50]="filtroNivelModal() !== 1"
                    (click)="filtroNivelModal.set(1)"
                  >
                    Nivel 1 (Tipo)
                  </button>
                  <button
                    type="button"
                    class="px-2 py-1 text-[11px] font-semibold rounded-md transition-colors"
                    [class.bg-purple-600]="filtroNivelModal() === 2"
                    [class.text-white]="filtroNivelModal() === 2"
                    [class.text-purple-700]="filtroNivelModal() !== 2"
                    [class.bg-purple-50]="filtroNivelModal() !== 2"
                    (click)="filtroNivelModal.set(2)"
                  >
                    Nivel 2 (Objeto)
                  </button>
                  <button
                    type="button"
                    class="px-2 py-1 text-[11px] font-semibold rounded-md transition-colors"
                    [class.bg-amber-600]="filtroNivelModal() === 3"
                    [class.text-white]="filtroNivelModal() === 3"
                    [class.text-amber-700]="filtroNivelModal() !== 3"
                    [class.bg-amber-50]="filtroNivelModal() !== 3"
                    (click)="filtroNivelModal.set(3)"
                  >
                    Nivel 3 (Sub-objeto)
                  </button>
                  <button
                    type="button"
                    class="px-2 py-1 text-[11px] font-semibold rounded-md transition-colors"
                    [class.bg-emerald-600]="filtroNivelModal() === 4"
                    [class.text-white]="filtroNivelModal() === 4"
                    [class.text-emerald-700]="filtroNivelModal() !== 4"
                    [class.bg-emerald-50]="filtroNivelModal() !== 4"
                    (click)="filtroNivelModal.set(4)"
                  >
                    Nivel 4 (Auxiliar)
                  </button>
                </div>

                <!-- Búsqueda en vivo -->
                <div class="relative">
                  <input
                    type="text"
                    class="field-control !h-8 text-xs pl-8 pr-7 w-full"
                    placeholder="🔍 Buscar por código (ej. 2, 2.3, 2.3.1.1) o nombre..."
                    [ngModel]="busquedaAuxiliarModal()"
                    (ngModelChange)="busquedaAuxiliarModal.set($event)"
                  />
                  @if (busquedaAuxiliarModal()) {
                    <button
                      type="button"
                      class="absolute right-2 top-1.5 text-xs text-ink-muted hover:text-ink font-bold"
                      (click)="busquedaAuxiliarModal.set('')"
                    >
                      ✕
                    </button>
                  }
                </div>

                <!-- Lista Visual de Selección -->
                <div class="max-h-48 overflow-y-auto border border-border rounded-lg bg-surface divide-y divide-border custom-scrollbar">
                  @for (aux of auxiliaresFiltrados(); track aux.id) {
                    <div
                      class="p-2 flex items-center justify-between hover:bg-surface-sunken cursor-pointer transition-colors text-xs"
                      [class.bg-blue-50/90]="nuevoTecho.expenseObjectId === aux.id"
                      [class.border-l-4]="nuevoTecho.expenseObjectId === aux.id"
                      [class.border-l-blue-600]="nuevoTecho.expenseObjectId === aux.id"
                      (click)="nuevoTecho.expenseObjectId = aux.id"
                    >
                      <div class="flex items-center gap-2 overflow-hidden">
                        <span
                          class="inline-flex items-center rounded px-1.5 py-0.5 text-[10px] font-bold shrink-0 border"
                          [class.bg-blue-100]="getItemLevel(aux) === 1"
                          [class.text-blue-800]="getItemLevel(aux) === 1"
                          [class.border-blue-200]="getItemLevel(aux) === 1"
                          [class.bg-purple-100]="getItemLevel(aux) === 2"
                          [class.text-purple-800]="getItemLevel(aux) === 2"
                          [class.border-purple-200]="getItemLevel(aux) === 2"
                          [class.bg-amber-100]="getItemLevel(aux) === 3"
                          [class.text-amber-800]="getItemLevel(aux) === 3"
                          [class.border-amber-200]="getItemLevel(aux) === 3"
                          [class.bg-emerald-100]="getItemLevel(aux) >= 4"
                          [class.text-emerald-800]="getItemLevel(aux) >= 4"
                          [class.border-emerald-200]="getItemLevel(aux) >= 4"
                        >
                          N{{ getItemLevel(aux) }}
                        </span>
                        <span class="font-mono font-bold text-ink whitespace-nowrap">{{ aux.code }}</span>
                        <span class="text-ink truncate" [title]="aux.name">{{ aux.name }}</span>
                      </div>
                      @if (nuevoTecho.expenseObjectId === aux.id) {
                        <span class="text-blue-600 font-bold shrink-0 text-xs">✓ Seleccionado</span>
                      }
                    </div>
                  } @empty {
                    <div class="p-4 text-center text-xs text-ink-muted">
                      No se encontraron elementos para el nivel o término seleccionado.
                    </div>
                  }
                </div>
              </div>

              <!-- Banner Informativo del Objeto Seleccionado -->
              @if (objetoSeleccionadoModal(); as selObj) {
                <div class="rounded-md bg-blue-50 border border-blue-200 p-3 text-xs text-blue-900 flex items-start gap-2">
                  <span class="text-sm font-bold text-blue-700">ℹ️</span>
                  <div>
                    <strong class="font-bold">Bolsa Presupuestaria Seleccionada:</strong>
                    Configurando techo para <span class="font-mono font-bold">{{ selObj.code }}</span> ({{ selObj.name }} - Nivel {{ getItemLevel(selObj) }}).
                    <p class="mt-1 text-[11px] text-blue-800">
                      Todos los requerimientos y artículos bajo el código <b>{{ selObj.code }}</b> compartirán este mismo disponible.
                    </p>
                  </div>
                </div>
              }

              <label class="field-label">Fuente de Financiamiento
                <select class="field-control mt-1" [(ngModel)]="nuevoTecho.fundingSource">
                  <option value="">-- Seleccione Fuente de Financiamiento --</option>
                  @for (f of listaFuentes(); track f.id) {
                    <option [value]="f.code + ' - ' + f.name">{{ f.code }} - {{ f.name }}</option>
                  }
                </select>
              </label>

              <div class="grid grid-cols-2 gap-3">
                <label class="field-label">Año Fiscal
                  <input type="number" class="field-control mt-1" [(ngModel)]="nuevoTecho.fiscalYear" />
                </label>
                <label class="field-label">Techo Inicial (RD$)
                  <input type="number" class="field-control mt-1 text-right" [(ngModel)]="nuevoTecho.initialAmount" />
                </label>
              </div>

              <div class="grid grid-cols-2 gap-3">
                <label class="field-label">Umbral Advertencia (%)
                  <input type="number" class="field-control mt-1 text-right" [(ngModel)]="nuevoTecho.thresholdPercentage" />
                </label>
                <label class="field-label">Comportamiento Exceso
                  <select class="field-control mt-1" [(ngModel)]="nuevoTecho.validationBehavior">
                    <option value="Block">Bloquear (Block)</option>
                    <option value="Warning">Advertir (Warning)</option>
                    <option value="AllowWithJustification">Permitir con Justificación</option>
                  </select>
                </label>
              </div>
            </div>

            <div class="mt-6 flex justify-end gap-2">
              <button type="button" class="btn-ghost" (click)="modalCrearAbierto.set(false)">Cancelar</button>
              <button type="button" class="btn-primary" [disabled]="!nuevoTecho.expenseObjectId || !nuevoTecho.initialAmount" (click)="guardarNuevoTecho()">
                Guardar Techo
              </button>
            </div>
          </div>
        </div>
      }


      <!-- ════════════════════ MODAL: REGISTRAR AJUSTE ════════════════════ -->
      @if (modalAjusteAbierto() && techoSeleccionado()) {
        <div class="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4 backdrop-blur-xs">
          <div class="card max-w-lg w-full p-6 shadow-xl slide-in-top">
            <h2 class="text-lg font-bold text-ink mb-1">Ajuste Presupuestario</h2>
            <p class="text-xs text-ink-muted mb-3">
              Auxiliar: <strong>{{ techoSeleccionado()?.expenseObjectCode }}</strong> - {{ techoSeleccionado()?.expenseObjectName }}
            </p>

            <div class="flex flex-col gap-3">
              <label class="field-label">Tipo de Ajuste
                <select class="field-control mt-1" [(ngModel)]="nuevoAjuste.adjustmentType">
                  <option value="Increase">➕ Incremento (+)</option>
                  <option value="Reduction">➖ Reducción (-)</option>
                </select>
              </label>

              <label class="field-label">Monto del Ajuste (RD$)
                <input type="number" class="field-control mt-1 text-right font-semibold" [(ngModel)]="nuevoAjuste.amount" />
              </label>

              <label class="field-label">Motivo o Justificación del Ajuste
                <textarea class="field-control mt-1" rows="3" [(ngModel)]="nuevoAjuste.reason" placeholder="Ej.: Modificación presupuestaria aprobada por DIGEPRES según resolución..."></textarea>
              </label>
            </div>

            <div class="mt-6 flex justify-end gap-2">
              <button type="button" class="btn-ghost" (click)="modalAjusteAbierto.set(false)">Cancelar</button>
              <button type="button" class="btn-primary" [disabled]="!nuevoAjuste.amount || !nuevoAjuste.reason.trim()" (click)="verificarImpactoAjuste()">
                Aplicar Ajuste
              </button>
            </div>
          </div>
        </div>
      }

      <!-- ════════════════════ MODAL: PREVISTA DE IMPACTO (PRD Sección 12) ════════════════════ -->
      @if (modalPreviewAbierto() && previewImpacto()) {
        <div class="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4 backdrop-blur-xs">
          <div class="card max-w-xl w-full p-6 shadow-2xl slide-in-top border-l-4 border-warning">
            <h2 class="text-lg font-bold text-ink mb-1 flex items-center gap-2">
              <span>⚠️</span> Vista Previa del Impacto del Ajuste
            </h2>
            <p class="text-xs text-ink-muted mb-4">Esta modificación afectará los requerimientos formulados y el disponible presupuestario.</p>

            <div class="rounded-lg bg-surface-sunken p-3 mb-4 text-xs flex flex-col gap-1.5">
              <div class="flex justify-between">
                <span>Techo Actual:</span> <strong class="tabular-nums">{{ fmt(previewImpacto()!.currentCeiling) }}</strong>
              </div>
              <div class="flex justify-between">
                <span>Propuesto tras Reducción:</span> <strong class="tabular-nums text-primary">{{ fmt(previewImpacto()!.proposedCeiling) }}</strong>
              </div>
              <div class="flex justify-between border-t border-border pt-1 font-bold">
                <span>Nuevo Disponible Proyectado:</span>
                <span class="tabular-nums" [class.text-error-text]="previewImpacto()!.newAvailableBudget < 0">
                  {{ fmt(previewImpacto()!.newAvailableBudget) }}
                </span>
              </div>
            </div>

            @if (previewImpacto()!.totalExceededAmount > 0) {
              <div class="mb-4 rounded-lg bg-error-bg p-3 text-xs text-error-text font-semibold">
                ⚠️ ¡Atención! La reducción provocará un déficit presupuestario de {{ fmt(previewImpacto()!.totalExceededAmount) }} afectando {{ previewImpacto()!.affectedRequirementsCount }} requerimiento(s) en {{ previewImpacto()!.affectedAreasCount }} área(s).
              </div>
            }

            <div class="mt-6 flex justify-end gap-2">
              <button type="button" class="btn-ghost" (click)="modalPreviewAbierto.set(false)">Cancelar</button>
              <button type="button" class="btn-primary bg-warning text-ink" (click)="confirmarAjuste()">
                Confirmar y Aplicar Ajuste
              </button>
            </div>
          </div>
        </div>
      }

      <!-- ════════════════════ DRAWER: DETALLE DE TECHO (PRD Sección 6) ════════════════════ -->
      @if (drawerAbierto() && techoDetalle()) {
        <div class="fixed inset-0 z-40 bg-black/30 backdrop-blur-xs" (click)="drawerAbierto.set(false)"></div>
        <div class="fixed right-0 top-0 bottom-0 z-50 w-full max-w-xl bg-surface p-6 shadow-2xl overflow-y-auto flex flex-col justify-between">
          <div>
            <div class="flex items-center justify-between border-b border-border pb-3 mb-4">
              <div>
                <span class="text-xs font-mono text-primary uppercase tracking-wide">Detalle de Techo Presupuestario</span>
                <h2 class="text-lg font-bold text-ink">{{ techoDetalle()!.expenseObjectCode }}</h2>
                <p class="text-xs text-ink-muted">{{ techoDetalle()!.expenseObjectName }}</p>
              </div>
              <button type="button" class="btn-ghost btn-sm" (click)="drawerAbierto.set(false)">❌</button>
            </div>

            <!-- Flujo del Ciclo Presupuestario (PRD Sección 6) -->
            <div class="mb-6 rounded-lg bg-surface-sunken p-4 border border-border">
              <h3 class="text-xs font-bold uppercase tracking-wider text-ink-muted mb-3">Ciclo Presupuestario</h3>
              <div class="grid grid-cols-2 gap-3 text-xs">
                <div><span>Inicial:</span> <strong class="block tabular-nums text-sm">{{ fmt(techoDetalle()!.initialAmount) }}</strong></div>
                <div><span>Ajustes Acumulados:</span> <strong class="block tabular-nums text-sm text-primary">{{ fmt(techoDetalle()!.totalAdjustments) }}</strong></div>
                <div><span>Vigente:</span> <strong class="block tabular-nums text-sm font-bold">{{ fmt(techoDetalle()!.currentAmount) }}</strong></div>
                <div><span>Reservado (Nuevos/Revisión):</span> <strong class="block tabular-nums text-sm text-warning-text">{{ fmt(techoDetalle()!.reservedAmount) }}</strong></div>
                <div><span>Planificado (Aprobados):</span> <strong class="block tabular-nums text-sm text-info-text">{{ fmt(techoDetalle()!.plannedAmount) }}</strong></div>
                <div><span>Comprometido (Finanzas):</span> <strong class="block tabular-nums text-sm font-semibold">{{ fmt(techoDetalle()!.committedAmount) }}</strong></div>
                <div><span>Ejecutado (Avance):</span> <strong class="block tabular-nums text-sm text-ink-muted">{{ fmt(techoDetalle()!.executedAmount) }}</strong></div>
                <div><span>Disponible:</span> <strong class="block tabular-nums text-sm font-bold" [class.text-error-text]="techoDetalle()!.availableAmount < 0">{{ fmt(techoDetalle()!.availableAmount) }}</strong></div>
              </div>
            </div>

            <!-- Consumo por Producto -->
            <div class="mb-6">
              <h3 class="text-xs font-bold uppercase tracking-wider text-ink-muted mb-2">Consumo por Producto</h3>
              <table class="w-full text-xs">
                <thead>
                  <tr class="border-b border-border bg-surface-sunken">
                    <th class="px-2 py-1 text-left">Producto</th>
                    <th class="px-2 py-1 text-right">Monto</th>
                    <th class="px-2 py-1 text-right">% Techo</th>
                  </tr>
                </thead>
                <tbody>
                  @for (p of techoDetalle()!.consumptionByProduct; track p.masterProductId) {
                    <tr class="border-b border-border">
                      <td class="px-2 py-1.5 font-medium">{{ p.masterProductName }}</td>
                      <td class="px-2 py-1.5 text-right tabular-nums">{{ fmt(p.totalAmount) }}</td>
                      <td class="px-2 py-1.5 text-right tabular-nums font-semibold">{{ p.percentageOfCeiling }}%</td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>

            <!-- Consumo por Área Origen -->
            <div class="mb-6">
              <h3 class="text-xs font-bold uppercase tracking-wider text-ink-muted mb-2">Consumo por Área Origen</h3>
              <table class="w-full text-xs">
                <thead>
                  <tr class="border-b border-border bg-surface-sunken">
                    <th class="px-2 py-1 text-left">Área</th>
                    <th class="px-2 py-1 text-right">Requerimientos</th>
                    <th class="px-2 py-1 text-right">Monto</th>
                  </tr>
                </thead>
                <tbody>
                  @for (a of techoDetalle()!.consumptionByArea; track a.areaId) {
                    <tr class="border-b border-border">
                      <td class="px-2 py-1.5 font-medium">{{ a.areaName }}</td>
                      <td class="px-2 py-1.5 text-right tabular-nums text-ink-muted">{{ a.requirementCount }}</td>
                      <td class="px-2 py-1.5 text-right tabular-nums font-semibold">{{ fmt(a.totalAmount) }}</td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          </div>

          <div class="border-t border-border pt-3">
            <button type="button" class="btn-ghost w-full" (click)="drawerAbierto.set(false)">Cerrar Panel</button>
          </div>
        </div>
      }
    </app-page-shell>
  `,
})
export class ConfigTechosComponent implements OnInit {
  protected readonly catalogService = inject(RegulatoryCatalogService);
  protected readonly ceilingService = inject(BudgetCeilingService);
  protected readonly fundingService = inject(FundingSourceService);

  protected readonly fmt = formatRD;
  protected readonly mathMin = Math.min;

  // Estados de vista y filtros
  protected readonly vista = signal<'configuracion' | 'monitoreo' | 'area' | 'cruzada'>('configuracion');
  protected readonly filtroAnio = signal<number>(2026);
  protected readonly filtroFuente = signal<string>('');
  protected readonly filtroEstado = signal<string>('');
  protected readonly filtroBusqueda = signal<string>('');
  protected readonly busquedaAuxiliarModal = signal<string>('');
  protected readonly filtroNivelModal = signal<number>(0);

  // Notificaciones
  protected readonly mensajeExito = signal<string>('');
  protected readonly mensajeError = signal<string>('');

  // Datos
  protected readonly techos = signal<BudgetCeilingDto[]>([]);
  protected readonly reporteMonitoreo = signal<BudgetMonitoringDto[]>([]);
  protected readonly reporteConsumoArea = signal<AreaConsumptionReportDto[]>([]);
  protected readonly reporteEjecucionCruzada = signal<CrossExecutionReportDto[]>([]);
  protected readonly listaAuxiliares = signal<ExpenseObjectDto[]>([]);
  protected readonly listaFuentes = signal<FundingSourceDto[]>([]);

  // Derivación robusta del nivel por e.level o por segmentos de código (dot-notation)
  protected getItemLevel(a: ExpenseObjectDto): number {
    if (a.level && a.level >= 1 && a.level <= 4) {
      return a.level;
    }
    const parts = (a.code || '').split('.').filter((p) => p.trim().length > 0);
    return parts.length === 0 ? 4 : Math.min(parts.length, 4);
  }

  // Búsqueda en vivo y filtrado por nivel del Objeto del Gasto
  protected readonly auxiliaresFiltrados = computed(() => {
    let list = this.listaAuxiliares();
    const nivel = this.filtroNivelModal();
    if (nivel > 0) {
      list = list.filter((a) => this.getItemLevel(a) === nivel);
    }
    const term = this.busquedaAuxiliarModal().trim().toLowerCase();
    if (term) {
      list = list.filter(
        (a) => a.code.toLowerCase().includes(term) || a.name.toLowerCase().includes(term)
      );
    }
    return list;
  });

  protected readonly objetoSeleccionadoModal = computed(() => {
    const id = this.nuevoTecho.expenseObjectId;
    if (!id) return null;
    return this.listaAuxiliares().find((x) => x.id === id) || null;
  });

  // Modales y Drawer
  protected readonly modalCrearAbierto = signal<boolean>(false);
  protected readonly modalAjusteAbierto = signal<boolean>(false);
  protected readonly modalPreviewAbierto = signal<boolean>(false);
  protected readonly drawerAbierto = signal<boolean>(false);

  protected readonly techoSeleccionado = signal<BudgetCeilingDto | null>(null);
  protected readonly techoDetalle = signal<BudgetCeilingDetailDto | null>(null);
  protected readonly previewImpacto = signal<BudgetImpactPreviewDto | null>(null);

  // Formularios
  protected nuevoTecho: CreateBudgetCeilingDto = {
    fiscalYear: 2026,
    expenseObjectId: '',
    fundingSource: 'Fuente 10 - Recursos del Tesoro',
    initialAmount: 0,
    thresholdPercentage: 10,
    validationBehavior: 'Block',
  };

  protected nuevoAjuste: CreateBudgetAdjustmentDto = {
    adjustmentType: 'Increase',
    amount: 0,
    reason: '',
  };

  ngOnInit(): void {
    this.cargarTechos();
    this.cargarAuxiliares();
    this.cargarFuentes();
  }

  protected cambiarVista(v: 'configuracion' | 'monitoreo' | 'area' | 'cruzada'): void {
    this.vista.set(v);
    if (v === 'configuracion') this.cargarTechos();
    if (v === 'monitoreo') this.cargarMonitoreo();
    if (v === 'area') this.cargarConsumoArea();
    if (v === 'cruzada') this.cargarEjecucionCruzada();
  }

  protected cargarTechos(): void {
    this.ceilingService
      .list({
        fiscalYear: this.filtroAnio(),
        fundingSource: this.filtroFuente() || undefined,
        status: this.filtroEstado() || undefined,
        q: this.filtroBusqueda() || undefined,
        pageSize: 100,
      })
      .subscribe({
        next: (res) => this.techos.set(res.value || res.items || []),
        error: (err) => this.mostrarError('Error al cargar techos presupuestarios.'),
      });
  }

  protected cargarAuxiliares(): void {
    this.catalogService.listExpenseObjects(undefined, undefined, 1, 2000).subscribe({
      next: (objs: any) => {
        const items: any[] = objs?.value || objs?.items || (Array.isArray(objs) ? objs : []);
        this.listaAuxiliares.set(items);
      },
    });
  }

  protected cargarFuentes(): void {
    this.fundingService.list(undefined, true).subscribe({
      next: (fuentes) => this.listaFuentes.set(fuentes),
    });
  }

  protected cargarMonitoreo(): void {
    this.ceilingService.getMonitoring(this.filtroAnio(), this.filtroFuente() || undefined).subscribe({
      next: (data) => this.reporteMonitoreo.set(data),
    });
  }

  protected cargarConsumoArea(): void {
    this.ceilingService.getAreaConsumption(this.filtroAnio()).subscribe({
      next: (data) => this.reporteConsumoArea.set(data),
    });
  }

  protected cargarEjecucionCruzada(): void {
    this.ceilingService.getCrossExecution(this.filtroAnio()).subscribe({
      next: (data) => this.reporteEjecucionCruzada.set(data),
    });
  }

  protected abrirModalCrear(): void {
    this.busquedaAuxiliarModal.set('');
    this.nuevoTecho = {
      fiscalYear: this.filtroAnio(),
      expenseObjectId: '',
      fundingSource: '',
      initialAmount: 0,
      thresholdPercentage: 10,
      validationBehavior: 'Block',
    };
    this.modalCrearAbierto.set(true);
  }

  protected guardarNuevoTecho(): void {
    if (!this.nuevoTecho.expenseObjectId || !this.nuevoTecho.initialAmount) return;

    this.ceilingService.create(this.nuevoTecho).subscribe({
      next: () => {
        this.modalCrearAbierto.set(false);
        this.mostrarExito('Techo presupuestario configurado exitosamente.');
        this.cargarTechos();
      },
      error: (err) => this.mostrarError(err.error?.detail || 'Error al guardar el techo presupuestario.'),
    });
  }

  protected verDetalle(id: string): void {
    this.ceilingService.getById(id).subscribe({
      next: (detail) => {
        this.techoDetalle.set(detail);
        this.drawerAbierto.set(true);
      },
    });
  }

  protected abrirModalAjuste(t: BudgetCeilingDto): void {
    this.techoSeleccionado.set(t);
    this.nuevoAjuste = { adjustmentType: 'Increase', amount: 0, reason: '' };
    this.modalAjusteAbierto.set(true);
  }

  protected verificarImpactoAjuste(): void {
    const ceiling = this.techoSeleccionado();
    if (!ceiling) return;

    const monto = Number(this.nuevoAjuste.amount);
    if (!monto || monto <= 0) return;

    if (this.nuevoAjuste.adjustmentType === 'Reduction') {
      const propuesto = ceiling.currentAmount - monto;
      this.ceilingService.previewImpact(ceiling.id, propuesto).subscribe({
        next: (preview) => {
          this.previewImpacto.set(preview);
          this.modalAjusteAbierto.set(false);
          this.modalPreviewAbierto.set(true);
        },
      });
    } else {
      this.confirmarAjuste();
    }
  }

  protected confirmarAjuste(): void {
    const ceiling = this.techoSeleccionado();
    if (!ceiling) return;

    this.ceilingService.addAdjustment(ceiling.id, this.nuevoAjuste).subscribe({
      next: () => {
        this.modalAjusteAbierto.set(false);
        this.modalPreviewAbierto.set(false);
        this.mostrarExito('Ajuste presupuestario registrado y ciclo recalculado.');
        this.cargarTechos();
      },
      error: (err) => this.mostrarError(err.error?.detail || 'Error al aplicar el ajuste presupuestario.'),
    });
  }

  private mostrarExito(msg: string): void {
    this.mensajeExito.set(msg);
    this.mensajeError.set('');
    setTimeout(() => this.mensajeExito.set(''), 5000);
  }

  private mostrarError(msg: string): void {
    this.mensajeError.set(msg);
    this.mensajeExito.set('');
    setTimeout(() => this.mensajeError.set(''), 6000);
  }
}
