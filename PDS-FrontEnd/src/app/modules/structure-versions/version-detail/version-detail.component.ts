import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { LayoutService } from '@public-intelligence/ui-core';
import { OrgChartViewerComponent } from '../../../components/ui/org-chart/org-chart-viewer.component';
import { AuditLogPanelComponent } from '../../../components/ui/audit-log-panel/audit-log-panel.component';
import { VersionWorkflowStepper } from '../../../components/ui/version-workflow-stepper/version-workflow-stepper.component';
import { StructureVersionService } from '../../../services/structure-version/structure-version.service';
import { NotificationService } from '../../../services/notification/notification.service';
import {
  LEGAL_INSTRUMENT_TYPE_LABELS,
  StructureVersionDetail,
  VERSION_STATUS_LABELS,
  versionLabel,
} from '../../../models/structure-version.model';
import { versionStatusBadge } from '../../../shared/status-styles';
import { OrgChartNode } from '../../../models/org-chart.model';

@Component({
  selector: 'app-version-detail',
  imports: [OrgChartViewerComponent, AuditLogPanelComponent, VersionWorkflowStepper, RouterLink],
  templateUrl: './version-detail.component.html',
})
export class VersionDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private versionService = inject(StructureVersionService);
  private layoutService = inject(LayoutService);
  private notification = inject(NotificationService);

  protected isLoading = signal(true);
  protected version = signal<StructureVersionDetail | null>(null);
  protected nodes = signal<OrgChartNode[]>([]);

  protected readonly statusLabels = VERSION_STATUS_LABELS;
  protected readonly typeLabels = LEGAL_INSTRUMENT_TYPE_LABELS;
  protected readonly versionLabel = versionLabel;
  protected readonly badge = versionStatusBadge;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    this.layoutService.setPageTitle('Detalle de versión');
    this.layoutService.breadcrumbs.set([
      { label: 'Versiones', url: '/structure-versions' },
      { label: 'Detalle', url: '' },
    ]);
    if (id) this.load(id);
  }

  private async load(id: string): Promise<void> {
    this.isLoading.set(true);
    try {
      const [detail, chart] = await Promise.all([
        firstValueFrom(this.versionService.get(id)),
        firstValueFrom(this.versionService.orgChart(id, { includePositions: true })),
      ]);
      this.version.set(detail);
      this.nodes.set(chart.nodes);
    } catch {
      this.notification.error('No se pudo cargar la versión.');
    } finally {
      this.isLoading.set(false);
    }
  }
}
