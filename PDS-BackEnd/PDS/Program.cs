using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using PDS;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using PDS.Application.Data;
using PDS.Application.Extensions;
using PDS.Application.Interceptors;
using PDS.ParameterTransformers;
using PDS.Security;
using PDS.Service.Http;
using PDS.Service.Mappers;
using PDS.Service.Services;
using PDS.Service.Services.Planning;
using PDS.Service.Settings;
using Scalar.AspNetCore;
using System.IdentityModel.Tokens.Jwt;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Logs de ILogger hacia App Service (Log Stream y LogFiles/Application). Sin esto, el
// "Fallo no manejado" que registra UseExceptionHandler no queda en ningun lugar consultable
// en los ambientes desplegados. Fuera de App Service no hace nada. Requiere activar
// "Registros de App Service → Registro de aplicaciones" en la web app.
builder.Logging.AddAzureWebAppDiagnostics();

// ── Key Vault para desarrollo local ───────────────────────────────────────────
// Para que nadie tenga que guardar credenciales en su maquina: los secretos de trabajo local
// salen del vault de QA. DefaultAzureCredential usa la sesion de `az login`.
//
// Es opcional: sin `KeyVault:LocalUri` no pasa nada y todo sigue como antes. Se agrega
// despues de los demas proveedores, asi que gana — quien necesite pisar un valor puntual en
// su maquina simplemente no configura la clave.
//
// En los ambientes desplegados esto NO corre: alli los secretos llegan como App Settings.
var uriDelVaultLocal = builder.Configuration["KeyVault:LocalUri"];
if (builder.Environment.IsDevelopment() && !string.IsNullOrWhiteSpace(uriDelVaultLocal))
{
    try
    {
        builder.Configuration.AddAzureKeyVault(
            new Uri(uriDelVaultLocal),
            new DefaultAzureCredential(),
            new AzureKeyVaultConfigurationOptions { Manager = new SecretosDelVaultLocal() });
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException(
            $"No se pudo leer el Key Vault de desarrollo ({uriDelVaultLocal}). Revisa que " +
            "tengas sesion abierta con `az login` y el rol 'Key Vault Secrets User' sobre ese " +
            "vault. Si no lo necesitas, quita la clave 'KeyVault:LocalUri' de tu configuracion " +
            "local.", ex);
    }
}

// RF-RE-06: QuestPDF requiere fijar la licencia antes de renderizar. Community es gratuita para
// organizaciones con ingresos anuales < US$1M; verifique elegibilidad para uso institucional.
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

// Los guards de configuracion validan vacio, no solo null. appsettings.json trae estas
// claves como cadena vacia a proposito (los valores reales no van al repositorio), y '??'
// solo atrapa null: la cadena vacia pasaba de largo y reventaba mas abajo con un mensaje
// que no nombra la clave que falta. Con Jwt:Secret vacio el error era
// "IDX10703: key length is zero" al construir la llave, la aplicacion no arrancaba, y
// cada peticion respondia 500 sin encabezados de CORS. El navegador lo reportaba como
// error de CORS y el diagnostico se perdia.
static string LeerAjusteRequerido(IConfiguration config, string clave, string comoResolver)
{
    var valor = config[clave];
    if (string.IsNullOrWhiteSpace(valor))
        throw new InvalidOperationException(
            $"La clave de configuracion '{clave}' esta vacia o ausente. {comoResolver}");

    return valor;
}

var cadenaDeConexion = LeerAjusteRequerido(
    builder.Configuration,
    "ConnectionStrings:DefaultConnection",
    "Definila en appsettings.Development.json o en user-secrets antes de levantar la API.");

builder.Services.AddDbContext<PDSDbContext>(
    (sp, options) =>
    {
        options
            .UseSqlServer(
                cadenaDeConexion,
                opt => opt.MigrationsAssembly("PDS.Application").EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null)
            )
            .AddInterceptors(sp.GetRequiredService<SoftDeleteInterceptor>());
    }
);

var base64UrlSecret = LeerAjusteRequerido(
    builder.Configuration,
    "Jwt:Secret",
    "Es el AppSecret que GOB-ID emitio para PDS, en base64url. Si te acaban de rotar el "
        + "secreto, este es el valor que hay que actualizar.");
var keyBytes = Base64UrlEncoder.DecodeBytes(base64UrlSecret);
var signingKey = new SymmetricSecurityKey(keyBytes);

// PolicyScheme: detecta si el Bearer token es M2M (kid="app:*" + is_service_account=true)
// y enruta al esquema correcto sin validar firma.
static bool IsBearerM2M(string? authorization)
{
    if (string.IsNullOrEmpty(authorization) || !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        return false;

    try
    {
        var raw = authorization["Bearer ".Length..].Trim();
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(raw);

        var kid = jwt.Header.Kid ?? string.Empty;
        if (!kid.StartsWith("app:", StringComparison.OrdinalIgnoreCase))
            return false;

        var isServiceAccount = jwt.Claims
            .FirstOrDefault(c => c.Type == "is_service_account")?.Value;

        return string.Equals(isServiceAccount, "true", StringComparison.OrdinalIgnoreCase);
    }
    catch
    {
        return false;
    }
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme          = "SmartBearer";
    options.DefaultAuthenticateScheme = "SmartBearer";
    options.DefaultChallengeScheme = "SmartBearer";
})
.AddPolicyScheme("SmartBearer", displayName: null, configureOptions: opts =>
{
    opts.ForwardDefaultSelector = ctx =>
    {
        string? auth = ctx.Request.Headers.Authorization;
        return IsBearerM2M(auth)
            ? GobidM2MAuthenticationHandler.SchemeName
            : JwtBearerDefaults.AuthenticationScheme;
    };
})
.AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    var rawIssuer = builder.Configuration["Jwt:ValidIssuer"] ?? string.Empty;
    var validIssuers = new List<string>();
    if (!string.IsNullOrEmpty(rawIssuer))
    {
        validIssuers.Add(rawIssuer);
        validIssuers.Add(rawIssuer.TrimEnd('/') + "/");
        validIssuers.Add(rawIssuer.TrimEnd('/'));
    }

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer           = true,
        ValidIssuers             = validIssuers.Distinct(),
        ValidateAudience         = true,
        ValidAudience            = builder.Configuration["Jwt:ValidAudience"],
        ValidateLifetime         = true,
        ClockSkew                = TimeSpan.Zero,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey         = signingKey
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(context.Exception, "[JwtBearer] Falló la autenticación del token JWT.");
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("[JwtBearer] Challenge invocado: Error={Error}, Description={Description}",
                context.Error, context.ErrorDescription);
            return Task.CompletedTask;
        }
    };
})
.AddScheme<AuthenticationSchemeOptions, GobidM2MAuthenticationHandler>(
    GobidM2MAuthenticationHandler.SchemeName, configureOptions: null);

builder.Services.AddAuthorization();

builder.Services.Configure<GobidAuthSettings>(
    builder.Configuration.GetRequiredSection(GobidAuthSettings.SectionName)
);

builder.Services.Configure<PublicOrgChartSettings>(
    builder.Configuration.GetSection(PublicOrgChartSettings.SectionName)
);

builder.Services.AddGobidUserAuth(builder.Configuration);

// M2M: configuración, cliente tipado, provider de token y handler
builder.Services.Configure<GobidM2MSettings>(
    builder.Configuration.GetRequiredSection(GobidM2MSettings.SectionName)
);

builder.Services
    .AddHttpClient<IGobidM2MClient, GobidM2MClient>((sp, httpClient) =>
    {
        var settings = sp.GetRequiredService<IOptions<GobidM2MSettings>>().Value;
        httpClient.BaseAddress = new Uri(settings.BaseUrl);
        httpClient.Timeout = TimeSpan.FromSeconds(30);
    });

builder.Services.AddSingleton<IGobidM2MTokenProvider, GobidM2MTokenProvider>();
builder.Services.AddTransient<GobidM2MBearerHandler>();

// Planificación (M1): resolución del área del usuario vía GOBID Personnel + overrides de pruebas.
builder.Services.Configure<GobidPersonnelSettings>(
    builder.Configuration.GetSection(GobidPersonnelSettings.SectionName)
);
builder.Services.Configure<PlanningDevOverridesSettings>(
    builder.Configuration.GetSection(PlanningDevOverridesSettings.SectionName)
);

builder.Services.Configure<CalendarioSettings>(
    builder.Configuration.GetSection(CalendarioSettings.SectionName)
);

builder.Services.AddMemoryCache();

builder.Services
    .AddHttpClient<IGobidPersonnelClient, GobidPersonnelClient>((sp, httpClient) =>
    {
        var settings = sp.GetRequiredService<IOptions<GobidM2MSettings>>().Value;
        httpClient.BaseAddress = new Uri(settings.BaseUrl);
        httpClient.Timeout = TimeSpan.FromSeconds(30);
    })
    .AddHttpMessageHandler<GobidM2MBearerHandler>();

builder.Services
    .AddHttpClient<IGobidUserPositionsClient, GobidUserPositionsClient>((sp, httpClient) =>
    {
        var settings = sp.GetRequiredService<IOptions<GobidM2MSettings>>().Value;
        httpClient.BaseAddress = new Uri(settings.BaseUrl);
        httpClient.Timeout = TimeSpan.FromSeconds(30);
    })
    .AddHttpMessageHandler<GobidM2MBearerHandler>();

// Roles por posición en GOBID: el OrganizationApplicationId viene de configuración/variables de entorno
// (GobidRoles__OrganizationApplicationId).
builder.Services.Configure<GobidRoleSettings>(
    builder.Configuration.GetSection(GobidRoleSettings.SectionName)
);

builder.Services
    .AddHttpClient<IGobidRoleClient, GobidRoleClient>((sp, httpClient) =>
    {
        var settings = sp.GetRequiredService<IOptions<GobidM2MSettings>>().Value;
        httpClient.BaseAddress = new Uri(settings.BaseUrl);
        httpClient.Timeout = TimeSpan.FromSeconds(30);
    })
    .AddHttpMessageHandler<GobidM2MBearerHandler>();


// Add services to the container.
builder.Services.AddControllers(options =>
{
    options.Conventions.Add(new RouteTokenTransformerConvention(new SlugifyParameterTransformer()));
    options.Conventions.Add(new ApiPrefixConvention());
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});
builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddCors(opt =>
{
    opt.AddDefaultPolicy(policy =>
    {
        var origins = builder.Configuration
            .GetSection("AllowedClientOrigins")
            .Get<List<string>>() ?? new List<string>();

        policy.WithOrigins(origins.ToArray())
            .SetIsOriginAllowed(_ => true)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .WithExposedHeaders("Content-Disposition", "X-Current-Page", "X-Page-Count", "X-Page-Size", "X-Total", "Link", "ETag");
    });
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnetcore/openapi
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new()
        {
            Version = "v1",
            Title = "PDS API",
            Description = "API REST del Sistema de personal.",
        };

        // Configurar esquema de seguridad Bearer
        var bearerScheme = new OpenApiSecurityScheme
        {
            In = ParameterLocation.Header,
            Description = "JSON Web Token (JWT)",
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            BearerFormat = "JWT",
            Scheme = "Bearer"
        };

        document.Components ??= new();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = bearerScheme;

        var securityRequirement = new OpenApiSecurityRequirement();
        var securitySchemeReference = new OpenApiSecuritySchemeReference("Bearer", document);
        securityRequirement[securitySchemeReference] = new List<string>();

        foreach (var operation in document.Paths.Values.SelectMany(path => path.Operations.Values))
        {
            operation.Security = new List<OpenApiSecurityRequirement> { securityRequirement };
        }

        return Task.CompletedTask;
    });
});

builder.Services.AddUnitOfWork<PDSDbContext>();
builder.Services.AddRepositories();
builder.Services.AddServices();

// Health check para la compuerta de despliegue. AddDbContextCheck abre la conexion de verdad,
// asi que /health distingue dos fallas que desde afuera se parecen: si devuelve 200 la
// aplicacion arranco Y la base responde; si devuelve 503 el proceso esta vivo pero la base no
// contesta —cadena de conexion mala, o una referencia al vault sin resolver—. Sin esa
// distincion, un despliegue "exitoso" puede dejar el servicio inutil sin que nadie se entere:
// la tarea AzureWebApp reporta exito al COPIAR el paquete, no al arrancarlo.
builder.Services.AddHealthChecks()
    .AddDbContextCheck<PDSDbContext>("base-de-datos");

builder.Services.AddScoped<SoftDeleteInterceptor>();

builder.Services.AddAutoMapper(cfg => cfg.AddProfile<MapperProfile>());

var app = builder.Build();

// Primero en el pipeline para que atrape todo lo que viene despues.
//
// Sin esto, una excepcion no manejada abortaba la respuesta y UseCors nunca alcanzaba a
// escribir sus encabezados: el 500 salia sin Access-Control-Allow-Origin y el navegador
// lo reportaba como error de CORS. Por eso cada falla del servidor llegaba al equipo
// nombrando la parte sana del sistema, y el diagnostico se perdia. Verificado A/B contra
// /api/v1/public/org-chart/current con la base inalcanzable: sin este handler la respuesta
// 500 no traia ni un encabezado de CORS; con el, los trae y el 500 se ve como un 500.
app.UseExceptionHandler(constructor =>
{
    constructor.Run(async contexto =>
    {
        var falla = contexto.Features.Get<IExceptionHandlerFeature>();
        var registro = contexto.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("PDS.ErroresNoManejados");
        registro.LogError(
            falla?.Error,
            "Fallo no manejado en {Metodo} {Ruta}",
            contexto.Request.Method,
            contexto.Request.Path);

        contexto.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var problema = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Error interno del servidor",
            Instance = contexto.Request.Path,
            // El detalle solo fuera de produccion: en produccion filtraria interioridades.
            Detail = app.Environment.IsDevelopment() ? falla?.Error.ToString() : null,
        };
        // El traceId es lo que une este 500 con su entrada en el log.
        problema.Extensions["traceId"] = contexto.TraceIdentifier;

        // El content type va aca y no en Response.ContentType: WriteAsJsonAsync lo sobreescribe.
        await contexto.Response.WriteAsJsonAsync(
            problema,
            options: null,
            contentType: "application/problem+json");
    });
});

// Configure the HTTP request pipeline.
if (app.Environment.IsProduction() || app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors();
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

// Registra la primera petición autenticada de cada sesión (quién entró y desde qué área).
app.UseMiddleware<PDS.Middleware.EntryAuditMiddleware>();

app.MapControllers();

// Anonimo a proposito: lo sondea el pipeline de despliegue, que no tiene token. Va despues de
// UseAuthentication/UseAuthorization pero no lleva [Authorize], asi que responde sin credenciales.
// No expone nada sensible: el cuerpo es "Healthy" o "Unhealthy", sin detalle de la base.
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();
