using System.Net;
using PDS.Domain.Enums;
using PDS.Domain.Response;

namespace PDS.Service.Services.Calendario;

/// <summary>Quien ejecuta una operación del calendario: tenant, usuario, canal y permisos del token (RF-LO-39).</summary>
public sealed record CalendarioActor(
    int OrganizationId, Guid UserId, string? UserName, CanalCambio Canal, IReadOnlyCollection<string> Permissions);

/// <summary>Resultados de error con el formato <c>ErrorDto { title, detail { clave: [mensaje] } }</c> (SPEC-LOC-CAL-01 §10.5).</summary>
internal static class CalendarioResults
{
    public static IHttpResult<T> Fail<T>(string title, string key, string msg, HttpStatusCode code) =>
        HttpResult<T>.Failure(title, new Dictionary<string, IEnumerable<string>> { { key, [msg] } }, code);

    public static IHttpResult Fail(string title, string key, string msg, HttpStatusCode code) =>
        HttpResult.Failure(title, new Dictionary<string, IEnumerable<string>> { { key, [msg] } }, code);

    public static IHttpResult<T> FailMany<T>(string title, string key, IEnumerable<string> msgs, HttpStatusCode code) =>
        HttpResult<T>.Failure(title, new Dictionary<string, IEnumerable<string>> { { key, msgs.ToList() } }, code);

    public static IHttpResult FailMany(string title, string key, IEnumerable<string> msgs, HttpStatusCode code) =>
        HttpResult.Failure(title, new Dictionary<string, IEnumerable<string>> { { key, msgs.ToList() } }, code);

    public static IHttpResult<T> SinPermiso<T>() =>
        Fail<T>("Sin permiso", "permiso", "No tiene permiso para modificar este calendario.", HttpStatusCode.Forbidden);

    public static IHttpResult SinPermiso() =>
        Fail("Sin permiso", "permiso", "No tiene permiso para modificar este calendario.", HttpStatusCode.Forbidden);

    public static IHttpResult<T> Concurrencia<T>() =>
        Fail<T>("Conflicto de concurrencia", "concurrencia",
            "Otra persona modificó este registro. Recargue para ver los cambios.", HttpStatusCode.PreconditionFailed);

    public static IHttpResult Concurrencia() =>
        Fail("Conflicto de concurrencia", "concurrencia",
            "Otra persona modificó este registro. Recargue para ver los cambios.", HttpStatusCode.PreconditionFailed);
}
