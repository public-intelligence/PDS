import { Component, computed, forwardRef, input, signal } from '@angular/core';
import {
  AbstractControl,
  ControlValueAccessor,
  NG_VALIDATORS,
  NG_VALUE_ACCESSOR,
  ValidationErrors,
  Validator,
} from '@angular/forms';
import { IntervaloLaboral } from '../../../models/locality-calendar.model';
import {
  endTimeOptions,
  intervalsError,
  nextInterval,
  startTimeOptions,
} from '../../../utils/intervals';

let intervalsEditorIdSeq = 0;

/**
 * Intervalos laborales de un día (UI-06/08/11): hasta 4, `HH:mm` en pasos de 15 minutos y `24:00`
 * como fin. El control queda inválido (`intervalos`) si un fin no supera su inicio o hay solape.
 */
@Component({
  selector: 'app-intervals-editor',
  templateUrl: './intervals-editor.html',
  providers: [
    { provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => IntervalsEditor), multi: true },
    { provide: NG_VALIDATORS, useExisting: forwardRef(() => IntervalsEditor), multi: true },
  ],
})
export class IntervalsEditor implements ControlValueAccessor, Validator {
  readonly label = input('');
  readonly required = input(false);
  readonly emptyText = input('Sin intervalos');
  readonly id = input(`intervals-editor-${++intervalsEditorIdSeq}`);

  protected readonly startOptions = startTimeOptions();
  protected readonly endOptions = endTimeOptions();

  protected intervals = signal<IntervaloLaboral[]>([]);
  protected disabled = signal(false);

  protected error = computed(() => intervalsError(this.intervals()) ?? '');
  protected canAdd = computed(() => !this.disabled() && nextInterval(this.intervals()) !== null);
  protected errorId = computed(() => `${this.id()}-error`);

  private _onChange: (value: IntervaloLaboral[]) => void = () => undefined;
  private _onTouched: () => void = () => undefined;

  writeValue(value: IntervaloLaboral[] | null): void {
    this.intervals.set(value ?? []);
  }

  registerOnChange(fn: (value: IntervaloLaboral[]) => void): void {
    this._onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this._onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled.set(isDisabled);
  }

  validate(control: AbstractControl<IntervaloLaboral[] | null>): ValidationErrors | null {
    const error = intervalsError(control.value ?? []);
    return error ? { intervalos: error } : null;
  }

  /** Incluye la hora guardada aunque no caiga en un paso de 15 minutos, para no ocultarla. */
  protected optionsWith(options: string[], value: string): string[] {
    return options.includes(value) ? options : [...options, value].sort();
  }

  protected add(): void {
    const next = nextInterval(this.intervals());
    if (next) this.commit([...this.intervals(), next]);
  }

  protected remove(index: number): void {
    this.commit(this.intervals().filter((_, i) => i !== index));
  }

  protected update(index: number, field: keyof IntervaloLaboral, event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    this.commit(this.intervals().map((it, i) => (i === index ? { ...it, [field]: value } : it)));
  }

  protected onBlur(): void {
    this._onTouched();
  }

  private commit(intervals: IntervaloLaboral[]): void {
    this.intervals.set(intervals);
    this._onChange(intervals);
  }
}
