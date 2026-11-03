# Configurar PDS en tu máquina

Los secretos no viven en el repositorio. En tu máquina los guardás con **user-secrets** (el gestor
de secretos de .NET), que los deja fuera del proyecto para que no haya forma de subirlos por error.
En dev y en producción los lee la configuración del App Service. En `appsettings.json` y
`appsettings.Development.json` solo quedan valores que no son sensibles.

Esta guía sirve igual en macOS, Windows y Linux.

## Qué necesitás instalado

| | Versión | Cómo verificar |
|---|---|---|
| SDK de .NET | **10.0** o superior | `dotnet --version` |
| SQL Server | cualquiera que acepte la cadena de conexión que uses | ver abajo |

## Qué secretos vas a necesitar

| Clave | Para qué es | De dónde sale el valor |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | La base de PDS | Tu SQL local, o la de dev |
| `Jwt:Secret` | Valida la firma de los tokens de GOB-ID | El AppSecret que GOB-ID emitió para PDS |
| `GobidM2M:ClientSecret` | El cliente máquina a máquina que sale hacia GOB-ID | GOB-ID dev |

`PDS/secrets.example.json` (en el repo) es la referencia de qué claves existen y con qué forma.

**Lo que no es secreto no va acá.** `GobidM2M:ClientId`, `Jwt:ValidIssuer`, `Jwt:ValidAudience`,
`GobidAuth:BaseUrl`, `AllowedClientOrigins` y los IDs de estructura organizacional son
identificadores y URLs, no credenciales: están versionados en `appsettings.Development.json` para
que clonar y correr funcione sin configurar nada. No los dupliques en tus secretos.

## De dónde sale el archivo con los valores

Los valores reales viven en un **Secure File** de Azure DevOps (`pds-dev-secrets.json`, en
Pipelines → Library → Secure Files). Ese archivo es la **fuente única**: se mantiene ahí y desde ahí
lo consumen los pipelines.

**Lo que no podés hacer es descargarlo vos.** Un Secure File es de solo escritura para las personas:
no tiene botón de descarga en la Library ni se puede leer por API — solo los pipelines lo leen, con
la tarea `DownloadSecureFile`. Así que el archivo **te lo tiene que pasar quien administra la
configuración**, por un canal seguro. Nunca por git ni por chat.

## Pasos (una sola vez)

1. Pedí el archivo `pds-dev-secrets.json` a quien administra la configuración. Viene con los valores
   de dev completos.

2. Guardalo en la carpeta del API con el nombre `secrets.local.json`:
   ```
   PDS/secrets.local.json
   ```
   Ese archivo está ignorado por git, así que los valores quedan solo en tu máquina.

3. Cargalos todos de una. El mismo archivo funciona en los tres sistemas:
   ```
   # macOS / Linux
   cat PDS/secrets.local.json | dotnet user-secrets set --project PDS

   # Windows PowerShell
   Get-Content -Raw PDS/secrets.local.json | dotnet user-secrets set --project PDS
   ```

4. Levantá la API:
   ```
   dotnet run --project PDS
   ```

Si más adelante necesitás cambiar un valor, editá `secrets.local.json` y volvé a correr el paso 3.

## Si falta un secreto

**La API no arranca**, y el mensaje nombra la clave que falta:

```
Unhandled exception. System.InvalidOperationException: La clave de configuracion 'Jwt:Secret'
esta vacia o ausente. Es el AppSecret que GOB-ID emitio para PDS, en base64url. Si te acaban
de rotar el secreto, este es el valor que hay que actualizar.
```

Es a propósito: preferimos que falle de entrada, diciendo qué falta, que arranque en un estado
inútil. Antes de esto, un `Jwt:Secret` vacío no daba ese mensaje — reventaba con
`IDX10703: key length is zero` al construir la llave de firma, la API no levantaba, y **cada
petición respondía 500 sin encabezados de CORS**. El navegador entonces reportaba un error de CORS
y el diagnóstico se perdía persiguiendo la parte sana del sistema.

Para ver qué tenés cargado:

```
dotnet user-secrets list --project PDS
```

## Si el frontend reporta un error de CORS

Mirá el cuerpo de la respuesta en la pestaña Network antes de revisar la configuración de CORS.
Un 500 del servidor ahora llega con `Access-Control-Allow-Origin` y un cuerpo así:

```json
{"title":"Error interno del servidor","status":500,"instance":"/api/v1/...","traceId":"0HNNH..."}
```

Ese `traceId` es lo que une la respuesta con su línea en el log del servidor. Si en cambio ves un
error de CORS real, ahí sí revisá `AllowedClientOrigins`.

## Comandos que te van a servir

```
dotnet user-secrets list  --project PDS     # ver qué claves tenés cargadas
dotnet user-secrets clear --project PDS     # borrar todo y empezar de cero
dotnet build PDS/PDS.csproj                 # compilar
dotnet run   --project PDS                  # levantar la API
```
