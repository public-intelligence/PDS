import { Component, computed, input } from '@angular/core';
import { NgClass } from '@angular/common';
import { VersionStatus, VERSION_STATUS_LABELS } from '../../../models/structure-version.model';

interface StepView {
  key: VersionStatus;
  label: string;
  state: 'done' | 'current' | 'upcoming';
}

/**
 * Visualización del flujo de aprobación de una versión:
 * Borrador → En revisión → Aprobada → Publicada, con ramal de "Rechazada".
 * Presentacional: solo recibe el estado actual.
 */
@Component({
  selector: 'app-version-workflow-stepper',
  imports: [NgClass],
  templateUrl: './version-workflow-stepper.component.html',
})
export class VersionWorkflowStepper {
  readonly status = input.required<VersionStatus>();

  private readonly flow: VersionStatus[] = ['Draft', 'InReview', 'Approved', 'Published'];
  protected readonly labels = VERSION_STATUS_LABELS;

  protected readonly isRejected = computed(() => this.status() === 'Rejected');
  protected readonly isSuperseded = computed(() => this.status() === 'Superseded');

  protected readonly steps = computed<StepView[]>(() => {
    const s = this.status();
    let currentIndex: number;
    if (s === 'Rejected')
      currentIndex = 1; // se detuvo en revisión
    else if (s === 'Superseded')
      currentIndex = this.flow.length; // recorrió todo el flujo
    else currentIndex = this.flow.indexOf(s);

    return this.flow.map((key, i) => ({
      key,
      label: this.labels[key],
      state: i < currentIndex ? 'done' : i === currentIndex ? 'current' : 'upcoming',
    }));
  });
}
