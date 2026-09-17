using NetflixClone.Application.Authentication.EmailConfirmation;
using NetflixClone.Application.Authentication.EmailConfirmation.Resend;
using NetflixClone.Application.Authentication.Register;
using NetflixClone.Application.Authentication.Login;
using NetflixClone.Application.Authentication.Refresh;
using NetflixClone.Application.Authentication.Logout;
using NetflixClone.Application.Authentication.Devices;
using NetflixClone.Infrastructure;
using NetflixClone.Infrastructure.Security;
using NetflixClone.Api.OpenApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

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
        policy.WithOrigins(webOrigins).WithMethods("GET", "POST").WithHeaders("Content-Type", "Authorization");
}));

builder.Services.AddScoped<IRegisterAccountUseCase, RegisterAccountUseCase>();
builder.Services.AddScoped<ILoginUseCase, LoginUseCase>();
builder.Services.AddScoped<IRefreshTokenUseCase, RefreshTokenUseCase>();
builder.Services.AddScoped<ILogoutUseCase, LogoutUseCase>();
builder.Services.AddScoped<IListDevicesUseCase, ListDevicesUseCase>();
builder.Services.AddScoped<IRevokeDeviceUseCase, RevokeDeviceUseCase>();
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

