import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { LayoutService } from '@public-intelligence/ui-core';
import { OrgChartViewerComponent } from '../../../components/ui/org-chart/org-chart-viewer.component';
import { StructureVersionService } from '../../../services/structure-version/structure-version.service';
import { NotificationService } from '../../../services/notification/notification.service';
import {
  StructureVersionSummary,
  VERSION_STATUS_LABELS,
  versionLabel,
} from '../../../models/structure-version.model';
import { versionStatusBadge } from '../../../shared/status-styles';
import { OrgChartNode } from '../../../models/org-chart.model';
import { DiffStatus, UnitChange, VersionComparison } from '../../../models/structure-diff.model';

@Component({
  selector: 'app-version-compare',
  imports: [OrgChartViewerComponent, RouterLink],
  templateUrl: './version-compare.component.html',
})
export class VersionCompareComponent implements OnInit {
  private versionService = inject(StructureVersionService);
  private layoutService = inject(LayoutService);
  private notification = inject(NotificationService);

  protected readonly versionLabel = versionLabel;
  protected readonly badge = versionStatusBadge;
  protected readonly statusLabels = VERSION_STATUS_LABELS;
  protected versions = signal<StructureVersionSummary[]>([]);
  protected fromId = signal('');
  protected toId = signal('');
  protected isLoading = signal(false);

  protected fromVersion = computed(() => this.versions().find((v) => v.id === this.fromId()) ?? null);
  protected toVersion = computed(() => this.versions().find((v) => v.id === this.toId()) ?? null);

  protected comparison = signal<VersionComparison | null>(null);
  protected nodes = signal<OrgChartNode[]>([]);
  protected diffMap = signal<Record<string, DiffStatus> | null>(null);

  protected canCompare = computed(() => !!this.fromId() && !!this.toId() && this.fromId() !== this.toId());

  protected created = computed(() => this.changesWith('Created'));
  protected deleted = computed(() => this.changesWith('Deleted'));
  protected modified = computed(() => this.changesWith('Modified'));
  protected relocated = computed(() => this.changesWith('Relocated'));

  ngOnInit(): void {
    this.layoutService.setPageTitle('Comparar versiones');
    this.layoutService.breadcrumbs.set([
      { label: 'Versiones', url: '/structure-versions' },
      { label: 'Comparar', url: '' },
    ]);
    this.loadVersions();
  }

  private async loadVersions(): Promise<void> {
    try {
      const all = await firstValueFrom(this.versionService.list());
      this.versions.set(all.filter((v) => v.number != null));
    } catch {
      this.notification.error('No se pudieron cargar las versiones.');
    }
  }

  protected onFromChange(value: string): void {
    this.fromId.set(value);
  }

  protected onToChange(value: string): void {
    this.toId.set(value);
  }

  protected async compare(): Promise<void> {
    if (!this.canCompare()) {
      this.notification.error('Seleccione dos versiones distintas.');
      return;
    }
    this.isLoading.set(true);
    try {
      const [result, chart] = await Promise.all([
        firstValueFrom(this.versionService.compare({ fromVersionId: this.fromId(), toVersionId: this.toId() })),
        firstValueFrom(this.versionService.orgChart(this.toId(), { includePositions: true })),
      ]);
      this.comparison.set(result);
      this.nodes.set(chart.nodes);
      this.diffMap.set(this.buildDiffMap(result));
    } catch {
      this.notification.error('No se pudo comparar. Ambas versiones deben estar publicadas.');
    } finally {
      this.isLoading.set(false);
    }
  }

  private buildDiffMap(result: VersionComparison): Record<string, DiffStatus> {
    const map: Record<string, DiffStatus> = {};
    for (const c of result.unitChanges) {
      map[c.sourceUnitId] = this.primaryStatus(c);
    }
    return map;
  }

  private primaryStatus(c: UnitChange): DiffStatus {
    if (c.changeType.includes('Created')) return 'created';
    if (c.changeType.includes('Deleted')) return 'deleted';
    if (c.changeType.includes('Relocated')) return 'relocated';
    return 'modified';
  }

  private changesWith(flag: string): UnitChange[] {
    return this.comparison()?.unitChanges.filter((c) => c.changeType.includes(flag)) ?? [];
  }

  protected label(c: UnitChange): string {
    return c.after?.name ?? c.before?.name ?? c.sourceUnitId;
  }
}
