import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DecimalPipe, NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PageShellComponent } from '@public-intelligence/ui-core';
import { RegulatoryCatalogService } from '../../../../services/planning/regulatory-catalog.service';
import { UnifiedCodeService } from '../../../../services/planning/unified-code.service';
import { FundingSourceService } from '../../../../services/planning/funding-source.service';
import {
  AccountingAccountDto,
  ExpenseObjectDto,
  FundingSourceDto,
  CreateFundingSourceDto,
  UpdateFundingSourceDto,
  UnifiedCodeDto,
  MastersoftAccountDto,
  MastersoftDigecobMappingDto,
  MastersoftImportSummaryDto,
} from '../../data/planning-api.models';

type Pestana = 'unspsc' | 'objetos' | 'cuentas' | 'fuentes' | 'mastersoft' | 'mastersoft-vinc';

export interface TreeNode {
  id: string;
  codigo: string;
  nombre: string;
  detalle?: string;
  nivel: string;
  nivelNum: number;
  marca: boolean;
  parentId: string | null;
  childCount: number;
  isExpanded: boolean;
  isLoadingChildren: boolean;
  childrenLoaded: boolean;
  children: TreeNode[];
  matchedInSearch?: boolean;
  rawItem: UnifiedCodeDto | ExpenseObjectDto | AccountingAccountDto;
}

@Component({
  selector: 'app-config-catalogos-regulatorios',
  standalone: true,
  imports: [FormsModule, PageShellComponent, DecimalPipe, NgClass],
  styles: [
    `
      :host button {
        background-color: transparent;
        color: inherit;
        text-transform: none;
        padding: initial;
        border-radius: initial;
        font-weight: inherit;
        box-shadow: none;
      }
      :host .btn-custom-primary {
        background-color: #003876 !important;
        color: #ffffff !important;
        border-radius: 0.5rem !important;
        padding: 0.5rem 1rem !important;
        font-weight: 600 !important;
        text-transform: none !important;
        font-size: 0.875rem !important;
        display: inline-flex !important;
        align-items: center !important;
        gap: 0.5rem !important;
        box-shadow: 0 1px 2px 0 rgba(0, 0, 0, 0.05) !important;
      }
      :host .btn-custom-primary:hover {
        background-color: #1a579a !important;
      }
      :host .btn-custom-outline {
        background-color: #ffffff !important;
        color: #334155 !important;
        border: 1px solid #cbd5e1 !important;
        border-radius: 0.5rem !important;
        padding: 0.375rem 0.75rem !important;
        font-size: 0.75rem !important;
        font-weight: 500 !important;
        text-transform: none !important;
        display: inline-flex !important;
        align-items: center !important;
        gap: 0.375rem !important;
      }
      :host .btn-custom-outline:hover {
        background-color: #f1f5f9 !important;
        color: #0f172a !important;
      }
      :host .kebab-btn {
        background-color: transparent !important;
        color: #64748b !important;
        border: 0 !important;
        border-radius: 0.375rem !important;
        width: 2rem !important;
        height: 2rem !important;
        line-height: 2rem !important;
        padding: 0 !important;
        display: inline-flex !important;
        align-items: center !important;
        justify-content: center !important;
        font-weight: normal !important;
        font-size: 1.25rem !important;
        cursor: pointer !important;
        transition: background-color 150ms ease-in-out !important;
      }
      :host .kebab-btn:hover {
        background-color: #f1f5f9 !important;
        color: #0f172a !important;
      }
      :host .caret-btn {
        background-color: transparent !important;
        color: #475569 !important;
        border: 0 !important;
        border-radius: 0.375rem !important;
        width: 1.5rem !important;
        height: 1.5rem !important;
        padding: 0 !important;
        display: inline-flex !important;
        align-items: center !important;
        justify-content: center !important;
        font-size: 0.75rem !important;
        cursor: pointer !important;
        transition: background-color 150ms ease-in-out !important;
      }
      :host .caret-btn:hover {
        background-color: #e2e8f0 !important;
        color: #0f172a !important;
      }
    `,
  ],
  template: `
    <app-page-shell
      title="Catálogos regulatorios"
      description="Navegación jerárquica por árbol de carpetas con búsqueda global y gestión de catálogos institucionales (UNSPSC, Objeto del Gasto y Plan de Cuentas)"
    >
      <div class="space-y-6" (click)="activeKebabMenuId = null">
        <!-- Header & Tab Controls Bar -->
        <div
          class="card p-4 flex flex-col md:flex-row md:items-center justify-between gap-4 bg-white border border-slate-200"
        >
          <div class="flex items-center gap-4 flex-wrap">
            <!-- Modern Intuitive Tab Navigation Switcher -->
            <div
              class="flex flex-wrap items-center gap-2 bg-slate-100 p-1.5 rounded-xl border border-slate-200"
            >
              <button
                type="button"
                (click)="cambiar('unspsc')"
                class="px-4 py-2 text-xs font-bold rounded-lg transition-all flex items-center gap-2 cursor-pointer"
                [ngClass]="
                  pestana() === 'unspsc'
                    ? 'bg-indigo-600 text-white shadow-sm ring-1 ring-indigo-500'
                    : 'text-slate-600 hover:text-slate-900 hover:bg-slate-200/70'
                "
              >
                <span>UNSPSC · DGCP</span>
                @if (pestana() === 'unspsc') {
                  <span class="size-2 rounded-full bg-emerald-400"></span>
                }
              </button>

              <button
                type="button"
                (click)="cambiar('objetos')"
                class="px-4 py-2 text-xs font-bold rounded-lg transition-all flex items-center gap-2 cursor-pointer"
                [ngClass]="
                  pestana() === 'objetos'
                    ? 'bg-indigo-600 text-white shadow-sm ring-1 ring-indigo-500'
                    : 'text-slate-600 hover:text-slate-900 hover:bg-slate-200/70'
                "
              >
                <span>Objeto del Gasto · DIGEPRES</span>
                @if (pestana() === 'objetos') {
                  <span class="size-2 rounded-full bg-emerald-400"></span>
                }
              </button>

              <button
                type="button"
                (click)="cambiar('cuentas')"
                class="px-4 py-2 text-xs font-bold rounded-lg transition-all flex items-center gap-2 cursor-pointer"
                [ngClass]="
                  pestana() === 'cuentas'
                    ? 'bg-indigo-600 text-white shadow-sm ring-1 ring-indigo-500'
                    : 'text-slate-600 hover:text-slate-900 hover:bg-slate-200/70'
                "
              >
                <span>Plan de Cuentas · DIGECOB</span>
                @if (pestana() === 'cuentas') {
                  <span class="size-2 rounded-full bg-emerald-400"></span>
                }
              </button>

              <button
                type="button"
                (click)="cambiar('fuentes')"
                class="px-4 py-2 text-xs font-bold rounded-lg transition-all flex items-center gap-2 cursor-pointer"
                [ngClass]="
                  pestana() === 'fuentes'
                    ? 'bg-indigo-600 text-white shadow-sm ring-1 ring-indigo-500'
                    : 'text-slate-600 hover:text-slate-900 hover:bg-slate-200/70'
                "
              >
                <span>Fuentes de Financiamiento</span>
                @if (pestana() === 'fuentes') {
                  <span class="size-2 rounded-full bg-emerald-400"></span>
                }
              </button>

              <button
                type="button"
                (click)="cambiar('mastersoft')"
                class="px-4 py-2 text-xs font-bold rounded-lg transition-all flex items-center gap-2 cursor-pointer"
                [ngClass]="
                  pestana() === 'mastersoft'
                    ? 'bg-indigo-600 text-white shadow-sm ring-1 ring-indigo-500'
                    : 'text-slate-600 hover:text-slate-900 hover:bg-slate-200/70'
                "
              >
                <span>Catálogo MasterSoft</span>
                @if (pestana() === 'mastersoft') {
                  <span class="size-2 rounded-full bg-emerald-400"></span>
                }
              </button>

              <button
                type="button"
                (click)="cambiar('mastersoft-vinc')"
                class="px-4 py-2 text-xs font-bold rounded-lg transition-all flex items-center gap-2 cursor-pointer"
                [ngClass]="
                  pestana() === 'mastersoft-vinc'
                    ? 'bg-indigo-600 text-white shadow-sm ring-1 ring-indigo-500'
                    : 'text-slate-600 hover:text-slate-900 hover:bg-slate-200/70'
                "
              >
                <span>Vinculación MasterSoft (DIGECOB)</span>
                @if (pestana() === 'mastersoft-vinc') {
                  <span class="size-2 rounded-full bg-emerald-400"></span>
                }
              </button>
            </div>

            <!-- Admin Mode Switch -->
            <label
              class="flex items-center gap-2 cursor-pointer text-xs font-semibold text-slate-700 bg-slate-100 px-3 py-2 rounded-xl border border-slate-200"
            >
              <input
                type="checkbox"
                class="!size-4 accent-indigo-600"
                [ngModel]="isAdminMode()"
                (ngModelChange)="isAdminMode.set($event)"
              />
              <span [ngClass]="isAdminMode() ? 'text-indigo-600 font-bold' : 'text-slate-600'">
                Modo Edición / Admin
              </span>
            </label>
          </div>

          @if (
            isAdminMode() &&
            pestana() !== 'fuentes' &&
            pestana() !== 'mastersoft' &&
            pestana() !== 'mastersoft-vinc'
          ) {
            <button type="button" (click)="openCreateModal()" class="btn-custom-primary">
              <i class="bi bi-plus-lg"></i>
              + Nuevo Elemento en {{ tituloPestana() }}
            </button>
          }
        </div>

        <!-- ═══════════ FUENTES DE FINANCIAMIENTO TAB ═══════════ -->
        @if (pestana() === 'fuentes') {
          <!-- Fuentes search bar -->
          <div class="card p-4 space-y-4 bg-white border border-slate-200">
            <div class="flex flex-wrap items-center gap-3">
              <div class="flex items-center gap-2 flex-1 max-w-lg">
                <div class="relative flex-1">
                  <input
                    type="text"
                    class="field-control w-full pl-9 pr-8"
                    placeholder="Buscar por código o nombre de fuente…"
                    [(ngModel)]="fuenteBusqueda"
                    (keyup.enter)="cargarFuentes()"
                  />
                  <span class="absolute left-3 top-2.5 text-slate-400 text-sm">🔍</span>
                  @if (fuenteBusqueda) {
                    <button
                      type="button"
                      (click)="fuenteBusqueda = ''; cargarFuentes()"
                      class="absolute right-2.5 top-2.5 text-slate-400 hover:text-slate-600 text-xs font-bold"
                    >
                      ✕
                    </button>
                  }
                </div>
                <button
                  type="button"
                  (click)="cargarFuentes()"
                  class="btn-custom-primary !py-2 !px-4"
                >
                  Filtrar
                </button>
              </div>

              <div class="ml-auto">
                <button type="button" (click)="abrirModalFuente()" class="btn-custom-primary">
                  + Nueva Fuente de Financiamiento
                </button>
              </div>
            </div>
          </div>

          @if (fuenteError()) {
            <p class="rounded-lg bg-rose-50 border border-rose-200 px-4 py-3 text-sm text-rose-700">
              {{ fuenteError() }}
            </p>
          }

          @if (fuenteExito()) {
            <p
              class="rounded-lg bg-emerald-50 border border-emerald-200 px-4 py-3 text-sm text-emerald-700"
            >
              {{ fuenteExito() }}
            </p>
          }

          <!-- Fuentes table -->
          <div class="card overflow-hidden bg-white border border-slate-200 shadow-sm">
            @if (fuenteCargando()) {
              <div class="px-4 py-12 text-center text-sm text-slate-500">
                <span
                  class="mb-2 inline-block size-6 animate-spin rounded-full border-2 border-indigo-600 border-t-transparent"
                ></span>
                Cargando fuentes de financiamiento…
              </div>
            } @else {
              <div class="overflow-x-auto">
                <table class="w-full text-sm text-left border-collapse">
                  <thead>
                    <tr class="border-b border-slate-200 bg-slate-50">
                      <th class="px-4 py-3 text-xs font-semibold uppercase text-slate-600">
                        Código
                      </th>
                      <th class="px-4 py-3 text-xs font-semibold uppercase text-slate-600">
                        Nombre
                      </th>
                      <th class="px-4 py-3 text-xs font-semibold uppercase text-slate-600">
                        Descripción
                      </th>
                      <th
                        class="px-4 py-3 text-xs font-semibold uppercase text-slate-600 text-center"
                      >
                        Estado
                      </th>
                      <th
                        class="px-4 py-3 text-xs font-semibold uppercase text-slate-600 text-right"
                      >
                        Acciones
                      </th>
                    </tr>
                  </thead>
                  <tbody class="divide-y divide-slate-100">
                    @for (f of listaFuentes(); track f.id) {
                      <tr class="hover:bg-slate-50 transition-colors">
                        <td class="px-4 py-3 font-mono font-semibold text-indigo-700">
                          {{ f.code }}
                        </td>
                        <td class="px-4 py-3 font-semibold text-slate-900">{{ f.name }}</td>
                        <td class="px-4 py-3 text-xs text-slate-500">{{ f.description || '—' }}</td>
                        <td class="px-4 py-3 text-center">
                          @if (f.isActive) {
                            <span
                              class="inline-flex items-center gap-1 rounded-full bg-emerald-100 px-2.5 py-0.5 text-xs font-semibold text-emerald-700"
                              >● Activa</span
                            >
                          } @else {
                            <span
                              class="inline-flex items-center gap-1 rounded-full bg-slate-200 px-2.5 py-0.5 text-xs font-semibold text-slate-600"
                              >○ Inactiva</span
                            >
                          }
                        </td>
                        <td class="px-4 py-3 text-right">
                          <div class="flex items-center justify-end gap-2">
                            <button
                              type="button"
                              (click)="editarFuente(f)"
                              class="btn-custom-outline !py-1 !px-2 !text-xs"
                            >
                              ✏️ Editar
                            </button>
                            <button
                              type="button"
                              (click)="eliminarFuente(f)"
                              class="btn-custom-outline !py-1 !px-2 !text-xs !text-rose-600 !border-rose-300 hover:!bg-rose-50"
                            >
                              🗑 Eliminar
                            </button>
                          </div>
                        </td>
                      </tr>
                    } @empty {
                      <tr>
                        <td colspan="5" class="px-4 py-12 text-center text-sm text-slate-400">
                          No se encontraron fuentes de financiamiento registradas.
                        </td>
                      </tr>
                    }
                  </tbody>
                </table>
              </div>

              <!-- Footer -->
              <div
                class="flex items-center justify-between border-t border-slate-200 px-4 py-3 text-xs text-slate-500 bg-slate-50"
              >
                <div>
                  Mostrando
                  <span class="font-semibold text-slate-800">{{
                    listaFuentes().length | number
                  }}</span>
                  fuente(s) de financiamiento
                </div>
              </div>
            }
          </div>
        } @else if (pestana() === 'mastersoft') {
          <!-- ═══════════ CATÁLOGO MASTERSOFT TAB ═══════════ -->
          <div class="card bg-white border border-slate-200 overflow-hidden shadow-sm">
            <div
              class="p-4 border-b border-slate-200 flex flex-wrap items-center justify-between gap-4 bg-slate-50"
            >
              <div class="flex items-center gap-3 flex-1 max-w-md">
                <input
                  type="text"
                  class="field-control w-full text-xs"
                  placeholder="Buscar por código o descripción MasterSoft..."
                  [ngModel]="busquedaMastersoft()"
                  (ngModelChange)="busquedaMastersoft.set($event); cargarMastersoft()"
                />
              </div>
              <div class="text-xs text-slate-500 font-semibold">
                Total cuentas MasterSoft:
                <span class="text-slate-900 font-bold">{{ totalMastersoft() | number }}</span>
              </div>
            </div>

            @if (mastersoftCargando()) {
              <div class="p-12 text-center text-sm text-slate-500">
                <div
                  class="inline-block animate-spin rounded-full h-6 w-6 border-b-2 border-indigo-600 mb-2"
                ></div>
                <div>Cargando catálogo MasterSoft...</div>
              </div>
            } @else {
              <div class="overflow-x-auto">
                <table class="w-full text-left text-xs border-collapse">
                  <thead>
                    <tr
                      class="bg-slate-100 border-b border-slate-200 text-slate-700 font-bold uppercase tracking-wider text-[11px]"
                    >
                      <th class="px-4 py-3">Código MasterSoft</th>
                      <th class="px-4 py-3">Descripción</th>
                      <th class="px-4 py-3">Cuenta Control (Padre)</th>
                      <th class="px-4 py-3 text-center">Nivel</th>
                      <th class="px-4 py-3 text-center">Imputable</th>
                    </tr>
                  </thead>
                  <tbody class="divide-y divide-slate-100">
                    @for (m of listaMastersoft(); track m.id) {
                      <tr class="hover:bg-slate-50/80 transition-colors">
                        <td class="px-4 py-3 font-mono font-bold text-slate-900">{{ m.code }}</td>
                        <td class="px-4 py-3 font-semibold text-slate-800">{{ m.name }}</td>
                        <td class="px-4 py-3 font-mono text-slate-500">
                          {{ m.controlAccountCode || '—' }}
                        </td>
                        <td class="px-4 py-3 text-center font-semibold text-slate-600">
                          {{ m.level }}
                        </td>
                        <td class="px-4 py-3 text-center">
                          @if (m.isPostable) {
                            <span
                              class="inline-flex items-center rounded-full bg-emerald-100 px-2 py-0.5 text-[10px] font-bold text-emerald-700"
                              >Sí</span
                            >
                          } @else {
                            <span
                              class="inline-flex items-center rounded-full bg-slate-200 px-2 py-0.5 text-[10px] font-bold text-slate-600"
                              >No</span
                            >
                          }
                        </td>
                      </tr>
                    } @empty {
                      <tr>
                        <td colspan="5" class="px-4 py-12 text-center text-sm text-slate-400">
                          No se encontraron cuentas en el catálogo MasterSoft.
                        </td>
                      </tr>
                    }
                  </tbody>
                </table>
              </div>
            }
          </div>
        } @else if (pestana() === 'mastersoft-vinc') {
          <!-- ═══════════ VINCULACIÓN MASTERSOFT (DIGECOB) TAB ═══════════ -->
          <div class="card bg-white border border-slate-200 overflow-hidden shadow-sm">
            <div
              class="p-4 border-b border-slate-200 flex flex-wrap items-center justify-between gap-4 bg-slate-50"
            >
              <div class="flex items-center gap-3 flex-wrap flex-1">
                <input
                  type="text"
                  class="field-control w-64 text-xs"
                  placeholder="Buscar código/nombre DIGECOB o MasterSoft..."
                  [ngModel]="busquedaVinculacion()"
                  (ngModelChange)="busquedaVinculacion.set($event); cargarVinculacion()"
                />

                <select
                  class="field-control text-xs"
                  [ngModel]="filtroModoVinculacion()"
                  (ngModelChange)="filtroModoVinculacion.set($event); cargarVinculacion()"
                >
                  <option value="all">Ver Todas las Cuentas</option>
                  <option value="unlinked">⚠️ Sin Vinculación MasterSoft</option>
                  <option value="linked">✅ Con Vinculación (Directa o Heredada)</option>
                  <option value="direct">🟢 Vinculación Directa</option>
                  <option value="inherited">🔵 Vinculación Heredada del Padre</option>
                </select>
              </div>

              <div class="flex items-center gap-2">
                <button
                  type="button"
                  (click)="abrirModalNuevaVinc()"
                  class="btn-custom-primary !text-xs !py-1.5 !px-3 flex items-center gap-1.5"
                  title="Crear una nueva vinculación directa entre DIGECOB y MasterSoft"
                >
                  ➕ Nueva Vinculación
                </button>

                <button
                  type="button"
                  (click)="exportarHomologacionMastersoft()"
                  class="btn-custom-outline !text-xs !py-1.5 !px-3 flex items-center gap-1"
                  title="Descargar reporte completo en Excel"
                >
                  📥 Exportar Homologación (.xlsx)
                </button>

                <label
                  class="btn-custom-outline !text-xs !py-1.5 !px-3 cursor-pointer flex items-center gap-1"
                >
                  📤 Importar Homologación (.xlsx)
                  <input
                    type="file"
                    accept=".xlsx"
                    class="hidden"
                    (change)="onImportMastersoftFileSelected($event)"
                  />
                </label>
              </div>
            </div>

            @if (vincExito()) {
              <div
                class="p-3 bg-emerald-50 text-emerald-800 text-xs font-semibold border-b border-emerald-200"
              >
                ✅ {{ vincExito() }}
              </div>
            }
            @if (vincError()) {
              <div
                class="p-3 bg-rose-50 text-rose-800 text-xs font-semibold border-b border-rose-200"
              >
                ⚠️ {{ vincError() }}
              </div>
            }

            @if (vincCargando()) {
              <div class="p-12 text-center text-sm text-slate-500">
                <div
                  class="inline-block animate-spin rounded-full h-6 w-6 border-b-2 border-indigo-600 mb-2"
                ></div>
                <div>Cargando tabla de vinculación DIGECOB <-> MasterSoft...</div>
              </div>
            } @else {
              <div class="overflow-x-auto">
                <table class="w-full text-left text-xs border-collapse">
                  <thead>
                    <tr
                      class="bg-slate-100 border-b border-slate-200 text-slate-700 font-bold uppercase tracking-wider text-[11px]"
                    >
                      <th class="px-4 py-3">Cuenta DIGECOB</th>
                      <th class="px-4 py-3">Descripción DIGECOB</th>
                      <th class="px-4 py-3 text-center">Imputable</th>
                      <th class="px-4 py-3 text-center">Estado Link</th>
                      <th class="px-4 py-3">Cuenta MasterSoft</th>
                      <th class="px-4 py-3">Descripción MasterSoft</th>
                      <th class="px-4 py-3">Heredado De</th>
                      <th class="px-4 py-3 text-right">Acciones</th>
                    </tr>
                  </thead>
                  <tbody class="divide-y divide-slate-100">
                    @for (v of listaVinculacion(); track v.digecobAccountId) {
                      <tr class="hover:bg-slate-50/80 transition-colors">
                        <td class="px-4 py-3 font-mono font-bold text-slate-900">
                          {{ v.digecobAccountCode }}
                        </td>
                        <td
                          class="px-4 py-3 font-semibold text-slate-800 max-w-xs truncate"
                          [title]="v.digecobAccountName"
                        >
                          {{ v.digecobAccountName }}
                        </td>
                        <td class="px-4 py-3 text-center">
                          <span
                            class="font-mono text-[10px]"
                            [ngClass]="
                              v.digecobIsPostable ? 'text-emerald-700 font-bold' : 'text-slate-400'
                            "
                          >
                            {{ v.digecobIsPostable ? 'S' : 'N' }}
                          </span>
                        </td>
                        <td class="px-4 py-3 text-center">
                          @if (!v.mastersoftAccountCode) {
                            <span
                              class="inline-flex items-center rounded-full bg-amber-100 px-2 py-0.5 text-[10px] font-bold text-amber-800"
                            >
                              Sin Link
                            </span>
                          } @else if (v.isInherited) {
                            <span
                              class="inline-flex items-center rounded-full bg-blue-100 px-2 py-0.5 text-[10px] font-bold text-blue-800"
                              [title]="'Heredado de la cuenta padre ' + v.inheritedFromDigecobCode"
                            >
                              Heredada
                            </span>
                          } @else {
                            <span
                              class="inline-flex items-center rounded-full bg-emerald-100 px-2 py-0.5 text-[10px] font-bold text-emerald-800"
                            >
                              Directa
                            </span>
                          }
                        </td>
                        <td
                          class="px-4 py-3 font-mono font-bold"
                          [ngClass]="v.mastersoftAccountCode ? 'text-indigo-700' : 'text-slate-300'"
                        >
                          {{ v.mastersoftAccountCode || '—' }}
                        </td>
                        <td
                          class="px-4 py-3 font-semibold text-slate-700 max-w-xs truncate"
                          [title]="v.mastersoftAccountName || ''"
                        >
                          {{ v.mastersoftAccountName || '—' }}
                        </td>
                        <td class="px-4 py-3 font-mono text-xs text-blue-600">
                          {{ v.inheritedFromDigecobCode || '—' }}
                        </td>
                        <td class="px-4 py-3 text-right">
                          <div class="flex items-center justify-end gap-1.5">
                            <button
                              type="button"
                              (click)="abrirModalVinc(v)"
                              class="btn-custom-outline !py-1 !px-2 !text-[11px] flex items-center gap-1"
                              [title]="
                                v.mastersoftAccountCode && !v.isInherited
                                  ? 'Editar vinculación directa'
                                  : 'Vincular cuenta con MasterSoft'
                              "
                            >
                              ✏️
                              {{
                                v.mastersoftAccountCode && !v.isInherited ? 'Editar' : 'Vincular'
                              }}
                            </button>

                            @if (v.mastersoftAccountCode && !v.isInherited) {
                              <button
                                type="button"
                                (click)="abrirConfirmarEliminarVinc(v)"
                                class="btn-custom-outline !py-1 !px-2 !text-[11px] !text-rose-600 !border-rose-300 hover:!bg-rose-50 flex items-center gap-1"
                                title="Eliminar vinculación directa"
                              >
                                🗑️ Eliminar
                              </button>
                            }
                          </div>
                        </td>
                      </tr>
                    } @empty {
                      <tr>
                        <td colspan="8" class="px-4 py-12 text-center text-sm text-slate-400">
                          No se encontraron cuentas con los filtros seleccionados.
                        </td>
                      </tr>
                    }
                  </tbody>
                </table>
              </div>

              <div
                class="flex items-center justify-between border-t border-slate-200 px-4 py-3 text-xs text-slate-500 bg-slate-50"
              >
                <div>
                  Mostrando
                  <span class="font-semibold text-slate-800">{{
                    listaVinculacion().length | number
                  }}</span>
                  registros (Total: {{ totalVinculacion() | number }})
                </div>
              </div>
            }
          </div>
        } @else {
          <!-- ═══════════ TREE-BASED CATALOGS (UNSPSC / OBJETOS / CUENTAS) ═══════════ -->

          <!-- Search & Hierarchy Controls -->
          <div class="card p-4 space-y-4 bg-white border border-slate-200">
            <div class="flex flex-wrap items-center gap-3">
              <!-- Search Box with Explicit Filter Button (Fires ONLY on Enter or Click) -->
              <div class="flex items-center gap-2 flex-1 max-w-lg">
                <div class="relative flex-1">
                  <input
                    type="text"
                    class="field-control w-full pl-9 pr-8"
                    [placeholder]="placeholder()"
                    [(ngModel)]="busquedaInput"
                    (keyup.enter)="ejecutarBusqueda()"
                  />
                  <span class="absolute left-3 top-2.5 text-slate-400 text-sm">🔍</span>
                  @if (busquedaInput) {
                    <button
                      type="button"
                      (click)="limpiarBusqueda()"
                      class="absolute right-2.5 top-2.5 text-slate-400 hover:text-slate-600 text-xs font-bold"
                    >
                      ✕
                    </button>
                  }
                </div>

                <button
                  type="button"
                  (click)="ejecutarBusqueda()"
                  class="btn-custom-primary !py-2 !px-4"
                >
                  Filtrar
                </button>
              </div>

              @if (pestana() === 'unspsc') {
                <div
                  class="text-xs text-slate-500 font-medium bg-slate-50 px-3 py-2 rounded-lg border border-slate-200 flex items-center gap-2"
                >
                  <span class="font-bold text-slate-700">Jerarquía UNSPSC:</span>
                  <span class="text-indigo-600 font-semibold">SEGMENTO</span> &gt;
                  <span class="text-indigo-600 font-semibold">FAMILIA</span> &gt;
                  <span class="text-indigo-600 font-semibold">CLASE</span> &gt;
                  <span class="text-indigo-600 font-semibold">MATERIAL</span>
                </div>
              }

              @if (pestana() === 'objetos') {
                <label class="flex items-center gap-2 text-sm text-slate-600 cursor-pointer">
                  <input
                    type="checkbox"
                    class="!size-4 accent-indigo-600"
                    [ngModel]="soloImputables()"
                    (ngModelChange)="alternarFiltro($event)"
                  />
                  Solo niveles de detalle (Imputables)
                </label>
              }

              @if (pestana() === 'cuentas') {
                <label class="flex items-center gap-2 text-sm text-slate-600 cursor-pointer">
                  <input
                    type="checkbox"
                    class="!size-4 accent-indigo-600"
                    [ngModel]="soloImputables()"
                    (ngModelChange)="alternarFiltro($event)"
                  />
                  Solo cuentas imputables
                </label>
              }

              <div class="flex items-center gap-2 ml-auto">
                <button
                  type="button"
                  (click)="expandirTodo()"
                  class="btn-custom-outline"
                  title="Expandir todos los nodos del árbol"
                >
                  <i class="bi bi-folder-plus"></i> Expandir Todo
                </button>
                <button
                  type="button"
                  (click)="colapsarTodo()"
                  class="btn-custom-outline"
                  title="Colapsar todos los nodos"
                >
                  <i class="bi bi-folder-minus"></i> Colapsar Todo
                </button>
              </div>
            </div>
          </div>

          @if (error()) {
            <p class="rounded-lg bg-rose-50 border border-rose-200 px-4 py-3 text-sm text-rose-700">
              {{ error() }}
            </p>
          }

          <!-- Hierarchical Tree Table -->
          <div class="card overflow-hidden bg-white border border-slate-200 shadow-sm">
            @if (cargando()) {
              <div class="px-4 py-12 text-center text-sm text-slate-500">
                <span
                  class="mb-2 inline-block size-6 animate-spin rounded-full border-2 border-indigo-600 border-t-transparent"
                ></span>
                Cargando estructura jerárquica de {{ tituloPestana() }}...
              </div>
            } @else {
              <div class="overflow-x-auto">
                <table class="w-full text-sm text-left border-collapse">
                  <thead>
                    <tr class="border-b border-slate-200 bg-slate-50">
                      <th class="px-4 py-3 text-xs font-semibold uppercase text-slate-600">
                        Estructura / Denominación
                      </th>
                      <th class="px-4 py-3 text-xs font-semibold uppercase text-slate-600">
                        Código
                      </th>
                      <th class="px-4 py-3 text-xs font-semibold uppercase text-slate-600">
                        Nivel Jerárquico
                      </th>
                      <th class="px-4 py-3 text-xs font-semibold uppercase text-slate-600">
                        {{
                          pestana() === 'cuentas'
                            ? 'Imputable'
                            : pestana() === 'objetos'
                              ? 'Detalle'
                              : 'Habilitado'
                        }}
                      </th>
                      @if (isAdminMode()) {
                        <th
                          class="px-4 py-3 text-xs font-semibold uppercase text-slate-600 text-right"
                        >
                          Opciones
                        </th>
                      }
                    </tr>
                  </thead>
                  <tbody class="divide-y divide-slate-100">
                    @for (node of visibleNodes(); track node.id) {
                      <tr
                        class="hover:bg-slate-50 transition-colors"
                        [ngClass]="{ 'bg-amber-50/50': node.matchedInSearch }"
                      >
                        <!-- Hierarchical Folder & Name Column -->
                        <td class="py-3 pr-4" [style.paddingLeft.px]="getIndentPadding(node)">
                          <div class="flex items-center gap-2">
                            <!-- Tree Branch Connector for Child Nodes -->
                            @if (getIndentDepth(node) > 0) {
                              <span class="text-indigo-400 font-mono text-xs select-none font-bold"
                                >└─</span
                              >
                            }

                            <!-- Expand / Collapse Caret & Folder Icon -->
                            @if (
                              node.childCount > 0 || (node.children && node.children.length > 0)
                            ) {
                              <button
                                type="button"
                                (click)="toggleExpand(node, $event)"
                                class="caret-btn"
                              >
                                @if (node.isLoadingChildren) {
                                  <span
                                    class="inline-block size-3 animate-spin rounded-full border-2 border-indigo-600 border-t-transparent"
                                  ></span>
                                } @else {
                                  <span>{{ node.isExpanded ? '▼' : '▶' }}</span>
                                }
                              </button>
                              <span class="text-amber-500 font-bold text-sm">
                                {{ node.isExpanded ? '📂' : '📁' }}
                              </span>
                            } @else {
                              <span class="w-6"></span>
                              <span class="text-slate-400 text-xs">📄</span>
                            }

                            <div class="flex flex-col">
                              <span class="font-medium text-slate-900 flex items-center gap-2">
                                {{ node.nombre }}
                                @if (node.matchedInSearch) {
                                  <span
                                    class="text-[10px] bg-amber-100 text-amber-800 px-1.5 py-0.5 rounded font-bold border border-amber-200"
                                  >
                                    Coincidencia
                                  </span>
                                }
                              </span>
                              @if (node.detalle) {
                                <span class="text-xs text-slate-500 mt-0.5">{{
                                  node.detalle
                                }}</span>
                              }
                            </div>
                          </div>
                        </td>

                        <!-- Code Column -->
                        <td
                          class="px-4 py-3 font-mono text-xs font-bold text-indigo-600 whitespace-nowrap"
                        >
                          {{ node.codigo }}
                        </td>

                        <!-- Level Badge Column -->
                        <td class="px-4 py-3 text-xs">
                          <span
                            class="px-2 py-0.5 rounded bg-slate-100 border border-slate-200 text-slate-700 font-semibold"
                          >
                            {{ formatNivelLabel(node.nivel) }}
                          </span>
                        </td>

                        <!-- Imputable / Status Badge -->
                        <td class="px-4 py-3">
                          @if (node.marca) {
                            <span
                              class="inline-flex items-center gap-1 text-xs text-emerald-800 bg-emerald-50 px-2.5 py-0.5 rounded-full font-semibold border border-emerald-200"
                            >
                              Sí
                            </span>
                          } @else {
                            <span
                              class="inline-flex items-center gap-1 text-xs text-slate-700 bg-slate-100 px-2.5 py-0.5 rounded-full font-semibold border border-slate-200"
                            >
                              No
                            </span>
                          }
                        </td>

                        <!-- Kebab Menu (Three-Dots Vertical) Admin Column -->
                        @if (isAdminMode()) {
                          <td class="px-4 py-3 text-right relative">
                            <div class="inline-block text-left" (click)="$event.stopPropagation()">
                              <button
                                type="button"
                                (click)="toggleKebabMenu(node.id, $event)"
                                class="kebab-btn"
                                title="Opciones de elemento"
                              >
                                ⋮
                              </button>

                              <!-- Floating Kebab Dropdown Menu (Positioned cleanly without shifting rows) -->
                              @if (activeKebabMenuId === node.id) {
                                <div
                                  class="absolute right-2 top-full mt-1 z-50 w-52 bg-white rounded-xl shadow-2xl border border-slate-200 p-1.5 text-xs text-left divide-y divide-slate-100"
                                >
                                  <div class="py-1 space-y-0.5">
                                    <button
                                      type="button"
                                      (click)="openEditModal(node)"
                                      class="w-full text-left px-3.5 py-2 rounded-lg hover:bg-slate-100 text-slate-800 font-medium transition-colors"
                                    >
                                      Editar Elemento
                                    </button>
                                    <button
                                      type="button"
                                      (click)="openAddChildModal(node)"
                                      class="w-full text-left px-3.5 py-2 rounded-lg hover:bg-indigo-50 text-indigo-700 font-semibold transition-colors"
                                    >
                                      Agregar Sub-elemento
                                    </button>
                                  </div>
                                  <div class="pt-1 mt-1">
                                    <button
                                      type="button"
                                      (click)="openDeleteModal(node)"
                                      class="w-full text-left px-3.5 py-2 rounded-lg hover:bg-rose-50 text-rose-600 font-medium transition-colors"
                                    >
                                      Eliminar
                                    </button>
                                  </div>
                                </div>
                              }
                            </div>
                          </td>
                        }
                      </tr>
                    } @empty {
                      <tr>
                        <td
                          [attr.colspan]="isAdminMode() ? 5 : 4"
                          class="px-4 py-12 text-center text-sm text-slate-500"
                        >
                          No se encontraron registros en el catálogo {{ tituloPestana() }}.
                        </td>
                      </tr>
                    }
                  </tbody>
                </table>
              </div>

              <!-- Footer Tree Summary Bar -->
              <div
                class="flex flex-col sm:flex-row items-center justify-between border-t border-slate-200 px-4 py-3 text-xs text-slate-500 gap-3 bg-slate-50"
              >
                <div>
                  Mostrando
                  <span class="font-semibold text-slate-800">{{
                    visibleNodes().length | number
                  }}</span>
                  elementos visibles en el árbol jerárquico
                </div>

                <div class="flex items-center gap-2 text-slate-500 font-medium">
                  💡 Haz clic en <span class="font-bold text-slate-800">▶</span> para abrir
                  subcarpetas e inspeccionar la jerarquía.
                </div>
              </div>
            }
          </div>
        }
        <!-- end @else for tree catalogs -->
      </div>
    </app-page-shell>

    <!-- ═══════════ MODAL: CREAR / EDITAR FUENTE DE FINANCIAMIENTO ═══════════ -->
    @if (fuenteModalAbierto()) {
      <div
        class="fixed inset-0 z-50 bg-slate-900/50 backdrop-blur-sm flex items-center justify-center p-4"
      >
        <div
          class="bg-white rounded-2xl max-w-md w-full p-6 shadow-2xl border border-slate-200 space-y-6"
        >
          <div class="flex items-center justify-between border-b border-slate-200 pb-4">
            <h3 class="text-lg font-bold text-slate-900">
              {{ fuenteEditandoId ? '✏️ Editar' : '➕ Nueva' }} Fuente de Financiamiento
            </h3>
            <button type="button" (click)="fuenteModalAbierto.set(false)" class="kebab-btn">
              ✕
            </button>
          </div>

          <div class="space-y-4">
            <div>
              <label class="block text-xs font-semibold text-slate-700 mb-1">Código</label>
              <input
                type="text"
                [(ngModel)]="fuenteFormCode"
                placeholder="Ej. Fuente 10"
                class="field-control w-full"
              />
            </div>

            <div>
              <label class="block text-xs font-semibold text-slate-700 mb-1">Nombre</label>
              <input
                type="text"
                [(ngModel)]="fuenteFormName"
                placeholder="Ej. Recursos del Tesoro"
                class="field-control w-full"
              />
            </div>

            <div>
              <label class="block text-xs font-semibold text-slate-700 mb-1">Descripción</label>
              <textarea
                rows="2"
                [(ngModel)]="fuenteFormDesc"
                placeholder="Detalle adicional de esta fuente de financiamiento..."
                class="field-control w-full text-xs"
              ></textarea>
            </div>

            <div class="pt-2">
              <label
                class="flex items-center gap-2 cursor-pointer text-xs font-semibold text-slate-700"
              >
                <input
                  type="checkbox"
                  [(ngModel)]="fuenteFormActiva"
                  class="!size-4 accent-indigo-600"
                />
                <span>Fuente Activa</span>
              </label>
            </div>
          </div>

          <div class="flex justify-end gap-3 pt-4 border-t border-slate-200">
            <button
              type="button"
              (click)="fuenteModalAbierto.set(false)"
              class="btn-custom-outline"
            >
              Cancelar
            </button>
            <button
              type="button"
              (click)="guardarFuente()"
              class="btn-custom-primary"
              [disabled]="!fuenteFormCode.trim() || !fuenteFormName.trim()"
            >
              Guardar Fuente
            </button>
          </div>
        </div>
      </div>
    }

    <!-- ═══════════ MODAL: ELIMINAR FUENTE DE FINANCIAMIENTO ═══════════ -->
    @if (fuenteDeleteModalAbierto()) {
      <div
        class="fixed inset-0 z-50 bg-slate-900/50 backdrop-blur-sm flex items-center justify-center p-4"
      >
        <div
          class="bg-white rounded-2xl max-w-sm w-full p-6 shadow-2xl border border-slate-200 space-y-6 text-center"
        >
          <div
            class="w-12 h-12 rounded-full bg-rose-100 text-rose-600 flex items-center justify-center mx-auto text-xl font-bold"
          >
            !
          </div>

          <div class="space-y-2">
            <h3 class="text-base font-bold text-slate-900">¿Eliminar fuente de financiamiento?</h3>
            <p class="text-xs text-slate-500">
              Estás a punto de eliminar la fuente
              <b class="font-mono text-slate-800">{{ fuenteParaEliminar()?.code }}</b> -
              {{ fuenteParaEliminar()?.name }}.
            </p>
          </div>

          <div class="flex justify-center gap-3 pt-2">
            <button
              type="button"
              (click)="fuenteDeleteModalAbierto.set(false)"
              class="btn-custom-outline w-full"
            >
              Cancelar
            </button>
            <button
              type="button"
              (click)="confirmarEliminarFuente()"
              class="btn-custom-primary !bg-rose-600 hover:!bg-rose-700 w-full"
            >
              Sí, Eliminar
            </button>
          </div>
        </div>
      </div>
    }

    <!-- Create / Edit Item Modal -->
    @if (isItemModalOpen) {
      <div
        class="fixed inset-0 z-50 bg-slate-900/50 backdrop-blur-sm flex items-center justify-center p-4"
      >
        <div
          class="bg-white rounded-2xl max-w-md w-full p-6 shadow-2xl border border-slate-200 space-y-6"
        >
          <div class="flex items-center justify-between border-b border-slate-200 pb-4">
            <h3 class="text-lg font-bold text-slate-900">
              {{ modalMode === 'create' ? 'Agregar a' : 'Modificar en' }} {{ tituloPestana() }}
            </h3>
            <button
              type="button"
              (click)="isItemModalOpen = false"
              class="text-slate-400 hover:text-slate-700 text-lg font-bold"
            >
              ✕
            </button>
          </div>

          <div class="space-y-4 text-sm">
            @if (parentForNewChild) {
              <div
                class="p-3 bg-indigo-50 rounded-lg text-xs text-indigo-700 border border-indigo-200 font-medium"
              >
                Sub-elemento derivado de:
                <b>{{ parentForNewChild.codigo }} - {{ parentForNewChild.nombre }}</b>
              </div>
            }

            <div>
              <label class="block text-xs font-semibold text-slate-700 mb-1"
                >Código Identificador</label
              >
              <input
                type="text"
                [(ngModel)]="formCode"
                placeholder="Ej. 10101501 o 2.6.1.1"
                class="field-control w-full font-mono"
              />
            </div>

            <div>
              <label class="block text-xs font-semibold text-slate-700 mb-1"
                >Denominación / Nombre</label
              >
              <input
                type="text"
                [(ngModel)]="formName"
                placeholder="Nombre descriptivo del catálogo"
                class="field-control w-full"
              />
            </div>

            <div>
              <label class="block text-xs font-semibold text-slate-700 mb-1"
                >Descripción / Detalle Adicional</label
              >
              <textarea
                rows="2"
                [(ngModel)]="formDetail"
                placeholder="Información o notas adicionales..."
                class="field-control w-full text-xs"
              ></textarea>
            </div>

            <div>
              <label class="block text-xs font-semibold text-slate-700 mb-1"
                >Nivel Jerárquico</label
              >
              @if (pestana() === 'unspsc') {
                <select [(ngModel)]="formLevel" class="field-control w-full">
                  <option value="Segment">Segmento</option>
                  <option value="Family">Familia</option>
                  <option value="Class">Clase</option>
                  <option value="Material">Material</option>
                </select>
              } @else {
                <input
                  type="text"
                  [(ngModel)]="formLevel"
                  placeholder="Ej. Nivel 4..."
                  class="field-control w-full"
                />
              }
            </div>

            <div class="pt-2">
              <label
                class="flex items-center gap-2 cursor-pointer text-xs font-semibold text-slate-700"
              >
                <input type="checkbox" [(ngModel)]="formMarca" class="!size-4 accent-indigo-600" />
                <span>
                  {{
                    pestana() === 'cuentas'
                      ? 'Es Cuenta Imputable'
                      : pestana() === 'objetos'
                        ? 'Es Nivel de Detalle'
                        : 'Habilitado Institucionalmente'
                  }}
                </span>
              </label>
            </div>
          </div>

          <div class="flex justify-end gap-3 pt-4 border-t border-slate-200">
            <button type="button" (click)="isItemModalOpen = false" class="btn-custom-outline">
              Cancelar
            </button>
            <button type="button" (click)="saveItem()" class="btn-custom-primary">
              Guardar Elemento
            </button>
          </div>
        </div>
      </div>
    }

    <!-- Delete Confirmation Modal -->
    @if (isDeleteModalOpen) {
      <div
        class="fixed inset-0 z-50 bg-slate-900/50 backdrop-blur-sm flex items-center justify-center p-4"
      >
        <div
          class="bg-white rounded-2xl max-w-sm w-full p-6 shadow-2xl border border-slate-200 space-y-6 text-center"
        >
          <div
            class="w-12 h-12 rounded-full bg-rose-100 text-rose-600 flex items-center justify-center mx-auto text-xl font-bold"
          >
            !
          </div>

          <div class="space-y-2">
            <h3 class="text-base font-bold text-slate-900">¿Eliminar elemento del catálogo?</h3>
            <p class="text-xs text-slate-500">
              Estás a punto de eliminar el registro
              <b class="font-mono text-slate-800">{{ selectedNodeForDelete?.codigo }}</b> -
              {{ selectedNodeForDelete?.nombre }}.
            </p>
          </div>

          <div class="flex justify-center gap-3 pt-2">
            <button
              type="button"
              (click)="isDeleteModalOpen = false"
              class="btn-custom-outline w-full"
            >
              Cancelar
            </button>
            <button
              type="button"
              (click)="executeDelete()"
              class="btn-custom-primary !bg-rose-600 hover:!bg-rose-700 w-full"
            >
              Sí, Eliminar
            </button>
          </div>
        </div>
      </div>
    }

    <!-- MasterSoft Link Modal (Create or Edit Link) -->
    @if (isVincModalOpen()) {
      <div
        class="fixed inset-0 z-50 bg-slate-900/50 backdrop-blur-sm flex items-center justify-center p-4"
      >
        <div
          class="bg-white rounded-2xl max-w-lg w-full p-6 shadow-2xl border border-slate-200 space-y-5"
        >
          <div class="flex items-center justify-between border-b border-slate-100 pb-3">
            <h3 class="text-base font-bold text-slate-900">
              🔗 {{ isNewVincMode() ? 'Nueva Vinculación Directa' : 'Editar Vinculación Directa' }}
            </h3>
            <button
              type="button"
              (click)="isVincModalOpen.set(false)"
              class="text-slate-400 hover:text-slate-600 font-bold"
            >
              ✕
            </button>
          </div>

          @if (isNewVincMode()) {
            <div>
              <label class="block text-xs font-bold text-slate-700 mb-1"
                >1. Seleccionar Cuenta DIGECOB (Origen)</label
              >
              <input
                type="text"
                class="field-control w-full text-xs mb-2"
                placeholder="Filtrar por código o nombre de cuenta DIGECOB..."
                [ngModel]="digecobPickerSearch()"
                (ngModelChange)="digecobPickerSearch.set($event); filtrarPickerDigecob()"
              />

              <select
                class="field-control w-full text-xs h-36"
                size="5"
                [ngModel]="selectedDigecobIdForVinc()"
                (ngModelChange)="selectedDigecobIdForVinc.set($event)"
              >
                @for (d of digecobOptionsFiltered(); track d.id) {
                  <option [value]="d.id">[{{ d.code }}] {{ d.name }} (Nivel {{ d.level }})</option>
                }
              </select>
              <div
                class="mt-1.5 text-[12px] font-bold min-h-10 border   rounded-lg  px-2 py-1 flex items-center gap-1"
                [ngClass]="
                  !selectedDigecobOptionForVinc()
                    ? 'bg-slate-100 text-slate-500'
                    : 'bg-emerald-100 text-emerald-700'
                "
              >
                @if (!selectedDigecobOptionForVinc()) {
                  <span class="text-slate-500">La cuenta seleccionada aparecerá aquí</span>
                }
                @if (selectedDigecobOptionForVinc(); as sel) {
                  ✓ Cuenta Seleccionada: [{{ sel.code }}] {{ sel.name }}
                }
              </div>
            </div>
          } @else if (selectedDigecobForVinc()) {
            <div class="p-3 bg-indigo-50/80 rounded-xl text-xs space-y-1 border border-indigo-100">
              <div class="font-bold text-indigo-900">Cuenta DIGECOB a Vincular:</div>
              <div class="font-mono text-indigo-700 font-bold">
                {{ selectedDigecobForVinc()?.digecobAccountCode }} —
                {{ selectedDigecobForVinc()?.digecobAccountName }}
              </div>
              @if (selectedDigecobForVinc()?.isInherited) {
                <div class="text-[11px] text-blue-700 font-medium mt-1">
                  ℹ️ Actualmente hereda el link de la cuenta padre
                  {{ selectedDigecobForVinc()?.inheritedFromDigecobCode }}. Al guardar, se
                  establecerá una vinculación directa específica.
                </div>
              }
            </div>
          }

          <div>
            <label class="block text-xs font-bold text-slate-700 mb-1">
              {{
                isNewVincMode()
                  ? '2. Seleccionar Cuenta MasterSoft (Destino)'
                  : 'Seleccionar Cuenta MasterSoft de Destino'
              }}
            </label>
            <input
              type="text"
              class="field-control w-full text-xs mb-2"
              placeholder="Escriba para filtrar cuentas MasterSoft (código o nombre)..."
              [ngModel]="pickerSearch()"
              (ngModelChange)="pickerSearch.set($event); filtrarPickerMastersoft()"
            />

            <select
              class="field-control w-full text-xs h-36"
              size="5"
              [ngModel]="selectedMastersoftIdForVinc()"
              (ngModelChange)="selectedMastersoftIdForVinc.set($event)"
            >
              @for (m of mastersoftOptionsFiltered(); track m.id) {
                <option [value]="m.id">
                  [{{ m.code }}] {{ m.name }} (Control: {{ m.controlAccountCode || '—' }})
                </option>
              }
            </select>
            <div
              class="mt-1.5 text-[12px] font-bold min-h-10 border   rounded-lg  px-2 py-1 flex items-center gap-1"
              [ngClass]="
                !selectedMastersoftOptionForVinc()
                  ? 'bg-slate-100 text-slate-500'
                  : 'bg-emerald-100 text-emerald-700'
              "
            >
              @if (!selectedMastersoftOptionForVinc()) {
                <span class="text-slate-500">La cuenta seleccionada aparecerá aquí</span>
              }
              @if (selectedMastersoftOptionForVinc(); as sel) {
                ✓ Cuenta Seleccionada: [{{ sel.code }}] {{ sel.name }}
              }
            </div>
          </div>

          <div class="flex justify-end gap-3 pt-2">
            <button type="button" (click)="isVincModalOpen.set(false)" class="btn-custom-outline">
              Cancelar
            </button>
            <button
              type="button"
              (click)="guardarVinculacion()"
              [disabled]="
                (isNewVincMode() && !selectedDigecobIdForVinc()) || !selectedMastersoftIdForVinc()
              "
              class="btn-custom-primary"
            >
              Guardar Vinculación
            </button>
          </div>
        </div>
      </div>
    }

    <!-- Vinculación Delete Confirmation Modal -->
    @if (isVincDeleteModalOpen()) {
      <div
        class="fixed inset-0 z-50 bg-slate-900/50 backdrop-blur-sm flex items-center justify-center p-4"
      >
        <div
          class="bg-white rounded-2xl max-w-sm w-full p-6 shadow-2xl border border-slate-200 text-center space-y-4"
        >
          <div
            class="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-rose-100 text-rose-600 font-bold text-xl"
          >
            🗑️
          </div>
          <div>
            <h3 class="text-base font-bold text-slate-900">¿Eliminar Vinculación Directa?</h3>
            <p class="text-xs text-slate-500 mt-1">
              Estás a punto de eliminar la vinculación directa de la cuenta DIGECOB
              <b class="font-mono text-slate-800">{{ vincToDelete()?.digecobAccountCode }}</b> -
              {{ vincToDelete()?.digecobAccountName }}.
            </p>
            <p class="text-[11px] text-blue-600 font-medium mt-2">
              ℹ️ Si existe una cuenta padre vinculada, esta cuenta pasará a heredar la vinculación
              del padre automáticamente.
            </p>
          </div>

          <div class="flex justify-center gap-3 pt-2">
            <button
              type="button"
              (click)="isVincDeleteModalOpen.set(false)"
              class="btn-custom-outline w-full"
            >
              Cancelar
            </button>
            <button
              type="button"
              (click)="ejecutarEliminarVinculacion()"
              class="btn-custom-primary !bg-rose-600 hover:!bg-rose-700 w-full"
            >
              Sí, Eliminar Vinculación
            </button>
          </div>
        </div>
      </div>
    }
  `,
})
export class ConfigCatalogosRegulatoriosComponent implements OnInit {
  private readonly regulatoryService = inject(RegulatoryCatalogService);
  private readonly unifiedCodeService = inject(UnifiedCodeService);
  private readonly fundingService = inject(FundingSourceService);

  protected readonly pestana = signal<Pestana>('unspsc');
  protected readonly busqueda = signal('');
  protected readonly soloImputables = signal(false);
  protected readonly cargando = signal(false);
  protected readonly error = signal('');
  protected readonly isAdminMode = signal(true);

  // MasterSoft & Vinculación Signals
  protected readonly mastersoftCargando = signal(false);
  protected readonly mastersoftError = signal('');
  protected readonly mastersoftExito = signal('');
  protected readonly listaMastersoft = signal<MastersoftAccountDto[]>([]);
  protected readonly totalMastersoft = signal(0);
  protected readonly busquedaMastersoft = signal('');

  protected readonly vincCargando = signal(false);
  protected readonly vincError = signal('');
  protected readonly vincExito = signal('');
  protected readonly listaVinculacion = signal<MastersoftDigecobMappingDto[]>([]);
  protected readonly totalVinculacion = signal(0);
  protected readonly busquedaVinculacion = signal('');
  protected readonly filtroModoVinculacion = signal<string>('all');

  // Picker & Link Modal State
  protected readonly isVincModalOpen = signal(false);
  protected readonly isNewVincMode = signal(false);
  protected readonly selectedDigecobForVinc = signal<MastersoftDigecobMappingDto | null>(null);
  protected readonly selectedDigecobIdForVinc = signal<string | null>(null);
  protected readonly selectedMastersoftIdForVinc = signal<string | null>(null);

  protected readonly digecobOptions = signal<AccountingAccountDto[]>([]);
  protected readonly digecobOptionsFiltered = signal<AccountingAccountDto[]>([]);
  protected readonly digecobPickerSearch = signal('');

  protected readonly selectedDigecobOptionForVinc = computed(
    () => this.digecobOptions().find((d) => d.id === this.selectedDigecobIdForVinc()) ?? null,
  );
  protected readonly selectedMastersoftOptionForVinc = computed(
    () => this.mastersoftOptions().find((m) => m.id === this.selectedMastersoftIdForVinc()) ?? null,
  );

  protected readonly mastersoftOptions = signal<MastersoftAccountDto[]>([]);
  protected readonly mastersoftOptionsFiltered = signal<MastersoftAccountDto[]>([]);
  protected readonly pickerSearch = signal('');

  protected readonly isVincDeleteModalOpen = signal(false);
  protected readonly vincToDelete = signal<MastersoftDigecobMappingDto | null>(null);

  // Search input bound to user typing (only triggers search on Enter or "Filtrar" click)
  busquedaInput = '';

  // Tree nodes state
  protected readonly rootNodes = signal<TreeNode[]>([]);
  protected activeKebabMenuId: string | null = null;

  // Modals state
  isItemModalOpen = false;
  modalMode: 'create' | 'edit' = 'create';
  editingItemId: string | null = null;
  parentForNewChild: TreeNode | null = null;
  formCode = '';
  formName = '';
  formDetail = '';
  formLevel = 'Class';
  formMarca = true;

  isDeleteModalOpen = false;
  selectedNodeForDelete: TreeNode | null = null;

  // ─── Fuentes de Financiamiento State ───
  protected readonly listaFuentes = signal<FundingSourceDto[]>([]);
  protected readonly fuenteCargando = signal(false);
  protected readonly fuenteError = signal('');
  protected readonly fuenteExito = signal('');
  protected readonly fuenteModalAbierto = signal(false);
  protected readonly fuenteDeleteModalAbierto = signal(false);
  protected readonly fuenteParaEliminar = signal<FundingSourceDto | null>(null);
  fuenteBusqueda = '';
  fuenteEditandoId: string | null = null;
  fuenteFormCode = '';
  fuenteFormName = '';
  fuenteFormDesc = '';
  fuenteFormActiva = true;

  protected readonly tituloPestana = computed(() => {
    switch (this.pestana()) {
      case 'unspsc':
        return 'UNSPSC (DGCP)';
      case 'objetos':
        return 'Objeto del Gasto (DIGEPRES)';
      case 'cuentas':
        return 'Plan de Cuentas (DIGECOB)';
      case 'fuentes':
        return 'Fuentes de Financiamiento';
      case 'mastersoft':
        return 'Catálogo MasterSoft';
      case 'mastersoft-vinc':
        return 'Vinculación MasterSoft';
      default:
        return 'Catálogo';
    }
  });

  protected readonly placeholder = computed(() =>
    this.pestana() === 'unspsc'
      ? 'Buscar por código UNSPSC o descripción…'
      : this.pestana() === 'objetos'
        ? 'Buscar por código o denominación presupuestaria…'
        : 'Buscar por cuenta o nombre contable…',
  );

  /** Aplanar el árbol jerárquico respetando el estado isExpanded */
  protected readonly visibleNodes = computed<TreeNode[]>(() => {
    const list: TreeNode[] = [];

    const flatten = (nodes: TreeNode[]) => {
      for (const node of nodes) {
        list.push(node);
        if (node.isExpanded && node.children && node.children.length > 0) {
          flatten(node.children);
        }
      }
    };

    flatten(this.rootNodes());
    return list;
  });

  ngOnInit(): void {
    this.cargar();
  }

  protected cambiar(p: Pestana): void {
    if (this.pestana() === p) return;
    this.pestana.set(p);
    this.busquedaInput = '';
    this.busqueda.set('');
    this.soloImputables.set(false);
    if (p === 'fuentes') {
      this.cargarFuentes();
    } else {
      this.cargar();
    }
  }

  protected ejecutarBusqueda(): void {
    this.busqueda.set(this.busquedaInput.trim());
    this.cargar();
  }

  protected limpiarBusqueda(): void {
    this.busquedaInput = '';
    this.busqueda.set('');
    this.cargar();
  }

  protected alternarFiltro(valor: boolean): void {
    this.soloImputables.set(valor);
    this.cargar();
  }

  getIndentDepth(node: TreeNode): number {
    if (this.pestana() === 'unspsc' && node.nivel) {
      switch (node.nivel.toLowerCase()) {
        case 'segment':
          return 0;
        case 'family':
          return 1;
        case 'class':
          return 2;
        case 'material':
          return 3;
        default:
          return node.nivelNum || 0;
      }
    }
    return node.nivelNum || 0;
  }

  getIndentPadding(node: TreeNode): number {
    const depth = this.getIndentDepth(node);
    return depth * 36 + 16;
  }

  formatNivelLabel(nivel: string): string {
    if (!nivel) return '';
    switch (nivel.toLowerCase()) {
      case 'segment':
        return 'Segmento';
      case 'family':
        return 'Familia';
      case 'class':
        return 'Clase';
      case 'material':
        return 'Material';
      default:
        return nivel;
    }
  }

  toggleKebabMenu(id: string, event: Event): void {
    event.stopPropagation();
    this.activeKebabMenuId = this.activeKebabMenuId === id ? null : id;
  }

  /** Expande o colapsa un nodo e invoca recarga de señal y petición asíncrona de hijos */
  toggleExpand(node: TreeNode, event?: Event): void {
    if (event) event.stopPropagation();
    node.isExpanded = !node.isExpanded;
    this.triggerTreeRefresh();

    if (node.isExpanded && !node.childrenLoaded && this.pestana() === 'unspsc') {
      node.isLoadingChildren = true;
      const currentDepth = this.getIndentDepth(node);

      this.unifiedCodeService.list({ parentId: node.id, pageSize: 200 }).subscribe({
        next: (children) => {
          node.children = (children ?? []).map((ch) => ({
            id: ch.id,
            codigo: ch.code,
            nombre: ch.name,
            nivel: ch.level,
            nivelNum: currentDepth + 1,
            marca: ch.institutionallyEnabled,
            parentId: ch.parentId,
            childCount: ch.childCount,
            isExpanded: false,
            isLoadingChildren: false,
            childrenLoaded: false,
            children: [],
            rawItem: ch,
          }));
          node.childrenLoaded = true;
          node.isLoadingChildren = false;
          this.triggerTreeRefresh();
        },
        error: () => {
          node.isLoadingChildren = false;
          this.triggerTreeRefresh();
        },
      });
    }
  }

  expandirTodo(): void {
    const expandRec = (nodes: TreeNode[]) => {
      for (const n of nodes) {
        n.isExpanded = true;
        if (n.children) expandRec(n.children);
      }
    };
    expandRec(this.rootNodes());
    this.triggerTreeRefresh();
  }

  colapsarTodo(): void {
    const collapseRec = (nodes: TreeNode[]) => {
      for (const n of nodes) {
        n.isExpanded = false;
        if (n.children) collapseRec(n.children);
      }
    };
    collapseRec(this.rootNodes());
    this.triggerTreeRefresh();
  }

  private triggerTreeRefresh(): void {
    this.rootNodes.set([...this.rootNodes()]);
  }

  private cargar(): void {
    this.cargando.set(true);
    this.error.set('');
    const q = this.busqueda().trim() || undefined;
    const filtro = this.soloImputables() ? true : undefined;

    if (this.pestana() === 'unspsc') {
      this.unifiedCodeService.list({ q, pageSize: q ? 100 : 200 }).subscribe({
        next: (items) => {
          const nodes: TreeNode[] = (items ?? []).map((c) => ({
            id: c.id,
            codigo: c.code,
            nombre: c.name,
            nivel: c.level,
            nivelNum: 0,
            marca: c.institutionallyEnabled,
            parentId: c.parentId,
            childCount: c.childCount,
            isExpanded: !!q,
            isLoadingChildren: false,
            childrenLoaded: false,
            children: [],
            matchedInSearch:
              !!q &&
              (c.name.toLowerCase().includes(q.toLowerCase()) ||
                c.code.toLowerCase().includes(q.toLowerCase())),
            rawItem: c,
          }));
          this.rootNodes.set(nodes);
          this.cargando.set(false);
        },
        error: () => this.fallo('No se pudo cargar el catálogo UNSPSC.'),
      });
      return;
    }

    if (this.pestana() === 'objetos') {
      this.regulatoryService.listExpenseObjects(q, filtro, 1, 1000).subscribe({
        next: (res) => {
          const items = (res as any).items ?? res.value ?? [];
          const tree = this.buildHierarchicalTreeFromFlatList(
            items.map((o: any) => ({
              id: o.id,
              codigo: o.code,
              nombre: o.name,
              detalle: o.description ?? undefined,
              nivel: `Nivel ${o.level}`,
              marca: o.isDetail,
              parentCode: o.parentCode,
              rawItem: o,
            })),
            q,
          );
          this.rootNodes.set(tree);
          this.cargando.set(false);
        },
        error: () => this.fallo('No se pudo cargar el catálogo de objetos del gasto.'),
      });
      return;
    }

    if (this.pestana() === 'mastersoft') {
      this.cargarMastersoft();
      return;
    }

    if (this.pestana() === 'mastersoft-vinc') {
      this.cargarVinculacion();
      return;
    }

    this.regulatoryService.listAccountingAccounts(q, filtro, 1, 5000).subscribe({
      next: (res) => {
        const items = (res as any).items ?? res.value ?? [];
        const tree = this.buildHierarchicalTreeFromFlatList(
          items.map((a: any) => ({
            id: a.id,
            codigo: a.code,
            nombre: a.name,
            detalle: a.extendedDescription ?? undefined,
            nivel: `Nivel ${a.level}`,
            marca: a.isPostable,
            parentCode: a.parentCode,
            rawItem: a,
          })),
          q,
        );
        this.rootNodes.set(tree);
        this.cargando.set(false);
      },
      error: () => this.fallo('No se pudo cargar el plan de cuentas.'),
    });
  }

  /** Construye la jerarquía padre-hijo a partir de códigos (ej: 2 -> 2.1 -> 2.1.1 -> 2.1.1.1) */
  private buildHierarchicalTreeFromFlatList(flatList: any[], q?: string): TreeNode[] {
    const nodeMap = new Map<string, TreeNode>();
    const roots: TreeNode[] = [];

    for (const item of flatList) {
      const node: TreeNode = {
        id: item.id,
        codigo: item.codigo,
        nombre: item.nombre,
        detalle: item.detalle,
        nivel: item.nivel,
        nivelNum: 0,
        marca: item.marca,
        parentId: null,
        childCount: 0,
        isExpanded: !!q,
        isLoadingChildren: false,
        childrenLoaded: true,
        children: [],
        matchedInSearch:
          !!q &&
          (item.nombre.toLowerCase().includes(q.toLowerCase()) ||
            item.codigo.toLowerCase().includes(q.toLowerCase())),
        rawItem: item.rawItem,
      };
      nodeMap.set(item.codigo, node);
    }

    for (const item of flatList) {
      const node = nodeMap.get(item.codigo)!;
      const parentCode = item.parentCode || itemParentCode(node.codigo);
      if (parentCode && nodeMap.has(parentCode)) {
        const parentNode = nodeMap.get(parentCode)!;
        node.nivelNum = parentNode.nivelNum + 1;
        parentNode.children.push(node);
        parentNode.childCount++;
        if (node.matchedInSearch) {
          parentNode.isExpanded = true;
        }
      } else {
        node.isExpanded = true;
        roots.push(node);
      }
    }

    return roots;
  }

  private fallo(mensaje: string): void {
    this.error.set(mensaje);
    this.cargando.set(false);
  }

  // --- CRUD MODALS & ACTIONS ---

  openCreateModal(): void {
    this.modalMode = 'create';
    this.editingItemId = null;
    this.parentForNewChild = null;
    this.formCode = '';
    this.formName = '';
    this.formDetail = '';
    this.formLevel = this.pestana() === 'unspsc' ? 'Class' : '4';
    this.formMarca = true;
    this.isItemModalOpen = true;
  }

  openAddChildModal(parent: TreeNode): void {
    this.activeKebabMenuId = null;
    this.modalMode = 'create';
    this.editingItemId = null;
    this.parentForNewChild = parent;
    this.formCode = `${parent.codigo}.`;
    this.formName = '';
    this.formDetail = '';

    if (this.pestana() === 'unspsc') {
      const parentLvl = (parent.nivel || '').toLowerCase();
      if (parentLvl === 'segment') this.formLevel = 'Family';
      else if (parentLvl === 'family') this.formLevel = 'Class';
      else this.formLevel = 'Material';
    } else {
      this.formLevel = `Nivel ${parent.nivelNum + 2}`;
    }

    this.formMarca = true;
    this.isItemModalOpen = true;
  }

  openEditModal(node: TreeNode): void {
    this.activeKebabMenuId = null;
    this.modalMode = 'edit';
    this.editingItemId = node.id;
    this.parentForNewChild = null;
    this.formCode = node.codigo;
    this.formName = node.nombre;
    this.formDetail = node.detalle || '';
    this.formLevel = node.nivel;
    this.formMarca = node.marca;
    this.isItemModalOpen = true;
  }

  saveItem(): void {
    if (!this.formCode || !this.formName) return;

    if (this.pestana() === 'unspsc') {
      const dto = {
        code: this.formCode,
        name: this.formName,
        level: this.formLevel,
        institutionallyEnabled: this.formMarca,
        parentId: this.parentForNewChild?.id || null,
      };

      const obs =
        this.modalMode === 'create'
          ? this.unifiedCodeService.create(dto)
          : this.unifiedCodeService.update(this.editingItemId!, dto);

      obs.subscribe({
        next: () => {
          this.isItemModalOpen = false;
          this.cargar();
        },
        error: () => {
          this.isItemModalOpen = false;
          this.cargar();
        },
      });
      return;
    }

    if (this.pestana() === 'objetos') {
      const dto = {
        code: this.formCode,
        name: this.formName,
        description: this.formDetail || null,
        level: parseInt(this.formLevel.replace('Nivel ', ''), 10) || 4,
        isDetail: this.formMarca,
        parentCode: this.parentForNewChild?.codigo || null,
      };

      const obs =
        this.modalMode === 'create'
          ? this.regulatoryService.createExpenseObject(dto)
          : this.regulatoryService.updateExpenseObject(this.editingItemId!, dto);

      obs.subscribe({
        next: () => {
          this.isItemModalOpen = false;
          this.cargar();
        },
        error: () => {
          this.isItemModalOpen = false;
          this.cargar();
        },
      });
      return;
    }

    if (this.pestana() === 'cuentas') {
      const dto = {
        code: this.formCode,
        name: this.formName,
        extendedDescription: this.formDetail || null,
        level: parseInt(this.formLevel.replace('Nivel ', ''), 10) || 4,
        isPostable: this.formMarca,
        parentCode: this.parentForNewChild?.codigo || null,
      };

      const obs =
        this.modalMode === 'create'
          ? this.regulatoryService.createAccountingAccount(dto)
          : this.regulatoryService.updateAccountingAccount(this.editingItemId!, dto);

      obs.subscribe({
        next: () => {
          this.isItemModalOpen = false;
          this.cargar();
        },
        error: () => {
          this.isItemModalOpen = false;
          this.cargar();
        },
      });
    }
  }

  openDeleteModal(node: TreeNode): void {
    this.activeKebabMenuId = null;
    this.selectedNodeForDelete = node;
    this.isDeleteModalOpen = true;
  }

  executeDelete(): void {
    if (!this.selectedNodeForDelete) return;
    const id = this.selectedNodeForDelete.id;

    let obs;
    if (this.pestana() === 'unspsc') {
      obs = this.unifiedCodeService.delete(id);
    } else if (this.pestana() === 'objetos') {
      obs = this.regulatoryService.deleteExpenseObject(id);
    } else {
      obs = this.regulatoryService.deleteAccountingAccount(id);
    }

    obs.subscribe({
      next: () => {
        this.isDeleteModalOpen = false;
        this.cargar();
      },
      error: () => {
        this.isDeleteModalOpen = false;
        this.cargar();
      },
    });
  }

  // ─────────────────────── FUENTES DE FINANCIAMIENTO CRUD ───────────────────────

  protected cargarFuentes(): void {
    this.fuenteCargando.set(true);
    this.fuenteError.set('');
    const q = this.fuenteBusqueda.trim() || undefined;
    this.fundingService.list(q).subscribe({
      next: (items) => {
        this.listaFuentes.set(items);
        this.fuenteCargando.set(false);
      },
      error: () => {
        this.fuenteError.set('Error al cargar las fuentes de financiamiento.');
        this.fuenteCargando.set(false);
      },
    });
  }

  protected abrirModalFuente(): void {
    this.fuenteEditandoId = null;
    this.fuenteFormCode = '';
    this.fuenteFormName = '';
    this.fuenteFormDesc = '';
    this.fuenteFormActiva = true;
    this.fuenteModalAbierto.set(true);
  }

  protected editarFuente(f: FundingSourceDto): void {
    this.fuenteEditandoId = f.id;
    this.fuenteFormCode = f.code;
    this.fuenteFormName = f.name;
    this.fuenteFormDesc = f.description || '';
    this.fuenteFormActiva = f.isActive;
    this.fuenteModalAbierto.set(true);
  }

  protected eliminarFuente(f: FundingSourceDto): void {
    this.fuenteParaEliminar.set(f);
    this.fuenteDeleteModalAbierto.set(true);
  }

  protected guardarFuente(): void {
    const code = this.fuenteFormCode.trim();
    const name = this.fuenteFormName.trim();
    if (!code || !name) return;

    if (this.fuenteEditandoId) {
      const dto: UpdateFundingSourceDto = {
        code,
        name,
        description: this.fuenteFormDesc.trim() || null,
        isActive: this.fuenteFormActiva,
      };
      this.fundingService.update(this.fuenteEditandoId, dto).subscribe({
        next: () => {
          this.fuenteModalAbierto.set(false);
          this.mostrarFuenteExito('Fuente de financiamiento actualizada exitosamente.');
          this.cargarFuentes();
        },
        error: (err) => this.fuenteError.set(err.error?.detail || 'Error al actualizar la fuente.'),
      });
    } else {
      const dto: CreateFundingSourceDto = {
        code,
        name,
        description: this.fuenteFormDesc.trim() || null,
        isActive: this.fuenteFormActiva,
      };
      this.fundingService.create(dto).subscribe({
        next: () => {
          this.fuenteModalAbierto.set(false);
          this.mostrarFuenteExito('Fuente de financiamiento creada exitosamente.');
          this.cargarFuentes();
        },
        error: (err) => this.fuenteError.set(err.error?.detail || 'Error al crear la fuente.'),
      });
    }
  }

  protected confirmarEliminarFuente(): void {
    const f = this.fuenteParaEliminar();
    if (!f) return;

    this.fundingService.delete(f.id).subscribe({
      next: () => {
        this.fuenteDeleteModalAbierto.set(false);
        this.mostrarFuenteExito(`Fuente '${f.code} - ${f.name}' eliminada.`);
        this.cargarFuentes();
      },
      error: () => this.fuenteError.set('Error al eliminar la fuente de financiamiento.'),
    });
  }

  private mostrarFuenteExito(msg: string): void {
    this.fuenteExito.set(msg);
    this.fuenteError.set('');
    setTimeout(() => this.fuenteExito.set(''), 5000);
  }

  // ─────────────────────── MASTERSOFT & VINCULACIÓN METODOS ───────────────────────

  protected cargarMastersoft(): void {
    this.mastersoftCargando.set(true);
    this.regulatoryService.listMastersoftAccounts(this.busquedaMastersoft(), 1, 5000).subscribe({
      next: (res) => {
        const items = (res as any).items ?? res.value ?? [];
        const total = (res as any).totalCount ?? res.totalRecords ?? items.length;
        this.listaMastersoft.set(items);
        this.totalMastersoft.set(total);
        this.mastersoftCargando.set(false);
      },
      error: () => {
        this.mastersoftError.set('Error al cargar el catálogo MasterSoft');
        this.mastersoftCargando.set(false);
      },
    });
  }

  protected cargarVinculacion(): void {
    this.vincCargando.set(true);
    const q = this.busquedaVinculacion().trim() || undefined;
    const filterMode =
      this.filtroModoVinculacion() !== 'all' ? this.filtroModoVinculacion() : undefined;

    this.regulatoryService.listMastersoftMappings(q, filterMode, 1, 5000).subscribe({
      next: (res) => {
        const items = (res as any).items ?? res.value ?? [];
        const total = (res as any).totalCount ?? res.totalRecords ?? items.length;
        this.listaVinculacion.set(items);
        this.totalVinculacion.set(total);
        this.vincCargando.set(false);
      },
      error: () => {
        this.vincError.set('Error al cargar la tabla de vinculación');
        this.vincCargando.set(false);
      },
    });
  }

  protected abrirModalNuevaVinc(): void {
    this.isNewVincMode.set(true);
    this.selectedDigecobForVinc.set(null);
    this.selectedDigecobIdForVinc.set(null);
    this.selectedMastersoftIdForVinc.set(null);
    this.pickerSearch.set('');
    this.digecobPickerSearch.set('');

    this.regulatoryService.listAccountingAccounts(undefined, undefined, 1, 5000).subscribe({
      next: (res) => {
        const list = (res as any).items ?? res.value ?? [];
        this.digecobOptions.set(list);
        this.digecobOptionsFiltered.set(list);
      },
    });

    this.regulatoryService.listMastersoftAccounts(undefined, 1, 5000).subscribe({
      next: (res) => {
        const list = (res as any).items ?? res.value ?? [];
        this.mastersoftOptions.set(list);
        this.mastersoftOptionsFiltered.set(list);
        this.isVincModalOpen.set(true);
      },
    });
  }

  protected abrirModalVinc(item: MastersoftDigecobMappingDto): void {
    this.isNewVincMode.set(false);
    this.selectedDigecobForVinc.set(item);
    this.selectedDigecobIdForVinc.set(item.digecobAccountId);
    this.selectedMastersoftIdForVinc.set(item.mastersoftAccountId || null);
    this.pickerSearch.set('');

    this.regulatoryService.listMastersoftAccounts(undefined, 1, 5000).subscribe({
      next: (res) => {
        const list = (res as any).items ?? res.value ?? [];
        this.mastersoftOptions.set(list);
        this.mastersoftOptionsFiltered.set(list);
        this.isVincModalOpen.set(true);
      },
    });
  }

  protected filtrarPickerDigecob(): void {
    const q = this.digecobPickerSearch().toLowerCase().trim();
    if (!q) {
      this.digecobOptionsFiltered.set(this.digecobOptions());
    } else {
      this.digecobOptionsFiltered.set(
        this.digecobOptions().filter(
          (a: any) => a.code.toLowerCase().includes(q) || a.name.toLowerCase().includes(q),
        ),
      );
    }
  }

  protected filtrarPickerMastersoft(): void {
    const q = this.pickerSearch().toLowerCase().trim();
    if (!q) {
      this.mastersoftOptionsFiltered.set(this.mastersoftOptions());
    } else {
      this.mastersoftOptionsFiltered.set(
        this.mastersoftOptions().filter(
          (m: any) => m.code.toLowerCase().includes(q) || m.name.toLowerCase().includes(q),
        ),
      );
    }
  }

  protected guardarVinculacion(): void {
    const digId = this.isNewVincMode()
      ? this.selectedDigecobIdForVinc()
      : this.selectedDigecobForVinc()?.digecobAccountId;
    const msId = this.selectedMastersoftIdForVinc();
    if (!digId || !msId) return;

    this.regulatoryService
      .createOrUpdateMastersoftMapping({
        digecobAccountId: digId,
        mastersoftAccountId: msId,
      })
      .subscribe({
        next: () => {
          this.isVincModalOpen.set(false);
          this.vincExito.set('Vinculación guardada exitosamente.');
          setTimeout(() => this.vincExito.set(''), 4000);
          this.cargarVinculacion();
        },
        error: () => {
          this.vincError.set('Error al guardar la vinculación.');
        },
      });
  }

  protected abrirConfirmarEliminarVinc(item: MastersoftDigecobMappingDto): void {
    this.vincToDelete.set(item);
    this.isVincDeleteModalOpen.set(true);
  }

  protected ejecutarEliminarVinculacion(): void {
    const v = this.vincToDelete();
    if (!v) return;

    const idToDelete = v.id || v.digecobAccountId;

    this.regulatoryService.deleteMastersoftMapping(idToDelete).subscribe({
      next: () => {
        this.isVincDeleteModalOpen.set(false);
        this.vincToDelete.set(null);
        this.vincExito.set(`Vinculación directa de la cuenta ${v.digecobAccountCode} eliminada.`);
        setTimeout(() => this.vincExito.set(''), 4000);
        this.cargarVinculacion();
      },
      error: () => {
        this.isVincDeleteModalOpen.set(false);
        this.vincError.set('Error al eliminar la vinculación.');
      },
    });
  }

  protected onImportMastersoftFileSelected(event: any): void {
    const file = event.target.files?.[0];
    if (!file) return;

    this.vincCargando.set(true);
    this.regulatoryService.importMastersoftCatalog(file).subscribe({
      next: (summary) => {
        this.vincCargando.set(false);
        this.vincExito.set(
          `Importación exitosa: ${summary.mappingsCreated} vinculaciones creadas, ${summary.mappingsUpdated} actualizadas.`,
        );
        setTimeout(() => this.vincExito.set(''), 5000);
        this.cargarVinculacion();
      },
      error: () => {
        this.vincCargando.set(false);
        this.vincError.set('Error al procesar el archivo Excel.');
      },
    });
  }

  protected exportarHomologacionMastersoft(): void {
    this.regulatoryService.exportMastersoftHomologationReport().subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `Homologacion_DIGECOB_MasterSoft_${new Date().toISOString().slice(0, 10)}.xlsx`;
        a.click();
        window.URL.revokeObjectURL(url);
      },
      error: () => {
        this.vincError.set('Error al generar la exportación de homologación.');
      },
    });
  }
}

/** Helper para obtener el código padre de un código estructurado (ej. '2.6.1.1' -> '2.6.1') */
function itemParentCode(code: string): string | null {
  if (!code || !code.includes('.')) return null;
  const parts = code.split('.');
  parts.pop();
  return parts.join('.');
}
