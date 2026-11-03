namespace PDS.Domain.Models;

/// <summary>
/// Valor personalizado de un producto POA para una columna de la plantilla (M5).
/// </summary>
public class PoaProductCustomValue
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid TemplateColumnId { get; set; }

    public string? Value { get; set; }

    public virtual PoaProduct? Product { get; set; }
    public virtual PlanningTemplateColumn? TemplateColumn { get; set; }
}
