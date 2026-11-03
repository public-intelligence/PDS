/**
 * Plantilla portable de la estructura organizacional usada para importar/exportar en JSON.
 * Solo describe la jerarquía (nombre + tipo de área + dependencias anidadas); el backend
 * regenera los códigos e identificadores al importar. Contrato:
 * `public/schemas/organization-structure.schema.json`.
 */
export interface StructureTemplate {
  version?: number;
  nodes: StructureTemplateNode[];
}

export interface StructureTemplateNode {
  /** Nombre de la unidad organizativa (obligatorio, ≤200 caracteres). */
  name: string;
  /** Tipo de área por nombre (forma primaria, portable entre entornos). */
  areaType: string;
  /** Identificador explícito del tipo de área (opcional; tiene prioridad si es válido). */
  areaTypeId?: string;
  /** Dependencias anidadas. */
  children: StructureTemplateNode[];
}
