import { Component, OnInit, inject, signal, ViewChild } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { LayoutService } from '@public-intelligence/ui-core';
import { TableComponent } from '../../../components/ui/table/table';
import { TableFilter } from '../../../components/shared/table-filter/table-filter';
import { CrudPageLayout } from '../../../components/ui/layout/crud-page/crud-page.layout';
import { ConfirmationModalComponent } from '../../../components/ui/modals/confirmation-modal/confirmation-modal.component';
import { EstadoLocalidadModalComponent } from '../../../components/ui/modals/estado-localidad-modal/estado-localidad-modal.component';
import { Column, ColumnData } from '../../../models/table';
import { NotificationService } from '../../../services/notification/notification.service';
import { EstadoLocalidadService } from '../../../services/locality-catalog/locality-catalog.services';
import { EstadoLocalidad } from '../../../models/locality-catalog.model';
import { Paginator } from '../../../components/ui/paginator/paginator';

@Component({
  selector: 'app-estado-localidad',
  imports: [TableComponent, TableFilter, CrudPageLayout, EstadoLocalidadModalComponent, ConfirmationModalComponent, Paginator],
  templateUrl: './estado-localidad.component.html',
})
export class EstadoLocalidadComponent implements OnInit {
  @ViewChild(ConfirmationModalComponent) confirmationModal!: ConfirmationModalComponent;
  private service = inject(EstadoLocalidadService);
  private layoutService = inject(LayoutService);
  private notification = inject(NotificationService);

  protected isLoading = signal(true);
  protected isModalOpen = signal(false);
  protected selected = signal<EstadoLocalidad | null>(null);
  protected allData = signal<ColumnData[]>([]);
  protected pageNumber = signal(1);
  protected pageSize = signal(20);
  protected totalCount = signal(0);
  protected totalPages = signal(0);
  private searchTerm = '';

  protected readonly headers: Column[] = [
    { value: 'Código', dataType: 'string', justify: 'left' },
    { value: 'Nombre', dataType: 'string', justify: 'left' },
    { value: 'Terminal', dataType: 'string', justify: 'center' },
  ];

  ngOnInit(): void {
    this.layoutService.setPageTitle('Estados de localidad');
    this.layoutService.breadcrumbs.set([{ label: 'Estados de localidad', url: '/settings/estados-localidad' }]);
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
      this.notification.error('Error al cargar los estados de localidad');
    } finally {
      this.isLoading.set(false);
    }
  }

  private toColumnData(item: EstadoLocalidad): ColumnData {
    return {
      id: item.id,
      name: item.nombre,
      showActions: false,
      columns: [
        { value: item.codigo, dataType: 'string', justify: 'left' },
        { value: item.nombre, dataType: 'string', justify: 'left' },
        { value: item.esTerminal ? 'Sí' : 'No', dataType: 'string', justify: 'center' },
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
      title: 'Eliminar estado de localidad',
      name,
      isDelete: true,
      paragraph: 'Está seguro que desea eliminar este estado de localidad',
      confirmButton: 'Eliminar',
      onConfirm: () => this.onDelete(id),
    });
  }

  private onDelete(id: string): void {
    this.service.delete(id).subscribe({
      next: () => {
        void this.load();
        this.notification.success('Estado de localidad eliminado');
      },
      error: () => this.notification.error('Error al eliminar'),
    });
    this.confirmationModal.closeModal();
  }
}
