import { DestroyRef, Signal, inject, signal } from '@angular/core';

const ONE_MINUTE_MS = 60_000;

/**
 * Hora actual como señal, refrescada cada minuto para las "horas locales" en pantalla. Debe
 * llamarse en contexto de inyección; el temporizador se libera con el componente.
 */
export function injectNow(): Signal<Date> {
  const now = signal(new Date());
  const timer = setInterval(() => now.set(new Date()), ONE_MINUTE_MS);
  inject(DestroyRef).onDestroy(() => clearInterval(timer));
  return now.asReadonly();
}
