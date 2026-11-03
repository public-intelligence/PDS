namespace PDS.Domain.DTOs.Auth;

public record ExchangeCodeGobidRequestDto
{
    public required string Code { get; init; }
    public required string RedirectUri { get; init; }
    public required string OrganizationApplicationId { get; init; }
}
