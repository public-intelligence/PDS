import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { NotificationService } from '../notification/notification.service';
import {
  CambioPei,
  MetasAnuales,
  Pei,
  ProductoEstrategico,
  UnidadMedidaPei,
  claveDeAnio,
} from '../../modules/planning/data/planning.models';

// ─────────────────────── DTOs del backend (camelCase) ───────────────────────

export interface StrategicAnnualTargetDto {
  year: number;
  value: number;
}

export interface StrategicMeasureUnitDto {
  id: string;
  name: string;
  responsibleAreaId: string | null;
  responsibleAreaName: string | null;
  discontinuedFromYear: number | null;
  discontinuationReason: string | null;
  targets: StrategicAnnualTargetDto[];
}

export interface StrategicProductDto {
  id: string;
  name: string;
  discontinuedFromYear: number | null;
  discontinuationReason: string | null;
  involvedAreas: { areaId: string; areaName: string | null }[];
  measureUnits: StrategicMeasureUnitDto[];
}

export interface StrategicObjectiveDto {
  id: string;
  code: string;
  name: string;
  products: StrategicProductDto[];
}

export interface StrategicFocusDto {
  id: string;
  name: string;
  endAlignment: string | null;
  objectives: StrategicObjectiveDto[];
}

export interface StrategicPlanDto {
  id: string;
  name: string;
  periodStartYear: number;
  periodEndYear: number;
  versionNumber: number;
  status: 'Draft' | 'Current' | 'Historical';
  reason: string | null;
  createdByName: string | null;
  createdAt: string;
  approvedByName: string | null;
  approvedAt: string | null;
}

export interface StrategicPlanDetailDto extends StrategicPlanDto {
  focuses: StrategicFocusDto[];
  changes: {
    date: string;
    userName: string | null;
    changeType: 'Added' | 'Edited' | 'Discontinued';
    level: 'Focus' | 'Objective' | 'Product' | 'MeasureUnit';
    description: string;
  }[];
}

export interface ImportError {
  row: number;
  column: string | null;
  message: string;
}

export interface ImportResult {
  applied: boolean;
  errors: ImportError[];
  warnings: string[];
  summary: { added: number; updated: number; unchanged: number; removed: number } | null;
}

interface StructureResponse {
  id: string;
  name: string;
  parentId?: string;
}

/** Área del organigrama real, con la forma mínima que usan las pantallas del PEI. */
export interface AreaPei {
  id: string;
  nombre: string;
  corta: string;
  esRaiz: boolean;
}

// ─────────────────────── Adaptadores DTO → modelos del mock ───────────────────────

const ESTADOS: Record<StrategicPlanDto['status'], Pei['estado']> = {
  Draft: 'En elaboración',
  Current: 'Vigente',
  Historical: 'Histórico',
};

const TIPOS_CAMBIO: Record<string, CambioPei['tipo']> = {
  Added: 'Alta',
  Edited: 'Edición',
  Discontinued: 'Descontinuación',
};

const NIVELES_CAMBIO: Record<string, CambioPei['nivel']> = {
  Focus: 'Foco',
  Objective: 'Objetivo',
  Product: 'Producto',
  MeasureUnit: 'Unidad de medida',
};

function metasDe(targets: StrategicAnnualTargetDto[], inicio: number, fin: number): MetasAnuales {
  const metas: MetasAnuales = {};
  for (let anio = inicio; anio <= fin; anio++) metas[claveDeAnio(anio)] = 0;
  for (const t of targets) metas[claveDeAnio(t.year)] = t.value;
  return metas;
}

/** Adapta el DTO del backend al modelo Pei del mock. Reutilizado por el POA (M5) para alinear. */
export function adaptarPlanEstrategico(dto: StrategicPlanDetailDto): Pei {
  return {
    id: dto.id,
    nombre: dto.name,
    periodo: `${dto.periodStartYear}-${dto.periodEndYear}`,
    version: `v${dto.versionNumber}.0`,
    estado: ESTADOS[dto.status],
    motivo: dto.reason ?? '',
    creadaPor: dto.createdByName ?? '',
    fechaCreacion: dto.createdAt?.slice(0, 10) ?? '',
    aprobadoPor: dto.approvedByName ?? '',
    fechaAprobacion: dto.approvedAt?.slice(0, 10) ?? '',
    focos: dto.focuses.map((f) => ({
      id: f.id,
      nombre: f.name,
      alineacionEnd: f.endAlignment ?? '',
      objetivos: f.objectives.map((o) => ({
        id: o.id,
        codigo: o.code,
        nombre: o.name,
        productos: o.products.map((p) => ({
          id: p.id,
          nombre: p.name,
          descontinuadoDesde: p.discontinuedFromYear ?? undefined,
          motivoDescontinuacion: p.discontinuationReason ?? undefined,
          areasInvolucradasIds: p.involvedAreas.map((a) => a.areaId),
          unidades: p.measureUnits.map((u) => ({
            id: u.id,
            nombre: u.name,
            responsableAreaId: u.responsibleAreaId ?? '',
            descontinuadoDesde: u.discontinuedFromYear ?? undefined,
            motivoDescontinuacion: u.discontinuationReason ?? undefined,
            metasAnuales: metasDe(u.targets, dto.periodStartYear, dto.periodEndYear),
          })),
        })),
      })),
    })),
    cambios: dto.changes.map((c) => ({
      fecha: c.date?.slice(0, 10) ?? '',
      autor: c.userName ?? '',
      tipo: TIPOS_CAMBIO[c.changeType] ?? 'Edición',
      nivel: NIVELES_CAMBIO[c.level] ?? 'Producto',
      descripcion: c.description,
    })),
  };
}

// ─────────────────────── Store del PEI contra el backend (M3) ───────────────────────

/**
 * Facade del PEI real: expone la MISMA superficie que la porción PEI del
 * PlanningStoreService del mock (señales `peiVersiones`/`pei`/`peiBorrador`,
 * helpers de historia y mutaciones con los mismos nombres) pero respaldada por
 * la API strategic-plans. Las mutaciones son optimistas en UX: disparan la
 * llamada y refrescan las señales cuando el servidor confirma.
 */
@Injectable({ providedIn: 'root' })
export class PeiStoreService {
  private http = inject(HttpClient);
  private notification = inject(NotificationService);
  private readonly base = `${environment.apiUrl}strategic-plans`;

  readonly anioActual = new Date().getFullYear();

  private readonly versionesSignal = signal<Pei[]>([]);
  private readonly areasSignal = signal<AreaPei[]>([]);
  private readonly cargandoSignal = signal(false);
  private readonly cargadoSignal = signal(false);
  private readonly descargandoSignal = signal(false);
  private readonly errorSignal = signal('');

  readonly peiVersiones = this.versionesSignal.asReadonly();
  readonly cargando = this.cargandoSignal.asReadonly();
  readonly cargado = this.cargadoSignal.asReadonly();
  readonly descargando = this.descargandoSignal.asReadonly();
  readonly error = this.errorSignal.asReadonly();
  readonly areas = this.areasSignal.asReadonly();

  /** Versión vigente (la que consume el POA). Undefined si la organización aún no tiene PEI. */
  readonly pei = computed<Pei | undefined>(
    () => this.versionesSignal().find((v) => v.estado === 'Vigente') ?? this.versionesSignal()[0],
  );

  /** Enmienda en curso, si la hay (como máximo una). */
  readonly peiBorrador = computed<Pei | undefined>(() =>
    this.versionesSignal().find((v) => v.estado === 'En elaboración'),
  );

  // ─────────────── Carga ───────────────

  async cargar(): Promise<void> {
    if (this.cargandoSignal()) return;
    this.cargandoSignal.set(true);
    this.errorSignal.set('');
    try {
      const [resumenes, areas] = await Promise.all([
        firstValueFrom(this.http.get<StrategicPlanDto[]>(this.base)),
        this.cargarAreas(),
      ]);
      const detalles = await Promise.all(
        resumenes.map((r) =>
          firstValueFrom(this.http.get<StrategicPlanDetailDto>(`${this.base}/${r.id}`)),
        ),
      );
      this.areasSignal.set(areas);
      this.versionesSignal.set(detalles.map(adaptarPlanEstrategico));
    } catch {
      this.errorSignal.set('No se pudo cargar el PEI desde el servidor.');
    } finally {
      this.cargandoSignal.set(false);
      this.cargadoSignal.set(true);
    }
  }

  private async cargarAreas(): Promise<AreaPei[]> {
    try {
      const config = await firstValueFrom(
        this.http.get<{ qualifyingAreas: { id: string; name: string; parentId: string | null; qualifiesForPlanning: boolean }[] }>(
          `${environment.apiUrl}planning-config/area-guardrails`,
        ),
      );
      if (config?.qualifyingAreas) {
        return config.qualifyingAreas
          .filter((a) => a.qualifiesForPlanning)
          .map((e) => ({
            id: e.id,
            nombre: e.name,
            corta: e.name,
            esRaiz: !e.parentId,
          }));
      }
      const estructuras = await firstValueFrom(
        this.http.get<StructureResponse[]>(`${environment.apiUrl}organizational-structures`),
      );
      return estructuras.map((e) => ({
        id: e.id,
        nombre: e.name,
        corta: e.name,
        esRaiz: !e.parentId,
      }));
    } catch {
      return [];
    }
  }

  private async refrescar(): Promise<void> {
    this.cargadoSignal.set(false);
    this.versionesSignal.set([]);
    await this.cargar();
  }

  area(id: string): AreaPei | undefined {
    return this.areasSignal().find((a) => a.id === id);
  }

  // ─────────────── Años y reglas de historia (dinámicos por versión) ───────────────

  /** Años del período de la versión indicada, con su condición de editables. */
  aniosDe(pei: Pei | undefined): { anio: number; clave: `a${number}`; editable: boolean }[] {
    if (!pei) return [];
    const [inicio, fin] = pei.periodo.split('-').map(Number);
    const anios: { anio: number; clave: `a${number}`; editable: boolean }[] = [];
    for (let anio = inicio; anio <= (fin || inicio); anio++) {
      anios.push({ anio, clave: claveDeAnio(anio), editable: anio > this.anioActual });
    }
    return anios;
  }

  /** Una unidad tiene historia si comprometió metas (> 0) en años ya transcurridos. */
  unidadTieneHistoria(u: UnidadMedidaPei): boolean {
    return Object.entries(u.metasAnuales).some(
      ([clave, valor]) => Number(clave.slice(1)) <= this.anioActual && (valor ?? 0) > 0,
    );
  }

  productoTieneHistoria(p: ProductoEstrategico): boolean {
    return p.unidades.some((u) => this.unidadTieneHistoria(u));
  }

  // ─────────────── Versionado ───────────────

  async crearPeiInicial(nombre: string, inicio: number, fin: number): Promise<Pei | undefined> {
    try {
      const dto = await firstValueFrom(
        this.http.post<StrategicPlanDetailDto>(this.base, {
          name: nombre,
          periodStartYear: inicio,
          periodEndYear: fin,
        }),
      );
      await this.refrescar();
      return this.versionesSignal().find((v) => v.id === dto.id);
    } catch (e) {
      this.notificarError(e, 'No se pudo crear el PEI.');
      return undefined;
    }
  }

  async iniciarEnmienda(motivo: string): Promise<Pei | undefined> {
    try {
      const dto = await firstValueFrom(
        this.http.post<StrategicPlanDetailDto>(`${this.base}/amendments`, { reason: motivo }),
      );
      await this.refrescar();
      return this.versionesSignal().find((v) => v.id === dto.id);
    } catch (e) {
      this.notificarError(e, 'No se pudo abrir la enmienda.');
      return undefined;
    }
  }

  async descartarEnmienda(): Promise<void> {
    try {
      await firstValueFrom(this.http.delete(`${this.base}/amendments/current`));
      await this.refrescar();
    } catch (e) {
      this.notificarError(e, 'No se pudo descartar la enmienda.');
    }
  }

  async aprobarEnmienda(): Promise<void> {
    try {
      await firstValueFrom(this.http.post(`${this.base}/amendments/current/approve`, {}));
      await this.refrescar();
    } catch (e) {
      this.notificarError(e, 'No se pudo aprobar la enmienda.');
    }
  }

  // ─────────────── Mutaciones del árbol (operan sobre la enmienda en curso) ───────────────

  agregarFoco(nombre: string, alineacionEnd: string): void {
    void this.mutar((planId) =>
      this.http.post(`${this.base}/${planId}/focuses`, { name: nombre, endAlignment: alineacionEnd }),
    );
  }

  editarFoco(focoId: string, nombre: string): void {
    void this.mutar((planId) => {
      const foco = this.peiBorrador()?.focos.find((f) => f.id === focoId);
      return this.http.put(`${this.base}/${planId}/focuses/${focoId}`, {
        name: nombre,
        endAlignment: foco?.alineacionEnd ?? null,
      });
    });
  }

  /** Pre-chequeo síncrono de historia (mismo contrato que el mock); el servidor revalida. */
  eliminarFoco(focoId: string): boolean {
    const foco = this.peiBorrador()?.focos.find((f) => f.id === focoId);
    if (!foco) return false;
    if (foco.objetivos.some((o) => o.productos.some((p) => this.productoTieneHistoria(p)))) return false;
    void this.mutar((planId) => this.http.delete(`${this.base}/${planId}/focuses/${focoId}`));
    return true;
  }

  agregarObjetivo(focoId: string, nombre: string): void {
    void this.mutar((planId) =>
      this.http.post(`${this.base}/${planId}/focuses/${focoId}/objectives`, { name: nombre }),
    );
  }

  editarObjetivo(_focoId: string, objetivoId: string, nombre: string): void {
    void this.mutar((planId) =>
      this.http.put(`${this.base}/${planId}/objectives/${objetivoId}`, { name: nombre }),
    );
  }

  eliminarObjetivo(focoId: string, objetivoId: string): boolean {
    const obj = this.peiBorrador()
      ?.focos.find((f) => f.id === focoId)
      ?.objetivos.find((o) => o.id === objetivoId);
    if (!obj) return false;
    if (obj.productos.some((p) => this.productoTieneHistoria(p))) return false;
    void this.mutar((planId) => this.http.delete(`${this.base}/${planId}/objectives/${objetivoId}`));
    return true;
  }

  agregarProducto(_focoId: string, objetivoId: string, nombre: string): void {
    void this.mutar((planId) =>
      this.http.post(`${this.base}/${planId}/objectives/${objetivoId}/products`, { name: nombre }),
    );
  }

  editarProducto(
    productoId: string,
    patch: Partial<Pick<ProductoEstrategico, 'nombre' | 'areasInvolucradasIds'>>,
  ): void {
    void this.mutar((planId) =>
      this.http.put(`${this.base}/${planId}/products/${productoId}`, {
        name: patch.nombre ?? null,
        involvedAreaIds: patch.areasInvolucradasIds ?? null,
      }),
    );
  }

  eliminarProducto(productoId: string): boolean {
    const prod = this.buscarProductoEnBorrador(productoId);
    if (!prod || this.productoTieneHistoria(prod)) return false;
    void this.mutar((planId) => this.http.delete(`${this.base}/${planId}/products/${productoId}`));
    return true;
  }

  descontinuarProducto(productoId: string, desde: number, motivo: string): void {
    void this.mutar((planId) =>
      this.http.post(`${this.base}/${planId}/products/${productoId}/discontinue`, {
        fromYear: desde,
        reason: motivo,
      }),
    );
  }

  descontinuarUnidad(productoId: string, unidadId: string, desde: number, motivo: string): void {
    void this.mutar((planId) =>
      this.http.post(`${this.base}/${planId}/measure-units/${unidadId}/discontinue`, {
        fromYear: desde,
        reason: motivo,
      }),
    );
  }

  reemplazarUnidadesPei(productoId: string, unidades: UnidadMedidaPei[]): void {
    void this.mutar((planId) =>
      this.http.put(`${this.base}/${planId}/products/${productoId}/measure-units`, {
        units: unidades.map((u) => ({
          id: u.id || null,
          name: u.nombre,
          responsibleAreaId: u.responsableAreaId || null,
          targets: Object.entries(u.metasAnuales).map(([clave, valor]) => ({
            year: Number(clave.slice(1)),
            value: valor ?? 0,
          })),
        })),
      }),
    );
  }

  private buscarProductoEnBorrador(productoId: string): ProductoEstrategico | undefined {
    for (const f of this.peiBorrador()?.focos ?? [])
      for (const o of f.objetivos) {
        const prod = o.productos.find((x) => x.id === productoId);
        if (prod) return prod;
      }
    return undefined;
  }

  /** Ejecuta una mutación sobre la enmienda en curso y refresca el estado al confirmar. */
  private async mutar(peticion: (planId: string) => import('rxjs').Observable<unknown>): Promise<void> {
    const borrador = this.peiBorrador();
    if (!borrador) return;
    try {
      await firstValueFrom(peticion(borrador.id));
      await this.refrescar();
    } catch (e) {
      this.notificarError(e, 'No se pudo guardar el cambio en el PEI.');
      await this.refrescar();
    }
  }

  // ─────────────── Excel ───────────────

  async descargarPlantilla(planId: string): Promise<void> {
    await this.descargarArchivo(`${this.base}/${planId}/excel-template`, 'Plantilla PEI.xlsx');
  }

  async exportar(planId: string): Promise<void> {
    await this.descargarArchivo(`${this.base}/${planId}/export`, 'PEI.xlsx');
  }

  async importar(planId: string, archivo: File, modo: 'actualizar' | 'reemplazar', dryRun: boolean): Promise<ImportResult> {
    const form = new FormData();
    form.append('file', archivo);
    try {
      const resultado = await firstValueFrom(
        this.http.post<ImportResult>(`${this.base}/${planId}/import?modo=${modo}&dryRun=${dryRun}`, form),
      );
      if (resultado.applied) await this.refrescar();
      return resultado;
    } catch {
      return {
        applied: false,
        errors: [{ row: 0, column: null, message: 'No se pudo procesar el archivo en el servidor.' }],
        warnings: [],
        summary: null,
      };
    }
  }

  private async descargarArchivo(url: string, nombreFallback: string): Promise<void> {
    this.descargandoSignal.set(true);
    this.notification.onToast({
      message: `Generando y descargando ${nombreFallback}…`,
      status: 'default',
      duration: 3000,
    });
    try {
      const respuesta = await firstValueFrom(this.http.get(url, { observe: 'response', responseType: 'blob' }));
      const disposition = respuesta.headers.get('Content-Disposition') ?? '';
      const match = /filename\*?=(?:UTF-8'')?"?([^";]+)"?/i.exec(disposition);
      const nombre = match ? decodeURIComponent(match[1]) : nombreFallback;

      const enlace = document.createElement('a');
      enlace.href = URL.createObjectURL(respuesta.body!);
      enlace.download = nombre;
      enlace.click();
      URL.revokeObjectURL(enlace.href);
      this.notification.success(`Archivo «${nombre}» descargado correctamente.`);
    } catch {
      this.errorSignal.set('No se pudo descargar el archivo.');
      this.notification.error('No se pudo descargar el archivo.');
    } finally {
      this.descargandoSignal.set(false);
    }
  }

  private notificarError(e: unknown, fallback: string): void {
    const mensaje =
      (e as { error?: { detail?: { errors?: string[] } } })?.error?.detail?.errors?.[0] ?? fallback;
    this.errorSignal.set(mensaje);
  }
}
