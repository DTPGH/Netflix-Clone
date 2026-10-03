using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetflixClone.Application.Common.Abstractions.Messaging;
using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Common.Abstractions.Security;
using NetflixClone.Application.Common.Abstractions.Time;
using NetflixClone.Infrastructure.Messaging;
using NetflixClone.Infrastructure.Persistence;
using NetflixClone.Infrastructure.Persistence.Repositories;
using NetflixClone.Infrastructure.Persistence.Queries;
using NetflixClone.Infrastructure.Security;
using NetflixClone.Infrastructure.Time;
using NetflixClone.Application.Common.Abstractions.Reporting;
using NetflixClone.Infrastructure.Reporting;

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
        services.AddScoped<IMovieCatalogQueries, MovieCatalogQueries>();
        services.AddScoped<IAdminMovieRepository, AdminMovieRepository>();
        services.AddScoped<IAdminPlanRepository, AdminPlanRepository>();
        services.AddScoped<IAdminPlanMutationScopeFactory, AdminPlanMutationScopeFactory>();
        services.AddScoped<IAdminGenreRepository, AdminGenreRepository>();
        services.AddScoped<IAdminGenreMutationScopeFactory, AdminGenreMutationScopeFactory>();
        services.AddScoped<IAdminUserRepository, AdminUserRepository>();
        services.AddScoped<IAdminUserMutationScopeFactory, AdminUserMutationScopeFactory>();
        services.AddScoped<IMovieCollectionRepository, MovieCollectionRepository>();
        services.AddScoped<IMovieCollectionQueries, MovieCollectionRepository>();
        services.AddScoped<IGenreCatalogQueries, GenreCatalogQueries>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IDeviceRepository, DeviceRepository>();
        services.AddScoped<IProfileRepository, ProfileRepository>();
        services.AddScoped<IWatchHistoryRepository, WatchHistoryRepository>();
        services.AddScoped<IViewingSessionRepository, ViewingSessionRepository>();
        services.AddScoped<IAdminViewingReportQueries, AdminViewingReportQueries>();
        services.AddScoped<IViewingReportExporter, ViewingReportExcelExporter>();
        services.AddScoped<IViewingSessionScopeFactory, ViewingSessionScopeFactory>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<ISubscriptionPurchaseScopeFactory, SubscriptionPurchaseScopeFactory>();
        services.AddScoped<IMyListRepository, MyListRepository>();
        services.AddScoped<IRatingRepository, RatingRepository>();
        services.AddScoped<IProfilePreferenceRepository, ProfilePreferenceRepository>();
        services.AddScoped<IPersonalizationQueries, PersonalizationQueries>();
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
