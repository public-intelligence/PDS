using PDS.Domain.DTOs.CatalogoLocalidad;
using PDS.Domain.Response;
using NodaTime;
using System.Net;

namespace PDS.Service.Services;

public interface IZonaHorariaService
{
    /// <summary>Catálogo de zonas horarias IANA, opcionalmente filtrado por país (alfa-2).</summary>
    IHttpResult<IEnumerable<ZonaHorariaDto>> Get(string? paisCodigo);
}

/// <summary>
/// B05 (RF-LO-21): catálogo de zonas horarias a partir de la base tzdb de NodaTime. Un código de país mal formado
/// responde 400, uno vacío no filtra y un código con formato válido pero sin zonas (o desconocido) responde una lista vacía.
/// </summary>
public class ZonaHorariaService : IZonaHorariaService
{
    private readonly TimeProvider _time;

    public ZonaHorariaService(TimeProvider time) => _time = time;

    public IHttpResult<IEnumerable<ZonaHorariaDto>> Get(string? paisCodigo)
    {
        // Un valor vacío (p. ej. `?paisCodigo=`) equivale a no filtrar.
        var codigo = string.IsNullOrWhiteSpace(paisCodigo) ? null : paisCodigo.Trim();
        if (codigo is not null && !IanaTimeZones.IsCountryCodeFormat(codigo))
            return HttpResult<IEnumerable<ZonaHorariaDto>>.Failure(
                "Código de país inválido",
                new Dictionary<string, IEnumerable<string>> { { "paisCodigo", ["Debe ser un código ISO 3166-1 alfa-2 de dos letras (p. ej. DO, US)"] } },
                HttpStatusCode.BadRequest);

        // RF-LO-21: desplazamiento y horario de verano "actuales" se calculan con el reloj inyectado.
        var ahora = Instant.FromDateTimeOffset(_time.GetUtcNow());
        var zonas = IanaTimeZones.ForCountry(codigo)
            .Select(z =>
            {
                var actual = IanaTimeZones.CurrentOffset(z.Id, ahora);
                return new ZonaHorariaDto
                {
                    Id = z.Id,
                    PaisCodigo = z.PaisCodigo,
                    Comentario = string.IsNullOrWhiteSpace(z.Comentario) ? null : z.Comentario,
                    DesplazamientoActual = actual?.Offset ?? "+00:00",
                    EsHorarioVeranoActual = actual?.IsDaylightSaving ?? false,
                };
            })
            .ToList();
        return HttpResult<IEnumerable<ZonaHorariaDto>>.Success(zonas);
    }
}
