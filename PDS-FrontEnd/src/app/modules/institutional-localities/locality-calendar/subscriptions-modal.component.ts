import {
  Component,
  ViewChild,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
  untracked,
} from '@angular/core';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';

import { CustomInput } from '../../../components/shared/custom-input/custom-input';
import { CustomModal } from '../../../components/shared/custom-modal/custom-modal';
import { SelectOption } from '../../../components/shared/custom-select/custom-select';
import { CustomSelectSearch } from '../../../components/shared/custom-select-search/custom-select-search';
import { FormErrorBanner } from '../../../components/shared/form-error-banner/form-error-banner';
import { ConfirmationModalComponent } from '../../../components/ui/modals/confirmation-modal/confirmation-modal.component';
import { CalendarioFeriados } from '../../../models/holiday-calendar.model';
import { SuscripcionCalendario } from '../../../models/locality-calendar.model';
import { HolidayCalendarService } from '../../../services/holiday-calendar/holiday-calendar.service';
import { LocalityCalendarService } from '../../../services/locality-calendar/locality-calendar.service';
import { NotificationService } from '../../../services/notification/notification.service';
import { resolveFormApiError } from '../../../utils/http-error';

/** Calendarios de feriados de una localidad: guardar reemplaza el conjunto completo (UI-07). */
@Component({
  selector: 'app-subscriptions-modal',
  imports: [
    ReactiveFormsModule,
    CustomModal,
    CustomInput,
    CustomSelectSearch,
    FormErrorBanner,
    ConfirmationModalComponent,
  ],
  templateUrl: './subscriptions-modal.component.html',
})
export class SubscriptionsModalComponent {
  @ViewChild(ConfirmationModalComponent) confirmationModal!: ConfirmationModalComponent;
  private fb = inject(FormBuilder);
  private service = inject(LocalityCalendarService);
  private holidayService = inject(HolidayCalendarService);
  private notification = inject(NotificationService);

  readonly isOpen = input(false);
  readonly localityId = input.required<string>();
  readonly localityName = input('');
  /** País de la localidad: sus calendarios se ofrecen primero. */
  readonly paisId = input<string | null>(null);
  readonly suscripciones = input<SuscripcionCalendario[]>([]);
  readonly closed = output<void>();
  readonly saved = output<void>();

  protected isLoading = signal(false);
  protected isSaving = signal(false);
  protected formError = signal('');
  protected isStale = signal(false);

  private calendarios = signal<CalendarioFeriados[]>([]);
  /** Ids ya suscritos; señal aparte porque el FormArray no es reactivo para `computed`. */
  private subscribedIds = signal<string[]>([]);

  protected form = this.fb.group({
    agregarId: [''],
    filas: this.fb.array([] as FormGroup[]),
  });

  /** Calendarios activos aún no suscritos, con los del país de la localidad primero. */
  protected availableOptions = computed<SelectOption[]>(() => {
    const ownCountry = (c: CalendarioFeriados) => (c.paisId && c.paisId === this.paisId() ? 0 : 1);
    return this.calendarios()
      .filter((c) => c.activo && !this.subscribedIds().includes(c.id))
      .sort((a, b) => ownCountry(a) - ownCountry(b) || a.codigo.localeCompare(b.codigo))
      .map((c) => ({ label: `${c.codigo} · ${c.nombre}`, value: c.id }));
  });

  constructor() {
    effect(() => {
      if (this.isOpen()) untracked(() => void this.onOpen(this.suscripciones()));
    });
  }

  protected get rows(): FormGroup[] {
    return this.rowsArray.controls as FormGroup[];
  }
  private get rowsArray(): FormArray {
    return this.form.controls.filas;
  }

  private async onOpen(suscripciones: SuscripcionCalendario[]): Promise<void> {
    this.formError.set('');
    this.isStale.set(false);
    this.rowsArray.clear();
    suscripciones.forEach((s) =>
      this.addRow(s.calendarioFeriadosId, `${s.codigo} · ${s.nombre}`, s.vigenteDesde, s.vigenteHasta),
    );

    this.isLoading.set(true);
    try {
      this.calendarios.set((await firstValueFrom(this.holidayService.getAll(1, 200))).items);
    } catch {
      this.notification.error('No se pudieron cargar los calendarios de feriados');
    } finally {
      this.isLoading.set(false);
    }
  }

  private addRow(id: string, label: string, desde?: string | null, hasta?: string | null): void {
    this.rowsArray.push(
      this.fb.group({
        calendarioFeriadosId: [id],
        label: [label],
        vigenteDesde: [desde?.slice(0, 10) ?? ''],
        vigenteHasta: [hasta?.slice(0, 10) ?? ''],
      }),
    );
    this.syncSubscribedIds();
  }

  protected onAdd(): void {
    const id = this.form.controls.agregarId.value;
    const option = this.availableOptions().find((o) => o.value === id);
    if (!id || !option) return;
    this.addRow(id, option.label);
    this.form.controls.agregarId.setValue('');
  }

  protected removeAt(index: number): void {
    this.rowsArray.removeAt(index);
    this.syncSubscribedIds();
  }

  private syncSubscribedIds(): void {
    this.subscribedIds.set(this.rows.map((row) => row.value.calendarioFeriadosId));
  }

  /** Quitar una suscripción puede mover plazos: se confirma antes de guardar (§12.2). */
  protected onSubmit(): void {
    const removed = this.suscripciones().filter(
      (s) => !this.subscribedIds().includes(s.calendarioFeriadosId),
    );
    if (!removed.length) {
      this.save();
      return;
    }
    this.confirmationModal.openModal({
      title: 'Quitar calendarios de feriados',
      name: removed.map((s) => s.codigo).join(', '),
      isDelete: true,
      paragraph: 'Está seguro que desea quitar de esta localidad',
      warning: 'Los plazos de las solicitudes en curso se recalcularán.',
      confirmButton: 'Quitar y guardar',
      onConfirm: () => {
        this.confirmationModal.closeModal();
        this.save();
      },
    });
  }

  private save(): void {
    this.formError.set('');
    const body = this.rows.map((row) => ({
      calendarioFeriadosId: row.value.calendarioFeriadosId as string,
      vigenteDesde: (row.value.vigenteDesde as string) || null,
      vigenteHasta: (row.value.vigenteHasta as string) || null,
    }));
    this.isSaving.set(true);
    this.service.reemplazarSuscripciones(this.localityId(), body).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.notification.success('Calendarios de feriados actualizados');
        this.saved.emit();
      },
      error: (err) => {
        this.isSaving.set(false);
        const error = resolveFormApiError(
          this.form,
          err,
          {},
          'No se pudieron guardar los calendarios de feriados',
        );
        this.formError.set(error.banner);
        this.isStale.set(error.stale);
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
