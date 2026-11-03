using PDS.Domain.Response;
using System.Linq.Expressions;

namespace PDS.Application.Repositories;

public interface IRepository<TEntity>
    where TEntity : class
{
    public Task<IResult<bool>> AnyAsync(
        Expression<Func<TEntity, bool>> expression,
        Func<IQueryable<TEntity>, IQueryable<TEntity>> query
    );
    public Task<IResult<bool>> AnyAsync(Expression<Func<TEntity, bool>> expression);
    public Task<IResult<IEnumerable<TEntity>>> GetAllAsync();
    public Task<IResult<IEnumerable<TEntity>>> GetPaginatedAsync(
        int pageNumber = 1,
        int pageSize = 50
    );
    public Task<IResult<IEnumerable<TEntity>>> FilterAsync(
        Expression<Func<TEntity, bool>> expression,
        Func<IQueryable<TEntity>, IQueryable<TEntity>> query
    );
    public Task<IResult<IEnumerable<TEntity>>> FilterAsync(
        Expression<Func<TEntity, bool>> expression
    );
    public Task<IResult<TEntity?>> GetAsync(Expression<Func<TEntity, bool>> expression);
    public Task<IResult<TEntity?>> GetWithDeletedAsync(
        Expression<Func<TEntity, bool>> expression,
        Func<IQueryable<TEntity>, IQueryable<TEntity>> query);
    public Task<IResult<TEntity?>> GetAsync(
        Expression<Func<TEntity, bool>> expression,
        Func<IQueryable<TEntity>, IQueryable<TEntity>> query
    );
    public Task<IResult<TEntity>> CreateAsync(TEntity entity);
    public Task<IResult<IEnumerable<TEntity>>> CreateRangeAsync(IEnumerable<TEntity> entities);
    public Task<IResult<TEntity>> UpdateAsync(TEntity entity);
    public Task<IResult<IEnumerable<TEntity>>> UpdateRangeAsync(IEnumerable<TEntity> entities);
    public Task<IResult<TEntity>> PartialUpdateAsync(
        Expression<Func<TEntity, bool>> expression,
        TEntity entity
    );
    public Task<IResult> DeleteAsync(Expression<Func<TEntity, bool>> expression);
    public Task<IResult> DeleteAsync(TEntity entity);
    public Task<IResult> DeleteRangeAsync(Expression<Func<TEntity, bool>> expression);
    public Task<IResult> DeleteRangeAsync(Expression<Func<TEntity, bool>> expression, Func<IQueryable<TEntity>, IQueryable<TEntity>> query);

    public Task<IResult> DeleteRangeAsync(IEnumerable<TEntity> entities);
    public Task<IResult<int>> CountAsync(Expression<Func<TEntity, bool>> expression);
    Task<IResult<IEnumerable<TEntity>>> FilterAsync(Expression<Func<TEntity, bool>> expression, Func<IQueryable<TEntity>, IQueryable<TEntity>> query, bool showDeleted);
    Task<IResult<int>> CountAsync(Expression<Func<TEntity, bool>> expression, Func<IQueryable<TEntity>, IQueryable<TEntity>> query, bool showDeleted);
}
