import { Component, effect, inject, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CustomModal } from '../../../shared/custom-modal/custom-modal';
import { CustomInput } from '../../../shared/custom-input/custom-input';
import { CustomTextarea } from '../../../shared/custom-textarea/custom-textarea';
import { NotificationService } from '../../../../services/notification/notification.service';
import { TipoLocalidadService } from '../../../../services/locality-catalog/locality-catalog.services';
import { TipoLocalidad, TipoLocalidadUpsert } from '../../../../models/locality-catalog.model';

@Component({
  selector: 'app-tipo-localidad-modal',
  imports: [ReactiveFormsModule, CustomModal, CustomInput, CustomTextarea],
  templateUrl: './tipo-localidad-modal.component.html',
})
export class TipoLocalidadModalComponent {
  private fb = inject(FormBuilder);
  private service = inject(TipoLocalidadService);
  private notification = inject(NotificationService);

  readonly isOpen = input(false);
  readonly tipo = input<TipoLocalidad | null>(null);
  readonly closed = output<void>();

  protected isSaving = signal(false);
  protected form = this.fb.group({
    codigo: ['', [Validators.required]],
    nombre: ['', [Validators.required]],
    descripcion: [''],
    color: [''],
  });

  constructor() {
    effect(() => {
      if (this.isOpen()) {
        this.form.reset();
        const t = this.tipo();
        if (t) this.form.patchValue({ codigo: t.codigo, nombre: t.nombre, descripcion: t.descripcion ?? '', color: t.color ?? '' });
      }
    });
  }

  protected get isEdit(): boolean {
    return !!this.tipo();
  }
  protected get codigoError(): string {
    const c = this.form.controls.codigo;
    return c.touched && c.hasError('required') ? 'El código es requerido' : '';
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
    const body: TipoLocalidadUpsert = {
      codigo: this.form.controls.codigo.value ?? undefined,
      nombre: this.form.controls.nombre.value ?? undefined,
      descripcion: this.form.controls.descripcion.value || undefined,
      color: this.form.controls.color.value || undefined,
    };
    this.isSaving.set(true);
    const req$ = this.isEdit ? this.service.update(this.tipo()!.id, body) : this.service.create(body);
    req$.subscribe({
      next: () => {
        this.isSaving.set(false);
        this.notification.success(`Tipo ${this.isEdit ? 'actualizado' : 'creado'}`);
        this.closed.emit();
      },
      error: (err) => {
        this.isSaving.set(false);
        this.notification.fromHttpError(err, 'Error al guardar el tipo de localidad');
      },
    });
  }

  protected onClose(): void {
    this.closed.emit();
  }
}
