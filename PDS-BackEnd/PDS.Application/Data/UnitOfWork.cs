using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PDS.Domain.Response;

namespace PDS.Application.Data;

public class UnitOfWork<TContext> : IUnitOfWork<TContext>
    where TContext : DbContext
{
    private readonly ILogger<UnitOfWork<TContext>> _logger;
    private readonly TContext _context;

    public TContext Context => _context;

    public UnitOfWork(ILogger<UnitOfWork<TContext>> logger, TContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<Result> BeginTransactionAsync()
    {
        if (_context.Database.CurrentTransaction is not null)
            return Result.Failure("A transaction is already in progress.");

        await _context.Database.BeginTransactionAsync();
        return Result.Success();
    }

    public async Task<Result> RollbackAsync()
    {
        if (_context.Database.CurrentTransaction is null)
            return Result.Failure("No transaction to rollback.");

        await _context.Database.RollbackTransactionAsync();
        return Result.Success();
    }

    public async Task<Result> CommitAsync()
    {
        if (_context.Database.CurrentTransaction is null)
            return Result.Failure("No transaction to commit.");

        await _context.Database.CommitTransactionAsync();
        return Result.Success();
    }
}