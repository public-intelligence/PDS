import { Component, inject, signal } from '@angular/core';
import { PlanningStoreService } from '../data/planning-store.service';

/**
 * Control flotante EXCLUSIVO DE LA DEMO: permite cambiar el punto de vista
 * (persona) para recorrer los wire flows sin cerrar sesión, y reiniciar el
 * estado simulado. No forma parte del producto final.
 */
@Component({
  selector: 'app-persona-switcher',
  template: `
    <div class="fixed right-4 bottom-4 z-50 flex flex-col items-end gap-2">
      @if (open()) {
        <div class="card scale-in w-72 p-3">
          <p class="mb-2 flex items-center justify-between text-xs font-semibold text-ink-muted">
            <span>Punto de vista de la demo</span>
            <span class="badge-warning !text-[10px] uppercase">Demo</span>
          </p>
          <ul class="flex flex-col gap-1">
            @for (p of store.personas; track p.id) {
              <li>
                <button
                  type="button"
                  (click)="seleccionar(p.id)"
                  class="!flex w-full items-center gap-2 !rounded-lg !bg-transparent !px-2 !py-1.5 text-left !normal-case transition-colors hover:!bg-surface-sunken"
                  [class.!bg-brand-50]="p.id === store.personaId()"
                >
                  <span
                    class="grid size-8 shrink-0 place-items-center rounded-full text-xs font-bold"
                    [class]="
                      p.id === store.personaId()
                        ? 'bg-brand text-white'
                        : 'bg-surface-sunken text-ink-muted'
                    "
                    >{{ p.iniciales }}</span
                  >
                  <span class="min-w-0">
                    <span class="block truncate text-sm font-medium text-ink">{{ p.nombre }}</span>
                    <span class="block truncate text-xs text-ink-muted">{{ p.cargo }}</span>
                  </span>
                </button>
              </li>
            }
          </ul>
          <button type="button" class="btn-ghost btn-sm mt-2 w-full" (click)="reiniciar()">
            ⟲ Reiniciar datos de la demo
          </button>
        </div>
      }
      <button
        type="button"
        (click)="open.set(!open())"
        class="!flex items-center gap-2 !rounded-full !bg-brand !px-4 !py-2 !text-white shadow-lg !normal-case hover:!bg-brand-700"
        title="Cambiar punto de vista (demo)"
      >
        <span
          class="grid size-6 place-items-center rounded-full bg-white/20 text-[10px] font-bold"
          >{{ store.persona().iniciales }}</span
        >
        <span class="text-sm font-medium">{{ store.persona().nombre.split(' ')[0] }}</span>
        <span class="rounded bg-accent px-1.5 py-0.5 text-[9px] font-bold text-ink uppercase">Demo</span>
      </button>
    </div>
  `,
})
export class PersonaSwitcherComponent {
  protected readonly store = inject(PlanningStoreService);
  protected readonly open = signal(false);

  protected seleccionar(id: (typeof this.store.personas)[number]['id']): void {
    this.store.setPersona(id);
    this.open.set(false);
  }

  protected reiniciar(): void {
    this.store.reset();
    this.open.set(false);
  }
}
