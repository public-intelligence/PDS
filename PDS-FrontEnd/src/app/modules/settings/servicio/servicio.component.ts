import { Component, OnInit, inject, signal, ViewChild } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { LayoutService } from '@public-intelligence/ui-core';
import { TableComponent } from '../../../components/ui/table/table';
import { TableFilter } from '../../../components/shared/table-filter/table-filter';
import { CrudPageLayout } from '../../../components/ui/layout/crud-page/crud-page.layout';
import { ConfirmationModalComponent } from '../../../components/ui/modals/confirmation-modal/confirmation-modal.component';
import { ServicioModalComponent } from '../../../components/ui/modals/servicio-modal/servicio-modal.component';
import { Column, ColumnData } from '../../../models/table';
import { NotificationService } from '../../../services/notification/notification.service';
import { ServicioService } from '../../../services/locality-catalog/locality-catalog.services';
import { Servicio } from '../../../models/locality-catalog.model';
import { Paginator } from '../../../components/ui/paginator/paginator';

@Component({
  selector: 'app-servicio',
  imports: [TableComponent, TableFilter, CrudPageLayout, ServicioModalComponent, ConfirmationModalComponent, Paginator],
  templateUrl: './servicio.component.html',
})
export class ServicioComponent implements OnInit {
  @ViewChild(ConfirmationModalComponent) confirmationModal!: ConfirmationModalComponent;
  private service = inject(ServicioService);
  private layoutService = inject(LayoutService);
  private notification = inject(NotificationService);

  protected isLoading = signal(true);
  protected isModalOpen = signal(false);
  protected selected = signal<Servicio | null>(null);
  protected allData = signal<ColumnData[]>([]);
  protected pageNumber = signal(1);
  protected pageSize = signal(20);
  protected totalCount = signal(0);
  protected totalPages = signal(0);
  private searchTerm = '';

  protected readonly headers: Column[] = [
    { value: 'Nombre', dataType: 'string', justify: 'left' },
    { value: 'Código', dataType: 'string', justify: 'left' },
    { value: 'Descripción', dataType: 'string', justify: 'left' },
  ];

  ngOnInit(): void {
    this.layoutService.setPageTitle('Servicios');
    this.layoutService.breadcrumbs.set([{ label: 'Servicios', url: '/settings/servicios' }]);
    void this.load();
  }

  private async load(): Promise<void> {
    this.isLoading.set(true);
    try {
      const result = await firstValueFrom(
        this.searchTerm
          ? this.service.searchPaged(this.searchTerm, this.pageNumber(), this.pageSize())
          : this.service.getAll(this.pageNumber(), this.pageSize()),
      );
      this.allData.set(result.items.map((i) => this.toColumnData(i)));
      this.pageNumber.set(result.pagination.currentPage);
      this.pageSize.set(result.pagination.pageSize);
      this.totalCount.set(result.pagination.total);
      this.totalPages.set(result.pagination.pageCount);
    } catch {
      this.notification.error('Error al cargar los servicios');
    } finally {
      this.isLoading.set(false);
    }
  }

  private toColumnData(item: Servicio): ColumnData {
    return {
      id: item.id,
      name: item.nombre,
      showActions: false,
      columns: [
        { value: item.nombre, dataType: 'string', justify: 'left' },
        { value: item.codigo ?? '-', dataType: 'string', justify: 'left' },
        { value: item.descripcion ?? '-', dataType: 'string', justify: 'left' },
      ],
      actionsOptions: [
        { name: 'Editar', function: (id) => this.onEdit(id) },
        { name: 'Eliminar', color: 'red', function: (id) => this.onConfirmationDelete(item.nombre, id) },
      ],
    };
  }

  protected openCreate(): void {
    this.selected.set(null);
    this.isModalOpen.set(true);
  }

  protected onModalClosed = (): void => {
    this.isModalOpen.set(false);
    void this.load();
  };

  protected onSearch(q: string): void {
    this.searchTerm = q;
    this.pageNumber.set(1);
    void this.load();
  }

  protected onPageChange(page: number): void {
    this.pageNumber.set(page);
    void this.load();
  }

  protected onPageSizeChange(size: number): void {
    this.pageSize.set(size);
    this.pageNumber.set(1);
    void this.load();
  }

  private onEdit = async (id: string): Promise<void> => {
    const item = await firstValueFrom(this.service.getById(id));
    if (!item) return;
    this.selected.set(item);
    this.isModalOpen.set(true);
  };

  private onConfirmationDelete(name: string, id: string): void {
    this.confirmationModal.openModal({
      title: 'Eliminar servicio',
      name,
      isDelete: true,
      paragraph: 'Está seguro que desea eliminar este servicio',
      confirmButton: 'Eliminar',
      onConfirm: () => this.onDelete(id),
    });
  }

  private onDelete(id: string): void {
    this.service.delete(id).subscribe({
      next: () => {
        void this.load();
        this.notification.success('Servicio eliminado con éxito');
      },
      error: () => this.notification.error('Error al eliminar el servicio'),
    });
    this.confirmationModal.closeModal();
  }
}
