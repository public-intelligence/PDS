import { Component, computed, effect, inject, input, output, signal, untracked } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { CustomInput } from '../../../components/shared/custom-input/custom-input';
import { CustomModal } from '../../../components/shared/custom-modal/custom-modal';
import { CustomSelect, SelectOption } from '../../../components/shared/custom-select/custom-select';
import { CustomSelectSearch } from '../../../components/shared/custom-select-search/custom-select-search';
import { CustomTextarea } from '../../../components/shared/custom-textarea/custom-textarea';
import { FormErrorBanner } from '../../../components/shared/form-error-banner/form-error-banner';
import { IntervalsEditor } from '../../../components/shared/intervals-editor/intervals-editor';
import {
  CategoriaExcepcion,
  ExcepcionCalendario,
  ExcepcionInput,
  IntervaloLaboral,
  OrigenExcepcion,
  TipoExcepcion,
} from '../../../models/locality-calendar.model';
import { LocalityCalendarService } from '../../../services/locality-calendar/locality-calendar.service';
import { NotificationService } from '../../../services/notification/notification.service';
import { daysBetween, isBeforeRetroWindow } from '../../../utils/calendar-dates';
import {
  CATEGORIA_EXCEPCION_OPTIONS,
  ORIGEN_EXCEPCION_OPTIONS,
  TIPO_EXCEPCION_HINTS,
  TIPO_EXCEPCION_LABELS,
} from '../../../utils/calendar-labels';
import { apiErrorStatus, controlError, resolveFormApiError } from '../../../utils/http-error';
import { formatIsoDate } from '../../../utils/locality-time';

const MAX_SPAN_DAYS = 366;
const FIELD_BY_API_KEY = {
  fechaInicio: 'fechaInicio',
  fechaFin: 'fechaFin',
  intervalos: 'intervalos',
  motivo: 'motivo',
};

/** Agregar o editar un día no laborable o una excepción de horario (UI-08, RF-LO-31). */
@Component({
  selector: 'app-exception-editor-modal',
  imports: [
    ReactiveFormsModule,
    CustomModal,
    CustomInput,
    CustomSelect,
    CustomSelectSearch,
    CustomTextarea,
    FormErrorBanner,
    IntervalsEditor,
  ],
  templateUrl: './exception-editor-modal.component.html',
})
export class ExceptionEditorModalComponent {
  private fb = inject(FormBuilder);
  private http = inject(HttpClient);
  private service = inject(LocalityCalendarService);
  private notification = inject(NotificationService);

  readonly isOpen = input(false);
  readonly localityId = input.required<string>();
  readonly excepcion = input<ExcepcionCalendario | null>(null);
  /** Fecha propuesta al crear (p. ej. el día elegido en la vista anual). */
  readonly defaultDate = input('');
  /** Excepciones ya cargadas: permiten nombrar la que se solapa (409). */
  readonly existing = input<ExcepcionCalendario[]>([]);
  /** Fecha local de la localidad (RF-LO-23). */
  readonly today = input.required<string>();
  readonly isAdmin = input(false);
  readonly retroWindowDays = input(7);
  readonly closed = output<void>();
  readonly saved = output<void>();
  /** Abrir la excepción que causa el solape. */
  readonly openConflict = output<ExcepcionCalendario>();

  protected readonly tipos: TipoExcepcion[] = ['Cierre', 'JornadaEspecial', 'DiaLaborableAdicional'];
  protected readonly tipoLabels = TIPO_EXCEPCION_LABELS;
  protected readonly tipoHints = TIPO_EXCEPCION_HINTS;
  protected readonly categoriaOptions = CATEGORIA_EXCEPCION_OPTIONS;
  protected readonly origenOptions = ORIGEN_EXCEPCION_OPTIONS;

  protected instrumentoOptions = signal<SelectOption[]>([]);
  protected isSaving = signal(false);
  protected formError = signal('');
  protected isStale = signal(false);
  protected conflict = signal<ExcepcionCalendario | null>(null);

  protected isEdit = computed(() => !!this.excepcion());

  protected form = this.fb.nonNullable.group({
    tipo: ['Cierre' as TipoExcepcion],
    fechaInicio: ['', [Validators.required]],
    fechaFin: ['', [Validators.required]],
    intervalos: [[] as IntervaloLaboral[]],
    categoria: ['DecisionAdministrativa' as CategoriaExcepcion, [Validators.required]],
    origen: ['Institucional' as OrigenExcepcion, [Validators.required]],
    motivo: ['', [Validators.required, Validators.maxLength(1000)]],
    referenciaNormativa: ['', [Validators.maxLength(500)]],
    instrumentoLegalId: [''],
  });

  constructor() {
    effect(() => {
      if (this.isOpen()) untracked(() => this.onOpen(this.excepcion()));
    });
    this.form.controls.tipo.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe((tipo) => this.syncIntervalos(tipo));
    // "Hasta" acompaña a "Desde" mientras no se haya elegido un fin posterior.
    this.form.controls.fechaInicio.valueChanges.pipe(takeUntilDestroyed()).subscribe((desde) => {
      const hasta = this.form.controls.fechaFin;
      if (!hasta.value || hasta.value < desde) hasta.setValue(desde);
    });
  }

  protected get needsIntervals(): boolean {
    return this.form.controls.tipo.value !== 'Cierre';
  }

  /** Reglas de fechas de RF-LO-31 y RF-LO-33 que el cliente puede adelantar. */
  protected get datesError(): string {
    const { fechaInicio, fechaFin } = this.form.getRawValue();
    if (!fechaInicio || !fechaFin) return '';
    if (fechaFin < fechaInicio) return 'La fecha «Hasta» no puede ser anterior a «Desde».';
    if (daysBetween(fechaInicio, fechaFin) > MAX_SPAN_DAYS) {
      return `El rango no puede superar ${MAX_SPAN_DAYS} días.`;
    }
    if (!this.isAdmin() && isBeforeRetroWindow(fechaInicio, this.today(), this.retroWindowDays())) {
      return `Fuera del plazo para cambios retroactivos (${this.retroWindowDays()} días).`;
    }
    return '';
  }
  protected get fechaInicioError(): string {
    return controlError(this.form.controls.fechaInicio, 'Indique la fecha');
  }
  protected get fechaFinError(): string {
    return controlError(this.form.controls.fechaFin, 'Indique la fecha');
  }
  protected get intervalosError(): string {
    return controlError(this.form.controls.intervalos, 'Indique el horario');
  }
  protected get motivoError(): string {
    return controlError(this.form.controls.motivo, 'El motivo es requerido');
  }
  protected get referenciaError(): string {
    return controlError(this.form.controls.referenciaNormativa);
  }

  private onOpen(excepcion: ExcepcionCalendario | null): void {
    this.formError.set('');
    this.isStale.set(false);
    this.conflict.set(null);
    const start = excepcion?.fechaInicio.slice(0, 10) ?? (this.defaultDate() || this.today());
    this.form.reset({
      tipo: excepcion?.tipo ?? 'Cierre',
      fechaInicio: start,
      fechaFin: excepcion?.fechaFin?.slice(0, 10) ?? start,
      intervalos: excepcion?.intervalos ?? [],
      categoria: excepcion?.categoria ?? 'DecisionAdministrativa',
      origen: excepcion?.origen ?? 'Institucional',
      motivo: excepcion?.motivo ?? '',
      referenciaNormativa: excepcion?.referenciaNormativa ?? '',
      instrumentoLegalId: excepcion?.instrumentoLegalId ?? '',
    });
    if (!this.instrumentoOptions().length) void this.loadInstrumentos();
  }

  private async loadInstrumentos(): Promise<void> {
    try {
      const instrumentos = await firstValueFrom(
        this.http.get<{ id: string; type: string; number: string }[]>(
          `${environment.apiUrl}legal-instruments`,
        ),
      );
      this.instrumentoOptions.set(
        instrumentos.map((i) => ({ label: `${i.type} ${i.number}`, value: i.id })),
      );
    } catch {
      /* el instrumento legal es opcional: el formulario sigue siendo utilizable */
    }
  }

  /** Un cierre no lleva horario; los otros dos tipos lo exigen (RF-LO-31). */
  private syncIntervalos(tipo: TipoExcepcion): void {
    const intervalos = this.form.controls.intervalos;
    intervalos.setValidators(tipo === 'Cierre' ? [] : [Validators.required]);
    intervalos.updateValueAndValidity({ emitEvent: false });
  }

  protected selectTipo(tipo: TipoExcepcion): void {
    this.form.controls.tipo.setValue(tipo);
  }

  protected onSubmit(): void {
    this.formError.set('');
    this.conflict.set(null);
    this.form.markAllAsTouched();
    if (this.form.invalid || this.datesError) return;

    const v = this.form.getRawValue();
    const body: ExcepcionInput = {
      fechaInicio: v.fechaInicio,
      fechaFin: v.fechaFin,
      tipo: v.tipo,
      categoria: v.categoria,
      origen: v.origen,
      motivo: v.motivo.trim(),
      referenciaNormativa: v.referenciaNormativa || undefined,
      instrumentoLegalId: v.instrumentoLegalId || undefined,
      intervalos: this.needsIntervals ? v.intervalos : null,
    };
    const excepcion = this.excepcion();
    this.isSaving.set(true);
    const request$ = excepcion
      ? this.service.actualizarExcepcion(this.localityId(), excepcion.id, body)
      : this.service.crearExcepcion(this.localityId(), body);
    request$.subscribe({
      next: () => {
        this.isSaving.set(false);
        this.notification.success(
          excepcion ? 'Día no laborable actualizado' : 'Día no laborable agregado',
        );
        this.saved.emit();
      },
      error: (err) => {
        this.isSaving.set(false);
        this.showSaveError(err, body);
      },
    });
  }

  private showSaveError(err: unknown, body: ExcepcionInput): void {
    const error = resolveFormApiError(
      this.form,
      err,
      FIELD_BY_API_KEY,
      'No se pudo guardar el día no laborable',
    );
    this.isStale.set(error.stale);
    const overlapping = apiErrorStatus(err) === 409 ? this.findOverlap(body) : undefined;
    this.conflict.set(overlapping ?? null);
    this.formError.set(overlapping ? this.overlapMessage(overlapping) : error.banner);
    if (error.forbidden) {
      this.notification.error(error.forbidden);
      this.saved.emit();
    }
  }

  /** Excepción de usuario ya cargada cuyas fechas se cruzan con las enviadas. */
  private findOverlap(body: ExcepcionInput): ExcepcionCalendario | undefined {
    return this.existing().find(
      (e) =>
        !e.esSistema &&
        e.id !== this.excepcion()?.id &&
        e.fechaInicio.slice(0, 10) <= body.fechaFin &&
        (e.fechaFin ?? e.fechaInicio).slice(0, 10) >= body.fechaInicio,
    );
  }

  private overlapMessage(e: ExcepcionCalendario): string {
    const dates = `${formatIsoDate(e.fechaInicio)} y ${formatIsoDate(e.fechaFin ?? e.fechaInicio)}`;
    return `Ya existe un día no laborable entre ${dates} (${e.motivo}). Edítelo o elija otras fechas.`;
  }

  protected onClose(): void {
    this.closed.emit();
  }
}
