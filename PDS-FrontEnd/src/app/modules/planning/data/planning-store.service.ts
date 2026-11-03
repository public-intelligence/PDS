import { computed, Injectable, signal } from '@angular/core';
import {
  Area,
  ArticuloServicio,
  ArtefactoAprobacion,
  CambioPei,
  CategoriaTransversal,
  Ciclo,
  ColumnaConfig,
  DecisionPriorizacion,
  EstadoTecho,
  EventoAuditoria,
  FamiliaServicio,
  FocoEstrategico,
  MetasAnuales,
  NivelAprobador,
  ObjetivoEstrategico,
  Pei,
  Persona,
  PersonaId,
  Plan,
  ProductoEstrategico,
  ProductoPoa,
  RegistroSeguimiento,
  ReglaRuteo,
  Requerimiento,
  RequerimientoEstado,
  RubroCatalogo,
  SelloAprobacion,
  TareaPoa,
  TechoRubro,
  TipoDecision,
  Trimestre,
  UnidadMedidaPei,
} from './planning.models';
import {
  ACTIVIDAD_INICIAL,
  ANIO_ACTUAL,
  AREAS,
  ARTICULOS_SERVICIO,
  CATEGORIAS,
  CICLO_ACTIVO_ID,
  CICLOS,
  COLUMNAS_CONFIG,
  DECISIONES_PRIORIZACION,
  FAMILIAS_SERVICIO,
  montoDe,
  PEI_VERSIONES,
  PERSONAS,
  PLANES,
  REGISTROS_SEGUIMIENTO,
  REGLAS_RUTEO,
  REQUERIMIENTOS,
  RUBROS_CATALOGO,
  TECHOS_RUBRO,
} from './planning.mock-data';

/**
 * Estado en memoria del MOCKUP de Planificación (iteración 3, ajustes de
 * stakeholders). Única fuente de verdad de la demo: las páginas leen
 * signals/computed y mutan a través de acciones que además dejan rastro en la
 * actividad (trazabilidad simulada). Se restaura con reset() o recargando.
 */
@Injectable({ providedIn: 'root' })
export class PlanningStoreService {
  // ------------------------------------------------------------------ estado
  readonly personas = PERSONAS;
  readonly areas: Area[] = AREAS;
  readonly categorias: CategoriaTransversal[] = CATEGORIAS;

  readonly personaId = signal<PersonaId>('planificacion');

  /**
   * Historial de versiones del PEI. El plan estratégico no se edita en
   * caliente: sólo una versión está `Vigente` y las históricas quedan
   * congeladas. Ver `iniciarEnmienda`.
   */
  readonly peiVersiones = signal<Pei[]>(structuredClone(PEI_VERSIONES));
  readonly ciclos = signal<Ciclo[]>(structuredClone(CICLOS));
  readonly planes = signal<Plan[]>(structuredClone(PLANES));
  readonly requerimientos = signal<Requerimiento[]>(structuredClone(REQUERIMIENTOS));
  readonly reglasRuteo = signal<ReglaRuteo[]>(structuredClone(REGLAS_RUTEO));
  readonly columnasConfig = signal<ColumnaConfig[]>(structuredClone(COLUMNAS_CONFIG));
  readonly seguimiento = signal<RegistroSeguimiento[]>(structuredClone(REGISTROS_SEGUIMIENTO));
  readonly actividad = signal<EventoAuditoria[]>(structuredClone(ACTIVIDAD_INICIAL));
  /** Techos por gran rubro institucional (ajuste 10). */
  readonly techosRubro = signal<TechoRubro[]>(structuredClone(TECHOS_RUBRO));
  /** Bitácora de decisiones de priorización (ajuste 14). */
  readonly decisiones = signal<DecisionPriorizacion[]>(structuredClone(DECISIONES_PRIORIZACION));

  // Catálogo jerárquico de rubros y servicios transversales
  readonly familias = signal<FamiliaServicio[]>(structuredClone(FAMILIAS_SERVICIO));
  readonly rubrosCatalogo = signal<RubroCatalogo[]>(structuredClone(RUBROS_CATALOGO));
  readonly articulos = signal<ArticuloServicio[]>(structuredClone(ARTICULOS_SERVICIO));

  /** Validación financiera del consolidado (persona Presupuesto). */
  readonly aprobacionFinanciera = signal<'pendiente' | 'aprobada'>('pendiente');
  /** Sellos de aprobación institucional (POA y PACC por separado, ajuste 15). */
  readonly sellos = signal<SelloAprobacion[]>([]);

  private seq = 100;

  // -------------------------------------------------------------- derivados
  readonly persona = computed<Persona>(
    () => this.personas.find((p) => p.id === this.personaId())!,
  );

  /** Año de referencia: lo anterior o igual a él es historia inmutable. */
  readonly anioActual = ANIO_ACTUAL;

  /**
   * PEI vigente. Es lo que consume el POA para alinear: una enmienda en
   * elaboración NO afecta la formulación hasta que la MAE la aprueba.
   */
  readonly pei = computed<Pei>(
    () => this.peiVersiones().find((v) => v.estado === 'Vigente') ?? this.peiVersiones()[0],
  );

  /** Enmienda en curso, si la hay (como máximo una). */
  readonly peiBorrador = computed<Pei | undefined>(() =>
    this.peiVersiones().find((v) => v.estado === 'En elaboración'),
  );

  readonly peiHistoricas = computed<Pei[]>(() =>
    this.peiVersiones().filter((v) => v.estado === 'Histórico'),
  );

  readonly areaActual = computed<Area>(
    () => this.areas.find((a) => a.id === this.persona().areaId)!,
  );

  /** Unidad organizativa de menor nivel del organigrama (el despacho, nivel 0). */
  readonly areaRaiz: Area = this.areas.find((a) => a.padreId === null) ?? this.areas[0];

  /**
   * Áreas que dependen directamente de la raíz (nivel 1). Son las únicas que
   * formulan plan propio en un ciclo: los departamentos consolidan dentro del
   * plan de su dirección, no reciben uno aparte.
   */
  readonly areasNivel1: Area[] = this.areas.filter((a) => a.padreId === this.areaRaiz.id);

  /** Áreas consolidadoras: las que reciben requerimientos de alguna familia. */
  readonly areasConsolidadoras = computed<Area[]>(() => {
    const ids = new Set(this.familias().map((f) => f.areaReceptoraId));
    return this.areas.filter((a) => ids.has(a.id));
  });

  readonly cicloActivo = computed<Ciclo>(
    () => this.ciclos().find((c) => c.id === CICLO_ACTIVO_ID) ?? this.ciclos()[0],
  );

  /** Columnas agregadas por configuración, visibles en el grid del plan. */
  readonly columnasPersonalizadas = computed(() =>
    this.columnasConfig().filter((c) => c.origen === 'personalizada' && c.visible),
  );

  /** Requerimientos que comprometen presupuesto (excluye devueltos/fusionados). */
  readonly requerimientosVigentes = computed(() =>
    this.requerimientos().filter((r) => !['Devuelto', 'Fusionado'].includes(r.estado)),
  );

  /**
   * Reporte derivado de consumo por área (ajuste 10: ya no es un techo, sólo un
   * reporte). El área a la que se atribuye el gasto es la ejecutora del rubro.
   */
  readonly comprometidoPorArea = computed<Record<string, number>>(() => {
    const out: Record<string, number> = {};
    for (const r of this.requerimientosVigentes()) {
      const areaId = this.areaEjecutoraDe(r);
      out[areaId] = (out[areaId] ?? 0) + montoDe(r);
    }
    return out;
  });

  /**
   * Estados financieros completos del techo de cada GRAN RUBRO (ajuste 10).
   * inicial → ajustes → vigente → reservado / planificado / comprometido /
   * por confirmar / ejecutado → disponible.
   */
  readonly estadosTecho = computed<EstadoTecho[]>(() =>
    this.techosRubro().map((techo) => {
      const rubro = this.rubrosCatalogo().find((r) => r.id === techo.rubroId);
      const inicial = techo.montoInicial;
      const ajustes = techo.ajustes.reduce((s, x) => s + x.monto, 0);
      const vigente = inicial + ajustes;

      const delRubro = this.requerimientosVigentes().filter(
        (r) => this.rubroDe(r)?.id === techo.rubroId,
      );
      const sumar = (pred: (r: Requerimiento) => boolean) =>
        delRubro.filter(pred).reduce((s, r) => s + montoDe(r), 0);

      const reservado = sumar((r) => ['Nuevo', 'En revisión'].includes(r.estado));
      const aprobados = sumar((r) => ['Aprobado', 'Reducido'].includes(r.estado));
      const porConfirmar = sumar((r) => this.llevaCosto(r) && !r.precioConfirmado);
      const validado = this.aprobacionFinanciera() === 'aprobada';
      const planificado = validado ? 0 : aprobados;
      const comprometido = validado ? aprobados : 0;

      // Ejecutado: se estima con el avance financiero reportado en seguimiento.
      const cortes = this.seguimiento().filter((s) => s.avanceFinanciero !== null);
      const avance = cortes.length
        ? cortes.reduce((s, c) => s + (c.avanceFinanciero ?? 0), 0) / cortes.length / 100
        : 0;
      const ejecutado = Math.round(comprometido * avance);

      const disponible = vigente - (reservado + planificado + comprometido);
      return {
        rubroId: techo.rubroId,
        nombre: rubro?.nombre ?? techo.rubroId,
        inicial,
        ajustes,
        vigente,
        reservado,
        planificado,
        comprometido,
        porConfirmar,
        ejecutado,
        disponible,
        saldoProyectado: vigente - (planificado + comprometido),
        fuente: techo.fuente,
        clasificador: techo.clasificador,
      };
    }),
  );

  // ------------------------------------------------------------- consultas
  area(id: string | null | undefined): Area | undefined {
    return this.areas.find((a) => a.id === id);
  }

  categoria(id: string | null | undefined): CategoriaTransversal | undefined {
    return this.categorias.find((c) => c.id === id);
  }

  plan(id: string | null | undefined): Plan | undefined {
    return this.planes().find((p) => p.id === id);
  }

  /** Techo vigente de un rubro = monto inicial + ajustes. */
  techoRubro(rubroId: string): number {
    const t = this.techosRubro().find((x) => x.rubroId === rubroId);
    if (!t) return 0;
    return t.montoInicial + t.ajustes.reduce((s, a) => s + a.monto, 0);
  }

  // --- Catálogo de rubros --------------------------------------------------
  familia(id: string | null | undefined): FamiliaServicio | undefined {
    return this.familias().find((f) => f.id === id);
  }

  /**
   * Área consolidadora de una categoría de apoyo (ajuste 5): la regla activa
   * manda; si no hay, el área destino declarada en la categoría.
   */
  consolidadoraDeCategoria(categoriaId: string | null | undefined): Area | undefined {
    if (!categoriaId) return undefined;
    const regla = this.reglasRuteo().find((r) => r.categoriaId === categoriaId && r.activa);
    const cat = this.categoria(categoriaId);
    return this.area(regla?.areaDestinoId ?? cat?.areaDestinoId);
  }

  /**
   * Rubro del catálogo asociado a un requerimiento. Resuelve por id y, si el
   * requerimiento es anterior al catálogo, por nombre normalizado (sin
   * acentos, mayúsculas ni plural) para no dejarlo sin regla en silencio.
   */
  rubroDe(req: Pick<Requerimiento, 'rubroId' | 'rubro'>): RubroCatalogo | undefined {
    const porId = req.rubroId ? this.rubrosCatalogo().find((r) => r.id === req.rubroId) : undefined;
    if (porId) return porId;
    const norm = (s: string) =>
      s
        .toLowerCase()
        .normalize('NFD')
        .replace(/[̀-ͯ]/g, '')
        .replace(/s\b/g, '')
        .trim();
    const objetivo = norm(req.rubro);
    return this.rubrosCatalogo().find((r) => norm(r.nombre) === objetivo);
  }

  rubrosDeFamilia(familiaId: string): RubroCatalogo[] {
    return this.rubrosCatalogo().filter((r) => r.familiaId === familiaId);
  }

  articulosDeRubro(rubroId: string): ArticuloServicio[] {
    return this.articulos().filter((a) => a.rubroId === rubroId);
  }

  rubroPorNombre(nombre: string): RubroCatalogo | undefined {
    return nombre ? this.rubroDe({ rubro: nombre }) : undefined;
  }

  /**
   * `false` = apoyo que se atiende con capacidad instalada: no lleva monto ni
   * fuente y no consume techo. Si el rubro no está en el catálogo se asume que
   * cuesta, que es la opción conservadora para el control presupuestario.
   */
  llevaCosto(req: Pick<Requerimiento, 'rubroId' | 'rubro'>): boolean {
    return this.rubroDe(req)?.llevaCosto !== false;
  }

  /**
   * Área que EJECUTA o contrata el gasto (ajuste 10). Ya no define de qué techo
   * se descuenta —los techos son por rubro institucional—, sólo alimenta el
   * reporte de «ejecución cruzada»:
   * - 'consolidadora': lo ejecuta el área consolidadora que recibe el rubro.
   * - 'requiriente': queda en el área que formuló la necesidad.
   */
  areaEjecutoraDe(req: Requerimiento): string {
    const rubro = this.rubroDe(req);
    if (rubro?.ejecutor === 'consolidadora') {
      const familia = this.familia(rubro.familiaId);
      const regla = this.reglasRuteo().find((r) => r.categoriaId === req.categoriaId && r.activa);
      return regla?.areaDestinoId ?? familia?.areaReceptoraId ?? req.origenAreaId;
    }
    return req.origenAreaId;
  }

  /** Requerimientos que otras áreas ejecutan por cuenta de esta (reporte). */
  ejecucionCruzada(areaId: string): Requerimiento[] {
    return this.requerimientosVigentes().filter(
      (r) => this.areaEjecutoraDe(r) === areaId && r.origenAreaId !== areaId,
    );
  }

  // --- Origen del requerimiento: producto › tarea ---------------------------
  /** Producto (nivel macro) del POA del que proviene el requerimiento. */
  productoDe(req: Requerimiento): ProductoPoa | undefined {
    return this.plan(req.planId)?.rows.find((r) => r.id === req.rowId);
  }

  /** Tarea concreta que necesita el apoyo. */
  tareaDe(req: Requerimiento): TareaPoa | undefined {
    return this.productoDe(req)?.tareas.find((t) => t.id === req.tareaId);
  }

  /**
   * Requerimientos vivos que dependen de una tarea. Si hay alguno, la tarea no
   * puede eliminarse: dejaría el requerimiento sin origen.
   */
  requerimientosDeTarea(tareaId: string): Requerimiento[] {
    return this.requerimientos().filter((r) => r.tareaId === tareaId && r.estado !== 'Fusionado');
  }

  /** Consumo vigente de un gran rubro institucional (todas las áreas). */
  comprometidoRubro(rubroId: string): number {
    return this.requerimientosVigentes()
      .filter((r) => this.rubroDe(r)?.id === rubroId)
      .reduce((s, r) => s + montoDe(r), 0);
  }

  foco(id: string | null | undefined): FocoEstrategico | undefined {
    return this.pei().focos.find((f) => f.id === id);
  }

  objetivo(focoId: string | null | undefined, objetivoId: string | null | undefined): ObjetivoEstrategico | undefined {
    return this.foco(focoId)?.objetivos.find((o) => o.id === objetivoId);
  }

  productoEstrategico(id: string | null | undefined):
    | { foco: FocoEstrategico; objetivo: ObjetivoEstrategico; producto: ProductoEstrategico }
    | undefined {
    if (!id) return undefined;
    for (const foco of this.pei().focos) {
      for (const objetivo of foco.objetivos) {
        const producto = objetivo.productos.find((p) => p.id === id);
        if (producto) return { foco, objetivo, producto };
      }
    }
    return undefined;
  }

  /**
   * Completitud de un producto del POA. Un producto del PEI exige alineación de
   * 2 niveles (foco → objetivo); uno OPERATIVO (ajuste 2) no la exige, pero sí
   * una justificación. En ambos casos: ≥1 unidad con nombre, meta > 0,
   * responsable (área), ≥1 municipio, medios de verificación y programación
   * trimestral consistente con la meta. Las tareas no puntúan.
   */
  rowIssues(row: ProductoPoa): string[] {
    const issues: string[] = [];
    if (!row.nombreProducto?.trim()) issues.push('Falta el producto');
    if (row.origen === 'operativo') {
      if (!row.justificacionOperativa?.trim()) {
        issues.push('Falta la justificación del producto operativo');
      }
    } else if (!row.focoId || !row.objetivoId) {
      issues.push('Falta la alineación estratégica');
    }
    if (!row.unidades.length) {
      issues.push('Falta al menos una unidad de medida');
    }
    for (const u of row.unidades) {
      const etiqueta = row.unidades.length > 1 ? ` (${u.nombre || 'unidad sin nombre'})` : '';
      if (!u.nombre?.trim()) issues.push('Unidad de medida sin nombre');
      if (!u.metaValor || u.metaValor <= 0) issues.push('Falta la meta' + etiqueta);
      if (!u.responsableAreaId) issues.push('Falta el área responsable' + etiqueta);
      if (!u.mediosVerificacion?.trim()) issues.push('Falta el medio de verificación' + etiqueta);
      const suma = u.programacion.T1 + u.programacion.T2 + u.programacion.T3 + u.programacion.T4;
      if (u.metaValor > 0) {
        if (u.metaTipo === 'absoluto' && suma !== u.metaValor) {
          issues.push('La programación trimestral no suma la meta' + etiqueta);
        }
        if (u.metaTipo === 'porcentaje' && u.programacion.T4 !== u.metaValor) {
          issues.push('El T4 debe alcanzar la meta anual' + etiqueta);
        }
      }
    }
    if (!row.municipiosImpacto.length) issues.push('Faltan los municipios de impacto');
    for (const col of this.columnasPersonalizadas()) {
      if (col.obligatoria && !row.custom[col.nombreTecnico]?.trim()) {
        issues.push(`Falta «${col.nombreVisible}»`);
      }
    }
    return issues;
  }

  planIssueCount(plan: Plan): number {
    return plan.rows.reduce((n, r) => n + (this.rowIssues(r).length ? 1 : 0), 0);
  }

  /**
   * Rubros del catálogo disponibles al crear un requerimiento (ajuste 6): los
   * que las reglas activas de la categoría enrutan, filtrados a la familia de
   * la consolidadora y a los habilitados institucionalmente (ajuste 8).
   */
  rubrosDeCategoria(categoriaId: string): RubroCatalogo[] {
    const reglas = this.reglasRuteo().filter((r) => r.categoriaId === categoriaId && r.activa);
    const ids = new Set(reglas.flatMap((r) => r.rubroIds));
    return this.rubrosCatalogo().filter((r) => ids.has(r.id) && r.habilitadoInstitucional);
  }

  // -------------------------------------------------------------- acciones
  setPersona(id: PersonaId): void {
    this.personaId.set(id);
  }

  private registrar(modulo: string, accion: string, detalle: string): void {
    const ahora = new Date();
    const fecha = ahora.toISOString().slice(0, 10) + ' ' + ahora.toTimeString().slice(0, 5);
    this.actividad.update((list) => [
      { id: `ev-${this.seq++}`, fecha, persona: this.persona().nombre, modulo, accion, detalle },
      ...list,
    ]);
  }

  // --- Ciclos -------------------------------------------------------------
  crearCiclo(datos: Omit<Ciclo, 'id' | 'estado'>): Ciclo {
    const nuevo: Ciclo = { ...datos, id: `ciclo-${this.seq++}`, estado: 'Borrador' };
    this.ciclos.update((c) => [...c, nuevo]);
    this.registrar('Ciclos', 'Creación de ciclo', `Ciclo «${nuevo.nombre}» creado en borrador.`);
    return nuevo;
  }

  publicarCiclo(id: string): void {
    this.ciclos.update((list) =>
      list.map((c) =>
        c.id === id
          ? { ...c, estado: 'Publicado', fechaPublicacion: new Date().toISOString().slice(0, 10) }
          : c,
      ),
    );
    const ciclo = this.ciclos().find((c) => c.id === id);
    this.registrar('Ciclos', 'Publicación de ciclo', `Ciclo «${ciclo?.nombre}» publicado.`);
  }

  // --- PEI: versionado, enmiendas e inmutabilidad --------------------------

  /**
   * ¿La meta de ese año se puede tocar? Los años ya transcurridos (o el
   * corriente) son historia ejecutada: se muestran, pero no se editan.
   */
  metaEditable(anio: number): boolean {
    return anio > this.anioActual;
  }

  /** Años del período con su condición de editables. */
  aniosPei(): { anio: number; clave: keyof MetasAnuales; editable: boolean }[] {
    return [
      { anio: 2025, clave: 'a2025' as const, editable: this.metaEditable(2025) },
      { anio: 2026, clave: 'a2026' as const, editable: this.metaEditable(2026) },
      { anio: 2027, clave: 'a2027' as const, editable: this.metaEditable(2027) },
      { anio: 2028, clave: 'a2028' as const, editable: this.metaEditable(2028) },
    ];
  }

  /** Una unidad tiene historia si comprometió metas en años ya transcurridos. */
  unidadTieneHistoria(u: UnidadMedidaPei): boolean {
    return this.aniosPei().some((a) => !a.editable && (u.metasAnuales[a.clave] ?? 0) > 0);
  }

  productoTieneHistoria(p: ProductoEstrategico): boolean {
    return p.unidades.some((u) => this.unidadTieneHistoria(u));
  }

  /** Elemento vigente en un año dado (no descontinuado antes de ese año). */
  vigenteEn(elemento: { descontinuadoDesde?: number }, anio = this.anioActual): boolean {
    return !elemento.descontinuadoDesde || elemento.descontinuadoDesde > anio;
  }

  /** Sólo se edita la enmienda en curso, y sólo la persona de Planificación. */
  puedeEditarPei(): boolean {
    return !!this.peiBorrador() && this.store_personaEsPlanificacion();
  }

  private store_personaEsPlanificacion(): boolean {
    return this.personaId() === 'planificacion';
  }

  /**
   * Abre una enmienda: clona la versión vigente en estado «En elaboración».
   * Exige una justificación porque modificar el PEI es un acto excepcional.
   */
  iniciarEnmienda(motivo: string): Pei | undefined {
    if (this.peiBorrador()) return undefined;
    const vigente = this.pei();
    const siguiente = this.siguienteNumeroVersion(vigente.version);
    const borrador: Pei = {
      ...structuredClone(vigente),
      id: `pei-${this.seq++}`,
      version: siguiente,
      estado: 'En elaboración',
      motivo,
      creadaPor: this.persona().nombre,
      fechaCreacion: new Date().toISOString().slice(0, 10),
      aprobadoPor: '',
      fechaAprobacion: '',
      cambios: [],
    };
    this.peiVersiones.update((l) => [borrador, ...l]);
    this.registrar(
      'Plan estratégico',
      'Apertura de enmienda',
      `Enmienda ${siguiente} abierta. Justificación: ${motivo}`,
    );
    return borrador;
  }

  private siguienteNumeroVersion(actual: string): string {
    const mayor = Number(actual.replace(/^v/, '').split('.')[0]) || 1;
    return `v${mayor + 1}.0`;
  }

  descartarEnmienda(): void {
    const b = this.peiBorrador();
    if (!b) return;
    this.peiVersiones.update((l) => l.filter((v) => v.id !== b.id));
    this.registrar('Plan estratégico', 'Enmienda descartada', `Se descartó la enmienda ${b.version}.`);
  }

  /**
   * Aprobación de la MAE: la enmienda pasa a vigente y la anterior queda
   * congelada como histórica. A partir de aquí el POA alinea contra la nueva.
   */
  aprobarEnmienda(): void {
    const b = this.peiBorrador();
    if (!b) return;
    const persona = this.persona();
    this.peiVersiones.update((l) =>
      l.map((v) => {
        if (v.id === b.id) {
          return {
            ...v,
            estado: 'Vigente' as const,
            aprobadoPor: `${persona.nombre} (MAE)`,
            fechaAprobacion: new Date().toISOString().slice(0, 10),
          };
        }
        if (v.estado === 'Vigente') return { ...v, estado: 'Histórico' as const };
        return v;
      }),
    );
    this.registrar(
      'Plan estratégico',
      'Aprobación de enmienda',
      `${b.version} aprobada por ${persona.nombre}; la versión anterior quedó histórica.`,
    );
  }

  // --- PEI: CRUD sobre la enmienda en curso --------------------------------

  /**
   * Aplica un cambio SOBRE LA ENMIENDA EN CURSO y lo anota en su registro de
   * cambios. Si no hay enmienda abierta no hace nada: las versiones vigente e
   * histórica son inmutables por diseño.
   */
  private mutarPei(
    fn: (pei: Pei) => void,
    accion: string,
    detalle: string,
    cambio?: { tipo: CambioPei['tipo']; nivel: CambioPei['nivel'] },
  ): void {
    const borrador = this.peiBorrador();
    if (!borrador) return;
    this.peiVersiones.update((list) =>
      list.map((v) => {
        if (v.id !== borrador.id) return v;
        const copia = structuredClone(v);
        fn(copia);
        if (cambio) {
          copia.cambios = [
            ...copia.cambios,
            {
              fecha: new Date().toISOString().slice(0, 10),
              autor: this.persona().nombre,
              tipo: cambio.tipo,
              nivel: cambio.nivel,
              descripcion: detalle,
            },
          ];
        }
        return copia;
      }),
    );
    this.registrar('Plan estratégico', accion, detalle);
  }

  agregarFoco(nombre: string, alineacionEnd: string): void {
    this.mutarPei(
      (p) => p.focos.push({ id: `foco-${this.seq++}`, nombre, alineacionEnd, objetivos: [] }),
      'Nuevo foco estratégico',
      `Foco «${nombre}» agregado.`,
      { tipo: 'Alta', nivel: 'Foco' },
    );
  }

  editarFoco(focoId: string, nombre: string): void {
    this.mutarPei(
      (p) => {
        const f = p.focos.find((x) => x.id === focoId);
        if (f) f.nombre = nombre;
      },
      'Edición de foco',
      `Foco renombrado a «${nombre}».`,
      { tipo: 'Edición', nivel: 'Foco' },
    );
  }

  eliminarFoco(focoId: string): boolean {
    const foco = this.peiBorrador()?.focos.find((f) => f.id === focoId);
    if (!foco) return false;
    if (this.focoTieneHistoria(foco)) return false;
    this.mutarPei(
      (p) => (p.focos = p.focos.filter((x) => x.id !== focoId)),
      'Eliminación de foco',
      `Foco «${foco.nombre}» eliminado (sin metas ejecutadas).`,
      { tipo: 'Edición', nivel: 'Foco' },
    );
    return true;
  }

  agregarObjetivo(focoId: string, nombre: string): void {
    this.mutarPei(
      (p) => {
        const f = p.focos.find((x) => x.id === focoId);
        if (!f) return;
        f.objetivos.push({
          id: `obj-${this.seq++}`,
          codigo: `OE ${p.focos.indexOf(f) + 1}.${f.objetivos.length + 1}`,
          nombre,
          productos: [],
        });
      },
      'Nuevo objetivo estratégico',
      `Objetivo «${nombre}» agregado.`,
      { tipo: 'Alta', nivel: 'Objetivo' },
    );
  }

  editarObjetivo(focoId: string, objetivoId: string, nombre: string): void {
    this.mutarPei(
      (p) => {
        const o = p.focos.find((x) => x.id === focoId)?.objetivos.find((x) => x.id === objetivoId);
        if (o) o.nombre = nombre;
      },
      'Edición de objetivo',
      `Objetivo renombrado a «${nombre}».`,
      { tipo: 'Edición', nivel: 'Objetivo' },
    );
  }

  eliminarObjetivo(focoId: string, objetivoId: string): boolean {
    const obj = this.peiBorrador()
      ?.focos.find((f) => f.id === focoId)
      ?.objetivos.find((o) => o.id === objetivoId);
    if (!obj) return false;
    if (obj.productos.some((pr) => this.productoTieneHistoria(pr))) {
      return false;
    }
    this.mutarPei(
      (p) => {
        const f = p.focos.find((x) => x.id === focoId);
        if (f) f.objetivos = f.objetivos.filter((x) => x.id !== objetivoId);
      },
      'Eliminación de objetivo',
      `Objetivo «${obj.nombre}» eliminado (sin metas ejecutadas).`,
      { tipo: 'Edición', nivel: 'Objetivo' },
    );
    return true;
  }

  agregarProducto(focoId: string, objetivoId: string, nombre: string): void {
    this.mutarPei(
      (p) => {
        const o = p.focos.find((x) => x.id === focoId)?.objetivos.find((x) => x.id === objetivoId);
        o?.productos.push({ id: `prod-${this.seq++}`, nombre, unidades: [], areasInvolucradasIds: [] });
      },
      'Nuevo producto estratégico',
      `Producto «${nombre}» agregado.`,
      { tipo: 'Alta', nivel: 'Producto' },
    );
  }

  editarProducto(
    productoId: string,
    patch: Partial<Pick<ProductoEstrategico, 'nombre' | 'areasInvolucradasIds'>>,
  ): void {
    this.mutarPei(
      (p) => {
        for (const f of p.focos)
          for (const o of f.objetivos) {
            const prod = o.productos.find((x) => x.id === productoId);
            if (prod) Object.assign(prod, patch);
          }
      },
      'Edición de producto',
      `Producto «${patch.nombre ?? ''}» actualizado.`,
      { tipo: 'Edición', nivel: 'Producto' },
    );
  }

  /**
   * Sólo se puede eliminar un producto que nunca comprometió metas en años
   * transcurridos. Si tiene historia, la vía correcta es descontinuarlo.
   */
  eliminarProducto(productoId: string): boolean {
    const prod = this.buscarProductoEnBorrador(productoId);
    if (!prod || this.productoTieneHistoria(prod)) return false;
    this.mutarPei(
      (p) => {
        for (const f of p.focos)
          for (const o of f.objetivos) {
            o.productos = o.productos.filter((x) => x.id !== productoId);
          }
      },
      'Eliminación de producto',
      `Producto «${prod.nombre}» eliminado (sin metas ejecutadas).`,
      { tipo: 'Edición', nivel: 'Producto' },
    );
    return true;
  }

  /**
   * Descontinuar es la alternativa a eliminar cuando hay historia: el producto
   * deja de medirse a partir de un año futuro, pero sus metas pasadas siguen
   * siendo consultables.
   */
  descontinuarProducto(productoId: string, desde: number, motivo: string): void {
    const prod = this.buscarProductoEnBorrador(productoId);
    this.mutarPei(
      (p) => {
        for (const f of p.focos)
          for (const o of f.objetivos) {
            const x = o.productos.find((pr) => pr.id === productoId);
            if (x) {
              x.descontinuadoDesde = desde;
              x.motivoDescontinuacion = motivo;
            }
          }
      },
      'Descontinuación de producto',
      `«${prod?.nombre}» se descontinúa desde ${desde}. Motivo: ${motivo}`,
      { tipo: 'Descontinuación', nivel: 'Producto' },
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

  agregarUnidadPei(productoId: string, unidad: Omit<UnidadMedidaPei, 'id'>): void {
    this.mutarPei(
      (p) => {
        for (const f of p.focos)
          for (const o of f.objetivos) {
            const prod = o.productos.find((x) => x.id === productoId);
            prod?.unidades.push({ ...unidad, id: `um-${this.seq++}` });
          }
      },
      'Nueva unidad de medida',
      `Unidad «${unidad.nombre}» agregada.`,
      { tipo: 'Alta', nivel: 'Unidad de medida' },
    );
  }

  descontinuarUnidad(productoId: string, unidadId: string, desde: number, motivo: string): void {
    this.mutarPei(
      (p) => {
        for (const f of p.focos)
          for (const o of f.objetivos) {
            const u = o.productos.find((x) => x.id === productoId)?.unidades.find((x) => x.id === unidadId);
            if (u) {
              u.descontinuadoDesde = desde;
              u.motivoDescontinuacion = motivo;
            }
          }
      },
      'Descontinuación de unidad de medida',
      `Unidad descontinuada desde ${desde}. Motivo: ${motivo}`,
      { tipo: 'Descontinuación', nivel: 'Unidad de medida' },
    );
  }

  /**
   * Guarda las unidades editadas en el drawer preservando lo inmutable: las
   * metas de años transcurridos se restauran desde la versión guardada aunque
   * el formulario intente cambiarlas, y no se admite quitar una unidad con
   * historia (para eso está `descontinuarUnidad`).
   */
  reemplazarUnidadesPei(productoId: string, unidades: UnidadMedidaPei[]): void {
    const original = this.buscarProductoEnBorrador(productoId);
    if (!original) return;
    const bloqueados = this.aniosPei().filter((a) => !a.editable);

    const saneadas: UnidadMedidaPei[] = unidades.map((u) => {
      const previa = original.unidades.find((x) => x.id === u.id);
      if (!previa) return { ...u, id: u.id || `um-${this.seq++}` };
      const metas = { ...u.metasAnuales };
      for (const a of bloqueados) metas[a.clave] = previa.metasAnuales[a.clave];
      return { ...u, metasAnuales: metas };
    });

    // Reincorpora las unidades con historia que el formulario haya quitado.
    for (const previa of original.unidades) {
      if (!saneadas.some((u) => u.id === previa.id) && this.unidadTieneHistoria(previa)) {
        saneadas.push(previa);
      }
    }

    this.mutarPei(
      (p) => {
        for (const f of p.focos)
          for (const o of f.objetivos) {
            const prod = o.productos.find((x) => x.id === productoId);
            if (prod) prod.unidades = saneadas;
          }
      },
      'Edición de unidades de medida',
      `Unidades de «${original.nombre}» actualizadas (metas de años transcurridos preservadas).`,
      { tipo: 'Edición', nivel: 'Unidad de medida' },
    );
  }

  eliminarUnidadPei(productoId: string, unidadId: string): boolean {
    const prod = this.buscarProductoEnBorrador(productoId);
    const u = prod?.unidades.find((x) => x.id === unidadId);
    if (!u || this.unidadTieneHistoria(u)) return false;
    this.mutarPei(
      (p) => {
        for (const f of p.focos)
          for (const o of f.objetivos) {
            const x = o.productos.find((pr) => pr.id === productoId);
            if (x) x.unidades = x.unidades.filter((y) => y.id !== unidadId);
          }
      },
      'Eliminación de unidad de medida',
      `Unidad «${u.nombre}» eliminada (sin metas ejecutadas).`,
      { tipo: 'Edición', nivel: 'Unidad de medida' },
    );
    return true;
  }

  private focoTieneHistoria(f: FocoEstrategico): boolean {
    return f.objetivos.some((o) => o.productos.some((p) => this.productoTieneHistoria(p)));
  }

  // --- Plan anual ---------------------------------------------------------
  updateRow(planId: string, rowId: string, patch: Partial<ProductoPoa>): void {
    this.planes.update((list) =>
      list.map((p) =>
        p.id !== planId
          ? p
          : {
              ...p,
              estado: p.estado === 'Sin iniciar' ? 'Borrador' : p.estado,
              rows: p.rows.map((r) => (r.id === rowId ? { ...r, ...patch } : r)),
            },
      ),
    );
  }

  updateRowCustom(planId: string, rowId: string, campo: string, valor: string): void {
    this.planes.update((list) =>
      list.map((p) =>
        p.id !== planId
          ? p
          : {
              ...p,
              rows: p.rows.map((r) =>
                r.id === rowId ? { ...r, custom: { ...r.custom, [campo]: valor } } : r,
              ),
            },
      ),
    );
  }

  addRow(planId: string): ProductoPoa {
    const plan = this.plan(planId)!;
    const area = this.area(plan.areaId)!;
    const prefijo = area.corta.slice(0, 2).toUpperCase();
    const row: ProductoPoa = {
      id: `row-${this.seq++}`,
      codigo: `${prefijo}-${String(plan.rows.length + 1).padStart(2, '0')}`,
      origen: 'pei',
      focoId: null,
      objetivoId: null,
      productoEstrategicoId: null,
      nombreProducto: '',
      unidades: [
        {
          id: `upoa-${this.seq++}`,
          nombre: '',
          metaValor: 0,
          metaTipo: 'absoluto',
          mediosVerificacion: '',
          responsableAreaId: plan.areaId,
          programacion: { T1: 0, T2: 0, T3: 0, T4: 0 },
        },
      ],
      municipiosImpacto: [],
      tareas: [],
      areasInvolucradasIds: [],
      custom: {},
    };
    this.planes.update((list) =>
      list.map((p) =>
        p.id !== planId
          ? p
          : { ...p, estado: p.estado === 'Sin iniciar' ? 'Borrador' : p.estado, rows: [...p.rows, row] },
      ),
    );
    return row;
  }

  deleteRow(planId: string, rowId: string): void {
    this.planes.update((list) =>
      list.map((p) => (p.id !== planId ? p : { ...p, rows: p.rows.filter((r) => r.id !== rowId) })),
    );
  }

  /**
   * Carga desde la plantilla Excel/CSV. `actualizar` respeta los productos que
   * no vienen en el archivo; `reemplazar` deja el plan igual al archivo.
   */
  importarProductos(
    planId: string,
    productos: ProductoPoa[],
    modo: 'actualizar' | 'reemplazar',
  ): { agregados: number; actualizados: number; conservados: number } {
    const plan = this.plan(planId)!;
    const codigosArchivo = new Set(productos.map((p) => p.codigo));
    const actualizados = plan.rows.filter((r) => codigosArchivo.has(r.codigo)).length;
    const agregados = productos.length - actualizados;
    const conservados =
      modo === 'actualizar' ? plan.rows.filter((r) => !codigosArchivo.has(r.codigo)).length : 0;

    const rows = this.filasTrasImportar(plan, productos, modo);

    this.planes.update((list) =>
      list.map((p) =>
        p.id !== planId
          ? p
          : { ...p, estado: p.estado === 'Sin iniciar' ? 'Borrador' : p.estado, rows },
      ),
    );
    this.registrar(
      'Plan anual',
      'Importación desde plantilla',
      `${plan.nombre}: ${agregados} producto(s) agregado(s), ${actualizados} actualizado(s), ${conservados} conservado(s) (modo ${modo}).`,
    );
    return { agregados, actualizados, conservados };
  }

  /** Filas con las que quedaría el plan según el modo de importación. */
  private filasTrasImportar(
    plan: Plan,
    productos: ProductoPoa[],
    modo: 'actualizar' | 'reemplazar',
  ): ProductoPoa[] {
    if (modo === 'reemplazar') return productos;
    return [
      ...plan.rows.map((r) => productos.find((p) => p.codigo === r.codigo) ?? r),
      ...productos.filter((p) => !plan.rows.some((r) => r.codigo === p.codigo)),
    ];
  }

  /**
   * Requerimientos que quedarían sin origen si se aplicara la importación: la
   * carga no puede borrar (ni renombrar) un producto o una tarea que tenga
   * apoyo vivo enlazado.
   */
  requerimientosHuerfanosTras(
    planId: string,
    productos: ProductoPoa[],
    modo: 'actualizar' | 'reemplazar',
  ): Requerimiento[] {
    const plan = this.plan(planId);
    if (!plan) return [];
    const tareasVigentes = new Set(
      this.filasTrasImportar(plan, productos, modo).flatMap((p) => p.tareas.map((t) => t.id)),
    );
    return this.requerimientos().filter(
      (r) => r.planId === planId && r.estado !== 'Fusionado' && !tareasVigentes.has(r.tareaId),
    );
  }

  /**
   * Flujo clave del discovery: marcar una necesidad transversal en una TAREA
   * del POA crea automáticamente el requerimiento (formato PACC) en la bandeja
   * del área consolidadora según la regla de ruteo activa.
   *
   * La tarea es obligatoria. Devuelve `null` si la tarea no existe.
   */
  marcarRequerimiento(
    planId: string,
    rowId: string,
    tareaId: string,
    categoriaId: string,
    datos: {
      rubro: string;
      rubroId?: string;
      articuloId?: string;
      descripcion: string;
      cantidad: number;
      unidadMedida: string;
      precioEstimado: number;
      trimestre: Trimestre;
      fechaRequerida?: string;
      fuenteFinanciamiento: string;
    },
  ): Requerimiento | null {
    const plan = this.plan(planId)!;
    const row = plan.rows.find((r) => r.id === rowId)!;
    const tarea = row.tareas.find((t) => t.id === tareaId);
    if (!tarea) return null;
    const categoria = this.categoria(categoriaId)!;
    const regla = this.reglasRuteo().find((rr) => rr.categoriaId === categoriaId && rr.activa);
    const destino = this.area(regla?.areaDestinoId ?? categoria.areaDestinoId)!;

    const rubroCatalogo = datos.rubroId
      ? this.rubrosCatalogo().find((r) => r.id === datos.rubroId)
      : this.rubrosCatalogo().find((r) => r.nombre === datos.rubro);
    const req: Requerimiento = {
      id: `req-${this.seq++}`,
      codigo: `RQ-2027-${String(this.requerimientos().length + 1).padStart(3, '0')}`,
      origenAreaId: plan.areaId,
      planId,
      rowId,
      tareaId,
      categoriaId,
      rubro: datos.rubro,
      rubroId: rubroCatalogo?.id,
      articuloId: datos.articuloId,
      descripcion: datos.descripcion || tarea.nombre,
      cantidad: datos.cantidad,
      unidadMedida: datos.unidadMedida,
      precioEstimado: datos.precioEstimado,
      precioConfirmado: false,
      trimestre: datos.trimestre,
      fechaRequerida: datos.fechaRequerida,
      fuenteFinanciamiento: datos.fuenteFinanciamiento,
      prioridad: 'Media',
      estado: 'Nuevo',
      reglaAplicada: `Regla ${regla?.id ?? '—'} v${regla?.version ?? 1}: rubro «${datos.rubro}» → ${destino.nombre}`,
      comentarios: [],
    };
    this.requerimientos.update((list) => [req, ...list]);
    this.registrar(
      'Requerimientos',
      'Ruteo automático',
      `${req.codigo} (${categoria.nombre}) enviado a ${destino.corta} desde ${row.codigo} › «${tarea.nombre}».`,
    );
    return req;
  }

  /**
   * El área requiriente sólo puede tocar su requerimiento mientras no esté en
   * manos del área consolidadora: `Nuevo` o `Devuelto`.
   */
  requerimientoEditable(req: Requerimiento): boolean {
    return ['Nuevo', 'Devuelto'].includes(req.estado);
  }

  /** Edita un requerimiento y vuelve a aplicar la regla de ruteo vigente. */
  actualizarRequerimiento(
    id: string,
    datos: {
      tareaId: string;
      categoriaId: string;
      rubro: string;
      rubroId?: string;
      articuloId?: string;
      descripcion: string;
      cantidad: number;
      unidadMedida: string;
      precioEstimado: number;
      trimestre: Trimestre;
      fechaRequerida?: string;
      fuenteFinanciamiento: string;
    },
  ): Requerimiento | null {
    const previo = this.requerimientos().find((r) => r.id === id);
    if (!previo || !this.requerimientoEditable(previo)) return null;

    const categoria = this.categoria(datos.categoriaId)!;
    const regla = this.reglasRuteo().find((rr) => rr.categoriaId === datos.categoriaId && rr.activa);
    const destino = this.area(regla?.areaDestinoId ?? categoria.areaDestinoId)!;
    const rubroCatalogo = datos.rubroId
      ? this.rubrosCatalogo().find((r) => r.id === datos.rubroId)
      : this.rubrosCatalogo().find((r) => r.nombre === datos.rubro);

    const actualizado: Requerimiento = {
      ...previo,
      ...datos,
      rubroId: rubroCatalogo?.id,
      montoAprobado: undefined,
      precioConfirmado: false,
      estado: 'Nuevo',
      reglaAplicada: `Regla ${regla?.id ?? '—'} v${regla?.version ?? 1}: rubro «${datos.rubro}» → ${destino.nombre}`,
    };
    this.requerimientos.update((list) => list.map((r) => (r.id === id ? actualizado : r)));

    const cambioDestino = previo.categoriaId !== datos.categoriaId;
    this.registrar(
      'Requerimientos',
      'Edición del requerimiento',
      `${actualizado.codigo} actualizado por el área requiriente${cambioDestino ? ` y re-enrutado a ${destino.corta}` : ''}.`,
    );
    return actualizado;
  }

  /**
   * El precio lo pone el área consolidadora, que es la que conoce el mercado:
   * el área requiriente sólo trae el precio de referencia del catálogo.
   */
  estimarPrecio(id: string, precioUnitario: number): void {
    const req = this.requerimientos().find((r) => r.id === id);
    if (!req) return;
    this.requerimientos.update((list) =>
      list.map((r) =>
        r.id === id ? { ...r, precioEstimado: precioUnitario, precioConfirmado: true } : r,
      ),
    );
    this.registrar(
      'Requerimientos',
      'Estimación de costo',
      `${req.codigo}: ${this.area(this.persona().areaId)?.corta} confirmó ${precioUnitario} por ${req.unidadMedida.toLowerCase()} (${req.cantidad} × ${precioUnitario}).`,
    );
  }

  eliminarRequerimiento(id: string): void {
    const req = this.requerimientos().find((r) => r.id === id);
    if (!req || !this.requerimientoEditable(req)) return;
    this.requerimientos.update((list) => list.filter((r) => r.id !== id));
    this.registrar(
      'Requerimientos',
      'Eliminación del requerimiento',
      `${req.codigo} («${req.descripcion}») eliminado por el área requiriente.`,
    );
  }

  someterPlan(planId: string): void {
    this.planes.update((list) =>
      list.map((p) =>
        p.id === planId ? { ...p, estado: 'Listo para revisión', version: p.version + 1 } : p,
      ),
    );
    const plan = this.plan(planId)!;
    this.registrar('Plan anual', 'Sometimiento a revisión', `«${plan.nombre}» sometido (v${plan.version}).`);
  }

  devolverPlan(planId: string, observacion: string): void {
    this.planes.update((list) =>
      list.map((p) =>
        p.id === planId ? { ...p, estado: 'Observado', observacionGeneral: observacion } : p,
      ),
    );
    const plan = this.plan(planId)!;
    this.registrar('Plan anual', 'Devolución con observaciones', `«${plan.nombre}»: ${observacion}`);
  }

  aprobarPlan(planId: string): void {
    this.planes.update((list) =>
      list.map((p) => (p.id === planId ? { ...p, estado: 'Aprobado' } : p)),
    );
    const plan = this.plan(planId)!;
    this.registrar('Consolidación', 'Aprobación de subconjunto', `«${plan.nombre}» aprobado.`);
  }

  // --- Fase de ajustes y priorización (ajustes 11 y 12) --------------------

  /**
   * Abre la fase formal de ajustes (ajuste 11): los planes ya sometidos pasan a
   * «En ajustes» para que las áreas revisen y prioricen según los recursos.
   */
  abrirFaseAjustes(): void {
    let n = 0;
    this.planes.update((list) =>
      list.map((p) => {
        if (['Listo para revisión', 'Observado'].includes(p.estado)) {
          n++;
          return { ...p, estado: 'En ajustes' as const };
        }
        return p;
      }),
    );
    this.registrar(
      'Consolidación',
      'Apertura de fase de ajustes',
      `${n} plan(es) pasaron a «En ajustes» para priorización por las áreas.`,
    );
  }

  /** El área reenvía su plan tras reprogramar (ajuste 12). */
  someterAjustes(planId: string): void {
    this.planes.update((list) =>
      list.map((p) =>
        p.id === planId ? { ...p, estado: 'Listo para revisión', version: p.version + 1 } : p,
      ),
    );
    const plan = this.plan(planId)!;
    this.registrar('Plan anual', 'Ajustes sometidos', `«${plan.nombre}» reprogramado y reenviado (v${plan.version}).`);
  }

  /** Requerimientos de un plan cuya decisión obliga a reprogramar (ajuste 12). */
  requerimientosAjustables(planId: string): Requerimiento[] {
    return this.requerimientos().filter(
      (r) => r.planId === planId && ['Devuelto', 'Reducido', 'Reprogramado'].includes(r.estado),
    );
  }

  // --- Requerimientos: decisiones de la consolidadora ----------------------
  private registrarDecision(
    req: Requerimiento,
    tipo: TipoDecision,
    montoAprobado: number,
    motivo: string,
    nivel: 'consolidadora' | 'autoridad',
  ): void {
    const decision: DecisionPriorizacion = {
      id: `dec-${this.seq++}`,
      requerimientoId: req.id,
      codigoRequerimiento: req.codigo,
      rubroId: this.rubroDe(req)?.id,
      tipo,
      montoSolicitado: (req.cantidad || 0) * (req.precioEstimado || 0),
      montoAprobado,
      motivo,
      decidePor: this.persona().nombre,
      areaDecideId: this.persona().areaId,
      fecha: new Date().toISOString().slice(0, 10),
      nivel,
    };
    this.decisiones.update((l) => [decision, ...l]);
  }

  /** Decisiones de priorización de un requerimiento (bitácora, ajuste 14). */
  decisionesDe(requerimientoId: string): DecisionPriorizacion[] {
    return this.decisiones().filter((d) => d.requerimientoId === requerimientoId);
  }

  decidirRequerimiento(
    id: string,
    decision: Extract<RequerimientoEstado, 'Aprobado' | 'Devuelto' | 'Reprogramado'>,
    comentario: string,
  ): void {
    this.requerimientos.update((list) =>
      list.map((r) =>
        r.id !== id
          ? r
          : {
              ...r,
              estado: decision,
              montoAprobado: decision === 'Aprobado' ? undefined : r.montoAprobado,
              comentarios: comentario
                ? [
                    ...r.comentarios,
                    {
                      autor: this.persona().nombre,
                      area: this.areaActual().corta,
                      fecha: new Date().toISOString().slice(0, 10),
                      texto: comentario,
                    },
                  ]
                : r.comentarios,
            },
      ),
    );
    const req = this.requerimientos().find((r) => r.id === id)!;
    const tipo: TipoDecision =
      decision === 'Aprobado' ? 'Priorización' : decision === 'Devuelto' ? 'Devolución' : 'Reprogramación';
    const montoAprobado = decision === 'Aprobado' ? montoDe(req) : 0;
    this.registrarDecision(req, tipo, montoAprobado, comentario || 'sin comentario', 'consolidadora');
    this.registrar('Requerimientos', decision, `${req.codigo}: ${comentario || 'sin comentario'}.`);
  }

  /**
   * Reduce un requerimiento por techo insuficiente (ajuste 14): fija el monto
   * priorizado y deja el rastro de la decisión.
   */
  reducirRequerimiento(id: string, montoAprobado: number, motivo: string): void {
    const previo = this.requerimientos().find((r) => r.id === id);
    if (!previo) return;
    this.requerimientos.update((list) =>
      list.map((r) =>
        r.id !== id
          ? r
          : {
              ...r,
              estado: 'Reducido' as const,
              montoAprobado,
              comentarios: [
                ...r.comentarios,
                {
                  autor: this.persona().nombre,
                  area: this.areaActual().corta,
                  fecha: new Date().toISOString().slice(0, 10),
                  texto: motivo,
                },
              ],
            },
      ),
    );
    this.registrarDecision(previo, 'Reducción', montoAprobado, motivo, 'consolidadora');
    this.registrar(
      'Requerimientos',
      'Reducción por techo',
      `${previo.codigo} reducido a RD$ ${montoAprobado.toLocaleString('es-DO')}. Motivo: ${motivo}.`,
    );
  }

  fusionarRequerimientos(ids: string[], descripcionConsolidada: string): Requerimiento {
    const seleccionados = this.requerimientos().filter((r) => ids.includes(r.id));
    const base = seleccionados[0];
    const total = seleccionados.reduce((s, r) => s + montoDe(r), 0);
    const consolidado: Requerimiento = {
      ...base,
      id: `req-${this.seq++}`,
      codigo: `RQ-2027-${String(this.requerimientos().length + 1).padStart(3, '0')}`,
      descripcion: descripcionConsolidada,
      cantidad: 1,
      unidadMedida: 'Lote consolidado',
      precioEstimado: total,
      montoAprobado: undefined,
      estado: 'En revisión',
      comentarios: [],
      reglaAplicada: `Consolidación manual de ${ids.length} requerimientos similares`,
    };
    this.requerimientos.update((list) => [
      consolidado,
      ...list.map((r) =>
        ids.includes(r.id) ? { ...r, estado: 'Fusionado' as const, fusionadoEnId: consolidado.id } : r,
      ),
    ]);
    this.registrar('Requerimientos', 'Fusión', `${ids.length} requerimientos fusionados en ${consolidado.codigo}.`);
    return consolidado;
  }

  // --- Reglas de ruteo ----------------------------------------------------
  agregarReglaRuteo(datos: { categoriaId: string; rubroIds: string[]; areaDestinoId: string }): void {
    const regla: ReglaRuteo = {
      id: `rr-${this.seq++}`,
      categoriaId: datos.categoriaId,
      rubroIds: datos.rubroIds,
      areaDestinoId: datos.areaDestinoId,
      version: 1,
      activa: true,
      autor: this.persona().nombre,
      fecha: new Date().toISOString().slice(0, 10),
    };
    this.reglasRuteo.update((list) => [...list, regla]);
    const cat = this.categoria(datos.categoriaId);
    this.registrar(
      'Configuración',
      'Nueva regla de ruteo',
      `Regla para «${cat?.nombre}» con ${datos.rubroIds.length} rubro(s) → ${this.area(datos.areaDestinoId)?.corta}.`,
    );
  }

  editarReglaRuteo(id: string, patch: { rubroIds?: string[]; areaDestinoId?: string; activa?: boolean }): void {
    this.reglasRuteo.update((list) =>
      list.map((r) =>
        r.id !== id
          ? r
          : {
              ...r,
              ...patch,
              version: r.version + 1,
              autor: this.persona().nombre,
              fecha: new Date().toISOString().slice(0, 10),
            },
      ),
    );
    this.registrar('Configuración', 'Edición de regla de ruteo', `Regla ${id} actualizada (nueva versión).`);
  }

  // --- Techos presupuestarios por rubro (ajuste 10) ------------------------
  /** Fija el monto inicial del techo de un rubro (sin tocar los ajustes). */
  setTechoRubro(rubroId: string, monto: number): void {
    this.techosRubro.update((list) => {
      const existe = list.some((t) => t.rubroId === rubroId);
      return existe
        ? list.map((t) => (t.rubroId === rubroId ? { ...t, montoInicial: monto } : t))
        : [...list, { rubroId, montoInicial: monto, ajustes: [], fuente: '—', clasificador: '—' }];
    });
    const rubro = this.rubrosCatalogo().find((r) => r.id === rubroId);
    this.registrar(
      'Configuración',
      'Techo por rubro',
      `Techo inicial de «${rubro?.nombre ?? rubroId}» fijado en RD$ ${monto.toLocaleString('es-DO')}.`,
    );
  }

  /** Incremento (monto > 0) o reducción (monto < 0) del techo de un rubro. */
  ajustarTechoRubro(rubroId: string, monto: number, motivo: string): void {
    const ajuste = { fecha: new Date().toISOString().slice(0, 10), motivo, monto };
    this.techosRubro.update((list) =>
      list.map((t) => (t.rubroId === rubroId ? { ...t, ajustes: [...t.ajustes, ajuste] } : t)),
    );
    const rubro = this.rubrosCatalogo().find((r) => r.id === rubroId);
    this.registrar(
      'Configuración',
      monto >= 0 ? 'Incremento de techo' : 'Reducción de techo',
      `«${rubro?.nombre ?? rubroId}»: RD$ ${Math.abs(monto).toLocaleString('es-DO')} — ${motivo}.`,
    );
  }

  // --- Catálogo de rubros y servicios --------------------------------------
  agregarFamilia(nombre: string, areaReceptoraId: string): void {
    this.familias.update((l) => [...l, { id: `fam-${this.seq++}`, nombre, areaReceptoraId }]);
    this.registrar('Configuración', 'Nueva familia de servicio', `Familia «${nombre}» agregada al catálogo.`);
  }

  agregarRubro(familiaId: string, nombre: string): void {
    const rubro: RubroCatalogo = {
      id: `rub-${this.seq++}`,
      familiaId,
      nombre,
      codigoPresupuestario: '',
      unidadMedidaSugerida: 'Unidades',
      camposRequeridos: [],
      evidencias: [],
      anticipacionDias: 30,
      fusionable: true,
      generaCompra: false,
      habilitadoInstitucional: true,
      llevaCosto: true,
      ejecutor: 'requiriente',
    };
    this.rubrosCatalogo.update((l) => [...l, rubro]);
    this.registrar('Configuración', 'Nuevo rubro', `Rubro «${nombre}» agregado al catálogo.`);
  }

  editarRubro(id: string, patch: Partial<RubroCatalogo>): void {
    this.rubrosCatalogo.update((l) => l.map((r) => (r.id === id ? { ...r, ...patch } : r)));
    const rubro = this.rubrosCatalogo().find((r) => r.id === id);
    let detalle = `Rubro «${rubro?.nombre}» actualizado.`;
    if (patch.ejecutor !== undefined) {
      detalle = `Rubro «${rubro?.nombre}»: lo ejecuta ahora el área ${patch.ejecutor === 'consolidadora' ? 'consolidadora' : 'requiriente'}.`;
    } else if (patch.habilitadoInstitucional !== undefined) {
      detalle = `Rubro «${rubro?.nombre}» ${patch.habilitadoInstitucional ? 'habilitado' : 'deshabilitado'} para las áreas.`;
    }
    this.registrar('Configuración', 'Edición de rubro', detalle);
  }

  eliminarRubro(id: string): void {
    this.rubrosCatalogo.update((l) => l.filter((r) => r.id !== id));
    this.articulos.update((l) => l.filter((a) => a.rubroId !== id));
    this.registrar('Configuración', 'Eliminación de rubro', 'Rubro eliminado del catálogo con sus artículos.');
  }

  agregarArticulo(rubroId: string, nombre: string, unidadMedida: string, precioReferencia: number): void {
    this.articulos.update((l) => [
      ...l,
      { id: `art-${this.seq++}`, rubroId, nombre, unidadMedida, precioReferencia, habilitadoInstitucional: true },
    ]);
    this.registrar('Configuración', 'Nuevo artículo', `Artículo «${nombre}» agregado al catálogo.`);
  }

  toggleArticuloHabilitado(id: string): void {
    this.articulos.update((l) =>
      l.map((a) => (a.id === id ? { ...a, habilitadoInstitucional: !a.habilitadoInstitucional } : a)),
    );
  }

  eliminarArticulo(id: string): void {
    this.articulos.update((l) => l.filter((a) => a.id !== id));
    this.registrar('Configuración', 'Eliminación de artículo', 'Artículo eliminado del catálogo.');
  }

  // --- Consolidación / aprobaciones ---------------------------------------
  aprobarFinanciero(): void {
    this.aprobacionFinanciera.set('aprobada');
    this.registrar(
      'Consolidación',
      'Validación financiera',
      'Consolidado validado por Presupuesto. Estado: listo para aprobación directiva.',
    );
  }

  /** Sello de un artefacto (POA o PACC), si ya fue aprobado. */
  selloDe(artefacto: ArtefactoAprobacion): SelloAprobacion | undefined {
    return this.sellos().find((s) => s.artefacto === artefacto);
  }

  /**
   * Aprobación institucional de un artefacto (ajuste 15). El PACC exige que el
   * POA ya esté aprobado. Registra el nivel del aprobador (MAE o ejecutivo).
   */
  aprobarArtefacto(artefacto: ArtefactoAprobacion, nivel: NivelAprobador): void {
    if (this.selloDe(artefacto)) return;
    if (artefacto === 'PACC' && !this.selloDe('POA')) return;
    const p = this.persona();
    const sello: SelloAprobacion = {
      artefacto,
      usuario: p.nombre,
      cargo: p.cargo,
      nivel,
      fecha: new Date().toISOString().slice(0, 16).replace('T', ' '),
      version: `${artefacto} 2027 — v1.0 consolidada`,
    };
    this.sellos.update((l) => [...l, sello]);
    this.registrar('Aprobación', `Aprobación del ${artefacto}`, `${artefacto} aprobado y sellado por ${p.nombre} (${nivel}).`);
  }

  // --- Configuración ------------------------------------------------------
  agregarColumna(config: Omit<ColumnaConfig, 'id' | 'origen' | 'version' | 'autor' | 'fecha'>): void {
    const col: ColumnaConfig = {
      ...config,
      id: `col-${this.seq++}`,
      origen: 'personalizada',
      version: 1,
      autor: this.persona().nombre,
      fecha: new Date().toISOString().slice(0, 10),
    };
    this.columnasConfig.update((list) => [...list, col]);
    this.registrar(
      'Configuración',
      'Nueva columna',
      `Columna «${col.nombreVisible}» agregada a la plantilla del plan anual (aplica al ciclo activo).`,
    );
  }

  toggleColumnaVisible(id: string): void {
    this.columnasConfig.update((list) =>
      list.map((c) => (c.id === id ? { ...c, visible: !c.visible, version: c.version + 1 } : c)),
    );
  }

  // --- Seguimiento --------------------------------------------------------
  reportarAvance(
    registroId: string,
    datos: { avanceFisico: number; avanceFinanciero: number; comentario: string; evidencia?: string; riesgo?: string },
  ): void {
    this.seguimiento.update((list) =>
      list.map((r) =>
        r.id !== registroId
          ? r
          : {
              ...r,
              estado: 'Reportado',
              avanceFisico: datos.avanceFisico,
              avanceFinanciero: datos.avanceFinanciero,
              comentario: datos.comentario,
              riesgo: datos.riesgo,
              evidencias: datos.evidencia ? [...r.evidencias, datos.evidencia] : r.evidencias,
            },
      ),
    );
    const reg = this.seguimiento().find((r) => r.id === registroId)!;
    this.registrar(
      'Seguimiento',
      'Reporte de avance',
      `«${reg.actividad}»: físico ${datos.avanceFisico}%, financiero ${datos.avanceFinanciero}%.`,
    );
  }

  // --- Demo ---------------------------------------------------------------
  reset(): void {
    this.peiVersiones.set(structuredClone(PEI_VERSIONES));
    this.ciclos.set(structuredClone(CICLOS));
    this.planes.set(structuredClone(PLANES));
    this.requerimientos.set(structuredClone(REQUERIMIENTOS));
    this.reglasRuteo.set(structuredClone(REGLAS_RUTEO));
    this.columnasConfig.set(structuredClone(COLUMNAS_CONFIG));
    this.seguimiento.set(structuredClone(REGISTROS_SEGUIMIENTO));
    this.actividad.set(structuredClone(ACTIVIDAD_INICIAL));
    this.techosRubro.set(structuredClone(TECHOS_RUBRO));
    this.decisiones.set(structuredClone(DECISIONES_PRIORIZACION));
    this.familias.set(structuredClone(FAMILIAS_SERVICIO));
    this.rubrosCatalogo.set(structuredClone(RUBROS_CATALOGO));
    this.articulos.set(structuredClone(ARTICULOS_SERVICIO));
    this.aprobacionFinanciera.set('pendiente');
    this.sellos.set([]);
  }
}
