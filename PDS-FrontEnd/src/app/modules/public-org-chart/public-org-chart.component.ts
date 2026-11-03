import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { firstValueFrom, Observable } from 'rxjs';
import { OrgChartViewerComponent } from '../../components/ui/org-chart/org-chart-viewer.component';
import { PublicOrgChartService } from '../../services/public-org-chart/public-org-chart.service';
import { NotificationService } from '../../services/notification/notification.service';
import { OrgChart, OrgChartNode } from '../../models/org-chart.model';
import { StructureVersionSummary } from '../../models/structure-version.model';
import { versionStatusBadge, versionStatusLabel } from '../../shared/status-styles';

@Component({
  selector: 'app-public-org-chart',
  imports: [OrgChartViewerComponent],
  templateUrl: './public-org-chart.component.html',
})
export class PublicOrgChartComponent implements OnInit {
  private publicService = inject(PublicOrgChartService);
  private route = inject(ActivatedRoute);
  private notification = inject(NotificationService);

  protected isLoading = signal(true);
  /** True tras la primera carga (éxito o fallo). Distingue el spinner inicial de las recargas. */
  protected firstLoadDone = signal(false);
  protected nodes = signal<OrgChartNode[]>([]);
  protected version = signal<StructureVersionSummary | null>(null);
  protected versions = signal<StructureVersionSummary[]>([]);
  protected notFound = signal(false);
  protected orgName = signal<string | null>(null);

  protected readonly generatedAt = new Date().toLocaleDateString('es-DO', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  });

  // Helpers de presentación del estado de versión (solo visual).
  protected readonly versionStatusBadge = versionStatusBadge;
  protected readonly versionStatusLabel = versionStatusLabel;

  private organizationId: number | null = null;

  async ngOnInit(): Promise<void> {
    // Multitenant: la organización se identifica por el slug de la ruta (/public/:slug). Como
    // respaldo se admite ?organizationId=; sin ninguno, el backend usa la organización por defecto.
    const slug = this.route.snapshot.paramMap.get('slug');
    if (slug) {
      const resolved = await this.resolveOrganization(slug);
      if (!resolved) {
        this.isLoading.set(false);
        this.notFound.set(true);
        this.firstLoadDone.set(true);
        return;
      }
    } else {
      const orgParam = this.route.snapshot.queryParamMap.get('organizationId');
      this.organizationId = orgParam ? Number(orgParam) : null;
    }

    this.loadVersions();
    this.loadCurrent();
  }

  /** Resuelve el slug a una organización; devuelve false si no existe. */
  private async resolveOrganization(slug: string): Promise<boolean> {
    try {
      const org = await firstValueFrom(this.publicService.organization(slug));
      this.organizationId = org.organizationId;
      this.orgName.set(org.name);
      return true;
    } catch {
      return false;
    }
  }

  private async loadVersions(): Promise<void> {
    try {
      const list = await firstValueFrom(this.publicService.versions(this.organizationId ?? undefined));
      this.versions.set(list);
    } catch {
      // El selector de versiones es opcional; no bloquea la vista.
    }
  }

  private async loadCurrent(): Promise<void> {
    await this.run(() =>
      this.publicService.current({ includePositions: true, organizationId: this.organizationId }),
    );
  }

  protected onDateSelected(date: string | null): void {
    if (!date) {
      this.loadCurrent();
      return;
    }
    this.run(() =>
      this.publicService.asOf(date, { includePositions: true, organizationId: this.organizationId }),
    );
  }

  protected onVersionSelected(versionId: string | null): void {
    if (!versionId) {
      this.loadCurrent();
      return;
    }
    const v = this.versions().find((x) => x.id === versionId);
    if (!v || v.number == null) {
      this.loadCurrent();
      return;
    }
    this.run(() =>
      this.publicService.byNumber(v.number!, { includePositions: true, organizationId: this.organizationId }),
    );
  }

  private async run(fetch: () => Observable<OrgChart>): Promise<void> {
    this.isLoading.set(true);
    this.notFound.set(false);
    try {
      const chart = await firstValueFrom(fetch());
      this.version.set(chart.version);
      this.nodes.set(chart.nodes);
    } catch {
      this.notFound.set(true);
      this.version.set(null);
      this.nodes.set([]);
    } finally {
      this.isLoading.set(false);
      this.firstLoadDone.set(true);
    }
  }
}
