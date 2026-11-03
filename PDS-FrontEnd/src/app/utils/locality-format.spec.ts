import { describe, expect, it } from 'vitest';
import { formatLocation, isAbroad } from './locality-format';

describe('formatLocation', () => {
  it('muestra provincia y municipio en la República Dominicana', () => {
    expect(
      formatLocation({ paisCodigo: 'DO', provinciaNombre: 'Santiago', municipioNombre: 'Licey' }),
    ).toBe('Santiago · Licey');
    expect(formatLocation({ provinciaNombre: 'Santiago' })).toBe('Santiago');
  });

  it('muestra ciudad y país en el exterior', () => {
    expect(
      formatLocation({ paisCodigo: 'US', paisNombre: 'Estados Unidos', ciudad: 'Nueva York', provinciaNombre: 'x' }),
    ).toBe('Nueva York, Estados Unidos');
  });

  it('muestra una raya cuando no hay datos', () => {
    expect(formatLocation({})).toBe('—');
  });
});

describe('isAbroad', () => {
  it('se guía por el código de país y, si falta, por la ciudad', () => {
    expect(isAbroad({ paisCodigo: 'DO', ciudad: 'Santo Domingo' })).toBe(false);
    expect(isAbroad({ paisCodigo: 'ES' })).toBe(true);
    expect(isAbroad({ ciudad: 'Madrid' })).toBe(true);
    expect(isAbroad({})).toBe(false);
  });
});
