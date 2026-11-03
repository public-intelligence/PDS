/** Textos de ubicación compartidos por la lista, el mapa y el detalle de localidades. */

const DOMINICAN_REPUBLIC_CODE = 'DO';

interface LocationFields {
  paisCodigo?: string | null;
  paisNombre?: string | null;
  ciudad?: string | null;
  provinciaNombre?: string | null;
  municipioNombre?: string | null;
}

/** Localidad fuera de la República Dominicana (RF-LO-18). */
export function isAbroad(location: Pick<LocationFields, 'paisCodigo' | 'ciudad'>): boolean {
  if (location.paisCodigo) return location.paisCodigo !== DOMINICAN_REPUBLIC_CODE;
  return !!location.ciudad;
}

/** Columna «Ubicación» (UI-01): `Provincia · Municipio` en RD, `Ciudad, País` en el exterior. */
export function formatLocation(location: LocationFields): string {
  const parts = isAbroad(location)
    ? [location.ciudad, location.paisNombre]
    : [location.provinciaNombre, location.municipioNombre];
  return parts.filter(Boolean).join(isAbroad(location) ? ', ' : ' · ') || '—';
}
