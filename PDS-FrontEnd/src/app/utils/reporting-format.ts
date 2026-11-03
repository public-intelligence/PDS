const LOCALE = 'es-DO';

/** Entero localizado (es-DO). */
export function num(value?: number | null): string {
  return (value ?? 0).toLocaleString(LOCALE);
}

/** Moneda estimada localizada (RD$), o "—" si no hay valor. */
export function money(value?: number | null): string {
  if (value == null) return '—';
  return `RD$ ${value.toLocaleString(LOCALE, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

/** Delta con signo explícito. */
export function signed(value: number): string {
  return (value > 0 ? '+' : '') + value.toLocaleString(LOCALE);
}
