import { Component, signal } from '@angular/core';
import { CustomModal } from '../../../shared/custom-modal/custom-modal';

@Component({
  selector: 'app-confirmation-modal',
  imports: [CustomModal],
  templateUrl: './confirmation-modal.component.html',
})
export class ConfirmationModalComponent {
  title = '';
  paragraph = '';
  name = '';
  confirmButton = '';
  isDelete = false;
  warning = '';
  onConfirm!: () => void;

  protected readonly isOpen = signal(false);

  /**
   * Cierra el modal de confirmación
   */
  closeModal() {
    this.isOpen.set(false);
  }

  /**
   * Abre el modal de confirmación
   * @param title Título del modal
   * @param paragraph Texto del modal
   * @param name Nombre del elemento a eliminar
   * @param confirmButton Texto del botón de confirmación
   * @param warning Texto de advertencia
   * @param onConfirm Función a ejecutar al confirmar
   * @param isDelete Indica si es una acción de eliminación
   */
  openModal({
    title,
    paragraph,
    name,
    confirmButton,
    warning,
    onConfirm,
    isDelete,
  }: {
    title: string;
    name: string;
    paragraph: string;
    confirmButton: string;
    warning?: string;
    onConfirm: () => void;
    isDelete?: boolean;
  }) {
    this.title = title;
    this.name = name;
    this.warning = warning || '';
    this.paragraph = paragraph;
    this.confirmButton = confirmButton;
    this.onConfirm = onConfirm;
    this.isDelete = isDelete || false;
    this.isOpen.set(true);
  }
}
