import {
  Component,
  ElementRef,
  effect,
  inject,
  input,
  output,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import { HttpClient } from '@angular/common/http';
import {
  AbstractControl,
  FormArray,
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../../../environments/environment';
import { CustomModal } from '../../../shared/custom-modal/custom-modal';
import { CustomInput } from '../../../shared/custom-input/custom-input';
import { CustomTextarea } from '../../../shared/custom-textarea/custom-textarea';
import { CustomSelect, SelectOption } from '../../../shared/custom-select/custom-select';
import { CustomSelectSearch } from '../../../shared/custom-select-search/custom-select-search';
import { FormErrorBanner } from '../../../shared/form-error-banner/form-error-banner';
import { LocalityDuplicateModalComponent } from '../locality-duplicate-modal/locality-duplicate-modal.component';

import { NotificationService } from '../../../../services/notification/notification.service';
import { AuthService } from '../../../../services/auth/auth.service';
import { InstitutionalLocalityService } from '../../../../services/institutional-locality/institutional-locality.service';
import { TerritoryService } from '../../../../services/territory/territory.service';
import {
  FuncionLocalidadService,
  NaturalezaAdministrativaService,
  ResponsableService,
  ServicioService,
  TipoLocalidadService,
  ZonaHorariaService,
} from '../../../../services/locality-catalog/locality-catalog.services';
import { StructureService } from '../../../../services/structure/structure.service';
import {
  DuplicateWarning,
  LocalityResponsibleLink,
  LocalityUpsert,
} from '../../../../models/institutional-locality.model';
import { Pais } from '../../../../models/territory.model';
import { FuncionLocalidad } from '../../../../models/locality-catalog.model';
import { isFeatureEnabled } from '../../../../utils/feature-flags';
import { timeZoneOptionLabel } from '../../../../utils/time-zone';
import {
  clearApiErrors,
  controlError,
  resolveFormApiError,
} from '../../../../utils/http-error';
import { MapHandle, MapProvider, RD_CENTER, RD_ZOOM } from '../../../../services/map/map-provider';
import { LeafletMapProvider } from '../../../../services/map/leaflet-map.provider';

const DOMINICAN_REPUBLIC_CODE = 'DO';
const ABROAD_TEXT_MAX_LENGTH = 150;

/** Claves de `detail` del API (§10.5) que el formulario muestra junto a un campo. */
const FIELD_BY_API_KEY: Record<string, string> = {
  codigo: 'codigo',
  nombreOficial: 'nombreOficial',
  direccionCompleta: 'direccionCompleta',
  motivo: 'motivo',
  zonaHoraria: 'zonaHoraria',
  ciudad: 'ciudad',
  territorio: 'paisId',
  funciones: 'funcionIds',
};

/** Una sección consular solo existe dentro de una embajada (RF-LO-45). */
const CONSULAR_SECTION_CODE = 'SECCION_CONSULAR';
const EMBASSY_CODE = 'EMBAJADA';
const CONSULAR_SECTION_ERROR = 'Una sección consular debe pertenecer a una embajada.';

/** Modal grande de crear/editar localidad (RF-LO-01/06) con cascada territorial, selector de
 *  coordenadas en el mapa y flujo de duplicados (RF-LO-13). */
@Component({
  selector: 'app-locality-form-modal',
  imports: [
    ReactiveFormsModule,
    CustomModal,
    CustomInput,
    CustomTextarea,
    CustomSelect,
    CustomSelectSearch,
    FormErrorBanner,
    LocalityDuplicateModalComponent,
  ],
  providers: [{ provide: MapProvider, useClass: LeafletMapProvider }],
  templateUrl: './locality-form-modal.component.html',
})
export class LocalityFormModalComponent {
  private fb = inject(FormBuilder);
  private http = inject(HttpClient);
  private service = inject(InstitutionalLocalityService);
  private territory = inject(TerritoryService);
  private tipoService = inject(TipoLocalidadService);
  private naturalezaService = inject(NaturalezaAdministrativaService);
  private servicioService = inject(ServicioService);
  private responsableService = inject(ResponsableService);
  private structureService = inject(StructureService);
  private zonaHorariaService = inject(ZonaHorariaService);
  private funcionService = inject(FuncionLocalidadService);
  private auth = inject(AuthService);
  private notification = inject(NotificationService);
  private mapProvider = inject(MapProvider);

  readonly isOpen = input(false);
  readonly localityId = input<string | null>(null);

  readonly closed = output<void>();
  readonly saved = output<void>();

  protected isSaving = signal(false);
  protected isLoading = signal(false);
  protected isEdit = signal(false);

  // Opciones
  protected tipoOptions = signal<SelectOption[]>([]);
  protected naturalezaOptions = signal<SelectOption[]>([]);
  protected servicioOptions = signal<SelectOption[]>([]);
  protected responsableOptions = signal<SelectOption[]>([]);
  protected unidadOptions = signal<SelectOption[]>([]);
  protected instrumentoOptions = signal<SelectOption[]>([]);
  protected paisOptions = signal<SelectOption[]>([]);
  protected regionOptions = signal<SelectOption[]>([]);
  protected provinciaOptions = signal<SelectOption[]>([]);
  protected municipioOptions = signal<SelectOption[]>([]);
  protected distritoOptions = signal<SelectOption[]>([]);
  protected localidadOptions = signal<SelectOption[]>([]);
  protected zonaOptions = signal<SelectOption[]>([]);

  /** Funciones de servicio exterior (RF-LO-45); vacío mientras la funcionalidad esté apagada. */
  protected readonly showFunciones = isFeatureEnabled('funcionesLocalidad');
  protected funciones = signal<FuncionLocalidad[]>([]);
  private isCalendarAdmin = false;

  /** En edición, solo los administradores de calendario cambian la zona horaria (RF-LO-20). */
  protected zonaReadOnly = signal(false);
  private paises: Pais[] = [];

  /** País distinto de la República Dominicana: ciudad en vez de la cascada (RF-LO-18). */
  protected isAbroad = signal(false);

  /** Errores del API sin campo propio: banner al inicio del formulario (§12.2). */
  protected formError = signal('');

  // Mapa para escoger coordenadas
  protected showMap = signal(false);
  private pickContainer = viewChild<ElementRef<HTMLElement>>('pickMap');
  private mapHandle?: MapHandle;
  private mapInited = false;

  // Flujo de duplicados
  protected duplicateWarnings = signal<DuplicateWarning[]>([]);
  protected isDuplicateOpen = signal(false);
  private pendingBody: LocalityUpsert | null = null;

  protected form: FormGroup = this.fb.group({
    codigo: ['', [Validators.required]],
    nombreOficial: ['', [Validators.required]],
    nombreCorto: [''],
    sigla: [''],
    tipoLocalidadId: ['', [Validators.required]],
    naturalezaAdministrativaId: ['', [Validators.required]],
    funcionIds: [[] as string[], [(c: AbstractControl) => this.consularSectionError(c.value ?? [])]],
    unidadResponsableId: [''],
    unidadVinculadaId: [''],
    localidadPadreId: [''],
    paisId: ['', [Validators.required]],
    regionId: [''],
    provinciaId: [''],
    municipioId: [''],
    distritoMunicipalId: [''],
    ciudad: [''],
    subdivisionAdministrativa: ['', [Validators.maxLength(ABROAD_TEXT_MAX_LENGTH)]],
    sectorBarrioParaje: [''],
    direccionCompleta: ['', [Validators.required]],
    edificioPisoLocalReferenciaInterna: [''],
    codigoPostal: [''],
    zonaHoraria: ['', [Validators.required]],
    latitud: [null as number | null],
    longitud: [null as number | null],
    horarioRegular: [''],
    notasAcceso: [''],
    observaciones: [''],
    fechaApertura: [''],
    motivo: [''],
    servicios: this.fb.array([] as FormGroup[]),
    responsables: this.fb.array([] as FormGroup[]),
    documentos: this.fb.array([] as FormGroup[]),
    instrumentosLegales: this.fb.array([] as FormGroup[]),
    unidadesPresencia: this.fb.array([] as FormGroup[]),
    coberturas: this.fb.array([] as FormGroup[]),
  });

  constructor() {
    // Carga inicial y parcheo al abrir.
    // Solo `isOpen` debe disparar la carga: `untracked` evita que las señales leídas
    // durante onOpen (p. ej. isAbroad/zonaReadOnly vía valueChanges) relancen el efecto.
    effect(() => {
      const open = this.isOpen();
      untracked(() => (open ? void this.onOpen() : this.teardownMap()));
    });

    // Inicializa el mapa de selección cuando se muestra.
    effect(() => {
      const el = this.pickContainer()?.nativeElement;
      if (this.isOpen() && this.showMap() && el && !this.mapInited) void this.initPickMap(el);
    });

    // Cascada territorial.
    this.form
      .get('paisId')!
      .valueChanges.pipe(takeUntilDestroyed())
      .subscribe((paisId) => {
        void this.onPaisChange(paisId);
        void this.onPaisZonaChange(paisId);
      });
    this.form
      .get('regionId')!
      .valueChanges.pipe(takeUntilDestroyed())
      .subscribe((regionId) => {
        void this.onRegionChange(regionId);
      });
    this.form
      .get('provinciaId')!
      .valueChanges.pipe(takeUntilDestroyed())
      .subscribe((provinciaId) => {
        void this.onProvinciaChange(provinciaId);
      });
    this.form
      .get('municipioId')!
      .valueChanges.pipe(takeUntilDestroyed())
      .subscribe((municipioId) => {
        void this.onMunicipioChange(municipioId);
      });
  }

  // ── FormArray helpers ──
  protected arr(name: string): FormArray {
    return this.form.get(name) as FormArray;
  }
  protected groups(name: string): FormGroup[] {
    return this.arr(name).controls as FormGroup[];
  }
  protected addServicio(): void {
    this.arr('servicios').push(
      this.fb.group({ servicioId: ['', Validators.required], habilitado: [true] }),
    );
  }
  protected addResponsable(): void {
    this.arr('responsables').push(this.responsableGroup());
  }

  /** «Gestiona calendario» solo lo cambian los administradores de calendario (RF-LO-38). */
  private responsableGroup(link?: LocalityResponsibleLink): FormGroup {
    return this.fb.group({
      responsableId: [link?.responsableId ?? '', Validators.required],
      rol: [link?.rol ?? ''],
      principal: [link?.principal ?? false],
      gestionaCalendario: [
        { value: link?.gestionaCalendario ?? false, disabled: !this.isCalendarAdmin },
      ],
    });
  }
  protected addDocumento(): void {
    this.arr('documentos').push(
      this.fb.group({
        tipoDocumento: ['', Validators.required],
        descripcion: [''],
        url: ['', Validators.required],
      }),
    );
  }
  protected addInstrumento(): void {
    this.arr('instrumentosLegales').push(
      this.fb.group({ instrumentoLegalId: ['', Validators.required], proposito: [''] }),
    );
  }
  protected addUnidad(): void {
    this.arr('unidadesPresencia').push(
      this.fb.group({ unidadOrganizativaId: ['', Validators.required], notas: [''] }),
    );
  }
  protected addCobertura(): void {
    this.arr('coberturas').push(this.fb.group({ provinciaId: ['', Validators.required] }));
  }
  protected removeAt(name: string, i: number): void {
    this.arr(name).removeAt(i);
  }

  // ── Apertura / carga ──
  private async onOpen(): Promise<void> {
    this.isLoading.set(true);
    try {
      await this.loadForm();
    } finally {
      this.isLoading.set(false);
    }
  }

  private async loadForm(): Promise<void> {
    this.resetForm();
    this.isCalendarAdmin = this.auth.isCalendarAdmin();
    const id = this.localityId();
    this.isEdit.set(!!id);
    await this.loadStaticOptions();
    const motivo = this.form.get('motivo')!;
    if (id) {
      motivo.setValidators([Validators.required]);
      await this.loadForEdit(id);
    } else {
      motivo.clearValidators();
      this.form.get('paisId')!.setValue(this.dominicanRepublicId() ?? '');
    }
    motivo.updateValueAndValidity({ emitEvent: false });
  }

  private resetForm(): void {
    this.form.reset({ latitud: null, longitud: null, funcionIds: [] });
    [
      'servicios',
      'responsables',
      'documentos',
      'instrumentosLegales',
      'unidadesPresencia',
      'coberturas',
    ].forEach((n) => this.arr(n).clear());
    this.regionOptions.set([]);
    this.provinciaOptions.set([]);
    this.municipioOptions.set([]);
    this.distritoOptions.set([]);
    this.zonaOptions.set([]);
    this.setZonaReadOnly(false);
    this.setCountryBranch('');
    this.formError.set('');
    this.showMap.set(false);
    this.duplicateWarnings.set([]);
    this.isDuplicateOpen.set(false);
    this.pendingBody = null;
  }

  private async loadStaticOptions(): Promise<void> {
    try {
      const [
        tiposResult,
        naturalezasResult,
        serviciosResult,
        responsablesResult,
        unidadesResult,
        paises,
        instrumentos,
        localidades,
      ] = await Promise.all([
        firstValueFrom(this.tipoService.getAll()),
        firstValueFrom(this.naturalezaService.getAll()),
        firstValueFrom(this.servicioService.getAll()),
        firstValueFrom(this.responsableService.getAll()),
        firstValueFrom(this.structureService.searchPaged('', 1, 20)),
        firstValueFrom(this.territory.paises()),
        firstValueFrom(
          this.http.get<{ id: string; type: string; number: string }[]>(
            `${environment.apiUrl}legal-instruments`,
          ),
        ),
        firstValueFrom(this.service.list({ pageSize: 200 })),
      ]);
      this.tipoOptions.set(tiposResult.items.map((t) => ({ label: t.nombre, value: t.id })));
      this.naturalezaOptions.set(
        naturalezasResult.items.map((n) => ({ label: n.nombre, value: n.id })),
      );
      this.servicioOptions.set(
        serviciosResult.items.map((s) => ({ label: s.nombre, value: s.id })),
      );
      this.responsableOptions.set(
        responsablesResult.items.map((r) => ({ label: r.nombre, value: r.id })),
      );
      this.unidadOptions.set(
        unidadesResult.items.map((u) => ({ label: `${u.code} · ${u.name}`, value: u.id })),
      );
      this.paises = paises;
      this.paisOptions.set(paises.map((p) => ({ label: p.nombre, value: p.id })));
      this.instrumentoOptions.set(
        instrumentos.map((i) => ({ label: `${i.type} ${i.number}`, value: i.id })),
      );
      this.localidadOptions.set(
        paises.length
          ? (await firstValueFrom(this.service.list({ pageSize: 200 }))).items.map((l) => ({
              label: l.nombreOficial,
              value: l.id,
            }))
          : [],
      );
      void localidades;
      if (this.showFunciones) {
        this.funciones.set((await firstValueFrom(this.funcionService.getAll(1, 100))).items);
      }
    } catch {
      this.notification.error('No se pudieron cargar los catálogos del formulario');
    }
  }

  private async loadForEdit(id: string): Promise<void> {
    try {
      const d = await firstValueFrom(this.service.detail(id));
      this.form.patchValue(
        {
          codigo: d.codigo,
          nombreOficial: d.nombreOficial,
          nombreCorto: d.nombreCorto ?? '',
          sigla: d.sigla ?? '',
          tipoLocalidadId: d.tipoLocalidadId,
          naturalezaAdministrativaId: d.naturalezaAdministrativaId,
          funcionIds: (d.funciones ?? []).map((f) => f.id),
          unidadResponsableId: d.unidadResponsableId ?? '',
          unidadVinculadaId: d.unidadVinculadaId ?? '',
          localidadPadreId: d.localidadPadreId ?? '',
          paisId: d.paisId,
          ciudad: d.ciudad ?? '',
          subdivisionAdministrativa: d.subdivisionAdministrativa ?? '',
          sectorBarrioParaje: d.sectorBarrioParaje ?? '',
          direccionCompleta: d.direccionCompleta,
          edificioPisoLocalReferenciaInterna: d.edificioPisoLocalReferenciaInterna ?? '',
          codigoPostal: d.codigoPostal ?? '',
          latitud: d.latitud ?? null,
          longitud: d.longitud ?? null,
          horarioRegular: d.horarioRegular ?? '',
          notasAcceso: d.notasAcceso ?? '',
          observaciones: d.observaciones ?? '',
          fechaApertura: d.fechaApertura ?? '',
        },
        { emitEvent: false },
      );
      this.setCountryBranch(d.paisId);
      // Cascada territorial en orden, sin disparar reseteos.
      if (d.paisId && !this.isAbroad()) {
        this.regionOptions.set(
          (await firstValueFrom(this.territory.regiones(d.paisId))).map((r) => ({
            label: r.nombre,
            value: r.id,
          })),
        );
        this.provinciaOptions.set(
          (await firstValueFrom(this.territory.provincias(d.regionId ?? undefined, d.paisId))).map(
            (p) => ({ label: p.nombre, value: p.id }),
          ),
        );
      }
      if (d.provinciaId) {
        this.municipioOptions.set(
          (await firstValueFrom(this.territory.municipios(d.provinciaId))).map((m) => ({
            label: m.nombre,
            value: m.id,
          })),
        );
      }
      if (d.municipioId) {
        this.distritoOptions.set(
          (await firstValueFrom(this.territory.distritos(d.municipioId))).map((dm) => ({
            label: dm.nombre,
            value: dm.id,
          })),
        );
      }
      this.form.patchValue(
        {
          regionId: d.regionId ?? '',
          provinciaId: d.provinciaId ?? '',
          municipioId: d.municipioId ?? '',
          distritoMunicipalId: d.distritoMunicipalId ?? '',
        },
        { emitEvent: false },
      );
      await this.loadZonaForEdit(d.paisId, d.zonaHoraria ?? '');
      // Colecciones
      d.servicios.forEach((s) =>
        this.arr('servicios').push(
          this.fb.group({
            servicioId: [s.servicioId, Validators.required],
            habilitado: [s.habilitado],
          }),
        ),
      );
      d.responsables.forEach((r) => this.arr('responsables').push(this.responsableGroup(r)));
      d.documentos.forEach((doc) =>
        this.arr('documentos').push(
          this.fb.group({
            tipoDocumento: [doc.tipoDocumento, Validators.required],
            descripcion: [doc.descripcion ?? ''],
            url: [doc.url, Validators.required],
          }),
        ),
      );
      d.instrumentosLegales.forEach((i) =>
        this.arr('instrumentosLegales').push(
          this.fb.group({
            instrumentoLegalId: [i.instrumentoLegalId, Validators.required],
            proposito: [i.proposito ?? ''],
          }),
        ),
      );
      d.unidadesPresencia.forEach((u) =>
        this.arr('unidadesPresencia').push(
          this.fb.group({
            unidadOrganizativaId: [u.unidadOrganizativaId, Validators.required],
            notas: [u.notas ?? ''],
          }),
        ),
      );
      d.coberturas
        .filter((c) => c.provinciaId)
        .forEach((c) =>
          this.arr('coberturas').push(
            this.fb.group({ provinciaId: [c.provinciaId, Validators.required] }),
          ),
        );
    } catch {
      this.notification.error('No se pudo cargar la localidad para edición');
    }
  }

  // ── Búsqueda de unidades organizativas (servidor) ──
  protected async onUnidadSearch(query: string): Promise<void> {
    const result = await firstValueFrom(this.structureService.searchPaged(query, 1, 20));
    this.unidadOptions.set(
      result.items.map((u) => ({ label: `${u.code} · ${u.name}`, value: u.id })),
    );
  }

  // ── Cascada territorial ──
  private async onPaisChange(paisId: string): Promise<void> {
    this.setCountryBranch(paisId);
    // Cada rama limpia los campos de la otra (RF-LO-18).
    this.form.patchValue(
      {
        regionId: '',
        provinciaId: '',
        municipioId: '',
        distritoMunicipalId: '',
        ...(this.isAbroad() ? {} : { ciudad: '', subdivisionAdministrativa: '' }),
      },
      { emitEvent: false },
    );
    this.regionOptions.set([]);
    this.provinciaOptions.set([]);
    this.municipioOptions.set([]);
    this.distritoOptions.set([]);
    if (!paisId || this.isAbroad()) return;
    const [regiones, provincias] = await Promise.all([
      firstValueFrom(this.territory.regiones(paisId)),
      firstValueFrom(this.territory.provincias(undefined, paisId)),
    ]);
    this.regionOptions.set(regiones.map((r) => ({ label: r.nombre, value: r.id })));
    this.provinciaOptions.set(provincias.map((p) => ({ label: p.nombre, value: p.id })));
  }

  private dominicanRepublicId(): string | undefined {
    return this.paises.find((p) => p.codigo === DOMINICAN_REPUBLIC_CODE)?.id;
  }

  private setCountryBranch(paisId: string): void {
    const abroad = !!paisId && paisId !== this.dominicanRepublicId();
    this.isAbroad.set(abroad);
    const ciudad = this.form.get('ciudad')!;
    ciudad.setValidators(
      abroad ? [Validators.required, Validators.maxLength(ABROAD_TEXT_MAX_LENGTH)] : [],
    );
    ciudad.updateValueAndValidity({ emitEvent: false });
  }

  private async onRegionChange(regionId: string): Promise<void> {
    this.form.patchValue(
      { provinciaId: '', municipioId: '', distritoMunicipalId: '' },
      { emitEvent: false },
    );
    this.municipioOptions.set([]);
    this.distritoOptions.set([]);
    const paisId = this.form.get('paisId')!.value;
    const provincias = await firstValueFrom(
      this.territory.provincias(regionId || undefined, paisId || undefined),
    );
    this.provinciaOptions.set(provincias.map((p) => ({ label: p.nombre, value: p.id })));
  }

  private async onProvinciaChange(provinciaId: string): Promise<void> {
    this.form.patchValue({ municipioId: '', distritoMunicipalId: '' }, { emitEvent: false });
    this.distritoOptions.set([]);
    if (!provinciaId) {
      this.municipioOptions.set([]);
      return;
    }
    const municipios = await firstValueFrom(this.territory.municipios(provinciaId));
    this.municipioOptions.set(municipios.map((m) => ({ label: m.nombre, value: m.id })));
  }

  private async onMunicipioChange(municipioId: string): Promise<void> {
    this.form.patchValue({ distritoMunicipalId: '' }, { emitEvent: false });
    if (!municipioId) {
      this.distritoOptions.set([]);
      return;
    }
    const distritos = await firstValueFrom(this.territory.distritos(municipioId));
    this.distritoOptions.set(distritos.map((d) => ({ label: d.nombre, value: d.id })));
  }

  // ── Zona horaria (RF-LO-20/21) ──
  private async onPaisZonaChange(paisId: string): Promise<void> {
    // Quien no puede cambiar la zona la conserva aunque cambie el país; el servidor decide.
    if (this.zonaReadOnly()) return;
    const options = await this.fetchZonaOptions(paisId);
    if (this.form.get('paisId')!.value !== paisId) return; // respuesta de un país ya descartado
    this.zonaOptions.set(options);

    const control = this.form.get('zonaHoraria')!;
    if (options.length === 1) {
      control.setValue(options[0].value);
    } else if (!options.some((o) => o.value === control.value)) {
      control.setValue('');
    }
  }

  private async loadZonaForEdit(paisId: string, zonaHoraria: string): Promise<void> {
    const options = await this.fetchZonaOptions(paisId);
    // La zona guardada siempre debe poder mostrarse, aunque el catálogo no la liste.
    const missing = zonaHoraria && !options.some((o) => o.value === zonaHoraria);
    this.zonaOptions.set(
      missing ? [{ label: zonaHoraria, value: zonaHoraria }, ...options] : options,
    );
    this.form.patchValue({ zonaHoraria }, { emitEvent: false });
    this.setZonaReadOnly(!this.auth.isCalendarAdmin());
  }

  private async fetchZonaOptions(paisId: string): Promise<SelectOption[]> {
    const paisCodigo = this.paises.find((p) => p.id === paisId)?.codigo;
    if (!paisCodigo) return [];
    try {
      const zonas = await firstValueFrom(this.zonaHorariaService.list(paisCodigo));
      return zonas.map((z) => ({ label: timeZoneOptionLabel(z), value: z.id }));
    } catch {
      this.notification.error('No se pudieron cargar las zonas horarias');
      return [];
    }
  }

  // ── Funciones de servicio exterior (RF-LO-45/46) ──
  protected isFuncionSelected(id: string): boolean {
    return this.selectedFuncionIds().includes(id);
  }

  protected toggleFuncion(id: string): void {
    const selected = this.selectedFuncionIds();
    const control = this.form.get('funcionIds')!;
    control.setValue(selected.includes(id) ? selected.filter((f) => f !== id) : [...selected, id]);
    control.markAsTouched();
  }

  /** Aviso no bloqueante: la estructura de la representación vive en la unidad vinculada. */
  protected get showUnidadVinculadaWarning(): boolean {
    return this.selectedFuncionIds().length > 0 && !this.form.get('unidadVinculadaId')!.value;
  }

  private selectedFuncionIds(): string[] {
    return this.form.get('funcionIds')!.value ?? [];
  }

  private consularSectionError(ids: string[]): { funciones: string } | null {
    const codes = this.funciones()
      .filter((f) => ids.includes(f.id))
      .map((f) => f.codigo);
    return codes.includes(CONSULAR_SECTION_CODE) && !codes.includes(EMBASSY_CODE)
      ? { funciones: CONSULAR_SECTION_ERROR }
      : null;
  }

  private setZonaReadOnly(readOnly: boolean): void {
    const control = this.form.get('zonaHoraria')!;
    if (readOnly) control.disable({ emitEvent: false });
    else control.enable({ emitEvent: false });
    this.zonaReadOnly.set(readOnly);
  }

  // ── Mapa de selección ──
  protected togglePickMap(): void {
    this.showMap.set(!this.showMap());
  }

  private async initPickMap(el: HTMLElement): Promise<void> {
    this.mapInited = true;
    this.mapHandle = await this.mapProvider.init({
      container: el,
      center: RD_CENTER,
      zoom: RD_ZOOM,
      interactive: true,
      showAttribution: false,
    });
    const lat = this.form.get('latitud')!.value as number | null;
    const lng = this.form.get('longitud')!.value as number | null;
    if (lat != null && lng != null) {
      this.mapProvider.setView([lat, lng], 14);
      this.mapProvider.setSingleMarker({ lat, lng });
    }
    this.mapProvider.enablePickMode((p) => {
      this.form.patchValue(
        { latitud: Number(p.lat.toFixed(6)), longitud: Number(p.lng.toFixed(6)) },
        { emitEvent: false },
      );
    });
  }

  private teardownMap(): void {
    this.mapHandle?.destroy();
    this.mapHandle = undefined;
    this.mapInited = false;
  }

  // ── Envío + duplicados ──
  protected onSubmit(): void {
    this.formError.set('');
    clearApiErrors(this.form, FIELD_BY_API_KEY);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.notification.warning('Revise los campos obligatorios');
      return;
    }
    const body = this.buildBody(false);
    void this.checkDuplicatesThenSubmit(body);
  }

  private async checkDuplicatesThenSubmit(body: LocalityUpsert): Promise<void> {
    try {
      const result = await firstValueFrom(
        this.service.validarDuplicado({
          codigo: body.codigo,
          nombreOficial: body.nombreOficial,
          direccionCompleta: body.direccionCompleta,
          latitud: body.latitud,
          longitud: body.longitud,
          provinciaId: body.provinciaId,
          municipioId: body.municipioId,
          paisId: body.paisId,
          ciudad: body.ciudad,
          tipoLocalidadId: body.tipoLocalidadId,
          excludeId: this.localityId() ?? undefined,
        }),
      );
      const nonCode = result.warnings.filter((w) => w.campo !== 'codigo');
      const codeDup = result.warnings.find((w) => w.campo === 'codigo');
      if (codeDup) {
        this.notification.error('Ya existe una localidad con ese código');
        return;
      }
      if (nonCode.length) {
        this.pendingBody = body;
        this.duplicateWarnings.set(nonCode);
        this.isDuplicateOpen.set(true);
        return;
      }
      this.submit(body);
    } catch {
      // Si la verificación previa falla, intentamos guardar igualmente (el backend re-valida).
      this.submit(body);
    }
  }

  protected onDuplicateOverride(justificacion: string): void {
    this.isDuplicateOpen.set(false);
    if (!this.pendingBody) return;
    const body = { ...this.pendingBody, override: true, justificacionDuplicado: justificacion };
    this.submit(body);
  }

  protected onDuplicateCancel(): void {
    this.isDuplicateOpen.set(false);
  }

  private submit(body: LocalityUpsert): void {
    this.isSaving.set(true);
    const id = this.localityId();
    const request$ = id ? this.service.update(id, body) : this.service.create(body);
    request$.subscribe({
      next: () => {
        this.isSaving.set(false);
        this.notification.success(`Localidad ${id ? 'actualizada' : 'creada'} exitosamente`);
        this.saved.emit();
      },
      error: (err) => {
        this.isSaving.set(false);
        this.showSaveError(err);
      },
    });
  }

  private showSaveError(err: unknown): void {
    const error = resolveFormApiError(
      this.form,
      err,
      FIELD_BY_API_KEY,
      'No se pudo guardar la localidad',
    );
    this.formError.set(error.banner);
    if (error.forbidden) this.notification.error(error.forbidden);
  }

  private buildBody(override: boolean): LocalityUpsert {
    const v = this.form.getRawValue();
    const opt = (s: unknown) => (s ? (s as string) : undefined);
    return {
      codigo: v.codigo,
      nombreOficial: v.nombreOficial,
      nombreCorto: v.nombreCorto || undefined,
      sigla: v.sigla || undefined,
      tipoLocalidadId: v.tipoLocalidadId,
      naturalezaAdministrativaId: v.naturalezaAdministrativaId,
      funcionIds: this.showFunciones ? v.funcionIds : undefined,
      unidadResponsableId: opt(v.unidadResponsableId),
      unidadVinculadaId: opt(v.unidadVinculadaId),
      localidadPadreId: opt(v.localidadPadreId),
      paisId: v.paisId,
      regionId: opt(v.regionId),
      provinciaId: opt(v.provinciaId),
      municipioId: opt(v.municipioId),
      distritoMunicipalId: opt(v.distritoMunicipalId),
      ciudad: opt(v.ciudad?.trim()),
      subdivisionAdministrativa: opt(v.subdivisionAdministrativa?.trim()),
      sectorBarrioParaje: v.sectorBarrioParaje || undefined,
      direccionCompleta: v.direccionCompleta,
      edificioPisoLocalReferenciaInterna: v.edificioPisoLocalReferenciaInterna || undefined,
      codigoPostal: v.codigoPostal || undefined,
      zonaHoraria: v.zonaHoraria || undefined,
      latitud: v.latitud != null && v.latitud !== '' ? Number(v.latitud) : undefined,
      longitud: v.longitud != null && v.longitud !== '' ? Number(v.longitud) : undefined,
      horarioRegular: v.horarioRegular || undefined,
      notasAcceso: v.notasAcceso || undefined,
      observaciones: v.observaciones || undefined,
      fechaApertura: v.fechaApertura || undefined,
      motivo: this.isEdit() ? v.motivo : undefined,
      override,
      servicios: (v.servicios as { servicioId: string; habilitado: boolean }[]).map((s) => ({
        servicioId: s.servicioId,
        habilitado: s.habilitado,
      })),
      responsables: (
        v.responsables as {
          responsableId: string;
          rol: string;
          principal: boolean;
          gestionaCalendario: boolean;
        }[]
      ).map((r) => ({
        responsableId: r.responsableId,
        rol: r.rol || undefined,
        principal: r.principal,
        // Solo el administrador lo envía: para el resto el API lo rechaza (RF-LO-38).
        gestionaCalendario: this.isCalendarAdmin ? r.gestionaCalendario : undefined,
      })),
      documentos: (
        v.documentos as { tipoDocumento: string; descripcion: string; url: string }[]
      ).map((d) => ({
        tipoDocumento: d.tipoDocumento,
        descripcion: d.descripcion || undefined,
        url: d.url,
      })),
      instrumentosLegales: (
        v.instrumentosLegales as { instrumentoLegalId: string; proposito: string }[]
      ).map((i) => ({
        instrumentoLegalId: i.instrumentoLegalId,
        proposito: i.proposito || undefined,
      })),
      unidadesPresencia: (
        v.unidadesPresencia as { unidadOrganizativaId: string; notas: string }[]
      ).map((u) => ({ unidadOrganizativaId: u.unidadOrganizativaId, notas: u.notas || undefined })),
      coberturas: (v.coberturas as { provinciaId: string }[]).map((c) => ({
        provinciaId: c.provinciaId,
      })),
    };
  }

  private fieldError(name: string, requiredMessage?: string): string {
    return controlError(this.form.get(name)!, requiredMessage);
  }

  protected get codigoError(): string {
    return this.fieldError('codigo', 'El código es requerido');
  }
  protected get nombreError(): string {
    return this.fieldError('nombreOficial', 'El nombre oficial es requerido');
  }
  protected get funcionesError(): string {
    const control = this.form.get('funcionIds')!;
    return control.getError('api') ?? control.getError('funciones') ?? '';
  }
  protected get direccionError(): string {
    return this.fieldError('direccionCompleta', 'La dirección es requerida');
  }
  protected get paisError(): string {
    return this.fieldError('paisId', 'El país es requerido');
  }
  protected get ciudadError(): string {
    return this.fieldError('ciudad', 'La ciudad es requerida');
  }
  protected get subdivisionError(): string {
    return this.fieldError('subdivisionAdministrativa');
  }
  protected get zonaHorariaError(): string {
    return this.fieldError('zonaHoraria', 'La zona horaria es requerida');
  }
  protected get motivoError(): string {
    return this.fieldError('motivo', 'Indique el motivo del cambio');
  }

  protected onClose(): void {
    this.closed.emit();
  }
}
