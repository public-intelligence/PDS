namespace PDS.Service.Settings;

/// <summary>
/// Configuración del calendario laboral de localidades (sección <c>Calendario</c>, SPEC-LOC-CAL-01 Apéndice C).
/// Mientras <see cref="Habilitado"/> sea false las escrituras del calendario se rechazan; se activa junto con el flag
/// del frontend una vez cargados los datos migrados.
/// </summary>
public class CalendarioSettings
{
    public const string SectionName = "Calendario";

    public bool Habilitado { get; init; }

    /// <summary>RF-LO-33: días hacia atrás que un gestor no administrador puede modificar.</summary>
    public int VentanaRetroactivaDias { get; init; } = 7;

    /// <summary>RF-LO-34: rango máximo (días) del calendario efectivo.</summary>
    public int MaxRangoDias { get; init; } = 400;

    /// <summary>RF-LO-35: horizonte de búsqueda (días) de los cálculos de tiempo laborable.</summary>
    public int HorizonteCalculoDias { get; init; } = 366;

    /// <summary>RF-LO-43: retención mínima (días) del feed de cambios.</summary>
    public int RetencionCambiosDias { get; init; } = 180;

    /// <summary>RF-LO-43: si el proceso en segundo plano purga el feed vencido. Solo actúa con <see cref="Habilitado"/>.</summary>
    public bool PurgaCambiosHabilitada { get; init; } = true;

    /// <summary>RF-LO-43: cada cuántas horas corre la purga del feed.</summary>
    public int PurgaCambiosIntervaloHoras { get; init; } = 24;

    /// <summary>RF-LO-22: máximo de ids por lote.</summary>
    public int MaxIdsLote { get; init; } = 200;
}
