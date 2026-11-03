using System.Globalization;
using System.Reflection;
using System.Text.Json;
using PDS.Domain.Enums;
using PDS.Domain.Models;
using PDS.Domain.ValueObjects;

namespace PDS.Service.Services.Calendario;

/// <summary>Acciones de la bitácora de calendarios de feriados (<see cref="CalendarioFeriadosAuditoria.Accion"/>).</summary>
public static class CalendarioFeriadosAcciones
{
    public const string Created = "Created";
    public const string Updated = "Updated";
    public const string Deleted = "Deleted";
    public const string DiaAdded = "DiaAdded";
    public const string DiaUpdated = "DiaUpdated";
    public const string DiaRemoved = "DiaRemoved";
    public const string DiasImported = "DiasImported";
}

/// <summary>RF-LO-44: canal por el que llega un cambio. <c>X-Client-Application</c> presente = API externa; ausente = UI de PDS.</summary>
public static class CanalResolver
{
    public const string HeaderName = "X-Client-Application";

    /// <summary>El valor de la cabecera solo identifica el canal para la bitácora: nunca se usa para autorizar.</summary>
    public static CanalCambio FromHeader(string? valorCabecera) =>
        string.IsNullOrWhiteSpace(valorCabecera) ? CanalCambio.PdsUi : CanalCambio.ApiExterna;
}

/// <summary>
/// RF-LO-44: diff JSON <c>{ campo: { from, to } }</c> entre dos instantáneas de un agregado. Solo incluye campos que cambiaron;
/// devuelve <c>null</c> si no hay cambios. Los nombres de campo van en camelCase (contrato de la API).
/// </summary>
public static class AuditDiff
{
    // UnsafeRelaxedJsonEscaping: la bitácora se lee en español (ñ, acentos) sin secuencias \uXXXX; el JSON no se incrusta en HTML.
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = false,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>Campos que nunca forman parte del diff (técnicos o derivados).</summary>
    private static readonly HashSet<string> Excluidos = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(CalendarioFeriados.RowVersion), nameof(CalendarioFeriados.UpdatedAt), nameof(CalendarioFeriados.CreatedAt),
        nameof(LocalidadExcepcionCalendario.ModificadoPor), nameof(LocalidadExcepcionCalendario.ModificadoPorId),
    };

    /// <summary>Instantánea de las propiedades escalares (y listas de intervalos/jornada) de una entidad.</summary>
    public static IReadOnlyDictionary<string, string?> Snapshot(object entidad)
    {
        var resultado = new SortedDictionary<string, string?>(StringComparer.Ordinal);
        foreach (var p in entidad.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!p.CanRead || p.GetIndexParameters().Length > 0 || Excluidos.Contains(p.Name)) continue;
            if (!EsSoportado(p.PropertyType)) continue;
            resultado[ToCamel(p.Name)] = Describir(p.GetValue(entidad));
        }
        return resultado;
    }

    public static string? Compute(object? antes, object? despues) =>
        Compute(antes is null ? Empty : Snapshot(antes), despues is null ? Empty : Snapshot(despues));

    public static string? Compute(IReadOnlyDictionary<string, string?> antes, IReadOnlyDictionary<string, string?> despues)
    {
        var cambios = new SortedDictionary<string, object>(StringComparer.Ordinal);
        foreach (var campo in antes.Keys.Union(despues.Keys))
        {
            antes.TryGetValue(campo, out var a);
            despues.TryGetValue(campo, out var d);
            if (a != d) cambios[campo] = new Dictionary<string, string?> { ["from"] = a, ["to"] = d };
        }
        return cambios.Count == 0 ? null : JsonSerializer.Serialize(cambios, Json);
    }

    private static readonly IReadOnlyDictionary<string, string?> Empty = new Dictionary<string, string?>();

    private static bool EsSoportado(Type t)
    {
        t = Nullable.GetUnderlyingType(t) ?? t;
        return t.IsPrimitive || t.IsEnum || t == typeof(string) || t == typeof(Guid) || t == typeof(DateOnly) ||
               t == typeof(DateTime) || t == typeof(decimal) ||
               typeof(IEnumerable<IntervaloLaboral>).IsAssignableFrom(t) || typeof(IEnumerable<JornadaDia>).IsAssignableFrom(t);
    }

    /// <summary>Representación textual estable y legible de un valor, para comparar y mostrar.</summary>
    public static string? Describir(object? valor) => valor switch
    {
        null => null,
        string s => s,
        bool b => b ? "true" : "false",
        DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        DateTime dt => dt.ToString("yyyy-MM-ddTHH:mm:ss'Z'", CultureInfo.InvariantCulture),
        Enum e => e.ToString(),
        IntervaloLaboral i => i.ToString(),
        IEnumerable<IntervaloLaboral> ivs => string.Join(", ", ivs.OrderBy(i => i.InicioMinuto).Select(i => i.ToString())),
        IEnumerable<JornadaDia> dias => string.Join("; ", dias.OrderBy(d => d.Dia)
            .Select(d => $"{d.Dia}: {(d.Intervalos.Count == 0 ? "-" : string.Join(", ", d.Intervalos.OrderBy(i => i.InicioMinuto)))}")),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => valor.ToString(),
    };

    private static string ToCamel(string nombre) => char.ToLowerInvariant(nombre[0]) + nombre[1..];
}
