/**
 * Extrae el nombre de archivo de un encabezado Content-Disposition (filename* o filename),
 * con un nombre de respaldo cuando el encabezado no está disponible (p. ej. no expuesto por CORS).
 */
export function filenameFromContentDisposition(header: string | null, fallback: string): string {
  if (!header) return fallback;

  const star = /filename\*=(?:UTF-8'')?([^;]+)/i.exec(header);
  if (star?.[1]) {
    try {
      return decodeURIComponent(star[1].trim().replace(/^"|"$/g, ''));
    } catch {
      // continúa con el filename simple
    }
  }

  const plain = /filename="?([^";]+)"?/i.exec(header);
  if (plain?.[1]) return plain[1].trim();

  return fallback;
}
