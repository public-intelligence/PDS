import { Component, computed, inject, input, signal } from '@angular/core';
import { ControlValueAccessor, NgControl } from '@angular/forms';

let selectIdSeq = 0;

export interface SelectOption {
  value: string | number;
  label: string;
  disabled?: boolean;
}

@Component({
  selector: 'app-custom-select',
  templateUrl: './custom-select.html',
})
export class CustomSelect implements ControlValueAccessor {
  constructor() {
    const ngControl = inject(NgControl, { self: true, optional: true });
    if (ngControl) ngControl.valueAccessor = this;
  }
  readonly label = input('');
  readonly options = input<SelectOption[]>([]);
  readonly placeholder = input('Seleccionar...');
  readonly hint = input('');
  readonly errorMessage = input('');
  readonly required = input(false);
  readonly id = input(`custom-select-${++selectIdSeq}`);

  protected value = signal<string | number>('');
  protected disabled = signal(false);

  protected hasError = computed(() => !!this.errorMessage());

  protected selectClasses = computed(() => {
    const base = 'field-control cursor-pointer appearance-none pr-9';
    const colorClass = this.value()
      ? 'text-ink font-medium'
      : 'text-ink-subtle font-normal';
    if (this.disabled()) return `${base} field-control-disabled`;
    if (this.hasError()) return `${base} ${colorClass} field-control-error`;
    return `${base} ${colorClass}`;
  });

  protected errorId = computed(() => `${this.id()}-error`);

  private _onChange: (value: string | number) => void = () => undefined;
  private _onTouched: () => void = () => undefined;

  writeValue(value: string | number): void {
    this.value.set(value ?? '');
  }

  registerOnChange(fn: (value: string | number) => void): void {
    this._onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this._onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled.set(isDisabled);
  }

  protected onSelectChange(event: Event): void {
    const val = (event.target as HTMLSelectElement).value;
    this.value.set(val);
    this._onChange(val);
  }

  protected onBlur(): void {
    this._onTouched();
  }
}
