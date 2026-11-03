import { describe, expect, it } from 'vitest';
import {
  addDays,
  daysBetween,
  formatDateWithWeekday,
  isBeforeRetroWindow,
  monthGrid,
  yearRange,
} from './calendar-dates';

describe('aritmética de fechas', () => {
  it('suma días cruzando meses, años y bisiestos', () => {
    expect(addDays('2026-12-31', 1)).toBe('2027-01-01');
    expect(addDays('2028-02-28', 1)).toBe('2028-02-29');
    expect(addDays('2026-03-01', -1)).toBe('2026-02-28');
  });

  it('cuenta los días entre dos fechas', () => {
    expect(daysBetween('2026-11-02', '2026-11-10')).toBe(8);
    expect(daysBetween('2026-11-10', '2026-11-02')).toBe(-8);
  });

  it('da el rango completo de un año', () => {
    expect(yearRange(2027)).toEqual({ desde: '2027-01-01', hasta: '2027-12-31' });
  });

  it('formatea la fecha con el día de la semana', () => {
    expect(formatDateWithWeekday('2027-01-01')).toBe('vie 01/01/2027');
  });
});

describe('monthGrid', () => {
  it('empieza en lunes y rellena con null los días de otros meses', () => {
    // Enero de 2027 empieza en viernes y tiene 31 días.
    const weeks = monthGrid(2027, 0);
    expect(weeks[0]).toEqual([null, null, null, null, '2027-01-01', '2027-01-02', '2027-01-03']);
    expect(weeks.at(-1)).toEqual(['2027-01-25', '2027-01-26', '2027-01-27', '2027-01-28', '2027-01-29', '2027-01-30', '2027-01-31']);
    expect(weeks.every((week) => week.length === 7)).toBe(true);
  });

  it('contiene todos los días del mes una sola vez', () => {
    const days = monthGrid(2028, 1).flat().filter(Boolean);
    expect(days).toHaveLength(29);
    expect(days[0]).toBe('2028-02-01');
  });
});

describe('isBeforeRetroWindow', () => {
  it('permite hasta N días atrás y rechaza los anteriores (RF-LO-33)', () => {
    expect(isBeforeRetroWindow('2026-10-01', '2026-10-08', 7)).toBe(false);
    expect(isBeforeRetroWindow('2026-09-30', '2026-10-08', 7)).toBe(true);
    expect(isBeforeRetroWindow('2026-10-20', '2026-10-08', 7)).toBe(false);
  });
});
