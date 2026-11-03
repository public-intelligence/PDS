using NodaTime;
using PDS.Domain.Enums;
using PDS.Domain.Models;
using PDS.Domain.ValueObjects;

namespace PDS.Service.Services.Calendario.Motor;

// SPEC-LOC-CAL-01 §15.4. Tipos puros del motor: sin EF, sin E/S, sin reloj. Los servicios construyen el snapshot.

/// <summary>Versión de la jornada semanal: intervalos locales por día ISO (1 = lunes). Un día ausente o vacío es no laborable.</summary>
public sealed record JornadaVersion(DateOnly VigenteDesde, IReadOnlyDictionary<int, IReadOnlyList<IntervaloLaboral>> Semana)
{
    public static JornadaVersion From(DateOnly vigenteDesde, IEnumerable<JornadaDia> semana) =>
        new(vigenteDesde, semana.GroupBy(d => d.Dia).ToDictionary(g => g.Key, g => (IReadOnlyList<IntervaloLaboral>)g.SelectMany(d => d.Intervalos).ToList()));

    public static JornadaVersion From(LocalidadJornada j) => From(j.VigenteDesde, j.Semana);

    public IReadOnlyList<IntervaloLaboral> Intervalos(int diaIso) =>
        Semana.TryGetValue(diaIso, out var iv) ? iv : Array.Empty<IntervaloLaboral>();
}

/// <summary>
/// Entrada de un calendario de feriados al que la localidad está suscrita. La vigencia de la suscripción viaja con la entrada:
/// el motor solo la aplica si <c>SuscripcionDesde ≤ d ≤ SuscripcionHasta</c> (nulo = sin límite). Los datos del calendario
/// (país, ámbito) permiten a los consumidores derivar la fuente host/home/mission.
/// </summary>
public sealed record FeriadoAplicable(
    Guid CalendarioFeriadosId,
    string CalendarioCodigo,
    Guid? PaisId,
    AmbitoCalendarioFeriados Ambito,
    DateOnly Fecha,
    string Nombre,
    AlcanceFeriado Alcance,
    IReadOnlyList<IntervaloLaboral> Intervalos,
    DateOnly? SuscripcionDesde = null,
    DateOnly? SuscripcionHasta = null,
    Guid? DiaId = null);

/// <summary>Excepción de usuario o de sistema. <see cref="FechaFin"/> nula = abierta (solo sistema).</summary>
public sealed record ExcepcionAplicable(
    Guid Id,
    TipoExcepcionCalendario Tipo,
    CategoriaExcepcionCalendario Categoria,
    OrigenExcepcionCalendario Origen,
    DateOnly FechaInicio,
    DateOnly? FechaFin,
    IReadOnlyList<IntervaloLaboral> Intervalos,
    string Motivo,
    bool EsSistema,
    string? ReferenciaNormativa = null)
{
    public bool Cubre(DateOnly d) => FechaInicio <= d && (FechaFin is null || d <= FechaFin);
}

/// <summary>Todo lo que el motor necesita saber de una localidad. Las listas llegan sin orden garantizado.</summary>
public sealed record CalendarioSnapshot(
    Guid LocalidadId,
    string ZonaHoraria,
    long CalendarioVersion,
    DateOnly? FechaApertura,
    IReadOnlyList<JornadaVersion> Jornadas,
    IReadOnlyList<FeriadoAplicable> Feriados,
    IReadOnlyList<ExcepcionAplicable> Excepciones);

/// <summary>Intervalo en UTC (instantes NodaTime), semiabierto [Inicio, Fin).</summary>
public sealed record IntervaloUtc(Instant Inicio, Instant Fin);

/// <summary>Explica por qué un día es como es (nombre del feriado, motivo de la excepción) con sus identificadores.</summary>
public sealed record MotivoDia(
    string Tipo,
    string Nombre,
    Guid? CalendarioFeriadosId = null,
    Guid? ExcepcionId = null,
    string? CalendarioCodigo = null,
    Guid? PaisId = null,
    AmbitoCalendarioFeriados? Ambito = null,
    AlcanceFeriado? Alcance = null,
    string? ReferenciaNormativa = null);

/// <summary>Resultado de resolver un día local (§11.3.5).</summary>
public sealed record DiaEfectivo(
    DateOnly Fecha,
    string DiaSemana,
    bool Laborable,
    IReadOnlyList<IntervaloLaboral> Intervalos,
    IReadOnlyList<IntervaloUtc> IntervalosUtc,
    FuenteDiaCalendario Fuente,
    IReadOnlyList<MotivoDia> Motivos,
    IReadOnlyList<IntervaloLaboral> IntervalosJornada);

/// <summary>Estado del calendario en un instante (RF-LO-34 <c>estado</c>).</summary>
public sealed record EstadoCalendario(bool Laborando, IntervaloUtc? IntervaloActual, Instant? ProximoInstanteLaborable);
