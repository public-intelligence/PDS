import { Component, OnInit, ViewChild, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { firstValueFrom } from 'rxjs';
import { debounceTime } from 'rxjs/operators';
import { LayoutService } from '@public-intelligence/ui-core';

import { TableComponent } from '../../../components/ui/table/table';
import { Paginator } from '../../../components/ui/paginator/paginator';
import { CrudPageLayout } from '../../../components/ui/layout/crud-page/crud-page.layout';
import { CustomInput } from '../../../components/shared/custom-input/custom-input';
import { CustomSelect, SelectOption } from '../../../components/shared/custom-select/custom-select';
import { ConfirmationModalComponent } from '../../../components/ui/modals/confirmation-modal/confirmation-modal.component';
import { HolidayCalendarModalComponent } from '../../../components/ui/modals/holiday-calendar-modal/holiday-calendar-modal.component';
import { Actions, Column, ColumnData } from '../../../models/table';
import { CalendarioFeriados } from '../../../models/holiday-calendar.model';
import { AuthService } from '../../../services/auth/auth.service';
import { HolidayCalendarService } from '../../../services/holiday-calendar/holiday-calendar.service';
import { NotificationService } from '../../../services/notification/notification.service';
import { TerritoryService } from '../../../services/territory/territory.service';
import { AMBITO_LABELS } from '../../../utils/calendar-labels';
import { apiErrorDetail, apiErrorStatus } from '../../../utils/http-error';

/** Desde octubre (mes 9) se avisa de los calendarios sin feriados del año siguiente (UI-09). */
const NEXT_YEAR_WARNING_FROM_MONTH = 9;
const MISSING_HOLIDAYS_CHIP = 'Sin feriados cargados';

/** UI-09: lista de calendarios de feriados (RF-LO-27, RF-LO-40). */
@Component({
  selector: 'app-holiday-calendars-list',
  imports: [
    ReactiveFormsModule,
    TableComponent,
    Paginator,
    CrudPageLayout,
    CustomInput,
    CustomSelect,
    ConfirmationModalComponent,
    HolidayCalendarModalComponent,
  ],
  templateUrl: './holiday-calendars-list.component.html',
})
export class HolidayCalendarsListComponent implements OnInit {
  @ViewChild(ConfirmationModalComponent) confirmationModal!: ConfirmationModalComponent;
  private fb = inject(FormBuilder);
  private service = inject(HolidayCalendarService);
  private territory = inject(TerritoryService);
  private auth = inject(AuthService);
  private layoutService = inject(LayoutService);
  private notification = inject(NotificationService);
  private router = inject(Router);

  private readonly today = new Date();
  private readonly currentYear = this.today.getFullYear();
  private readonly warnMissingNextYear = this.today.getMonth() >= NEXT_YEAR_WARNING_FROM_MONTH;

  protected readonly isAdmin = this.auth.isCalendarAdmin();
  protected isLoading = signal(true);
  protected hasError = signal(false);
  protected rows = signal<ColumnData[]>([]);
  protected paisOptions = signal<SelectOption[]>([]);
  protected pageNumber = signal(1);
  protected pageSize = signal(20);
  protected totalCount = signal(0);
  protected totalPages = signal(0);

  protected isModalOpen = signal(false);
  protected selected = signal<CalendarioFeriados | null>(null);

  /** Calendarios cuyos feriados mantiene un gestor no administrador (RF-LO-40). */
  private managed = signal<CalendarioFeriados[]>([]);
  protected canFilterManaged = computed(() => this.managed().length > 0);

  protected filterForm = this.fb.group({ q: [''], paisId: [''], soloGestiono: [false] });

  protected readonly headers: Column[] = [
    { value: 'Código', dataType: 'string', justify: 'left' },
    { value: 'Nombre', dataType: 'string', justify: 'left' },
    { value: 'País', dataType: 'string', justify: 'left' },
    { value: 'Ámbito', dataType: 'string', justify: 'left' },
    { value: 'Localidad gestora', dataType: 'string', justify: 'left' },
    { value: 'Localidades suscritas', dataType: 'number', justify: 'right' },
    { value: `Feriados ${this.currentYear}`, dataType: 'number', justify: 'right' },
    { value: `Feriados ${this.currentYear + 1}`, dataType: 'number', justify: 'right' },
  ];

  constructor() {
    this.filterForm.valueChanges.pipe(debounceTime(300), takeUntilDestroyed()).subscribe(() => {
      this.pageNumber.set(1);
      void this.load();
    });
  }

  ngOnInit(): void {
    this.layoutService.setPageTitle('Calendarios de feriados');
    this.layoutService.breadcrumbs.set([
      { label: 'Calendarios de feriados', url: '/settings/calendarios-feriados' },
    ]);
    void this.loadFilters();
    void this.load();
  }

  private async loadFilters(): Promise<void> {
    try {
      const paises = await firstValueFrom(this.territory.paises());
      this.paisOptions.set(paises.map((p) => ({ label: p.nombre, value: p.id })));
      if (!this.isAdmin) this.managed.set(await firstValueFrom(this.service.gestionables()));
    } catch {
      this.notification.error('No se pudieron cargar los filtros');
    }
  }

  protected async load(): Promise<void> {
    this.isLoading.set(true);
    this.hasError.set(false);
    try {
      const { q, paisId, soloGestiono } = this.filterForm.getRawValue();
      if (soloGestiono) {
        this.showPage(this.managed(), 1, 1, this.managed().length);
        return;
      }
      const result = await firstValueFrom(
        this.service.getAll(this.pageNumber(), this.pageSize(), { q, paisId }),
      );
      const { currentPage, pageCount, total } = result.pagination;
      this.showPage(result.items, currentPage, pageCount, total);
    } catch {
      this.hasError.set(true);
    } finally {
      this.isLoading.set(false);
    }
  }

  private showPage(items: CalendarioFeriados[], page: number, pages: number, total: number): void {
    this.rows.set(items.map((item) => this.toColumnData(item)));
    this.pageNumber.set(page);
    this.totalPages.set(pages);
    this.totalCount.set(total);
  }

  private toColumnData(item: CalendarioFeriados): ColumnData {
    const actions: Actions[] = [{ name: 'Ver', function: (id) => this.open(id) }];
    if (this.isAdmin) {
      actions.push(
        { name: 'Editar', function: () => this.openEdit(item) },
        { name: 'Eliminar', color: 'red', function: () => this.confirmDelete(item) },
      );
    }
    const missingNextYear = this.warnMissingNextYear && !item.feriadosAnioSiguiente;
    return {
      id: item.id,
      name: item.nombre,
      showActions: false,
      columns: [
        { value: item.codigo, dataType: 'string', justify: 'left' },
        { value: item.nombre, dataType: 'string', justify: 'left' },
        { value: item.paisNombre ?? '—', dataType: 'string', justify: 'left' },
        { value: AMBITO_LABELS[item.ambito] ?? item.ambito, dataType: 'string', justify: 'left' },
        { value: item.localidadGestoraNombre ?? '—', dataType: 'string', justify: 'left' },
        { value: String(item.localidadesSuscritas ?? 0), dataType: 'number', justify: 'right' },
        { value: String(item.feriadosAnioActual ?? 0), dataType: 'number', justify: 'right' },
        missingNextYear
          ? { value: MISSING_HOLIDAYS_CHIP, dataType: 'string', justify: 'right', color: 'yellow' }
          : { value: String(item.feriadosAnioSiguiente ?? 0), dataType: 'number', justify: 'right' },
      ],
      actionsOptions: actions,
    };
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

  private open(id: string): void {
    void this.router.navigate(['/settings/calendarios-feriados', id]);
  }

  protected openCreate(): void {
    this.selected.set(null);
    this.isModalOpen.set(true);
  }

  private openEdit(item: CalendarioFeriados): void {
    this.selected.set(item);
    this.isModalOpen.set(true);
  }

  protected onModalClosed(): void {
    this.isModalOpen.set(false);
    void this.load();
  }

  private confirmDelete(item: CalendarioFeriados): void {
    this.confirmationModal.openModal({
      title: 'Eliminar calendario de feriados',
      name: `${item.codigo} · ${item.nombre}`,
      isDelete: true,
      paragraph: 'Está seguro que desea eliminar el calendario',
      confirmButton: 'Eliminar',
      onConfirm: () => this.delete(item.id),
    });
  }

  private delete(id: string): void {
    this.confirmationModal.closeModal();
    this.service.delete(id).subscribe({
      next: () => {
        this.notification.success('Calendario de feriados eliminado');
        void this.load();
      },
      error: (err) => this.showDeleteError(err),
    });
  }

  /** 409: el calendario tiene localidades suscritas y el API las enumera (RF-LO-27). */
  private showDeleteError(err: unknown): void {
    const subscribers = apiErrorDetail(err, 'suscriptores');
    if (apiErrorStatus(err) === 409 && subscribers.length) {
      this.notification.error(
        `No se puede eliminar: lo usan ${subscribers.length} localidades (${subscribers.join(', ')}).`,
        8000,
      );
      return;
    }
    this.notification.fromHttpError(err, 'No se pudo eliminar el calendario de feriados');
  }
}
