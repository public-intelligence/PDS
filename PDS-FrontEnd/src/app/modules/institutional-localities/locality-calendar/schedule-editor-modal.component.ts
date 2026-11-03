import { Component, effect, inject, input, output, signal, untracked } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { firstValueFrom } from 'rxjs';

import { CustomInput } from '../../../components/shared/custom-input/custom-input';
import { CustomModal } from '../../../components/shared/custom-modal/custom-modal';
import { SelectOption } from '../../../components/shared/custom-select/custom-select';
import { CustomSelectSearch } from '../../../components/shared/custom-select-search/custom-select-search';
import { CustomTextarea } from '../../../components/shared/custom-textarea/custom-textarea';
import { FormErrorBanner } from '../../../components/shared/form-error-banner/form-error-banner';
import { IntervalsEditor } from '../../../components/shared/intervals-editor/intervals-editor';
import {
  DIAS_SEMANA,
  DiaSemana,
  IntervaloLaboral,
  JornadaSemanal,
} from '../../../models/locality-calendar.model';
import { InstitutionalLocalityService } from '../../../services/institutional-locality/institutional-locality.service';
import { LocalityCalendarService } from '../../../services/locality-calendar/locality-calendar.service';
import { NotificationService } from '../../../services/notification/notification.service';
import { controlError, resolveFormApiError } from '../../../utils/http-error';
import { formatIsoDate } from '../../../utils/locality-time';
import {
  DAY_INITIAL,
  DAY_LONG,
  SCHEDULE_PRESETS,
  normalizeWeek,
  presetWeek,
  scheduleError,
  scheduleTimeline,
  summarizeSchedule,
  weekFrom,
} from '../../../utils/schedule';

const FIELD_BY_API_KEY = { vigenteDesde: 'vigenteDesde', motivo: 'motivo' };
const COPY_PRESET = 'copiar';

/** Editor del horario semanal: crea una versión nueva, nunca reescribe las vigentes (UI-06). */
@Component({
  selector: 'app-schedule-editor-modal',
  imports: [
    ReactiveFormsModule,
    CustomModal,
    CustomInput,
    CustomSelectSearch,
    CustomTextarea,
    FormErrorBanner,
    IntervalsEditor,
  ],
  templateUrl: './schedule-editor-modal.component.html',
})
export class ScheduleEditorModalComponent {
  private fb = inject(FormBuilder);
  private service = inject(LocalityCalendarService);
  private localityService = inject(InstitutionalLocalityService);
  private notification = inject(NotificationService);

  readonly isOpen = input(false);
  readonly localityId = input.required<string>();
  /** Horario vigente: punto de partida del editor. */
  readonly currentWeek = input<JornadaSemanal | null>(null);
  /** Fecha local de la localidad (RF-LO-23). */
  readonly today = input.required<string>();
  /** Solo el administrador puede fechar el horario en el pasado (RF-LO-25). */
  readonly isAdmin = input(false);
  readonly closed = output<void>();
  readonly saved = output<void>();

  protected readonly days = DIAS_SEMANA;
  protected readonly dayInitial = DAY_INITIAL;
  protected readonly dayLong = DAY_LONG;
  protected readonly presets = SCHEDULE_PRESETS;
  protected readonly copyPreset = COPY_PRESET;

  protected activePreset = signal('');
  protected localidadOptions = signal<SelectOption[]>([]);
  protected isSaving = signal(false);
  protected formError = signal('');

  protected form = this.fb.nonNullable.group({
    vigenteDesde: ['', [Validators.required]],
    motivo: ['', [Validators.maxLength(1000)]],
    copiarDeId: [''],
    dias: [[] as DiaSemana[]],
    horario: [[] as IntervaloLaboral[]],
    personalizar: [false],
    porDia: this.fb.nonNullable.group({
      mon: [[] as IntervaloLaboral[]],
      tue: [[] as IntervaloLaboral[]],
      wed: [[] as IntervaloLaboral[]],
      thu: [[] as IntervaloLaboral[]],
      fri: [[] as IntervaloLaboral[]],
      sat: [[] as IntervaloLaboral[]],
      sun: [[] as IntervaloLaboral[]],
    }),
  });

  constructor() {
    effect(() => {
      if (this.isOpen()) untracked(() => this.onOpen());
    });
    this.form.controls.vigenteDesde.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.syncMotivoRequired());
    this.form.controls.copiarDeId.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe((id) => void this.copyFrom(id));
    // Al personalizar, cada día parte del horario común que tenía.
    this.form.controls.personalizar.valueChanges.pipe(takeUntilDestroyed()).subscribe((on) => {
      const { dias, horario } = this.form.getRawValue();
      if (on) this.form.controls.porDia.setValue(weekFrom(dias, horario));
    });
  }

  // ── Estado derivado ──
  protected get isBackdated(): boolean {
    const date = this.form.controls.vigenteDesde.value;
    return !!date && date < this.today();
  }

  /** Semana que se guardará: la personalizada o el horario común en los días elegidos. */
  protected get week(): JornadaSemanal {
    const v = this.form.getRawValue();
    return v.personalizar ? v.porDia : weekFrom(v.dias, v.horario);
  }
  protected get summary(): string {
    return summarizeSchedule(this.week);
  }
  protected get weekError(): string {
    return scheduleError(this.week) ?? '';
  }
  protected get vigenteDesdeError(): string {
    const control = this.form.controls.vigenteDesde;
    if (this.isBackdated && !this.isAdmin()) return 'La fecha debe ser hoy o posterior.';
    return controlError(control, 'Indique desde cuándo rige el horario');
  }
  protected get motivoError(): string {
    return controlError(this.form.controls.motivo, 'Indique el motivo del cambio retroactivo');
  }

  protected isDaySelected(day: DiaSemana): boolean {
    return this.form.controls.dias.value.includes(day);
  }

  // ── Edición ──
  private onOpen(): void {
    this.formError.set('');
    this.activePreset.set('');
    this.form.reset({ vigenteDesde: this.today() });
    this.loadWeek(this.currentWeek() ?? presetWeek(SCHEDULE_PRESETS[0].days));
  }

  /** Vuelca una semana al editor; usa el modo simple si todos los días abiertos comparten horario. */
  private loadWeek(source: JornadaSemanal): void {
    const week = normalizeWeek(source);
    const openDays = DIAS_SEMANA.filter((day) => week[day].length > 0);
    const hours = openDays.length ? week[openDays[0]] : [];
    const sameHours = openDays.every((day) => JSON.stringify(week[day]) === JSON.stringify(hours));
    this.form.patchValue(
      { dias: openDays, horario: hours, porDia: week, personalizar: !sameHours },
      { emitEvent: false },
    );
  }

  protected applyPreset(id: string, days: readonly DiaSemana[]): void {
    this.activePreset.set(id);
    this.loadWeek(presetWeek(days));
  }

  protected chooseCopy(): void {
    this.activePreset.set(COPY_PRESET);
    if (!this.localidadOptions().length) void this.loadLocalidades();
  }

  private async loadLocalidades(): Promise<void> {
    try {
      const { items } = await firstValueFrom(this.localityService.list({ pageSize: 200 }));
      this.localidadOptions.set(
        items
          .filter((l) => l.id !== this.localityId())
          .map((l) => ({ label: l.nombreOficial, value: l.id })),
      );
    } catch {
      this.notification.error('No se pudieron cargar las localidades');
    }
  }

  private async copyFrom(localityId: string): Promise<void> {
    if (!localityId) return;
    try {
      const calendario = await firstValueFrom(this.service.calendario(localityId));
      const source = scheduleTimeline(calendario.jornadas, this.today()).current;
      if (source) this.loadWeek(source.dias);
      else this.notification.warning('Esa localidad no tiene un horario vigente');
    } catch {
      this.notification.error('No se pudo copiar el horario de esa localidad');
    }
  }

  protected toggleDay(day: DiaSemana): void {
    const selected = this.form.controls.dias.value;
    this.form.controls.dias.setValue(
      selected.includes(day) ? selected.filter((d) => d !== day) : [...selected, day],
    );
  }

  /** Fechar en el pasado recalcula plazos ya transcurridos: exige motivo (RF-LO-25). */
  private syncMotivoRequired(): void {
    const motivo = this.form.controls.motivo;
    motivo.setValidators(
      this.isBackdated
        ? [Validators.required, Validators.maxLength(1000)]
        : [Validators.maxLength(1000)],
    );
    motivo.updateValueAndValidity({ emitEvent: false });
  }

  // ── Guardado ──
  protected onSubmit(): void {
    this.formError.set('');
    this.form.markAllAsTouched();
    if (this.form.invalid || this.weekError || this.vigenteDesdeError) return;

    const { vigenteDesde, motivo } = this.form.getRawValue();
    this.isSaving.set(true);
    this.service
      .crearJornada(this.localityId(), {
        vigenteDesde,
        motivo: motivo.trim() || undefined,
        dias: this.week,
      })
      .subscribe({
        next: () => {
          this.isSaving.set(false);
          this.notification.success(
            vigenteDesde > this.today()
              ? `Horario programado desde ${formatIsoDate(vigenteDesde)}`
              : 'Horario actualizado',
          );
          this.saved.emit();
        },
        error: (err) => {
          this.isSaving.set(false);
          const error = resolveFormApiError(
            this.form,
            err,
            FIELD_BY_API_KEY,
            'No se pudo guardar el horario',
          );
          this.formError.set(error.banner);
          if (error.forbidden) {
            this.notification.error(error.forbidden);
            this.saved.emit();
          }
        },
      });
  }

  protected onClose(): void {
    this.closed.emit();
  }
}
