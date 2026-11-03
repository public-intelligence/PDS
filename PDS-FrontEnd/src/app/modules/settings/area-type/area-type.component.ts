import { Component, computed, inject, OnInit, signal, ViewChild } from '@angular/core';
import { AreaTypeService } from '../../../services/area-type/area-type.service';
import { TableComponent } from '../../../components/ui/table/table';
import { TableFilter, FilterSelectOption } from '../../../components/shared/table-filter/table-filter';
import { CrudPageLayout } from '../../../components/ui/layout/crud-page/crud-page.layout';
import { AreaTypeModalComponent } from '../../../components/ui/modals/area-type-modal/area-type-modal.component';
import { AreaType } from '../../../models/area-type';
import { Column, ColumnData } from '../../../models/table';
import { LayoutService } from '@public-intelligence/ui-core';
import { ConfirmationModalComponent } from '../../../components/ui/modals/confirmation-modal/confirmation-modal.component';
import { NotificationService } from '../../../services/notification/notification.service';
import { firstValueFrom } from 'rxjs';
import { Paginator } from '../../../components/ui/paginator/paginator';

@Component({
  selector: 'app-area-type',
  imports: [TableComponent, TableFilter, CrudPageLayout, AreaTypeModalComponent, ConfirmationModalComponent, Paginator],
  templateUrl: './area-type.component.html',
})
export class AreaTypeComponent implements OnInit {
  @ViewChild(ConfirmationModalComponent) confirmationModal!: ConfirmationModalComponent;

  private areaTypeService = inject(AreaTypeService);
  private layoutService = inject(LayoutService);
  private notification = inject(NotificationService);

  protected isLoading = signal(true);
  protected isModalOpen = signal(false);
  protected selectedAreaType = signal<AreaType | null>(null);
  protected allData = signal<ColumnData[]>([]);
  protected selectedOrder = signal<string | number>('');
  protected pageNumber = signal(1);
  protected pageSize = signal(20);
  protected totalCount = signal(0);
  protected totalPages = signal(0);
  private searchTerm = '';

  protected readonly headers: Column[] = [
    { value: 'Nombre', dataType: 'string', justify: 'left' },
    { value: 'Orden', dataType: 'number', justify: 'center' },
  ];

  protected orderOptions = computed<FilterSelectOption[]>(() =>
    [...new Set(this.allData().map((d) => d.columns[1]?.value))]
      .filter(Boolean)
      .sort((a, b) => Number(a) - Number(b))
      .map((v) => ({ value: v, label: `Orden ${v}` })),
  );

  protected filteredData = computed(() => {
    const order = this.selectedOrder();
    return !order
      ? this.allData()
      : this.allData().filter((row) => row.columns[1]?.value === String(order));
  });

  ngOnInit(): void {
    this.layoutService.setPageTitle('Tipos de área');
    this.layoutService.breadcrumbs.set([{ label: 'Tipos de área', url: '/areatypes' }]);
    void this.loadAreaTypes();
  }

  private async loadAreaTypes(): Promise<void> {
    this.isLoading.set(true);
    try {
      const result = await firstValueFrom(
        this.searchTerm
          ? this.areaTypeService.searchPaged(this.searchTerm, this.pageNumber(), this.pageSize())
          : this.areaTypeService.getAll(this.pageNumber(), this.pageSize()),
      );
      this.allData.set(result.items.map((item) => this.toColumnData(item)));
      this.pageNumber.set(result.pagination.currentPage);
      this.pageSize.set(result.pagination.pageSize);
      this.totalCount.set(result.pagination.total);
      this.totalPages.set(result.pagination.pageCount);
    } catch {
      this.notification.error('Error al cargar los tipos de área');
    } finally {
      this.isLoading.set(false);
    }
  }

  private toColumnData(item: AreaType): ColumnData {
    return {
      id: item.id,
      name: item.name,
      showActions: false,
      columns: [
        { value: item.name, dataType: 'string', justify: 'left' },
        { value: String(item.order ?? '-'), dataType: 'string', justify: 'center' },
      ],
      actionsOptions: [
        { name: 'Editar', function: (id) => this.onEdit(id) },
        { name: 'Eliminar', color: 'red', function: (id) => this.onConfirmationDelete({ name: item.name, id }) },
      ],
    };
  }

  protected openCreateModal(): void {
    this.selectedAreaType.set(null);
    this.isModalOpen.set(true);
  }

  protected onModalSaved = (): void => {
    this.isModalOpen.set(false);
    void this.loadAreaTypes();
  };

  protected onModalClosed = (): void => {
    this.isModalOpen.set(false);
    void this.loadAreaTypes();
  };

  protected onSearch(query: string): void {
    this.searchTerm = query;
    this.pageNumber.set(1);
    this.selectedOrder.set('');
    void this.loadAreaTypes();
  }

  protected onSelectChange(value: string | number): void {
    this.selectedOrder.set(value);
  }

  protected onPageChange(page: number): void {
    this.pageNumber.set(page);
    void this.loadAreaTypes();
  }

  protected onPageSizeChange(size: number): void {
    this.pageSize.set(size);
    this.pageNumber.set(1);
    void this.loadAreaTypes();
  }

  private onEdit = async (id: string) => {
    const item = await firstValueFrom(this.areaTypeService.getById(id));
    if (!item) return;
    this.selectedAreaType.set({ id: item.id, name: item.name, order: Number(item.order) || undefined, parentId: item.parentId });
    this.isModalOpen.set(true);
  };

  private onConfirmationDelete({ name, id }: { name: string; id: string }): void {
    this.confirmationModal.openModal({
      title: 'Eliminar dependencia',
      name,
      isDelete: true,
      paragraph: 'Está seguro que desea eliminar este tipo de área',
      confirmButton: 'Eliminar',
      onConfirm: () => this.onDelete(id),
    });
  }

  private onDelete(id: string): void {
    this.areaTypeService.delete(id).subscribe({
      next: () => {
        void this.loadAreaTypes();
        this.notification.success('Tipo de área eliminado con éxito');
      },
      error: () => this.notification.error('Error al eliminar el tipo de área'),
    });
    this.confirmationModal.closeModal();
  }
}
