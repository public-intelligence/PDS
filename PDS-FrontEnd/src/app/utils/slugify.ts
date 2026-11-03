/**
 * Genera un slug a partir de un texto. Réplica del algoritmo del backend
 * (PDS.Service OrganizationDirectoryService.Slugify) para que el slug calculado en el cliente
 * coincida con el almacenado: minúsculas, sin acentos, alfanumérico separado por guiones.
 */
export function slugify(value: string | null | undefined): string {
  if (!value) return '';

  // NFD + remover marcas diacríticas combinantes => "Educación" -> "educacion".
  const normalized = value
    .trim()
    .toLowerCase()
    .normalize('NFD')
    .replace(/\p{Diacritic}/gu, '');

  let out = '';
  for (const c of normalized) {
    if (/[a-z0-9]/.test(c)) out += c;
    else if (c === ' ' || c === '-' || c === '_' || c === '.') out += '-';
  }

  while (out.includes('--')) out = out.replace('--', '-');
  return out.replace(/^-+|-+$/g, '');
}
