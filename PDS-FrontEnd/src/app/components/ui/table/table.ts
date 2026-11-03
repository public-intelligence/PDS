import { Component, inject, Input, OnChanges, signal } from '@angular/core';
import { NgClass } from '@angular/common';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { Actions, ColumnData, Column } from '../../../models/table';
import { RouterLink } from '@angular/router';
import { ClickOutsideDirective } from '../../../shared/directives/click-outside.directive';

@Component({
  selector: 'app-table',
  imports: [NgClass, RouterLink, ClickOutsideDirective],
  templateUrl: './table.html',
})
export class TableComponent implements OnChanges {
  private sanitizer = inject(DomSanitizer);

  @Input() headers: Column[] = [];
  @Input() columnData: ColumnData[] = [];
  @Input() actions = true;
  @Input() showIcons = false;
  @Input() isLoading = false;
  @Input() noDataDescription = 'No hay datos para mostrar...';
  @Input() editAction!: (id: string) => void;
  @Input() deleteAction!: (id: string) => void;

  protected data = signal<ColumnData[]>([]);
  protected dropdownPosition = signal<{ top: number; left: number }>({
    top: 0,
    left: 0,
  });

  ngOnChanges(): void {
    this.data.set(this.columnData);
  }

  safeHtml(icon: string | undefined): SafeHtml {
    return this.sanitizer.bypassSecurityTrustHtml(icon ?? '');
  }

  /**
   * Decide si una celda se muestra como "pill"/badge de estado y con qué color.
   * Usa el color explícito (cuando se provee) o reconoce los estados Activo/Inactivo.
   */
  protected pillColor(cell: Column): 'green' | 'red' | 'yellow' | 'default' | null {
    if (cell.color) return cell.color;
    if (cell.value === 'Activo') return 'green';
    if (cell.value === 'Inactivo') return 'red';
    return null;
  }

  protected badgeClasses(color: 'green' | 'red' | 'yellow' | 'default'): string {
    switch (color) {
      case 'green':
        return 'badge-success';
      case 'red':
        return 'badge-error';
      case 'yellow':
        return 'badge-warning';
      default:
        return 'badge-neutral';
    }
  }

  toggleActions = ({
    id,
    bool,
    event,
  }: {
    id: string;
    bool: boolean;
    event?: MouseEvent;
  }): void => {
    if (bool && event) {
      const rect = (event.currentTarget as HTMLElement).getBoundingClientRect();
      this.dropdownPosition.set({ top: rect.bottom, left: rect.left + rect.width / 2 });
    }
    const elements = this.columnData.map((item, index) => {
      if (`${item.id}-${index}` === id) {
        return { ...item, showActions: bool };
      }
      return { ...item, showActions: false };
    });
    this.data.set(elements);
  };

  protected runAction(action: Actions, id: string): void {
    action.function(id);
    this.toggleActions({ id, bool: false });
  }
}
