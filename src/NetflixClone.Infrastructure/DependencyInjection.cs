using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Infrastructure.Persistence;

namespace NetflixClone.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<NetflixCloneDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IUnitOfWork>(ServiceProvider =>
            ServiceProvider.GetRequiredService<NetflixCloneDbContext>());

        return services;
    }
}