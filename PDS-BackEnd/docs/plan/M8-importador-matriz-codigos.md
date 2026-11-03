# M8 — Importador de la matriz de códigos consolidada

**Depende de:** M2 + **archivo consolidado del usuario (BLOQUEADO hasta que exista)**
**Estado:** Implementado — ver [M9](M9-maestra-de-articulos.md)

## Contexto

El usuario está preparando un archivo consolidado nuevo, con base en `Unificación de Códigos\ComprasCatalogo.xlsx`:
- Hoja **M** (~49,663 filas): materiales UNSPSC con jerarquía Segmento→Familia→Clase→Material (códigos 8 dígitos), descripción ES/EN, objeto de gasto, definiciones, sinónimos, códigos adicionales.
- Hoja **SUBCLASE** (~20,740 filas): todos los niveles (S/F/C/M) con OBJETO DE GASTO I/II y descripciones.
- Acompañado de `DIGECOB-Plan.pdf` y `DIGEPRES-Clasificador.pdf`.

Al archivo consolidado se le sumará la columna de **área consolidadora**. El modelo de M2 (`UnifiedCode` con atributos nullable heredables + `UnifiedCodeAlias` por sistema) ya está preparado para absorberlo **sin migración estructural**.

## Objetivo

Cargar la matriz completa a BD como fuente única de verdad consultable por otros sistemas (inventarios, compras) vía M2M.

## Alcance previsto

- `IUnifiedCatalogImportService.ImportAsync(stream, dryRun)` con el pipeline Excel de M1:
  - Validar jerarquía completa (cada Material tiene Clase/Familia/Segmento), códigos únicos, áreas consolidadoras resolubles contra el organigrama, aliases por sistema (DIGEPRES, DIGECOB, INVENTARIO…).
  - Aplicación transaccional con **upsert por Code**; códigos ya referenciados por requerimientos nunca se eliminan — se deshabilitan (`InstitutionallyEnabled = false`).
- `POST api/v1/unified-codes/import?dryRun=` (permiso `planning:config:manage`).
- Endurecimiento del endpoint M2M: paginación + filtro `changedSince` para sincronización incremental de inventarios/compras.

## Pendiente de definición (cuando llegue el archivo)
- Mapeo exacto de columnas del archivo consolidado (la hoja M actual no tiene encabezados).
- Nivel al que vendrá asignada el área consolidadora (el modelo soporta cualquier nivel con herencia).
- Sistemas de alias definitivos y su semántica (objeto de gasto I vs II, cuenta DIGECOB).

## Criterios de aceptación (previstos)
1. Import completo del archivo (~50k filas) en tiempo razonable (lotes/`AddRange`).
2. Re-import idempotente: mismas filas → 0 cambios.
3. Un material referenciado por un requerimiento que desaparece del archivo → queda deshabilitado, no borrado.
4. Sistema externo con token M2M sincroniza por `changedSince`.
