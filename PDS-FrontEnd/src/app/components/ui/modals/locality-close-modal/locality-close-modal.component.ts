import { Component, effect, inject, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CustomModal } from '../../../shared/custom-modal/custom-modal';
import { CustomInput } from '../../../shared/custom-input/custom-input';
import { CustomTextarea } from '../../../shared/custom-textarea/custom-textarea';
import { InstitutionalLocalityService } from '../../../../services/institutional-locality/institutional-locality.service';
import { NotificationService } from '../../../../services/notification/notification.service';

/** Diálogo de cierre / desactivación (RF-LO-10). No elimina datos históricos. */
@Component({
  selector: 'app-locality-close-modal',
  imports: [ReactiveFormsModule, CustomModal, CustomInput, CustomTextarea],
  templateUrl: './locality-close-modal.component.html',
})
export class LocalityCloseModalComponent {
  private fb = inject(FormBuilder);
  private service = inject(InstitutionalLocalityService);
  private notification = inject(NotificationService);

  readonly isOpen = input(false);
  readonly localityId = input<string | null>(null);
  readonly localityName = input('');

  readonly closed = output<void>();
  readonly done = output<void>();

  protected isSaving = signal(false);

  protected form = this.fb.group({
    motivo: ['', [Validators.required]],
    cierreFecha: [''],
    serviciosAfectados: [''],
  });

  constructor() {
    effect(() => {
      if (this.isOpen()) this.form.reset();
    });
  }

  protected get motivoError(): string {
    const c = this.form.controls.motivo;
    return c.touched && c.hasError('required') ? 'El motivo es requerido' : '';
  }

  protected onSubmit(): void {
    const id = this.localityId();
    if (!id) return;
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.isSaving.set(true);
    this.service
      .cerrar(id, {
        motivo: this.form.controls.motivo.value ?? '',
        cierreFecha: this.form.controls.cierreFecha.value || undefined,
        serviciosAfectados: this.form.controls.serviciosAfectados.value || undefined,
      })
      .subscribe({
        next: () => {
          this.isSaving.set(false);
          this.notification.success('Localidad cerrada');
          this.done.emit();
        },
        error: (err) => {
          this.isSaving.set(false);
          this.notification.fromHttpError(err, 'No se pudo cerrar la localidad');
        },
      });
  }

  protected onClose(): void {
    this.closed.emit();
  }
}
