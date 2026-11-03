# MasterSoft ERP & DIGECOB Catalogs Integration Guide (M2M)

This technical guide describes how external systems (ERP integrations, ETL pipelines, automated sync scripts) can interact with the **PDS MasterSoft Regulatory Catalog & Homologation APIs** via Machine-to-Machine (M2M) authentication.

---

## 1. Overview & Architectural Concepts

### Purpose
The **MasterSoft Regulatory Catalog** bridges the ministry's legacy MasterSoft ERP accounting accounts with the official DIGECOB (*Dirección General de Contabilidad Gubernamental*) Plan de Cuentas used across PDS planning, procurement (PACC), and consolidation modules.

### Key Concepts
- **DIGECOB Account (`AccountingAccount`)**: The official 3,575+ account catalog in PDS.
- **MasterSoft Account (`MastersoftAccount`)**: The legacy 969 account catalog in MasterSoft.
- **Homologation Mapping (`MastersoftDigecobMapping`)**: Linkage associating a DIGECOB account with a MasterSoft account.
- **Hierarchy Fallback Resolution**:
  When querying mappings for a detail DIGECOB account:
  1. **Direct Link**: Checks if a direct mapping exists for the given DIGECOB code.
  2. **Parent Fallback**: If no direct link exists, PDS traverses up the DIGECOB parent hierarchy (`ParentCode` or split by `.`) until an ancestral parent mapping is found.
  3. **Inherited Badge**: If resolved via parent, `isMastersoftInherited` is returned as `true` with `inheritedFromDigecobCode` set to the parent account code that established the mapping.

---

## 2. Authentication & Authorization (M2M)

External applications authenticate using standard OAuth 2.0 / JWT Client Credentials bearer tokens.

### Required JWT Claims
| Claim | Type | Required Value / Description |
| :--- | :--- | :--- |
| `is_service_account` | `string` | `"true"` |
| `permissions` | `array<string>` | Must include `"access:M2M"` plus `"catalog:read"` and/or `"catalog:manage"` |
| `organization_id` / `org_id` | `integer` | Organization ID (e.g. `1`). Defaults to `1` if omitted. |
| `sub` / `client_id` | `string` | System or Service Account Unique Identifier. |

### Sample JWT Payload
```json
{
  "sub": "b2a6f183-5d4e-4f11-9a7b-3c12d4567890",
  "client_id": "mastersoft-erp-sync-service",
  "is_service_account": "true",
  "organization_id": 1,
  "permissions": [
    "access:M2M",
    "catalog:read",
    "catalog:manage"
  ],
  "iss": "https://identity.example.com",
  "aud": "https://api.pds.example.com",
  "exp": 1786284000
}
```

### HTTP Request Header
```http
Authorization: Bearer <JWT_TOKEN>
Accept: application/json
```

---

## 3. API Endpoints Reference

Base URL: `https://<pds-api-host>/api/v1/regulatory-catalogs`

---

### 3.1 `GET /regulatory-catalogs/mastersoft-accounts`
Retrieves a paginated list of MasterSoft accounting accounts. Supports filtering by search query `q`.

- **Required Permission**: `catalog:read` or `catalog:manage`
- **Query Parameters**:
  - `q` (*string, optional*): Text search on code or description.
  - `page` (*integer, default: 1*): Page number (1-indexed).
  - `pageSize` (*integer, default: 50*): Number of items per page.

#### JSON Output Schema
```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "properties": {
    "items": {
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "id": { "type": "string", "format": "uuid" },
          "code": { "type": "string" },
          "name": { "type": "string" },
          "controlAccountCode": { "type": ["string", "null"] },
          "controlAccountId": { "type": ["string", "null"], "format": "uuid" },
          "level": { "type": "integer" },
          "isPostable": { "type": "boolean" }
        },
        "required": ["id", "code", "name", "level", "isPostable"]
      }
    },
    "page": { "type": "integer" },
    "pageSize": { "type": "integer" },
    "totalCount": { "type": "integer" },
    "totalPages": { "type": "integer" },
    "hasPreviousPage": { "type": "boolean" },
    "hasNextPage": { "type": "boolean" }
  },
  "required": ["items", "page", "pageSize", "totalCount", "totalPages", "hasPreviousPage", "hasNextPage"]
}
```

#### Sample Output Response
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "items": [
    {
      "id": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
      "code": "1-1-01-01-001",
      "name": "CAJA GENERAL",
      "controlAccountCode": "1-1-01-01",
      "controlAccountId": "e32bc10b-12aa-4372-a567-0e02b2c3d111",
      "level": 5,
      "isPostable": true
    },
    {
      "id": "e32bc10b-12aa-4372-a567-0e02b2c3d111",
      "code": "1-1-01-01",
      "name": "CAJA Y BANCOS",
      "controlAccountCode": "1-1-01",
      "controlAccountId": null,
      "level": 4,
      "isPostable": false
    }
  ],
  "page": 1,
  "pageSize": 50,
  "totalCount": 969,
  "totalPages": 20,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

---

### 3.2 `GET /regulatory-catalogs/mastersoft-mappings`
Lists DIGECOB accounts alongside resolved MasterSoft account mappings (including direct and inherited fallback links).

- **Required Permission**: `catalog:read` or `catalog:manage`
- **Query Parameters**:
  - `q` (*string, optional*): Search query matching DIGECOB code/name or MasterSoft code/name.
  - `filterMode` (*string, optional*): Filter results by link state:
    - `all` (*default*): Returns all accounts.
    - `unlinked`: Only accounts with no MasterSoft mapping.
    - `linked`: Accounts with direct or inherited mapping.
    - `direct`: Accounts with direct mapping.
    - `inherited`: Accounts resolved via parent hierarchy fallback.
  - `page` (*integer, default: 1*): Page number.
  - `pageSize` (*integer, default: 50*): Number of items per page.

#### JSON Output Schema
```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "properties": {
    "items": {
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "id": { "type": "string", "format": "uuid" },
          "digecobAccountId": { "type": "string", "format": "uuid" },
          "digecobAccountCode": { "type": "string" },
          "digecobAccountName": { "type": "string" },
          "digecobIsPostable": { "type": "boolean" },
          "digecobLevel": { "type": "integer" },
          "mastersoftAccountId": { "type": ["string", "null"], "format": "uuid" },
          "mastersoftAccountCode": { "type": ["string", "null"] },
          "mastersoftAccountName": { "type": ["string", "null"] },
          "isInherited": { "type": "boolean" },
          "inheritedFromDigecobCode": { "type": ["string", "null"] }
        },
        "required": ["id", "digecobAccountId", "digecobAccountCode", "digecobAccountName", "digecobIsPostable", "digecobLevel", "isInherited"]
      }
    },
    "page": { "type": "integer" },
    "pageSize": { "type": "integer" },
    "totalCount": { "type": "integer" },
    "totalPages": { "type": "integer" },
    "hasPreviousPage": { "type": "boolean" },
    "hasNextPage": { "type": "boolean" }
  }
}
```

#### Sample Output Response
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "items": [
    {
      "id": "a9101112-1314-1516-1718-192021222324",
      "digecobAccountId": "b1011121-1314-1516-1718-192021222324",
      "digecobAccountCode": "1.1.1.1.01.001",
      "digecobAccountName": "Caja Chica Central",
      "digecobIsPostable": true,
      "digecobLevel": 6,
      "mastersoftAccountId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
      "mastersoftAccountCode": "1-1-01-01-001",
      "mastersoftAccountName": "CAJA GENERAL",
      "isInherited": false,
      "inheritedFromDigecobCode": null
    },
    {
      "id": "00000000-0000-0000-0000-000000000000",
      "digecobAccountId": "c2022232-2425-2627-2829-303132333435",
      "digecobAccountCode": "1.1.1.1.01.002",
      "digecobAccountName": "Caja Auxiliar Pagaduría",
      "digecobIsPostable": true,
      "digecobLevel": 6,
      "mastersoftAccountId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
      "mastersoftAccountCode": "1-1-01-01-001",
      "mastersoftAccountName": "CAJA GENERAL",
      "isInherited": true,
      "inheritedFromDigecobCode": "1.1.1.1.01"
    }
  ],
  "page": 1,
  "pageSize": 50,
  "totalCount": 3575,
  "totalPages": 72,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

---

### 3.3 `POST /regulatory-catalogs/mastersoft-mappings`
Creates or updates a direct homologation mapping between a DIGECOB account and a MasterSoft account.

- **Required Permission**: `catalog:manage`

#### JSON Input Schema
```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "properties": {
    "digecobAccountId": { "type": "string", "format": "uuid" },
    "mastersoftAccountId": { "type": "string", "format": "uuid" }
  },
  "required": ["digecobAccountId", "mastersoftAccountId"]
}
```

#### Sample Input Request Payload
```http
POST /api/v1/regulatory-catalogs/mastersoft-mappings HTTP/1.1
Content-Type: application/json

{
  "digecobAccountId": "b1011121-1314-1516-1718-192021222324",
  "mastersoftAccountId": "f47ac10b-58cc-4372-a567-0e02b2c3d479"
}
```

#### Sample Output Response
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "id": "a9101112-1314-1516-1718-192021222324",
  "digecobAccountId": "b1011121-1314-1516-1718-192021222324",
  "digecobAccountCode": "1.1.1.1.01.001",
  "digecobAccountName": "Caja Chica Central",
  "digecobIsPostable": true,
  "digecobLevel": 6,
  "mastersoftAccountId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "mastersoftAccountCode": "1-1-01-01-001",
  "mastersoftAccountName": "CAJA GENERAL",
  "isInherited": false,
  "inheritedFromDigecobCode": null
}
```

---

### 3.4 `DELETE /regulatory-catalogs/mastersoft-mappings/{id}`
Deletes a direct homologation mapping by its mapping ID (`Guid`).

- **Required Permission**: `catalog:manage`
- **Path Parameters**:
  - `id` (*string, format: uuid, required*): The mapping ID to delete.

#### Sample Request
```http
DELETE /api/v1/regulatory-catalogs/mastersoft-mappings/a9101112-1314-1516-1718-192021222324 HTTP/1.1
Authorization: Bearer <JWT_TOKEN>
```

#### Sample Response
```http
HTTP/1.1 204 No Content
```

---

### 3.5 `POST /regulatory-catalogs/mastersoft-import`
Imports a 3-sheet MasterSoft Excel workbook (`.xlsx`) to bulk populate/update MasterSoft accounts and homologation mappings.

- **Required Permission**: `catalog:manage`
- **Content-Type**: `multipart/form-data`
- **Expected Excel Sheets**:
  1. `Plan de Cuentas DIGECOB`: List of DIGECOB accounts.
  2. `Catálogo de Máster Soft`: Columns `Cuenta MasterSoft`, `Descripción MasterSoft`, `Cta. Control`.
  3. `Vinculacion`: Columns `Código DIGECOB`, `Descripción DIGECOB`, `Código MasterSoft`, `Descripción MasterSoft`.

#### JSON Output Schema
```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "properties": {
    "mastersoftAccountsCreated": { "type": "integer" },
    "mastersoftAccountsUpdated": { "type": "integer" },
    "digecobAccountsProcessed": { "type": "integer" },
    "mappingsCreated": { "type": "integer" },
    "mappingsUpdated": { "type": "integer" },
    "warnings": {
      "type": "array",
      "items": { "type": "string" }
    }
  },
  "required": ["mastersoftAccountsCreated", "mastersoftAccountsUpdated", "digecobAccountsProcessed", "mappingsCreated", "mappingsUpdated", "warnings"]
}
```

#### Sample Response Payload
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "mastersoftAccountsCreated": 969,
  "mastersoftAccountsUpdated": 0,
  "digecobAccountsProcessed": 3575,
  "mappingsCreated": 207,
  "mappingsUpdated": 0,
  "warnings": []
}
```

---

### 3.6 `GET /regulatory-catalogs/mastersoft-export-homologation`
Downloads the complete homologation Excel report (`.xlsx`) containing DIGECOB accounts, direct & inherited MasterSoft linkage statuses, and resolved MasterSoft code/name columns.

- **Required Permission**: `catalog:read` or `catalog:manage`
- **Response Content-Type**: `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`

#### Sample Response Header
```http
HTTP/1.1 200 OK
Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet
Content-Disposition: attachment; filename="Homologacion_DIGECOB_MasterSoft_20260809_120000.xlsx"
```

---

## 4. Error Handling & HTTP Status Codes

When an API error occurs, PDS returns standard HTTP status codes along with a JSON error envelope.

### Error Envelope Schema
```json
{
  "errors": [
    "Detailed description of the error."
  ]
}
```

### HTTP Status Codes Table
| Code | Reason | Typical Cause |
| :--- | :--- | :--- |
| **400 Bad Request** | Invalid Input Payload | Invalid UUID format or missing required file. |
| **401 Unauthorized** | Authentication Failure | Expired/missing Bearer token or missing claims (`sub`, `is_service_account`). |
| **403 Forbidden** | Authorization Failure | Service account token missing required scope (`catalog:read`, `catalog:manage`, or `access:M2M`). |
| **404 Not Found** | Resource Missing | Specified `digecobAccountId` or `mastersoftAccountId` does not exist. |
| **500 Internal Error** | Server Exception | Unhandled internal exception. |
