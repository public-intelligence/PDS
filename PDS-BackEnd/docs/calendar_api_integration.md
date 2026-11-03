# Guía de integración: calendarios laborales de localidades

Esta guía es para los sistemas externos (en primer lugar, la intranet) que consultan o mantienen el calendario laboral de las localidades a través de la API de PDS.

Referencias: RF-LO-31, 34, 38 a 42 e INT-PDS-001 a 004 en `docs/localities-time-zones-calendars.md`. Para las integraciones de solo lectura entre servicios, ver `docs/m2m_api_documentation.md`.

---

## 1. Principios

- **No hay una API de escritura aparte.** La intranet usa **los mismos endpoints de usuario** que la interfaz de PDS (RF-LO-41). No hay escritura por M2M.
- **PDS decide los permisos.** El cliente puede ocultar acciones, pero siempre manda PDS. Una respuesta 403 es definitiva.
- **Las fechas son locales de la localidad.** `fechaInicio`, `fechaFin`, `fecha` y los horarios (`HH:mm`) se expresan en la zona horaria de la localidad, nunca en la del navegador ni en UTC. Para hacer cálculos, use `intervalosUtc` del calendario efectivo.
- **Los mensajes se muestran tal cual.** Los errores vienen en español y están pensados para el usuario final.

---

## 2. Autenticación

| Aspecto | Valor |
|---|---|
| Tipo de token | JWT **de usuario** emitido por GobID para la **audiencia de PDS** (`Jwt:ValidAudience`) |
| Cabecera | `Authorization: Bearer <token>` |
| Mecanismo preferido | Intercambio de token en GobID (*on-behalf-of*): la intranet canjea el token de la sesión por uno con audiencia PDS |
| Organización | Sale del token; no se envía en la ruta |
| Identidad | El `sub` del token tiene que coincidir con el `GobidUserId` de un responsable de la localidad |

> **Pendiente (OQ-02):** falta que GobID confirme que puede emitir tokens con audiencia PDS para las sesiones de la intranet. La alternativa (un token M2M con una cabecera que indique el usuario que actúa) cambia el modelo de confianza y **necesita aprobación por escrito** antes de usarse.

**Cabecera recomendada (INT-PDS-004):**

```
X-Client-Application: intranet
```

PDS la registra como el canal del cambio (`ApiExterna`, que se ve como "Intranet / API externa" en la bitácora). **No se usa para autorizar.**

**CORS:** pida que el origen de la intranet se agregue a `AllowedClientOrigins` de PDS.

---

## 3. Quién puede hacer qué

Una persona es **gestor de calendario** de la localidad *L* cuando se cumplen todas estas condiciones (RF-LO-38):

1. existe un responsable *R* asignado activamente a *L* con **"Gestiona calendario"** marcado;
2. el `GobidUserId` de *R* es igual al `sub` del token;
3. *R* no está eliminado y pertenece a la organización del token;
4. *L* no está en un estado terminal.

Solo un administrador de PDS puede vincular el usuario de GobID y marcar "Gestiona calendario".

| Acción | Usuario autenticado | Gestor de *L* | Admin de calendario |
|---|---|---|---|
| Ver el calendario, el calendario efectivo y el estado (abierta o cerrada) | sí | sí | sí |
| Crear, editar o eliminar días no laborables (excepciones) | no | sí, **dentro de la ventana retroactiva** | sí (con `motivo` si es retroactivo) |
| Editar excepciones de sistema (suspensión o cierre) | no | no | no |
| Mantener los feriados de un calendario cuya localidad gestora es *L* | no | sí, dentro de la ventana | sí |
| Crear o eliminar calendarios de feriados | no | no | sí |

**Ventana retroactiva (RF-LO-33):** un gestor que no es administrador solo puede tocar fechas a partir de **hoy (local) − N días**. N vale 7 por defecto y su valor actual llega en `ventanaRetroactivaDias`.

---

## 4. Convenciones

- Ruta base: `/api/v1`.
- Formato JSON en camelCase. Los enums van como texto.
- Fechas `YYYY-MM-DD`, horas `HH:mm` (con `24:00` permitido como fin) e instantes ISO 8601 en UTC con `Z`.
- Concurrencia (RF-LO-42): los GET de un solo recurso devuelven `ETag`. En PUT y DELETE envíe `If-Match: <etag>`. Si el ETag no coincide, la respuesta es **412**.

---

## 5. Flujos

### 5.1 Localidades que gestiono

```
GET /api/v1/localidades/calendario/gestionables
```

```json
[
  {
    "id": "3f0c…",
    "codigo": "CG-NYC",
    "nombreOficial": "Consulado General en Nueva York",
    "pais": "Estados Unidos",
    "ciudad": "Nueva York",
    "zonaHoraria": "America/New_York",
    "rol": "Gestor"
  }
]
```

`rol` vale `Gestor` o `Admin`. Un administrador recibe todas las localidades activas. Si la lista viene vacía, el usuario no gestiona ninguna localidad y la intranet debería ocultar la función.

### 5.2 Definición del calendario

```
GET /api/v1/localidades/{id}/calendario
```

Devuelve la zona horaria, `calendarioVersion`, `puedeGestionar`, `ventanaRetroactivaDias`, las versiones del horario semanal y las suscripciones a calendarios de feriados. Use `puedeGestionar` para mostrar u ocultar las acciones.

### 5.3 Calendario efectivo (mes o año)

```
GET /api/v1/localidades/{id}/calendario/efectivo?desde=2026-11-26&hasta=2026-11-27
```

El rango puede ser de 400 días como máximo. Para ver un año, pida del 1 de enero al 31 de diciembre.

```json
{
  "localidadId": "3f0c…",
  "zonaHoraria": "America/New_York",
  "tzdbVersion": "2026a",
  "calendarioVersion": 42,
  "desde": "2026-11-26",
  "hasta": "2026-11-27",
  "dias": [
    {
      "fecha": "2026-11-26",
      "diaSemana": "thu",
      "laborable": false,
      "intervalos": [],
      "intervalosUtc": [],
      "fuente": "Feriado",
      "motivos": [{ "tipo": "Feriado", "nombre": "Thanksgiving Day", "calendarioFeriadosId": "…" }]
    },
    {
      "fecha": "2026-11-27",
      "diaSemana": "fri",
      "laborable": true,
      "intervalos": [{ "inicio": "09:00", "fin": "13:00" }],
      "intervalosUtc": [{ "inicio": "2026-11-27T14:00:00Z", "fin": "2026-11-27T18:00:00Z" }],
      "fuente": "Excepcion",
      "motivos": [{ "tipo": "JornadaEspecial", "nombre": "Media jornada posterior a Acción de Gracias", "excepcionId": "…" }]
    }
  ]
}
```

`fuente` indica de dónde sale el resultado del día: `Jornada` (horario semanal), `Feriado`, `Excepcion` o `CicloVida` (suspensión, cierre o fecha anterior a la apertura).

### 5.4 Abierta o cerrada ahora

```
GET /api/v1/localidades/{id}/calendario/estado[?instante=2026-11-27T15:00:00Z]
```

```json
{
  "laborable": true,
  "intervaloActual": { "inicio": "09:00", "fin": "13:00" },
  "proximoInstanteLaborable": null
}
```

Si no se envía `instante`, se usa el momento actual.

### 5.5 Días no laborables (excepciones)

**Listar:**

```
GET /api/v1/localidades/{id}/calendario/excepciones?desde=2027-01-01&hasta=2027-12-31
```

Cada elemento trae `id`, `fechaInicio`, `fechaFin` (que es `null` en las excepciones de sistema sin fin), `tipo`, `categoria`, `origen`, `motivo`, `referenciaNormativa`, `instrumentoLegalId`, `intervalos`, `esSistema`, `registradoPor` y `canal`. **Las filas con `esSistema = true` son solo de lectura.**

Las filas de sistema las genera PDS al suspender, reactivar o cerrar la localidad (RF-LO-32); la intranet no las crea ni las edita:

- **Suspender** crea una excepción `categoria = Suspension`, `tipo = Cierre`, desde el inicio hasta el fin de la suspensión (sin fin si no se indicó).
- **Reactivar** le pone como `fechaFin` el día local anterior a la reactivación; ese día ya sigue el horario. La fila se conserva como historial. Si la suspensión aún no había empezado, se descarta.
- **Cerrar** crea una excepción `categoria = CierreDefinitivo`, sin fin, desde `CierreFecha`.
- Los días anteriores a `FechaApertura` son no laborables (`fuente = CicloVida`) sin que exista ninguna fila.

Todos estos cambios suben `calendarioVersion` y quedan en el feed de cambios con `motivo = CicloVida`.

**Crear:**

```
POST /api/v1/localidades/{id}/calendario/excepciones
X-Client-Application: intranet
```

```json
{
  "fechaInicio": "2027-01-15",
  "fechaFin": "2027-01-15",
  "tipo": "Cierre",
  "categoria": "DecisionAdministrativa",
  "origen": "PaisAnfitrion",
  "motivo": "Día no laborable decretado por el gobierno anfitrión (elecciones)",
  "referenciaNormativa": "Decreto 12/2027",
  "intervalos": null
}
```

Responde `201` con la excepción guardada y el nuevo `calendarioVersion`.

**Editar y eliminar:**

```
PUT    /api/v1/localidades/{id}/calendario/excepciones/{excepcionId}   (If-Match)
DELETE /api/v1/localidades/{id}/calendario/excepciones/{excepcionId}   (If-Match)
```

El cuerpo del PUT es igual al del POST. La eliminación es lógica (*soft delete*). Antes de eliminar, avise al usuario de que se recalcularán los plazos de las solicitudes en curso.

**Campos:**

| Campo | Obligatorio | Valores y reglas |
|---|---|---|
| `fechaInicio`, `fechaFin` | sí | Fechas locales inclusivas; `fechaFin` ≥ `fechaInicio`; rango de 366 días como máximo |
| `tipo` | sí | `Cierre` (no se trabaja), `JornadaEspecial` (se trabaja con otro horario), `DiaLaborableAdicional` (se trabaja un día que normalmente es libre) |
| `intervalos` | según el tipo | Debe ser `null` en `Cierre`. Es obligatorio en los otros dos tipos: hasta 4 intervalos `{ "inicio": "HH:mm", "fin": "HH:mm" }` que no se solapen |
| `categoria` | sí | `DecisionAdministrativa`, `FeriadoLocal`, `DueloOficial`, `Emergencia`, `EventoInstitucional`, `Otro` |
| `origen` | sí | `PaisAnfitrion`, `PaisPropio` (República Dominicana), `Institucional` |
| `motivo` | sí | Hasta 1000 caracteres |
| `referenciaNormativa` | no | Hasta 500 caracteres, p. ej. "Decreto 12/2027" |
| `instrumentoLegalId` | no | ID de un instrumento legal de la organización |

Las excepciones de una misma localidad **no pueden solaparse**. Si se solapan, la respuesta es 409.

### 5.6 Feriados de un calendario gestionado

Solo aplica cuando la localidad del usuario es la **localidad gestora** de ese calendario de feriados.

```
GET  /api/v1/catalogos/calendarios-feriados/gestionables
GET  /api/v1/catalogos/calendarios-feriados/{id}/dias?anio=2027
POST /api/v1/catalogos/calendarios-feriados/{id}/dias
PUT  /api/v1/catalogos/calendarios-feriados/{id}/dias/{diaId}      (If-Match)
DELETE /api/v1/catalogos/calendarios-feriados/{id}/dias/{diaId}    (If-Match)
```

```json
{
  "fecha": "2027-12-24",
  "nombre": "Nochebuena",
  "alcance": "JornadaReducida",
  "intervalos": [{ "inicio": "09:00", "fin": "13:00" }],
  "referenciaNormativa": null,
  "notas": null
}
```

- `alcance` vale `DiaCompleto` o `JornadaReducida`. Los `intervalos` son obligatorios solo en `JornadaReducida` e indican el horario que queda ese día.
- No puede haber dos feriados con la misma `fecha` en un calendario (409).
- Los feriados son fechas concretas: los móviles se cargan cada año tal como se publican oficialmente.

**Carga masiva:**

```
POST /api/v1/catalogos/calendarios-feriados/{id}/dias/importar?modo=agregar&validarSolamente=true
```

```json
[
  { "fecha": "2027-01-01", "nombre": "Año Nuevo", "alcance": "DiaCompleto" },
  { "fecha": "2027-12-24", "nombre": "Nochebuena", "alcance": "JornadaReducida",
    "intervalos": [{ "inicio": "09:00", "fin": "13:00" }] }
]
```

- `modo` vale `agregar` o `reemplazarAnio` (por defecto `agregar`). Con `validarSolamente=true` se hace una prueba sin guardar nada.
- `agregar` crea las fechas nuevas, actualiza las que cambian y omite las idénticas; no elimina nada. `reemplazarAnio` hace lo mismo y además elimina (de forma lógica) los feriados de los años presentes en la carga que no vienen en ella.
- Un cambio sobre fechas pasadas sigue la ventana retroactiva (sección 3). Un administrador envía el `motivo` como parámetro de consulta (`?motivo=`).
- La importación es atómica: si una línea falla, no se guarda ninguna.
- Respuesta: `{ "creados": 2, "actualizados": 0, "omitidos": 0, "eliminados": 0, "errores": [] }`. Cada error trae `linea` (base 1), `clave` y `mensaje`.
- Con errores de línea, la respuesta es **400** con ese mismo informe y no se guarda nada (los contadores quedan en cero). Con `validarSolamente=true` el informe siempre llega con **200**: la intranet habilita "Importar" solo si `errores` viene vacío.

### 5.7 Cálculo de vencimientos y feed de cambios (solo servicios M2M)

Estos endpoints son para WF y SDS, no para la intranet. Requieren una cuenta de servicio con el permiso `localities:calendar:read`; la organización sale de la ruta.

```
GET  /m2m/organization/{orgId}/calendario/cambios?cursor=&limite=
POST /m2m/organization/{orgId}/calendario/calcular-vencimiento
POST /m2m/organization/{orgId}/calendario/tiempo-laborable
```

- **Feed** (`cambios`): devuelve `{ items, siguienteCursor, hayMas }` con las filas posteriores al cursor, hasta `limite` (máximo 1000). Responde **410** si el cursor ya fue purgado: el consumidor debe resincronizar todo. Las filas se conservan 180 días y un proceso diario elimina las vencidas.
- **`calcular-vencimiento`**: `{ "localidadId", "inicio", "cantidad", "unidad" }`, con `unidad` = `min`, `hours`, `bizdays` o `caldays`. Responde `{ vencimiento, vencimientoLocal, zonaHoraria, calendarioVersion, convencion: "v1" }`.
- **`tiempo-laborable`**: `{ "localidadId", "desde", "hasta" }`. Responde `{ minutosLaborables }`, redondeado hacia abajo a minutos enteros.
- Si el cuerpo trae `items` (máximo 100) es un lote; cada ítem trae su propio `error` `{ estado, clave, mensaje }`.
- Los instantes (`inicio`, `desde`, `hasta`) deben llevar `Z` u otro desfase explícito.
- Un resultado más allá de `Calendario:HorizonteCalculoDias` (366 días) responde **422** con la clave `horizonte`; `desde` > `hasta` responde **400** con la clave `rango`.
- `calendarioVersion` es la misma versión que publica el feed: si cambia después del cálculo, hay que recalcular.

---

## 6. Errores

Todos los errores tienen esta forma:

```json
{ "title": "Excepción solapada", "detail": { "solapamiento": ["Se solapa con la excepción 7d1e… (2027-01-14 a 2027-01-16)"] } }
```

Asocie cada clave de `detail` al campo correspondiente del formulario y muestre el mensaje sin cambiarlo. Si la clave no corresponde a ningún campo, muestre el mensaje en un aviso general.

| Estado | Clave de `detail` | Qué significa | Qué hacer |
|---|---|---|---|
| 400 | `fechaInicio`, `fechaFin` | Fechas en el orden incorrecto o rango de más de 366 días | Marcar el campo |
| 400 | `intervalos` | Falta el horario, sobra o tiene un formato inválido | Marcar el campo |
| 400 | `motivo` | Falta el motivo de un cambio retroactivo hecho por un admin | Marcar el campo |
| 400 | `rango` | Rango de consulta de más de 400 días, o `desde` > `hasta` | Corregir la consulta |
| 401 | — | Token inválido o vencido | Renovar el token |
| 403 | `permiso` | El usuario no es gestor de la localidad, o intentó tocar una excepción de sistema | Ocultar la acción y recargar los permisos |
| 403 | `fechaInicio` (excepciones) o `fecha` (feriados) | La fecha queda fuera de la ventana retroactiva | Mostrar el mensaje junto a la fecha |
| 400 | `dias` | La importación de feriados llegó vacía o con más de 2000 filas | Corregir la carga |
| 409 | `solapamiento` | Se solapa con otra excepción | Mostrar el mensaje y ofrecer abrir la excepción existente |
| 409 | `fecha` | Ya hay un feriado en esa fecha | Marcar la fecha |
| 412 | `concurrencia` | Otra persona modificó el registro | Mostrar "Otra persona modificó este registro" con un botón Recargar |

---

## 7. Verificación

Un cambio hecho desde la intranet tiene que cumplir esto (RF-LO-41, AC1):

1. Con un token de gestor y `X-Client-Application: intranet`, `POST …/excepciones` responde `201`.
2. `GET …/calendario/efectivo` muestra esos días con `fuente = Excepcion` y el `motivo`.
3. La bitácora de la localidad en PDS muestra el cambio con el canal "Intranet / API externa".
4. Un segundo `PUT` enviado con el `If-Match` viejo responde `412`.
5. Un usuario que no es gestor recibe `403` con la clave `permiso`.

---

## 8. Pendientes

| ID | Tema | Responsable |
|---|---|---|
| OQ-02 | Que GobID emita tokens con audiencia PDS para la intranet (intercambio de token) | GobID / arquitectura |
| OQ-03 | Que GobID tenga una API para buscar usuarios al vincular un responsable | GobID |
| OQ-07 | Duración final de la ventana retroactiva (hoy son 7 días) | Producto |
| — | Agregar el origen de la intranet a `AllowedClientOrigins` | Backend PDS |
