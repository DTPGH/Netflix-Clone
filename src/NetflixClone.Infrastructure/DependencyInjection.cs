using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetflixClone.Application.Common.Abstractions.Messaging;
using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Common.Abstractions.Security;
using NetflixClone.Application.Common.Abstractions.Time;
using NetflixClone.Infrastructure.Messaging;
using NetflixClone.Infrastructure.Persistence;
using NetflixClone.Infrastructure.Persistence.Repositories;
using NetflixClone.Infrastructure.Security;
using NetflixClone.Infrastructure.Time;

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
        services.AddScoped<IDeviceRepository, DeviceRepository>();
        services.AddScoped<IProfileRepository, ProfileRepository>();
        services.AddScoped<IProfileCreationScopeFactory, ProfileCreationScopeFactory>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IDeviceIdentifierService, DeviceIdentifierService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<IAccessTokenGenerator, JwtAccessTokenGenerator>();
        services.AddScoped<IEmailConfirmationTokenService, EmailConfirmationTokenService>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IEmailConfirmationSender, DevelopmentEmailConfirmationSender>();

        return services;
    }
}
