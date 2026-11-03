/**
 * Fechas y horas de una localidad (RF-LO-23, §12.2): siempre en la zona horaria de la localidad,
 * nunca en la del navegador.
 */

type Instant = Date | string;

function parts(instant: Instant, timeZone: string, options: Intl.DateTimeFormatOptions) {
  const formatted = new Intl.DateTimeFormat('en-US', { timeZone, ...options }).formatToParts(
    new Date(instant),
  );
  return Object.fromEntries(formatted.map((part) => [part.type, part.value]));
}

function dateParts(instant: Instant, timeZone: string) {
  return parts(instant, timeZone, { year: 'numeric', month: '2-digit', day: '2-digit' });
}

/** "Hoy" de la localidad como `YYYY-MM-DD`. */
export function localityToday(timeZone: string, now: Date = new Date()): string {
  const { year, month, day } = dateParts(now, timeZone);
  return `${year}-${month}-${day}`;
}

/** Fecha local de un instante como `dd/MM/yyyy`. */
export function formatLocalityDate(instant: Instant, timeZone: string): string {
  const { year, month, day } = dateParts(instant, timeZone);
  return `${day}/${month}/${year}`;
}

/** Hora local de un instante en 24 h (`HH:mm`). */
export function formatLocalityTime(instant: Instant, timeZone: string): string {
  const { hour, minute } = parts(instant, timeZone, {
    hour: '2-digit',
    minute: '2-digit',
    hourCycle: 'h23',
  });
  return `${hour}:${minute}`;
}

/** Abreviatura vigente de la zona (p. ej. `EDT`); el navegador da `GMT-4` cuando no conoce una. */
export function timeZoneAbbreviation(timeZone: string, at: Instant = new Date()): string {
  return parts(at, timeZone, { timeZoneName: 'short' })['timeZoneName'] ?? '';
}

/** `dd/MM/yyyy HH:mm (EDT)`. */
export function formatLocalityDateTime(instant: Instant, timeZone: string): string {
  const date = formatLocalityDate(instant, timeZone);
  const time = formatLocalityTime(instant, timeZone);
  return `${date} ${time} (${timeZoneAbbreviation(timeZone, instant)})`;
}

/** Fecha de calendario del API (`YYYY-MM-DD`) como `dd/MM/yyyy`, sin conversión de zona. */
export function formatIsoDate(date: string | null | undefined): string {
  if (!date) return '';
  const [year, month, day] = date.slice(0, 10).split('-');
  return `${day}/${month}/${year}`;
}

/** Etiqueta un horario de la localidad: `08:00–16:00, hora local (EDT)`. */
export function withLocalTimeLabel(text: string, abbreviation: string): string {
  return `${text}, hora local (${abbreviation})`;
}

/**
 * Hora de reloj en la que ocurre un cambio de hora (`01/11/2026 02:00`): el instante de la
 * transición visto con el desplazamiento vigente antes del cambio.
 */
export function formatTransition(instant: Instant, offsetMinutesBefore: number): string {
  const wallClock = new Date(new Date(instant).getTime() + offsetMinutesBefore * 60_000);
  return `${formatLocalityDate(wallClock, 'UTC')} ${formatLocalityTime(wallClock, 'UTC')}`;
}
