import { Component, effect, inject, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CustomModal } from '../../../shared/custom-modal/custom-modal';
import { CustomInput } from '../../../shared/custom-input/custom-input';
import { CustomTextarea } from '../../../shared/custom-textarea/custom-textarea';
import { NotificationService } from '../../../../services/notification/notification.service';
import { NaturalezaAdministrativaService } from '../../../../services/locality-catalog/locality-catalog.services';
import {
  NaturalezaAdministrativa,
  NaturalezaAdministrativaUpsert,
} from '../../../../models/locality-catalog.model';

@Component({
  selector: 'app-naturaleza-administrativa-modal',
  imports: [ReactiveFormsModule, CustomModal, CustomInput, CustomTextarea],
  templateUrl: './naturaleza-administrativa-modal.component.html',
})
export class NaturalezaAdministrativaModalComponent {
  private fb = inject(FormBuilder);
  private service = inject(NaturalezaAdministrativaService);
  private notification = inject(NotificationService);

  readonly isOpen = input(false);
  readonly naturaleza = input<NaturalezaAdministrativa | null>(null);
  readonly closed = output<void>();

  protected isSaving = signal(false);
  protected form = this.fb.group({
    codigo: ['', [Validators.required]],
    nombre: ['', [Validators.required]],
    descripcion: [''],
  });

  constructor() {
    effect(() => {
      if (this.isOpen()) {
        this.form.reset();
        const n = this.naturaleza();
        if (n) this.form.patchValue({ codigo: n.codigo, nombre: n.nombre, descripcion: n.descripcion ?? '' });
      }
    });
  }

  protected get isEdit(): boolean {
    return !!this.naturaleza();
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
    const body: NaturalezaAdministrativaUpsert = {
      codigo: this.form.controls.codigo.value ?? undefined,
      nombre: this.form.controls.nombre.value ?? undefined,
      descripcion: this.form.controls.descripcion.value || undefined,
    };
    this.isSaving.set(true);
    const req$ = this.isEdit ? this.service.update(this.naturaleza()!.id, body) : this.service.create(body);
    req$.subscribe({
      next: () => {
        this.isSaving.set(false);
        this.notification.success(`Naturaleza ${this.isEdit ? 'actualizada' : 'creada'}`);
        this.closed.emit();
      },
      error: (err) => {
        this.isSaving.set(false);
        this.notification.fromHttpError(err, 'Error al guardar la naturaleza administrativa');
      },
    });
  }

  protected onClose(): void {
    this.closed.emit();
  }
}
