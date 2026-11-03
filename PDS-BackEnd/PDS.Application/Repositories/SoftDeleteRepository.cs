using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PDS.Application.Data;
using PDS.Application.Interceptors;
using PDS.Domain.Models;
using PDS.Domain.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace PDS.Application.Repositories;

public class SoftDeleteRepository<TEntity> : GenericRepository<TEntity>
    where TEntity : class, ISoftDeletable
{
    /// <summary>
    /// Creates a new instance of the <see cref="SoftDeleteRepository{TEntity}"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="context">The database context.</param>
    public SoftDeleteRepository(
        ILogger<SoftDeleteRepository<TEntity>> logger,
        IUnitOfWork<PDSDbContext> unitOfWork,
        SoftDeleteInterceptor softDeleteInterceptor
    ) : base(logger, unitOfWork, softDeleteInterceptor)
    {
    }

    /// <summary>
    /// Creates an entity in the database.
    /// </summary>
    /// <param name="entity">The entity to create.</param>
    /// <returns>If the entity was created successfully, a success result with the entity; otherwise, a failure result.</returns>
    public override async Task<IResult<TEntity>> CreateAsync(TEntity entity)
    {
        _dbSet.Add(entity);
        _logger.LogInformation("Entity added to context.");

        _logger.LogInformation("Saving changes to database...");
        await _unitOfWork.Context.SaveChangesAsync();
        _logger.LogInformation("Entity saved to database.");

        return Result<TEntity>.Success(entity);
    }

    /// <summary>
    /// Adds a collection of entities to the database and saves the changes asynchronously.
    /// </summary>
    /// <remarks>This method adds the specified entities to the database context and commits the changes 
    /// immediately. The returned result object contains the added entities if the operation succeeds.</remarks>
    /// <param name="entities">The collection of entities to add. Cannot be null.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a result object  indicating the
    /// success or failure of the operation, along with the added entities.</returns>
    public override async Task<IResult<IEnumerable<TEntity>>> CreateRangeAsync(IEnumerable<TEntity> entities)
    {
        _dbSet.AddRange(entities);
        _logger.LogInformation("Entity added to context.");

        _logger.LogInformation("Saving changes to database...");
        await _unitOfWork.Context.SaveChangesAsync();
        _logger.LogInformation("Entity saved to database.");

        return Result<IEnumerable<TEntity>>.Success(entities);
    }

    /// <summary>
    /// Deletes an entity from the database.
    /// </summary>
    /// <param name="entity">The entity to delete.</param>
    /// <returns>If the entity was deleted successfully, a success result; otherwise, a failure result.</returns>
    public override async Task<IResult> DeleteAsync(TEntity entity)
    {
        _softDeleteInterceptor.ApplySoftDelete = true;

        _dbSet.Remove(entity);
        _logger.LogInformation("Entity removed from context.");

        _logger.LogInformation("Saving changes to database...");
        await _unitOfWork.Context.SaveChangesAsync();
        _logger.LogInformation("Entity removed from database.");

        return Result.Success();
    }

    public override async Task<IResult> DeleteRangeAsync(IEnumerable<TEntity> entities)
    {
        _softDeleteInterceptor.ApplySoftDelete = true;

        _dbSet.RemoveRange(entities);
        _logger.LogInformation("Entities removed from context.");

        _logger.LogInformation("Saving changes to database...");
        await _unitOfWork.Context.SaveChangesAsync();
        _logger.LogInformation("Entities removed from database.");

        return Result.Success();
    }

    public override async Task<IResult> DeleteRangeAsync(Expression<Func<TEntity, bool>> expression)
    {
        var result = await FilterAsync(expression);
        if (!result.IsSuccess)
            return result;

        var entities = result.Value;
        if (!entities.Any())
        {
            var error = "Entities not found";
            return Result.Failure(error);
        }

        await DeleteRangeAsync(entities);

        return Result.Success();
    }

    /// <summary>
    /// Deletes an entity from the database.
    /// </summary>
    /// <param name="expression">The expression to find the entity to delete.</param>
    /// <returns>If the entity was deleted successfully, a success result; otherwise, a failure result.</returns>
    public override async Task<IResult> DeleteAsync(Expression<Func<TEntity, bool>> expression)
    {
        var result = await GetAsync(expression);
        if (!result.IsSuccess)
            return result;

        var entity = result.Value;
        if (entity == null)
        {
            var error = "Entity not found.";
            return Result.Failure(error);
        }

        await DeleteAsync(entity);

        return Result.Success();
    }

    /// <summary>
    /// Filters entities in the database.
    /// </summary>
    /// <param name="expression">The expression to filter the entities.</param>
    /// <returns>If the entities were found, a success result with the entities; otherwise, a failure result.</returns>
    public override async Task<IResult<IEnumerable<TEntity>>> FilterAsync(
        Expression<Func<TEntity, bool>> expression
    )
    {
        var entities = await _dbSet.Where(expression).Where(e => e.DeletedAt == null).ToListAsync();
        return Result<IEnumerable<TEntity>>.Success(entities);
    }

    /// <summary>
    /// Gets all entities from the database.
    /// </summary>
    /// <returns>If the entities were found, a success result with the entities; otherwise, a failure result.</returns>
    public override async Task<IResult<IEnumerable<TEntity>>> GetAllAsync()
    {
        var entities = await _dbSet.Where(e => e.DeletedAt == null).ToListAsync();
        return Result<IEnumerable<TEntity>>.Success(entities);
    }

    /// <summary>
    /// Gets a paginated list of entities from the database.
    /// </summary>
    /// <param name="pageNumber">The page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <returns>If the entities were found, a success result with the entities; otherwise, a failure result.</returns>
    public override async Task<IResult<IEnumerable<TEntity>>> GetPaginatedAsync(
        int pageNumber = 1,
        int pageSize = 50
    )
    {
        var checkPaginationResult = CheckPagination(pageNumber, pageSize);
        if (!checkPaginationResult.IsSuccess)
            return Result<IEnumerable<TEntity>>.Failure(checkPaginationResult.Error);

        var entities = await _dbSet

            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return Result<IEnumerable<TEntity>>.Success(entities);
    }

    public override async Task<IResult<TEntity?>> GetAsync(Expression<Func<TEntity, bool>> expression)
    {
        var query = _dbSet.Where(expression).Where(e => e.DeletedAt == null);
        if (!query.Any())
        {
            _logger.LogInformation("Entity not found.");
            return Result<TEntity?>.Success(null);
        }

        if (query.Count() > 1)
        {
            _logger.LogInformation("Multiple entities found.");
            return Result<TEntity?>.Failure("Multiple entities found.");
        }

        var entity = await query.SingleAsync();
        return Result<TEntity?>.Success(entity);
    }
    public override async Task<IResult<TEntity?>> GetWithDeletedAsync(
    Expression<Func<TEntity, bool>> expression,
    Func<IQueryable<TEntity>, IQueryable<TEntity>> query
)
    {
        var queryResult = query(_dbSet.Where(expression)); // No filtramos por DeletedAt
        if (await queryResult.CountAsync() > 1)
        {
            _logger.LogInformation("Multiple entities found.");
            return Result<TEntity?>.Failure("Multiple entities found.");
        }

        var entity = await queryResult.SingleOrDefaultAsync();
        return Result<TEntity?>.Success(entity);
    }

    /// <summary>
    /// Gets an entity from the database.
    /// </summary>
    /// <param name="expression">The expression to find the entity.</param>
    /// <returns>If the entity was found, a success result with the entity; otherwise, a failure result.</returns>
    public override async Task<IResult<TEntity?>> GetAsync(
        Expression<Func<TEntity, bool>> expression,
        Func<IQueryable<TEntity>, IQueryable<TEntity>> query
    )
    {
        var queryResult = query(_dbSet.Where(expression).Where(e => e.DeletedAt == null));
        if (queryResult.Count() > 1)
        {
            _logger.LogInformation("Multiple entities found.");
            return Result<TEntity?>.Failure("Multiple entities found.");
        }

        var entity = await queryResult.SingleOrDefaultAsync();
        return Result<TEntity?>.Success(entity);
    }

    /// <summary>
    /// Partially updates an entity in the database.
    /// </summary>
    /// <param name="expression">The expression to find the entity to update.</param>
    /// <param name="entity">The entity with the updated values.</param>
    /// <returns>If the entity was updated successfully, the updated entity; otherwise, a failure result.</returns>
    public override async Task<IResult<TEntity>> PartialUpdateAsync(
        Expression<Func<TEntity, bool>> expression,
        TEntity entity
    )
    {
        var result = await GetAsync(expression);
        if (!result.IsSuccess)
        {
            var error = result.Error;
            return Result<TEntity>.Failure(error);
        }

        if (result.Value == null)
        {
            var error = "Entity not found.";
            return Result<TEntity>.Failure(error);
        }

        var entityToUpdate = result.Value;
        _unitOfWork.Context.Entry(entityToUpdate).CurrentValues.SetValues(entity);
        await _unitOfWork.Context.SaveChangesAsync();

        return Result<TEntity>.Success(entityToUpdate);
    }

    /// <summary>
    /// Updates an entity in the database.
    /// </summary>
    /// <param name="expression">The expression to find the entity to update.</param>
    /// <param name="entity">The entity with the updated values.</param>
    /// <returns>If the entity was updated successfully, the updated entity; otherwise, a failure result.</returns>
    public override async Task<IResult<TEntity>> UpdateAsync(TEntity entity)
    {
        _unitOfWork.Context.Update(entity);

        await _unitOfWork.Context.SaveChangesAsync();
        return Result<TEntity>.Success(entity);
    }

    /// <summary>
    /// Checks if the page number and page size are valid.
    /// </summary>
    /// <param name="pageNumber">The page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <returns>A result indicating whether the page number and page size are valid.</returns>
    protected Result CheckPagination(int pageNumber, int pageSize)
    {
        if (pageNumber < 1)
            return Result.Failure("Invalid page number. Must be greater than 0.");

        if (pageSize < 1)
            return Result.Failure("Invalid page size. Must be greater than 0.");

        return Result.Success();
    }

    public override async Task<IResult<IEnumerable<TEntity>>> FilterAsync(
        Expression<Func<TEntity, bool>> expression,
        Func<IQueryable<TEntity>, IQueryable<TEntity>> query
    )
    {
        var entities = await query(_dbSet.Where(expression).Where(e => e.DeletedAt == null))
            .ToListAsync();
        return Result<IEnumerable<TEntity>>.Success(entities);
    }

    public override async Task<IResult<IEnumerable<TEntity>>> FilterAsync(
        Expression<Func<TEntity, bool>> expression,
        Func<IQueryable<TEntity>, IQueryable<TEntity>> query,
        bool showDeleted
    )
    {
        if (!showDeleted)
        {
            return await FilterAsync(expression, query);
        }
        var entities = await query(_dbSet.Where(e => e.DeletedAt == null).Where(expression))
            .ToListAsync();
        return Result<IEnumerable<TEntity>>.Success(entities);
    }
    public override async Task<IResult<bool>> AnyAsync(
        Expression<Func<TEntity, bool>> expression,
        Func<IQueryable<TEntity>, IQueryable<TEntity>> query
    )
    {
        var entities = await query(_dbSet.Where(e => e.DeletedAt == null)).AnyAsync(expression);
        return Result<bool>.Success(entities);
    }

    public override async Task<IResult<bool>> AnyAsync(Expression<Func<TEntity, bool>> expression)
    {
        var entities = await _dbSet.Where(e => e.DeletedAt == null).AnyAsync(expression);
        return Result<bool>.Success(entities);
    }

    public override async Task<IResult<IEnumerable<TEntity>>> UpdateRangeAsync(IEnumerable<TEntity> entities)
    {
        _dbSet.UpdateRange(entities);
        await _unitOfWork.Context.SaveChangesAsync();

        return Result<IEnumerable<TEntity>>.Success(entities);
    }



    public override async Task<IResult<int>> CountAsync(Expression<Func<TEntity, bool>> expression)
    {
        var count = await _dbSet.Where(e => e.DeletedAt == null).CountAsync(expression);
        return Result<int>.Success(count);
    }

    public override async Task<IResult<int>> CountAsync(
        Expression<Func<TEntity, bool>> expression,
        Func<IQueryable<TEntity>, IQueryable<TEntity>> query,
        bool showDeleted
    )
    {
        if (!showDeleted)
        {
            return await CountAsync(expression, query);
        }
        var count = await query(_dbSet.Where(e => e.DeletedAt == null)).CountAsync(expression);
        return Result<int>.Success(count);
    }

    private async Task<IResult<int>> CountAsync(Expression<Func<TEntity, bool>> expression, Func<IQueryable<TEntity>, IQueryable<TEntity>> query)
    {
        var count = await query(_dbSet.Where(e => e.DeletedAt == null)).CountAsync(expression);
        return Result<int>.Success(count);
    }
}