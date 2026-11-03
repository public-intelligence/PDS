import { describe, expect, it } from 'vitest';
import {
  formatIsoDate,
  formatLocalityDate,
  formatLocalityDateTime,
  formatLocalityTime,
  formatTransition,
  localityToday,
  timeZoneAbbreviation,
  withLocalTimeLabel,
} from './locality-time';

const INSTANT = '2026-10-07T20:00:00Z';

describe('localityToday', () => {
  it('usa la fecha local de la localidad, no la UTC (RF-LO-23 AC1)', () => {
    expect(localityToday('Asia/Tokyo', new Date(INSTANT))).toBe('2026-10-08');
    expect(localityToday('America/Santo_Domingo', new Date(INSTANT))).toBe('2026-10-07');
  });
});

describe('formato en la zona de la localidad', () => {
  it('muestra la fecha como dd/MM/yyyy', () => {
    expect(formatLocalityDate(INSTANT, 'Asia/Tokyo')).toBe('08/10/2026');
    expect(formatLocalityDate(INSTANT, 'America/New_York')).toBe('07/10/2026');
  });

  it('muestra la hora en 24 h como HH:mm', () => {
    expect(formatLocalityTime(INSTANT, 'America/New_York')).toBe('16:00');
    expect(formatLocalityTime('2026-10-07T04:05:00Z', 'America/New_York')).toBe('00:05');
  });

  it('da la abreviatura vigente de la zona', () => {
    expect(timeZoneAbbreviation('America/New_York', new Date(INSTANT))).toBe('EDT');
    expect(timeZoneAbbreviation('America/New_York', new Date('2026-12-07T20:00:00Z'))).toBe('EST');
  });

  it('une fecha, hora y abreviatura', () => {
    expect(formatLocalityDateTime(INSTANT, 'America/New_York')).toBe('07/10/2026 16:00 (EDT)');
  });
});

describe('formatIsoDate', () => {
  it('convierte una fecha ISO sin desplazarla de día', () => {
    expect(formatIsoDate('2026-11-02')).toBe('02/11/2026');
  });

  it('devuelve vacío cuando no hay fecha', () => {
    expect(formatIsoDate(null)).toBe('');
  });
});

describe('withLocalTimeLabel', () => {
  it('etiqueta el horario como hora local con su abreviatura', () => {
    expect(withLocalTimeLabel('08:00–16:00', 'EDT')).toBe('08:00–16:00, hora local (EDT)');
  });
});

describe('formatTransition', () => {
  it('muestra la hora de reloj previa al cambio (fin del horario de verano en Nueva York)', () => {
    expect(formatTransition('2026-11-01T06:00:00Z', -240)).toBe('01/11/2026 02:00');
  });
});
