# M6 — POA Excel: plantilla, import por área, export institucional

**Depende de:** M5
**Estado:** ✅ Implementado (2026-07-26, junto con M5)

## Implementación realizada

- `PoaExcelService`: plantilla con las 19 columnas fijas + columnas personalizadas visibles de la plantilla del ciclo + hoja "Catálogos" (orígenes, tipos de medición, árbol del PEI del ciclo, formato de tareas); export por plan; export institucional del ciclo (`GET annual-plans/cycle-export?cycleId=`, columna Área al frente); import en dos fases (dryRun → errores por fila/columna → aplicar transaccional) con modos actualizar/reemplazar, agrupación de filas por Código, resolución de nombres contra PEI del ciclo/organigrama/municipios BD, y **conservación de ids** de productos (por código), unidades y tareas (por nombre normalizado) — protege los enlaces de requerimientos de M7.
- Codificación de tareas: `"Nombre (meses: 1,2,3)"` / `"Nombre (continua)"`, listas separadas por `|` (mismo contrato del CSV del mock).
- Frontend: el panel de importación del workspace sube el .xlsx al backend (validación previa automática al elegir archivo y al cambiar de modo; aplicar solo sin errores); botones plantilla/exportar descargan del backend; `planning-poa-csv.ts` quedó sin uso en el workspace (lo conserva `reportes-pds`, aún mock).

## Pendiente / diferido
- [ ] M7: huérfanos de requerimientos como error bloqueante del import (TODO marcado).
- [ ] Prueba manual: ciclo completo plantilla → llenar → dryRun con errores → corregir → aplicar → grid actualizado.

## Objetivo

Sustituir el CSV cliente del mock por .xlsx real generado/parseado en el backend (ClosedXML), conservando el contrato funcional (una fila por unidad de medida, filas con el mismo Código = un producto, conservación de ids).

## Entregable verificable

Descargar la plantilla del plan (con las columnas personalizadas del ciclo y hoja "Catálogos" con validación de datos), llenarla, subirla en modo actualizar/reemplazar con reporte de errores por fila, y descargar el POA institucional consolidado del ciclo.

## Sin modelos nuevos

`IPoaExcelService` (usa la infra Excel de M1 y catálogos reales):
- `BuildTemplateAsync(planId)` — hoja "POA" con cabeceras + fila de ejemplo tomada del PEI del ciclo + hoja "Catálogos" oculta (focos/objetivos/productos PEI, áreas, municipios, tipos de medición) con validación de datos de Excel.
- `ExportPlanAsync(planId)`.
- `ExportInstitutionalAsync(cycleId)` — todas las áreas, columna "Área" al frente.
- `ImportAsync(planId, stream, modo, dryRun)` — agrupa filas por Código (mismo algoritmo `csvAProductos` del mock), resuelve nombres→ids contra PEI del ciclo/organigrama/municipios BD, **conserva ids** de productos/tareas/unidades por código/nombre normalizado (no romper requerimientos), calcula huérfanos (bloqueante desde M7), aplica transaccional.

### Columnas hoja "POA"
`Código | Origen del producto | Foco estratégico | Objetivo estratégico | Producto estratégico (PEI) | Producto | Justificación operativa | Unidad de medida | Meta anual | Tipo de medición | T1 | T2 | T3 | T4 | Medios de verificación | Área responsable | Municipios de impacto (|) | Áreas involucradas (|) | Tareas ("Nombre (meses: 1,2)" / "Nombre (continua)") | [columnas personalizadas de la plantilla del ciclo]`

Conciliación con el formulario oficial DPD-FO-01: Resultado esperado/Indicador/Riesgo/Mitigación/Recursos se cubren con columnas personalizadas de la plantilla seed de M2. Los montos NO viajan en el POA (viven en Requerimientos).

## Endpoints

- `GET api/v1/annual-plans/{id}/excel-template`
- `GET api/v1/annual-plans/{id}/export`
- `POST api/v1/annual-plans/{id}/import?modo=actualizar|reemplazar&dryRun=` (multipart)
- `GET api/v1/planning-cycles/{id}/poa-export`

Misma autorización de edición del plan (import) / lectura (export).

## Frontend

El panel de importación del workspace sube el archivo y renderiza `ImportResultDto` (reutiliza la UI de errores por fila existente); `planning-poa-csv.ts` y sus botones CSV se eliminan.

## Criterios de aceptación
1. Ciclo completo: plantilla → llenar → dryRun con errores → corregir → aplicar → grid actualizado.
2. Import en modo actualizar conserva los ids de productos/tareas existentes (verificable por requerimientos intactos en M7).
3. Archivo con área/municipio/foco inexistente → error por fila con columna y mensaje.
4. Export institucional incluye todas las áreas del ciclo.
