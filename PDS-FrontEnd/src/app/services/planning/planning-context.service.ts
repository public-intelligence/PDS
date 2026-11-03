import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface AreaInfo {
  id: string;
  name: string;
}

/**
 * Contexto de planificación del usuario autenticado (M1), resuelto por el backend:
 * claim personnel_id → GOBID Personnel (positionId) → Position de PDS → área organizacional.
 * Es la base de los filtros de acciones por área del módulo de Planificación.
 */
export interface PlanningContext {
  userId: string;
  userName: string | null;
  positionId: string | null;
  positionName: string | null;
  unitAreaId: string | null;
  unitAreaName: string | null;
  /** Área de nivel 1 (la que formula plan propio); null si no se pudo resolver. */
  planAreaId: string | null;
  planAreaName: string | null;
  /**
   * Áreas sobre las que el usuario puede actuar: la suya y sus ancestros que califican para
   * planificar. El dueño de un plan de nivel 2 aparece aquí aunque su planAreaId sea el nivel 1.
   */
  eligibleAreaIds: string[];
  eligibleAreas?: AreaInfo[];
  /** Áreas consolidadoras a las que pertenece. Vacío = no consolida, aunque tenga el permiso. */
  consolidatorAreaIds: string[];
  consolidatorAreas?: AreaInfo[];
  permissions: string[];
  isDevOverride: boolean;
}

/** Un código del catálogo y si el usuario lo tiene concedido. */
export interface PermissionCatalogEntry {
  code: string;
  granted: boolean;
  viaAdminWildcard: boolean;
}

/** Permission strings del módulo (deben coincidir con PlanningPermissions del backend). */
export const PLANNING_PERMISSIONS = {
  admin: 'planning:admin',
  cyclesManage: 'planning:cycles:manage',
  configManage: 'planning:config:manage',
  peiEdit: 'planning:pei:edit',
  peiApprove: 'planning:pei:approve',
  poaEdit: 'planning:poa:edit',
  poaReview: 'planning:poa:review',
  requirementsEdit: 'planning:requirements:edit',
  requirementsConsolidate: 'planning:requirements:consolidate',
  catalogRead: 'planning:catalog:read',
  catalogManage: 'planning:catalog:manage',
  structureManage: 'planning:structure:manage',
} as const;

@Injectable({ providedIn: 'root' })
export class PlanningContextService {
  private http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}planning-context`;

  private readonly contextSignal = signal<PlanningContext | null>(null);
  private readonly loadingSignal = signal(false);
  private loadPromise: Promise<PlanningContext | null> | null = null;

  /** Contexto actual (null mientras no se haya cargado o si falló la carga). */
  readonly context = this.contextSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();

  /** Área de plan del usuario (nivel 1) — null si no resuelta. */
  readonly planAreaId = computed(() => this.contextSignal()?.planAreaId ?? null);
  readonly planAreaName = computed(() => this.contextSignal()?.planAreaName ?? null);

  /**
   * Carga el contexto una sola vez (idempotente): llamadas concurrentes comparten la misma
   * petición. Usar al entrar al módulo de planificación.
   */
  load(): Promise<PlanningContext | null> {
    if (this.contextSignal()) return Promise.resolve(this.contextSignal());
    if (this.loadPromise) return this.loadPromise;

    this.loadPromise = this.fetch(`${this.base}/me`, 'GET');
    return this.loadPromise;
  }

  /** Invalida el contexto en el backend (caché) y lo vuelve a cargar. */
  /** Catálogo de permisos que PDS reconoce, marcando los concedidos al usuario actual. */
  permissionCatalog(): Promise<PermissionCatalogEntry[]> {
    return firstValueFrom(
      this.http.get<PermissionCatalogEntry[]>(`${this.base}/me/permission-catalog`),
    );
  }

  refresh(): Promise<PlanningContext | null> {
    this.loadPromise = this.fetch(`${this.base}/me/refresh`, 'POST');
    return this.loadPromise;
  }

  hasPermission(permission: string): boolean {
    const ctx = this.contextSignal();
    if (!ctx) return false;
    return (
      ctx.permissions.includes(PLANNING_PERMISSIONS.admin) ||
      ctx.permissions.includes(permission)
    );
  }

  private async fetch(url: string, method: 'GET' | 'POST'): Promise<PlanningContext | null> {
    this.loadingSignal.set(true);
    try {
      const request =
        method === 'GET'
          ? this.http.get<PlanningContext>(url)
          : this.http.post<PlanningContext>(url, {});
      const context = await firstValueFrom(request);
      this.contextSignal.set(context);
      return context;
    } catch {
      // Sin backend accesible (o sin sesión) el módulo sigue funcionando en modo mock.
      this.contextSignal.set(null);
      return null;
    } finally {
      this.loadingSignal.set(false);
      this.loadPromise = null;
    }
  }
}
