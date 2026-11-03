import { IntervaloLaboral } from '../models/locality-calendar.model';

export const MAX_INTERVALS_PER_DAY = 4;
const STEP_MINUTES = 15;
const DAY_MINUTES = 24 * 60;
const DEFAULT_INTERVAL: IntervaloLaboral = { inicio: '08:00', fin: '16:00' };
const DEFAULT_LENGTH_MINUTES = 60;
const TIME_PATTERN = /^(\d{2}):(\d{2})$/;

export const INTERVAL_ERRORS = {
  format: 'Indique las horas en formato HH:mm.',
  order: 'La hora de fin debe ser posterior a la de inicio.',
  overlap: 'Los intervalos de un mismo día no pueden solaparse.',
  max: `Se permiten hasta ${MAX_INTERVALS_PER_DAY} intervalos por día.`,
} as const;

/** Minutos desde la medianoche local, o `null` si el texto no es una hora válida. */
function toMinutes(time: string): number | null {
  const match = TIME_PATTERN.exec(time);
  if (!match) return null;
  const minutes = Number(match[1]) * 60 + Number(match[2]);
  return Number(match[2]) < 60 && minutes <= DAY_MINUTES ? minutes : null;
}

function toTime(minutes: number): string {
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${pad(Math.floor(minutes / 60))}:${pad(minutes % 60)}`;
}

function stepsBetween(from: number, to: number): string[] {
  const options: string[] = [];
  for (let m = from; m <= to; m += STEP_MINUTES) options.push(toTime(m));
  return options;
}

/** Horas de inicio: `00:00` a `23:45`. */
export function startTimeOptions(): string[] {
  return stepsBetween(0, DAY_MINUTES - STEP_MINUTES);
}

/** Horas de fin: `00:15` a `24:00`. */
export function endTimeOptions(): string[] {
  return stepsBetween(STEP_MINUTES, DAY_MINUTES);
}

/** Primera regla incumplida por los intervalos de un día (RF-LO-24), o `null` si son válidos. */
export function intervalsError(intervals: IntervaloLaboral[]): string | null {
  if (intervals.length > MAX_INTERVALS_PER_DAY) return INTERVAL_ERRORS.max;

  const ranges: [number, number][] = [];
  for (const { inicio, fin } of intervals) {
    const start = toMinutes(inicio);
    const end = toMinutes(fin);
    if (start === null || end === null || start === DAY_MINUTES) return INTERVAL_ERRORS.format;
    if (end <= start) return INTERVAL_ERRORS.order;
    ranges.push([start, end]);
  }

  ranges.sort((a, b) => a[0] - b[0]);
  const overlaps = ranges.some(([start], i) => i > 0 && start < ranges[i - 1][1]);
  return overlaps ? INTERVAL_ERRORS.overlap : null;
}

/** Intervalo sugerido al pulsar "Agregar intervalo"; `null` si ya no cabe otro. */
export function nextInterval(intervals: IntervaloLaboral[]): IntervaloLaboral | null {
  if (intervals.length >= MAX_INTERVALS_PER_DAY) return null;
  if (!intervals.length) return { ...DEFAULT_INTERVAL };

  const lastEnd = Math.max(...intervals.map((i) => toMinutes(i.fin) ?? 0));
  if (lastEnd >= DAY_MINUTES) return null;
  return {
    inicio: toTime(lastEnd),
    fin: toTime(Math.min(lastEnd + DEFAULT_LENGTH_MINUTES, DAY_MINUTES)),
  };
}
