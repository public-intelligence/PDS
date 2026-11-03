import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ConsolidatorUnitImpactDto, StructureVersionImpactReportDto } from '../data/planning-api.models';

@Component({
  selector: 'app-consolidator-impact-modal',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="fixed inset-0 z-50 bg-slate-900/60 backdrop-blur-sm flex items-center justify-center p-4">
      <div class="bg-white rounded-2xl max-w-xl w-full p-6 shadow-2xl border border-amber-200 space-y-5 animate-in fade-in zoom-in-95 duration-100">
        <!-- Header -->
        <div class="flex items-center gap-3 text-amber-600 border-b border-amber-100 pb-4">
          <div class="p-3 rounded-full bg-amber-50 border border-amber-200 text-amber-600">
            <i class="bi bi-shield-exclamation text-2xl"></i>
          </div>
          <div>
            <h3 class="text-base font-bold text-slate-900">
              {{ isUnitMode ? 'Advertencia de Gobernanza: Unidad Consolidadora' : 'Reporte de Impacto de Versión de Organigrama' }}
            </h3>
            <p class="text-xs text-slate-500">
              Se han detectado afectaciones en las reglas de ruteo y en el catálogo maestro.
            </p>
          </div>
        </div>

        <!-- Unit Mode Content -->
        @if (isUnitMode && unitImpact) {
          <div class="space-y-3 text-xs">
            <div class="p-3 bg-amber-50/70 border border-amber-200 rounded-xl space-y-1 text-amber-900">
              <span class="font-bold">Unidad Afectada:</span> {{ unitImpact.unitName }} ({{ unitImpact.code || 'Sin código' }})
              <p class="text-[11px] leading-relaxed text-amber-800 pt-1">
                Esta unidad organizativa actúa como <b>Área Consolidadora</b>. Modificarla o eliminarla causará una fractura en el ruteo automático.
              </p>
            </div>

            <div class="grid grid-cols-2 gap-3 pt-1">
              <div class="p-3 bg-slate-50 border border-slate-200 rounded-xl text-center">
                <div class="text-lg font-bold text-amber-600">{{ unitImpact.affectedRoutingRulesCount }}</div>
                <div class="text-[11px] text-slate-600">Reglas de Ruteo Afectadas</div>
                @if (unitImpact.affectedRuleKeys.length > 0) {
                  <div class="flex flex-wrap justify-center gap-1 mt-1.5">
                    @for (key of unitImpact.affectedRuleKeys; track key) {
                      <span class="px-1.5 py-0.5 bg-amber-100 text-amber-800 text-[10px] font-mono rounded">
                        {{ key }}
                      </span>
                    }
                  </div>
                }
              </div>

              <div class="p-3 bg-slate-50 border border-slate-200 rounded-xl text-center">
                <div class="text-lg font-bold text-sky-600">{{ unitImpact.affectedMasterProductsCount | number }}</div>
                <div class="text-[11px] text-slate-600">Artículos en Catálogo Maestro</div>
                <div class="text-[10px] text-slate-400 mt-1">Requerirán re-evaluación</div>
              </div>
            </div>
          </div>
        }

        <!-- Version Mode Content -->
        @if (!isUnitMode && versionReport) {
          <div class="space-y-3 text-xs">
            <div class="p-3 bg-amber-50/70 border border-amber-200 rounded-xl space-y-1 text-amber-900">
              <div class="font-bold">Versión {{ versionReport.versionNumber ? ('v' + versionReport.versionNumber) : '' }}: {{ versionReport.versionName }}</div>
              <p class="text-[11px] leading-relaxed text-amber-800 pt-1">
                Publicar esta versión modificará <b>{{ versionReport.totalConsolidatorAreasAffected }}</b> Áreas Consolidadoras, afectando a <b>{{ versionReport.totalRoutingRulesAffected }}</b> reglas de ruteo y <b>{{ versionReport.totalMasterProductsAffected | number }}</b> artículos institucionales.
              </p>
            </div>

            <div class="max-h-52 overflow-y-auto space-y-2 pr-1">
              @for (unit of versionReport.affectedUnits; track unit.unitId) {
                <div class="p-2.5 bg-slate-50 border border-slate-200 rounded-xl flex justify-between items-center text-xs">
                  <div>
                    <div class="font-bold text-slate-800">{{ unit.unitName }}</div>
                    <div class="text-[11px] text-slate-500">
                      {{ unit.affectedRoutingRulesCount }} reglas | {{ unit.affectedMasterProductsCount }} artículos
                    </div>
                  </div>
                  <span class="px-2 py-0.5 rounded text-[10px] font-bold bg-amber-100 text-amber-800">
                    Afectada
                  </span>
                </div>
              }
            </div>
          </div>
        }

        <!-- Recommendations -->
        <div class="p-3 bg-sky-50 border border-sky-200 rounded-xl text-xs text-sky-900 flex items-start gap-2">
          <i class="bi bi-lightbulb-fill text-sky-600 text-base mt-0.5"></i>
          <div>
            <span class="font-bold text-sky-950">Recomendación de Gobernanza:</span>
            <p class="text-[11px] text-sky-800 mt-0.5">
              Tras confirmar esta modificación en el organigrama, visite la pantalla de <b>Reglas de Ruteo</b> para redirigir las reglas afectadas a una nueva Área Consolidadora viva y ejecute la re-evaluación masiva.
            </p>
          </div>
        </div>

        <!-- Footer Actions -->
        <div class="flex justify-end gap-3 pt-3 border-t border-slate-200">
          <button type="button" (click)="cancel.emit()" class="btn-outline text-xs">
            Cancelar Cambio
          </button>
          <button type="button" (click)="confirm.emit()" class="btn-primary !bg-amber-600 hover:!bg-amber-700 text-xs flex items-center gap-1.5">
            <i class="bi bi-check-lg"></i>
            Entendido y Confirmar
          </button>
        </div>
      </div>
    </div>
  `
})
export class ConsolidatorImpactModalComponent {
  @Input() isUnitMode = true;
  @Input() unitImpact: ConsolidatorUnitImpactDto | null = null;
  @Input() versionReport: StructureVersionImpactReportDto | null = null;

  @Output() confirm = new EventEmitter<void>();
  @Output() cancel = new EventEmitter<void>();
}
