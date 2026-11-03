import { Component, effect, inject, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CustomModal } from '../../../shared/custom-modal/custom-modal';
import { CustomInput } from '../../../shared/custom-input/custom-input';
import { CustomTextarea } from '../../../shared/custom-textarea/custom-textarea';
import { NotificationService } from '../../../../services/notification/notification.service';
import { ServicioService } from '../../../../services/locality-catalog/locality-catalog.services';
import { Servicio, ServicioUpsert } from '../../../../models/locality-catalog.model';

@Component({
  selector: 'app-servicio-modal',
  imports: [ReactiveFormsModule, CustomModal, CustomInput, CustomTextarea],
  templateUrl: './servicio-modal.component.html',
})
export class ServicioModalComponent {
  private fb = inject(FormBuilder);
  private service = inject(ServicioService);
  private notification = inject(NotificationService);

  readonly isOpen = input(false);
  readonly servicio = input<Servicio | null>(null);
  readonly closed = output<void>();

  protected isSaving = signal(false);
  protected form = this.fb.group({
    nombre: ['', [Validators.required]],
    codigo: [''],
    descripcion: [''],
  });

  constructor() {
    effect(() => {
      if (this.isOpen()) {
        this.form.reset();
        const s = this.servicio();
        if (s) this.form.patchValue({ nombre: s.nombre, codigo: s.codigo ?? '', descripcion: s.descripcion ?? '' });
      }
    });
  }

  protected get isEdit(): boolean {
    return !!this.servicio();
  }
  protected get nombreError(): string {
    const c = this.form.controls.nombre;
    return c.touched && c.hasError('required') ? 'El nombre es requerido' : '';
  }

  protected onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const body: ServicioUpsert = {
      nombre: this.form.controls.nombre.value ?? undefined,
      codigo: this.form.controls.codigo.value || undefined,
      descripcion: this.form.controls.descripcion.value || undefined,
    };
    this.isSaving.set(true);
    const req$ = this.isEdit ? this.service.update(this.servicio()!.id, body) : this.service.create(body);
    req$.subscribe({
      next: () => {
        this.isSaving.set(false);
        this.notification.success(`Servicio ${this.isEdit ? 'actualizado' : 'creado'}`);
        this.closed.emit();
      },
      error: (err) => {
        this.isSaving.set(false);
        this.notification.fromHttpError(err, 'Error al guardar el servicio');
      },
    });
  }

  protected onClose(): void {
    this.closed.emit();
  }
}
