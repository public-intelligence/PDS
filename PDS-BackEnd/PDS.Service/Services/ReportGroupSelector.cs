using PDS.Domain.Enums;

namespace PDS.Service.Services;

/// <summary>
/// Resuelve la clave/etiqueta/orden de grupo para una fila hoja según la dimensión de agrupación.
/// Compartido por RF-RE-01 (plazas autorizadas) y RF-RE-03 (costo estimado).
/// </summary>
internal static class ReportGroupSelector
{
    public readonly record struct GroupSel(string Key, string Label, double Sort);

    public static GroupSel Select(
        ReportGrouping groupBy, Guid? unitId, string? unitCode, string? unitName,
        int? level, int grade, string? occupationalGroupName)
        => groupBy switch
        {
            ReportGrouping.HierarchyLevel =>
                new GroupSel($"L{level ?? 0}", $"Nivel {level ?? 0}", level ?? 0),
            ReportGrouping.SalaryCategory =>
                new GroupSel($"G{grade}", $"Grado {grade}", grade),
            ReportGrouping.OccupationalGroup =>
                new GroupSel(occupationalGroupName ?? "(Sin grupo)", occupationalGroupName ?? "(Sin grupo)", 0),
            _ =>
                new GroupSel(
                    unitId?.ToString() ?? "—",
                    string.IsNullOrWhiteSpace(unitCode) ? (unitName ?? "—") : $"{unitCode} — {unitName}",
                    level ?? 0)
        };
}
