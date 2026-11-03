import { Component, inject, OnInit, signal, ViewChild } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { LayoutService } from '@public-intelligence/ui-core';
import { PositionService } from '../../services/position/position.service';
import { TableComponent } from '../../components/ui/table/table';
import { TableFilter } from '../../components/shared/table-filter/table-filter';
import { CrudPageLayout } from '../../components/ui/layout/crud-page/crud-page.layout';
import { PositionModalComponent } from '../../components/ui/modals/position-modal/position-modal.component';
import { ConfirmationModalComponent } from '../../components/ui/modals/confirmation-modal/confirmation-modal.component';
import { NotificationService } from '../../services/notification/notification.service';
import { Position } from '../../models/position.model';
import { Column, ColumnData } from '../../models/table';
import { Paginator } from '../../components/ui/paginator/paginator';

@Component({
  selector: 'app-position',
  imports: [
    TableComponent,
    TableFilter,
    CrudPageLayout,
    PositionModalComponent,
    ConfirmationModalComponent,
    Paginator,
  ],
  templateUrl: './position.component.html',
})
export class PositionComponent implements OnInit {
  @ViewChild(ConfirmationModalComponent)
  confirmationModal!: ConfirmationModalComponent;

  private positionService = inject(PositionService);
  private layoutService = inject(LayoutService);
  private notification = inject(NotificationService);

  protected isLoading = signal(true);
  protected isModalOpen = signal(false);
  protected selectedPosition = signal<Position | null>(null);

  protected allData = signal<ColumnData[]>([]);
  protected pageNumber = signal(1);
  protected pageSize = signal(10);
  protected totalCount = signal(0);
  protected totalPages = signal(0);
  private searchTerm = '';

  protected readonly headers: Column[] = [
    { value: 'Nombre', dataType: 'link', justify: 'left' },
    { value: 'Grupo ocupacional', dataType: 'string', justify: 'left' },
    { value: 'Estructura', dataType: 'string', justify: 'left' },
    { value: 'Es Supervisor?', dataType: 'string', justify: 'center' },
  ];

  ngOnInit(): void {
    this.layoutService.setPageTitle('Cargos');
    this.layoutService.breadcrumbs.set([{ label: 'Cargos', url: '/positions' }]);
    this.loadPositions();
  }

  private async loadPositions(): Promise<void> {
    this.isLoading.set(true);
    try {
      const result = await firstValueFrom(
        this.searchTerm
          ? this.positionService.searchPaged(this.searchTerm, this.pageNumber(), this.pageSize())
          : this.positionService.getAll(this.pageNumber(), this.pageSize()),
      );
      this.allData.set(result.items.map((item) => this.toColumnData(item)));
      this.pageNumber.set(result.pagination.currentPage);
      this.pageSize.set(result.pagination.pageSize);
      this.totalCount.set(result.pagination.total);
      this.totalPages.set(result.pagination.pageCount);
    } catch {
      this.notification.error('Error al cargar los cargos');
    } finally {
      this.isLoading.set(false);
    }
  }

  private toColumnData(item: Position): ColumnData {
    return {
      id: item.id,
      name: item.name,
      showActions: false,
      columns: [
        {
          value: item.name,
          dataType: 'link',
          justify: 'left',
          href: `/positions/details/${item.id}`,
          target: '_self',
        },
        { value: item.occupationalGroup?.name ?? '-', dataType: 'string', justify: 'left' },
        { value: item.organizationalStructureName ?? '-', dataType: 'string', justify: 'left' },
        { value: item.isManager ? 'Sí' : 'No', dataType: 'string', justify: 'center' },
      ],
      actionsOptions: [
        { name: 'Editar', function: (id) => this.onEdit(id) },
        {
          name: 'Eliminar',
          color: 'red',
          function: (id) => this.onConfirmationDelete({ name: item.name, id }),
        },
      ],
    };
  }

  protected openCreateModal(): void {
    this.selectedPosition.set(null);
    this.isModalOpen.set(true);
  }

  protected onModalClosed = (): void => {
    this.isModalOpen.set(false);
    this.loadPositions();
  };

  protected onSearch(query: string): void {
    this.searchTerm = query;
    this.pageNumber.set(1);
    void this.loadPositions();
  }

  protected onPageChange(page: number): void {
    this.pageNumber.set(page);
    void this.loadPositions();
  }

  protected onPageSizeChange(size: number): void {
    this.pageSize.set(size);
    this.pageNumber.set(1);
    void this.loadPositions();
  }

  private onEdit = async (id: string) => {
    const item = await firstValueFrom(this.positionService.getById(id));
    if (!item) return;
    this.selectedPosition.set(item);
    this.isModalOpen.set(true);
  };

  private onConfirmationDelete({ name, id }: { name: string; id: string }): void {
    this.confirmationModal.openModal({
      title: 'Eliminar cargo',
      name,
      isDelete: true,
      paragraph: 'Está seguro que desea eliminar este cargo',
      confirmButton: 'Eliminar',
      onConfirm: () => this.onDelete(id),
    });
  }

  private onDelete(id: string): void {
    this.positionService.delete(id).subscribe({
      next: () => {
        this.loadPositions();
        this.notification.success('Cargo eliminado con éxito');
      },
      error: () => {
        this.notification.error('Error al eliminar el cargo');
      },
    });
    this.confirmationModal.closeModal();
  }
}
