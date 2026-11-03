namespace PDS.Domain.Models;

/// <summary>
/// Unidad organizativa con presencia operativa en una localidad (RF-LO-01). N:M con la estructura.
/// </summary>
public class LocalidadUnidadPresencia
{
    public Guid Id { get; set; }
    public Guid LocalidadId { get; set; }
    public Guid UnidadOrganizativaId { get; set; }
    public string? Notas { get; set; }

    public Localidad? Localidad { get; set; }
    public OrganizationalStructure? UnidadOrganizativa { get; set; }
}
