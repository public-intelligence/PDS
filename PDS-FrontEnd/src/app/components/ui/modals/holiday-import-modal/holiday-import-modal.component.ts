import { Component, ViewChild, computed, effect, inject, input, output, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { CustomModal } from '../../../shared/custom-modal/custom-modal';
import { FormErrorBanner } from '../../../shared/form-error-banner/form-error-banner';
import { ConfirmationModalComponent } from '../confirmation-modal/confirmation-modal.component';
import { ImportacionResultado, ModoImportacion } from '../../../../models/holiday-calendar.model';
import { HolidayCalendarService } from '../../../../services/holiday-calendar/holiday-calendar.service';
import { NotificationService } from '../../../../services/notification/notification.service';
import { extractApiErrors } from '../../../../utils/http-error';

const YEARS_AHEAD = 2;

/** Importar feriados desde CSV o JSON, con validación previa obligatoria (UI-12, RF-LO-29). */
@Component({
  selector: 'app-holiday-import-modal',
  imports: [CustomModal, FormErrorBanner, ConfirmationModalComponent],
  templateUrl: './holiday-import-modal.component.html',
})
export class HolidayImportModalComponent {
  @ViewChild(ConfirmationModalComponent) confirmationModal!: ConfirmationModalComponent;
  private service = inject(HolidayCalendarService);
  private notification = inject(NotificationService);

  readonly isOpen = input(false);
  readonly calendarioId = input.required<string>();
  /** Año que se está viendo en el detalle: el propuesto para «Reemplazar el año». */
  readonly year = input(new Date().getFullYear());
  readonly closed = output<void>();
  readonly imported = output<void>();

  protected mode = signal<ModoImportacion>('agregar');
  protected replaceYear = signal(new Date().getFullYear());
  protected content = signal('');
  protected result = signal<ImportacionResultado | null>(null);
  protected isBusy = signal(false);
  protected formError = signal('');

  protected yearOptions = computed(() =>
    Array.from({ length: YEARS_AHEAD + 2 }, (_, i) => this.year() - 1 + i),
  );
  /** «Importar» solo se habilita tras validar sin errores el contenido actual. */
  protected canImport = computed(() => this.result()?.errores.length === 0 && !this.isBusy());

  constructor() {
    effect(() => {
      if (!this.isOpen()) return;
      this.mode.set('agregar');
      this.replaceYear.set(this.year());
      this.content.set('');
      this.result.set(null);
      this.formError.set('');
    });
  }

  protected setMode(mode: ModoImportacion): void {
    this.mode.set(mode);
    this.result.set(null);
  }

  protected setReplaceYear(event: Event): void {
    this.replaceYear.set(Number((event.target as HTMLSelectElement).value));
    this.result.set(null);
  }

  protected setContent(event: Event): void {
    this.content.set((event.target as HTMLTextAreaElement).value);
    this.result.set(null);
  }

  protected async onFile(event: Event): Promise<void> {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    this.content.set(await file.text());
    this.result.set(null);
  }

  protected validate(): void {
    void this.send(true);
  }

  protected onImport(): void {
    if (this.mode() !== 'reemplazarAnio') {
      void this.send(false);
      return;
    }
    void this.confirmReplace();
  }

  /** Reemplazar un año borra sus feriados: se confirma con la cantidad afectada. */
  private async confirmReplace(): Promise<void> {
    const year = this.replaceYear();
    const existing = await firstValueFrom(this.service.dias(this.calendarioId(), year)).catch(
      () => [],
    );
    this.confirmationModal.openModal({
      title: 'Reemplazar el año',
      name: String(year),
      isDelete: true,
      paragraph: `Se reemplazarán los ${existing.length} feriados de`,
      warning: 'Los plazos de las solicitudes en curso se recalcularán.',
      confirmButton: 'Reemplazar',
      onConfirm: () => {
        this.confirmationModal.closeModal();
        void this.send(false);
      },
    });
  }

  private async send(validarSolamente: boolean): Promise<void> {
    const content = this.content().trim();
    if (!content) {
      this.formError.set('Pegue los feriados o elija un archivo.');
      return;
    }
    this.formError.set('');
    this.isBusy.set(true);
    try {
      const result = await firstValueFrom(
        this.service.importar(this.calendarioId(), content, !content.startsWith('['), {
          modo: this.mode(),
          anio: this.mode() === 'reemplazarAnio' ? this.replaceYear() : undefined,
          validarSolamente,
        }),
      );
      this.result.set(result);
      if (!validarSolamente && result.errores.length === 0) {
        this.notification.success(
          `Feriados importados: ${result.creados} nuevos, ${result.actualizados} actualizados.`,
        );
        this.imported.emit();
      }
    } catch (err) {
      this.result.set(null);
      this.formError.set(extractApiErrors(err, 'No se pudieron procesar los feriados'));
    } finally {
      this.isBusy.set(false);
    }
  }

  protected onClose(): void {
    this.closed.emit();
  }
}
