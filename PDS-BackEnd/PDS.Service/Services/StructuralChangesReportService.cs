using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PDS.Application.Repositories;
using PDS.Domain.DTOs.LegalInstrument;
using PDS.Domain.DTOs.Reporting;
using PDS.Domain.DTOs.StructureVersion;
using PDS.Domain.Enums;
using PDS.Domain.Models;
using PDS.Domain.Response;
using System.Net;

namespace PDS.Service.Services;

// RF-RE-05: lista las versiones publicadas en el período (cada una = un cambio estructural aprobado),
// con sus actores de aprobación, instrumento legal y el diff frente a su versión predecesora.
public class StructuralChangesReportService(
    ILogger<StructuralChangesReportService> logger,
    IRepository<StructureVersion> versionRepository,
    IComparativeReportService comparativeService
) : IStructuralChangesReportService
{
    private readonly ILogger<StructuralChangesReportService> _logger = logger;
    private readonly IRepository<StructureVersion> _versionRepository = versionRepository;
    private readonly IComparativeReportService _comparativeService = comparativeService;

    public async Task<IHttpResult<StructuralChangesReportDto>> GetAsync(
        int organizationId, StructuralChangesFilterDto filter, string? generatedByName)
    {
        if (filter.To < filter.From)
            return Fail("El rango de fechas es inválido: la fecha final es anterior a la inicial.");

        var fromDt = filter.From.ToDateTime(TimeOnly.MinValue);
        var toDtExclusive = filter.To.AddDays(1).ToDateTime(TimeOnly.MinValue);

        // Versiones publicadas (o ya superseded) cuyo publish cae en el período. Solo publicadas (RF-RE-05).
        var periodResult = await _versionRepository.FilterAsync(
            v => v.OrganizationId == organizationId
                && (v.Status == VersionStatus.Published || v.Status == VersionStatus.Superseded)
                && v.PublishedAt != null
                && v.PublishedAt >= fromDt && v.PublishedAt < toDtExclusive
                && (filter.ApproverId == null
                    || v.ReviewedBy == filter.ApproverId
                    || v.ApprovedBy == filter.ApproverId
                    || v.PublishedBy == filter.ApproverId),
            q => q.Include(v => v.LegalInstrument)
                  .Include(v => v.AuditLogs)
                  .OrderByDescending(v => v.PublishedAt));
        if (!periodResult.IsSuccess)
            return Fail("No se pudieron recuperar las versiones del período.");

        // Todas las versiones materializadas, para resolver predecesores por número.
        var allResult = await _versionRepository.FilterAsync(
            v => v.OrganizationId == organizationId
                && v.Number != null
                && (v.Status == VersionStatus.Published || v.Status == VersionStatus.Superseded),
            q => q.OrderBy(v => v.Number));
        var allByNumber = allResult.IsSuccess
            ? allResult.Value.Where(v => v.Number.HasValue).OrderBy(v => v.Number).ToList()
            : new List<StructureVersion>();

        var entries = new List<StructuralChangeEntryDto>();
        foreach (var version in periodResult.Value)
        {
            var predecessor = allByNumber
                .Where(v => v.Number < version.Number)
                .OrderByDescending(v => v.Number)
                .FirstOrDefault();

            ComparisonSummaryDto? unitSummary = null;
            PositionDeltaSummaryDto? positionSummary = null;
            ICollection<UnitChangeDto> unitChanges = [];

            if (predecessor is not null)
            {
                var comparison = await _comparativeService.GetAsync(
                    organizationId, predecessor.Id, null, version.Id, null, SalaryBasis.Midpoint, generatedByName);
                if (comparison.IsSuccess)
                {
                    unitSummary = comparison.Value.UnitComparison.Summary;
                    positionSummary = comparison.Value.PositionDeltas;
                    unitChanges = comparison.Value.UnitComparison.UnitChanges;
                }
            }

            // Filtros que dependen del diff (tipo de cambio / unidad afectada).
            if (!PassesChangeTypeFilter(filter.ChangeType, unitChanges)) continue;
            if (filter.UnitId.HasValue && !unitChanges.Any(c => c.SourceUnitId == filter.UnitId.Value)) continue;

            entries.Add(new StructuralChangeEntryDto
            {
                VersionId = version.Id,
                Number = version.Number,
                Name = version.Name,
                Status = version.Status,
                EffectiveFrom = version.EffectiveFrom,
                LegalInstrument = MapLegal(version.LegalInstrument),
                CreatedBy = version.CreatedBy,
                CreatedByName = version.CreatedByName,
                CreatedAt = version.CreatedAt,
                ReviewedByName = version.ReviewedByName,
                ReviewedAt = version.ReviewedAt,
                ApprovedByName = version.ApprovedByName,
                ApprovedAt = version.ApprovedAt,
                PublishedByName = version.PublishedByName,
                PublishedAt = version.PublishedAt,
                AuditLogs = version.AuditLogs.OrderBy(a => a.Date).Select(MapAudit).ToList(),
                UnitChangeSummary = unitSummary,
                PositionDeltaSummary = positionSummary
            });
        }

        return HttpResult<StructuralChangesReportDto>.Success(new StructuralChangesReportDto
        {
            From = filter.From,
            To = filter.To,
            Filters = filter,
            Entries = entries
        });
    }

    private static bool PassesChangeTypeFilter(string? changeType, ICollection<UnitChangeDto> changes)
        => string.IsNullOrWhiteSpace(changeType)
           || changes.Any(c => c.ChangeType.Any(t => string.Equals(t, changeType, StringComparison.OrdinalIgnoreCase)));

    private static AuditLogDto MapAudit(StructureVersionAuditLog a) => new()
    {
        Id = a.Id,
        Action = a.Action,
        UserId = a.UserId,
        UserName = a.UserName,
        Date = a.Date,
        Comment = a.Comment
    };

    private static LegalInstrumentDto? MapLegal(LegalInstrument? li) => li is null ? null : new LegalInstrumentDto
    {
        Id = li.Id,
        Type = li.Type,
        Number = li.Number,
        Date = li.Date,
        IssuingAuthority = li.IssuingAuthority,
        DocumentUrl = li.DocumentUrl
    };

    private static HttpResult<StructuralChangesReportDto> Fail(string message, HttpStatusCode code = HttpStatusCode.BadRequest)
        => HttpResult<StructuralChangesReportDto>.Failure("Error en el reporte de cambios estructurales",
            new Dictionary<string, IEnumerable<string>> { { "errors", [message] } }, code);
}
