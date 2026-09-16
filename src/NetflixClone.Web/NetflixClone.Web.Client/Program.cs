using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using NetflixClone.Web.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

var apiBaseUrl = builder.Configuration["Api:BaseUrl"];
if (!Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var apiUri) || apiUri.Scheme != "https")
    throw new InvalidOperationException("Configure Api:BaseUrl with the HTTPS API origin.");

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = apiUri, Timeout = TimeSpan.FromSeconds(20) });
builder.Services.AddScoped<AuthApiClient>();
builder.Services.AddScoped<ICredentialStore, BrowserCredentialStore>();
builder.Services.AddScoped<AuthSession>();
builder.Services.AddScoped<AuthenticationStateProvider>(services => services.GetRequiredService<AuthSession>());
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();

await builder.Build().RunAsync();
