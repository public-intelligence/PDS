import { Component, effect, inject, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { CustomModal } from '../../../shared/custom-modal/custom-modal';
import { CustomForm } from '../../../shared/custom-form/custom-form';
import { CustomTextarea } from '../../../shared/custom-textarea/custom-textarea';
import { VersionWorkflowStepper } from '../../version-workflow-stepper/version-workflow-stepper.component';
import { StructureVersionService } from '../../../../services/structure-version/structure-version.service';
import { NotificationService } from '../../../../services/notification/notification.service';

export type ReviewMode = 'approve' | 'reject';

@Component({
  selector: 'app-version-review-modal',
  imports: [CustomModal, CustomForm, CustomTextarea, VersionWorkflowStepper, ReactiveFormsModule],
  templateUrl: './version-review-modal.component.html',
  host: { class: 'contents' },
})
export class VersionReviewModalComponent {
  private fb = inject(FormBuilder);
  private versionService = inject(StructureVersionService);
  private notification = inject(NotificationService);

  readonly isOpen = input(false);
  readonly mode = input<ReviewMode>('approve');
  readonly versionId = input<string>('');
  readonly versionName = input<string>('');

  readonly closed = output<boolean>();

  protected isSaving = signal(false);

  protected form = this.fb.group({
    comment: [''],
  });

  constructor() {
    effect(() => {
      if (!this.isOpen()) return;
      this.form.reset();
      const commentCtrl = this.form.get('comment');
      if (this.mode() === 'reject') {
        commentCtrl?.setValidators([Validators.required, Validators.minLength(3)]);
      } else {
        commentCtrl?.clearValidators();
      }
      commentCtrl?.updateValueAndValidity();
    });
  }

  protected get isReject(): boolean {
    return this.mode() === 'reject';
  }

  protected get commentError(): string {
    const ctrl = this.form.get('comment');
    if (!ctrl?.invalid || !ctrl.touched) return '';
    if (ctrl.errors?.['required']) return 'Debe indicar el motivo del rechazo';
    if (ctrl.errors?.['minlength']) return 'Mínimo 3 caracteres';
    return '';
  }

  protected close(): void {
    this.form.reset();
    this.closed.emit(false);
  }

  protected async onSubmit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const comment = this.form.value.comment ?? '';
    try {
      this.isSaving.set(true);
      if (this.isReject) {
        await firstValueFrom(this.versionService.reject(this.versionId(), comment));
        this.notification.success('Versión rechazada. Regresó a borrador.');
      } else {
        await firstValueFrom(this.versionService.approve(this.versionId(), comment || undefined));
        this.notification.success('Versión aprobada.');
      }
      this.form.reset();
      this.closed.emit(true);
    } catch {
      this.notification.error('No se pudo procesar la acción.');
    } finally {
      this.isSaving.set(false);
    }
  }
}
