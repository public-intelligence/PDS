import { saveAs } from 'file-saver';
import { StructureVisualizer } from '../models/structure.model';
import { StructureTemplate, StructureTemplateNode } from '../models/structure-template.model';

/** Versión del formato de plantilla que produce/acepta esta app. */
export const STRUCTURE_TEMPLATE_VERSION = 1;

/** Profundidad máxima de anidamiento aceptada (espejo de la validación del backend). */
const MAX_DEPTH = 10;

/** Longitud máxima del nombre de una unidad (espejo del JSON Schema). */
const MAX_NAME_LENGTH = 200;

/**
 * Convierte el árbol cargado (StructureVisualizer) en una plantilla portable, descartando
 * `id`/`code`/`showDependencies`. El tipo de área se exporta por nombre.
 */
export function toTemplate(nodes: StructureVisualizer[]): StructureTemplate {
  const mapNode = (node: StructureVisualizer): StructureTemplateNode => ({
    name: node.name,
    areaType: node.areaType?.name ?? '',
    children: (node.dependencies ?? []).map((dep) => mapNode(dep as unknown as StructureVisualizer)),
  });

  return {
    version: STRUCTURE_TEMPLATE_VERSION,
    nodes: nodes.map(mapNode),
  };
}

/** Serializa la plantilla y dispara la descarga del archivo JSON (usa file-saver). */
export function downloadTemplate(template: StructureTemplate, filename: string): void {
  const blob = new Blob([JSON.stringify(template, null, 2)], {
    type: 'application/json;charset=utf-8',
  });
  saveAs(blob, filename);
}

/** Cuenta el total de unidades (recursivo) de una plantilla, para mensajes al usuario. */
export function countNodes(nodes: StructureTemplateNode[]): number {
  return nodes.reduce((total, node) => total + 1 + countNodes(node.children ?? []), 0);
}

export type ValidationResult =
  | { ok: true; value: StructureTemplate }
  | { ok: false; errors: string[] };

const ALLOWED_ROOT_KEYS = new Set(['version', 'nodes']);
const ALLOWED_NODE_KEYS = new Set(['name', 'areaType', 'areaTypeId', 'children']);
const UUID_RE = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

function isPlainObject(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

/**
 * Valida que un objeto ya parseado cumpla el contrato de la plantilla
 * (`organization-structure.schema.json`). No hace red ni POST: es la primera barrera del cliente.
 */
export function validateTemplate(data: unknown): ValidationResult {
  const errors: string[] = [];

  if (!isPlainObject(data)) {
    return { ok: false, errors: ['El archivo debe contener un objeto JSON.'] };
  }

  for (const key of Object.keys(data)) {
    if (!ALLOWED_ROOT_KEYS.has(key)) {
      errors.push(`Propiedad no permitida en la raíz: '${key}'.`);
    }
  }

  if ('version' in data && data['version'] !== undefined) {
    const version = data['version'];
    if (typeof version !== 'number' || !Number.isInteger(version) || version < 1) {
      errors.push("'version' debe ser un entero mayor o igual a 1.");
    }
  }

  const nodes = data['nodes'];
  if (!Array.isArray(nodes)) {
    errors.push("'nodes' es obligatorio y debe ser un arreglo.");
  } else if (nodes.length === 0) {
    errors.push("'nodes' no puede estar vacío.");
  } else {
    validateNodes(nodes, 1, 'nodes', errors);
  }

  if (errors.length > 0) {
    return { ok: false, errors };
  }
  return { ok: true, value: data as unknown as StructureTemplate };
}

function validateNodes(nodes: unknown[], depth: number, path: string, errors: string[]): void {
  if (depth > MAX_DEPTH) {
    errors.push(`${path}: se excedió la profundidad máxima permitida (${MAX_DEPTH} niveles).`);
    return;
  }

  nodes.forEach((node, i) => {
    const nodePath = `${path}[${i}]`;
    if (!isPlainObject(node)) {
      errors.push(`${nodePath}: cada nodo debe ser un objeto.`);
      return;
    }

    for (const key of Object.keys(node)) {
      if (!ALLOWED_NODE_KEYS.has(key)) {
        errors.push(`${nodePath}: propiedad no permitida '${key}'.`);
      }
    }

    const name = node['name'];
    if (typeof name !== 'string' || name.trim().length === 0) {
      errors.push(`${nodePath}: 'name' es obligatorio.`);
    } else if (name.trim().length > MAX_NAME_LENGTH) {
      errors.push(`${nodePath}: 'name' supera los ${MAX_NAME_LENGTH} caracteres.`);
    }

    const areaType = node['areaType'];
    if (typeof areaType !== 'string' || areaType.trim().length === 0) {
      errors.push(`${nodePath}: 'areaType' es obligatorio.`);
    }

    if ('areaTypeId' in node && node['areaTypeId'] !== undefined) {
      const areaTypeId = node['areaTypeId'];
      if (typeof areaTypeId !== 'string' || !UUID_RE.test(areaTypeId)) {
        errors.push(`${nodePath}: 'areaTypeId' debe ser un GUID válido.`);
      }
    }

    if ('children' in node && node['children'] !== undefined) {
      const children = node['children'];
      if (!Array.isArray(children)) {
        errors.push(`${nodePath}: 'children' debe ser un arreglo.`);
      } else if (children.length > 0) {
        validateNodes(children, depth + 1, `${nodePath}.children`, errors);
      }
    }
  });
}

/** Plantilla de ejemplo mostrada/descargable en el modal de importación. */
export const SAMPLE_TEMPLATE: StructureTemplate = {
  version: STRUCTURE_TEMPLATE_VERSION,
  nodes: [
    {
      name: 'Despacho del Ministro',
      areaType: 'Ministerio',
      children: [
        {
          name: 'Viceministerio de Comercio Interno',
          areaType: 'Viceministerio',
          children: [
            {
              name: 'Dirección de Comercio',
              areaType: 'Dirección',
              children: [],
            },
          ],
        },
      ],
    },
  ],
};
