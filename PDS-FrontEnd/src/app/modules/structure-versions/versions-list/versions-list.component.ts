import { Component, computed, inject, OnInit, signal, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { LayoutService } from '@public-intelligence/ui-core';
import { TableComponent } from '../../../components/ui/table/table';
import { CrudPageLayout } from '../../../components/ui/layout/crud-page/crud-page.layout';
import { VersionReviewModalComponent, ReviewMode } from '../../../components/ui/modals/version-review-modal/version-review-modal.component';
import { VersionPublishModalComponent } from '../../../components/ui/modals/version-publish-modal/version-publish-modal.component';
import { ConfirmationModalComponent } from '../../../components/ui/modals/confirmation-modal/confirmation-modal.component';
import { NotificationService } from '../../../services/notification/notification.service';
import { StructureVersionService } from '../../../services/structure-version/structure-version.service';
import {
  LEGAL_INSTRUMENT_TYPE_LABELS,
  StructureVersionSummary,
  VERSION_STATUS_LABELS,
  VersionStatus,
  versionLabel,
} from '../../../models/structure-version.model';
import { Column, ColumnData } from '../../../models/table';

@Component({
  selector: 'app-versions-list',
  imports: [TableComponent, CrudPageLayout, VersionReviewModalComponent, VersionPublishModalComponent, ConfirmationModalComponent],
  templateUrl: './versions-list.component.html',
})
export class VersionsListComponent implements OnInit {
  private versionService = inject(StructureVersionService);
  private layoutService = inject(LayoutService);
  private notification = inject(NotificationService);
  private router = inject(Router);

  @ViewChild(ConfirmationModalComponent)
  private confirmationModal!: ConfirmationModalComponent;

  protected isLoading = signal(true);
  private allData = signal<ColumnData[]>([]);
  private versions = signal<StructureVersionSummary[]>([]);

  protected reviewModalOpen = signal(false);
  protected reviewMode = signal<ReviewMode>('approve');
  protected publishModalOpen = signal(false);
  protected selectedVersionId = signal('');
  protected selectedVersionName = signal('');

  protected readonly headers: Column[] = [
    { value: 'Versión', dataType: 'string', justify: 'left' },
    { value: 'Estado', dataType: 'string', justify: 'left' },
    { value: 'Vigencia', dataType: 'string', justify: 'left' },
    { value: 'Instrumento legal', dataType: 'string', justify: 'left' },
    { value: 'Autor', dataType: 'string', justify: 'left' },
  ];

  protected data = computed(() => this.allData());

  ngOnInit(): void {
    this.layoutService.setPageTitle('Versiones de la estructura');
    this.layoutService.breadcrumbs.set([{ label: 'Versiones', url: '/structure-versions' }]);
    this.load();
  }

  private async load(): Promise<void> {
    this.isLoading.set(true);
    try {
      const items = await firstValueFrom(this.versionService.list());
      this.versions.set(items);
      this.allData.set(items.map((v) => this.toColumnData(v)));
    } catch {
      this.notification.error('Error al cargar las versiones');
    } finally {
      this.isLoading.set(false);
    }
  }

  protected async createDraft(): Promise<void> {
    try {
      await firstValueFrom(this.versionService.createDraft({ name: 'Nuevo borrador' }));
      this.notification.success('Borrador creado. Edite la estructura y envíela a revisión.');
      this.router.navigate(['/structures']);
    } catch (e) {
      this.notification.fromHttpError(e, 'No se pudo crear el borrador. ¿Ya existe una versión en curso?');
    }
  }

  protected onReviewClosed = (changed: boolean): void => {
    this.reviewModalOpen.set(false);
    if (changed) this.load();
  };

  protected onPublishClosed = (changed: boolean): void => {
    this.publishModalOpen.set(false);
    if (changed) this.load();
  };

  private toColumnData(v: StructureVersionSummary): ColumnData {
    const vigencia = v.effectiveFrom
      ? `${v.effectiveFrom} → ${v.effectiveTo ?? 'vigente'}`
      : '—';
    const instrumento = v.legalInstrument
      ? `${LEGAL_INSTRUMENT_TYPE_LABELS[v.legalInstrument.type]} ${v.legalInstrument.number}`
      : '—';

    return {
      id: v.id,
      name: v.name,
      showActions: false,
      columns: [
        { value: this.labelFor(v), dataType: 'string', justify: 'left' },
        { value: VERSION_STATUS_LABELS[v.status], dataType: 'string', justify: 'left', color: this.statusColor(v.status) },
        { value: vigencia, dataType: 'string', justify: 'left' },
        { value: instrumento, dataType: 'string', justify: 'left' },
        { value: v.createdByName ?? '—', dataType: 'string', justify: 'left' },
      ],
      actionsOptions: this.actionsFor(v),
    };
  }

  private actionsFor(v: StructureVersionSummary) {
    const view = { name: 'Ver', function: () => this.router.navigate(['/structure-versions', v.id]) };
    const compare = { name: 'Comparar', function: () => this.router.navigate(['/structure-versions/compare']) };

    switch (v.status) {
      case 'Draft':
        return [
          { name: 'Editar borrador', function: () => this.router.navigate(['/structures']) },
          { name: 'Enviar a revisión', color: 'green' as const, function: () => this.submitReview(v.id) },
          view,
        ];
      case 'InReview':
        return [
          { name: 'Aprobar', color: 'green' as const, function: () => this.openReview(v, 'approve') },
          { name: 'Rechazar', color: 'red' as const, function: () => this.openReview(v, 'reject') },
          view,
        ];
      case 'Approved':
        return [
          { name: 'Publicar', color: 'green' as const, function: () => this.openPublish(v) },
          { name: 'Rechazar', color: 'red' as const, function: () => this.openReview(v, 'reject') },
          view,
        ];
      case 'Published':
      case 'Superseded':
        return [view, compare];
      default:
        return [view];
    }
  }

  private async submitReview(id: string): Promise<void> {
    try {
      await firstValueFrom(this.versionService.submitReview(id));
      this.notification.success('Versión enviada a revisión.');
      this.load();
    } catch (e) {
      this.notification.fromHttpError(e, 'No se pudo enviar a revisión.');
    }
  }

  private openReview(v: StructureVersionSummary, mode: ReviewMode): void {
    this.selectedVersionId.set(v.id);
    this.selectedVersionName.set(this.labelFor(v));
    this.reviewMode.set(mode);
    this.reviewModalOpen.set(true);
  }

  private openPublish(v: StructureVersionSummary): void {
    const current = this.versions().find((x) => x.status === 'Published');
    if (current) {
      const label = versionLabel(current);
      this.confirmationModal.openModal({
        title: 'Publicar nueva versión',
        paragraph: 'Está seguro que desea publicar la versión',
        name: this.labelFor(v),
        warning: `La versión vigente (${label}) pasará a estado «Histórica» y dejará de estar vigente al publicar esta nueva versión.`,
        confirmButton: 'Publicar',
        onConfirm: () => {
          this.confirmationModal.closeModal();
          this.openPublishForm(v);
        },
      });
      return;
    }
    this.openPublishForm(v);
  }

  private openPublishForm(v: StructureVersionSummary): void {
    this.selectedVersionId.set(v.id);
    this.selectedVersionName.set(this.labelFor(v));
    this.publishModalOpen.set(true);
  }

  /** Próximo número de versión previsto: máximo publicado + 1 (se asigna al publicar). */
  private nextNumber(): number {
    return this.versions().reduce((m, v) => Math.max(m, v.number ?? 0), 0) + 1;
  }

  /** Etiqueta derivada: "v.{n}" si está publicada; "v.{próximo} - draft" si está en curso. */
  private labelFor(v: StructureVersionSummary): string {
    return versionLabel(v, this.nextNumber());
  }

  private statusColor(status: VersionStatus): 'red' | 'green' | 'yellow' | 'default' {
    switch (status) {
      case 'Draft':
        return 'yellow';
      case 'Approved':
      case 'Published':
        return 'green';
      case 'Rejected':
        return 'red';
      default:
        return 'default';
    }
  }
}
