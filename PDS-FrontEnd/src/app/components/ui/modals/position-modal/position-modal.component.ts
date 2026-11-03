import { Component, effect, inject, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CustomModal } from '../../../shared/custom-modal/custom-modal';
import { CustomInput } from '../../../shared/custom-input/custom-input';
import { CustomTextarea } from '../../../shared/custom-textarea/custom-textarea';
import { PositionService } from '../../../../services/position/position.service';
import { Position, PositionUpsert } from '../../../../models/position.model';
import { NotificationService } from '../../../../services/notification/notification.service';
import { CustomSelect, SelectOption } from '../../../shared/custom-select/custom-select';
import { OccupationalGroupService } from '../../../../services/occupational-group/occupational-group.service';
import { firstValueFrom } from 'rxjs';
import { StructureService } from '../../../../services/structure/structure.service';
import { CustomSelectSearch } from '../../../shared/custom-select-search/custom-select-search';

@Component({
  selector: 'app-position-modal',
  imports: [
    CustomModal,
    CustomInput,
    CustomTextarea,
    ReactiveFormsModule,
    CustomSelect,
    CustomSelectSearch,
  ],
  templateUrl: './position-modal.component.html',
})
export class PositionModalComponent {
  private fb = inject(FormBuilder);
  private positionService = inject(PositionService);
  private notification = inject(NotificationService);
  private occupationalGroupService = inject(OccupationalGroupService);
  private structureService = inject(StructureService);

  readonly isOpen = input(false);
  readonly position = input<Position | null>(null);
  readonly closed = output<void>();

  protected isSaving = signal(false);
  protected occupationalGroups = signal<SelectOption[]>([]);
  protected organizationalStructures = signal<SelectOption[]>([]);

  protected form = this.fb.group({
    name: ['', [Validators.required]],
    minSalary: ['', [Validators.required]],
    maxSalary: ['', [Validators.required]],
    isManager: [false],
    competencies: [''],
    occupationalGroupId: ['', Validators.required],
    yearsOfExperienceRequired: ['', [Validators.required]],
    organizationalStructureId: ['', Validators.required],
    natureOfWork: [''],
    typicalTasks: [''],
    requiredEducation: [''],
    characteristics: [''],
    otherDesirableRequirements: [''],
    probatoryPeriod: [''],
    punctuation: [''],
    grade: [''],
    code: [''],
  });

  constructor() {
    effect(() => {
      const item = this.position();
      if (this.isOpen()) {
        this.form.reset();
        this.getOccupationalGroupId();
        this.getOrganizationalStructureId();
        if (item) {
          this.form.patchValue({
            name: item.name,
            minSalary: item.minSalary ?? null,
            maxSalary: item.maxSalary ?? null,
            isManager: item.isManager ?? false,
            competencies: this.toDisplay(item.competencies),
            occupationalGroupId: item.occupationalGroup.id,
            yearsOfExperienceRequired: item.yearsOfExperienceRequired ?? null,
            organizationalStructureId: item.organizationalStructureId,
            natureOfWork: this.toDisplay(item.natureOfWork),
            typicalTasks: this.toDisplay(item.typicalTasks),
            requiredEducation: this.toDisplay(item.requiredEducation),
            characteristics: this.toDisplay(item.characteristics),
            otherDesirableRequirements: this.toDisplay(item.otherDesirableRequirements),
            probatoryPeriod: item.probatoryPeriod ?? '',
            punctuation: item.punctuation ?? null,
            grade: item.grade ?? '',
            code: item.code ?? '',
          });
        }
      }
    });
  }

  protected get isEditMode(): boolean {
    return !!this.position();
  }

  protected get nameError(): string {
    const ctrl = this.form.controls.name;
    if (ctrl.touched && ctrl.hasError('required')) return 'El nombre es requerido';
    return '';
  }

  protected get minSalaryError(): string {
    const ctrl = this.form.controls.minSalary;
    if (ctrl.touched && ctrl.hasError('required')) return 'El salario minimo es requerido';
    return '';
  }

  protected get natureOfWorkError(): string {
    const ctrl = this.form.controls.natureOfWork;
    if (ctrl.touched && ctrl.hasError('required')) return 'La naturaleza del trabajo es requerida';
    return '';
  }

  protected get typicalTasksError(): string {
    const ctrl = this.form.controls.typicalTasks;
    if (ctrl.touched && ctrl.hasError('required')) return 'Las tareas tipicas son requeridas';
    return '';
  }

  protected get requiredEducationError(): string {
    const ctrl = this.form.controls.requiredEducation;
    if (ctrl.touched && ctrl.hasError('required')) return 'La educación requerida es requerida';
    return '';
  }

  protected get characteristicsError(): string {
    const ctrl = this.form.controls.characteristics;
    if (ctrl.touched && ctrl.hasError('required')) return 'Las características son requeridas';
    return '';
  }

  protected get maxSalaryError(): string {
    const ctrl = this.form.controls.maxSalary;
    if (ctrl.touched && ctrl.hasError('required')) return 'El salario maximo es requerido';
    return '';
  }

  protected get occupationalGroupError(): string {
    const ctrl = this.form.controls.occupationalGroupId;
    if (ctrl.touched && ctrl.hasError('required')) return 'El grupo ocupacional es requerido';
    return '';
  }

  protected get yearsOfExperienceRequiredError(): string {
    const ctrl = this.form.controls.yearsOfExperienceRequired;
    if (ctrl.touched && ctrl.hasError('required'))
      return 'Los anios de experiencia requeridos son requeridos';
    return '';
  }

  protected get organizationalStructureError(): string {
    const ctrl = this.form.controls.organizationalStructureId;
    if (ctrl.touched && ctrl.hasError('required'))
      return 'La estructura organizacional es requerida';
    return '';
  }

  protected get otherDesirableRequirementsError(): string {
    const ctrl = this.form.controls.otherDesirableRequirements;
    if (ctrl.touched && ctrl.hasError('required'))
      return 'Las otras deseables requeridas son requeridas';
    return '';
  }

  protected get probatoryPeriodError(): string {
    const ctrl = this.form.controls.probatoryPeriod;
    if (ctrl.touched && ctrl.hasError('required')) return 'El periodo probatorio es requerido';
    return '';
  }

  protected get punctuationError(): string {
    const ctrl = this.form.controls.punctuation;
    if (ctrl.touched && ctrl.hasError('required')) return 'El puntaje es requerido';
    return '';
  }

  protected get gradeError(): string {
    const ctrl = this.form.controls.grade;
    if (ctrl.touched && ctrl.hasError('required')) return 'El grado es requerido';
    return '';
  }

  protected get codeError(): string {
    const ctrl = this.form.controls.code;
    if (ctrl.touched && ctrl.hasError('required')) return 'El codigo es requerido';
    return '';
  }

  private async getOccupationalGroupId() {
    try {
      const result = await firstValueFrom(this.occupationalGroupService.getAll());
      this.occupationalGroups.set(result.items.map((g) => ({ label: g.name, value: g.id })));
    } catch {
      this.notification.error('Error al cargar los grupos ocupacionales');
    }
  }

  private async getOrganizationalStructureId() {
    try {
      const result = await firstValueFrom(this.structureService.getAll());
      const options: SelectOption[] = [];

      const collect = (items: { id: string; name: string; dependencies?: typeof items }[]) => {
        for (const item of items) {
          options.push({ label: item.name, value: item.id });
          if (item.dependencies?.length) collect(item.dependencies);
        }
      };

      collect(result.items);
      this.organizationalStructures.set(options);
    } catch {
      this.notification.error('Error al cargar las estructuras organizativas');
    }
  }

  protected onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const body: PositionUpsert = {
      name: this.form.controls.name.value ?? undefined,
      minSalary: this.form.controls.minSalary.value ?? undefined,
      maxSalary: this.form.controls.maxSalary.value ?? undefined,
      isManager: this.form.controls.isManager.value ?? undefined,
      competencies: this.toStorage(this.form.controls.competencies.value),
      occupationalGroupId: this.form.controls.occupationalGroupId.value || undefined,
      yearsOfExperienceRequired: this.form.controls.yearsOfExperienceRequired.value ?? undefined,
      organizationalStructureId: this.form.controls.organizationalStructureId.value || undefined,
      natureOfWork: this.toStorage(this.form.controls.natureOfWork.value),
      typicalTasks: this.toStorage(this.form.controls.typicalTasks.value),
      requiredEducation: this.toStorage(this.form.controls.requiredEducation.value),
      characteristics: this.toStorage(this.form.controls.characteristics.value),
      otherDesirableRequirements: this.toStorage(this.form.controls.otherDesirableRequirements.value),
      probatoryPeriod: this.form.controls.probatoryPeriod.value || undefined,
      punctuation: this.form.controls.punctuation.value ?? undefined,
      grade: this.form.controls.grade.value || undefined,
      code: this.form.controls.code.value || undefined,
    };

    this.isSaving.set(true);
    const request$ = this.isEditMode
      ? this.positionService.update(this.position()!.id, body)
      : this.positionService.create(body);
    request$.subscribe({
      next: () => {
        this.isSaving.set(false);
        this.onClose();
        this.notification.success(
          `Cargo ${this.isEditMode ? 'actualizado' : 'creado'} exitosamente`,
        );
      },
      error: () => {
        this.notification.error('Error al guardar el cargo');
        this.isSaving.set(false);
      },
    });
  }

  protected onClose(): void {
    this.closed.emit();
  }

  private toDisplay(value: string | null | undefined): string {
    return (value ?? '').split('\x1F').join('\n');
  }

  private toStorage(value: string | null | undefined): string | undefined {
    const result = (value ?? '').split('\n').filter(s => s.trim()).join('\x1F');
    return result || undefined;
  }
}
