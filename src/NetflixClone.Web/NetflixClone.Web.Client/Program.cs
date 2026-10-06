using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using NetflixClone.Web.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

var apiBaseUrl = builder.Configuration["Api:BaseUrl"];
if (!Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var apiUri) || apiUri.Scheme != "https")
    throw new InvalidOperationException("Configure Api:BaseUrl with the HTTPS API origin.");

builder.Services.AddScoped<ProfileAccessState>();
builder.Services.AddScoped(services => new HttpClient(new ProfileAccessHandler(
    services.GetRequiredService<ProfileAccessState>(), services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>(), apiUri)
    { InnerHandler = new HttpClientHandler() }) { BaseAddress = apiUri, Timeout = TimeSpan.FromSeconds(20) });
builder.Services.AddScoped<AuthApiClient>();
builder.Services.AddScoped<ProfilesApiClient>();
builder.Services.AddScoped<ActiveProfileState>();
builder.Services.AddScoped<MyListApiClient>();
builder.Services.AddScoped<RatingsApiClient>();
builder.Services.AddScoped<PersonalizationApiClient>();
builder.Services.AddScoped<WatchHistoryApiClient>();
builder.Services.AddScoped<ViewingSessionsApiClient>();
builder.Services.AddScoped<AdminViewingReportsApiClient>();
builder.Services.AddScoped<AdminDashboardApiClient>();
builder.Services.AddScoped<SubscriptionsApiClient>();
builder.Services.AddScoped<SubscriptionPurchaseDraftStore>();
builder.Services.AddScoped<DevicesApiClient>();
builder.Services.AddScoped<CatalogApiClient>();
builder.Services.AddScoped<AdminMoviesApiClient>();
builder.Services.AddScoped<AdminPlansApiClient>();
builder.Services.AddScoped<AdminGenresApiClient>();
builder.Services.AddScoped<AdminUsersApiClient>();
builder.Services.AddScoped<CollectionsApiClient>();
builder.Services.AddScoped(_ => new AdminMediaUploadTransport(apiUri));
builder.Services.AddScoped<ICredentialStore, BrowserCredentialStore>();
builder.Services.AddScoped<AuthSession>();
builder.Services.AddScoped<AuthenticationStateProvider>(services => services.GetRequiredService<AuthSession>());
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();

await builder.Build().RunAsync();
