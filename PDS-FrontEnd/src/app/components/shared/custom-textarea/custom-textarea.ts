import { Component, computed, effect, ElementRef, inject, input, signal, ViewChild } from '@angular/core';
import { ControlValueAccessor, NgControl } from '@angular/forms';

let textareaIdSeq = 0;

@Component({
  selector: 'app-custom-textarea',
  templateUrl: './custom-textarea.html',
})
export class CustomTextarea implements ControlValueAccessor {
  @ViewChild('textareaRef') private textareaRef?: ElementRef<HTMLTextAreaElement>;

  constructor() {
    const ngControl = inject(NgControl, { self: true, optional: true });
    if (ngControl) ngControl.valueAccessor = this;

    effect(() => {
      this.value();
      queueMicrotask(() => this.autoResize());
    });
  }
  readonly label = input('');
  readonly placeholder = input('');
  readonly hint = input('');
  readonly errorMessage = input('');
  readonly required = input(false);
  readonly rows = input(4);
  readonly maxLength = input<number | null>(null);
  readonly id = input(`custom-textarea-${++textareaIdSeq}`);

  protected value = signal('');
  protected disabled = signal(false);

  protected hasError = computed(() => !!this.errorMessage());
  protected charCount = computed(() => this.value().length);
  protected showCharCount = computed(() => this.maxLength() !== null);

  protected textareaClasses = computed(() => {
    const base =
      'w-full resize-none overflow-hidden rounded-lg border bg-surface-raised px-3 py-2 text-sm text-ink placeholder:text-ink-subtle outline-none transition-colors';
    if (this.disabled())
      return `${base} cursor-not-allowed border-border bg-surface-sunken text-ink-subtle`;
    if (this.hasError())
      return `${base} border-error focus:border-error focus-visible:ring-2 focus-visible:ring-error/30`;
    return `${base} border-border-strong hover:border-brand-300 focus:border-brand focus-visible:ring-2 focus-visible:ring-brand-200`;
  });

  protected errorId = computed(() => `${this.id()}-error`);

  protected charCountWarning = computed(
    () => this.maxLength() !== null && this.charCount() >= (this.maxLength() ?? 0),
  );

  private _onChange: (value: string) => void = () => undefined;
  private _onTouched: () => void = () => undefined;

  writeValue(value: string): void {
    this.value.set(value ?? '');
  }

  registerOnChange(fn: (value: string) => void): void {
    this._onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this._onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled.set(isDisabled);
  }

  private autoResize(): void {
    const el = this.textareaRef?.nativeElement;
    if (!el) return;
    if (!this.value()) {
      el.style.height = '';
      return;
    }
    el.style.height = '0px';
    el.style.height = `${el.scrollHeight}px`;
  }

  protected onInput(event: Event): void {
    const el = event.target as HTMLTextAreaElement;
    this.value.set(el.value);
    this._onChange(el.value);
  }

  protected onBlur(): void {
    this._onTouched();
  }
}
