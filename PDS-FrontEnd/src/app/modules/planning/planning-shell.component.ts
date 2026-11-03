import { Component, computed, inject } from '@angular/core';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { filter, map, startWith } from 'rxjs';
import { PersonaSwitcherComponent } from './components/persona-switcher.component';
import { AuthService } from '../../services/auth/auth.service';
import { PlanningContextService } from '../../services/planning/planning-context.service';

/**
 * Rutas ya migradas a backend real (M1+): en ellas se oculta el persona-switcher de la demo
 * y manda el contexto real (PlanningContextService). Cada milestone agrega aquí sus rutas.
 */
const MIGRATED_ROUTE_PREFIXES: string[] = ['/pei', '/configuracion-pds', '/ciclos', '/plan-anual', '/requerimientos', '/consolidacion'];

/**
 * Contenedor de las pantallas de Planificación: renderiza la ruta hija y superpone el
 * selector de persona de la demo SOLO en pantallas aún mock. Al entrar al módulo carga
 * el contexto real del usuario (área organizacional + permisos) desde el backend.
 */
@Component({
  selector: 'app-planning-shell',
  imports: [RouterOutlet, PersonaSwitcherComponent],
  template: `
    <router-outlet />
    @if (showPersonaSwitcher()) {
      <app-persona-switcher />
    }
  `,
})
export class PlanningShellComponent {
  private router = inject(Router);
  private auth = inject(AuthService);
  private planningContext = inject(PlanningContextService);

  private readonly currentUrl = toSignal(
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      map((e) => e.urlAfterRedirects),
      startWith(this.router.url),
    ),
    { initialValue: this.router.url },
  );

  readonly showPersonaSwitcher = computed(() => {
    const url = this.currentUrl();
    return !MIGRATED_ROUTE_PREFIXES.some((prefix) => url.startsWith(prefix));
  });

  constructor() {
    // Contexto real del usuario (área + permisos); si no hay sesión o backend, el mock sigue.
    if (this.auth.getAccessToken()) {
      void this.planningContext.load();
    }
  }
}
