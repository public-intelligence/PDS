import { Component, effect, inject, input, output, signal, untracked } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CustomModal } from '../../../shared/custom-modal/custom-modal';
import { CustomInput } from '../../../shared/custom-input/custom-input';
import { CustomTextarea } from '../../../shared/custom-textarea/custom-textarea';
import { FormErrorBanner } from '../../../shared/form-error-banner/form-error-banner';
import { IntervalsEditor } from '../../../shared/intervals-editor/intervals-editor';
import { AlcanceFeriado, Feriado, FeriadoInput } from '../../../../models/holiday-calendar.model';
import { IntervaloLaboral } from '../../../../models/locality-calendar.model';
import { HolidayCalendarService } from '../../../../services/holiday-calendar/holiday-calendar.service';
import { NotificationService } from '../../../../services/notification/notification.service';
import { ALCANCE_LABELS } from '../../../../utils/calendar-labels';
import { apiErrorStatus, controlError, resolveFormApiError } from '../../../../utils/http-error';
import { isBeforeRetroWindow } from '../../../../utils/calendar-dates';
import { formatIsoDate } from '../../../../utils/locality-time';

const FIELD_BY_API_KEY = { fecha: 'fecha', nombre: 'nombre', intervalos: 'intervalos' };

/** Agregar o editar un feriado de un calendario (UI-11, RF-LO-28). */
@Component({
  selector: 'app-holiday-entry-modal',
  imports: [
    ReactiveFormsModule,
    CustomModal,
    CustomInput,
    CustomTextarea,
    FormErrorBanner,
    IntervalsEditor,
  ],
  templateUrl: './holiday-entry-modal.component.html',
})
export class HolidayEntryModalComponent {
  private fb = inject(FormBuilder);
  private service = inject(HolidayCalendarService);
  private notification = inject(NotificationService);

  readonly isOpen = input(false);
  readonly calendarioId = input.required<string>();
  readonly feriado = input<Feriado | null>(null);
  /** Fecha sugerida al crear (primer día del año que se está viendo). */
  readonly defaultDate = input('');
  /** Ventana retroactiva de quien no es administrador (RF-LO-33). */
  readonly today = input('');
  readonly isAdmin = input(false);
  readonly retroWindowDays = input(7);
  readonly closed = output<void>();
  readonly saved = output<void>();

  protected readonly alcances: AlcanceFeriado[] = ['DiaCompleto', 'JornadaReducida'];
  protected readonly alcanceLabels = ALCANCE_LABELS;
  protected isSaving = signal(false);
  protected formError = signal('');
  protected isStale = signal(false);

  protected form = this.fb.group({
    fecha: ['', [Validators.required]],
    nombre: ['', [Validators.required, Validators.maxLength(250)]],
    alcance: ['DiaCompleto' as AlcanceFeriado],
    intervalos: [[] as IntervaloLaboral[]],
    referenciaNormativa: ['', [Validators.maxLength(500)]],
    notas: ['', [Validators.maxLength(1000)]],
  });

  constructor() {
    effect(() => {
      if (this.isOpen()) untracked(() => this.onOpen(this.feriado()));
    });
    this.form.controls.alcance.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe((alcance) => this.syncIntervalos(alcance));
  }

  protected get isEdit(): boolean {
    return !!this.feriado();
  }
  protected get isReducida(): boolean {
    return this.form.controls.alcance.value === 'JornadaReducida';
  }
  protected get fechaError(): string {
    return (
      controlError(this.form.controls.fecha, 'La fecha es requerida') || this.retroWindowError
    );
  }
  private get retroWindowError(): string {
    const fecha = this.form.controls.fecha.value;
    const tooOld =
      !!fecha && !!this.today() && isBeforeRetroWindow(fecha, this.today(), this.retroWindowDays());
    return !this.isAdmin() && tooOld
      ? `Fuera del plazo para cambios retroactivos (${this.retroWindowDays()} días).`
      : '';
  }
  protected get nombreError(): string {
    return controlError(this.form.controls.nombre, 'El nombre es requerido');
  }
  protected get intervalosError(): string {
    return controlError(this.form.controls.intervalos, 'Indique el horario que se trabaja');
  }
  protected get referenciaError(): string {
    return controlError(this.form.controls.referenciaNormativa);
  }
  protected get notasError(): string {
    return controlError(this.form.controls.notas);
  }

  private onOpen(feriado: Feriado | null): void {
    this.formError.set('');
    this.isStale.set(false);
    this.form.reset({
      fecha: feriado?.fecha.slice(0, 10) ?? this.defaultDate(),
      nombre: feriado?.nombre ?? '',
      alcance: feriado?.alcance ?? 'DiaCompleto',
      intervalos: feriado?.intervalos ?? [],
      referenciaNormativa: feriado?.referenciaNormativa ?? '',
      notas: feriado?.notas ?? '',
    });
  }

  /** El horario solo aplica, y entonces es obligatorio, en una jornada reducida (RF-LO-28). */
  private syncIntervalos(alcance: AlcanceFeriado | null): void {
    const intervalos = this.form.controls.intervalos;
    intervalos.setValidators(alcance === 'JornadaReducida' ? [Validators.required] : []);
    intervalos.updateValueAndValidity({ emitEvent: false });
  }

  protected onSubmit(): void {
    this.formError.set('');
    if (this.form.invalid || this.retroWindowError) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    const body: FeriadoInput = {
      fecha: v.fecha!,
      nombre: v.nombre!.trim(),
      alcance: v.alcance!,
      intervalos: this.isReducida ? v.intervalos : null,
      referenciaNormativa: v.referenciaNormativa || undefined,
      notas: v.notas || undefined,
    };
    this.isSaving.set(true);
    const feriado = this.feriado();
    const request$ = feriado
      ? this.service.actualizarDia(this.calendarioId(), feriado.id, body)
      : this.service.crearDia(this.calendarioId(), body);
    request$.subscribe({
      next: () => {
        this.isSaving.set(false);
        this.notification.success(feriado ? 'Feriado actualizado' : 'Feriado agregado');
        this.saved.emit();
      },
      error: (err) => {
        this.isSaving.set(false);
        this.showSaveError(err, body.fecha);
      },
    });
  }

  private showSaveError(err: unknown, fecha: string): void {
    const error = resolveFormApiError(this.form, err, FIELD_BY_API_KEY, 'No se pudo guardar el feriado');
    this.isStale.set(error.stale);
    this.formError.set(error.banner);
    if (apiErrorStatus(err) === 409) {
      // El duplicado se nombra con la fecha en conflicto (UI-11).
      this.form.controls.fecha.setErrors({
        api: `Ya existe un feriado el ${formatIsoDate(fecha)} en este calendario.`,
      });
    }
    if (error.forbidden) {
      this.notification.error(error.forbidden);
      this.saved.emit();
    }
  }

  protected onClose(): void {
    this.closed.emit();
  }
}
