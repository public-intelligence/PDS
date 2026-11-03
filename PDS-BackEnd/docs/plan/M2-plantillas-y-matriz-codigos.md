# M2 — Configuración: multi-plantilla del plan anual + matriz de códigos

**Depende de:** M1
**Estado:** Implementado

## Objetivo

Plantillas del plan anual versionables y múltiples (las columnas configurables pasan a pertenecer a una plantilla), y el modelo flexible de la **matriz de códigos unificada** (compras UNSPSC + DIGEPRES + DIGECOB + área consolidadora) con endpoint M2M para que otros sistemas (inventarios, compras) la consulten como fuente única de verdad.

## Entregable verificable

- `configuracion-pds/plantillas` gestiona plantillas reales (crear, columnas, activar).
- `configuracion-pds/rubros` (re-etiquetada "Matriz de códigos") navega la matriz real con herencia de área consolidadora.
- `GET api/v1/unified-codes` responde con token de usuario **y** con token M2M (`planning:catalog:read` + `access:M2M`).

## Modelos (esquema `Planning`)

- **`PlanningTemplate`**: OrganizationId, Name(250), Description?, Version int, IsActive bit, soft delete; único filtrado (Org, Name).
- **`PlanningTemplateColumn`**: TemplateId FK, VisibleName(150), TechnicalName(100), Type enum string (Text|Number|Currency|Select), Required, Visible, Origin (Base|Custom), OptionsJson?, Order, Version, CreatedBy/Name/At; único (TemplateId, TechnicalName); soft delete.
- **`UnifiedCode`**: OrganizationId, Level enum (Segment|Family|Class|Material), Code varchar(20) UNSPSC, Name(500), ParentId self-FK, atributos **nullable con herencia hacia abajo**: ConsolidatorAreaId FK→OrganizationalStructures + Name snapshot, HasCost?, Executor? (Requester|Consolidator), GeneratesPurchase?, Mergeable?, SuggestedUnit?, ReferencePrice?, AnticipationDays?, InstitutionallyEnabled bit default 1. Soft delete. Único filtrado (Org, Code); índices (Org, ParentId), (Org, ConsolidatorAreaId).
- **`UnifiedCodeAlias`**: UnifiedCodeId FK, System varchar(30) ('DIGEPRES','DIGECOB','INVENTARIO',…), Code(50), Name?; único (UnifiedCodeId, System, Code). Absorbe lo que traiga el archivo consolidado sin migración estructural.
- **`SupportCategory`** + **`SupportCategoryCode`** (puente a subárboles de la matriz): las categorías se mantienen como **agrupador UX** del wizard de requerimientos; la **autoridad de ruteo es exclusivamente `ConsolidatorAreaId` heredado**. La entidad `ReglaRuteo` del mock desaparece (trazabilidad vía PlanningActivityLog).

## Endpoints

- `PlanningTemplatesController`: GET/POST `planning-templates`, GET/PUT `/{id}`, POST `/{id}/columns`, PUT `/{id}/columns/{columnId}`, POST `/{id}/activate` — permiso `planning:config:manage`.
- `UnifiedCodesController`: GET `unified-codes` (árbol/paginado; filtros level/parentId/q/enabled), GET `/{id}` (atributos efectivos resueltos por herencia), POST, PUT `/{id}`, PUT `/{id}/consolidator-area`. Lectura usuario+M2M; escritura `planning:config:manage`.
- `SupportCategoriesController`: CRUD + PUT `/{id}/codes`.

## Frontend
`planning-template.service.ts`, `unified-code.service.ts`, `support-category.service.ts` (+adapters a `ColumnaConfig` y árbol de rubros); conectar `plantillas.component.ts` y `rubros.component.ts`; rehacer fino `reglas-ruteo.component.ts` (categoría + consolidadora por nodo). Techos sigue mock.

## Migración / seeds
`PlanningM2_TemplatesAndCodes`. Seeds idempotentes: plantilla "Formulario de Cadena de Valor (DPD-FO-01)" (columnas base + custom: Riesgo identificado, Acción de mitigación, Recursos no financieros); subconjunto demo de matriz (2 segmentos, ~20 materiales) **solo en dev** para probar M7 antes del archivo final.

## Criterios de aceptación
1. Crear una segunda plantilla y activarla sin afectar la histórica.
2. Asignar consolidadora a una Familia y verificar que un Material descendiente la hereda (atributos efectivos).
3. `GET unified-codes` con token M2M válido responde 200; sin permiso `planning:catalog:read` responde 403.
