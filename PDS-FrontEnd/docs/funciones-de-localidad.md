# Funciones de localidad: backend contract for the frontend

Page: `/settings/funciones-localidad` (UI-13, RF-LO-45, backend blocker B-M6).
The frontend page, modal, service and model are already built. Once the backend serves the endpoints below exactly as described, the page works with no frontend changes.

Base URL: `{apiUrl}` = `/api/v1/`. Same auth (Bearer token) and same pattern as `catalogos/tipos-localidad`.

## Endpoints

| UI element | Method | Route | Query / body | Response |
|---|---|---|---|---|
| Table + pagination (no search text) | GET | `/api/v1/catalogos/funciones-localidad` | `?pageNumber=1&pageSize=20` | `200`, `FuncionLocalidadDto[]` + pagination headers |
| Search bar ("Buscar...") | GET | `/api/v1/catalogos/funciones-localidad/search` | `?q=emb&pageNumber=1&pageSize=20` | `200`, `FuncionLocalidadDto[]` + pagination headers |
| "Editar" (loads the modal) | GET | `/api/v1/catalogos/funciones-localidad/{id}` | none | `200`, `FuncionLocalidadDto` |
| "Nueva función" → "Crear" | POST | `/api/v1/catalogos/funciones-localidad` | `FuncionLocalidadUpsertDto` | `201`, `FuncionLocalidadDto` (empty body is also accepted) |
| "Editar" → "Guardar" | PUT | `/api/v1/catalogos/funciones-localidad/{id}` | `FuncionLocalidadUpsertDto` | `200`, `FuncionLocalidadDto` |
| "Eliminar" | DELETE | `/api/v1/catalogos/funciones-localidad/{id}` | none | `204` |

`{id}` is a GUID string.

## DTOs

```jsonc
// FuncionLocalidadDto (response)
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "codigo": "EMBAJADA",
  "nombre": "Embajada",
  "categoria": "Diplomatica",   // string, NOT a number
  "descripcion": "Texto opcional" // or null
}

// FuncionLocalidadUpsertDto (POST and PUT body)
{
  "codigo": "EMBAJADA",          // required
  "nombre": "Embajada",          // required
  "categoria": "Diplomatica",    // required
  "descripcion": "Texto opcional" // optional; may be omitted
}
```

`categoria` enum (`CategoriaFuncionLocalidad`): `"Diplomatica"`, `"Consular"`, `"Multilateral"`.
It must be serialized as a **string** in both directions (e.g. `JsonStringEnumConverter`). The frontend maps these exact values to the labels "Diplomática", "Consular" and "Multilateral".

## Pagination

The list and search endpoints return a **plain JSON array** in the body. Pagination goes in response headers:

| Header | Meaning |
|---|---|
| `x-current-page` | Current page (1-based) |
| `x-page-count` | Total number of pages |
| `x-page-size` | Page size used |
| `x-total` | Total number of records matching the query |

These headers must be listed in CORS `Access-Control-Expose-Headers`, or the browser hides them and the paginator shows 0 records.
The page sizes the user can pick are 5, 10, 20, 50 and 100 (default 20).

## Rules

- **Search (`q`):** case-insensitive "contains" match on `codigo` and `nombre` (`descripcion` too if cheap). Same pagination as the list.
- **Sort order:** by `codigo` or `nombre`, ascending, stable between pages.
- **`codigo` is unique** among non-deleted records. A duplicate on POST returns `409` with key `codigo`.
- **`codigo` is immutable.** The PUT body still sends it (unchanged); ignore it, or return `400` with key `codigo` if it differs.
- **PUT replaces the whole record.** If `descripcion` is omitted, store `null`. That is how the user clears it.
- **Soft delete** (`DeletedAt`). Deleted records never appear in list, search or get-by-id (`404`).
- **Delete in use:** if any locality uses the function, return `409` with a Spanish message, e.g. `"No se puede eliminar: hay localidades que usan esta función."`. The frontend shows that message as-is.
- **Seed** (Appendix C of the spec):

| codigo | nombre | categoria |
|---|---|---|
| `EMBAJADA` | Embajada | `Diplomatica` |
| `MISION_PERMANENTE` | Misión permanente ante organismo internacional | `Multilateral` |
| `CONSULADO_GENERAL` | Consulado general | `Consular` |
| `SECCION_CONSULAR` | Sección consular | `Consular` |

## Errors

Use the existing `ErrorDto` shape, with messages in Spanish. The frontend shows every message in `detail` in a toast.

```json
{
  "title": "Error de validación",
  "detail": {
    "codigo": ["Ya existe una función con el código EMBAJADA."]
  }
}
```

| Status | When | `detail` key |
|---|---|---|
| 400 | Missing `codigo`, `nombre` or `categoria`; unknown `categoria` | the field name |
| 404 | `id` not found or deleted | — |
| 409 | Duplicate `codigo` | `codigo` |
| 409 | Delete of a function used by localities | `funcion` (any key works; the message is shown) |

## Out of scope for this page (needed later by T31 and T32)

The locality form and list need these, but this page does not:

- `funcionIds: string[]` in locality POST and PUT. `SECCION_CONSULAR` without `EMBAJADA` returns 400 with key `funciones`.
- `funciones` in the locality detail and list DTOs.
- `funcionId` and `funcionCodigo` filters on the locality list.
