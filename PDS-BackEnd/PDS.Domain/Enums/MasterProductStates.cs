namespace PDS.Domain.Enums;

/// <summary>
/// Estados de gobierno de un artículo de la Maestra (M8). Se persisten como string,
/// igual que <see cref="AnnualPlanStatuses"/>, para que el valor de la BD sea legible
/// y el catálogo pueda crecer sin migración.
/// </summary>
public static class MasterProductStates
{
    /// <summary>Sin área consolidadora resuelta: no aparece en el buscador del POA.</summary>
    public const string Pending = "Pending";

    /// <summary>Área consolidadora resuelta: se puede requerir desde una tarea del POA.</summary>
    public const string Active = "Active";

    /// <summary>Propuesto por un área desde «No encuentro lo que necesito»; espera a Planificación.</summary>
    public const string Requested = "Requested";

    /// <summary>Desapareció del archivo fuente o se retiró. Nunca se borra: puede estar referenciado.</summary>
    public const string Disabled = "Disabled";

    public static readonly IReadOnlyCollection<string> Todos =
        [Pending, Active, Requested, Disabled];
}

/// <summary>
/// Origen del área consolidadora de un artículo. <see cref="Manual"/> congela la fila:
/// el motor de reglas nunca la sobrescribe.
/// </summary>
public static class ConsolidatorAreaSources
{
    public const string None = "None";
    public const string Imported = "Imported";
    public const string Rule = "Rule";
    public const string Manual = "Manual";
}
