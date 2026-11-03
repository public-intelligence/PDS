import { Directive, ElementRef, OnDestroy, OnInit, inject, output } from '@angular/core';

@Directive({
  selector: '[appClickOutside]',
})
export class ClickOutsideDirective implements OnInit, OnDestroy {
  private el = inject(ElementRef<HTMLElement>);

  readonly appClickOutside = output<void>();

  private handler = (event: MouseEvent): void => {
    if (!this.el.nativeElement.contains(event.target as Node)) {
      this.appClickOutside.emit();
    }
  };

  ngOnInit(): void {
    document.addEventListener('click', this.handler, true);
  }

  ngOnDestroy(): void {
    document.removeEventListener('click', this.handler, true);
  }
}
