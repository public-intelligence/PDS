namespace PDS.Domain.Enums;

/// <summary>
/// Dimensión de agrupación/pivote para los reportes de plazas autorizadas y costo estimado.
/// SalaryCategory equivale al grado salarial (Position.Grade), distinto del grupo ocupacional.
/// </summary>
public enum ReportGrouping
{
    Unit,
    HierarchyLevel,
    SalaryCategory,
    OccupationalGroup
}
