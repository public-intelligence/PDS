import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ArticuloServicio,
  CategoriaTransversal,
  ColumnaConfig,
  Pei,
  Plan,
  ProductoPoa,
  Requerimiento,
  RubroCatalogo,
  TareaPoa,
  UnidadMedidaPoa,
} from '../../modules/planning/data/planning.models';
import {
  AreaPei,
  ImportResult,
  StrategicPlanDetailDto,
  adaptarPlanEstrategico,
} from './pei-store.service';
import { PlanningContextService } from './planning-context.service';
import { NotificationService } from '../notification/notification.service';

// ─────────────────────── DTOs del backend (camelCase) ───────────────────────

interface PlanningCycleListDto {
  id: string;
  name: string;
  year: number;
  endYear: number | null;
  status: 'Draft' | 'Published' | 'Closed';
  strategicPlanId: string;
  templateId: string;
}

interface AnnualPlanListDto {
  id: string;
  cycleId: string;
  cycleName: string;
  cycleYear: number;
  cycleStatus: string;
  areaId: string;
  areaName: string;
  name: string;
  status: string;
  version: number;
  dueDate: string | null;
  generalObservation: string | null;
  productCount: number;
  incompleteProductCount: number;
}

interface TemplateColumnDto {
  id: string;
  visibleName: string;
  technicalName: string;
  type: 'Text' | 'Number' | 'Currency' | 'Select';
  required: boolean;
  visible: boolean;
  origin: 'Base' | 'Custom';
  optionsJson: string | null;
  order: number;
  version: number;
}

interface PoaUnitDto {
  id: string;
  name: string;
  targetValue: number;
  targetType: 'Absolute' | 'Percentage';
  verificationMeans: string | null;
  responsibleAreaId: string | null;
  responsibleAreaName: string | null;
  q1: number;
  q2: number;
  q3: number;
  q4: number;
}

interface PoaTaskDto {
  id: string;
  code: string;
  name: string;
  isContinuous: boolean;
  months: number[];
}

interface PoaProductDto {
  id: string;
  code: string;
  origin: 'Pei' | 'Operational';
  strategicFocusId: string | null;
  strategicObjectiveId: string | null;
  strategicProductId: string | null;
  productName: string;
  operationalJustification: string | null;
  observation: string | null;
  order: number;
  measureUnits: PoaUnitDto[];
  tasks: PoaTaskDto[];
  municipalities: { municipioId: string; municipioName: string }[];
  involvedAreas: { areaId: string; areaName: string | null }[];
  customValues: Record<string, string>;
  issues: string[];
  rowVersion: string | null;
}

interface WorkspaceDto extends AnnualPlanListDto {
  strategicPlanId: string;
  templateColumns: TemplateColumnDto[];
  products: PoaProductDto[];
  readOnly: boolean;
}

interface UpdateProductBody {
  origin?: string;
  strategicFocusId?: string | null;
  strategicObjectiveId?: string | null;
  strategicProductId?: string | null;
  clearAlignment?: boolean;
  productName?: string;
  operationalJustification?: string;
  observation?: string;
  measureUnits?: unknown[];
  tasks?: unknown[];
  municipioIds?: string[];
  involvedAreaIds?: string[];
  customValues?: Record<string, string>;
}

interface MunicipioDto {
  id: string;
  nombre: string;
}

interface StructureResponse {
  id: string;
  name: string;
  parentId?: string;
}

/** Resumen del ciclo para la navegación por listados (columna/filtro Ciclo). */
export interface CicloResumen {
  id: string;
  nombre: string;
  periodo: string;
  estado: 'Borrador' | 'Publicado' | 'Cerrado';
  strategicPlanId: string;
}

const ESTADO_CICLO: Record<PlanningCycleListDto['status'], CicloResumen['estado']> = {
  Draft: 'Borrador',
  Published: 'Publicado',
  Closed: 'Cerrado',
};

const TIPO_COLUMNA: Record<TemplateColumnDto['type'], ColumnaConfig['tipo']> = {
  Text: 'texto',
  Number: 'numero',
  Currency: 'moneda',
  Select: 'seleccion',
};

const GUID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

// ─────────────────────── Store del POA contra el backend (M5+M6) ───────────────────────

/**
 * Facade del plan anual real: expone la superficie del PlanningStoreService que usan
 * planes-list, plan-workspace, plan-grid y plan-revisar, respaldada por la API
 * annual-plans. La validación del servidor (Issues por producto) es la fuente de verdad
 * de rowIssues(). Los REQUERIMIENTOS siguen en mock hasta M7: sus miembros devuelven
 * vacío/no-op y el tab del drawer muestra un aviso.
 */
@Injectable({ providedIn: 'root' })
export class PoaStoreService {
  private readonly http = inject(HttpClient);
  private readonly notification = inject(NotificationService);
  private readonly contexto = inject(PlanningContextService);
  private readonly base = `${environment.apiUrl}annual-plans`;

  // ─────────────── Estado ───────────────

  private readonly ciclosSignal = signal<CicloResumen[]>([]);
  private readonly cicloSeleccionadoIdSignal = signal<string>('');
  private readonly planesSignal = signal<Plan[]>([]);
  private readonly peiSignal = signal<Pei | undefined>(undefined);
  private readonly areasSignal = signal<AreaPei[]>([]);
  private readonly municipiosSignal = signal<MunicipioDto[]>([]);
  private readonly columnasSignal = signal<ColumnaConfig[]>([]);
  private readonly issuesPorFila = new Map<string, string[]>();
  private readonly rowVersionPorFila = new Map<string, string>();
  private readonly conteosPorPlan = new Map<string, { productos: number; incompletos: number }>();
  private readonly cargandoSignal = signal(false);
  private readonly cargadoSignal = signal(false);
  private readonly descargandoSignal = signal(false);
  private readonly errorSignal = signal('');

  readonly ciclos = this.ciclosSignal.asReadonly();
  readonly planes = this.planesSignal.asReadonly();
  readonly pei = computed<Pei>(() => this.peiSignal() ?? PEI_VACIO);
  readonly cargando = this.cargandoSignal.asReadonly();
  readonly cargado = this.cargadoSignal.asReadonly();
  readonly descargando = this.descargandoSignal.asReadonly();
  readonly error = this.errorSignal.asReadonly();

  readonly cicloActivo = computed<CicloResumen>(() => {
    const seleccionado = this.ciclosSignal().find((c) => c.id === this.cicloSeleccionadoIdSignal());
    return (
      seleccionado ??
      this.ciclosSignal().find((c) => c.estado === 'Publicado') ??
      this.ciclosSignal()[0] ?? { id: '', nombre: '—', periodo: '', estado: 'Publicado', strategicPlanId: '' }
    );
  });

  /** Áreas del organigrama (array, como el mock exponía `store.areas`). */
  get areas(): AreaPei[] {
    return this.areasSignal();
  }

  /** Nombres de municipios para el multi-select (reemplaza MUNICIPIOS del mock). */
  readonly municipios = computed<string[]>(() => this.municipiosSignal().map((m) => m.nombre));

  columnasPersonalizadas(): ColumnaConfig[] {
    return this.columnasSignal().filter((c) => c.origen === 'personalizada' && c.visible);
  }

  // ─────────────── Contexto (permisos y área del usuario) ───────────────

  persona(): { areaId: string } {
    return { areaId: this.contexto.planAreaId() ?? '' };
  }

  areaActual(): { nombre: string } {
    return { nombre: this.contexto.planAreaName() ?? '—' };
  }

  puedeRevisar(): boolean {
    return this.contexto.hasPermission('planning:poa:review');
  }

  puedeEditar(): boolean {
    return this.contexto.hasPermission('planning:poa:edit');
  }

  // ─────────────── Carga ───────────────

  async cargar(): Promise<void> {
    if (this.cargandoSignal()) return;
    this.cargandoSignal.set(true);
    this.errorSignal.set('');
    try {
      await this.contexto.load();
      const [ciclos, areas, municipios] = await Promise.all([
        firstValueFrom(this.http.get<PlanningCycleListDto[]>(`${environment.apiUrl}planning-cycles`)),
        this.cargarAreas(),
        firstValueFrom(this.http.get<MunicipioDto[]>(`${environment.apiUrl}territorio/municipios`)),
      ]);

      this.areasSignal.set(areas);
      this.municipiosSignal.set(municipios);
      this.ciclosSignal.set(
        ciclos.map((c) => ({
          id: c.id,
          nombre: c.name,
          periodo: c.endYear ? `${c.year}-${c.endYear}` : `${c.year}`,
          estado: ESTADO_CICLO[c.status] ?? 'Publicado',
          strategicPlanId: c.strategicPlanId,
        })),
      );

      if (!this.cicloSeleccionadoIdSignal()) {
        const activo = this.ciclosSignal().find((c) => c.estado === 'Publicado') ?? this.ciclosSignal()[0];
        this.cicloSeleccionadoIdSignal.set(activo?.id ?? '');
      }

      await Promise.all([this.cargarPlanes(), this.cargarPei()]);
    } catch {
      this.errorSignal.set('No se pudo cargar el plan anual desde el servidor.');
    } finally {
      this.cargandoSignal.set(false);
      this.cargadoSignal.set(true);
    }
  }

  async seleccionarCiclo(cicloId: string): Promise<void> {
    if (cicloId === this.cicloSeleccionadoIdSignal()) return;
    this.cicloSeleccionadoIdSignal.set(cicloId);
    this.planesSignal.set([]);
    this.peiSignal.set(undefined);
    await Promise.all([this.cargarPlanes(), this.cargarPei()]);
  }

  private async cargarPlanes(): Promise<void> {
    const cicloId = this.cicloActivo().id;
    if (!cicloId) return;
    const dtos = await firstValueFrom(
      this.http.get<AnnualPlanListDto[]>(`${this.base}?cycleId=${cicloId}`),
    );
    this.planesSignal.set(dtos.map((d) => this.adaptarPlanResumen(d)));
  }

  private async cargarPei(): Promise<void> {
    const strategicPlanId = this.cicloActivo().strategicPlanId;
    if (!strategicPlanId) return;
    try {
      const dto = await firstValueFrom(
        this.http.get<StrategicPlanDetailDto>(`${environment.apiUrl}strategic-plans/${strategicPlanId}`),
      );
      this.peiSignal.set(adaptarPlanEstrategico(dto));
    } catch {
      this.peiSignal.set(undefined);
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
          .map((e) => ({ id: e.id, nombre: e.name, corta: e.name, esRaiz: !e.parentId }));
      }
      const estructuras = await firstValueFrom(
        this.http.get<StructureResponse[]>(`${environment.apiUrl}organizational-structures`),
      );
      return estructuras.map((e) => ({ id: e.id, nombre: e.name, corta: e.name, esRaiz: !e.parentId }));
    } catch {
      return [];
    }
  }

  /** Carga el expediente completo del plan (productos, columnas de la plantilla, PEI del ciclo). */
  async cargarWorkspace(planId: string): Promise<Plan | undefined> {
    try {
      const dto = await firstValueFrom(this.http.get<WorkspaceDto>(`${this.base}/${planId}/workspace`));

      this.columnasSignal.set(dto.templateColumns.map((c) => this.adaptarColumna(c)));

      if (dto.cycleId !== this.cicloSeleccionadoIdSignal()) {
        this.cicloSeleccionadoIdSignal.set(dto.cycleId);
        await this.cargarPlanes();
      }
      if (this.peiSignal() === undefined || this.cicloActivo().strategicPlanId !== dto.strategicPlanId) {
        await this.cargarPei();
      }

      const plan = this.adaptarPlanCompleto(dto);
      this.planesSignal.update((lista) => {
        const sinEste = lista.filter((p) => p.id !== plan.id);
        return [...sinEste, plan];
      });
      return plan;
    } catch {
      this.errorSignal.set('No se pudo cargar el plan desde el servidor.');
      return undefined;
    }
  }

  // ─────────────── Lookups (superficie del mock) ───────────────

  plan(planId: string): Plan | undefined {
    return this.planesSignal().find((p) => p.id === planId);
  }

  area(id: string): AreaPei | undefined {
    return this.areasSignal().find((a) => a.id === id);
  }

  foco(focoId: string | null | undefined) {
    return this.pei().focos.find((f) => f.id === focoId);
  }

  objetivo(focoId: string | null | undefined, objetivoId: string | null | undefined) {
    return this.foco(focoId)?.objetivos.find((o) => o.id === objetivoId);
  }

  /** Ruta completa del producto estratégico (mismo contrato que el mock). */
  productoEstrategico(id: string | null | undefined) {
    if (!id) return undefined;
    for (const foco of this.pei().focos)
      for (const objetivo of foco.objetivos) {
        const producto = objetivo.productos.find((p) => p.id === id);
        if (producto) return { foco, objetivo, producto };
      }
    return undefined;
  }

  /** Incidencias del producto según la VALIDACIÓN DEL SERVIDOR (fuente de verdad). */
  rowIssues(row: ProductoPoa): string[] {
    return this.issuesPorFila.get(row.id) ?? [];
  }

  planIssueCount(plan: Plan): number {
    if (plan.rows.length > 0)
      return plan.rows.reduce((n, r) => n + (this.rowIssues(r).length ? 1 : 0), 0);
    return this.conteosPorPlan.get(plan.id)?.incompletos ?? 0;
  }

  /** Conteos del listado (calculados por el servidor sin cargar el grafo). */
  conteoProductos(planId: string): number {
    const plan = this.plan(planId);
    if (plan && plan.rows.length > 0) return plan.rows.length;
    return this.conteosPorPlan.get(planId)?.productos ?? 0;
  }

  conteoIncompletos(planId: string): number {
    return this.conteosPorPlan.get(planId)?.incompletos ?? 0;
  }

  // ─────────────── Mutaciones ───────────────

  async addRow(planId: string): Promise<ProductoPoa | null> {
    try {
      const dto = await firstValueFrom(
        this.http.post<PoaProductDto>(`${this.base}/${planId}/products`, {}),
      );
      await this.cargarWorkspace(planId);
      return this.plan(planId)?.rows.find((r) => r.id === dto.id) ?? null;
    } catch (e) {
      this.notificarError(e, 'No se pudo agregar el producto.');
      return null;
    }
  }

  /**
   * Persiste el patch y devuelve la fila ya recargada del servidor: quien llama la
   * necesita para resolver ids reales (p. ej. una tarea recién agregada solo tiene
   * un id local hasta que este PUT la crea en la BD).
   */
  async updateRow(
    planId: string,
    rowId: string,
    patch: Partial<ProductoPoa>,
  ): Promise<ProductoPoa | null> {
    await this.mutarProducto(planId, rowId, this.patchABody(patch));
    return this.plan(planId)?.rows.find((r) => r.id === rowId) ?? null;
  }

  updateRowCustom(planId: string, rowId: string, campo: string, valor: string): void {
    void this.mutarProducto(planId, rowId, { customValues: { [campo]: valor } });
  }

  deleteRow(planId: string, rowId: string): void {
    void (async () => {
      try {
        await firstValueFrom(this.http.delete(`${this.base}/${planId}/products/${rowId}`));
        await this.cargarWorkspace(planId);
      } catch (e) {
        this.notificarError(e, 'No se pudo eliminar el producto.');
      }
    })();
  }

  /** Somete el plan. Devuelve la lista de errores del SERVIDOR (vacía = sometido). */
  async someterPlan(planId: string): Promise<string[]> {
    try {
      await firstValueFrom(this.http.post(`${this.base}/${planId}/submit`, {}));
      await this.cargarWorkspace(planId);
      return [];
    } catch (e) {
      const errores = this.erroresDe(e);
      await this.cargarWorkspace(planId);
      return errores.length ? errores : ['No se pudo someter el plan.'];
    }
  }

  devolverPlan(planId: string, observacion: string): void {
    void (async () => {
      try {
        await firstValueFrom(
          this.http.post(`${this.base}/${planId}/return`, { observation: observacion }),
        );
        await this.cargarWorkspace(planId);
      } catch (e) {
        this.notificarError(e, 'No se pudo devolver el plan.');
      }
    })();
  }

  aprobarPlan(planId: string): void {
    void (async () => {
      try {
        await firstValueFrom(this.http.post(`${this.base}/${planId}/approve`, {}));
        await this.cargarWorkspace(planId);
      } catch (e) {
        this.notificarError(e, 'No se pudo aprobar el plan.');
      }
    })();
  }

  private async mutarProducto(planId: string, rowId: string, body: UpdateProductBody): Promise<void> {
    try {
      const rowVersion = this.rowVersionPorFila.get(rowId);
      await firstValueFrom(
        this.http.put(`${this.base}/${planId}/products/${rowId}`, { ...body, rowVersion }),
      );
      await this.cargarWorkspace(planId);
    } catch (e) {
      this.notificarError(e, 'No se pudo guardar el cambio del producto.');
      await this.cargarWorkspace(planId);
    }
  }

  // ─────────────── Excel (M6) ───────────────

  async descargarPlantilla(planId: string): Promise<void> {
    await this.descargarArchivo(`${this.base}/${planId}/excel-template`, 'Plantilla POA.xlsx');
  }

  async exportarPlan(planId: string): Promise<void> {
    await this.descargarArchivo(`${this.base}/${planId}/export`, 'POA.xlsx');
  }

  async exportarCiclo(cicloId: string): Promise<void> {
    await this.descargarArchivo(`${this.base}/cycle-export?cycleId=${cicloId}`, 'POA institucional.xlsx');
  }

  async importar(
    planId: string,
    archivo: File,
    modo: 'actualizar' | 'reemplazar',
    dryRun: boolean,
  ): Promise<ImportResult> {
    const form = new FormData();
    form.append('file', archivo);
    try {
      const resultado = await firstValueFrom(
        this.http.post<ImportResult>(`${this.base}/${planId}/import?modo=${modo}&dryRun=${dryRun}`, form),
      );
      if (resultado.applied) await this.cargarWorkspace(planId);
      return resultado;
    } catch (e) {
      return {
        applied: false,
        errors: this.erroresDe(e).map((m) => ({ row: 0, column: null, message: m })),
        warnings: [],
        summary: null,
      };
    }
  }


  // ─────────────── Adaptadores ───────────────

  private adaptarPlanResumen(dto: AnnualPlanListDto): Plan {
    this.conteosPorPlan.set(dto.id, {
      productos: dto.productCount,
      incompletos: dto.incompleteProductCount,
    });
    return {
      id: dto.id,
      areaId: dto.areaId,
      cicloId: dto.cycleId,
      nombre: dto.name,
      estado: dto.status as Plan['estado'],
      version: dto.version,
      fechaLimite: dto.dueDate?.slice(0, 10) ?? '',
      rows: this.plan(dto.id)?.rows ?? [],
      observacionGeneral: dto.generalObservation ?? undefined,
    };
  }

  private adaptarPlanCompleto(dto: WorkspaceDto): Plan {
    const rows = dto.products.map((p) => this.adaptarProducto(p));
    return {
      id: dto.id,
      areaId: dto.areaId,
      cicloId: dto.cycleId,
      nombre: dto.name,
      estado: dto.status as Plan['estado'],
      version: dto.version,
      fechaLimite: dto.dueDate?.slice(0, 10) ?? '',
      rows,
      observacionGeneral: dto.generalObservation ?? undefined,
    };
  }

  private adaptarProducto(dto: PoaProductDto): ProductoPoa {
    this.issuesPorFila.set(dto.id, dto.issues);
    if (dto.rowVersion) this.rowVersionPorFila.set(dto.id, dto.rowVersion);

    return {
      id: dto.id,
      codigo: dto.code,
      origen: dto.origin === 'Operational' ? 'operativo' : 'pei',
      focoId: dto.strategicFocusId,
      objetivoId: dto.strategicObjectiveId,
      productoEstrategicoId: dto.strategicProductId,
      nombreProducto: dto.productName,
      justificacionOperativa: dto.operationalJustification ?? undefined,
      unidades: dto.measureUnits.map((u) => ({
        id: u.id,
        nombre: u.name,
        metaValor: u.targetValue,
        metaTipo: u.targetType === 'Percentage' ? 'porcentaje' : 'absoluto',
        mediosVerificacion: u.verificationMeans ?? '',
        responsableAreaId: u.responsibleAreaId ?? '',
        programacion: { T1: u.q1, T2: u.q2, T3: u.q3, T4: u.q4 },
      })),
      municipiosImpacto: dto.municipalities.map((m) => m.municipioName),
      tareas: dto.tasks.map((t) => ({
        id: t.id,
        nombre: t.name,
        meses: t.months,
        continua: t.isContinuous,
      })),
      areasInvolucradasIds: dto.involvedAreas.map((a) => a.areaId),
      observacion: dto.observation ?? undefined,
      custom: { ...dto.customValues },
    };
  }

  private adaptarColumna(dto: TemplateColumnDto): ColumnaConfig {
    let opciones: string[] | undefined;
    if (dto.optionsJson) {
      try {
        opciones = JSON.parse(dto.optionsJson) as string[];
      } catch {
        opciones = undefined;
      }
    }
    return {
      id: dto.id,
      nombreVisible: dto.visibleName,
      nombreTecnico: dto.technicalName,
      tipo: TIPO_COLUMNA[dto.type] ?? 'texto',
      obligatoria: dto.required,
      visible: dto.visible,
      origen: dto.origin === 'Custom' ? 'personalizada' : 'base',
      opciones,
      version: dto.version,
      autor: '',
      fecha: '',
    };
  }

  private patchABody(patch: Partial<ProductoPoa>): UpdateProductBody {
    const body: UpdateProductBody = {};

    if (patch.origen !== undefined)
      body.origin = patch.origen === 'operativo' ? 'Operational' : 'Pei';
    if (patch.nombreProducto !== undefined) body.productName = patch.nombreProducto;
    if (patch.justificacionOperativa !== undefined)
      body.operationalJustification = patch.justificacionOperativa ?? '';
    if (patch.observacion !== undefined) body.observation = patch.observacion ?? '';

    const tocaAlineacion =
      'focoId' in patch || 'objetivoId' in patch || 'productoEstrategicoId' in patch;
    if (tocaAlineacion) {
      if (!patch.focoId && !patch.objetivoId && !patch.productoEstrategicoId) {
        body.clearAlignment = true;
      } else {
        if (patch.focoId !== undefined) body.strategicFocusId = patch.focoId;
        if (patch.objetivoId !== undefined) body.strategicObjectiveId = patch.objetivoId;
        if (patch.productoEstrategicoId !== undefined)
          body.strategicProductId = patch.productoEstrategicoId;
      }
    }

    if (patch.unidades !== undefined) {
      body.measureUnits = patch.unidades.map((u: UnidadMedidaPoa) => ({
        id: GUID_PATTERN.test(u.id) ? u.id : null,
        name: u.nombre,
        targetValue: u.metaValor,
        targetType: u.metaTipo === 'porcentaje' ? 'Percentage' : 'Absolute',
        verificationMeans: u.mediosVerificacion,
        responsibleAreaId: u.responsableAreaId || null,
        q1: u.programacion.T1,
        q2: u.programacion.T2,
        q3: u.programacion.T3,
        q4: u.programacion.T4,
      }));
    }

    if (patch.tareas !== undefined) {
      body.tasks = patch.tareas.map((t: TareaPoa) => ({
        id: GUID_PATTERN.test(t.id) ? t.id : null,
        name: t.nombre,
        isContinuous: t.continua,
        months: t.meses,
      }));
    }

    if (patch.municipiosImpacto !== undefined) {
      const catalogo = this.municipiosSignal();
      body.municipioIds = patch.municipiosImpacto
        .map((nombre) => catalogo.find((m) => m.nombre === nombre)?.id)
        .filter((id): id is string => !!id);
    }

    if (patch.areasInvolucradasIds !== undefined)
      body.involvedAreaIds = patch.areasInvolucradasIds;

    if (patch.custom !== undefined) body.customValues = { ...patch.custom };

    return body;
  }

  // ─────────────── Utilidades ───────────────

  private async descargarArchivo(url: string, nombreFallback: string): Promise<void> {
    this.descargandoSignal.set(true);
    this.notification.onToast({
      message: `Generando y descargando ${nombreFallback}…`,
      status: 'default',
      duration: 3000,
    });
    try {
      const respuesta = await firstValueFrom(
        this.http.get(url, { observe: 'response', responseType: 'blob' }),
      );
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

  private erroresDe(e: unknown): string[] {
    const detail = (e as { error?: { detail?: { errors?: string[] } } })?.error?.detail?.errors;
    return Array.isArray(detail) ? detail : [];
  }

  private notificarError(e: unknown, fallback: string): void {
    this.errorSignal.set(this.erroresDe(e)[0] ?? fallback);
  }
}

const PEI_VACIO: Pei = {
  id: '',
  nombre: '—',
  periodo: '',
  version: '',
  estado: 'Vigente',
  motivo: '',
  creadaPor: '',
  fechaCreacion: '',
  aprobadoPor: '',
  fechaAprobacion: '',
  focos: [],
  cambios: [],
};
