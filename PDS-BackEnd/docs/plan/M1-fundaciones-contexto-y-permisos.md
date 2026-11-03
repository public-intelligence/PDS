# M1 — Fundaciones: contexto de usuario, permisos, auditoría e infra Excel

**Depende de:** — (primer milestone)
**Estado:** En implementación

## Objetivo

Que el backend sepa *quién es el usuario, de qué área organizacional es y qué puede hacer*, y que exista la plomería común (bitácora de actividad, lectura de Excel, esquema `Planning`) que usan todos los milestones siguientes.

## Entregable verificable

`GET api/v1/planning-context/me` devuelve el área real del usuario logueado (resuelta vía GOBID Personnel M2M → `positionId` → `Position.OrganizationalStructureId` local) y sus permisos efectivos. El shell de Planificación del frontend carga ese contexto tras login; el persona-switcher de demo queda restringido a las pantallas aún mock.

## Diseño

### Resolución de área por login
1. Claim `personnel_id` del JWT (fallback `person_id`).
2. `IGobidPersonnelClient.GetPersonnelAsync(personnelId)` — typed HttpClient con `GobidM2MBearerHandler` (primer encadenamiento real del handler existente). Ruta del endpoint configurable en settings (`GobidPersonnel:PathTemplate`), porque el contrato exacto de GOBID puede cambiar. **Confirmado por el usuario:** el `positionId` que retorna GOBID hace match con `Position.Id` de la BD de PDS.
3. `Position.OrganizationalStructureId` → ascenso por `ParentId` hasta la unidad **nivel 1** (la que formula plan propio; raíz = unidad sin padre).
4. Caché `IMemoryCache` 15 min, clave `planning-ctx:{orgId}:{userId}`; `POST planning-context/me/refresh` invalida.

### Overrides de desarrollo (indicación del usuario)
Sección `PlanningDevOverrides` en appsettings (`Enabled` solo true en Development):
```json
"PlanningDevOverrides": {
  "Enabled": true,
  "Users": [ { "UserId": "<guid>", "GrantAllPermissions": true, "PlanAreaId": null } ]
}
```
Si el `userId` del token está en la lista: otorga todos los permisos `planning:*`; si `PlanAreaId` viene definido, fuerza esa área (simular ser un área concreta). Sirve además de fallback mientras GOBID dev no tenga posiciones pobladas.

### Permisos (checks en servicio, no policies)
`PlanningPermissions` (constantes) + `IPlanningAccessGuard` en `PDS.Service/Authorization`:
`planning:admin`, `planning:cycles:manage`, `planning:config:manage`, `planning:pei:edit`, `planning:pei:approve`, `planning:poa:edit` (+área propia), `planning:poa:review`, `planning:requirements:edit` (+área origen), `planning:requirements:consolidate` (+pertenencia consolidadora), `planning:catalog:read` (M2M).

## Checklist

### Backend
- [x] Modelo `PDS.Domain/Models/PlanningActivityLog.cs` (append-only: OrganizationId, Date, UserId, UserName, Module, Action, Detail, EntityType?, EntityId?)
- [x] Config Fluent en `PDSDbContext` (esquema `Planning`; índices `(OrganizationId, Date)` y `(EntityType, EntityId)`) + DbSet + `AddRepositories()`
- [x] Migración `20260726160202_PlanningM1_Foundations` (aplicada a la BD dev)
- [x] Settings: `GobidPersonnelSettings`, `PlanningDevOverridesSettings`
- [x] `IGobidPersonnelClient` / `GobidPersonnelClient` + registro con `.AddHttpMessageHandler<GobidM2MBearerHandler>()`
- [x] `IPlanningContextService` / `PlanningContextService` (caché + overrides)
- [x] `PlanningPermissions` + `IPlanningAccessGuard` / `PlanningAccessGuard`
- [x] `IPlanningActivityService` / `PlanningActivityService`
- [x] Infra Excel: `PDS.Service/Services/Excel/ExcelTableReader` + `ImportErrorDto`/`ImportResultDto`
- [x] DTOs: `PlanningContextDto`, `GobidPersonnelDto`, `ActivityLogDto`
- [x] `PlanningContextController` (`GET api/v1/planning-context/me`, `POST api/v1/planning-context/me/refresh`)
- [x] `PlanningActivityController` (`GET api/v1/planning-activity` paginado por headers, filtros module/entityId)
- [x] Registros en `ServiceExtensions.AddServices()` / `Program.cs`; secciones appsettings

### Frontend
- [x] `src/app/services/planning/planning-context.service.ts` (HTTP + signal, carga idempotente al entrar al módulo)
- [x] `PlanningShellComponent`: carga el contexto real al entrar; persona-switcher visible solo en rutas mock (lista `MIGRATED_ROUTE_PREFIXES` que cada milestone amplía)

### Pendiente de configuración (usuario)
- [ ] Reemplazar el `UserId` placeholder (`00000000-...`) de `PlanningDevOverrides` en `appsettings.Development.json` con el GUID real del usuario de pruebas (claim `sub` del token).
- [ ] Confirmar con GOBID la ruta real del endpoint Personnel (ajustable en `GobidPersonnel:PathTemplate` sin recompilar).

## Criterios de aceptación
1. `dotnet build` limpio y migración aplicable (`dotnet ef database update`).
2. Con token real en Scalar: `GET planning-context/me` devuelve área y permisos; con el usuario del override devuelve todos los permisos `planning:*`.
3. Usuario sin posición/personnel → `PlanAreaId = null` y sin permisos de edición (solo los del claim).
4. `GET planning-activity` pagina con headers `X-Total`/`Link`.
5. Frontend compila; las pantallas mock siguen funcionando.

## Notas de migración
- Primera tabla del esquema `Planning` — la migración crea el esquema.
- Sin seeds.
