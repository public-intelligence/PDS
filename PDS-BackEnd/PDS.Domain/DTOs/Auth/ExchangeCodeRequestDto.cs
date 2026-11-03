namespace PDS.Domain.DTOs.Auth;

public record ExchangeCodeRequestDto
{
    public required string Code { get; init; }
    public required string OrganizationApplicationId { get; init; }
}
