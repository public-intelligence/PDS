import { describe, expect, it } from 'vitest';
import { DIAS_SEMANA, JornadaVersion } from '../models/locality-calendar.model';
import {
  EMPTY_WEEK_ERROR,
  SCHEDULE_PRESETS,
  normalizeWeek,
  presetWeek,
  scheduleError,
  scheduleTimeline,
  summarizeSchedule,
  weekFrom,
} from './schedule';

const HOURS = [{ inicio: '08:00', fin: '16:00' }];

describe('weekFrom', () => {
  it('guarda "todos excepto domingo" como lun–sáb con horario y dom vacío (RF-LO-24 AC1)', () => {
    const week = weekFrom(DIAS_SEMANA.slice(0, 6), HOURS);
    expect(week.sat).toEqual(HOURS);
    expect(week.sun).toEqual([]);
    expect(week.mon).not.toBe(week.tue);
  });
});

describe('summarizeSchedule', () => {
  it('agrupa días consecutivos con el mismo horario', () => {
    const week = weekFrom(DIAS_SEMANA.slice(0, 5), HOURS);
    week.sat = [{ inicio: '08:00', fin: '12:00' }];
    expect(summarizeSchedule(week)).toBe('Lun–Vie 08:00–16:00 · Sáb 08:00–12:00 · Dom cerrado');
  });

  it('une varios intervalos de un mismo día', () => {
    const week = weekFrom(['mon'], [
      { inicio: '08:00', fin: '12:00' },
      { inicio: '13:00', fin: '16:00' },
    ]);
    expect(summarizeSchedule(week)).toBe('Lun 08:00–12:00, 13:00–16:00 · Mar–Dom cerrado');
  });
});

describe('scheduleError', () => {
  it('exige al menos un día laborable (RF-LO-24 AC3)', () => {
    expect(scheduleError(weekFrom([], HOURS))).toBe(EMPTY_WEEK_ERROR);
  });

  it('señala el día con intervalos solapados (RF-LO-24 AC2)', () => {
    const week = weekFrom(['tue'], [
      { inicio: '08:00', fin: '12:00' },
      { inicio: '11:00', fin: '14:00' },
    ]);
    expect(scheduleError(week)).toBe('Martes: Los intervalos de un mismo día no pueden solaparse.');
  });

  it('acepta una semana válida', () => {
    expect(scheduleError(presetWeek(SCHEDULE_PRESETS[0].days))).toBeNull();
  });
});

describe('normalizeWeek', () => {
  it('completa los días que falten', () => {
    expect(normalizeWeek({ mon: HOURS }).sun).toEqual([]);
    expect(Object.keys(normalizeWeek(null))).toHaveLength(7);
  });
});

describe('scheduleTimeline', () => {
  const version = (id: string, vigenteDesde: string): JornadaVersion => ({
    id,
    vigenteDesde,
    dias: weekFrom(['mon'], HOURS),
  });
  const versions = [version('b', '2027-01-01'), version('a', '2000-01-01'), version('c', '2025-06-01')];

  it('la vigente es la de mayor vigenteDesde que no supera hoy (RF-LO-25 AC1)', () => {
    const timeline = scheduleTimeline(versions, '2026-12-31');
    expect(timeline.current?.id).toBe('c');
    expect(timeline.upcoming.map((v) => v.id)).toEqual(['b']);
    expect(timeline.past.map((v) => v.id)).toEqual(['a']);
  });

  it('una versión entra en vigor el mismo día de su vigenteDesde', () => {
    expect(scheduleTimeline(versions, '2027-01-01').current?.id).toBe('b');
  });

  it('no hay vigente si todas son futuras', () => {
    expect(scheduleTimeline([version('x', '2030-01-01')], '2026-01-01').current).toBeUndefined();
  });
});
