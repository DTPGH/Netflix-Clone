using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Common.Abstractions.Security;
using NetflixClone.Infrastructure.Persistence;
using NetflixClone.Infrastructure.Persistence.Repositories;
using NetflixClone.Infrastructure.Security;

namespace NetflixClone.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<NetflixCloneDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IUnitOfWork>(ServiceProvider =>
            ServiceProvider.GetRequiredService<NetflixCloneDbContext>());

        services.AddScoped<IUserAccountRepository, UserAccountRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();

        return services;
    }
}