import { Component, inject, OnInit, signal, ViewChild } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { LayoutService } from '@public-intelligence/ui-core';
import { OccupationalGroupService } from '../../../services/occupational-group/occupational-group.service';
import { OccupationalGroup } from '../../../models/occupational-group.model';
import { TableComponent } from '../../../components/ui/table/table';
import { CrudPageLayout } from '../../../components/ui/layout/crud-page/crud-page.layout';
import { ConfirmationModalComponent } from '../../../components/ui/modals/confirmation-modal/confirmation-modal.component';
import { NotificationService } from '../../../services/notification/notification.service';
import { Column, ColumnData } from '../../../models/table';
import { Paginator } from '../../../components/ui/paginator/paginator';

@Component({
  selector: 'app-occupacional-group',
  imports: [TableComponent, CrudPageLayout, ConfirmationModalComponent, Paginator],
  templateUrl: './occupacional-group.component.html',
})
export class OccupacionalGroupComponent implements OnInit {
  @ViewChild(ConfirmationModalComponent) confirmationModal!: ConfirmationModalComponent;

  private occupationalGroupService = inject(OccupationalGroupService);
  private layoutService = inject(LayoutService);
  private notification = inject(NotificationService);

  protected isLoading = signal(true);
  protected isModalOpen = signal(false);
  protected selectedGroup = signal<OccupationalGroup | null>(null);
  protected allData = signal<ColumnData[]>([]);
  protected pageNumber = signal(1);
  protected pageSize = signal(20);
  protected totalCount = signal(0);
  protected totalPages = signal(0);

  protected readonly headers: Column[] = [{ value: 'Nombre', dataType: 'string', justify: 'left' }];

  ngOnInit(): void {
    this.layoutService.setPageTitle('Grupos ocupacionales');
    this.layoutService.breadcrumbs.set([
      { label: 'Grupos ocupacionales', url: '/settings/occupational-groups' },
    ]);
    void this.loadGroups();
  }

  private async loadGroups(): Promise<void> {
    this.isLoading.set(true);
    try {
      const result = await firstValueFrom(
        this.occupationalGroupService.getAll(this.pageNumber(), this.pageSize()),
      );
      this.allData.set(result.items.map((item) => this.toColumnData(item)));
      this.pageNumber.set(result.pagination.currentPage);
      this.pageSize.set(result.pagination.pageSize);
      this.totalCount.set(result.pagination.total);
      this.totalPages.set(result.pagination.pageCount);
    } catch {
      this.notification.error('Error al cargar los grupos ocupacionales');
    } finally {
      this.isLoading.set(false);
    }
  }

  private toColumnData(item: OccupationalGroup): ColumnData {
    return {
      id: item.id,
      name: item.name,
      showActions: false,
      columns: [{ value: item.name, dataType: 'string', justify: 'left' }],
      actionsOptions: [
        { name: 'Editar', function: (id) => this.onEdit(id) },
        { name: 'Eliminar', color: 'red', function: (id) => this.onConfirmationDelete({ name: item.name, id }) },
      ],
    };
  }

  protected openCreateModal(): void {
    this.selectedGroup.set(null);
    this.isModalOpen.set(true);
  }

  protected onModalClosed = (): void => {
    this.isModalOpen.set(false);
    void this.loadGroups();
  };

  protected onPageChange(page: number): void {
    this.pageNumber.set(page);
    void this.loadGroups();
  }

  protected onPageSizeChange(size: number): void {
    this.pageSize.set(size);
    this.pageNumber.set(1);
    void this.loadGroups();
  }

  private onEdit = async (id: string) => {
    const item = await firstValueFrom(this.occupationalGroupService.getById(id));
    if (!item) return;
    this.selectedGroup.set(item);
    this.isModalOpen.set(true);
  };

  private onConfirmationDelete({ name, id }: { name: string; id: string }): void {
    this.confirmationModal.openModal({
      title: 'Eliminar grupo ocupacional',
      name,
      isDelete: true,
      paragraph: 'Está seguro que desea eliminar este grupo ocupacional',
      confirmButton: 'Eliminar',
      onConfirm: () => this.onDelete(id),
    });
  }

  private onDelete(id: string): void {
    this.occupationalGroupService.delete(id).subscribe({
      next: () => {
        void this.loadGroups();
        this.notification.success('Grupo ocupacional eliminado con éxito');
      },
      error: () => this.notification.error('Error al eliminar el grupo ocupacional'),
    });
    this.confirmationModal.closeModal();
  }
}
