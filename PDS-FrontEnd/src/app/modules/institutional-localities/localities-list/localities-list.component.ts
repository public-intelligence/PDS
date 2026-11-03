import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder } from '@angular/forms';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { debounceTime } from 'rxjs/operators';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { LayoutService } from '@public-intelligence/ui-core';

import { TableComponent } from '../../../components/ui/table/table';
import { CrudPageLayout } from '../../../components/ui/layout/crud-page/crud-page.layout';
import { CustomInput } from '../../../components/shared/custom-input/custom-input';
import { CustomSelect, SelectOption } from '../../../components/shared/custom-select/custom-select';
import { Actions, Column, ColumnData } from '../../../models/table';
import { NotificationService } from '../../../services/notification/notification.service';
import { InstitutionalLocalityService } from '../../../services/institutional-locality/institutional-locality.service';
import {
  TipoLocalidadService,
  EstadoLocalidadService,
  FuncionLocalidadService,
} from '../../../services/locality-catalog/locality-catalog.services';
import { TerritoryService } from '../../../services/territory/territory.service';
import {
  LocalityListItem,
  LocalityListQuery,
  LocalityStatusCode,
} from '../../../models/institutional-locality.model';
import { EstadoLocalidad } from '../../../models/locality-catalog.model';
import { PagedResult } from '../../../models/paged-response.model';
import { LocalidadGestionable } from '../../../models/locality-calendar.model';
import { LocalityCalendarService } from '../../../services/locality-calendar/locality-calendar.service';
import { AuthService } from '../../../services/auth/auth.service';
import { isFeatureEnabled } from '../../../utils/feature-flags';
import { formatLocation } from '../../../utils/locality-format';
import { zoneWithOffset } from '../../../utils/time-zone';
import { LocalitiesMapComponent } from '../localities-map/localities-map.component';
import { LocalityFormModalComponent } from '../../../components/ui/modals/locality-form-modal/locality-form-modal.component';
import { LocalitySuspendModalComponent } from '../../../components/ui/modals/locality-suspend-modal/locality-suspend-modal.component';
import { LocalityCloseModalComponent } from '../../../components/ui/modals/locality-close-modal/locality-close-modal.component';

@Component({
  selector: 'app-localities-list',
  imports: [
    ReactiveFormsModule,
    TableComponent,
    CrudPageLayout,
    CustomInput,
    CustomSelect,
    LocalitiesMapComponent,
    LocalityFormModalComponent,
    LocalitySuspendModalComponent,
    LocalityCloseModalComponent,
  ],
  templateUrl: './localities-list.component.html',
})
export class LocalitiesListComponent implements OnInit {
  private fb = inject(FormBuilder);
  private service = inject(InstitutionalLocalityService);
  private tipoService = inject(TipoLocalidadService);
  private estadoService = inject(EstadoLocalidadService);
  private territoryService = inject(TerritoryService);
  private funcionService = inject(FuncionLocalidadService);
  private calendarService = inject(LocalityCalendarService);
  private auth = inject(AuthService);
  private layoutService = inject(LayoutService);
  private notification = inject(NotificationService);
  private router = inject(Router);

  protected isLoading = signal(true);
  protected viewMode = signal<'table' | 'map'>('table');
  protected paged = signal<PagedResult<LocalityListItem> | null>(null);
  protected query = signal<LocalityListQuery>({ pageNumber: 1, pageSize: 20 });

  protected tipoOptions = signal<SelectOption[]>([]);
  protected estadoOptions = signal<SelectOption[]>([]);
  protected provinciaOptions = signal<SelectOption[]>([]);
  protected paisOptions = signal<SelectOption[]>([]);
  protected funcionOptions = signal<SelectOption[]>([]);
  private estadoCodeById = new Map<string, LocalityStatusCode>();
  private funcionNameByCode = new Map<string, string>();

  protected readonly showFunciones = isFeatureEnabled('funcionesLocalidad');

  /** Localidades cuyo calendario gestiona un usuario no administrador (RF-LO-40). */
  private managed = signal<LocalidadGestionable[]>([]);
  protected canFilterManaged = computed(() => this.managed().length > 0);

  // Modales
  protected isFormOpen = signal(false);
  protected editingId = signal<string | null>(null);
  protected isSuspendOpen = signal(false);
  protected suspendMode = signal<'suspend' | 'reactivate'>('suspend');
  protected isCloseOpen = signal(false);
  protected targetId = signal<string | null>(null);
  protected targetName = signal<string>('');

  protected filterForm = this.fb.group({
    nombre: [''],
    tipoId: [''],
    estadoId: [''],
    provinciaId: [''],
    paisId: [''],
    funcionId: [''],
    soloGestiono: [false],
  });

  protected rows = signal<ColumnData[]>([]);

  protected readonly headers: Column[] = [
    { value: 'Código', dataType: 'string', justify: 'left' },
    { value: 'Nombre', dataType: 'string', justify: 'left' },
    { value: 'Tipo', dataType: 'string', justify: 'left' },
    { value: 'Estado', dataType: 'string', justify: 'center' },
    { value: 'Ubicación', dataType: 'string', justify: 'left' },
    { value: 'Zona horaria', dataType: 'string', justify: 'left' },
    ...(this.showFunciones
      ? [{ value: 'Funciones', dataType: 'string', justify: 'left' } satisfies Column]
      : []),
  ];

  protected totalPages = computed(() => this.paged()?.totalPages ?? 1);
  protected currentPage = computed(() => this.paged()?.page ?? 1);
  protected totalItems = computed(() => this.paged()?.total ?? 0);
  protected rangeFrom = computed(() => {
    const p = this.paged();
    if (!p || p.total === 0) return 0;
    return (p.page - 1) * p.pageSize + 1;
  });
  protected rangeTo = computed(() => {
    const p = this.paged();
    if (!p) return 0;
    return Math.min(p.page * p.pageSize, p.total);
  });

  constructor() {
    // `takeUntilDestroyed()` requiere un contexto de inyección, que existe en el constructor
    // (no en `ngOnInit`). Aquí enlazamos los filtros a la query y recargamos al cambiar.
    this.filterForm.valueChanges
      .pipe(debounceTime(300), takeUntilDestroyed())
      .subscribe((v) => {
        this.query.set({
          ...this.query(),
          pageNumber: 1,
          nombre: v.nombre || undefined,
          tipoId: v.tipoId || undefined,
          estadoId: v.estadoId || undefined,
          provinciaId: v.provinciaId || undefined,
          paisId: v.paisId || undefined,
          funcionId: v.funcionId || undefined,
        });
        void this.load();
      });
  }

  ngOnInit(): void {
    this.layoutService.setPageTitle('Localidades institucionales');
    this.layoutService.breadcrumbs.set([{ label: 'Localidades', url: '/localidades' }]);
    void this.loadFilters();
    void this.load();
  }

  private async loadFilters(): Promise<void> {
    try {
      const [tiposResult, estadosResult, provincias, paises] = await Promise.all([
        firstValueFrom(this.tipoService.getAll()),
        firstValueFrom(this.estadoService.getAll()),
        firstValueFrom(this.territoryService.provincias()),
        firstValueFrom(this.territoryService.paises()),
      ]);
      this.paisOptions.set(paises.map((p) => ({ label: p.nombre, value: p.id })));
      this.tipoOptions.set(tiposResult.items.map((t) => ({ label: t.nombre, value: t.id })));
      this.estadoOptions.set(estadosResult.items.map((e: EstadoLocalidad) => ({ label: e.nombre, value: e.id })));
      this.provinciaOptions.set(provincias.map((p) => ({ label: p.nombre, value: p.id })));
      this.estadoCodeById = new Map(estadosResult.items.map((e) => [e.id, e.codigo as LocalityStatusCode]));
    } catch {
      this.notification.error('No se pudieron cargar los filtros');
    }
    void this.loadFunciones();
    void this.loadManaged();
  }

  private async loadFunciones(): Promise<void> {
    if (!this.showFunciones) return;
    try {
      const { items } = await firstValueFrom(this.funcionService.getAll(1, 100));
      this.funcionOptions.set(items.map((f) => ({ label: f.nombre, value: f.id })));
      this.funcionNameByCode = new Map(items.map((f) => [f.codigo, f.nombre]));
      this.rows.set((this.paged()?.items ?? []).map((item) => this.toColumnData(item)));
    } catch {
      /* la columna muestra los códigos si el catálogo no carga */
    }
  }

  /** «Solo las que gestiono» es para gestores: el administrador ya gestiona todas (UI-01). */
  private async loadManaged(): Promise<void> {
    if (!isFeatureEnabled('calendarioLaboral') || this.auth.isCalendarAdmin()) return;
    try {
      this.managed.set(await firstValueFrom(this.calendarService.gestionables()));
    } catch {
      /* sin la lista, el filtro no se ofrece */
    }
  }

  protected async load(): Promise<void> {
    this.isLoading.set(true);
    try {
      const result = await firstValueFrom(this.service.list(this.query()));
      this.paged.set(result);
      this.rows.set(this.visibleItems(result.items).map((item) => this.toColumnData(item)));
    } catch {
      this.notification.error('Error al cargar las localidades');
    } finally {
      this.isLoading.set(false);
    }
  }

  /** El API no filtra por gestor: «Solo las que gestiono» acota la página cargada. */
  private visibleItems(items: LocalityListItem[]): LocalityListItem[] {
    if (!this.filterForm.controls.soloGestiono.value) return items;
    const managedIds = new Set(this.managed().map((m) => m.id));
    return items.filter((item) => managedIds.has(item.id));
  }

  private funcionesText(codes: string[] | null | undefined): string {
    return codes?.map((code) => this.funcionNameByCode.get(code) ?? code).join(', ') || '—';
  }

  private toColumnData(item: LocalityListItem): ColumnData {
    const code = item.estadoLocalidadId ? this.estadoCodeById.get(item.estadoLocalidadId) : undefined;
    const isSuspended = code === 'SUSPENDIDA_TEMP';
    const isClosed = code === 'EN_CIERRE' || code === 'INACTIVA';

    const actions: Actions[] = [
      { name: 'Ver detalle', function: (id: string) => this.router.navigate(['/localidades', id]) },
      { name: 'Editar', function: (id: string) => this.openEdit(id) },
    ];
    if (isSuspended) {
      actions.push({ name: 'Reactivar', function: (id: string) => this.openSuspend(id, item.nombreOficial, 'reactivate') });
    } else if (!isClosed) {
      actions.push({ name: 'Suspender', function: (id: string) => this.openSuspend(id, item.nombreOficial, 'suspend') });
    }
    if (!isClosed) {
      actions.push({ name: 'Cerrar', color: 'red', function: (id: string) => this.openClose(id, item.nombreOficial) });
    }

    return {
      id: item.id,
      name: item.nombreOficial,
      showActions: false,
      columns: [
        { value: item.codigo, dataType: 'string', justify: 'left' },
        { value: item.nombreOficial, dataType: 'string', justify: 'left' },
        { value: item.tipoNombre ?? '-', dataType: 'string', justify: 'left' },
        {
          value: item.estadoNombre ?? '-',
          dataType: 'string',
          justify: 'center',
          color: this.estadoColor(code),
        },
        { value: formatLocation(item), dataType: 'string', justify: 'left' },
        {
          value: item.zonaHoraria ? zoneWithOffset(item.zonaHoraria) : '—',
          dataType: 'string',
          justify: 'left',
        },
        ...(this.showFunciones
          ? [{ value: this.funcionesText(item.funciones), dataType: 'string', justify: 'left' } satisfies Column]
          : []),
      ],
      actionsOptions: actions,
    };
  }

  private estadoColor(code?: LocalityStatusCode): 'green' | 'red' | 'yellow' | 'default' {
    switch (code) {
      case 'ACTIVA':
        return 'green';
      case 'SUSPENDIDA_TEMP':
      case 'EN_APERTURA':
        return 'yellow';
      case 'EN_CIERRE':
      case 'INACTIVA':
        return 'red';
      default:
        return 'default';
    }
  }

  // ── Vista ──
  protected setView(mode: 'table' | 'map'): void {
    this.viewMode.set(mode);
  }

  protected setPage(page: number): void {
    if (page < 1 || page > this.totalPages()) return;
    this.query.set({ ...this.query(), pageNumber: page });
    void this.load();
  }

  // ── Modales ──
  protected openCreate(): void {
    this.editingId.set(null);
    this.isFormOpen.set(true);
  }

  protected openEdit(id: string): void {
    this.editingId.set(id);
    this.isFormOpen.set(true);
  }

  protected openSuspend(id: string, name: string, mode: 'suspend' | 'reactivate'): void {
    this.targetId.set(id);
    this.targetName.set(name);
    this.suspendMode.set(mode);
    this.isSuspendOpen.set(true);
  }

  protected openClose(id: string, name: string): void {
    this.targetId.set(id);
    this.targetName.set(name);
    this.isCloseOpen.set(true);
  }

  protected onFormClosed(): void {
    this.isFormOpen.set(false);
  }
  protected onFormSaved(): void {
    this.isFormOpen.set(false);
    void this.load();
  }
  protected onSuspendDone(): void {
    this.isSuspendOpen.set(false);
    void this.load();
  }
  protected onCloseDone(): void {
    this.isCloseOpen.set(false);
    void this.load();
  }
}
