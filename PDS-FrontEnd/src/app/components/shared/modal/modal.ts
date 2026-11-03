import { Component, Input, signal } from '@angular/core';

@Component({
  selector: 'app-modal',
  imports: [],
  templateUrl: './modal.html',
})
export class ModalComponent {
  @Input() title = '';
  @Input() hideOnClickOutside = false;
  @Input() isConfirmation = false;
  @Input() showCloseIcon = true;
  @Input() onClose?: () => void;

  public show = signal(false);

  toggleModal(bool: boolean) {
    this.show.set(bool);
  }

  closeModal() {
    this.show.set(false);
    if (!this.onClose) return;
    this.onClose();
  }
}
