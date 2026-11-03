import { Component, computed, inject, input, signal } from '@angular/core';
import { ControlValueAccessor, NgControl } from '@angular/forms';

let inputIdSeq = 0;

type InputType = 'text' | 'email' | 'password' | 'number' | 'tel' | 'url' | 'search' | 'date';

@Component({
  selector: 'app-custom-input',
  templateUrl: './custom-input.html',
})
export class CustomInput implements ControlValueAccessor {
  constructor() {
    const ngControl = inject(NgControl, { self: true, optional: true });
    if (ngControl) ngControl.valueAccessor = this;
  }
  readonly label = input('');
  readonly type = input<InputType>('text');
  readonly placeholder = input('');
  readonly hint = input('');
  readonly errorMessage = input('');
  readonly required = input(false);
  readonly id = input(`custom-input-${++inputIdSeq}`);

  protected value = signal('');
  protected disabled = signal(false);

  protected hasError = computed(() => !!this.errorMessage());

  protected inputClasses = computed(() => {
    if (this.disabled()) return 'field-control field-control-disabled';
    if (this.hasError()) return 'field-control field-control-error';
    return 'field-control';
  });

  protected errorId = computed(() => `${this.id()}-error`);

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

  protected onInput(event: Event): void {
    const val = (event.target as HTMLInputElement).value;
    this.value.set(val);
    this._onChange(val);
  }

  protected onBlur(): void {
    this._onTouched();
  }
}
