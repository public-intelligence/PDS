import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface PlanningAreaItemDto {
  id: string;
  code: string;
  name: string;
  parentId: string | null;
  parentName: string | null;
  depthLevel: number;
  qualifiesForPlanning: boolean;
}

export interface PlanningAreaConfigDto {
  maxLevel: number;
  description: string;
  qualifyingAreas: PlanningAreaItemDto[];
}

@Injectable({
  providedIn: 'root',
})
export class PlanningConfigService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}planning-config`;

  readonly config = signal<PlanningAreaConfigDto | null>(null);
  readonly cargando = signal<boolean>(false);
  readonly guardando = signal<boolean>(false);

  async cargarConfiguracion(): Promise<PlanningAreaConfigDto | null> {
    this.cargando.set(true);
    try {
      const res = await firstValueFrom(this.http.get<PlanningAreaConfigDto>(`${this.baseUrl}/area-guardrails`));
      this.config.set(res);
      return res;
    } catch {
      return null;
    } finally {
      this.cargando.set(false);
    }
  }

  async actualizarGuardrailNivel(maxLevel: number, customAreaIds?: string[]): Promise<PlanningAreaConfigDto | null> {
    this.guardando.set(true);
    try {
      const payload: { maxLevel: number; customAreaIds?: string[] } = { maxLevel };
      if (maxLevel === -1 && customAreaIds) {
        payload.customAreaIds = customAreaIds;
      }
      const res = await firstValueFrom(
        this.http.put<PlanningAreaConfigDto>(`${this.baseUrl}/area-guardrails`, payload)
      );
      this.config.set(res);
      return res;
    } catch {
      return null;
    } finally {
      this.guardando.set(false);
    }
  }
}
