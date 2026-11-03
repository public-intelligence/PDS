namespace PDS.Domain.DTOs.Organization;

/// <summary>Información pública mínima de una organización, resuelta por slug para el organigrama público.</summary>
public record OrganizationDirectoryDto
{
    public int OrganizationId { get; init; }
    public required string Name { get; init; }
    public string? Acronym { get; init; }
    public required string Slug { get; init; }
}
