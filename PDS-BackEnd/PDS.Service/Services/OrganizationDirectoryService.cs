using Microsoft.Extensions.Logging;
using PDS.Application.Repositories;
using PDS.Domain.DTOs.Organization;
using PDS.Domain.Models;
using PDS.Domain.Response;
using System.Globalization;
using System.Net;
using System.Text;

namespace PDS.Service.Services;

public class OrganizationDirectoryService(
    ILogger<OrganizationDirectoryService> logger,
    IRepository<OrganizationDirectory> repository
) : IOrganizationDirectoryService
{
    private readonly ILogger<OrganizationDirectoryService> _logger = logger;
    private readonly IRepository<OrganizationDirectory> _repository = repository;

    public async Task UpsertAsync(int organizationId, string? name, string? acronym)
    {
        try
        {
            var displayName = string.IsNullOrWhiteSpace(name) ? $"Organización {organizationId}" : name.Trim();
            var baseSlug = Slugify(!string.IsNullOrWhiteSpace(acronym) ? acronym! : displayName);
            if (string.IsNullOrEmpty(baseSlug)) baseSlug = $"org-{organizationId}";

            var existingResult = await _repository.GetAsync(o => o.Id == organizationId);
            var existing = existingResult.IsSuccess ? existingResult.Value : null;

            // El slug debe ser único frente a OTRAS organizaciones; si choca, lo desambiguamos con el id.
            var slug = baseSlug;
            var clashResult = await _repository.GetAsync(o => o.Slug == slug && o.Id != organizationId);
            if (clashResult.IsSuccess && clashResult.Value is not null)
                slug = $"{baseSlug}-{organizationId}";

            if (existing is null)
            {
                await _repository.CreateAsync(new OrganizationDirectory
                {
                    Id = organizationId,
                    Name = displayName,
                    Acronym = acronym?.Trim(),
                    Slug = slug,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            else
            {
                existing.Name = displayName;
                existing.Acronym = acronym?.Trim();
                // Mantener estable el slug ya publicado para no romper enlaces públicos existentes.
                if (string.IsNullOrWhiteSpace(existing.Slug))
                    existing.Slug = slug;
                existing.UpdatedAt = DateTime.UtcNow;
                await _repository.UpdateAsync(existing);
            }
        }
        catch (Exception ex)
        {
            // El directorio es auxiliar: un fallo aquí no debe tumbar la publicación.
            _logger.LogError(ex, "No se pudo actualizar el directorio de la organización {OrganizationId}", organizationId);
        }
    }

    public async Task<IHttpResult<OrganizationDirectoryDto>> ResolveBySlugAsync(string slug)
    {
        var normalized = Slugify(slug);
        var result = await _repository.GetAsync(o => o.Slug == normalized);
        if (!result.IsSuccess)
            return Fail("No se pudo resolver la organización.", HttpStatusCode.BadRequest);
        if (result.Value is null)
            return Fail("No existe una organización pública con ese identificador.", HttpStatusCode.NotFound);

        var o = result.Value;
        return HttpResult<OrganizationDirectoryDto>.Success(new OrganizationDirectoryDto
        {
            OrganizationId = o.Id,
            Name = o.Name,
            Acronym = o.Acronym,
            Slug = o.Slug
        });
    }

    private static HttpResult<OrganizationDirectoryDto> Fail(string message, HttpStatusCode code)
        => HttpResult<OrganizationDirectoryDto>.Failure("Organización",
            new Dictionary<string, IEnumerable<string>> { { "errors", [message] } }, code);

    /// <summary>Normaliza a slug: minúsculas, sin acentos, alfanumérico separado por guiones.</summary>
    private static string Slugify(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(c)) sb.Append(c);
            else if (c is ' ' or '-' or '_' or '.') sb.Append('-');
        }

        var slug = sb.ToString();
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        return slug.Trim('-');
    }
}
