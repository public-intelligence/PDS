import { Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CustomModal } from '../../../shared/custom-modal/custom-modal';
import { CustomInput } from '../../../shared/custom-input/custom-input';
import { CustomTextarea } from '../../../shared/custom-textarea/custom-textarea';
import { InstitutionalLocalityService } from '../../../../services/institutional-locality/institutional-locality.service';
import { NotificationService } from '../../../../services/notification/notification.service';

/** Diálogo de suspensión (RF-LO-09) o reactivación de una localidad. */
@Component({
  selector: 'app-locality-suspend-modal',
  imports: [ReactiveFormsModule, CustomModal, CustomInput, CustomTextarea],
  templateUrl: './locality-suspend-modal.component.html',
})
export class LocalitySuspendModalComponent {
  private fb = inject(FormBuilder);
  private service = inject(InstitutionalLocalityService);
  private notification = inject(NotificationService);

  readonly isOpen = input(false);
  readonly mode = input<'suspend' | 'reactivate'>('suspend');
  readonly localityId = input<string | null>(null);
  readonly localityName = input('');

  readonly closed = output<void>();
  readonly done = output<void>();

  protected isSaving = signal(false);
  protected isSuspend = computed(() => this.mode() === 'suspend');
  protected title = computed(() => (this.isSuspend() ? 'Suspender localidad' : 'Reactivar localidad'));

  protected form = this.fb.group({
    motivo: [''],
    fechaInicio: [''],
    fechaFin: [''],
  });

  constructor() {
    effect(() => {
      if (this.isOpen()) {
        this.form.reset();
        const motivo = this.form.controls.motivo;
        if (this.isSuspend()) motivo.setValidators([Validators.required]);
        else motivo.clearValidators();
        motivo.updateValueAndValidity();
      }
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
    const motivo = this.form.controls.motivo.value ?? '';
    const request$ = this.isSuspend()
      ? this.service.suspender(id, {
          motivo,
          fechaInicio: this.form.controls.fechaInicio.value || undefined,
          fechaFin: this.form.controls.fechaFin.value || undefined,
        })
      : this.service.reactivar(id, { motivo: motivo || undefined });

    request$.subscribe({
      next: () => {
        this.isSaving.set(false);
        this.notification.success(this.isSuspend() ? 'Localidad suspendida' : 'Localidad reactivada');
        this.done.emit();
      },
      error: (err) => {
        this.isSaving.set(false);
        this.notification.fromHttpError(err, 'No se pudo completar la acción');
      },
    });
  }

  protected onClose(): void {
    this.closed.emit();
  }
}
