namespace PDS.Service.Settings;

public class GobidAuthSettings
{
    public const string SectionName = "GobidAuth";

    public required string BaseUrl { get; init; }
    public required string ApiVersion { get; init; }
}
