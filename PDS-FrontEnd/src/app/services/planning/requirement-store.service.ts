import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { RequirementService } from './requirement.service';
import { ConsolidationService } from './consolidation.service';
import { PlanningContextService } from './planning-context.service';
import { NotificationService } from '../notification/notification.service';
import {
  RequirementDto,
  RequirementDetailDto,
  CreateRequirementDto,
  UpdateRequirementDto,
  RequirementDecisionDto,
  ConfirmPriceDto,
  ReduceRequirementDto,
  MergeRequirementsDto,
  ConsolidationBoardDto,
  ProductDemandBoardDto,
  TaskRequirementsDto,
} from '../../modules/planning/data/planning-api.models';

@Injectable({
  providedIn: 'root',
})
export class RequirementStoreService {
  private readonly requirementService = inject(RequirementService);
  private readonly consolidationService = inject(ConsolidationService);
  private readonly planningContext = inject(PlanningContextService);
  private readonly notification = inject(NotificationService);

  readonly requerimientos = signal<RequirementDto[]>([]);
  readonly requerimientoSeleccionado = signal<RequirementDetailDto | null>(null);
  readonly tableroConsolidacion = signal<ConsolidationBoardDto | null>(null);
  readonly tableroPorArticulo = signal<ProductDemandBoardDto | null>(null);
  readonly cargando = signal<boolean>(true);
  readonly descargando = signal<boolean>(false);

  readonly totalRequerimientos = computed(() => this.requerimientos().length);

  readonly montoTotalSolicitado = computed(() =>
    this.requerimientos().reduce(
      (sum, r) => sum + (r.calculatedTotal ?? r.quantity * r.estimatedUnitPrice),
      0,
    ),
  );

  readonly montoTotalAprobado = computed(() =>
    this.requerimientos()
      .filter((r) => r.status === 'Approved' || r.status === 'Reduced')
      .reduce(
        (sum, r) =>
          sum + (r.approvedAmount ?? r.calculatedTotal ?? r.quantity * r.estimatedUnitPrice),
        0,
      ),
  );

  async cargarPorOrigen(cycleId: string, areaId: string, status?: string): Promise<void> {
    this.cargando.set(true);
    try {
      const res = await firstValueFrom(
        this.requirementService.listByOrigin(cycleId, areaId, status),
      );
      this.requerimientos.set(res.value ?? []);
    } finally {
      this.cargando.set(false);
    }
  }

  async cargarPorConsolidador(
    cycleId: string,
    consolidatorAreaId: string,
    status?: string,
  ): Promise<void> {
    this.cargando.set(true);
    try {
      const res = await firstValueFrom(
        this.requirementService.listByConsolidator(cycleId, consolidatorAreaId, status),
      );
      this.requerimientos.set(res.value ?? []);
    } finally {
      this.cargando.set(false);
    }
  }

  async cargarDetalle(id: string): Promise<RequirementDetailDto | null> {
    this.cargando.set(true);
    try {
      const detail = await firstValueFrom(this.requirementService.getById(id));
      this.requerimientoSeleccionado.set(detail);
      return detail;
    } catch {
      return null;
    } finally {
      this.cargando.set(false);
    }
  }

  async cargarPorTarea(taskId: string): Promise<RequirementDto[]> {
    const items = await firstValueFrom(this.requirementService.listByTask(taskId));
    return items ?? [];
  }

  async cargarTareasPorProducto(taskId: string): Promise<TaskRequirementsDto[]> {
    const items = await firstValueFrom(this.requirementService.getTaskRequirements(taskId));
    return items ?? [];
  }

  async crearRequerimiento(dto: CreateRequirementDto): Promise<RequirementDetailDto> {
    const created = await firstValueFrom(this.requirementService.create(dto));
    this.requerimientos.update((list) => [created, ...list]);
    return created;
  }

  async actualizarRequerimiento(
    id: string,
    dto: UpdateRequirementDto,
  ): Promise<RequirementDetailDto> {
    const updated = await firstValueFrom(this.requirementService.update(id, dto));
    this.aplicarActualizacion(id, updated);
    return updated;
  }

  async eliminarRequerimiento(id: string): Promise<void> {
    await firstValueFrom(this.requirementService.delete(id));
    this.requerimientos.update((list) => list.filter((r) => r.id !== id));
    if (this.requerimientoSeleccionado()?.id === id) {
      this.requerimientoSeleccionado.set(null);
    }
  }

  async decidir(id: string, dto: RequirementDecisionDto): Promise<RequirementDetailDto> {
    const updated = await firstValueFrom(this.requirementService.decide(id, dto));
    this.aplicarActualizacion(id, updated);
    return updated;
  }

  async confirmarPrecio(id: string, dto: ConfirmPriceDto): Promise<RequirementDetailDto> {
    const updated = await firstValueFrom(this.requirementService.confirmPrice(id, dto));
    this.aplicarActualizacion(id, updated);
    return updated;
  }

  async reducir(id: string, dto: ReduceRequirementDto): Promise<RequirementDetailDto> {
    const updated = await firstValueFrom(this.requirementService.reduce(id, dto));
    this.aplicarActualizacion(id, updated);
    return updated;
  }

  /**
   * Fusiona varios requerimientos en uno consolidado. Los originales quedan en
   * estado `Merged`, así que se recargan desde el servidor en vez de parchearse.
   */
  async fusionar(dto: MergeRequirementsDto): Promise<RequirementDetailDto> {
    const consolidado = await firstValueFrom(this.requirementService.merge(dto));
    this.requerimientos.update((list) => [
      consolidado,
      ...list.map((r) =>
        dto.requirementIds.includes(r.id)
          ? { ...r, status: 'Merged', mergedIntoId: consolidado.id }
          : r,
      ),
    ]);
    return consolidado;
  }

  private aplicarActualizacion(id: string, updated: RequirementDetailDto): void {
    this.requerimientos.update((list) => list.map((r) => (r.id === id ? updated : r)));
    if (this.requerimientoSeleccionado()?.id === id) {
      this.requerimientoSeleccionado.set(updated);
    }
  }

  async agregarComentario(id: string, texto: string, areaName?: string): Promise<void> {
    const comment = await firstValueFrom(
      this.requirementService.addComment(id, { text: texto }, areaName),
    );
    const current = this.requerimientoSeleccionado();
    if (current && current.id === id) {
      this.requerimientoSeleccionado.set({
        ...current,
        comments: [...(current.comments ?? []), comment],
      });
    }
  }

  async cargarTableroConsolidacion(cycleId: string): Promise<ConsolidationBoardDto> {
    this.cargando.set(true);
    try {
      const board = await firstValueFrom(this.consolidationService.getBoard(cycleId));
      this.tableroConsolidacion.set(board);
      return board;
    } finally {
      this.cargando.set(false);
    }
  }

  async cargarTableroPorArticulo(
    cycleId: string,
    consolidatorAreaId?: string,
  ): Promise<ProductDemandBoardDto | null> {
    this.cargando.set(true);
    try {
      const board = await firstValueFrom(
        this.consolidationService.getProductBoard(cycleId, consolidatorAreaId),
      );
      this.tableroPorArticulo.set(board);
      return board;
    } catch {
      this.tableroPorArticulo.set(null);
      return null;
    } finally {
      this.cargando.set(false);
    }
  }

  async descargarPacc(cycleId: string, nombreArchivo = 'PACC.xlsx'): Promise<void> {
    this.descargando.set(true);
    this.notification.onToast({
      message: `Generando y descargando ${nombreArchivo}…`,
      status: 'default',
      duration: 3000,
    });
    try {
      const blob = await firstValueFrom(this.consolidationService.downloadPacc(cycleId));
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = nombreArchivo;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      window.URL.revokeObjectURL(url);
      this.notification.success(`Archivo «${nombreArchivo}» descargado correctamente.`);
    } catch {
      this.notification.error('No se pudo descargar el archivo.');
    } finally {
      this.descargando.set(false);
    }
  }
}
