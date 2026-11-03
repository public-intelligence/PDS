import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { PageShellComponent } from '@public-intelligence/ui-core';
import { MasterProductService } from '../../../../services/planning/master-product.service';
import { RegulatoryCatalogService } from '../../../../services/planning/regulatory-catalog.service';
import { ConsolidatorRoutingService } from '../../../../services/planning/consolidator-routing.service';
import { NotificationService } from '../../../../services/notification/notification.service';
import {
  ConsolidatorAreaProfileDto,
  MasterProductDto,
  MasterProductMappingSummaryDto,
} from '../../data/planning-api.models';

type Semaforo = 'completo' | 'revisar' | 'falta' | 'duplicado';

/**
 * Validación de mapeo: la pantalla donde Planificación cierra las brechas del
 * catálogo institucional. La ratificación masiva por rama UNSPSC es la palanca
 * para convertir miles de artículos «pendientes de definición» en utilizables.
 */
@Component({
  selector: 'app-config-validacion-mapeo',
  imports: [FormsModule, PageShellComponent, DecimalPipe],
  template: `
    <app-page-shell
      title="Validación de mapeo"
      description="Estado del mapeo regulatorio de la maestra: UNSPSC, objeto del gasto, cuenta contable y área consolidadora."
    >
      <!-- Contadores: son la foto de la deuda de configuración -->
      @if (resumen(); as r) {
        <div class="mb-4 grid grid-cols-2 gap-3 lg:grid-cols-4">
          <button type="button" class="card p-4 text-left hover:ring-1 hover:ring-brand" (click)="filtrarPor('todos')">
            <p class="!text-xs font-medium text-ink-muted">Artículos</p>
            <p class="mt-1 !text-2xl font-bold text-ink">{{ r.totalProducts | number }}</p>
            <p class="!text-xs text-ink-subtle">{{ r.activeProducts | number }} utilizables</p>
          </button>
          <button type="button" class="card p-4 text-left hover:ring-1 hover:ring-brand" (click)="filtrarPor('sin-area')">
            <p class="!text-xs font-medium text-ink-muted">Sin área consolidadora</p>
            <p class="mt-1 !text-2xl font-bold text-warning-text">{{ r.missingConsolidatorArea | number }}</p>
            <p class="!text-xs text-ink-subtle">{{ porcentaje(r.missingConsolidatorArea, r.totalProducts) }}% del catálogo</p>
          </button>
          <button type="button" class="card p-4 text-left hover:ring-1 hover:ring-brand" (click)="filtrarPor('sin-mapeo')">
            <p class="!text-xs font-medium text-ink-muted">Sin objeto / cuenta</p>
            <p class="mt-1 !text-2xl font-bold text-warning-text">{{ r.missingExpenseObject | number }} / {{ r.missingAccountingAccount | number }}</p>
            <p class="!text-xs text-ink-subtle">objeto del gasto / cuenta contable</p>
          </button>
          <button type="button" class="card p-4 text-left hover:ring-1 hover:ring-brand" (click)="filtrarPor('revisar')">
            <p class="!text-xs font-medium text-ink-muted">Requieren revisión</p>
            <p class="mt-1 !text-2xl font-bold text-danger-text">{{ r.requiresReviewCount | number }}</p>
            <p class="!text-xs text-ink-subtle">{{ r.areaAmbiguityCount | number }} con ambigüedad de área</p>
          </button>
        </div>
      }

      <!-- Filtros -->
      <div class="mb-4 flex flex-wrap items-end gap-3">
        <label class="field-label flex-1 min-w-[14rem]">Buscar
          <input class="field-control mt-1" placeholder="Nombre o código del artículo…" [ngModel]="busqueda()" (ngModelChange)="alBuscar($event)" />
        </label>
        <label class="field-label">Rama UNSPSC
          <input class="field-control mt-1" placeholder="Ej. 4321" [ngModel]="prefijo()" (ngModelChange)="alPrefijo($event)" />
        </label>
        <label class="field-label">Estado
          <select class="field-control mt-1" [ngModel]="estado()" (ngModelChange)="alEstado($event)">
            <option value="">Todos</option>
            <option value="Active">Utilizables</option>
            <option value="Pending">Pendientes de definición</option>
            <option value="Requested">Solicitados por usuarios</option>
            <option value="Disabled">Deshabilitados</option>
          </select>
        </label>
        <button type="button" class="btn-ghost btn-sm" (click)="reevaluar()" [disabled]="ocupado()">
          Re-evaluar reglas
        </button>
      </div>

      <!-- Ratificación masiva por rama: la palanca para los miles de pendientes -->
      <div class="card mb-4 p-4">
        <p class="!text-sm font-semibold text-ink">Ratificación masiva por rama UNSPSC</p>
        <p class="mt-0.5 !text-xs text-ink-muted">
          Asigna el área consolidadora a todos los artículos cuyo código UNSPSC empiece por el prefijo
          indicado. La ratificación es manual: queda congelada frente al motor de reglas.
        </p>
        <div class="mt-2 flex flex-wrap items-end gap-2">
          <label class="field-label">Prefijo UNSPSC
            <input class="field-control mt-1" [(ngModel)]="prefijoMasivo" placeholder="Ej. 5610 (mobiliario)" />
          </label>
          <label class="field-label flex-1 min-w-[16rem]">Área consolidadora
            <select class="field-control mt-1" [(ngModel)]="areaMasiva">
              <option value="">— Seleccione —</option>
              @for (a of areasDisponibles(); track a.id) {
                <option [value]="a.organizationalStructureId">{{ a.areaName }}</option>
              }
            </select>
          </label>
          <button
            type="button"
            class="btn-primary btn-sm"
            [disabled]="!prefijoMasivo.trim() || !areaMasiva || ocupado()"
            (click)="ratificarMasivo()"
          >
            {{ ocupado() ? 'Ratificando…' : 'Ratificar rama' }}
          </button>
        </div>
        @if (areasDisponibles().length === 0) {
          <p class="mt-2 !text-xs text-warning-text">
            Ninguna área del catálogo institucional resuelve contra el organigrama todavía. Corra el
            importador de la maestra o revise los nombres de área.
          </p>
        }
      </div>

      @if (mensaje()) {
        <p class="mb-4 rounded-lg bg-success-bg px-3 py-2 text-sm text-success-text ring-1 ring-success-border">{{ mensaje() }}</p>
      }
      @if (error()) {
        <p class="mb-4 rounded-lg bg-danger-bg px-3 py-2 text-sm text-danger-text">{{ error() }}</p>
      }

      <!-- Tabla de mapeo -->
      <div class="card overflow-hidden">
        <div class="overflow-x-auto">
          <table class="w-full min-w-[1100px] text-sm">
            <thead>
              <tr class="border-b border-border bg-surface-sunken text-left">
                <th class="px-4 py-3 text-xs font-semibold uppercase text-ink-muted">Artículo</th>
                <th class="px-4 py-3 text-xs font-semibold uppercase text-ink-muted">UNSPSC</th>
                <th class="px-4 py-3 text-xs font-semibold uppercase text-ink-muted">Objeto del gasto</th>
                <th class="px-4 py-3 text-xs font-semibold uppercase text-ink-muted">Cuenta contable</th>
                <th class="px-4 py-3 text-xs font-semibold uppercase text-ink-muted">Área consolidadora</th>
                <th class="px-4 py-3 text-xs font-semibold uppercase text-ink-muted">Estado</th>
              </tr>
            </thead>
            <tbody>
              @if (cargando()) {
                <tr><td colspan="6" class="px-4 py-12 text-center text-sm text-ink-muted">
                  <span class="mb-2 inline-block size-6 animate-spin rounded-full border-2 border-brand border-t-transparent"></span>
                  Cargando artículos…
                </td></tr>
              } @else {
                @for (p of articulos(); track p.id) {
                  <tr class="border-b border-border last:border-b-0 hover:bg-surface-sunken">
                    <td class="px-4 py-2.5">
                      <span class="font-medium text-ink">{{ p.name }}</span>
                      <span class="block font-mono !text-xs text-ink-subtle">{{ p.code }}</span>
                    </td>
                    <td class="px-4 py-2.5 font-mono !text-xs" [class.text-danger-text]="!p.unspscCode">
                      {{ p.unspscCode || 'falta' }}
                    </td>
                    <td class="px-4 py-2.5 font-mono !text-xs" [class.text-danger-text]="!p.expenseObjectCode">
                      {{ p.expenseObjectCode || 'falta' }}
                    </td>
                    <td class="px-4 py-2.5 font-mono !text-xs" [class.text-danger-text]="!p.accountingAccountCode">
                      {{ p.accountingAccountCode || 'falta' }}
                    </td>
                    <td class="px-4 py-2.5">
                      @if (p.consolidatorAreaName) {
                        <span class="text-ink">{{ p.consolidatorAreaName }}</span>
                        <span class="block !text-xs text-ink-subtle">
                          {{ origen(p) }}
                          @if (p.consolidatorAreaConfidence != null) { · confianza {{ p.consolidatorAreaConfidence }}% }
                        </span>
                      } @else {
                        <span class="text-danger-text">pendiente de definición</span>
                      }
                    </td>
                    <td class="px-4 py-2.5">
                      <span [class]="claseSemaforo(semaforo(p))">{{ etiquetaSemaforo(semaforo(p)) }}</span>
                      @if (p.reviewReason) {
                        <span class="block !text-xs text-ink-subtle" [title]="p.reviewReason">{{ p.reviewReason }}</span>
                      }
                    </td>
                  </tr>
                } @empty {
                  <tr><td colspan="6" class="px-4 py-10 text-center text-sm text-ink-muted">Sin artículos para este filtro.</td></tr>
                }
              }
            </tbody>
          </table>
        </div>

        @if (totalRegistros() > 0) {
          <div class="flex items-center justify-between border-t border-border px-4 py-3 !text-sm text-ink-muted">
            <span>{{ articulos().length }} de {{ totalRegistros() | number }} artículos</span>
            <div class="flex items-center gap-2">
              <button type="button" class="btn-ghost btn-sm" [disabled]="pagina() === 1" (click)="irA(pagina() - 1)">Anterior</button>
              <span class="px-2">Página {{ pagina() }} de {{ totalPaginas() }}</span>
              <button type="button" class="btn-ghost btn-sm" [disabled]="pagina() >= totalPaginas()" (click)="irA(pagina() + 1)">Siguiente</button>
            </div>
          </div>
        }
      </div>
    </app-page-shell>
  `,
})
export class ConfigValidacionMapeoComponent implements OnInit {
  private readonly masterProductService = inject(MasterProductService);
  private readonly regulatoryService = inject(RegulatoryCatalogService);
  private readonly routingService = inject(ConsolidatorRoutingService);
  private readonly notification = inject(NotificationService);

  private readonly tamanoPagina = 25;

  protected readonly resumen = signal<MasterProductMappingSummaryDto | null>(null);
  protected readonly articulos = signal<MasterProductDto[]>([]);
  protected readonly perfilesArea = signal<ConsolidatorAreaProfileDto[]>([]);
  protected readonly totalRegistros = signal(0);
  protected readonly pagina = signal(1);
  protected readonly cargando = signal(false);
  protected readonly ocupado = signal(false);
  protected readonly mensaje = signal('');
  protected readonly error = signal('');

  protected readonly busqueda = signal('');
  protected readonly prefijo = signal('');
  protected readonly estado = signal('');
  private readonly soloSinMapeo = signal<boolean | undefined>(undefined);
  private readonly soloRevisar = signal<boolean | undefined>(undefined);

  protected prefijoMasivo = '';
  protected areaMasiva = '';

  private readonly filtro$ = new Subject<void>();

  protected readonly totalPaginas = computed(() =>
    Math.max(1, Math.ceil(this.totalRegistros() / this.tamanoPagina)),
  );

  /** Solo las áreas que resuelven contra el organigrama pueden ratificarse. */
  protected readonly areasDisponibles = computed(() =>
    this.perfilesArea().filter((a) => !!a.organizationalStructureId),
  );

  constructor() {
    this.filtro$.pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed()).subscribe(() => {
      this.pagina.set(1);
      this.cargarArticulos();
    });
  }

  ngOnInit(): void {
    this.cargarResumen();
    this.cargarArticulos();
    this.regulatoryService.listConsolidatorAreaProfiles().subscribe({
      next: (items) => this.perfilesArea.set(items ?? []),
      error: () => this.perfilesArea.set([]),
    });
  }

  protected porcentaje(parte: number, total: number): number {
    return total === 0 ? 0 : Math.round((parte / total) * 100);
  }

  protected origen(p: MasterProductDto): string {
    switch (p.consolidatorAreaSource) {
      case 'Manual':
        return 'ratificada por Planificación';
      case 'Rule':
        return p.consolidatorRuleKey ? `regla ${p.consolidatorRuleKey} v${p.consolidatorRuleVersion}` : 'propuesta por regla';
      case 'Imported':
        return 'importada de la maestra';
      default:
        return 'sin origen';
    }
  }

  protected semaforo(p: MasterProductDto): Semaforo {
    if (p.duplicateOfMasterProductId) return 'duplicado';
    if (!p.unspscCode || !p.expenseObjectCode || !p.accountingAccountCode || !p.consolidatorAreaId) return 'falta';
    if (p.requiresReview || p.areaAmbiguity) return 'revisar';
    return 'completo';
  }

  protected etiquetaSemaforo(s: Semaforo): string {
    return { completo: '✓ Completo', revisar: '⚠ Revisar', falta: 'Falta mapeo', duplicado: 'Duplicado' }[s];
  }

  protected claseSemaforo(s: Semaforo): string {
    return { completo: 'badge-success', revisar: 'badge-warning', falta: 'badge-error', duplicado: 'badge-neutral' }[s];
  }

  protected alBuscar(valor: string): void {
    this.busqueda.set(valor);
    this.filtro$.next();
  }

  protected alPrefijo(valor: string): void {
    this.prefijo.set(valor);
    this.filtro$.next();
  }

  protected alEstado(valor: string): void {
    this.estado.set(valor);
    this.pagina.set(1);
    this.cargarArticulos();
  }

  protected filtrarPor(vista: 'todos' | 'sin-area' | 'sin-mapeo' | 'revisar'): void {
    this.estado.set(vista === 'sin-area' ? 'Pending' : '');
    this.soloSinMapeo.set(vista === 'sin-mapeo' ? true : undefined);
    this.soloRevisar.set(vista === 'revisar' ? true : undefined);
    this.pagina.set(1);
    this.cargarArticulos();
  }

  protected irA(p: number): void {
    if (p < 1 || p > this.totalPaginas()) return;
    this.pagina.set(p);
    this.cargarArticulos();
  }

  protected ratificarMasivo(): void {
    this.ocupado.set(true);
    this.error.set('');
    this.masterProductService
      .bulkRatify({ unspscPrefix: this.prefijoMasivo.trim(), consolidatorAreaId: this.areaMasiva })
      .subscribe({
        next: (res) => {
          this.ocupado.set(false);
          this.notificar(`${res.count} artículos ratificados en la rama ${this.prefijoMasivo.trim()}.`);
          this.prefijoMasivo = '';
          this.cargarResumen();
          this.cargarArticulos();
        },
        error: () => {
          this.ocupado.set(false);
          this.error.set('No se pudo ratificar la rama. Verifique el prefijo y el área seleccionada.');
        },
      });
  }

  protected reevaluar(): void {
    this.ocupado.set(true);
    this.error.set('');
    this.routingService.reevaluate(true).subscribe({
      next: (res) => {
        this.ocupado.set(false);
        this.notificar(`Reglas re-evaluadas: ${res.updatedCount} artículos actualizados.`);
        this.cargarResumen();
        this.cargarArticulos();
      },
      error: () => {
        this.ocupado.set(false);
        this.error.set('No se pudo re-evaluar el motor de reglas.');
      },
    });
  }

  private cargarResumen(): void {
    this.masterProductService.getMappingSummary().subscribe({
      next: (r) => this.resumen.set(r),
      error: () => this.resumen.set(null),
    });
  }

  private cargarArticulos(): void {
    this.cargando.set(true);
    this.error.set('');
    this.masterProductService
      .list({
        q: this.busqueda().trim() || undefined,
        state: this.estado() || undefined,
        unspscPrefix: this.prefijo().trim() || undefined,
        missingMapping: this.soloSinMapeo(),
        requiresReview: this.soloRevisar(),
        page: this.pagina(),
        pageSize: this.tamanoPagina,
      })
      .subscribe({
        next: (res) => {
          this.articulos.set(res.value ?? []);
          this.totalRegistros.set(res.totalRecords);
          this.cargando.set(false);
        },
        error: () => {
          this.articulos.set([]);
          this.cargando.set(false);
          this.error.set('No se pudieron cargar los artículos.');
        },
      });
  }

  private notificar(texto: string): void {
    this.mensaje.set(texto);
    this.notification.success(texto);
    setTimeout(() => this.mensaje.set(''), 5000);
  }
}
