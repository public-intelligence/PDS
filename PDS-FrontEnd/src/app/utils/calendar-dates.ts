/**
 * Aritmética de fechas de calendario (`YYYY-MM-DD`). Se opera en UTC para que el resultado no
 * dependa de la zona del navegador: una fecha de calendario no es un instante.
 */

const DAY_MS = 24 * 60 * 60 * 1000;
const WEEKDAY_SHORT = ['dom', 'lun', 'mar', 'mié', 'jue', 'vie', 'sáb'];

export const MONTH_NAMES = [
  'Enero',
  'Febrero',
  'Marzo',
  'Abril',
  'Mayo',
  'Junio',
  'Julio',
  'Agosto',
  'Septiembre',
  'Octubre',
  'Noviembre',
  'Diciembre',
];

function toUtc(date: string): number {
  const [year, month, day] = date.slice(0, 10).split('-').map(Number);
  return Date.UTC(year, month - 1, day);
}

function toIso(utc: number): string {
  return new Date(utc).toISOString().slice(0, 10);
}

export function addDays(date: string, days: number): string {
  return toIso(toUtc(date) + days * DAY_MS);
}

/** Días de `from` a `to`; negativo si `to` es anterior. */
export function daysBetween(from: string, to: string): number {
  return Math.round((toUtc(to) - toUtc(from)) / DAY_MS);
}

export function yearOf(date: string): number {
  return Number(date.slice(0, 4));
}

export function yearRange(year: number): { desde: string; hasta: string } {
  return { desde: `${year}-01-01`, hasta: `${year}-12-31` };
}

/** `vie 01/01/2027`. */
export function formatDateWithWeekday(date: string): string {
  const [year, month, day] = date.slice(0, 10).split('-');
  return `${WEEKDAY_SHORT[new Date(toUtc(date)).getUTCDay()]} ${day}/${month}/${year}`;
}

/**
 * Semanas de un mes (lunes a domingo) para la vista anual; `null` rellena los días de otros meses.
 * @param month 0 = enero.
 */
export function monthGrid(year: number, month: number): (string | null)[][] {
  const first = Date.UTC(year, month, 1);
  const daysInMonth = new Date(Date.UTC(year, month + 1, 0)).getUTCDate();
  const leading = (new Date(first).getUTCDay() + 6) % 7;

  const cells: (string | null)[] = Array<string | null>(leading).fill(null);
  for (let day = 0; day < daysInMonth; day++) cells.push(toIso(first + day * DAY_MS));
  while (cells.length % 7 !== 0) cells.push(null);

  const weeks: (string | null)[][] = [];
  for (let i = 0; i < cells.length; i += 7) weeks.push(cells.slice(i, i + 7));
  return weeks;
}

/**
 * Ventana retroactiva (RF-LO-33): un gestor no puede tocar fechas anteriores a hoy − N días,
 * donde "hoy" es la fecha local de la localidad.
 */
export function isBeforeRetroWindow(date: string, today: string, windowDays: number): boolean {
  return daysBetween(date, today) > windowDays;
}
