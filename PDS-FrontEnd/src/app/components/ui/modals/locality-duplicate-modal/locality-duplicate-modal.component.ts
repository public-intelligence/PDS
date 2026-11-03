import { Component, effect, inject, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CustomModal } from '../../../shared/custom-modal/custom-modal';
import { CustomTextarea } from '../../../shared/custom-textarea/custom-textarea';
import { DuplicateWarning } from '../../../../models/institutional-locality.model';

/** Aviso de posibles duplicados (RF-LO-13). No bloquea: permite continuar con justificación. */
@Component({
  selector: 'app-locality-duplicate-modal',
  imports: [ReactiveFormsModule, RouterLink, CustomModal, CustomTextarea],
  templateUrl: './locality-duplicate-modal.component.html',
})
export class LocalityDuplicateModalComponent {
  private fb = inject(FormBuilder);

  readonly isOpen = input(false);
  readonly warnings = input<DuplicateWarning[]>([]);

  readonly cancel = output<void>();
  readonly override = output<string>();

  protected isSaving = signal(false);

  protected form = this.fb.group({
    justificacion: ['', [Validators.required]],
  });

  constructor() {
    effect(() => {
      if (this.isOpen()) this.form.reset();
    });
  }

  protected get justificacionError(): string {
    const c = this.form.controls.justificacion;
    return c.touched && c.hasError('required') ? 'Debe justificar para continuar' : '';
  }

  protected onConfirm(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.override.emit(this.form.controls.justificacion.value ?? '');
  }

  protected onCancel(): void {
    this.cancel.emit();
  }
}
