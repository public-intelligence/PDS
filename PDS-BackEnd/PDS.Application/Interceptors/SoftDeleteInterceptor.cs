using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PDS.Domain.Models;

namespace PDS.Application.Interceptors;

public class SoftDeleteInterceptor : SaveChangesInterceptor
{
    public bool ApplySoftDelete { get; set; } = false;

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default
    )
    {
        if (eventData.Context is null || !ApplySoftDelete)
            return base.SavingChangesAsync(eventData, result, cancellationToken);

        var entries = eventData
            .Context.ChangeTracker.Entries<ISoftDeletable>()
            .Where(e => e.State == EntityState.Deleted);

        foreach (var softDeletable in entries)
        {
            softDeletable.State = EntityState.Modified;
            softDeletable.Entity.DeletedAt = DateTime.UtcNow;
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
