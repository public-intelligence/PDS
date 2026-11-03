using PDS.Domain.Models;

namespace PDS.Service.Services.Planning;

public sealed class PoaValidationService : IPoaValidationService
{
    public IReadOnlyCollection<string> GetProductIssues(
        PoaProduct product,
        IReadOnlyCollection<PlanningTemplateColumn> requiredColumns)
    {
        var issues = new List<string>();

        if (string.IsNullOrWhiteSpace(product.ProductName) && product.MeasureUnits.All(u => string.IsNullOrWhiteSpace(u.Name) && u.TargetValue <= 0))
        {
            return ["El producto no ha sido configurado. Complete sus campos o elimínelo si no lo utilizará."];
        }

        if (string.IsNullOrWhiteSpace(product.ProductName))
            issues.Add("Falta el producto");

        if (string.Equals(product.Origin, "Operational", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(product.OperationalJustification))
                issues.Add("Falta la justificación del producto operativo");
        }
        else if (product.StrategicFocusId is null || product.StrategicObjectiveId is null)
        {
            issues.Add("Falta la alineación estratégica");
        }

        if (product.MeasureUnits.Count == 0)
            issues.Add("Falta al menos una unidad de medida");

        foreach (var unit in product.MeasureUnits)
        {
            var label = product.MeasureUnits.Count > 1
                ? $" ({(string.IsNullOrWhiteSpace(unit.Name) ? "unidad sin nombre" : unit.Name)})"
                : string.Empty;

            if (string.IsNullOrWhiteSpace(unit.Name))
                issues.Add("Unidad de medida sin nombre");
            if (unit.TargetValue <= 0)
                issues.Add("Falta la meta" + label);
            if (unit.ResponsibleAreaId is null)
                issues.Add("Falta el área responsable" + label);
            if (string.IsNullOrWhiteSpace(unit.VerificationMeans))
                issues.Add("Falta el medio de verificación" + label);

            var quarterSum = unit.Q1 + unit.Q2 + unit.Q3 + unit.Q4;
            if (unit.TargetValue > 0)
            {
                if (string.Equals(unit.TargetType, "Absolute", StringComparison.OrdinalIgnoreCase)
                    && quarterSum != unit.TargetValue)
                {
                    issues.Add("La programación trimestral no suma la meta" + label);
                }

                if (string.Equals(unit.TargetType, "Percentage", StringComparison.OrdinalIgnoreCase)
                    && unit.Q4 != unit.TargetValue)
                {
                    issues.Add("El T4 debe alcanzar la meta anual" + label);
                }
            }
        }

        if (product.Municipalities.Count == 0)
            issues.Add("Faltan los municipios de impacto");

        foreach (var column in requiredColumns.Where(c => string.Equals(c.Origin, "Custom", StringComparison.OrdinalIgnoreCase)))
        {
            var value = product.CustomValues
                .FirstOrDefault(cv => cv.TemplateColumnId == column.Id)?.Value;
            if (string.IsNullOrWhiteSpace(value))
                issues.Add($"Falta «{column.VisibleName}»");
        }

        return issues;
    }
}
