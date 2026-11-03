using Microsoft.EntityFrameworkCore;
using PDS.Application.Data;

namespace PDS.Service.Services.Planning;

public sealed class SequenceService : ISequenceService
{
    private readonly IUnitOfWork<PDSDbContext> _unitOfWork;

    public SequenceService(IUnitOfWork<PDSDbContext> unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> NextAsync(int organizationId, string scope, string scopeKey)
    {
        // MERGE atómico: incrementa si existe, inserta con NextValue=2 si no (el número
        // reservado es NextValue-1 en ambos casos). HOLDLOCK serializa reservas concurrentes.
        var allocated = await _unitOfWork.Context.Database
            .SqlQuery<int>($@"
                MERGE [Planning].[PlanningSequences] WITH (HOLDLOCK) AS target
                USING (SELECT {organizationId} AS OrganizationId, {scope} AS Scope, {scopeKey} AS ScopeKey) AS source
                ON target.OrganizationId = source.OrganizationId
                    AND target.Scope = source.Scope
                    AND target.ScopeKey = source.ScopeKey
                WHEN MATCHED THEN UPDATE SET NextValue = target.NextValue + 1
                WHEN NOT MATCHED THEN INSERT (Id, OrganizationId, Scope, ScopeKey, NextValue)
                    VALUES (NEWID(), source.OrganizationId, source.Scope, source.ScopeKey, 2)
                OUTPUT inserted.NextValue - 1 AS Value;")
            .ToListAsync();

        return allocated[0];
    }
}
