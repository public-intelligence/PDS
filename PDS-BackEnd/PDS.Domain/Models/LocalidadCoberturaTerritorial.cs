namespace PDS.Domain.Models;

/// <summary>
/// Territorio que atiende/cubre una localidad (RF-LO-01). Se usa el nivel territorial que aplique
/// (región, provincia, municipio o distrito municipal).
/// </summary>
public class LocalidadCoberturaTerritorial
{
    public Guid Id { get; set; }
    public Guid LocalidadId { get; set; }
    public Guid? RegionId { get; set; }
    public Guid? ProvinciaId { get; set; }
    public Guid? MunicipioId { get; set; }
    public Guid? DistritoMunicipalId { get; set; }

    public Localidad? Localidad { get; set; }
    public Region? Region { get; set; }
    public Provincia? Provincia { get; set; }
    public Municipio? Municipio { get; set; }
    public DistritoMunicipal? DistritoMunicipal { get; set; }
}
