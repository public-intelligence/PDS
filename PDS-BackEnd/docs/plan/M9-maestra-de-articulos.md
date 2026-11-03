# M9 — La Maestra de Artículos como entidad maestra

**Depende de:** M7 + M8
**Estado:** Implementado

## Contexto

Hasta M7 el ruteo de un requerimiento dependía del **árbol UNSPSC**: `Requirement.UnifiedCodeId`
y un recorrido por `ParentId` hasta el primer `ConsolidatorAreaId` no nulo. Eso obliga a colgar
el objeto del gasto del nodo UNSPSC, y no funciona: de las **2,030 clases** del archivo
institucional, **522 mapean a más de un objeto del gasto**. Además el usuario del POA piensa en
«monitor de 27 pulgadas», no en `43211902`.

La relación se invierte: el **Artículo** (`MasterProduct`) es la entidad maestra y UNSPSC,
objeto del gasto, cuenta contable y área consolidadora son *mapeos del artículo*.

## Modelo (esquema `Planning`)

- **`MasterProduct`** — identidad (`SourceId` UUIDv5 del archivo = clave de idempotencia,
  `Code` `ART-XXXXXXXX`, `Name`, `SearchText` normalizado de 4,000 caracteres para el
  autocomplete), clasificación (`UnifiedCodeId` + `UnspscCode/Name`, `ItemType`, `ItemSubtype`,
  `AssetTreatment`, `MeasureUnit`, `ReferencePrice`), mapeo regulatorio (`ExpenseObject*`,
  `AccountingAccount*`), ruteo (`ConsolidatorArea*`, `ConsolidatorAreaSource`,
  `ConsolidatorRule*`, confianzas, `AreaAmbiguity`, `AlternativeAreasJson`, `RequiresReview`)
  y gobernanza (`State`, `DuplicateOfMasterProductId`, campos de solicitud).
  **`IX_MasterProducts_OrgUnspsc` no es único a propósito**: N artículos por código UNSPSC.
- **`MasterProductAlias`** — sinónimos de la fuente, deduplicados por texto normalizado.
- **`ExpenseObject`** (DIGEPRES) y **`AccountingAccount`** (DIGECOB) — catálogos jerárquicos.
- **`ConsolidatorRoutingRule`** — una fila por `(RuleKey, Version)`; el archivo ya trae
  AREA-TD-001 v1 «Histórica» y v2 activa.
- **`ConsolidatorAreaProfile`** — las 35 áreas del catálogo institucional; resuelve nombres
  de área contra `OrganizationalStructures`.
- **`UnifiedCode`** queda como catálogo regulatorio de **solo lectura**: pierde los 9 atributos
  de gestión y gana los descriptivos de la fuente.
- **`Requirement`** pierde `UnifiedCodeId`/`SupportCategoryId` y gana `MasterProductId` +
  **snapshots regulatorios** tomados al crear, para que el PACC no necesite joins y el
  histórico sobreviva a un remapeo posterior del artículo.
- **`SupportCategory` / `SupportCategoryCode`** se retiran por completo.

### Estados

`MasterProductStates`: `Pending` (sin área resuelta → oculto del POA) · `Active` (utilizable) ·
`Requested` (propuesto por un área) · `Disabled` (ausente del archivo; nunca se borra).
`ConsolidatorAreaSources`: `None` · `Imported` · `Rule` · **`Manual`** (congela la fila frente
al motor de reglas).

## Migraciones

| Migración | Qué hace |
|---|---|
| `PlanningM8_MasterDataCatalogs` | Crea las 6 tablas nuevas; aditiva |
| `PlanningM9_RequirementsOnMasterProduct` | `MasterProductId` nullable + snapshots + backfill de 3 pasos y `THROW 51000` si algo no resuelve |
| `PlanningM10_RequirementUnifiedCodeRemoval` | NOT NULL + FK; drop de `UnifiedCodeId`/`SupportCategoryId` |
| `PlanningM11_RetireSupportCategoriesAndUnifiedCodeAttributes` | Drop de las tablas de categorías y de los 9 atributos de `UnifiedCodes` |
| `PlanningM12_MasterDataUpdatedAtDefaults` | `DEFAULT GETDATE()` en `UpdatedAt` de las tablas nuevas (faltaba y el INSERT mandaba NULL) |
| `PlanningM13_WidenMasterDataTextColumns` | `SearchText` 1,000→4,000 y `LegacyExpenseObjectName` 300→2,000, medidos contra el archivo real |

## Importador

`POST api/v1/master-data/import?dryRun=` (multipart) y, solo en Development,
`POST api/v1/master-data/import/from-repository?dryRun=`. Permiso `planning:catalog:manage`.

Lee 6 hojas (ignora `_Listas` y `Diccionario de Datos`) en una sola transacción y en orden de
dependencia: Áreas → UNSPSC → Objetos del Gasto → Plan de Cuentas → Reglas → Artículos, y
termina re-evaluando el motor de reglas sobre lo no ratificado. `dryRun` revierte.

**Los nombres de columna son los del archivo**, no inventados: `ExcelTableRow.Get` devuelve
string vacío cuando la columna no existe, así que un nombre mal escrito no falla — importa
filas vacías en silencio. El test de abajo es la defensa contra esa regresión.

El nivel de un nodo UNSPSC sale de `nivel_fuente` (S/F/C/M) y el padre de la columna del nivel
superior: el código **no** es un prefijo truncado del hijo (`10101500` es la clase de
`10101501`), así que no se puede derivar por longitud.

## Motor de reglas

`IConsolidatorRoutingService`. Compuertas: tipo de artículo, prefijo UNSPSC, palabras clave por
token completo («aire» no casa con «aireación») y **veto por exclusión**. Gana la de menor
prioridad; desempate determinista por validación institucional → confianza base → versión →
clave. Confianza `BaseConfidence +10` (disparó prefijo) `+5` (validada) `−15` (ambigua)
`−20` (área sin resolver). **La ratificación manual siempre gana** y es inmune a
`ReevaluateAsync(onlyUnratified: true)`.

## Frontend

Configuración: `maestra-articulos`, `catalogos-regulatorios` (solo lectura),
`reglas-ruteo` (editor real de reglas con versionado) y `validacion-mapeo` (semáforo
✓/⚠/falta/duplicado + **ratificación masiva por rama UNSPSC**, la palanca para los ~16 mil
artículos pendientes). `rubros` redirige a `maestra-articulos`.

Operativo: el wizard del POA se reduce a **tarea → buscar artículo → cantidad, unidad,
trimestre, justificación**; los códigos y el área se muestran derivados y de solo lectura.
`master-product-picker` incluye «No encuentro lo que necesito» → `POST master-products/request`.
La bandeja gana el segmento **Bandeja consolidadora** y la fusión real; Consolidación agrupa
por artículo.

## Verificación

`MasterDataImportServiceTests` corre el **archivo real en `dryRun`** y compara los conteos por
hoja (18,302 artículos · 70,402 filas UNSPSC · 814 objetos · 3,665 cuentas · 38 reglas ·
35 áreas), y comprueba que no deja rastro en la BD.

`GET master-products/mapping-summary` es la prueba de regresión funcional: ~51% sin objeto del
gasto, ~83% sin cuenta contable, ~88% sin área consolidadora al primer import.
