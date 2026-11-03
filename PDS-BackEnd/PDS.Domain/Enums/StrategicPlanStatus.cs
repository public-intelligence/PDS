namespace PDS.Domain.Enums;

/// <summary>
/// Estados del versionado del PEI (M3). Se almacena como string en la BD para legibilidad
/// (enum documental, igual que <see cref="UnifiedCodeLevel"/>): como máximo un Draft y un
/// Current por organización (índices únicos filtrados).
/// </summary>
public enum StrategicPlanStatus
{
    /// <summary>Enmienda en elaboración (única versión editable).</summary>
    Draft,

    /// <summary>Versión vigente: la que consume el POA para alinear.</summary>
    Current,

    /// <summary>Versión histórica congelada (solo consulta).</summary>
    Historical
}
