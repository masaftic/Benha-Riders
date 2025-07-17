using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using BenhaScooters.Application;
using BenhaScooters.Data;
using BenhaScooters.Infrastructure;
using BenhaScooters.Infrastructure.Authentication;
using BenhaScooters.Infrastructure.Authentication.Services;
using BenhaScooters.Infrastructure.S3;
using BenhaScooters.Infrastructure.Trips.Services;
using BenhaScooters.Presentation;
using BenhaScooters.Services;
using BenhaScooters.Shared;
using ErrorOr;
using FastEndpoints;
using FastEndpoints.Security;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(o =>
{
    o.ListenAnyIP(5000);
});

builder.Services.AddRateLimiter(options =>
{
    options.OnRejected = (context, cancellationToken) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = TimeSpan.FromSeconds(10).TotalSeconds.ToString();
        return ValueTask.CompletedTask;
    };

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 40,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });
});


builder.Services.AddEndpoints();


builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"), o => o.UseNetTopologySuite()));

// Add infrastructure services
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddScoped<DataSeeder>();
builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<ISmsService, DevSmsService>();
builder.Services.AddScoped<IFareEstimator, FareEstimator>();

// Add the token cleanup background service
builder.Services.AddHostedService<TokenCleanupService>();

// builder.Services
//     .AddFastEndpoints()
//     .SwaggerDocument();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "BenhaScooters API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\""
    });
    // Apply the scheme globally to all endpoints marked as requiring auth
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>() // No scopes required
        }
    });

});

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails(c =>
{
    c.CustomizeProblemDetails = ctx =>
    {
        ctx.ProblemDetails.Instance = $"{ctx.HttpContext.Request.Method} {ctx.HttpContext.Request.Path}";
        // trace id
        ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;

        if (ctx.HttpContext.Items.TryGetValue("ErrorCodes", out var errorCodes))
        {
            ctx.ProblemDetails.Extensions["errorCodes"] = errorCodes;
        }
    };
});

builder.Services.AddApplication();


var jwtOptions = new JwtOptions();
builder.Configuration.Bind(JwtOptions.SectionName, jwtOptions);

builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new()
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseRateLimiter();

using (var scope = app.Services.CreateScope())
{
    if (app.Environment.EnvironmentName != "Testing")
    {
        // Apply migrations and seed data only in non-testing environments
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        var dataSeeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
        await dataSeeder.SeedAsync();

        // Initialize MinIO bucket
        var minioInitService = scope.ServiceProvider.GetRequiredService<IMinioInitializationService>();
        await minioInitService.InitializeAsync();
    }
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapEndpoints();

// app.UseDefaultExceptionHandler().UseFastEndpoints(c =>
// {
//     c.Endpoints.RoutePrefix = "api";
//     c.Errors.UseProblemDetails();
//     // c.Endpoints.Configurator = ep =>
//     // {
//     //     if (ep.ResDtoType.IsAssignableTo(typeof(IErrorOr)))
//     //     {
//     //         ep.DontAutoSendResponse();
//     //         ep.PostProcessor<ErrorOrResponseSender>(Order.After);
//     //         ep.Description(
//     //             b => b.ClearDefaultProduces()
//     //                 .Produces(200, ep.ResDtoType.GetGenericArguments()[0])
//     //                 .ProducesProblemDetails());
//     //     }
//     // };
//     c.Serializer.Options.Converters.Add(new JsonStringEnumConverter());
// });

app.MapOpenApi();
app.UseSwagger();
app.UseSwaggerUI();


app.Run();

// Make the implicit Program class public for integration tests
public partial class Program { }
