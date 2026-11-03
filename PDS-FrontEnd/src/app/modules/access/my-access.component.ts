import { Component, inject, signal, computed, OnInit } from '@angular/core';
import {
  PlanningContextService,
  PlanningContext,
  PermissionCatalogEntry,
  PLANNING_PERMISSIONS,
  AreaInfo,
} from '../../services/planning/planning-context.service';
import { StructureService } from '../../services/structure/structure.service';

interface PermissionGroup {
  module: string;
  entries: PermissionCatalogEntry[];
}

export interface AreaDisplayItem {
  id: string;
  name: string;
}

/**
 * "Mi acceso": qué puede hacer el usuario en PDS y sobre qué áreas. Existe para que nadie
 * tenga que decodificar su token cuando algo no aparece, y para verificar los permisos
 * concedidos antes de activar los filtros por área. De solo lectura: los permisos se
 * administran en GOB-ID.
 */
@Component({
  selector: 'app-my-access',
  standalone: true,
  templateUrl: './my-access.component.html',
})
export class MyAccessComponent implements OnInit {
  private contextService = inject(PlanningContextService);
  private structureService = inject(StructureService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly context = signal<PlanningContext | null>(null);
  readonly catalog = signal<PermissionCatalogEntry[]>([]);
  readonly refreshing = signal(false);
  readonly areaNamesMap = signal<Map<string, string>>(new Map());

  readonly eligibleAreas = computed<AreaDisplayItem[]>(() => {
    const ctx = this.context();
    if (!ctx) return [];
    const map = this.areaNamesMap();

    return (ctx.eligibleAreaIds ?? []).map((id) => {
      const fromCtx = ctx.eligibleAreas?.find((a) => a.id === id);
      if (fromCtx?.name && fromCtx.name !== id) {
        return { id, name: fromCtx.name };
      }

      let name = map.get(id);
      if (!name) {
        if (id === ctx.unitAreaId && ctx.unitAreaName) name = ctx.unitAreaName;
        else if (id === ctx.planAreaId && ctx.planAreaName) name = ctx.planAreaName;
        else name = id;
      }
      return { id, name };
    });
  });

  readonly consolidatorAreas = computed<AreaDisplayItem[]>(() => {
    const ctx = this.context();
    if (!ctx) return [];
    const map = this.areaNamesMap();

    return (ctx.consolidatorAreaIds ?? []).map((id) => {
      const fromCtx = ctx.consolidatorAreas?.find((a) => a.id === id);
      if (fromCtx?.name && fromCtx.name !== id) {
        return { id, name: fromCtx.name };
      }

      let name = map.get(id);
      if (!name) {
        if (id === ctx.unitAreaId && ctx.unitAreaName) name = ctx.unitAreaName;
        else if (id === ctx.planAreaId && ctx.planAreaName) name = ctx.planAreaName;
        else name = id;
      }
      return { id, name };
    });
  });

  /** Agrupa por módulo (segmento intermedio de `planning:<módulo>:<acción>`). */
  readonly groups = computed<PermissionGroup[]>(() => {
    const byModule = new Map<string, PermissionCatalogEntry[]>();

    for (const entry of this.catalog()) {
      const parts = entry.code.split(':');
      const module = parts.length > 2 ? parts[1] : 'general';
      const list = byModule.get(module) ?? [];
      list.push(entry);
      byModule.set(module, list);
    }

    return [...byModule.entries()]
      .map(([module, entries]) => ({ module, entries }))
      .sort((a, b) => a.module.localeCompare(b.module));
  });

  readonly grantedCount = computed(() => this.catalog().filter((e) => e.granted).length);

  /** El caso que más confunde: con permiso de consolidar pero sin pertenecer a una consolidadora. */
  readonly consolidatesButHasNoArea = computed(() => {
    const ctx = this.context();
    if (!ctx) return false;
    return (
      ctx.permissions.includes('planning:requirements:consolidate') &&
      !ctx.permissions.includes('planning:admin') &&
      (ctx.consolidatorAreaIds?.length ?? 0) === 0
    );
  });

  /** True cuando el catálogo se armó en el cliente porque el backend no lo expone todavía. */
  readonly catalogIsLocal = signal(false);

  ngOnInit(): void {
    void this.load();
    this.loadStructures();
  }

  private loadStructures(): void {
    this.structureService.getAll().subscribe({
      next: (structures) => {
        const map = new Map<string, string>();
        const traverse = (items: any[]) => {
          for (const item of items ?? []) {
            if (item.id && item.name) {
              map.set(item.id, item.name);
            }
            if (item.dependencies?.length) {
              traverse(item.dependencies);
            }
          }
        };
        const list = (structures as any)?.items || (structures as any)?.value || (Array.isArray(structures) ? structures : []);
        traverse(list);
        this.areaNamesMap.set(map);
      },
      error: () => {},
    });
  }

  /**
   * Contexto y catálogo se cargan de forma independiente: el catálogo es un extra (dice qué
   * códigos NO tiene el usuario) y un backend sin ese endpoint no debe dejar la página en
   * blanco. Los permisos que SÍ tiene ya vienen en el contexto.
   */
  private async load(): Promise<void> {
    const [contextResult, catalogResult] = await Promise.allSettled([
      this.contextService.load(),
      this.contextService.permissionCatalog(),
    ]);

    const context = contextResult.status === 'fulfilled' ? contextResult.value : null;
    this.context.set(context);

    if (!context) {
      this.error.set(
        'No se pudo cargar su contexto de planificación. Puede que su sesión haya expirado: ' +
          'vuelva a iniciar sesión. Si el problema persiste, repórtelo indicando la hora.',
      );
      this.loading.set(false);
      return;
    }

    if (catalogResult.status === 'fulfilled') {
      this.catalog.set(catalogResult.value);
    } else {
      // Respaldo: se arma con los códigos que conoce el frontend más los que traiga el token,
      // de modo que la página sigue siendo útil aunque el backend no tenga el endpoint.
      this.catalog.set(this.buildLocalCatalog(context));
      this.catalogIsLocal.set(true);
    }

    this.loading.set(false);
  }

  private buildLocalCatalog(context: PlanningContext): PermissionCatalogEntry[] {
    const held = new Set(context.permissions ?? []);
    const isAdmin = held.has(PLANNING_PERMISSIONS.admin);
    const known = Object.values(PLANNING_PERMISSIONS) as string[];

    // Códigos del token que el frontend aún no conoce: se muestran igual, no se ocultan.
    const all = [...new Set([...known, ...held])].sort();

    return all.map((code) => ({
      code,
      granted: isAdmin || held.has(code),
      viaAdminWildcard: isAdmin && !held.has(code),
    }));
  }

  /** Reresuelve el área contra GOB-ID: útil tras una reestructuración o un cambio de cargo. */
  async refresh(): Promise<void> {
    this.refreshing.set(true);
    // refresh() no propaga errores (devuelve null), así que el null es la señal de fallo.
    const context = await this.contextService.refresh();

    if (context) {
      this.context.set(context);
      this.error.set(null);
    } else {
      this.error.set('No se pudo actualizar su contexto. Intente de nuevo en unos momentos.');
    }

    this.refreshing.set(false);
  }
}
