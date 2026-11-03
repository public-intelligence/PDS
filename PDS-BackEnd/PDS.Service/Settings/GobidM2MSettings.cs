namespace PDS.Service.Settings;

public class GobidM2MSettings
{
    public const string SectionName = "GobidM2M";

    public required string BaseUrl { get; init; }
    public required string ApiVersion { get; init; }
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
    public string? Scope { get; init; }
}
