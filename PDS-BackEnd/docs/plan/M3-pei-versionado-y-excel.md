# M3 — PEI: versionado por enmiendas + Excel

**Depende de:** M1
**Estado:** ✅ Implementado (2026-07-26)

## Implementación realizada

- **Modelos** (esquema `Planning`, migración `20260726170335_PlanningM3_StrategicPlan` aplicada a BD dev): `StrategicPlan` (índices únicos filtrados un Draft/un Current por org), `StrategicFocus`, `StrategicObjective`, `StrategicProduct`, `StrategicProductInvolvedArea`, `StrategicMeasureUnit`, `StrategicAnnualTarget` (único (MeasureUnitId, Year)), `StrategicPlanChange`. Todos con `LineageId` donde aplica; **cascada dentro del agregado** (árbol estricto): descartar el Draft elimina el grafo. Strings en vez de enums (convención M2) + enum documental `StrategicPlanStatus`.
- **Servicios**: `StrategicPlanService` (enmiendas con clonado de grafo conservando LineageId vía `IUnitOfWork` transaccional; aprobación Draft→Current/Current→Historical; CRUD del árbol solo sobre Draft; inmutabilidad de metas de años ≤ actual; con historia → descontinuar; `ReplaceMeasureUnits` saneado como `reemplazarUnidadesPei`; registro de cambios + bitácora "PEI") y `StrategicPlanExcelService` (plantilla con años dinámicos + hoja Catálogos con validación de datos; export con niveles solo en su primera fila como el archivo institucional; import dryRun/aplicar con arrastre de celdas en blanco, modos actualizar/reemplazar, inmutabilidad validada por fila, con-historia se conserva con aviso).
- **Controller** `StrategicPlansController` (`strategic-plans`): GET lista/detalle, POST inicial, POST `amendments`, DELETE `amendments/current`, POST `amendments/current/approve`, PUT plan, sub-recursos focuses/objectives/products/measure-units, discontinue de producto y unidad, GET `excel-template`/`export`, POST `import?modo=&dryRun=`. Autorización: `planning:pei:edit` / `planning:pei:approve` vía contexto+guard (patrón M2).
- **Frontend**: `MetasAnuales` pasó a `Record<'a{año}', number>` (años dinámicos); `services/planning/pei-store.service.ts` (facade con la misma superficie del store mock: señales + mutaciones homónimas + adapter DTO→`Pei` + Excel); `pei.component.ts` conectado (estado vacío "Crear PEI", años dinámicos por versión, permisos reales por contexto, modal de importación con preview dryRun, export/plantilla); `/pei` agregado a `MIGRATED_ROUTE_PREFIXES` (persona-switcher oculto en la pantalla real).

## Pendiente de prueba manual
- [ ] Importar el archivo real `PEI 2025 - 2028 - V. de Normas.xlsx` sobre el Draft (nota: la hoja se llama "V. de Normas" y los encabezados reales difieren de la plantilla — usar la plantilla generada como base y pegar el contenido, o renombrar columnas).
- [ ] Aprobar enmienda con usuario MAE (permiso `planning:pei:approve`).
- [ ] Verificar 409 al editar metas de años transcurridos vía import.

## Objetivo

PEI persistido con las reglas del mock aprobado (una versión Vigente, máx. una En elaboración, inmutabilidad de años transcurridos, descontinuación en vez de borrado, registro de cambios) + carga/descarga Excel para que Planificación suba el PEI completo y edite después.

## Entregable verificable

La pantalla PEI opera contra BD: listar versiones (con los ciclos que las usan), abrir/descartar/aprobar enmienda, CRUD de focos/objetivos/productos/unidades, descontinuar, descargar plantilla xlsx, **importar el archivo real `PEI 2025 - 2028 - V. de Normas.xlsx`** y verlo en pantalla.

## Modelos (esquema `Planning`)

- **`StrategicPlan`**: OrganizationId, Name, PeriodStartYear, PeriodEndYear (elimina hardcode a2025..a2028), VersionNumber, Status (Draft|Current|Historical), Reason(2000) (motivo de enmienda), CreatedBy/Name/At, ApprovedBy/Name/At?, PreviousVersionId?. Índices únicos filtrados: **un Draft por org** y **un Current por org** (patrón StructureVersion). Sin soft delete: descartar enmienda = hard-delete del grafo SOLO en Draft (única ruta de borrado, en el servicio).
- **`StrategicFocus`** / **`StrategicObjective`** / **`StrategicProduct`** / **`StrategicMeasureUnit`**: todos con **`LineageId`** (identidad estable entre versiones, se conserva al clonar la enmienda), `Order`; producto y unidad llevan `DiscontinuedFromYear?` + `DiscontinuationReason?`; unidad lleva `ResponsibleAreaId` + Name snapshot.
- **`StrategicProductInvolvedArea`**: ProductId, AreaId + AreaName; único (ProductId, AreaId).
- **`StrategicAnnualTarget`**: MeasureUnitId, Year, Value decimal(18,2); único (MeasureUnitId, Year). **Normaliza MetasAnuales**.
- **`StrategicPlanChange`**: append-only (réplica de CambioPei): Date, UserId, UserName, ChangeType (Added|Edited|Discontinued), Level (Focus|Objective|Product|MeasureUnit), Description.

## Reglas de servicio (réplica del store — el servidor manda)

- `StartAmendmentAsync(reason)`: clona el grafo Current→Draft conservando LineageId; falla si ya hay Draft.
- `DiscardAmendmentAsync` / `ApproveAmendmentAsync` (Draft→Current, Current→Historical, transacción, permiso `planning:pei:approve`).
- Mutaciones solo sobre el Draft (`planning:pei:edit`). Metas con `Year <= añoActual` inmutables. Eliminar elemento con historia (metas > 0 en años transcurridos) → prohibido, usar descontinuación.
- Toda mutación escribe `StrategicPlanChange` + `PlanningActivityLog`.

## Excel

- `GET api/v1/strategic-plans/template` — plantilla con columnas de años **dinámicas del período** + hoja "Catálogos" (áreas nivel 1).
- `GET api/v1/strategic-plans/{id}/export`.
- `POST api/v1/strategic-plans/{id}/import?modo=actualizar|reemplazar&dryRun=` — solo Draft; una fila por unidad de medida; jerarquía por repetición de texto (match normalizado sin acentos); metas de años transcurridos que difieran → error de fila; "Meta total" se ignora al importar.

Columnas: `Foco estratégico | Alineación END | Código objetivo | Objetivo estratégico | Producto estratégico | Áreas involucradas (|) | Unidad de medida | Área responsable | Meta {Y1}..{Yn} | Meta total`.

## Endpoints

`StrategicPlansController`: GET `strategic-plans` (versiones + estado + ciclos que la usan), GET `/{id}` (grafo completo), POST `/amendments`, DELETE `/amendments/current`, POST `/{id}/approve`, sub-recursos `focuses` / `objectives` / `products` / `measure-units` (POST/PUT/DELETE), POST `products/{id}/discontinue`, export/template/import.

## Frontend
`strategic-plan.service.ts` + adapter → interfaces `Pei/FocoEstrategico/...` existentes; el componente PEI pasa a años dinámicos (cambio centralizado en `aniosPei()`).

## Migración / seeds
`PlanningM3_StrategicPlan`. Sin seed — el PEI real entra por Excel.

## Criterios de aceptación
1. Importar el PEI real → árbol completo visible; reimportar en modo actualizar no duplica.
2. Intentar editar meta de año transcurrido → 409/400 con mensaje claro.
3. Aprobar enmienda: la anterior queda Histórica, inmutable, consultable.
4. Descontinuar un producto con historia y verificar badge/estado en pantalla.
