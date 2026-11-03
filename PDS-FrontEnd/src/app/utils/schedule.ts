import {
  DIAS_SEMANA,
  DiaSemana,
  IntervaloLaboral,
  JornadaSemanal,
  JornadaVersion,
} from '../models/locality-calendar.model';
import { intervalsError } from './intervals';

export const DAY_SHORT: Record<DiaSemana, string> = {
  mon: 'Lun',
  tue: 'Mar',
  wed: 'Mié',
  thu: 'Jue',
  fri: 'Vie',
  sat: 'Sáb',
  sun: 'Dom',
};

export const DAY_LONG: Record<DiaSemana, string> = {
  mon: 'Lunes',
  tue: 'Martes',
  wed: 'Miércoles',
  thu: 'Jueves',
  fri: 'Viernes',
  sat: 'Sábado',
  sun: 'Domingo',
};

export const DAY_INITIAL: Record<DiaSemana, string> = {
  mon: 'L',
  tue: 'M',
  wed: 'X',
  thu: 'J',
  fri: 'V',
  sat: 'S',
  sun: 'D',
};

export const EMPTY_WEEK_ERROR = 'Seleccione al menos un día laborable.';

const STANDARD_HOURS: IntervaloLaboral = { inicio: '08:00', fin: '16:00' };

/** Semana con los mismos intervalos en los días indicados y cerrada el resto. */
export function weekFrom(days: readonly DiaSemana[], intervals: IntervaloLaboral[]): JornadaSemanal {
  const week = {} as JornadaSemanal;
  for (const day of DIAS_SEMANA) {
    week[day] = days.includes(day) ? intervals.map((i) => ({ ...i })) : [];
  }
  return week;
}

/** Completa los días que el API omita para que la semana siempre tenga los siete. */
export function normalizeWeek(dias: Partial<JornadaSemanal> | null | undefined): JornadaSemanal {
  const week = {} as JornadaSemanal;
  for (const day of DIAS_SEMANA) week[day] = dias?.[day] ?? [];
  return week;
}

/** Plantillas del editor de horario (RF-LO-26). */
export const SCHEDULE_PRESETS: readonly { id: string; label: string; days: readonly DiaSemana[] }[] = [
  { id: 'lun-vie', label: 'Lun–Vie 08:00–16:00', days: DIAS_SEMANA.slice(0, 5) },
  { id: 'lun-sab', label: 'Lun–Sáb', days: DIAS_SEMANA.slice(0, 6) },
  { id: 'excepto-domingo', label: 'Todos excepto domingo', days: DIAS_SEMANA.slice(0, 6) },
];

export function presetWeek(days: readonly DiaSemana[]): JornadaSemanal {
  return weekFrom(days, [STANDARD_HOURS]);
}

/** `08:00–12:00, 13:00–16:00`. */
export function formatIntervals(intervals: IntervaloLaboral[] | null | undefined): string {
  return (intervals ?? []).map((i) => `${i.inicio}–${i.fin}`).join(', ');
}

/** Resumen del horario: `Lun–Vie 08:00–16:00 · Sáb 08:00–12:00 · Dom cerrado`. */
export function summarizeSchedule(dias: JornadaSemanal): string {
  const groups: { first: DiaSemana; last: DiaSemana; hours: string }[] = [];
  for (const day of DIAS_SEMANA) {
    const hours = formatIntervals(dias[day]) || 'cerrado';
    const previous = groups.at(-1);
    if (previous?.hours === hours) previous.last = day;
    else groups.push({ first: day, last: day, hours });
  }
  return groups
    .map(({ first, last, hours }) => {
      const range = first === last ? DAY_SHORT[first] : `${DAY_SHORT[first]}–${DAY_SHORT[last]}`;
      return `${range} ${hours}`;
    })
    .join(' · ');
}

/** Primera regla incumplida por la semana (RF-LO-24), o `null` si es válida. */
export function scheduleError(dias: JornadaSemanal): string | null {
  for (const day of DIAS_SEMANA) {
    const error = intervalsError(dias[day]);
    if (error) return `${DAY_LONG[day]}: ${error}`;
  }
  return DIAS_SEMANA.some((day) => dias[day].length > 0) ? null : EMPTY_WEEK_ERROR;
}

export interface ScheduleTimeline {
  /** Versión vigente hoy: la de mayor `vigenteDesde` que no supera la fecha (RF-LO-25). */
  current?: JornadaVersion;
  /** Versiones programadas, de la más próxima a la más lejana. */
  upcoming: JornadaVersion[];
  /** Versiones ya reemplazadas, de la más reciente a la más antigua. */
  past: JornadaVersion[];
}

export function scheduleTimeline(jornadas: JornadaVersion[], today: string): ScheduleTimeline {
  const sorted = [...jornadas].sort((a, b) => a.vigenteDesde.localeCompare(b.vigenteDesde));
  const started = sorted.filter((v) => v.vigenteDesde.slice(0, 10) <= today);
  return {
    current: started.at(-1),
    upcoming: sorted.filter((v) => v.vigenteDesde.slice(0, 10) > today),
    past: started.slice(0, -1).reverse(),
  };
}
