import { describe, expect, it } from 'vitest';
import {
  browserTimeZones,
  formatUtcOffset,
  timeZoneOptionLabel,
  zoneWithOffset,
} from './time-zone';

describe('formatUtcOffset', () => {
  it('omite los minutos cuando son cero', () => {
    expect(formatUtcOffset('-04:00')).toBe('UTC−4');
    expect(formatUtcOffset('+01:00')).toBe('UTC+1');
  });

  it('conserva los minutos cuando no son cero', () => {
    expect(formatUtcOffset('+05:30')).toBe('UTC+5:30');
    expect(formatUtcOffset('-09:30')).toBe('UTC−9:30');
  });

  it('muestra UTC sin signo para el desplazamiento cero', () => {
    expect(formatUtcOffset('+00:00')).toBe('UTC');
    expect(formatUtcOffset('Z')).toBe('UTC');
  });

  it('devuelve el valor tal cual cuando no reconoce el formato', () => {
    expect(formatUtcOffset('desconocido')).toBe('UTC desconocido');
  });
});

describe('timeZoneOptionLabel', () => {
  it('une id, comentario y desplazamiento actual', () => {
    expect(
      timeZoneOptionLabel({
        id: 'America/New_York',
        comentario: 'Eastern (most areas)',
        desplazamientoActual: '-04:00',
      }),
    ).toBe('America/New_York · Eastern (most areas) · ahora UTC−4');
  });

  it('omite el comentario cuando no existe', () => {
    expect(
      timeZoneOptionLabel({ id: 'America/Santo_Domingo', desplazamientoActual: '-04:00' }),
    ).toBe('America/Santo_Domingo · ahora UTC−4');
  });
});

describe('browserTimeZones', () => {
  it('devuelve la única zona de la República Dominicana', () => {
    expect(browserTimeZones('DO')).toEqual([
      { id: 'America/Santo_Domingo', desplazamientoActual: '-04:00' },
    ]);
  });

  it('devuelve todas las zonas IANA para un país sin correspondencia local', () => {
    const ids = browserTimeZones('US').map((z) => z.id);
    expect(ids).toContain('America/New_York');
    expect(ids).toContain('Europe/Madrid');
  });
});

describe('zoneWithOffset', () => {
  it('añade el desplazamiento vigente a la zona', () => {
    expect(zoneWithOffset('America/Santo_Domingo')).toBe('America/Santo_Domingo (UTC−4)');
    expect(zoneWithOffset('Asia/Kolkata')).toBe('Asia/Kolkata (UTC+5:30)');
  });
});
