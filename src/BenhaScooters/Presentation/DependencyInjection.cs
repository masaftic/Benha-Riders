using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.OpenApi.Models;
using Thinktecture.AspNetCore.ModelBinding;
using Thinktecture.Swashbuckle;
using Thinktecture.Text.Json.Serialization;

namespace BenhaScooters.Presentation;



public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddControllers(options =>
        {
            options.ModelBinderProviders.Insert(0, new ThinktectureModelBinderProvider());
        }).AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            options.JsonSerializerOptions.Converters.Add(new ThinktectureJsonConverterFactory());
        });

        services.Configure<RouteOptions>(options =>
        {
            options.LowercaseUrls = true;
        });

        // Add FluentValidation
        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblyContaining<Program>();

        services.AddRateLimiter(options =>
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


        services.AddSwaggerGen(options =>
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

        services.AddOpenApi();

        services.AddSignalR(o => o.EnableDetailedErrors = true)
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.PayloadSerializerOptions.Converters.Add(new ThinktectureJsonConverterFactory());
            });

        services.AddProblemDetails(c =>
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

        services.AddThinktectureOpenApiFilters();

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            });
        });

        return services;
    }
}
