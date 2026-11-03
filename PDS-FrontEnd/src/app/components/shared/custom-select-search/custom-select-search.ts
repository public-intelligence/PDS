import { Component, DestroyRef, computed, inject, input, output, signal } from '@angular/core';
import { ControlValueAccessor, NgControl } from '@angular/forms';
import { SelectOption } from '../custom-select/custom-select';

let selectSearchIdSeq = 0;
const SEARCH_DEBOUNCE_MS = 300;

@Component({
  selector: 'app-custom-select-search',
  templateUrl: './custom-select-search.html',
})
export class CustomSelectSearch implements ControlValueAccessor {
  private destroyRef = inject(DestroyRef);
  private searchDebounce?: ReturnType<typeof setTimeout>;

  constructor() {
    const ngControl = inject(NgControl, { self: true, optional: true });
    if (ngControl) ngControl.valueAccessor = this;
    this.destroyRef.onDestroy(() => clearTimeout(this.searchDebounce));
  }

  readonly label = input('');
  readonly options = input<SelectOption[]>([]);
  readonly placeholder = input('Seleccionar...');
  readonly searchPlaceholder = input('Buscar...');
  readonly maxResults = input(10);
  readonly hint = input('');
  readonly errorMessage = input('');
  readonly required = input(false);
  readonly id = input(`custom-select-search-${++selectSearchIdSeq}`);

  /** Emite el texto de búsqueda (debounced) para consumidores que resuelven opciones en el servidor. */
  readonly search = output<string>();

  protected value = signal<string | number>('');
  protected disabled = signal(false);
  protected isOpen = signal(false);
  protected searchQuery = signal('');

  protected hasError = computed(() => !!this.errorMessage());

  private allMatching = computed(() => {
    const q = this.searchQuery().toLowerCase().trim();
    if (!q) return this.options();
    return this.options().filter((o) => o.label.toLowerCase().includes(q));
  });

  protected filteredOptions = computed(() => this.allMatching().slice(0, this.maxResults()));

  protected matchingCount = computed(() => this.allMatching().length);

  protected selectedLabel = computed(
    () => this.options().find((o) => o.value === this.value())?.label ?? '',
  );

  /** Muestra el texto de búsqueda mientras está abierto; la etiqueta seleccionada cuando está cerrado. */
  protected displayValue = computed(() => (this.isOpen() ? this.searchQuery() : this.selectedLabel()));

  protected effectivePlaceholder = computed(() =>
    this.isOpen() ? this.searchPlaceholder() : this.placeholder(),
  );

  protected triggerClasses = computed(() => {
    const base = 'field-control pr-9 text-left ';
    const weight = this.value() ? 'font-medium' : 'font-normal';
    if (this.disabled()) return `${base} ${weight} field-control-disabled`;
    if (this.hasError()) return `${base} ${weight} field-control-error`;
    if (this.isOpen()) return `${base} ${weight} border-brand ring-2 ring-brand-200`;
    return `${base} ${weight}`;
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

  protected onFocus(): void {
    if (this.disabled()) return;
    this.searchQuery.set('');
    this.isOpen.set(true);
  }

  protected close(): void {
    this.isOpen.set(false);
    this.searchQuery.set('');
    this._onTouched();
  }

  protected onSearch(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.searchQuery.set(value);
    clearTimeout(this.searchDebounce);
    this.searchDebounce = setTimeout(() => this.search.emit(value), SEARCH_DEBOUNCE_MS);
  }

  protected selectOption(option: SelectOption): void {
    if (option.disabled) return;
    this.value.set(option.value);
    this._onChange(option.value);
    this.close();
  }

  protected clearSelection(event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    if (this.disabled()) return;
    this.value.set('');
    this._onChange('');
    this.close();
  }
}
