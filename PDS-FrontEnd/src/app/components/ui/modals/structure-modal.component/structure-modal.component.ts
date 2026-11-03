import { Component, computed, effect, inject, input, OnInit, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CustomForm } from '../../../shared/custom-form/custom-form';
import { CustomInput } from '../../../shared/custom-input/custom-input';
import { CustomModal } from '../../../shared/custom-modal/custom-modal';
import { CustomSelect, SelectOption } from '../../../shared/custom-select/custom-select';
import { firstValueFrom } from 'rxjs';
import { StructureService } from '../../../../services/structure/structure.service';
import { AreaTypeService } from '../../../../services/area-type/area-type.service';
import { AreaType } from '../../../../models/area-type';
import { NotificationService } from '../../../../services/notification/notification.service';

export interface StructureModalConfig {
  isEdit: boolean;
  id?: string;
  name?: string;
  code?: string;
  parentId?: string | null;
  parentName?: string;
  parentCode?: string;
  areaTypeId?: string;
  parentAreaTypeId?: string;
}

@Component({
  selector: 'app-structure-modal',
  imports: [CustomModal, CustomForm, CustomInput, CustomSelect, ReactiveFormsModule],
  templateUrl: './structure-modal.component.html',
  host: { class: 'contents' },
})
export class StructureModalComponent implements OnInit {
  private fb = inject(FormBuilder);
  private structureService = inject(StructureService);
  private areaTypeService = inject(AreaTypeService);
  private notificator = inject(NotificationService);

  readonly isOpen = input(false);
  readonly config = input<StructureModalConfig>({ isEdit: false });

  readonly closed = output<void>();

  protected isLoading = signal(false);
  protected isSaving = signal(false);

  protected form = this.fb.group({
    name: ['', [Validators.required, Validators.minLength(3)]],
    areaTypeId: ['', Validators.required],
    code: [''],
  });

  private allAreaTypes = signal<AreaType[]>([]);

  protected areaTypeOptions = computed<SelectOption[]>(() => {
    const types = this.allAreaTypes();
    const parentAreaTypeId = this.config().parentAreaTypeId;

    if (!parentAreaTypeId) return types.map((t) => ({ label: t.name, value: t.id }));

    const parentOrder = types.find((t) => t.id === parentAreaTypeId)?.order;

    if (parentOrder == null) return types.map((t) => ({ label: t.name, value: t.id }));

    return types
      .filter((t) => t.order != null && t.order > parentOrder)
      .map((t) => ({ label: t.name, value: t.id }));
  });

  constructor() {
    effect(() => {
      if (!this.isOpen()) return;
      this.form.reset();
      const cfg = this.config();
      if (cfg.isEdit) {
        this.form.patchValue({
          name: cfg.name ?? '',
          areaTypeId: cfg.areaTypeId ?? '',
          code: cfg.code ?? '',
        });
      }
    });
  }

  ngOnInit(): void {
    this.getAreaType();
  }

  protected get nameError(): string {
    const ctrl = this.form.get('name');
    if (!ctrl?.invalid || !ctrl.touched) return '';
    if (ctrl.errors?.['required']) return 'El nombre es requerido';
    if (ctrl.errors?.['minlength']) return 'Mínimo 3 caracteres';
    return '';
  }

  protected get areaTypeError(): string {
    const ctrl = this.form.get('areaTypeId');
    if (!ctrl?.invalid || !ctrl.touched) return '';
    return 'Seleccione un tipo de área';
  }

  protected closeModal(): void {
    this.form.reset();
    this.closed.emit();
  }

  protected async onSubmit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const cfg = this.config();
    try {
      this.isSaving.set(true);
      if (cfg.isEdit) {
        await firstValueFrom(
          this.structureService.updateStructure(cfg.id || '', {
            name: this.form.value.name!,
            areaTypeId: this.form.value.areaTypeId!,
            parentId: cfg.parentId ?? null,
          }),
        );
      } else {
        await firstValueFrom(
          this.structureService.createStructure({
            name: this.form.value.name!,
            areaTypeId: this.form.value.areaTypeId!,
            parentId: cfg.parentId ?? null,
          }),
        );
      }
      this.notificator.success('Estructura guardada correctamente');
      this.closeModal();
    } catch (error) {
      console.error('Error al guardar la estructura', error);
      this.notificator.fromHttpError(error, 'Error al guardar la estructura');
    } finally {
      this.isSaving.set(false);
    }
  }

  private getAreaType() {
    this.isLoading.set(true);
    firstValueFrom(this.areaTypeService.getAll())
      .then((result) => this.allAreaTypes.set(result.items))
      .catch(() => console.error('Error al cargar los tipos de área'))
      .finally(() => this.isLoading.set(false));
  }
}
