import { describe, expect, it } from 'vitest';
import {
  INTERVAL_ERRORS,
  endTimeOptions,
  intervalsError,
  nextInterval,
  startTimeOptions,
} from './intervals';

describe('opciones de hora', () => {
  it('ofrece inicios cada 15 minutos sin 24:00', () => {
    const options = startTimeOptions();
    expect(options.slice(0, 3)).toEqual(['00:00', '00:15', '00:30']);
    expect(options.at(-1)).toBe('23:45');
    expect(options).toHaveLength(96);
  });

  it('ofrece 24:00 solo como fin', () => {
    const options = endTimeOptions();
    expect(options[0]).toBe('00:15');
    expect(options.at(-1)).toBe('24:00');
  });
});

describe('intervalsError', () => {
  it('acepta intervalos ordenados, contiguos y hasta 24:00', () => {
    expect(
      intervalsError([
        { inicio: '08:00', fin: '12:00' },
        { inicio: '12:00', fin: '16:00' },
        { inicio: '20:00', fin: '24:00' },
      ]),
    ).toBeNull();
    expect(intervalsError([])).toBeNull();
  });

  it('rechaza un fin que no es posterior al inicio', () => {
    expect(intervalsError([{ inicio: '12:00', fin: '12:00' }])).toBe(INTERVAL_ERRORS.order);
    expect(intervalsError([{ inicio: '16:00', fin: '08:00' }])).toBe(INTERVAL_ERRORS.order);
  });

  it('rechaza solapamientos sin importar el orden en que se escriben', () => {
    expect(
      intervalsError([
        { inicio: '11:00', fin: '14:00' },
        { inicio: '08:00', fin: '12:00' },
      ]),
    ).toBe(INTERVAL_ERRORS.overlap);
  });

  it('rechaza más de cuatro intervalos', () => {
    const five = ['08', '10', '12', '14', '16'].map((h) => ({ inicio: `${h}:00`, fin: `${h}:30` }));
    expect(intervalsError(five)).toBe(INTERVAL_ERRORS.max);
  });

  it('rechaza horas con formato inválido y 24:00 como inicio', () => {
    expect(intervalsError([{ inicio: '8am', fin: '12:00' }])).toBe(INTERVAL_ERRORS.format);
    expect(intervalsError([{ inicio: '24:00', fin: '24:00' }])).toBe(INTERVAL_ERRORS.format);
  });
});

describe('nextInterval', () => {
  it('propone la jornada habitual cuando no hay intervalos', () => {
    expect(nextInterval([])).toEqual({ inicio: '08:00', fin: '16:00' });
  });

  it('continúa una hora después del último fin', () => {
    expect(nextInterval([{ inicio: '08:00', fin: '12:00' }])).toEqual({
      inicio: '12:00',
      fin: '13:00',
    });
    expect(nextInterval([{ inicio: '08:00', fin: '23:30' }])).toEqual({
      inicio: '23:30',
      fin: '24:00',
    });
  });

  it('no propone nada si el día está lleno o ya hay cuatro', () => {
    expect(nextInterval([{ inicio: '08:00', fin: '24:00' }])).toBeNull();
    const four = ['08', '10', '12', '14'].map((h) => ({ inicio: `${h}:00`, fin: `${h}:30` }));
    expect(nextInterval(four)).toBeNull();
  });
});
