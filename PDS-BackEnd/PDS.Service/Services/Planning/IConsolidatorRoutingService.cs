using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PDS.Domain.DTOs.Planning;
using PDS.Domain.Models;
using PDS.Domain.Response;

namespace PDS.Service.Services.Planning;

public interface IConsolidatorRoutingService
{
    Task<RoutingProposalDto> EvaluateAsync(int organizationId, MasterProduct product);
    Task<IHttpResult<int>> ReevaluateAsync(int organizationId, Guid? masterProductId = null, bool onlyUnratified = true, Guid? userId = null, string? userName = null);
    Task<IHttpResult<RulePreviewResultDto>> PreviewAsync(int organizationId, Guid ruleId, int take = 20);
    Task<IHttpResult<IReadOnlyList<ConsolidatorRoutingRuleDto>>> ListRulesAsync(int organizationId);
    Task<IHttpResult<ConsolidatorRoutingRuleDto>> GetRuleByIdAsync(int organizationId, Guid id);
    Task<IHttpResult<ConsolidatorRoutingRuleDto>> CreateRuleAsync(int organizationId, CreateConsolidatorRoutingRuleDto dto);
    Task<IHttpResult<ConsolidatorRoutingRuleDto>> UpdateMetadataAsync(int organizationId, Guid id, UpdateConsolidatorRoutingRuleMetadataDto dto);
    Task<IHttpResult<ConsolidatorRoutingRuleDto>> CreateNewVersionAsync(int organizationId, Guid id, CreateNewRuleVersionDto dto);
    Task<IHttpResult> SetActiveAsync(int organizationId, Guid id, bool isActive);
    Task<IHttpResult> DeleteRuleAsync(int organizationId, Guid id);
}
