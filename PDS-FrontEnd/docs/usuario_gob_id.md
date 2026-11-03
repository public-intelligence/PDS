# PARTE 1 - Usuario GobID en Responsables: lo que el frontend necesita del backend

**Requisito:** RF-LO-37 · **Pantalla:** UI-14, `/settings/responsables`

## Problema

Al pulsar **Editar** en un responsable, el modal muestra todos los datos excepto **Usuario GobID**. El frontend pide `GET /api/v1/catalogos/responsables/{id}` y carga en el formulario el campo `gobidUserId` tal como llega. Si la respuesta no lo trae, el campo queda vacío.

## Lo que el backend debe hacer

1. **Incluir `gobidUserId` en la respuesta de `GET /api/v1/catalogos/responsables/{id}`**, no solo en el listado.
2. **Usar exactamente el nombre `gobidUserId`**, todo en minúscula después de `gob`. Una propiedad C# `GobIdUserId` se serializa como `gobIdUserId` y el frontend no la reconoce. Usar `GobidUserId` o `[JsonPropertyName("gobidUserId")]`.
3. **Devolver el valor a los administradores de calendario** (permiso `localities:calendar:manage`). A los demás se puede omitir o devolver `null`, porque el frontend solo muestra el campo a administradores.

```json
{
  "id": "…",
  "nombre": "Juan Pérez",
  "cargo": "Cónsul",
  "email": "juan@example.com",
  "telefono": "809-000-0000",
  "gobidUserId": "3f2a9c1e-5b7d-4e8a-9c0f-1a2b3c4d5e6f"
}
```

## DTO

```csharp
public sealed class ResponsableDto
{
    public Guid Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string? Cargo { get; init; }
    public string? Email { get; init; }
    public string? Telefono { get; init; }

    /// <summary>RF-LO-37: `sub` del usuario GobID. Solo para administradores de calendario.</summary>
    public Guid? GobidUserId { get; init; }
}
```

Usar el mismo DTO para el listado (`GET /catalogos/responsables`) y para el detalle (`GET /catalogos/responsables/{id}`), así los dos devuelven el campo.

## Verificación

Con un usuario administrador de calendario, `GET /api/v1/catalogos/responsables/{id}` de un responsable vinculado devuelve `"gobidUserId": "<guid>"`. Al pulsar Editar, el campo **Usuario GobID** aparece con ese valor.


# Parte 2

# Localidad predeterminada de calendario: lo que el frontend necesita del backend

**Requisito:** RF-LO-36 (MUST) · **Pantalla:** UI-04, detalle de localidad · **Tarea front:** T10 · **Bloque backend:** B-M2
**Especificación:** `docs/localities-time-zones-calendars.md` (§6.6 RF-LO-36, §10.4, §10.5, §11)

## 1. Problema

En el detalle de una localidad, el administrador pulsa **"Marcar como predeterminada de calendario"** y el frontend llama a:

```http
PUT https://localhost:7247/api/v1/localidades/59eb70bd-74bb-449a-b24f-b69abb8ff3a6/calendario/predeterminada
```

La respuesta es **404 Not Found** porque el endpoint todavía no existe en el API. El frontend ya está terminado y espera el contrato descrito abajo.

## 2. Contrato del endpoint

| | |
|---|---|
| Método | `PUT` |
| Ruta | `/api/v1/localidades/{id:guid}/calendario/predeterminada` |
| Autenticación | Token de usuario PDS (el mismo de las demás pantallas) |
| Permiso | `localities:calendar:manage` (administrador de calendario). Los gestores de localidad **no** pueden (§10.4). |
| Cuerpo | El frontend envía `{}`. No hay que leer nada del cuerpo. |
| Encabezados | El frontend **no** envía `If-Match`. No exigirlo en este endpoint. |
| Respuesta correcta | **`204 No Content`**, sin cuerpo. (Un `200` con cuerpo también funciona: el frontend lo ignora.) |

Llamada exacta del frontend (`src/app/services/locality-calendar/locality-calendar.service.ts`):

```ts
marcarPredeterminada(id: string): Observable<void> {
  return this.http.put<void>(`${this.base}/${id}/calendario/predeterminada`, {});
}
```

Después de un 2xx, el frontend muestra "Localidad predeterminada de calendario actualizada" y vuelve a pedir `GET /api/v1/localidades/{id}`.

### Respuestas de error

Usar el `ErrorDto` existente: `{ "title": "…", "detail": { "<campo>": ["mensaje"] } }`, con mensajes en español. El frontend muestra en un toast los mensajes de `detail`, o el `title` si `detail` viene vacío.

| Estado | Cuándo | `detail` sugerido |
|---|---|---|
| 401 | Sin token o token vencido | (lo maneja el middleware) |
| 403 | El usuario no tiene `localities:calendar:manage` | `{ "permiso": ["Solo los administradores de calendario pueden elegir la localidad predeterminada."] }` |
| 404 | La localidad no existe, está eliminada (`DeletedAt` no nulo) o es de otra organización | `{ "localidad": ["La localidad no existe."] }` |
| 409 | La localidad no está **ACTIVA** (suspendida o cerrada) | `{ "estado": ["Solo una localidad activa puede ser la predeterminada de calendario."] }` |

> La regla de "solo activa" sale de RF-LO-36 ("Exactly one **active** locality per organization…"). El frontend hoy no oculta el botón para localidades suspendidas o cerradas, así que la validación debe hacerla el backend.

## 3. Reglas de negocio

1. **Idempotente.** Si la localidad ya es la predeterminada, responder `204` sin cambios ni auditoría.
2. **Una sola por organización, en una transacción** (AC1 de RF-LO-36). Dentro de la misma transacción:
   1. Poner `EsLocalidadCalendarioPredeterminada = false` en la predeterminada actual de la organización (si existe).
   2. Poner `EsLocalidadCalendarioPredeterminada = true` en la localidad `{id}`.

   **El orden importa.** El índice único filtrado (sección 5) falla si las dos filas quedan en `true` aunque sea un instante. Con EF Core, guardar el paso 1 (`SaveChangesAsync`) antes del paso 2, ambos dentro de la misma transacción explícita.
3. **Auditoría (RF-LO-44).** Una fila en `LocalidadAuditoria` **por cada localidad afectada** (la anterior y la nueva). Usar la acción `Modified` con el diff `{ "esLocalidadCalendarioPredeterminada": { "from": …, "to": … } }` y `Canal = PdsUi`.
4. **Feed de cambios (RF-LO-43).** RF-LO-43 no incluye este cambio en la lista de lo que incrementa `CalendarioVersion`, porque el calendario efectivo de cada localidad no cambia. **No** incrementar la versión ni agregar filas a `CalendarioCambios`. Si WF necesita enterarse del cambio de respaldo, se define aparte.
5. **Concurrencia.** Si dos administradores marcan localidades distintas al mismo tiempo, el índice único hace que una transacción falle. Atrapar la `DbUpdateException` por violación de ese índice y responder `409` con `{ "concurrencia": ["Otra persona cambió la localidad predeterminada. Recargue e intente de nuevo."] }`.

## 4. Lo que el frontend lee en `GET /api/v1/localidades/{id}`

El detalle de la localidad **debe** incluir este campo (§11, cambios a endpoints existentes):

```json
{
  "id": "59eb70bd-74bb-449a-b24f-b69abb8ff3a6",
  "nombreOficial": "…",
  "esLocalidadCalendarioPredeterminada": true,
  "calendarioVersion": 12
}
```

El frontend lo usa para:

- mostrar la insignia **"Localidad predeterminada de calendario"** cuando es `true`;
- ocultar el botón "Marcar como predeterminada de calendario" cuando es `true`.

Si el campo no viene, el frontend lo trata como `false`. El PUT funcionaría, pero la insignia nunca se vería y el botón seguiría visible.

## 5. Base de datos

En la tabla `Localidades`:

| Columna | Tipo | Restricciones |
|---|---|---|
| `EsLocalidadCalendarioPredeterminada` | `bit` | NOT NULL, default `0` |

Índice único filtrado:

```sql
CREATE UNIQUE INDEX UX_Localidades_Org_CalendarioPredeterminada
    ON Localidades (OrganizationId)
    WHERE EsLocalidadCalendarioPredeterminada = 1 AND DeletedAt IS NULL;
```

En EF Core:

```csharp
builder.Property(l => l.EsLocalidadCalendarioPredeterminada)
       .IsRequired()
       .HasDefaultValue(false);

builder.HasIndex(l => l.OrganizationId)
       .IsUnique()
       .HasFilter("[EsLocalidadCalendarioPredeterminada] = 1 AND [DeletedAt] IS NULL")
       .HasDatabaseName("UX_Localidades_Org_CalendarioPredeterminada");
```

**Migración de datos (§13, paso 5):** marcar la localidad `SEDE_CENTRAL` activa de cada organización. Si una organización no tiene exactamente una, dejarla sin marcar y reportarla: un administrador debe elegirla desde la UI.

## 6. DTOs

### 6.1 Request

**No hay DTO de request.** El cuerpo `{}` se ignora. No poner `[FromBody]` en la acción.

### 6.2 Response del PUT

Ninguno (`204 No Content`).

### 6.3 Detalle de localidad (cambio al DTO existente)

Agregar al DTO que devuelve `GET /api/v1/localidades/{id}` (por ejemplo `LocalidadDetalleDto`):

```csharp
public sealed class LocalidadDetalleDto
{
    // … propiedades existentes …

    /// <summary>RF-LO-36: localidad de respaldo y plantilla de calendario de la organización.</summary>
    public bool EsLocalidadCalendarioPredeterminada { get; init; }

    /// <summary>RF-LO-43: versión del calendario efectivo.</summary>
    public long CalendarioVersion { get; init; }
}
```

Con la serialización camelCase por defecto se envían como `esLocalidadCalendarioPredeterminada` y `calendarioVersion`, que son los nombres que lee el frontend (`src/app/models/institutional-locality.model.ts`).

### 6.4 Error (DTO existente, sin cambios)

```csharp
public sealed class ErrorDto
{
    public string Title { get; init; } = string.Empty;
    public Dictionary<string, string[]> Detail { get; init; } = new();
}
```

### 6.5 M2M (no lo usa el frontend, pero es parte de RF-LO-36)

`GET /api/v1/m2m/organization/{organizationId}/calendario/localidad-predeterminada`, protegido con `RequireM2MServiceAccountAsync(LocalidadPermissions.CalendarRead)`:

```csharp
public sealed class LocalidadPredeterminadaDto
{
    public Guid LocalidadId { get; init; }
    public string Codigo { get; init; } = string.Empty;
    public string NombreOficial { get; init; } = string.Empty;
    public string ZonaHoraria { get; init; } = string.Empty;
    public long CalendarioVersion { get; init; }
}
```

Responder `404` si la organización todavía no tiene localidad predeterminada.

## 7. Implementación sugerida

### 7.1 Permiso (`PDS.Service/Authorization/LocalidadPermissions.cs`)

```csharp
public const string CalendarManage = "localities:calendar:manage";
public const string CalendarRead   = "localities:calendar:read";
```

### 7.2 Controlador

Ponerlo junto a los demás endpoints `…/calendario/…` de la localidad. `ApiPrefixConvention` ya agrega `api/v1`, así que la ruta de la acción es relativa a `localidades`:

```csharp
[HttpPut("{id:guid}/calendario/predeterminada")]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(typeof(ErrorDto), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ErrorDto), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ErrorDto), StatusCodes.Status409Conflict)]
public async Task<IActionResult> MarcarPredeterminada(Guid id, CancellationToken ct)
{
    await _calendarioService.MarcarLocalidadPredeterminadaAsync(id, ct);
    return NoContent();
}
```

### 7.3 Servicio

```csharp
public async Task MarcarLocalidadPredeterminadaAsync(Guid localidadId, CancellationToken ct)
{
    // Solo el administrador de calendario (§10.4). Lanza 403 con detail "permiso".
    _accessGuard.RequirePermission(LocalidadPermissions.CalendarManage);

    var orgId = _currentUser.OrganizationId;

    var localidad = await _db.Localidades
        .Include(l => l.Estado)
        .SingleOrDefaultAsync(l => l.Id == localidadId
                                && l.OrganizationId == orgId
                                && l.DeletedAt == null, ct)
        ?? throw new NotFoundException("localidad", "La localidad no existe.");

    if (localidad.EsLocalidadCalendarioPredeterminada)
        return; // Idempotente.

    if (localidad.Estado.Codigo != "ACTIVA")
        throw new ConflictException("estado",
            "Solo una localidad activa puede ser la predeterminada de calendario.");

    await using var tx = await _db.Database.BeginTransactionAsync(ct);
    try
    {
        var anterior = await _db.Localidades
            .SingleOrDefaultAsync(l => l.OrganizationId == orgId
                                    && l.EsLocalidadCalendarioPredeterminada
                                    && l.DeletedAt == null, ct);

        if (anterior is not null)
        {
            anterior.EsLocalidadCalendarioPredeterminada = false;
            _auditoria.Registrar(anterior, LocalidadAccion.Modified, CanalAuditoria.PdsUi,
                diff: new { esLocalidadCalendarioPredeterminada = new { from = true, to = false } });
            await _db.SaveChangesAsync(ct); // Antes de marcar la nueva: índice único filtrado.
        }

        localidad.EsLocalidadCalendarioPredeterminada = true;
        _auditoria.Registrar(localidad, LocalidadAccion.Modified, CanalAuditoria.PdsUi,
            diff: new { esLocalidadCalendarioPredeterminada = new { from = false, to = true } });
        await _db.SaveChangesAsync(ct);

        await tx.CommitAsync(ct);
    }
    catch (DbUpdateException ex) when (EsViolacionIndiceUnico(ex))
    {
        throw new ConflictException("concurrencia",
            "Otra persona cambió la localidad predeterminada. Recargue e intente de nuevo.");
    }
}
```

Los nombres `_accessGuard`, `_currentUser`, `_auditoria`, `NotFoundException` y `ConflictException` son de ejemplo. Usar los equivalentes que ya existan en el proyecto: `ICalendarioAccessGuard`, el registro de `LocalidadAuditoria` y las excepciones que el middleware convierte en `ErrorDto`.

## 8. Cómo verificar

1. **Sin permiso:** con un usuario sin `localities:calendar:manage`, el PUT responde `403`.
2. **Con permiso:**
   ```bash
   curl -k -X PUT -H "Authorization: Bearer <token>" -H "Content-Type: application/json" -d "{}" \
     https://localhost:7247/api/v1/localidades/59eb70bd-74bb-449a-b24f-b69abb8ff3a6/calendario/predeterminada
   ```
   Debe responder `204`.
3. `GET /api/v1/localidades/59eb70bd-…` devuelve `"esLocalidadCalendarioPredeterminada": true`.
4. La predeterminada anterior ahora devuelve `false` (AC1).
5. Repetir el PUT: `204` sin filas nuevas de auditoría.
6. Con una localidad suspendida o cerrada: `409` con `detail.estado`.
7. Con un GUID inexistente: `404`.
8. En el frontend (flag `calendarioLaboral` encendida y usuario administrador de calendario): el botón desaparece, aparece la insignia y se muestra el toast de éxito.
