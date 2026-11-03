using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PDS.Domain.DTOs.Planning;
using PDS.Domain.Response;
using PDS.Service.Settings;
using System.Net;
using System.Text.Json;

namespace PDS.Service.Services.Planning;

/// <summary>
/// Implementación del cliente Personnel de GOBID. El Bearer M2M lo inyecta GobidM2MBearerHandler
/// (encadenado al HttpClient tipado en Program.cs). El JSON se parsea de forma defensiva:
/// acepta el objeto directo o el envelope { data: {...} } que usa GOBID, y busca positionId
/// con independencia de mayúsculas.
/// </summary>
public sealed class GobidPersonnelClient : IGobidPersonnelClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GobidPersonnelClient> _logger;
    private readonly GobidPersonnelSettings _settings;
    private readonly GobidM2MSettings _m2mSettings;

    public GobidPersonnelClient(
        HttpClient httpClient,
        IOptions<GobidPersonnelSettings> settings,
        IOptions<GobidM2MSettings> m2mSettings,
        ILogger<GobidPersonnelClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _m2mSettings = m2mSettings.Value;
        _logger = logger;
    }

    public async Task<IHttpResult<GobidPersonnelDto>> GetPersonnelAsync(string personnelId, CancellationToken ct = default)
    {
        var endpoint = _settings.PathTemplate
            .Replace("{apiVersion}", _m2mSettings.ApiVersion, StringComparison.OrdinalIgnoreCase)
            .Replace("{personnelId}", Uri.EscapeDataString(personnelId), StringComparison.OrdinalIgnoreCase);

        try
        {
            using var response = await _httpClient.GetAsync(endpoint, ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return Fail("El personal no existe en GOBID", HttpStatusCode.NotFound);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("GOBID Personnel: GET {Endpoint} → {Status}", endpoint, (int)response.StatusCode);
                return Fail("GOBID no pudo devolver el registro de personal", HttpStatusCode.BadGateway);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            var root = UnwrapData(json.RootElement);
            if (root.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                return Fail("El personal no existe en GOBID", HttpStatusCode.NotFound);

            // `position` llega como objeto: de ahí salen el nombre del cargo y —lo importante—
            // la unidad organizacional, que es lo que permite resolver el área sin depender de
            // que el positionId de GOBID exista en la tabla Positions de PDS.
            JsonElement? positionObject = TryGetPropertyIgnoreCase(root, "position", out var pos)
                && pos.ValueKind == JsonValueKind.Object
                    ? pos
                    : null;

            JsonElement? structureObject = positionObject is { } p
                && TryGetPropertyIgnoreCase(p, "organizationalStructure", out var st)
                && st.ValueKind == JsonValueKind.Object
                    ? st
                    : null;

            var dto = new GobidPersonnelDto
            {
                PersonnelId = personnelId,
                PositionId = GetString(root, "positionId")
                    ?? GetString(root, "positionID")
                    ?? (positionObject is { } po ? GetString(po, "id") : null),
                PositionName = GetString(root, "positionName")
                    ?? (positionObject is { } pn ? GetString(pn, "name") : null)
                    ?? GetString(root, "position"),
                OrganizationalStructureId = structureObject is { } s ? GetString(s, "id") : null,
                OrganizationalStructureName = structureObject is { } sn ? GetString(sn, "name") : null,
                FullName = GetString(root, "fullName")
                    ?? JoinNames(GetString(root, "firstName"), GetString(root, "lastName"))
            };

            return HttpResult<GobidPersonnelDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al consultar el personal {PersonnelId} en GOBID.", personnelId);
            return Fail("No se pudo contactar la Personnel API de GOBID", HttpStatusCode.BadGateway);
        }
    }

    /// <summary>GOBID suele envolver la respuesta en { data: {...} }; si no, usa el objeto raíz.</summary>
    private static JsonElement UnwrapData(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Object
            && TryGetPropertyIgnoreCase(root, "data", out var data))
        {
            return data;
        }

        return root;
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;

        if (!TryGetPropertyIgnoreCase(element, propertyName, out var property))
            return null;

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number => property.GetRawText(),
            _ => null
        };
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static string? JoinNames(string? firstName, string? lastName)
    {
        var full = $"{firstName} {lastName}".Trim();
        return string.IsNullOrWhiteSpace(full) ? null : full;
    }

    private static HttpResult<GobidPersonnelDto> Fail(string message, HttpStatusCode code)
    {
        var detail = new Dictionary<string, IEnumerable<string>> { { "errors", [message] } };
        return HttpResult<GobidPersonnelDto>.Failure("Error al consultar GOBID Personnel", detail, code);
    }
}
