import { Component, effect, inject, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CustomModal } from '../../../shared/custom-modal/custom-modal';
import { CustomInput } from '../../../shared/custom-input/custom-input';
import { OccupationalGroupService } from '../../../../services/occupational-group/occupational-group.service';
import { OccupationalGroup } from '../../../../models/occupational-group.model';
import { NotificationService } from '../../../../services/notification/notification.service';

@Component({
  selector: 'app-occupational-group-modal',
  imports: [CustomModal, CustomInput, ReactiveFormsModule],
  templateUrl: './occupational-group-modal.component.html',
})
export class OccupationalGroupModalComponent {
  private fb = inject(FormBuilder);
  private occupationalGroupService = inject(OccupationalGroupService);
  private notification = inject(NotificationService);

  readonly isOpen = input(false);
  readonly occupationalGroup = input<OccupationalGroup | null>(null);
  readonly closed = output<void>();

  protected isSaving = signal(false);

  protected form = this.fb.group({
    name: ['', [Validators.required]],
  });

  constructor() {
    effect(() => {
      const item = this.occupationalGroup();
      if (this.isOpen()) {
        this.form.reset();
        if (item) {
          this.form.patchValue({ name: item.name });
        }
      }
    });
  }

  protected get isEditMode(): boolean {
    return !!this.occupationalGroup();
  }

  protected get nameError(): string {
    const ctrl = this.form.controls.name;
    if (ctrl.touched && ctrl.hasError('required')) return 'El nombre es requerido';
    return '';
  }

  protected onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const body = { name: this.form.controls.name.value! };

    this.isSaving.set(true);
    const request$ = this.isEditMode
      ? this.occupationalGroupService.update(this.occupationalGroup()!.id, body)
      : this.occupationalGroupService.create(body);

    request$.subscribe({
      next: () => {
        this.isSaving.set(false);
        this.onClose();
        this.notification.success(
          `Grupo ocupacional ${this.isEditMode ? 'actualizado' : 'creado'} exitosamente`,
        );
      },
      error: () => {
        this.notification.error('Error al guardar el grupo ocupacional');
        this.isSaving.set(false);
      },
    });
  }

  protected onClose(): void {
    this.closed.emit();
  }
}
