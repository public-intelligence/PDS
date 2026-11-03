import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PageShellComponent } from '@public-intelligence/ui-core';
import { PdfService } from '../../../../services/pdf/pdf.service';
import { PlanningStoreService } from '../../data/planning-store.service';
import { formatRD, MUNICIPIOS, montoDe } from '../../data/planning.mock-data';
import {
  CtxCsv,
  descargarCsv,
  FilaPaccCsv,
  paccACsv,
  planACsv,
  planesACsv,
} from '../../data/planning-poa-csv';
import {
  construirPaccPdf,
  construirPoaPdf,
  EncabezadoReporte,
  FilaPaccPdf,
  FilaPoaPdf,
} from '../../data/planning-pdf';
import { Plan, Requerimiento } from '../../data/planning.models';

/**
 * Reportes del POA y del PACC en PDF y Excel (ajuste 16): para publicación en
 * transparencia y respuesta a organismos rectores. PDF real (pdfmake) y Excel
 * vía CSV UTF-8 con separador «;» que Excel abre nativamente.
 */
@Component({
  selector: 'app-reportes-pds',
  imports: [PageShellComponent, FormsModule],
  template: `
    <app-page-shell
      title="Reportes del POA y del PACC"
      [description]="'Ciclo ' + store.cicloActivo().nombre + ' · genere el POA y el PACC en PDF y Excel para transparencia y organismos rectores'"
    >
      <label class="field-label mb-4 block max-w-md">Alcance del reporte
        <select class="field-control mt-1" [ngModel]="alcance()" (ngModelChange)="alcance.set($event)">
          <option value="institucional">Institucional (todas las áreas)</option>
          @for (p of store.planes(); track p.id) {
            <option [value]="p.areaId">{{ store.area(p.areaId)?.nombre }}</option>
          }
        </select>
      </label>

      <div class="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <!-- POA -->
        <div class="card p-5">
          <h2 class="!font-poppins !text-base font-semibold text-ink">POA — Plan Operativo Anual</h2>
          <p class="mt-1 !text-sm text-ink-muted">{{ filasPoa().length }} producto(s) en el alcance seleccionado.</p>
          <div class="mt-3 flex gap-2">
            <button type="button" class="btn-primary" (click)="poaPdf()">PDF</button>
            <button type="button" class="btn-secondary" (click)="poaExcel()">Excel</button>
          </div>
        </div>

        <!-- PACC -->
        <div class="card p-5">
          <h2 class="!font-poppins !text-base font-semibold text-ink">PACC — Plan Anual de Compras y Contrataciones</h2>
          <p class="mt-1 !text-sm text-ink-muted">{{ reqsPacc().length }} requerimiento(s) · {{ fmt(totalPacc()) }} consolidados.</p>
          <div class="mt-3 flex gap-2">
            <button type="button" class="btn-primary" (click)="paccPdf()">PDF</button>
            <button type="button" class="btn-secondary" (click)="paccExcel()">Excel</button>
          </div>
        </div>
      </div>

      @if (mensaje()) {
        <p class="slide-in-top mt-4 rounded-lg bg-success-bg px-3 py-2 text-sm text-success-text ring-1 ring-success-border">{{ mensaje() }}</p>
      }
    </app-page-shell>
  `,
})
export class ReportesPdsComponent {
  protected readonly store = inject(PlanningStoreService);
  private readonly pdf = inject(PdfService);
  protected readonly fmt = formatRD;

  protected readonly alcance = signal<string>('institucional');
  protected readonly mensaje = signal('');

  private readonly planesEnAlcance = computed<Plan[]>(() => {
    const a = this.alcance();
    return a === 'institucional' ? this.store.planes() : this.store.planes().filter((p) => p.areaId === a);
  });

  private ctxCsv(): CtxCsv {
    return {
      pei: this.store.pei(),
      areas: this.store.areas,
      columnas: this.store.columnasPersonalizadas(),
      municipiosValidos: MUNICIPIOS,
    };
  }

  private encabezado(titulo: string): EncabezadoReporte {
    const artefacto = titulo.startsWith('POA') ? 'POA' : 'PACC';
    const sello = this.store.selloDe(artefacto);
    return {
      institucion: 'Unidad de Planificación y Desarrollo',
      titulo,
      ciclo: this.store.cicloActivo().nombre,
      version: sello?.version ?? 'Borrador (sin aprobar)',
      sello: sello ? `Aprobado por ${sello.usuario} · ${sello.fecha}` : 'Pendiente de aprobación',
    };
  }

  // --- POA ------------------------------------------------------------------
  protected readonly filasPoa = computed<FilaPoaPdf[]>(() =>
    this.planesEnAlcance().flatMap((plan) =>
      plan.rows.map((row) => {
        const foco = this.store.foco(row.focoId);
        const obj = this.store.objetivo(row.focoId, row.objetivoId);
        const alineacion =
          row.origen === 'operativo'
            ? 'Operativo (no PEI)'
            : [foco?.nombre, obj?.codigo].filter(Boolean).join(' · ') || 'Sin alinear';
        return {
          codigo: row.codigo,
          origen: row.origen === 'operativo' ? 'Operativo' : 'PEI',
          alineacion,
          producto: row.nombreProducto || '(sin producto)',
          unidades: row.unidades.map((u) => u.nombre).filter(Boolean).join(' / ') || '—',
          meta: row.unidades.map((u) => u.metaValor + (u.metaTipo === 'porcentaje' ? '%' : '')).join(' / ') || '—',
          responsable: [...new Set(row.unidades.map((u) => this.store.area(u.responsableAreaId)?.corta).filter(Boolean))].join(', ') || '—',
          municipios: row.municipiosImpacto.join(', ') || '—',
          tareas:
            row.tareas
              .filter((t) => t.nombre.trim())
              .map((t) => (t.continua ? `${t.nombre} (continua)` : t.nombre))
              .join('; ') || '—',
        };
      }),
    ),
  );

  protected poaPdf(): void {
    this.pdf.download(construirPoaPdf(this.encabezado('POA ' + this.store.cicloActivo().nombre), this.filasPoa()), this.nombre('POA'));
    this.mensaje.set('POA generado en PDF.');
  }

  protected poaExcel(): void {
    const ctx = this.ctxCsv();
    const a = this.alcance();
    const contenido =
      a === 'institucional'
        ? planesACsv(this.store.planes(), ctx)
        : planACsv(this.planesEnAlcance()[0], ctx);
    descargarCsv(this.nombre('POA') + '.csv', contenido);
    this.mensaje.set('POA generado en Excel (CSV).');
  }

  // --- PACC -----------------------------------------------------------------
  protected readonly reqsPacc = computed<Requerimiento[]>(() => {
    const a = this.alcance();
    return this.store
      .requerimientosVigentes()
      .filter((r) => this.store.llevaCosto(r))
      .filter((r) => a === 'institucional' || r.origenAreaId === a);
  });

  protected readonly totalPacc = computed(() => this.reqsPacc().reduce((s, r) => s + montoDe(r), 0));

  private filaPacc(r: Requerimiento) {
    const rubro = this.store.rubroDe(r);
    return {
      codigo: r.codigo,
      rubro: r.rubro,
      codigoPresupuestario: rubro?.codigoPresupuestario ?? '',
      codigoUnspsc: rubro?.codigoUnspsc ?? '',
      requiriente: this.store.area(r.origenAreaId)?.corta ?? '',
      consolidadora: this.store.area(this.store.areaEjecutoraDe(r))?.corta ?? '',
      cantidad: r.cantidad,
      unidad: r.unidadMedida,
      precio: r.precioEstimado,
      monto: montoDe(r),
      trimestre: r.trimestre,
      estado: r.estado,
      fuente: r.fuenteFinanciamiento,
    };
  }

  protected paccPdf(): void {
    const filas: FilaPaccPdf[] = this.reqsPacc().map((r) => {
      const f = this.filaPacc(r);
      return {
        ...f,
        cantidad: String(f.cantidad),
        precio: this.fmt(f.precio),
        monto: this.fmt(f.monto),
      };
    });
    this.pdf.download(construirPaccPdf(this.encabezado('PACC ' + this.store.cicloActivo().nombre), filas), this.nombre('PACC'));
    this.mensaje.set('PACC generado en PDF.');
  }

  protected paccExcel(): void {
    const filas: FilaPaccCsv[] = this.reqsPacc().map((r) => this.filaPacc(r));
    descargarCsv(this.nombre('PACC') + '.csv', paccACsv(filas));
    this.mensaje.set('PACC generado en Excel (CSV).');
  }

  private nombre(tipo: string): string {
    const alcance = this.alcance() === 'institucional' ? 'institucional' : this.store.area(this.alcance())?.corta ?? 'area';
    return `${tipo}-${this.store.cicloActivo().periodo}-${alcance}`.replace(/\s+/g, '-');
  }
}
