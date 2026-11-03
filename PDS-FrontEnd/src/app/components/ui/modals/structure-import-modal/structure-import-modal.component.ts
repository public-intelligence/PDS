import { Component, EventEmitter, inject, Input, Output, signal, ViewChild } from '@angular/core';
import { ModalComponent } from '../../../shared/modal/modal';
import { StructureService } from '../../../../services/structure/structure.service';
import { NotificationService } from '../../../../services/notification/notification.service';
import { StructureTemplate } from '../../../../models/structure-template.model';
import {
  countNodes,
  downloadTemplate,
  SAMPLE_TEMPLATE,
  validateTemplate,
} from '../../../../utils/structure-template.util';

@Component({
  selector: 'app-structure-import-modal',
  imports: [ModalComponent],
  templateUrl: './structure-import-modal.component.html',
})
export class StructureImportModalComponent {
  @ViewChild(ModalComponent) modalComponent!: ModalComponent;

  /** La importación requiere un borrador en curso (misma regla que la edición manual). */
  @Input() hasDraft = false;

  /** Se emite tras una importación exitosa para que el contenedor recargue la estructura. */
  @Output() imported = new EventEmitter<void>();
  /** Se emite cuando el usuario solicita crear un borrador desde el modal. */
  @Output() createDraftRequested = new EventEmitter<void>();

  private structureService = inject(StructureService);
  private notification = inject(NotificationService);

  protected parsed = signal<StructureTemplate | null>(null);
  protected errors = signal<string[]>([]);
  protected fileName = signal<string>('');
  protected nodeCount = signal<number>(0);
  protected isImporting = signal<boolean>(false);

  /** Ejemplo formateado mostrado en el modal. */
  protected readonly sampleJson = JSON.stringify(SAMPLE_TEMPLATE, null, 2);

  open(): void {
    this.reset();
    this.modalComponent.toggleModal(true);
  }

  closeModal(): void {
    this.modalComponent.toggleModal(false);
  }

  private reset(): void {
    this.parsed.set(null);
    this.errors.set([]);
    this.fileName.set('');
    this.nodeCount.set(0);
    this.isImporting.set(false);
  }

  protected onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    this.parsed.set(null);
    this.errors.set([]);
    this.nodeCount.set(0);

    if (!file) {
      this.fileName.set('');
      return;
    }
    this.fileName.set(file.name);

    const reader = new FileReader();
    reader.onload = () => {
      let data: unknown;
      try {
        data = JSON.parse(reader.result as string);
      } catch {
        this.errors.set(['El archivo no es un JSON válido.']);
        return;
      }
      const result = validateTemplate(data);
      if (!result.ok) {
        this.errors.set(result.errors);
        return;
      }
      this.parsed.set(result.value);
      this.nodeCount.set(countNodes(result.value.nodes));
    };
    reader.onerror = () => this.errors.set(['No se pudo leer el archivo.']);
    reader.readAsText(file);
  }

  protected downloadSample(): void {
    downloadTemplate(SAMPLE_TEMPLATE, 'plantilla-estructura-organizacional.json');
  }

  protected onCreateDraft(): void {
    this.createDraftRequested.emit();
  }

  protected onImport(): void {
    const template = this.parsed();
    if (!template || !this.hasDraft || this.isImporting()) return;

    this.isImporting.set(true);
    this.structureService.importStructure(template).subscribe({
      next: () => {
        this.isImporting.set(false);
        this.notification.success('Estructura importada con éxito.');
        this.closeModal();
        this.imported.emit();
      },
      error: (err) => {
        this.isImporting.set(false);
        this.notification.fromHttpError(err, 'No se pudo importar la estructura.');
      },
    });
  }
}
