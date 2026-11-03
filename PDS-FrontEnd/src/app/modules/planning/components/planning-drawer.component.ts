import { Component, input, output } from '@angular/core';

/**
 * Drawer lateral (slide-over) para editar/consultar el detalle de un registro
 * sin perder el contexto del conjunto. Proyecta cuerpo y footer.
 */
@Component({
  selector: 'app-planning-drawer',
  template: `
    @if (abierto()) {
      <!-- Overlay -->
      <div class="fade-in fixed inset-0 z-40 bg-black/30" (click)="cerrar.emit()" aria-hidden="true"></div>
      <!-- Panel -->
      <aside
        class="fixed inset-y-0 right-0 z-50 flex w-full max-w-[560px] flex-col bg-surface-raised shadow-2xl"
        role="dialog"
        aria-modal="true"
        [attr.aria-label]="titulo()"
      >
        <header class="flex items-start justify-between gap-3 border-b border-border px-5 py-4">
          <div class="min-w-0">
            <h2 class="!font-poppins truncate !text-lg font-semibold text-ink">{{ titulo() }}</h2>
            @if (subtitulo()) {
              <p class="mt-0.5 truncate text-xs text-ink-muted">{{ subtitulo() }}</p>
            }
          </div>
          <button type="button" class="btn-icon shrink-0" (click)="cerrar.emit()" aria-label="Cerrar panel">
            ✕
          </button>
        </header>
        <div class="custom-scrollbar min-h-0 flex-1 overflow-y-auto px-5 py-4">
          <ng-content />
        </div>
        <footer class="flex items-center justify-end gap-2 border-t border-border px-5 py-3">
          <ng-content select="[slot=footer]" />
        </footer>
      </aside>
    }
  `,
})
export class PlanningDrawerComponent {
  readonly abierto = input.required<boolean>();
  readonly titulo = input('');
  readonly subtitulo = input('');
  readonly cerrar = output<void>();
}
