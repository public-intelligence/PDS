using System.Net;
using Microsoft.Extensions.Options;
using NodaTime;
using PDS.Domain.DTOs.Calendario;
using PDS.Domain.Response;
using PDS.Service.Services.Calendario.Motor;
using PDS.Service.Settings;

namespace PDS.Service.Services.Calendario;

/// <summary>
/// RF-LO-35 (SPEC-LOC-CAL-01 §10.3, §11.3.6): endpoints de referencia de tiempo laborable para WF y SDS. Solo orquesta: valida la
/// petición, carga el calendario de la localidad (del tenant que llama; otra organización responde 404) y delega la aritmética en
/// <see cref="ITiempoLaborableCalculator"/>, el mismo motor puro que ejecutan las pruebas del Apéndice B. La <c>calendarioVersion</c>
/// que devuelve sale del mismo snapshot cuya versión publica el feed de cambios, así que WF sabe con qué versión calculó.
/// </summary>
public interface ITiempoLaborableService
{
    Task<IHttpResult<VencimientoDto>> CalcularVencimientoAsync(int organizationId, CalcularVencimientoDto peticion, CancellationToken ct = default);

    /// <summary>Lote de hasta 100 cálculos: un ítem que falla no afecta a los demás y lleva su propio <c>error</c>.</summary>
    Task<IHttpResult<VencimientoLoteDto>> CalcularVencimientoLoteAsync(
        int organizationId, IReadOnlyList<CalcularVencimientoDto>? items, CancellationToken ct = default);

    Task<IHttpResult<MinutosLaborablesDto>> TiempoLaborableAsync(int organizationId, TiempoLaborableDto peticion, CancellationToken ct = default);

    Task<IHttpResult<MinutosLaborablesLoteDto>> TiempoLaborableLoteAsync(
        int organizationId, IReadOnlyList<TiempoLaborableDto>? items, CancellationToken ct = default);
}

public class TiempoLaborableService(
    ICalendarioSnapshotService snapshots,
    ITiempoLaborableCalculator calculadora,
    IOptions<CalendarioSettings> settings) : ITiempoLaborableService
{
    public const int MaxItemsLote = 100;

    private int Horizonte => Math.Max(1, settings.Value.HorizonteCalculoDias);

    public async Task<IHttpResult<VencimientoDto>> CalcularVencimientoAsync(
        int organizationId, CalcularVencimientoDto peticion, CancellationToken ct = default) =>
        await Vencimiento(organizationId, peticion, new Dictionary<Guid, SnapshotCargado?>(), ct);

    public async Task<IHttpResult<VencimientoLoteDto>> CalcularVencimientoLoteAsync(
        int organizationId, IReadOnlyList<CalcularVencimientoDto>? items, CancellationToken ct = default)
    {
        var invalido = ValidarLote<VencimientoLoteDto, CalcularVencimientoDto>(items);
        if (invalido is not null) return invalido;

        var cache = new Dictionary<Guid, SnapshotCargado?>();
        var respuesta = new VencimientoLoteDto();
        foreach (var item in items!)
        {
            var r = await Vencimiento(organizationId, item, cache, ct);
            respuesta.Items.Add(r.IsSuccess
                ? new VencimientoItemDto
                {
                    Vencimiento = r.Value.Vencimiento, VencimientoLocal = r.Value.VencimientoLocal, ZonaHoraria = r.Value.ZonaHoraria,
                    CalendarioVersion = r.Value.CalendarioVersion, Convencion = r.Value.Convencion,
                }
                : new VencimientoItemDto { Error = ErrorDe(r) });
        }
        return HttpResult<VencimientoLoteDto>.Success(respuesta);
    }

    public async Task<IHttpResult<MinutosLaborablesDto>> TiempoLaborableAsync(
        int organizationId, TiempoLaborableDto peticion, CancellationToken ct = default) =>
        await Minutos(organizationId, peticion, new Dictionary<Guid, SnapshotCargado?>(), ct);

    public async Task<IHttpResult<MinutosLaborablesLoteDto>> TiempoLaborableLoteAsync(
        int organizationId, IReadOnlyList<TiempoLaborableDto>? items, CancellationToken ct = default)
    {
        var invalido = ValidarLote<MinutosLaborablesLoteDto, TiempoLaborableDto>(items);
        if (invalido is not null) return invalido;

        var cache = new Dictionary<Guid, SnapshotCargado?>();
        var respuesta = new MinutosLaborablesLoteDto();
        foreach (var item in items!)
        {
            var r = await Minutos(organizationId, item, cache, ct);
            respuesta.Items.Add(r.IsSuccess
                ? new MinutosLaborablesItemDto { MinutosLaborables = r.Value.MinutosLaborables }
                : new MinutosLaborablesItemDto { Error = ErrorDe(r) });
        }
        return HttpResult<MinutosLaborablesLoteDto>.Success(respuesta);
    }

    // ───────────────────────────── Un cálculo ─────────────────────────────

    private async Task<IHttpResult<VencimientoDto>> Vencimiento(
        int organizationId, CalcularVencimientoDto p, Dictionary<Guid, SnapshotCargado?> cache, CancellationToken ct)
    {
        if (p.LocalidadId is not { } localidadId || localidadId == Guid.Empty)
            return Falla<VencimientoDto>("localidadId", "El campo «localidadId» es obligatorio.", HttpStatusCode.BadRequest);
        if (p.Inicio is not { } inicio)
            return Falla<VencimientoDto>("inicio", "El campo «inicio» es obligatorio (ISO 8601 con Z u offset).", HttpStatusCode.BadRequest);
        if (p.Cantidad is not { } cantidad || cantidad < 1)
            return Falla<VencimientoDto>("cantidad", "La cantidad debe ser un entero mayor o igual a 1.", HttpStatusCode.BadRequest);
        if (!TryParseUnidad(p.Unidad, out var unidad))
            return Falla<VencimientoDto>("unidad", "La unidad debe ser «min», «hours», «bizdays» o «caldays».", HttpStatusCode.BadRequest);

        var cargado = await CargarAsync(organizationId, localidadId, cache, ct);
        if (cargado is null) return NoEncontrada<VencimientoDto>();

        var snapshot = cargado.Snapshot;
        var resultado = calculadora.CalcularVencimiento(snapshot, Instant.FromDateTimeOffset(inicio), cantidad, unidad, Horizonte);
        if (resultado is null) return Horizontes<VencimientoDto>("El vencimiento queda fuera del horizonte de cálculo.");

        return HttpResult<VencimientoDto>.Success(new VencimientoDto
        {
            Vencimiento = DateTime.SpecifyKind(resultado.Instante.ToDateTimeUtc(), DateTimeKind.Utc),
            VencimientoLocal = DateTime.SpecifyKind(resultado.Local.ToDateTimeUnspecified(), DateTimeKind.Unspecified),
            ZonaHoraria = resultado.ZonaHoraria,
            CalendarioVersion = snapshot.CalendarioVersion,
            Convencion = "v1",
        });
    }

    private async Task<IHttpResult<MinutosLaborablesDto>> Minutos(
        int organizationId, TiempoLaborableDto p, Dictionary<Guid, SnapshotCargado?> cache, CancellationToken ct)
    {
        if (p.LocalidadId is not { } localidadId || localidadId == Guid.Empty)
            return Falla<MinutosLaborablesDto>("localidadId", "El campo «localidadId» es obligatorio.", HttpStatusCode.BadRequest);
        if (p.Desde is not { } desde)
            return Falla<MinutosLaborablesDto>("desde", "El campo «desde» es obligatorio (ISO 8601 con Z u offset).", HttpStatusCode.BadRequest);
        if (p.Hasta is not { } hasta)
            return Falla<MinutosLaborablesDto>("hasta", "El campo «hasta» es obligatorio (ISO 8601 con Z u offset).", HttpStatusCode.BadRequest);
        if (desde > hasta)
            return Falla<MinutosLaborablesDto>("rango", "«desde» no puede ser posterior a «hasta».", HttpStatusCode.BadRequest);

        var cargado = await CargarAsync(organizationId, localidadId, cache, ct);
        if (cargado is null) return NoEncontrada<MinutosLaborablesDto>();

        var minutos = calculadora.MinutosLaborables(cargado.Snapshot, Instant.FromDateTimeOffset(desde), Instant.FromDateTimeOffset(hasta), Horizonte);
        return minutos is null
            ? Horizontes<MinutosLaborablesDto>("El rango supera el horizonte de cálculo.")
            : HttpResult<MinutosLaborablesDto>.Success(new MinutosLaborablesDto { MinutosLaborables = minutos.Value });
    }

    // ───────────────────────────── Utilidades ─────────────────────────────

    // Un lote repite localidades: cada una se carga una sola vez (y una localidad inexistente se recuerda como nula).
    private async Task<SnapshotCargado?> CargarAsync(int organizationId, Guid localidadId, Dictionary<Guid, SnapshotCargado?> cache, CancellationToken ct)
    {
        if (cache.TryGetValue(localidadId, out var existente)) return existente;
        var loc = await snapshots.GetBaseAsync(organizationId, localidadId, ct);
        var cargado = loc is null ? null : await snapshots.CargarAsync(loc, ct);
        cache[localidadId] = cargado;
        return cargado;
    }

    private static bool TryParseUnidad(string? texto, out UnidadTiempo unidad)
    {
        switch (texto?.Trim().ToLowerInvariant())
        {
            case "min": unidad = UnidadTiempo.Min; return true;
            case "hours": unidad = UnidadTiempo.Hours; return true;
            case "bizdays": unidad = UnidadTiempo.Bizdays; return true;
            case "caldays": unidad = UnidadTiempo.Caldays; return true;
            default: unidad = default; return false;
        }
    }

    private static IHttpResult<TLote>? ValidarLote<TLote, TItem>(IReadOnlyList<TItem>? items)
    {
        if (items is null || items.Count == 0)
            return Falla<TLote>("items", "Envíe al menos un ítem en «items».", HttpStatusCode.BadRequest);
        if (items.Count > MaxItemsLote)
            return Falla<TLote>("items", $"Un lote admite como máximo {MaxItemsLote} ítems.", HttpStatusCode.BadRequest);
        return null;
    }

    private static ErrorItemDto ErrorDe<T>(IHttpResult<T> r)
    {
        var primero = r.Error.Detail.First();
        return new ErrorItemDto((int)r.StatusCode, primero.Key, primero.Value.First());
    }

    private static IHttpResult<T> NoEncontrada<T>() =>
        Falla<T>("localidadId", "Localidad no encontrada.", HttpStatusCode.NotFound);

    private static IHttpResult<T> Horizontes<T>(string mensaje) =>
        Falla<T>("horizonte", mensaje, HttpStatusCode.UnprocessableEntity);

    private static IHttpResult<T> Falla<T>(string clave, string mensaje, HttpStatusCode codigo) =>
        CalendarioResults.Fail<T>(Titulo(codigo), clave, mensaje, codigo);

    private static string Titulo(HttpStatusCode codigo) => codigo switch
    {
        HttpStatusCode.NotFound => "Localidad no encontrada",
        HttpStatusCode.UnprocessableEntity => "Fuera del horizonte de cálculo",
        _ => "Petición inválida",
    };
}
