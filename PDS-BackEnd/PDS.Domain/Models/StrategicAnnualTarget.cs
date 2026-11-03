namespace PDS.Domain.Models;

/// <summary>
/// Meta anual de una unidad de medida del PEI (M3). Normaliza las "metas por año" del
/// formato Excel (una fila por año). REGLA DE INMUTABILIDAD: las metas de años menores
/// o iguales al año actual son historia y no se editan (el servicio la aplica).
/// </summary>
public class StrategicAnnualTarget
{
    public Guid Id { get; set; }
    public Guid MeasureUnitId { get; set; }

    public int Year { get; set; }

    public decimal Value { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual StrategicMeasureUnit? MeasureUnit { get; set; }
}
