namespace PDS.Domain.Enums;

/// <summary>
/// Niveles del árbol UNSPSC en la matriz de códigos unificada (M2).
/// Se almacena como string en la BD para legibilidad.
/// </summary>
public enum UnifiedCodeLevel
{
    Segment,
    Family,
    Class,
    Material
}
