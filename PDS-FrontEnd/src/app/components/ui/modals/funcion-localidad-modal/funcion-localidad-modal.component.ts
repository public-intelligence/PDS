import { Component, effect, inject, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CustomModal } from '../../../shared/custom-modal/custom-modal';
import { CustomInput } from '../../../shared/custom-input/custom-input';
import { CustomSelect } from '../../../shared/custom-select/custom-select';
import { CustomTextarea } from '../../../shared/custom-textarea/custom-textarea';
import { NotificationService } from '../../../../services/notification/notification.service';
import { FuncionLocalidadService } from '../../../../services/locality-catalog/locality-catalog.services';
import {
  CategoriaFuncion,
  FuncionLocalidad,
  FuncionLocalidadUpsert,
} from '../../../../models/locality-catalog.model';
import { CATEGORIA_FUNCION_OPTIONS } from '../../../../utils/calendar-labels';

/** Crear o editar una función de localidad (UI-13, RF-LO-45). */
@Component({
  selector: 'app-funcion-localidad-modal',
  imports: [ReactiveFormsModule, CustomModal, CustomInput, CustomSelect, CustomTextarea],
  templateUrl: './funcion-localidad-modal.component.html',
})
export class FuncionLocalidadModalComponent {
  private fb = inject(FormBuilder);
  private service = inject(FuncionLocalidadService);
  private notification = inject(NotificationService);

  readonly isOpen = input(false);
  readonly funcion = input<FuncionLocalidad | null>(null);
  readonly closed = output<void>();

  protected readonly categoriaOptions = CATEGORIA_FUNCION_OPTIONS;
  protected isSaving = signal(false);
  protected form = this.fb.group({
    codigo: ['', [Validators.required]],
    nombre: ['', [Validators.required]],
    categoria: ['' as CategoriaFuncion | '', [Validators.required]],
    descripcion: [''],
  });

  constructor() {
    effect(() => {
      if (!this.isOpen()) return;
      this.form.reset();
      const f = this.funcion();
      if (f) {
        this.form.patchValue({
          codigo: f.codigo,
          nombre: f.nombre,
          categoria: f.categoria,
          descripcion: f.descripcion ?? '',
        });
      }
      // Las integraciones filtran por código: no cambia después de crear.
      if (f) this.form.controls.codigo.disable();
      else this.form.controls.codigo.enable();
    });
  }

  protected get isEdit(): boolean {
    return !!this.funcion();
  }
  protected get codigoError(): string {
    const c = this.form.controls.codigo;
    return c.touched && c.hasError('required') ? 'El código es requerido' : '';
  }
  protected get nombreError(): string {
    const c = this.form.controls.nombre;
    return c.touched && c.hasError('required') ? 'El nombre es requerido' : '';
  }
  protected get categoriaError(): string {
    const c = this.form.controls.categoria;
    return c.touched && c.hasError('required') ? 'La categoría es requerida' : '';
  }

  protected onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    const body: FuncionLocalidadUpsert = {
      codigo: v.codigo ?? undefined,
      nombre: v.nombre ?? undefined,
      categoria: v.categoria || undefined,
      descripcion: v.descripcion || undefined,
    };
    this.isSaving.set(true);
    const req$ = this.isEdit
      ? this.service.update(this.funcion()!.id, body)
      : this.service.create(body);
    req$.subscribe({
      next: () => {
        this.isSaving.set(false);
        this.notification.success(`Función ${this.isEdit ? 'actualizada' : 'creada'}`);
        this.closed.emit();
      },
      error: (err) => {
        this.isSaving.set(false);
        this.notification.fromHttpError(err, 'Error al guardar la función de localidad');
      },
    });
  }

  protected onClose(): void {
    this.closed.emit();
  }
}
