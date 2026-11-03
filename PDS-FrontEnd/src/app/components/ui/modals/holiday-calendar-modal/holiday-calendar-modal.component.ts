import { Component, effect, inject, input, output, signal, untracked } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { CustomModal } from '../../../shared/custom-modal/custom-modal';
import { CustomInput } from '../../../shared/custom-input/custom-input';
import { CustomSelect, SelectOption } from '../../../shared/custom-select/custom-select';
import { CustomSelectSearch } from '../../../shared/custom-select-search/custom-select-search';
import { CustomTextarea } from '../../../shared/custom-textarea/custom-textarea';
import { FormErrorBanner } from '../../../shared/form-error-banner/form-error-banner';
import {
  AmbitoCalendario,
  CalendarioFeriados,
  CalendarioFeriadosUpsert,
} from '../../../../models/holiday-calendar.model';
import { HolidayCalendarService } from '../../../../services/holiday-calendar/holiday-calendar.service';
import { InstitutionalLocalityService } from '../../../../services/institutional-locality/institutional-locality.service';
import { NotificationService } from '../../../../services/notification/notification.service';
import { TerritoryService } from '../../../../services/territory/territory.service';
import { AMBITO_OPTIONS } from '../../../../utils/calendar-labels';
import { controlError, resolveFormApiError } from '../../../../utils/http-error';

const CODE_MAX_LENGTH = 50;
const FIELD_BY_API_KEY = { codigo: 'codigo', nombre: 'nombre' };

/** Crear o editar un calendario de feriados (UI-09, RF-LO-27). Solo administradores. */
@Component({
  selector: 'app-holiday-calendar-modal',
  imports: [
    ReactiveFormsModule,
    CustomModal,
    CustomInput,
    CustomSelect,
    CustomSelectSearch,
    CustomTextarea,
    FormErrorBanner,
  ],
  templateUrl: './holiday-calendar-modal.component.html',
})
export class HolidayCalendarModalComponent {
  private fb = inject(FormBuilder);
  private service = inject(HolidayCalendarService);
  private territory = inject(TerritoryService);
  private localityService = inject(InstitutionalLocalityService);
  private notification = inject(NotificationService);

  readonly isOpen = input(false);
  readonly calendario = input<CalendarioFeriados | null>(null);
  readonly closed = output<void>();

  protected readonly ambitoOptions = AMBITO_OPTIONS;
  protected paisOptions = signal<SelectOption[]>([]);
  protected localidadOptions = signal<SelectOption[]>([]);
  protected isLoading = signal(false);
  protected isSaving = signal(false);
  protected formError = signal('');
  protected isStale = signal(false);

  protected form = this.fb.group({
    codigo: ['', [Validators.required, Validators.maxLength(CODE_MAX_LENGTH)]],
    nombre: ['', [Validators.required]],
    descripcion: [''],
    paisId: [''],
    ambito: ['Nacional' as AmbitoCalendario, [Validators.required]],
    localidadGestoraId: [''],
    activo: [true],
  });

  constructor() {
    effect(() => {
      if (this.isOpen()) untracked(() => void this.onOpen(this.calendario()));
    });
  }

  protected get isEdit(): boolean {
    return !!this.calendario();
  }
  protected get codigoError(): string {
    return controlError(this.form.controls.codigo, 'El código es requerido');
  }
  protected get nombreError(): string {
    return controlError(this.form.controls.nombre, 'El nombre es requerido');
  }

  private async onOpen(calendario: CalendarioFeriados | null): Promise<void> {
    this.formError.set('');
    this.isStale.set(false);
    this.form.reset({ ambito: 'Nacional', activo: true });
    // Las integraciones referencian el código: no cambia después de crear.
    if (calendario) this.form.controls.codigo.disable();
    else this.form.controls.codigo.enable();

    this.isLoading.set(true);
    try {
      const [paises, localidades] = await Promise.all([
        firstValueFrom(this.territory.paises()),
        firstValueFrom(this.localityService.list({ pageSize: 200 })),
      ]);
      this.paisOptions.set(paises.map((p) => ({ label: p.nombre, value: p.id })));
      this.localidadOptions.set(
        localidades.items.map((l) => ({ label: l.nombreOficial, value: l.id })),
      );
      if (calendario) {
        // Lectura fresca: trae la versión vigente para el control de concurrencia (RF-LO-42).
        this.patch(await firstValueFrom(this.service.getById(calendario.id)));
      }
    } catch {
      this.notification.error('No se pudieron cargar los datos del calendario');
    } finally {
      this.isLoading.set(false);
    }
  }

  private patch(c: CalendarioFeriados): void {
    this.form.patchValue({
      codigo: c.codigo,
      nombre: c.nombre,
      descripcion: c.descripcion ?? '',
      paisId: c.paisId ?? '',
      ambito: c.ambito,
      localidadGestoraId: c.localidadGestoraId ?? '',
      activo: c.activo,
    });
  }

  protected onSubmit(): void {
    this.formError.set('');
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    const body: CalendarioFeriadosUpsert = {
      codigo: v.codigo ?? undefined,
      nombre: v.nombre ?? undefined,
      descripcion: v.descripcion || null,
      paisId: v.paisId || null,
      ambito: v.ambito ?? undefined,
      localidadGestoraId: v.localidadGestoraId || null,
      activo: v.activo ?? true,
    };
    this.isSaving.set(true);
    const request$ = this.isEdit
      ? this.service.update(this.calendario()!.id, body)
      : this.service.create(body);
    request$.subscribe({
      next: () => {
        this.isSaving.set(false);
        this.notification.success(`Calendario de feriados ${this.isEdit ? 'actualizado' : 'creado'}`);
        this.closed.emit();
      },
      error: (err) => {
        this.isSaving.set(false);
        const error = resolveFormApiError(
          this.form,
          err,
          FIELD_BY_API_KEY,
          'No se pudo guardar el calendario de feriados',
        );
        this.formError.set(error.banner);
        this.isStale.set(error.stale);
        if (error.forbidden) {
          this.notification.error(error.forbidden);
          this.closed.emit();
        }
      },
    });
  }

  protected onReload(): void {
    void this.onOpen(this.calendario());
  }

  protected onClose(): void {
    this.closed.emit();
  }
}
