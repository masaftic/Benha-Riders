using System.Text.Json.Serialization;
using BenhaScooters.Data;
using BenhaScooters.Features.Authentication.Services;
using FastEndpoints;
using FastEndpoints.Security;
using FastEndpoints.Swagger;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<DataSeeder>();
builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<ISmsService, DevSmsService>();

builder.Services
    .AddFastEndpoints()
    .SwaggerDocument();

builder.Services
    .AddAuthenticationJwtBearer(s =>
        s.SigningKey = builder.Configuration["Jwt:SigningKey"]
        ?? throw new InvalidOperationException("JWT Signing Key is not configured."))
    .AddAuthentication();

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    if (app.Environment.EnvironmentName != "Testing")
    {
        // Apply migrations and seed data only in non-testing environments
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        var dataSeeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
        await dataSeeder.SeedAsync();
    }
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseDefaultExceptionHandler().UseFastEndpoints(c =>
{
    c.Endpoints.RoutePrefix = "api";
    c.Errors.UseProblemDetails(x =>
    {
        x.IndicateErrorCode = true; 
    });
    c.Serializer.Options.Converters.Add(new JsonStringEnumConverter());
}).UseSwaggerGen();


app.Run();

// Make the implicit Program class public for integration tests
public partial class Program { }
