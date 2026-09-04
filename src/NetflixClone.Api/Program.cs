using NetflixClone.Application.Authentication.Register;
using NetflixClone.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
// builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("NetflixCloneDb")
    ?? throw new InvalidOperationException("Connection string 'NetflixCloneDb' not found.");

builder.Services.AddInfrastructure(connectionString);

builder.Services.AddScoped<IRegisterAccountUseCase, RegisterAccountUseCase>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();

