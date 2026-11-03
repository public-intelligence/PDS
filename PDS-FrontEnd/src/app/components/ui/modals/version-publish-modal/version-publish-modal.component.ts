import { Component, effect, inject, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { CustomModal } from '../../../shared/custom-modal/custom-modal';
import { CustomForm } from '../../../shared/custom-form/custom-form';
import { CustomInput } from '../../../shared/custom-input/custom-input';
import { CustomSelect, SelectOption } from '../../../shared/custom-select/custom-select';
import { VersionWorkflowStepper } from '../../version-workflow-stepper/version-workflow-stepper.component';
import { StructureVersionService } from '../../../../services/structure-version/structure-version.service';
import { NotificationService } from '../../../../services/notification/notification.service';
import { LEGAL_INSTRUMENT_TYPE_LABELS, LegalInstrumentType } from '../../../../models/structure-version.model';

@Component({
  selector: 'app-version-publish-modal',
  imports: [CustomModal, CustomForm, CustomInput, CustomSelect, VersionWorkflowStepper, ReactiveFormsModule],
  templateUrl: './version-publish-modal.component.html',
  host: { class: 'contents' },
})
export class VersionPublishModalComponent {
  private fb = inject(FormBuilder);
  private versionService = inject(StructureVersionService);
  private notification = inject(NotificationService);

  readonly isOpen = input(false);
  readonly versionId = input<string>('');
  readonly versionName = input<string>('');

  readonly closed = output<boolean>();

  protected isSaving = signal(false);

  protected readonly typeOptions: SelectOption[] = (
    Object.keys(LEGAL_INSTRUMENT_TYPE_LABELS) as LegalInstrumentType[]
  ).map((key) => ({ value: key, label: LEGAL_INSTRUMENT_TYPE_LABELS[key] }));

  protected form = this.fb.group({
    type: ['Decree', Validators.required],
    number: ['', Validators.required],
    instrumentDate: ['', Validators.required],
    issuingAuthority: ['', Validators.required],
    documentUrl: [''],
    effectiveFrom: ['', Validators.required],
    isPublic: [true],
  });

  constructor() {
    effect(() => {
      if (this.isOpen()) this.form.reset({ type: 'Decree', isPublic: true });
    });
  }

  protected error(control: string, label: string): string {
    const ctrl = this.form.get(control);
    if (!ctrl?.invalid || !ctrl.touched) return '';
    return `${label} es requerido`;
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
    const v = this.form.value;
    try {
      this.isSaving.set(true);
      await firstValueFrom(
        this.versionService.publish(this.versionId(), {
          effectiveFrom: v.effectiveFrom!,
          isPublic: !!v.isPublic,
          legalInstrument: {
            type: v.type as LegalInstrumentType,
            number: v.number!,
            date: v.instrumentDate!,
            issuingAuthority: v.issuingAuthority!,
            documentUrl: v.documentUrl || null,
          },
        }),
      );
      this.notification.success('Versión publicada correctamente.');
      this.form.reset();
      this.closed.emit(true);
    } catch (e) {
      this.notification.fromHttpError(e, 'No se pudo publicar la versión. Verifique la fecha de vigencia y el instrumento legal.');
    } finally {
      this.isSaving.set(false);
    }
  }
}
