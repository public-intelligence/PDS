# M5 — Plan anual (POA): workspace real, validación central y flujo de estados

**Depende de:** M4
**Estado:** ✅ Implementado (2026-07-26)

## Implementación realizada (notas sobre el diseño original)

- **Los modelos ya existían**: el M4 del usuario creó las 11 tablas POA (`PlanningM4_Cycles`), así que M5 no requirió migración. Se agregó solo el enum documental `AnnualPlanStatuses`.
- **DECISIÓN — estados en ESPAÑOL**: `PublishAsync` (M4) ya persistía "Sin iniciar" y toda la UI usa los literales del contrato funcional; se adoptó ese vocabulario ("Sin iniciar", "Borrador", "Listo para revisión", "Observado", "En ajustes", "Aprobado", "En ejecución", "Cerrado") en vez del inglés del plan original. El doc-comment de `AnnualPlan` quedó corregido.
- **Sin repositorios**: se siguió el patrón M4 de acceso directo vía `IUnitOfWork.Context`.
- **Backend**: `SequenceService` (MERGE atómico sobre `PlanningSequences`; códigos `{prefijo-área}-NN` y `TR-NN` en servidor), `PoaValidationService` (réplica exacta de rowIssues; el DTO de producto incluye `Issues` y el frontend los usa como fuente de verdad), `AnnualPlanService` (list por ciclo con conteos, workspace con columnas de plantilla, patch parcial con rowversion → 409, submit bloqueante con errores por producto, return/approve con transición vigilada, ciclo Closed → 409), `AnnualPlansController` (autorización: editar = poa:edit + área dueña vía `CanActOnArea`; devolver/aprobar = poa:review).
- **Frontend**: `poa-store.service.ts` — facade con la superficie del store mock usada por las 4 pantallas (adapter DTO↔modelos en español, MonthsMask↔meses[], Q1..Q4↔programación, municipios nombre↔id, custom values por nombre técnico). `planes-list` con selector de ciclo (consultar el pasado) y conteos del servidor; `plan-workspace` + `plan-grid` + `plan-revisar` conectados; **el tab Requerimientos del drawer queda con aviso hasta M7** (stubs sin efectos). `/plan-anual` en `MIGRATED_ROUTE_PREFIXES`.
- **Extra**: `territorio/municipios` ahora acepta `provinciaId` opcional (catálogo completo para el selector); corregidas las URLs hardcodeadas `/api/v1/...` de `planning-cycle.service.ts` y `ciclo-nuevo.component.ts` (M4) → `environment.apiUrl`.

## Pendiente / diferido
- [ ] M7: guard de tareas con requerimientos vivos (TODOs marcados en `UpdateProductAsync`/`DeleteProductAsync`/import).
- [ ] Decidir si cerrar el ciclo congela los planes en "Cerrado" (hoy el cierre del ciclo solo bloquea escrituras vía 409).
- [ ] Prueba manual end-to-end con dos usuarios (área dueña vs. otra área → 403).

## Objetivo

El grid + drawer de 5 tabs del workspace opera contra BD; la validación `rowIssues()` del mock se replica en el backend como única fuente de verdad; flujo someter/devolver/aprobar con permisos por área.

## Entregable verificable

Un usuario de un área edita su POA (productos, unidades con programación trimestral, tareas, municipios del catálogo territorial real, columnas personalizadas de la plantilla del ciclo) y lo somete; Planificación lo devuelve con observación o lo aprueba. El listado de planes filtra por ciclo con columna "Ciclo".

## Modelos (esquema `Planning`)

- **`AnnualPlan`**: OrganizationId, CycleId FK, AreaId + AreaName, Name, Status (NotStarted|Draft|ReadyForReview|Observed|InAdjustments|Approved|InExecution|Closed), Version int, DueDate, GeneralObservation?, `rowversion`; único (CycleId, AreaId). Sin soft delete.
- **`PoaProduct`**: PlanId FK, Code varchar(20) **generado en servidor**, Origin (Pei|Operational), StrategicFocusId?/StrategicObjectiveId?/StrategicProductId? (FK a filas del PEI fijado por el ciclo, Restrict), ProductName(500), OperationalJustification?, Observation?, Order, `rowversion`; único (PlanId, Code).
- **`PoaMeasureUnit`**: ProductId FK, Name(500), TargetValue, TargetType (Absolute|Percentage), VerificationMeans(1000), ResponsibleAreaId + Name, **Q1..Q4 decimal columnas fijas**.
- **`PoaTask`**: ProductId FK, Code "TR-NN" (servidor), Name(500), IsContinuous, **MonthsMask smallint** (bit 0 = enero).
- **`PoaProductMunicipality`**: FK a `Organization.Municipios` existente + MunicipioName snapshot; único (ProductId, MunicipioId).
- **`PoaProductInvolvedArea`**: (ProductId, AreaId + AreaName); único.
- **`PoaProductCustomValue`**: ProductId, TemplateColumnId FK→PlanningTemplateColumn, Value nvarchar(max); único (ProductId, TemplateColumnId).
- **`PlanningSequence`**: OrganizationId, Scope varchar(30) ('POA_PRODUCT','POA_TASK','REQUIREMENT'), ScopeKey varchar(80), NextValue; único (Org, Scope, ScopeKey). `ISequenceService.NextAsync` con `UPDATE ... OUTPUT` bajo la transacción del insert — **los códigos se generan en backend** (resuelve colisiones del cliente).

## Servicios

- `IAnnualPlanService`: ListAsync(orgId, **cycleId?**, areaId?, status?, paginado) — consulta canónica por ciclo; GetWorkspaceAsync (plan + productos completos + columnas de plantilla del ciclo + catálogo PEI del ciclo); Add/Update/DeleteProductAsync (RowVersion, 409 en conflicto); UpsertTasksAsync (guard de requerimientos vivos — se activa en M7); SetCustomValueAsync; SubmitAsync (bloquea si hay issues); ReturnAsync(observación); ApproveAsync.
- `IPoaValidationService`: **réplica exacta de `rowIssues()`** (`planning-store.service.ts:381-417`): producto sin nombre; operativo sin justificación; PEI sin foco+objetivo; ≥1 unidad; por unidad nombre/meta>0/responsable/medios; ΣT1..T4 = meta si Absolute, T4 = meta si Percentage; ≥1 municipio; columnas personalizadas obligatorias.

## Endpoints

`AnnualPlansController`: GET `annual-plans?cycleId=&areaId=&status=`, GET `/{id}`, GET `/{id}/workspace`, POST `/{id}/products`, PUT `/{id}/products/{productId}`, DELETE `/{id}/products/{productId}`, PUT `/{id}/products/{productId}/tasks`, PUT `/{id}/products/{productId}/custom-values`, GET `/{id}/validation`, POST `/{id}/submit`, POST `/{id}/return`, POST `/{id}/approve`.
Autorización: editar = `planning:poa:edit` + `plan.AreaId == context.PlanAreaId` (o `planning:admin`); return/approve = `planning:poa:review`; ciclo Closed → 409 en escrituras.

## Frontend

`annual-plan.service.ts` + adapter (`MonthsMask`↔`meses[]`, `Q1..Q4`↔`programacion`); `planes-list` con columna/filtro Ciclo; workspace/grid/drawer/revisar contra API con autosave por producto (debounce + RowVersion; 409 → recarga de fila); municipios desde `territory.service.ts`; `rowIssues` cliente queda como pre-validación visual (el submit muestra lo del servidor).

## Migración / seeds
`PlanningM5_AnnualPlan`. Sin seeds.

## Criterios de aceptación
1. Someter un plan con issues → 400 con la lista por producto; sin issues → ReadyForReview y Version+1.
2. Usuario de otra área intenta editar → 403.
3. Dos ediciones concurrentes del mismo producto → segunda recibe 409.
4. Devolver plan → Observed con observación visible; aprobar → Approved.
