namespace PDS.Domain.DTOs.Servicio;

public record ServicioDto
{
    public Guid Id { get; init; }
    public string? Codigo { get; init; }
    public required string Nombre { get; init; }
    public string? Descripcion { get; init; }
}

public record CreateServicioDto
{
    public string? Codigo { get; init; }
    public required string Nombre { get; init; }
    public string? Descripcion { get; init; }
}

public record UpdateServicioDto
{
    public string? Codigo { get; init; }
    public required string Nombre { get; init; }
    public string? Descripcion { get; init; }
}
