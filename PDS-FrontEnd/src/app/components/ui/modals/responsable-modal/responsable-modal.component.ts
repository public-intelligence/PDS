import { Component, effect, inject, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CustomModal } from '../../../shared/custom-modal/custom-modal';
import { CustomInput } from '../../../shared/custom-input/custom-input';
import { NotificationService } from '../../../../services/notification/notification.service';
import { ResponsableService } from '../../../../services/locality-catalog/locality-catalog.services';
import { Responsable, ResponsableUpsert } from '../../../../models/locality-catalog.model';
import { AuthService } from '../../../../services/auth/auth.service';
import { apiErrorStatus, controlError, resolveFormApiError } from '../../../../utils/http-error';

const GUID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
const GOBID_TAKEN_MESSAGE = 'Ese usuario GobID ya está vinculado a otro responsable.';

@Component({
  selector: 'app-responsable-modal',
  imports: [ReactiveFormsModule, CustomModal, CustomInput],
  templateUrl: './responsable-modal.component.html',
})
export class ResponsableModalComponent {
  private fb = inject(FormBuilder);
  private service = inject(ResponsableService);
  private notification = inject(NotificationService);
  private auth = inject(AuthService);

  readonly isOpen = input(false);
  readonly responsable = input<Responsable | null>(null);
  readonly closed = output<void>();

  protected isSaving = signal(false);
  /** El vínculo con GobID da acceso a calendarios: solo lo ven y cambian los administradores. */
  protected canLinkGobId = signal(false);
  protected form = this.fb.group({
    nombre: ['', [Validators.required]],
    cargo: [''],
    email: ['', [Validators.email]],
    telefono: [''],
    gobidUserId: ['', [Validators.pattern(GUID_PATTERN)]],
  });

  constructor() {
    effect(() => {
      if (this.isOpen()) {
        this.form.reset();
        this.canLinkGobId.set(this.auth.isCalendarAdmin());
        const r = this.responsable();
        if (r) {
          this.form.patchValue({
            nombre: r.nombre,
            cargo: r.cargo ?? '',
            email: r.email ?? '',
            telefono: r.telefono ?? '',
            gobidUserId: r.id ?? '',
          });
        }
      }
    });
  }

  protected get isEdit(): boolean {
    return !!this.responsable();
  }
  protected get nombreError(): string {
    const c = this.form.controls.nombre;
    return c.touched && c.hasError('required') ? 'El nombre es requerido' : '';
  }
  protected get emailError(): string {
    const c = this.form.controls.email;
    return c.touched && c.hasError('email') ? 'Correo inválido' : '';
  }

  protected get gobidUserIdError(): string {
    const c = this.form.controls.gobidUserId;
    if (c.touched && c.hasError('pattern')) return 'Ingrese un identificador de usuario GobID válido.';
    return controlError(c);
  }

  protected onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const body: ResponsableUpsert = {
      nombre: this.form.controls.nombre.value ?? undefined,
      cargo: this.form.controls.cargo.value || undefined,
      email: this.form.controls.email.value || undefined,
      telefono: this.form.controls.telefono.value || undefined,
      // Quien no es administrador no envía el campo: el API lo rechazaría (RF-LO-37).
      ...(this.canLinkGobId() ? { gobidUserId: this.form.controls.gobidUserId.value || null } : {}),
    };
    this.isSaving.set(true);
    const req$ = this.isEdit ? this.service.update(this.responsable()!.id, body) : this.service.create(body);
    req$.subscribe({
      next: () => {
        this.isSaving.set(false);
        this.notification.success(`Responsable ${this.isEdit ? 'actualizado' : 'creado'}`);
        this.closed.emit();
      },
      error: (err) => {
        this.isSaving.set(false);
        const fallback = 'Error al guardar el responsable';
        const error = resolveFormApiError(this.form, err, { gobidUserId: 'gobidUserId' }, fallback);
        const message = apiErrorStatus(err) === 409 ? GOBID_TAKEN_MESSAGE : error.forbidden;
        this.notification.error(message || error.banner || 'Revise los campos marcados');
      },
    });
  }

  protected onClose(): void {
    this.closed.emit();
  }
}
