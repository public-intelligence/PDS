import { Component, effect, inject, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CustomModal } from '../../../shared/custom-modal/custom-modal';
import { CustomInput } from '../../../shared/custom-input/custom-input';
import { AreaTypeService } from '../../../../services/area-type/area-type.service';
import { AreaType, AreaTypeUpsert } from '../../../../models/area-type';
import { NotificationService } from '../../../../services/notification/notification.service';
import { SelectOption, CustomSelect } from '../../../shared/custom-select/custom-select';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-area-type-modal',
  imports: [CustomModal, CustomInput, ReactiveFormsModule, CustomSelect],
  templateUrl: './area-type-modal.component.html',
})
export class AreaTypeModalComponent {
  private fb = inject(FormBuilder);
  private areaTypeService = inject(AreaTypeService);

  readonly isOpen = input(false);
  readonly areaType = input<AreaType | null>(null);
  readonly closed = output<void>();

  protected isSaving = signal(false);
  protected areaTypes = signal<SelectOption[]>([]);

  private notification = inject(NotificationService);

  protected form = this.fb.group({
    name: ['', [Validators.required]],
    order: [null as number | null, [Validators.required]],
    parentId: [''],
  });

  constructor() {
    effect(() => {
      const item = this.areaType();
      if (this.isOpen()) {
        this.getAreaTypes();
        this.form.reset();
        if (item) {
          this.form.patchValue({
            name: item.name,
            order: item.order ?? null,
            parentId: item.parentId ?? null,
          });
        }
      }
    });
  }

  protected get isEditMode(): boolean {
    return !!this.areaType();
  }

  protected get nameError(): string {
    const ctrl = this.form.controls.name;
    if (ctrl.touched && ctrl.hasError('required')) return 'El nombre es requerido';
    return '';
  }

  protected get orderError(): string {
    const ctrl = this.form.controls.order;
    if (ctrl.touched && ctrl.hasError('required')) return 'El orden es requerido';
    return '';
  }

  async getAreaTypes() {
    try {
      const result = await firstValueFrom(this.areaTypeService.getAll());
      this.areaTypes.set(result.items.map((area) => ({ label: area.name, value: area.id })));
    } catch {
      this.notification.error('Error al obtener los tipos de áreas');
    }
  }

  protected onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const body: AreaTypeUpsert = {
      name: this.form.controls.name.value ?? undefined,
      order: this.form.controls.order.value ?? undefined,
      parentId: this.form.controls.parentId.value ?? undefined,
    };
    this.isSaving.set(true);
    console.log(body);
    const request$ = this.isEditMode
      ? this.areaTypeService.update(this.areaType()!.id, body)
      : this.areaTypeService.create(body);
    this.isSaving.set(false);
    request$.subscribe({
      next: () => {
        this.onClose();
        this.notification.success(
          `Tipo de área ${this.isEditMode ? 'actualizado' : 'creado'} exitosamente`,
        );
      },
      error: () => {
        this.notification.error('Error al guardar el tipo de área');
        this.isSaving.set(false);
      },
    });
  }

  protected onClose(): void {
    this.closed.emit();
  }
}
