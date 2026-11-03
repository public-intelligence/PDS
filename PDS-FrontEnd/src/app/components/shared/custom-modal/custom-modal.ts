import { Component, computed, effect, HostListener, inject, input, OnDestroy, output } from '@angular/core';
import { DOCUMENT } from '@angular/common';

export type ModalSize = 'sm' | 'md' | 'lg' | 'xl' | 'full';

let modalIdSeq = 0;

@Component({
  selector: 'app-custom-modal',
  templateUrl: './custom-modal.html',
  host: { class: 'contents' },
})
export class CustomModal implements OnDestroy {
  private document = inject(DOCUMENT);

  readonly isOpen = input(false);
  readonly title = input('');
  readonly size = input<ModalSize>('md');
  readonly closeOnBackdrop = input(true);
  readonly showCloseButton = input(true);
  /** Cubre el contenido con el loader mientras llegan los datos (p. ej. al abrir en edición). */
  readonly isLoading = input(false);

  readonly closed = output<void>();

  private idSeq = `custom-modal-${++modalIdSeq}`;
  protected titleId = computed(() => `${this.idSeq}-title`);

  protected panelClasses = computed(() => {
    const sizeMap: Record<ModalSize, string> = {
      sm: 'max-w-md',
      md: 'max-w-lg',
      lg: 'max-w-2xl',
      xl: 'max-w-3xl',
      full: 'max-w-5xl',
    };
    return `scale-in relative z-10 flex max-h-[90vh] w-full flex-col rounded-xl bg-surface-raised shadow-xl ring-1 ring-border ${sizeMap[this.size()]}`;
  });

  constructor() {
    effect(() => {
      this.document.body.style.overflow = this.isOpen() ? 'hidden' : '';
    });
  }

  ngOnDestroy(): void {
    this.document.body.style.overflow = '';
  }

  @HostListener('document:keydown.escape')
  onEscapeKey(): void {
    if (this.isOpen()) this.close();
  }

  protected onBackdropClick(): void {
    if (this.closeOnBackdrop()) this.close();
  }

  protected close(): void {
    this.closed.emit();
  }
}
