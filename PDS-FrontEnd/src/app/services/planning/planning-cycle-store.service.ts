import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { PlanningCycleService } from './planning-cycle.service';
import { PlanningCycleDto } from '../../modules/planning/data/planning-api.models';

@Injectable({
  providedIn: 'root',
})
export class PlanningCycleStoreService {
  private readonly cycleService = inject(PlanningCycleService);

  readonly ciclos = signal<PlanningCycleDto[]>([]);
  readonly cicloSeleccionadoId = signal<string>('');
  readonly cargando = signal<boolean>(false);

  readonly cicloActivo = computed<PlanningCycleDto | null>(() => {
    const list = this.ciclos();
    if (list.length === 0) return null;

    const selectedId = this.cicloSeleccionadoId();
    if (selectedId) {
      const found = list.find((c) => c.id === selectedId);
      if (found) return found;
    }

    // `PlanningCycleDto.status` viene del API en inglés (Draft | Published | Closed);
    // el 'Publicado' en español pertenece al adapter `CicloResumen` de poa-store.
    const published = list.find((c) => c.status === 'Published');
    return published ?? list[0] ?? null;
  });

  readonly cicloActivoId = computed<string>(() => this.cicloActivo()?.id ?? '');

  async cargarCiclos(): Promise<void> {
    if (this.cargando() || this.ciclos().length > 0) return;
    this.cargando.set(true);
    try {
      const list = await firstValueFrom(this.cycleService.list());
      this.ciclos.set(list ?? []);
    } catch {
      this.ciclos.set([]);
    } finally {
      this.cargando.set(false);
    }
  }

  seleccionarCiclo(id: string): void {
    this.cicloSeleccionadoId.set(id);
  }
}
