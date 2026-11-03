import { ZonaHoraria } from '../models/locality-catalog.model';

const OFFSET_PATTERN = /^([+-])(\d{2}):(\d{2})$/;

/** Países cuya única zona se conoce sin consultar el catálogo. */
const SINGLE_ZONE_BY_COUNTRY: Record<string, string> = {
  DO: 'America/Santo_Domingo',
};

/** Convierte un desplazamiento ISO (`-04:00`) al texto de la interfaz (`UTC−4`). */
export function formatUtcOffset(offset: string): string {
  if (offset === 'Z') return 'UTC';
  const match = OFFSET_PATTERN.exec(offset);
  if (!match) return `UTC ${offset}`;

  const [, sign, hours, minutes] = match;
  if (hours === '00' && minutes === '00') return 'UTC';

  const signText = sign === '-' ? '−' : '+';
  const minutesText = minutes === '00' ? '' : `:${minutes}`;
  return `UTC${signText}${Number(hours)}${minutesText}`;
}

/** Texto de opción del selector de zona horaria (UI-03). */
export function timeZoneOptionLabel(zona: ZonaHoraria): string {
  return [zona.id, zona.comentario, `ahora ${formatUtcOffset(zona.desplazamientoActual)}`]
    .filter(Boolean)
    .join(' · ');
}

/**
 * Zonas IANA que conoce el navegador. Respaldo mientras el catálogo del API (RF-LO-21) no esté
 * desplegado: sin él no se puede filtrar por país, salvo los de `SINGLE_ZONE_BY_COUNTRY`.
 */
export function browserTimeZones(paisCodigo?: string): ZonaHoraria[] {
  const single = paisCodigo ? SINGLE_ZONE_BY_COUNTRY[paisCodigo.toUpperCase()] : undefined;
  const ids = single ? [single] : Intl.supportedValuesOf('timeZone');
  return ids.map((id) => ({ id, desplazamientoActual: currentOffset(id) }));
}

/** `America/New_York (UTC−4)`, con el desplazamiento vigente según el navegador (UI-01). */
export function zoneWithOffset(timeZone: string): string {
  return `${timeZone} (${formatUtcOffset(currentOffset(timeZone))})`;
}

function currentOffset(timeZone: string): string {
  const name = new Intl.DateTimeFormat('en-US', { timeZone, timeZoneName: 'longOffset' })
    .formatToParts(new Date())
    .find((part) => part.type === 'timeZoneName')?.value;
  return name?.replace('GMT', '') || '+00:00';
}
