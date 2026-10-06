using NetflixClone.Application.Authentication.EmailConfirmation;
using NetflixClone.Application.Authentication.EmailConfirmation.Resend;
using NetflixClone.Application.Authentication.Register;
using NetflixClone.Application.Authentication.Login;
using NetflixClone.Application.Authentication.Refresh;
using NetflixClone.Application.Authentication.Logout;
using NetflixClone.Application.Authentication.Devices;
using NetflixClone.Application.Catalog.Movies;
using NetflixClone.Application.Catalog.Genres;
using NetflixClone.Application.Profiles;
using NetflixClone.Application.MyList;
using NetflixClone.Application.Ratings;
using NetflixClone.Application.Personalization;
using NetflixClone.Application.Viewing;
using NetflixClone.Application.Admin.Reports;
using NetflixClone.Application.Subscriptions;
using NetflixClone.Application.Admin.Movies;
using NetflixClone.Application.Admin.Media;
using NetflixClone.Application.Common.Abstractions.Media;
using NetflixClone.Infrastructure.Media;
using NetflixClone.Infrastructure;
using NetflixClone.Infrastructure.Security;
using NetflixClone.Api.OpenApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using NetflixClone.Application.Common.Abstractions.Security;
using NetflixClone.Api.Security;
using NetflixClone.Api.Media;

var builder = WebApplication.CreateBuilder(args);
// Hosting request-start logs include query strings. Media tickets must not enter logs.
builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Warning);
builder.Services.AddDataProtection().SetApplicationName("NetflixClone.Playback");
builder.Services.AddSingleton<IPlaybackTicketService, PlaybackTicketService>();
builder.Services.AddSingleton<PrivateDemoMedia>();
builder.Services.AddSingleton<CatalogImageMedia>();
builder.Services.AddSingleton<IAdminMediaStorage>(services => new LocalAdminMediaStorage(
    services.GetRequiredService<CatalogImageMedia>().RootPath,
    services.GetRequiredService<PrivateDemoMedia>().RootPath));
builder.Services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, PlaybackTicketHandler>(
    PlaybackTicketHandler.SchemeName, _ => { });

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
// builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("NetflixCloneDb")
    ?? throw new InvalidOperationException("Connection string 'NetflixCloneDb' not found.");

builder.Services.AddInfrastructure(connectionString);

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt configuration is required.");
jwtOptions.Validate();
builder.Services.AddSingleton(jwtOptions);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(jwtOptions.GetSigningKeyBytes()),
            ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
            RequireSignedTokens = true,
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "sub",
            RoleClaimType = "role"
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                if (!int.TryParse(context.Principal?.FindFirst("sub")?.Value, out var accountId) || accountId <= 0)
                {
                    context.Fail("The token subject is invalid.");
                }
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

var webOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy("WebClient", policy =>
{
    if (webOrigins.Length > 0)
        policy.WithOrigins(webOrigins).WithMethods("GET", "HEAD", "POST", "PUT", "DELETE").WithHeaders("Content-Type", "Authorization", "Range");
}));

builder.Services.AddScoped<IRegisterAccountUseCase, RegisterAccountUseCase>();
builder.Services.AddScoped<ILoginUseCase, LoginUseCase>();
builder.Services.AddScoped<NetflixClone.Application.Authentication.Passwords.IPasswordUseCase, NetflixClone.Application.Authentication.Passwords.PasswordUseCase>();
builder.Services.AddScoped<IRefreshTokenUseCase, RefreshTokenUseCase>();
builder.Services.AddScoped<ILogoutUseCase, LogoutUseCase>();
builder.Services.AddScoped<IBrowseMoviesUseCase, BrowseMoviesUseCase>();
builder.Services.AddScoped<IGetMovieDetailUseCase, GetMovieDetailUseCase>();
builder.Services.AddScoped<IGetMoviePlaybackUseCase, GetMoviePlaybackUseCase>();
builder.Services.AddScoped<IWatchProgressUseCase, WatchProgressUseCase>();
builder.Services.AddScoped<IViewingSessionUseCase, ViewingSessionUseCase>();
builder.Services.AddScoped<IAdminViewingReportUseCase, AdminViewingReportUseCase>();
builder.Services.AddScoped<NetflixClone.Application.Admin.Dashboard.IAdminDashboardUseCase, NetflixClone.Application.Admin.Dashboard.AdminDashboardUseCase>();
builder.Services.AddScoped<ISubscriptionUseCase, SubscriptionUseCase>();
builder.Services.AddScoped<IAdminMovieManagementUseCase, AdminMovieManagementUseCase>();
builder.Services.AddScoped<NetflixClone.Application.Admin.Plans.IAdminPlanUseCase, NetflixClone.Application.Admin.Plans.AdminPlanUseCase>();
builder.Services.AddScoped<NetflixClone.Application.Admin.Genres.IAdminGenreUseCase, NetflixClone.Application.Admin.Genres.AdminGenreUseCase>();
builder.Services.AddScoped<NetflixClone.Application.Admin.Users.IAdminUserUseCase, NetflixClone.Application.Admin.Users.AdminUserUseCase>();
builder.Services.AddScoped<NetflixClone.Application.Collections.IMovieCollectionUseCase, NetflixClone.Application.Collections.MovieCollectionUseCase>();
builder.Services.AddScoped<IAdminMediaUploadUseCase, AdminMediaUploadUseCase>();
builder.Services.AddScoped<IListGenresUseCase, ListGenresUseCase>();
builder.Services.AddScoped<IListDevicesUseCase, ListDevicesUseCase>();
builder.Services.AddScoped<IRevokeDeviceUseCase, RevokeDeviceUseCase>();
builder.Services.AddScoped<IRevokeAllDevicesUseCase, RevokeAllDevicesUseCase>();
builder.Services.AddScoped<IListProfilesUseCase, ListProfilesUseCase>();
builder.Services.AddScoped<IListMyListUseCase, ListMyListUseCase>();
builder.Services.AddScoped<IGetRatingUseCase, GetRatingUseCase>();
builder.Services.AddScoped<IGetOnboardingUseCase, GetOnboardingUseCase>();
builder.Services.AddScoped<IGetOnboardingMoviesUseCase, GetOnboardingMoviesUseCase>();
builder.Services.AddScoped<ICompleteOnboardingUseCase, CompleteOnboardingUseCase>();
builder.Services.AddScoped<IGetRecommendationsUseCase, GetRecommendationsUseCase>();
builder.Services.AddScoped<ISetRatingUseCase, SetRatingUseCase>();
builder.Services.AddScoped<IRemoveRatingUseCase, RemoveRatingUseCase>();
builder.Services.AddScoped<IGetMyListStatusUseCase, GetMyListStatusUseCase>();
builder.Services.AddScoped<IAddToMyListUseCase, AddToMyListUseCase>();
builder.Services.AddScoped<IRemoveFromMyListUseCase, RemoveFromMyListUseCase>();
builder.Services.AddScoped<ICreateProfileUseCase, CreateProfileUseCase>();
builder.Services.AddScoped<IUpdateProfileUseCase, UpdateProfileUseCase>();
builder.Services.AddScoped<IDeleteProfileUseCase, DeleteProfileUseCase>();
builder.Services.AddScoped<IConfirmEmailUseCase, ConfirmEmailUseCase>();
builder.Services.AddScoped<IResendEmailConfirmationUseCase, ResendEmailConfirmationUseCase>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Paste the access token returned by Login."
    });
    options.OperationFilter<AuthorizeOperationFilter>();
});

var app = builder.Build();
// Fail early if the configured media directory is a public web root.
_ = app.Services.GetRequiredService<PrivateDemoMedia>();
_ = app.Services.GetRequiredService<CatalogImageMedia>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("WebClient");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

