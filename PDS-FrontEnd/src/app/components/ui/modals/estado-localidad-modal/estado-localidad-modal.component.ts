import { Component, effect, inject, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CustomModal } from '../../../shared/custom-modal/custom-modal';
import { CustomInput } from '../../../shared/custom-input/custom-input';
import { CustomTextarea } from '../../../shared/custom-textarea/custom-textarea';
import { NotificationService } from '../../../../services/notification/notification.service';
import { EstadoLocalidadService } from '../../../../services/locality-catalog/locality-catalog.services';
import { EstadoLocalidad, EstadoLocalidadUpsert } from '../../../../models/locality-catalog.model';

@Component({
  selector: 'app-estado-localidad-modal',
  imports: [ReactiveFormsModule, CustomModal, CustomInput, CustomTextarea],
  templateUrl: './estado-localidad-modal.component.html',
})
export class EstadoLocalidadModalComponent {
  private fb = inject(FormBuilder);
  private service = inject(EstadoLocalidadService);
  private notification = inject(NotificationService);

  readonly isOpen = input(false);
  readonly estado = input<EstadoLocalidad | null>(null);
  readonly closed = output<void>();

  protected isSaving = signal(false);
  protected form = this.fb.group({
    codigo: ['', [Validators.required]],
    nombre: ['', [Validators.required]],
    descripcion: [''],
    color: [''],
    esTerminal: [false],
  });

  constructor() {
    effect(() => {
      if (this.isOpen()) {
        this.form.reset({ esTerminal: false });
        const e = this.estado();
        if (e)
          this.form.patchValue({
            codigo: e.codigo,
            nombre: e.nombre,
            descripcion: e.descripcion ?? '',
            color: e.color ?? '',
            esTerminal: e.esTerminal,
          });
      }
    });
  }

  protected get isEdit(): boolean {
    return !!this.estado();
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
    const body: EstadoLocalidadUpsert = {
      codigo: this.form.controls.codigo.value ?? undefined,
      nombre: this.form.controls.nombre.value ?? undefined,
      descripcion: this.form.controls.descripcion.value || undefined,
      color: this.form.controls.color.value || undefined,
      esTerminal: this.form.controls.esTerminal.value ?? false,
    };
    this.isSaving.set(true);
    const req$ = this.isEdit ? this.service.update(this.estado()!.id, body) : this.service.create(body);
    req$.subscribe({
      next: () => {
        this.isSaving.set(false);
        this.notification.success(`Estado ${this.isEdit ? 'actualizado' : 'creado'}`);
        this.closed.emit();
      },
      error: (err) => {
        this.isSaving.set(false);
        this.notification.fromHttpError(err, 'Error al guardar el estado de localidad');
      },
    });
  }

  protected onClose(): void {
    this.closed.emit();
  }
}
