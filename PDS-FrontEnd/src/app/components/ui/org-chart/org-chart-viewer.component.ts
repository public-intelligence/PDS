import { Component, computed, effect, ElementRef, inject, input, output, signal, untracked, viewChild } from '@angular/core';
import { stratify, tree, HierarchyPointNode } from 'd3-hierarchy';
import { OrgChartNode, OrgChartPosition } from '../../../models/org-chart.model';
import { StructureVersionSummary } from '../../../models/structure-version.model';
import { DiffStatus } from '../../../models/structure-diff.model';
import { OrgChartExportService } from '../../../services/org-chart/org-chart-export.service';
import { NotificationService } from '../../../services/notification/notification.service';

interface Row {
  id: string;
  parentId: string | null;
  data: OrgChartNode | null;
}

interface LaidOutNode {
  node: OrgChartNode;
  x: number;
  y: number;
}

interface LaidOutLink {
  x1: number;
  y1: number;
  x2: number;
  y2: number;
  /** 'bezier' (clásico, por defecto) o 'elbow' (conector ortogonal de columna apilada). */
  kind?: 'bezier' | 'elbow';
}

/** Nodo interno del layout compacto: árbol mutable con medidas (bottom-up) y posición (top-down). */
interface LNode {
  id: string;
  data: OrgChartNode;
  children: LNode[];
  // Medidas (pasada bottom-up):
  w: number;
  h: number;
  maxFanoutBelow: number;
  stacked: boolean;
  cols: number;
  // Posición (pasada top-down): centro absoluto de la caja.
  cx: number;
  cy: number;
}

interface Layout {
  nodes: LaidOutNode[];
  links: LaidOutLink[];
  viewBox: string;
  /** Tamaño intrínseco del contenido en px (== ancho/alto del viewBox, incluye el padding). */
  width: number;
  height: number;
}

const SYNTHETIC_ROOT = '__root__';
const BOX_W = 210;
const GAP_X = 28;
const GAP_Y = 64;
const MAX_POS_LINES = 4;

// ── Layout compacto (híbrido) ──
const V_PITCH_GAP = 22; // separación vertical entre hermanos apilados
const STACK_COL_GAP = 20; // separación horizontal entre columnas de una pila envuelta
const STACK_SPINE_INSET = 16; // x de la espina = centro padre − BOX_W/2 + esto (inferior-izquierda; libra el toggle)
const STACK_DROP = GAP_Y / 2; // caída desde el borde inferior del padre hasta la primera fila apilada
const STACK_MAX_FANOUT = 1; // un nodo apila a sus hijos solo si todo el subárbol son cadenas (ramificación ≤ esto)
const STACK_MAX_ROWS = 12; // filas por columna antes de envolver en otra columna
const WIDE_THRESHOLD_PX = 2600; // en modo 'auto', usar compacto cuando el árbol clásico supere este ancho

const ZOOM_MIN = 0.02;
const ZOOM_MAX = 2;
const FIT_PADDING = 0.95;

/**
 * Visor de organigrama reutilizable. Recibe una lista plana de nodos y la dibuja como un SVG
 * nativo (layout con d3-hierarchy). Controles: unidad raíz, profundidad, mostrar cargos, fecha/versión
 * histórica, zoom y exportación PDF/PNG/SVG. Soporta resaltado de diferencias (modo comparación).
 */
@Component({
  selector: 'app-org-chart-viewer',
  templateUrl: './org-chart-viewer.component.html',
})
export class OrgChartViewerComponent {
  private exportService = inject(OrgChartExportService);
  private notification = inject(NotificationService);

  readonly nodes = input<OrgChartNode[]>([]);
  readonly versions = input<StructureVersionSummary[]>([]);
  readonly currentVersion = input<StructureVersionSummary | null>(null);
  /** sourceUnitId → estado de cambio, para colorear en modo comparación. */
  readonly diffMap = input<Record<string, DiffStatus> | null>(null);
  readonly showHistoryControls = input(true);
  readonly title = input('Organigrama');
  /** Muestra una capa de carga sobre el lienzo sin destruir el visor (preserva zoom/pan/selección). */
  readonly loading = input(false);

  readonly dateSelected = output<string | null>();
  readonly versionSelected = output<string | null>();

  private svgRef = viewChild<ElementRef<SVGSVGElement>>('svg');
  private canvasRef = viewChild<ElementRef<HTMLElement>>('canvas');

  /** Encuadre inicial (colapsar a niveles superiores + ajustar) hecho una sola vez por carga de datos. */
  private initialized = false;

  /** Prefijo de id único por instancia para asociar <label for>/aria sin colisiones. */
  private static seq = 0;
  protected readonly ids = (() => {
    const p = `ocv-${++OrgChartViewerComponent.seq}`;
    return { root: `${p}-root`, level: `${p}-level`, date: `${p}-date`, version: `${p}-version` };
  })();

  protected rootUnitId = signal('');
  protected maxLevelSel = signal('');
  protected showPositions = signal(false);
  /** Modo de disposición: 'auto' decide por ancho; 'tree' clásico arriba-abajo; 'compact' híbrido vertical. */
  protected layoutMode = signal<'auto' | 'tree' | 'compact'>('auto');
  protected collapsed = signal<Set<string>>(new Set());
  protected zoom = signal(1);
  protected panX = signal(0);
  protected panY = signal(0);
  protected selectedDate = signal('');
  protected selectedVersionId = signal('');

  private isPanning = false;
  private panStart = { x: 0, y: 0, px: 0, py: 0 };

  constructor() {
    // Encuadre inicial: en cuanto hay datos, colapsa a los niveles superiores (vista legible) y ajusta
    // a la pantalla. Solo la primera vez por carga; recargas (fecha/versión) preservan zoom/pan/selección.
    effect(() => {
      const scoped = this.scopedNodes();
      if (!scoped.length || this.initialized) return;
      untracked(() => {
        this.collapsed.set(this.defaultCollapsedSet(scoped));
        this.initialized = true;
        this.scheduleFit();
      });
    });
  }

  protected boxH = computed(() => (this.showPositions() ? 150 : 58));

  protected zoomPercent = computed(() => Math.round(this.zoom() * 100));

  protected rootOptions = computed(() =>
    [...this.nodes()]
      .sort((a, b) => a.code.localeCompare(b.code))
      .map((n) => ({ value: n.sourceUnitId, label: `${n.code} · ${n.name}` })),
  );

  protected levelOptions = computed(() => {
    const max = this.nodes().reduce((m, n) => Math.max(m, n.level), 0);
    return Array.from({ length: max }, (_, i) => i + 1);
  });

  /** Aplica filtro de unidad raíz (subárbol, nivel rebasado) y de profundidad máxima. */
  private scopedNodes = computed<OrgChartNode[]>(() => {
    let list = this.nodes();
    const root = this.rootUnitId();

    if (root) {
      const childrenByParent = new Map<string, OrgChartNode[]>();
      for (const n of list) {
        if (!n.parentSourceUnitId) continue;
        const arr = childrenByParent.get(n.parentSourceUnitId) ?? [];
        arr.push(n);
        childrenByParent.set(n.parentSourceUnitId, arr);
      }
      const rootNode = list.find((n) => n.sourceUnitId === root);
      if (!rootNode) return [];
      const baseLevel = rootNode.level;
      const collected: OrgChartNode[] = [];
      const visited = new Set<string>();
      const queue = [rootNode];
      while (queue.length) {
        const node = queue.shift()!;
        if (visited.has(node.sourceUnitId)) continue;
        visited.add(node.sourceUnitId);
        collected.push({
          ...node,
          level: node.level - baseLevel + 1,
          parentSourceUnitId: node.sourceUnitId === root ? null : node.parentSourceUnitId,
        });
        for (const child of childrenByParent.get(node.sourceUnitId) ?? []) queue.push(child);
      }
      list = collected;
    }

    const max = this.maxLevelSel() ? Number(this.maxLevelSel()) : null;
    if (max) list = list.filter((n) => n.level <= max);

    const ids = new Set(list.map((n) => n.sourceUnitId));
    return list.map((n) =>
      n.parentSourceUnitId && !ids.has(n.parentSourceUnitId)
        ? { ...n, parentSourceUnitId: null }
        : n,
    );
  });

  protected childCount = computed(() => {
    const counts = new Map<string, number>();
    for (const n of this.scopedNodes()) {
      if (n.parentSourceUnitId) counts.set(n.parentSourceUnitId, (counts.get(n.parentSourceUnitId) ?? 0) + 1);
    }
    return counts;
  });

  private visibleNodes = computed<OrgChartNode[]>(() => {
    const scoped = this.scopedNodes();
    const collapsed = this.collapsed();
    if (!collapsed.size) return scoped;

    const byId = new Map(scoped.map((n) => [n.sourceUnitId, n]));
    const isHidden = (n: OrgChartNode): boolean => {
      let parentId = n.parentSourceUnitId ?? null;
      const guard = new Set<string>();
      while (parentId && !guard.has(parentId)) {
        guard.add(parentId);
        if (collapsed.has(parentId)) return true;
        parentId = byId.get(parentId)?.parentSourceUnitId ?? null;
      }
      return false;
    };
    return scoped.filter((n) => !isHidden(n));
  });

  /** Layout clásico (árbol arriba-abajo) de los nodos visibles. Derivado, sin efectos. */
  private treeLayout = computed<Layout>(() => this.buildTreeLayout(this.visibleNodes(), this.boxH()));

  /** Modo efectivo: 'auto' se resuelve midiendo el ancho real del árbol clásico. */
  protected effectiveMode = computed<'tree' | 'compact'>(() => {
    const m = this.layoutMode();
    if (m !== 'auto') return m; // override manual: corta sin parpadeo ni "pelea" con auto
    return this.treeLayout().width > WIDE_THRESHOLD_PX ? 'compact' : 'tree';
  });

  protected layout = computed<Layout>(() =>
    this.effectiveMode() === 'compact'
      ? this.buildCompactLayout(this.visibleNodes(), this.boxH())
      : this.treeLayout(),
  );

  protected svgTransform = computed(
    () => `translate(${this.panX()}px, ${this.panY()}px) scale(${this.zoom()})`,
  );

  // ───────────────────────── interacción ─────────────────────────

  protected hasChildren(id: string): boolean {
    return (this.childCount().get(id) ?? 0) > 0;
  }

  protected isCollapsed(id: string): boolean {
    return this.collapsed().has(id);
  }

  protected toggleCollapse(id: string): void {
    const next = new Set(this.collapsed());
    next.has(id) ? next.delete(id) : next.add(id);
    this.collapsed.set(next);
  }

  protected expandAll(): void {
    this.collapsed.set(new Set());
  }

  protected collapseAll(): void {
    // Deja visibles el nivel superior y sus hijos directos; colapsa el resto (vista legible).
    this.collapsed.set(this.defaultCollapsedSet(this.scopedNodes()));
  }

  /** Conjunto colapsado por defecto: muestra el nivel superior y sus hijos directos, colapsa lo demás. */
  private defaultCollapsedSet(scoped: OrgChartNode[]): Set<string> {
    if (!scoped.length) return new Set();
    const minLevel = scoped.reduce((m, n) => Math.min(m, n.level), Infinity);
    const hasKids = new Set<string>();
    for (const n of scoped) if (n.parentSourceUnitId) hasKids.add(n.parentSourceUnitId);
    return new Set(
      scoped
        .filter((n) => hasKids.has(n.sourceUnitId) && n.level >= minLevel + 1)
        .map((n) => n.sourceUnitId),
    );
  }

  protected onRootChange(value: string): void {
    this.rootUnitId.set(value);
    // Reencuadra deliberadamente al nuevo subárbol: vista compacta + ajuste a pantalla.
    this.collapsed.set(this.defaultCollapsedSet(this.scopedNodes()));
    this.scheduleFit();
  }

  protected onMaxLevelChange(value: string): void {
    this.maxLevelSel.set(value);
  }

  protected onTogglePositions(checked: boolean): void {
    this.showPositions.set(checked);
  }

  protected setLayoutMode(mode: 'auto' | 'tree' | 'compact'): void {
    this.layoutMode.set(mode);
    // El ancho/alto cambian en un orden de magnitud al alternar; reencuadra.
    this.scheduleFit();
  }

  protected onDateChange(value: string): void {
    this.selectedDate.set(value);
    this.selectedVersionId.set('');
    this.dateSelected.emit(value || null);
  }

  protected onVersionChange(value: string): void {
    this.selectedVersionId.set(value);
    this.selectedDate.set('');
    this.versionSelected.emit(value || null);
  }

  protected zoomIn(): void {
    const c = this.canvasRef()?.nativeElement;
    if (!c) {
      this.zoom.set(this.clampZoom(this.zoom() * 1.2));
      return;
    }
    this.zoomAt(this.zoom() * 1.2, c.clientWidth / 2, c.clientHeight / 2);
  }

  protected zoomOut(): void {
    const c = this.canvasRef()?.nativeElement;
    if (!c) {
      this.zoom.set(this.clampZoom(this.zoom() / 1.2));
      return;
    }
    this.zoomAt(this.zoom() / 1.2, c.clientWidth / 2, c.clientHeight / 2);
  }

  protected onWheel(event: WheelEvent): void {
    event.preventDefault();
    const c = this.canvasRef()?.nativeElement;
    if (!c) return;
    const rect = c.getBoundingClientRect();
    const ax = event.clientX - rect.left - c.clientLeft;
    const ay = event.clientY - rect.top - c.clientTop;
    const factor = event.deltaY < 0 ? 1.1 : 1 / 1.1;
    this.zoomAt(this.zoom() * factor, ax, ay);
  }

  /** Aplica un nuevo zoom manteniendo fijo el punto del contenido bajo (ax, ay) del lienzo. */
  private zoomAt(newZoom: number, ax: number, ay: number): void {
    const zOld = this.zoom();
    const zNew = this.clampZoom(newZoom);
    if (zNew === zOld) return;
    const ratio = zNew / zOld;
    this.panX.set(ax - ratio * (ax - this.panX()));
    this.panY.set(ay - ratio * (ay - this.panY()));
    this.zoom.set(zNew);
  }

  private clampZoom(z: number): number {
    return Math.min(Math.max(z, ZOOM_MIN), ZOOM_MAX);
  }

  /** Encuadra todo el organigrama (tamaño intrínseco) dentro del lienzo, centrado. */
  protected fitToView(): void {
    const canvas = this.canvasRef()?.nativeElement;
    const l = this.layout();
    if (!canvas || !l.nodes.length) return;
    const cw = canvas.clientWidth;
    const ch = canvas.clientHeight;
    if (cw === 0 || ch === 0) return;
    const z = this.clampZoom(Math.min(cw / l.width, ch / l.height) * FIT_PADDING);
    this.zoom.set(z);
    this.panX.set((cw - z * l.width) / 2);
    this.panY.set((ch - z * l.height) / 2);
  }

  /** Programa un ajuste tras el render (el lienzo y el layout colapsado ya tienen tamaño). */
  private scheduleFit(attempt = 0): void {
    if (typeof requestAnimationFrame === 'undefined') return;
    requestAnimationFrame(() => {
      const canvas = this.canvasRef()?.nativeElement;
      if (canvas && (canvas.clientWidth === 0 || canvas.clientHeight === 0)) {
        if (attempt < 30) this.scheduleFit(attempt + 1);
        return;
      }
      this.fitToView();
    });
  }

  protected onPointerDown(event: PointerEvent): void {
    this.isPanning = true;
    this.panStart = { x: event.clientX, y: event.clientY, px: this.panX(), py: this.panY() };
    (event.target as Element).setPointerCapture?.(event.pointerId);
  }

  protected onPointerMove(event: PointerEvent): void {
    if (!this.isPanning) return;
    this.panX.set(this.panStart.px + (event.clientX - this.panStart.x));
    this.panY.set(this.panStart.py + (event.clientY - this.panStart.y));
  }

  protected onPointerUp(): void {
    this.isPanning = false;
  }

  // ───────────────────────── render helpers ─────────────────────────

  protected linkPath(l: LaidOutLink): string {
    if (l.kind === 'elbow') {
      // Baja por la espina y luego stub horizontal hacia el borde izquierdo del hijo (esquina en x1,y2).
      return `M ${l.x1},${l.y1} V ${l.y2} H ${l.x2}`;
    }
    const midY = (l.y1 + l.y2) / 2;
    return `M ${l.x1},${l.y1} C ${l.x1},${midY} ${l.x2},${midY} ${l.x2},${l.y2}`;
  }

  protected nodeFill(node: OrgChartNode): string {
    const diff = this.diffMap();
    if (diff) {
      switch (diff[node.sourceUnitId]) {
        case 'created':
          return '#e9f9f0';
        case 'modified':
          return '#fdf8e7';
        case 'relocated':
          return '#e6f3ff';
        case 'deleted':
          return '#fdecea';
      }
    }
    return '#ffffff';
  }

  protected nodeStroke(node: OrgChartNode): string {
    const diff = this.diffMap();
    if (diff) {
      switch (diff[node.sourceUnitId]) {
        case 'created':
          return '#2ecc71';
        case 'modified':
          return '#ebc342';
        case 'relocated':
          return '#0087ff';
        case 'deleted':
          return '#ee2a24';
      }
    }
    return '#cbd5e1';
  }

  /** Barra de acento de la tarjeta: en comparación sigue el color del diff; si no, por nivel. */
  private static readonly LEVEL_ACCENTS = ['#003876', '#1a579a', '#2e8bc0', '#5fb0d9', '#88c4e3', '#b3d4ea'];

  protected accentColor(node: OrgChartNode): string {
    if (this.diffMap()) return this.nodeStroke(node);
    const palette = OrgChartViewerComponent.LEVEL_ACCENTS;
    return palette[Math.max(0, node.level - 1) % palette.length];
  }

  protected visiblePositions(node: OrgChartNode): OrgChartPosition[] {
    return [...node.positions]
      .sort((a, b) => Number(b.isSupervisor) - Number(a.isSupervisor))
      .slice(0, MAX_POS_LINES);
  }

  protected extraPositions(node: OrgChartNode): number {
    return Math.max(0, node.positions.length - MAX_POS_LINES);
  }

  protected truncate(text: string, max = 28): string {
    return text.length > max ? `${text.slice(0, max - 1)}…` : text;
  }

  // ───────────────────────── export ─────────────────────────

  protected exportSvg(): void {
    this.withSvg((svg) => this.exportService.exportSvg(svg, this.fileName('svg')));
  }

  protected async exportPng(): Promise<void> {
    await this.withSvgAsync((svg) => this.exportService.exportPng(svg, this.fileName('png')));
  }

  protected async exportPdf(): Promise<void> {
    await this.withSvgAsync((svg) => this.exportService.exportPdf(svg, this.fileName('pdf'), this.title()));
  }

  private fileName(ext: string): string {
    const v = this.currentVersion();
    const suffix = v?.number != null ? `-v${v.number}` : '';
    return `organigrama${suffix}.${ext}`;
  }

  private withSvg(action: (svg: SVGSVGElement) => void): void {
    const svg = this.svgRef()?.nativeElement;
    if (!svg || !this.layout().nodes.length) {
      this.notification.error('No hay organigrama para exportar.');
      return;
    }
    action(svg);
  }

  private async withSvgAsync(action: (svg: SVGSVGElement) => Promise<void>): Promise<void> {
    const svg = this.svgRef()?.nativeElement;
    if (!svg || !this.layout().nodes.length) {
      this.notification.error('No hay organigrama para exportar.');
      return;
    }
    try {
      await action(svg);
    } catch {
      this.notification.error('No se pudo exportar el organigrama.');
    }
  }

  // ───────────────────────── layout ─────────────────────────

  /** Layout clásico arriba-abajo (d3-hierarchy). Sin cambios respecto a la versión previa. */
  private buildTreeLayout(visible: OrgChartNode[], boxH: number): Layout {
    const empty: Layout = { nodes: [], links: [], viewBox: '0 0 100 100', width: 100, height: 100 };
    if (!visible.length) return empty;

    const ids = new Set(visible.map((n) => n.sourceUnitId));
    const rows: Row[] = visible.map((n) => ({
      id: n.sourceUnitId,
      parentId: n.parentSourceUnitId && ids.has(n.parentSourceUnitId) ? n.parentSourceUnitId : null,
      data: n,
    }));

    const roots = rows.filter((r) => r.parentId == null);
    let working = rows;
    if (roots.length !== 1) {
      working = [
        { id: SYNTHETIC_ROOT, parentId: null, data: null },
        ...rows.map((r) => (r.parentId == null ? { ...r, parentId: SYNTHETIC_ROOT } : r)),
      ];
    }

    let root;
    try {
      root = stratify<Row>()
        .id((d) => d.id)
        .parentId((d) => d.parentId ?? undefined)(working);
    } catch (e) {
      console.warn('No se pudo construir el árbol del organigrama', e);
      return empty;
    }

    tree<Row>().nodeSize([BOX_W + GAP_X, boxH + GAP_Y])(root);

    const points = root.descendants() as HierarchyPointNode<Row>[];
    const laidOut: LaidOutNode[] = [];
    const links: LaidOutLink[] = [];
    let minX = Infinity;
    let minY = Infinity;
    let maxX = -Infinity;
    let maxY = -Infinity;

    for (const p of points) {
      if (!p.data.data) continue;
      laidOut.push({ node: p.data.data, x: p.x, y: p.y });
      minX = Math.min(minX, p.x - BOX_W / 2);
      maxX = Math.max(maxX, p.x + BOX_W / 2);
      minY = Math.min(minY, p.y - boxH / 2);
      maxY = Math.max(maxY, p.y + boxH / 2);

      if (p.parent && p.parent.data.data) {
        links.push({ x1: p.parent.x, y1: p.parent.y + boxH / 2, x2: p.x, y2: p.y - boxH / 2 });
      }
    }

    const pad = 48;
    const width = maxX - minX + pad * 2;
    const height = maxY - minY + pad * 2;
    return { nodes: laidOut, links, viewBox: `${minX - pad} ${minY - pad} ${width} ${height}`, width, height };
  }

  /**
   * Layout híbrido compacto (estilo organigrama oficial): los niveles ramificados se disponen
   * arriba-abajo/horizontal; las "colas" finas (cadenas/hojas) se apilan en columna vertical bajo el
   * padre, envolviendo en varias columnas si hay muchos hermanos. Conserva el contrato de `Layout`
   * (x,y = centro de la caja). Conectores: 'bezier' para horizontal, 'elbow' (ortogonal) para apilado.
   */
  private buildCompactLayout(visible: OrgChartNode[], boxH: number): Layout {
    const empty: Layout = { nodes: [], links: [], viewBox: '0 0 100 100', width: 100, height: 100 };
    if (!visible.length) return empty;

    // 1) Árbol interno (preserva el orden de entrada para un orden de hermanos estable).
    const ids = new Set(visible.map((n) => n.sourceUnitId));
    const byId = new Map<string, LNode>();
    for (const n of visible) {
      byId.set(n.sourceUnitId, {
        id: n.sourceUnitId,
        data: n,
        children: [],
        w: 0,
        h: 0,
        maxFanoutBelow: 0,
        stacked: false,
        cols: 1,
        cx: 0,
        cy: 0,
      });
    }
    const roots: LNode[] = [];
    for (const n of visible) {
      const self = byId.get(n.sourceUnitId)!;
      const pid = n.parentSourceUnitId && ids.has(n.parentSourceUnitId) ? n.parentSourceUnitId : null;
      const parent = pid ? byId.get(pid) : undefined;
      if (parent) parent.children.push(self);
      else roots.push(self);
    }

    // 2) Medir (bottom-up): ramificación máxima, modo apilado/columnas y caja del subárbol.
    const measure = (nd: LNode): void => {
      if (nd.children.length === 0) {
        nd.w = BOX_W;
        nd.h = boxH;
        nd.maxFanoutBelow = 0;
        nd.stacked = false;
        return;
      }
      for (const c of nd.children) measure(c);
      nd.maxFanoutBelow = Math.max(...nd.children.map((c) => Math.max(c.children.length, c.maxFanoutBelow)));
      nd.stacked = nd.maxFanoutBelow <= STACK_MAX_FANOUT;

      if (nd.stacked) {
        const k = nd.children.length;
        nd.cols = Math.max(1, Math.ceil(k / STACK_MAX_ROWS));
        const rowsPerCol = Math.ceil(k / nd.cols);
        let maxColH = 0;
        let colW = 0;
        for (let col = 0; col < nd.cols; col++) {
          const slice = nd.children.slice(col * rowsPerCol, (col + 1) * rowsPerCol);
          if (!slice.length) continue;
          const colH = slice.reduce((s, c) => s + c.h, 0) + V_PITCH_GAP * (slice.length - 1);
          maxColH = Math.max(maxColH, colH);
          colW = Math.max(colW, ...slice.map((c) => c.w));
        }
        const columnsW = colW * nd.cols + STACK_COL_GAP * (nd.cols - 1);
        nd.w = Math.max(BOX_W, STACK_SPINE_INSET + columnsW);
        nd.h = boxH + STACK_DROP + maxColH;
      } else {
        nd.cols = 1;
        const childrenW = nd.children.reduce((s, c) => s + c.w, 0) + GAP_X * (nd.children.length - 1);
        const childrenH = Math.max(...nd.children.map((c) => c.h));
        nd.w = Math.max(BOX_W, childrenW);
        nd.h = boxH + GAP_Y + childrenH;
      }
    };
    roots.forEach(measure);

    const nodes: LaidOutNode[] = [];
    const links: LaidOutLink[] = [];

    // 3) Colocar (top-down): centros absolutos; emite nodos y conectores.
    const place = (nd: LNode, originX: number, originY: number): void => {
      if (nd.children.length === 0) {
        nd.cx = originX + nd.w / 2;
        nd.cy = originY + boxH / 2;
        nodes.push({ node: nd.data, x: nd.cx, y: nd.cy });
        return;
      }

      if (nd.stacked) {
        // Caja del padre en el extremo izquierdo de la banda, fila superior.
        nd.cx = originX + BOX_W / 2;
        nd.cy = originY + boxH / 2;
        nodes.push({ node: nd.data, x: nd.cx, y: nd.cy });

        // La espina cae desde la zona inferior-izquierda (libra el círculo de colapsar en el centro).
        const spineX = nd.cx - BOX_W / 2 + STACK_SPINE_INSET;
        const k = nd.children.length;
        const rowsPerCol = Math.ceil(k / nd.cols);
        const childTop = originY + boxH + STACK_DROP;
        let colWidth = 0;
        for (const c of nd.children) colWidth = Math.max(colWidth, c.w);

        for (let col = 0; col < nd.cols; col++) {
          const slice = nd.children.slice(col * rowsPerCol, (col + 1) * rowsPerCol);
          const colX = originX + STACK_SPINE_INSET + col * (colWidth + STACK_COL_GAP);
          let cursorY = childTop;
          for (const c of slice) {
            place(c, colX, cursorY);
            // Codo: baja por la espina hasta el centro del hijo, luego horizontal a su borde izquierdo.
            links.push({ x1: spineX, y1: nd.cy + boxH / 2, x2: c.cx - BOX_W / 2, y2: c.cy, kind: 'elbow' });
            cursorY += c.h + V_PITCH_GAP;
          }
        }
      } else {
        const childTop = originY + boxH + GAP_Y;
        const rowW = nd.children.reduce((s, c) => s + c.w, 0) + GAP_X * (nd.children.length - 1);
        let cursorX = originX + Math.max(0, (nd.w - rowW) / 2);
        for (const c of nd.children) {
          place(c, cursorX, childTop);
          cursorX += c.w + GAP_X;
        }
        const first = nd.children[0];
        const last = nd.children[nd.children.length - 1];
        nd.cx = (first.cx + last.cx) / 2;
        nd.cy = originY + boxH / 2;
        nodes.push({ node: nd.data, x: nd.cx, y: nd.cy });
        for (const c of nd.children) {
          links.push({ x1: nd.cx, y1: nd.cy + boxH / 2, x2: c.cx, y2: c.cy - boxH / 2, kind: 'bezier' });
        }
      }
    };

    // Múltiples raíces: cada raíz real de izquierda a derecha (sin nodo sintético en la salida).
    let cursorX = 0;
    for (const r of roots) {
      place(r, cursorX, 0);
      cursorX += r.w + GAP_X;
    }

    // 4) Caja envolvente: incluye cajas de nodos (+9 del círculo de colapsar) y extremos de conectores.
    let minX = Infinity;
    let minY = Infinity;
    let maxX = -Infinity;
    let maxY = -Infinity;
    for (const n of nodes) {
      minX = Math.min(minX, n.x - BOX_W / 2);
      maxX = Math.max(maxX, n.x + BOX_W / 2);
      minY = Math.min(minY, n.y - boxH / 2);
      maxY = Math.max(maxY, n.y + boxH / 2 + 9);
    }
    for (const l of links) {
      minX = Math.min(minX, l.x1, l.x2);
      maxX = Math.max(maxX, l.x1, l.x2);
      minY = Math.min(minY, l.y1, l.y2);
      maxY = Math.max(maxY, l.y1, l.y2);
    }

    const pad = 48;
    const width = maxX - minX + pad * 2;
    const height = maxY - minY + pad * 2;
    return { nodes, links, viewBox: `${minX - pad} ${minY - pad} ${width} ${height}`, width, height };
  }

  protected readonly boxWidth = BOX_W;
}
