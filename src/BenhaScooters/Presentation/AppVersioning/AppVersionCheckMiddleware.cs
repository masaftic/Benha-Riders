using BenhaScooters.Presentation.Localization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace BenhaScooters.Presentation.AppVersioning;

public class AppVersionCheckMiddleware(
    RequestDelegate next,
    IOptions<AppVersionSettings> settings,
    IProblemDetailsService problemDetailsService,
    IStringLocalizer<ApiErrorResources> localizer)
{
    private readonly AppVersionSettings _settings = settings.Value;
    private readonly RequestDelegate _next = next;
    private readonly IProblemDetailsService _problemDetailsService = problemDetailsService;
    private readonly IStringLocalizer<ApiErrorResources> _localizer = localizer;

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<BypassVersionCheckAttribute>() != null)
        {
            await _next(context);
            return;
        }

        if (_settings.Driver.Ignore &&
            !context.Request.Headers.ContainsKey("X-APP-TYPE"))
        {
            await _next(context);
            return;
        }

        // Extract the X-APP-TYPE header
        if (!context.Request.Headers.TryGetValue("X-APP-TYPE", out var clientAppTypeHeader))
        {
            context.Response.StatusCode = StatusCodes.Status426UpgradeRequired;
            await context.Response.WriteAsync($"Upgrade Required. X-APP-TYPE header is missing.");
            return;
        }

        if (clientAppTypeHeader != "RiderApp" && clientAppTypeHeader != "DriverApp")
        {
            context.Response.StatusCode = StatusCodes.Status426UpgradeRequired;
            await context.Response.WriteAsync($"Upgrade Required. X-APP-TYPE header is missing.");
            return;
        }

        var clientAppVersion = clientAppTypeHeader == "DriverApp" ? _settings.Driver : _settings.Rider;

        if (clientAppVersion.Ignore)
        {
            await _next(context);
            return;
        }

        // Extract the X-APP-VERSION header
        if (!context.Request.Headers.TryGetValue("X-APP-VERSION", out var clientVersionHeader))
        {
            context.Response.StatusCode = StatusCodes.Status426UpgradeRequired;
            await context.Response.WriteAsync($"Upgrade Required. X-APP-VERSION header is missing. Minimum version is {clientAppVersion.MinimumAppVersion}.");
            return;
        }

        // Parse the client's version
        if (!AppVersion.TryParse(clientVersionHeader!, out var clientVersion))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Invalid X-APP-VERSION format. Expected format: x.x.x+x");
            return;
        }

        // Perform the comparison
        if (clientVersion! < clientAppVersion.MinimumAppVersion)
        {
            context.Response.StatusCode = StatusCodes.Status426UpgradeRequired;
            context.Response.Headers.Append("Upgrade", $"AppVersion {clientAppVersion.MinimumAppVersion}"); // RFC 9110 standard compliance

            var problemDetails = new ProblemDetails
            {
                Extensions = new Dictionary<string, object?>
                {
                    ["code"] = "APP_UPDATE_REQUIRED"
                },
                Detail = _localizer["APP_UPDATE_REQUIRED"],
                Title = "App Update Required",
            };

            await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                ProblemDetails = problemDetails,
                HttpContext = context,
            });

            return;
        }

        await _next(context);
    }
}



public static class AppVersionCheckMiddlewareExtensions
{
    public static IApplicationBuilder UseAppVersionCheckMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseWhen(
            context => !context.Request.Path.StartsWithSegments("/swagger"),
            appBuilder =>
            {
                appBuilder.UseMiddleware<AppVersionCheckMiddleware>();
            }
        );
    }
}
