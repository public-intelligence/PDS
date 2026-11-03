namespace PDS.Domain.DTOs.Territorio;

public record PaisDto
{
    public Guid Id { get; init; }
    public required string Codigo { get; init; }
    public required string Nombre { get; init; }
    public string? CodigoIso3 { get; init; }
}

public record RegionDto
{
    public Guid Id { get; init; }
    public required string Codigo { get; init; }
    public required string Nombre { get; init; }
    public Guid PaisId { get; init; }
}

public record ProvinciaDto
{
    public Guid Id { get; init; }
    public required string Codigo { get; init; }
    public required string Nombre { get; init; }
    public Guid PaisId { get; init; }
    public Guid? RegionId { get; init; }
}

public record MunicipioDto
{
    public Guid Id { get; init; }
    public required string Codigo { get; init; }
    public required string Nombre { get; init; }
    public Guid ProvinciaId { get; init; }
}

public record DistritoMunicipalDto
{
    public Guid Id { get; init; }
    public required string Codigo { get; init; }
    public required string Nombre { get; init; }
    public Guid MunicipioId { get; init; }
}
