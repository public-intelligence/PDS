# Guía de Integración M2M para Sistemas de Terceros (PDS API)

## 1. Visión General
Esta documentación define la interfaz **Machine-to-Machine (M2M)** del Sistema de Planificación y Desarrollo Institucional (**PDS**). Permite a sistemas externos de terceros realizar operaciones de consulta automatizada de datos maestros (**Localidades**, **Artículos**, **Áreas del Organigrama** y **Rubros/Clasificadores**) y ejecutar acciones de **escritura (creación y actualización) en la Maestra Institucional de Artículos**.

---

## 2. Autenticación y Autorización M2M

### 2.1 Esquema de Autenticación
Todas las peticiones a la API M2M requieren un **JSON Web Token (JWT)** válido enviado en el encabezado HTTP:

```http
Authorization: Bearer <token_jwt_m2m>
Content-Type: application/json
```

### 2.2 Requisitos del Token M2M
El token JWT expedido para la aplicación o cuenta de servicio consumidora debe contener los siguientes claims mínimos:

| Claim | Tipo | Descripción | Ejemplo |
| :--- | :--- | :--- | :--- |
| `sub` | `String (GUID)` | Identificador único de la cuenta de servicio consumidora | `"b8c9d123-4567-89ab-cdef-0123456789ab"` |
| `organization_id` | `Integer` | Identificador de la institución u organización tenant | `1` |
| `is_service_account` | `String/Boolean` | Indicador de cuenta automatizada M2M | `"true"` |
| `permissions` | `Array[String]` | Permisos concedidos. Debe contener `"access:M2M"` | `["access:M2M"]` |

---

## 3. Catálogo de Endpoints M2M

**URL Base de la API**: `https://<pds-api-domain>/api/v1/m2m/organization/{organizationId}`

### Resumen de Endpoints

| Categoría | Método | RUTA | Descripción |
| :--- | :---: | :--- | :--- |
| **Localidades** | `GET` | `/localidades` | Lista completa (sin paginar) y filtrable de las localidades de la organización |
| **Artículos** | `GET` | `/articulos` | Listado paginado de la Maestra Institucional de Artículos |
| **Artículos** | `GET` | `/articulos/{id}` | Obtiene el detalle técnico y mapeos de un artículo |
| **Artículos** | `POST` | `/articulos` | **[ESCRITURA]** Crea un nuevo artículo en la Maestra |
| **Artículos** | `PUT` | `/articulos/{id}` | **[ESCRITURA]** Actualiza un artículo o sus mapeos |
| **Organigrama** | `GET` | `/organigrama/areas` | Obtiene las unidades y áreas organizacionales de la institución |
| **Rubros** | `GET` | `/rubros/objetos-gasto` | Consulta de Objetos del Gasto (Clasificador Presupuestario DIGEPRES) |
| **Rubros** | `GET` | `/rubros/areas-consolidadoras` | Perfiles de áreas consolidadoras y rubros de compra institucionales |
| **Rubros** | `GET` | `/rubros/cuentas-contables` | Consulta paginada del Plan de Cuentas Contables (DIGECOB - M8) |
| **UNSPSC** | `GET` | `/unspsc` | Consulta paginada y filtrado jerárquico del Catálogo UNSPSC DGCP (~50k nodos) |

---

## 4. Especificación Detallada de Endpoints y JSON Schemas

### 4.1 Localidades (`GET /localidades`)

Lista las **localidades de la organización** (sedes, consulados, oficinas; RF-LO-19 y RF-LO-52). Es la ruta que usa SDS para el selector de localidad en los procesos de servicio.

* **Lista completa, sin paginar:** una sola llamada devuelve **todas** las localidades que cumplen los filtros, en un arreglo JSON, ordenadas por `nombreOficial`. No hay encabezados `X-Total`, `X-Page-*` ni `Link`, y `pageNumber` / `pageSize` se ignoran. (La lista de la interfaz de PDS, `GET /api/v1/localidades`, sigue paginada.)
* **Eliminadas:** nunca se devuelven, con o sin filtros.
* **Sin filtros** se devuelven todos los estados. Para obtener solo las operativas, envíe `estadoCodigo=ACTIVA` (excluye `SUSPENDIDA_TEMP`, `EN_CIERRE` e inactivas).
* **Autenticación:** requiere una cuenta de servicio con `access:M2M`. Un token de usuario recibe 403.
* **Los ID son GUID.**

#### Parámetros de Consulta (Query Parameters)

Todos son opcionales y se combinan con AND. Los filtros por código no distinguen mayúsculas, y se prefieren en las integraciones porque los GUID cambian entre ambientes.

| Parámetro | Tipo | Descripción |
| :--- | :---: | :--- |
| `estadoCodigo` | string | Código del estado, por ejemplo `ACTIVA` |
| `paisCodigo` | string | Código ISO del país, por ejemplo `US` |
| `funcionCodigo` | string | Código de la función, por ejemplo `CONSULADO_GENERAL` |
| `paisId`, `funcionId`, `estadoId` | GUID | Los mismos filtros por identificador |
| `nombre`, `codigo` | string | Búsqueda parcial por nombre oficial/corto o por código |
| `tipoId`, `naturalezaId`, `regionId`, `provinciaId`, `municipioId`, `unidadId`, `instrumentoLegalId` | GUID | Filtros por identificador |
| `servicioId` | GUID | Sigue disponible, pero ninguna integración lo usa (el filtro por servicio está fuera de alcance, D16) |

#### Ejemplo de Llamada `cURL`
```bash
curl -X GET "https://api.pds.gob.do/api/v1/m2m/organization/1/localidades?estadoCodigo=ACTIVA&paisCodigo=US&funcionCodigo=CONSULADO_GENERAL" \
  -H "Authorization: Bearer <TOKEN_M2M>" \
  -H "Content-Type: application/json"
```

#### Respuesta Ejemplo (`HTTP 200 OK`)
```json
[
  {
    "id": "3f0c…",
    "codigo": "CG-NY",
    "nombreOficial": "Consulado General en Nueva York",
    "estadoCodigo": "ACTIVA",
    "paisId": "…",
    "paisCodigo": "US",
    "paisNombre": "Estados Unidos",
    "ciudad": "Nueva York",
    "provinciaNombre": null,
    "municipioNombre": null,
    "zonaHoraria": "America/New_York",
    "funciones": ["CONSULADO_GENERAL"]
  }
]
```

El ítem también trae `tipoLocalidadId`, `tipoNombre`, `estadoLocalidadId`, `estadoNombre`, `estadoColor`, `provinciaId`, `municipioId`, `latitud` y `longitud`.

#### Alias en desuso (`GET /m2m/organization/{organizationId}/locality`)

Alias **en desuso** de `…/localidades`: mismo comportamiento y mismos filtros, y responde con el encabezado `Deprecation: true`. Se retirará cuando WF cambie su operación del tool registry a `/localidades`.

### 4.1.1 Localidades por ID (`GET /m2m/localidades/by-ids`)

`GET /m2m/localidades/by-ids?localidadIds={guid}&localidadIds={guid}[&organizationId={n}]` devuelve los mismos ítems para los ID indicados.

* Las localidades eliminadas no se devuelven.
* `organizationId` es opcional: **con él** solo se devuelven localidades de esa organización (un ID de otra organización no devuelve nada). Se recomienda enviarlo siempre.

#### Pendiente de coordinar con SDS (LOC 19, RF-LO-51)

Estos cambios rompen a los clientes que hoy usan un token de usuario, y se activarán cuando SDS confirme y migre su token de cuenta de servicio:

* El alias `…/locality` y `by-ids` exigirán cuenta de servicio con `access:M2M` (hoy dejan pasar tokens que no son de servicio).
* `organizationId` pasará a ser obligatorio en `by-ids`.
* `locality`, `locality/{localityId}` y `by-ids` pasarán a llevar el prefijo `/api/v1`. Hoy responden en `/m2m/...` sin él.

---

### 4.2 Listado de Artículos (`GET /articulos`)

Consulta paginada de la Maestra Institucional con filtros por código UNSPSC, estado del producto o áreas consolidadoras.

#### Parámetros de Consulta (Query Parameters)
* `q` *(opcional, string)*: Búsqueda libre en código de artículo o descripción.
* `state` *(opcional, string)*: Estado del artículo (`Active`, `Pending`, `Disabled`).
* `unspscPrefix` *(opcional, string)*: Prefijo UNSPSC (ej. `4321` para tecnología).
* `consolidatorAreaId` *(opcional, GUID)*: Filtrar por área consolidadora ruteada.
* `page` *(opcional, int, default: 1)*: Número de página.
* `pageSize` *(opcional, int, default: 20)*: Tamaño de página.

#### Ejemplo de Llamada `cURL`
```bash
curl -X GET "https://api.pds.gob.do/api/v1/m2m/organization/1/articulos?unspscPrefix=4321&page=1&pageSize=2" \
  -H "Authorization: Bearer <TOKEN_M2M>"
```

#### Respuesta Ejemplo (`HTTP 200 OK`)
```json
{
  "pageNumber": 1,
  "pageSize": 20,
  "totalPages": 45,
  "totalRecords": 890,
  "value": [
    {
      "id": "e3b0c442-98fc-4c14-9662-7f97b6e92718",
      "code": "ART-43211503-01",
      "name": "Computador Portátil Intel Core i7 16GB RAM 512GB SSD",
      "itemType": "Goods",
      "unspscCode": "43211503",
      "unspscName": "Computadores portátiles",
      "expenseObjectCode": "2.6.1.1",
      "expenseObjectName": "Maquinaria y Equipo Informático",
      "accountingAccountCode": "1.2.3.1",
      "accountingAccountName": "Equipos de Computación",
      "consolidatorAreaId": "8f3b2a1e-5c4d-4e3a-9b1c-8f7e6d5c4b3a",
      "consolidatorAreaName": "Dirección de Tecnologías de la Información (DTI)",
      "consolidatorAreaSource": "Rule",
      "requiresReview": false,
      "state": "Active"
    }
  ]
}
```

---

### 4.3 Creación de Artículos (`POST /articulos`) — [ESCRITURA]

Permite a un sistema tercero registrar un nuevo bien o servicio en la Maestra Institucional.

#### JSON Schema de Petición (`CreateMasterProductDto`)
```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "required": ["code", "name", "itemType"],
  "properties": {
    "code": { "type": "string", "example": "ART-43211503-09" },
    "name": { "type": "string", "example": "Servidor Rack 2U Dual Xeon 64GB RAM" },
    "englishName": { "type": ["string", "null"] },
    "definition": { "type": ["string", "null"] },
    "unspscCode": { "type": ["string", "null"], "example": "43211501" },
    "itemType": { "type": "string", "enum": ["Goods", "Services", "Works"], "example": "Goods" },
    "measureUnit": { "type": ["string", "null"], "example": "Unidad" },
    "referencePrice": { "type": ["number", "null"], "example": 185000.00 },
    "currency": { "type": "string", "default": "DOP" },
    "expenseObjectId": { "type": ["string", "null"] },
    "accountingAccountId": { "type": ["string", "null"] },
    "consolidatorAreaId": { "type": ["string", "null"] }
  }
}
```

#### Ejemplo de Llamada `cURL`
```bash
curl -X POST "https://api.pds.gob.do/api/v1/m2m/organization/1/articulos" \
  -H "Authorization: Bearer <TOKEN_M2M>" \
  -H "Content-Type: application/json" \
  -d '{
    "code": "ART-EXT-2026-001",
    "name": "Licencia Anual de Software de Ciberseguridad Cloud",
    "itemType": "Services",
    "unspscCode": "43233200",
    "measureUnit": "Licencia",
    "referencePrice": 45000.00,
    "currency": "DOP"
  }'
```

#### Respuesta Ejemplo (`HTTP 201 Created`)
```json
{
  "id": "7a9b8c6d-5e4f-3a2b-1c0d-9e8f7a6b5c4d",
  "organizationId": 1,
  "code": "ART-EXT-2026-001",
  "name": "Licencia Anual de Software de Ciberseguridad Cloud",
  "itemType": "Services",
  "unspscCode": "43233200",
  "unspscName": "Software de seguridad y protección",
  "measureUnit": "Licencia",
  "referencePrice": 45000.00,
  "currency": "DOP",
  "expenseObjectCode": "2.2.8.2",
  "expenseObjectName": "Servicios de Informática y Sistemas",
  "consolidatorAreaName": "Dirección de Tecnologías de la Información (DTI)",
  "consolidatorAreaSource": "Rule",
  "state": "Active",
  "createdAt": "2026-07-30T23:55:00Z"
}
```

---

### 4.4 Actualización de Artículo (`PUT /articulos/{id}`) — [ESCRITURA]

Permite modificar los atributos o mapeos regulatorios de un artículo registrado.

#### Ejemplo de Llamada `cURL`
```bash
curl -X PUT "https://api.pds.gob.do/api/v1/m2m/organization/1/articulos/7a9b8c6d-5e4f-3a2b-1c0d-9e8f7a6b5c4d" \
  -H "Authorization: Bearer <TOKEN_M2M>" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Licencia Anual de Software de Ciberseguridad Cloud Enterprise (Actualizada)",
    "referencePrice": 48500.00,
    "state": "Active"
  }'
```

---

### 4.5 Áreas del Organigrama (`GET /organigrama/areas`)

Retorna las áreas organizacionales institucionales activas con su jerarquía estructural.

#### Ejemplo de Llamada `cURL`
```bash
curl -X GET "https://api.pds.gob.do/api/v1/m2m/organization/1/organigrama/areas" \
  -H "Authorization: Bearer <TOKEN_M2M>"
```

#### Respuesta Ejemplo (`HTTP 200 OK`)
```json
{
  "organizationId": 1,
  "organizationName": "Organización de ejemplo",
  "versionNumber": 1,
  "units": [
    {
      "id": "area-dti-guid",
      "code": "DTI",
      "name": "Dirección de Tecnologías de la Información",
      "unitType": "Dirección",
      "parentId": "area-despacho-guid",
      "isConsolidatorArea": true
    },
    {
      "id": "area-daf-guid",
      "code": "DAF",
      "name": "Dirección Administrativa y Financiera",
      "unitType": "Dirección",
      "parentId": "area-despacho-guid",
      "isConsolidatorArea": true
    }
  ]
}
```

---

### 4.6 Rubros y Clasificadores (`GET /rubros/objetos-gasto` & `GET /rubros/areas-consolidadoras`)

Consulta el clasificador presupuestario oficial DIGEPRES y los rubros de compra de las áreas consolidadoras.

#### Ejemplo de Llamada `cURL`
```bash
curl -X GET "https://api.pds.gob.do/api/v1/m2m/organization/1/rubros/objetos-gasto?onlyDetail=true&page=1&pageSize=2" \
  -H "Authorization: Bearer <TOKEN_M2M>"
```

---

### 4.7 Plan de Cuentas Contables (`GET /rubros/cuentas-contables`)

Consulta paginada del catálogo de Cuentas Contables Institucionales (DIGECOB - M8).

#### Parámetros de Consulta (Query Parameters)
* `q` *(opcional, string)*: Texto libre para buscar en código o nombre de la cuenta.
* `onlyPostable` *(opcional, boolean)*: Si es `true`, solo retorna cuentas imputables/transaccionales.
* `page` *(opcional, int, default: 1)*: Número de página.
* `pageSize` *(opcional, int, default: 50, max: 200)*: Tamaño de página.

#### Ejemplo de Llamada `cURL`
```bash
curl -X GET "https://api.pds.gob.do/api/v1/m2m/organization/1/rubros/cuentas-contables?onlyPostable=true&page=1&pageSize=2" \
  -H "Authorization: Bearer <TOKEN_M2M>"
```

#### Respuesta Ejemplo (`HTTP 200 OK`)
```json
{
  "pageNumber": 1,
  "pageSize": 2,
  "totalPages": 150,
  "totalRecords": 300,
  "value": [
    {
      "id": "a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d",
      "code": "1.1.05.03.01.01",
      "name": "Materiales de Construcción y Edificaciones",
      "isPostable": true,
      "level": 6,
      "parentCode": "1.1.05.03.01",
      "parentId": "b2c3d4e5-f6a7-8b9c-0d1e-2f3a4b5c6d7e",
      "extendedDescription": "Cuenta imputable para compras de materiales de construcción"
    }
  ]
}
```

---

### 4.8 Catálogo UNSPSC de Bienes y Servicios (`GET /unspsc`)

Consulta paginada y filtrable del catálogo estándar UNSPSC (DGCP). Diseñado con buenas prácticas para alto volumen (~50,000+ registros en 4 niveles jerárquicos).

#### Jerarquía UNSPSC
1. `Segment`: Nivel 1 (Segmentos raíz, ej. `43` - Tecnologías de la Información)
2. `Family`: Nivel 2 (Familias, ej. `4321` - Equipos informáticos)
3. `Class`: Nivel 3 (Clases, ej. `432115` - Computadores)
4. `Material`: Nivel 4 (Materiales/Productos finales, ej. `43211503` - Computadores portátiles)

#### Parámetros de Consulta (Query Parameters)
* `q` *(opcional, string)*: Búsqueda libre por código UNSPSC o descripción.
* `level` *(opcional, string)*: Filtrar por nivel jerárquico (`Segment`, `Family`, `Class`, `Material`).
* `parentId` *(opcional, GUID)*: Obtener los hijos directos de un nodo padre.
* `codePrefix` *(opcional, string)*: Prefijo numérico (ej. `4321` para obtener todos los descendientes de esa familia).
* `enabled` *(opcional, boolean)*: Filtrar por estado habilitado institucionalmente.
* `page` *(opcional, int, default: 1)*: Número de página.
* `pageSize` *(opcional, int, default: 50, max: 250)*: Tamaño de página (acotado a un máximo de 250 para proteger el rendimiento).

#### Ejemplo de Llamada `cURL`
```bash
curl -X GET "https://api.pds.gob.do/api/v1/m2m/organization/1/unspsc?level=Material&codePrefix=4321&page=1&pageSize=2" \
  -H "Authorization: Bearer <TOKEN_M2M>"
```

#### Respuesta Ejemplo (`HTTP 200 OK`)
```json
{
  "pageNumber": 1,
  "pageSize": 2,
  "totalPages": 420,
  "totalRecords": 840,
  "value": [
    {
      "id": "c3d4e5f6-a7b8-9c0d-1e2f-3a4b5c6d7e8f",
      "level": "Material",
      "code": "43211503",
      "name": "Computadores portátiles",
      "parentId": "d4e5f6a7-b89c-0d1e-2f3a-4b5c6d7e8f9a",
      "englishName": "Notebook computers",
      "institutionallyEnabled": true,
      "childCount": 0
    },
    {
      "id": "e5f6a7b8-9c0d-1e2f-3a4b-5c6d7e8f9a0b",
      "level": "Material",
      "code": "43211507",
      "name": "Computadores de escritorio",
      "parentId": "d4e5f6a7-b89c-0d1e-2f3a-4b5c6d7e8f9a",
      "englishName": "Desktop computers",
      "institutionallyEnabled": true,
      "childCount": 0
    }
  ]
}
```

---

## 5. Códigos de Respuesta HTTP Estandarizados

| Código HTTP | Significado | Causa Frecuente |
| :---: | :--- | :--- |
| `200 OK` | Operación exitosa | La consulta o actualización finalizó correctamente. |
| `201 Created` | Recurso creado | El artículo fue registrado exitosamente en la Maestra. |
| `400 Bad Request` | Petición inválida | El JSON enviado viola el formato o las validaciones del modelo. |
| `401 Unauthorized` | No autenticado | Token JWT ausente, expirado o con firma inválida. |
| `403 Forbidden` | Acceso denegado | El token M2M no incluye el permiso `"access:M2M"`. |
| `404 Not Found` | No encontrado | El ID de artículo o recurso especificado no existe en la organización. |
| `500 Server Error` | Error interno | Error no controlado del servidor. Contacte a soporte PDS. |

---

## 6. Buenas Prácticas para Desarrolladores
1. **Reutilización de Tokens JWT**: Almacene en caché su token M2M JWT hasta su expiración (`exp`) para evitar sobrecargar el endpoint de autenticación.
2. **Paginación Eficiente**: Para consultas masivas, utilice un `pageSize` moderado (entre `50` y `200`) y procese secuencialmente incrementando el parámetro `page`.
3. **Manejo de Reintentos**: En caso de recibir un error transitorio `503` o `429`, implemente un patrón de reintento con tiempo de espera exponencial (*exponential backoff*).
