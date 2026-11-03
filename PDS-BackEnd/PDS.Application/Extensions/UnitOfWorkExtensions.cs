using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PDS.Application.Data;

namespace PDS.Application.Extensions;

public static class UnitOfWorkExtensions
{
    public static IServiceCollection AddUnitOfWork<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext
    {
        services.AddScoped<IUnitOfWork<TDbContext>, UnitOfWork<TDbContext>>();
        return services;
    }
}