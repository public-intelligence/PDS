import { Component, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

export interface FilterSelectOption {
  value: string | number;
  label: string;
}

@Component({
  selector: 'app-table-filter',
  imports: [FormsModule],
  templateUrl: './table-filter.html',
})
export class TableFilter {
  readonly searchPlaceholder = input('Buscar...');
  readonly selectOptions = input<FilterSelectOption[]>([]);
  readonly selectPlaceholder = input('Todos');
  readonly selectLabel = input('');

  readonly searchChange = output<string>();
  readonly selectChange = output<string | number>();

  protected inputValue = signal('');
  protected searchValue = signal('');
  protected selectValue = signal<string | number>('');

  protected onInputChange(value: string): void {
    this.inputValue.set(value);
  }

  protected onSearch(): void {
    this.searchValue.set(this.inputValue());
    this.searchChange.emit(this.inputValue());
  }

  protected onSelectChange(value: string | number): void {
    this.selectValue.set(value);
    this.selectChange.emit(value);
  }

  protected clearFilters(): void {
    this.inputValue.set('');
    this.searchValue.set('');
    this.selectValue.set('');
    this.searchChange.emit('');
    this.selectChange.emit('');
  }

  protected get hasActiveFilters(): boolean {
    return !!this.searchValue() || !!this.selectValue();
  }
}
