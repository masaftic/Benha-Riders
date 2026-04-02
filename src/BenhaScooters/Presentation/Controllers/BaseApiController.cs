using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;
using ErrorOr;
using BenhaScooters.Presentation.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace BenhaScooters.Presentation.Controllers;

[ApiController]
public class BaseApiController : ControllerBase
{
    private static readonly Regex PlaceholderRegex = new(@"\{(?<key>[a-zA-Z0-9_]+)\}", RegexOptions.Compiled);

    protected IActionResult HandleErrors(List<Error> errors)
    {
        if (errors.Count == 0)
        {
            return Problem();
        }

        var firstError = errors[0];
        var statusCode = MapToStatusCode(firstError);

        // Store error codes for potential middleware use
        HttpContext.Items["ErrorCodes"] = errors.Select(e => e.Code).ToList();

        var extensions = new Dictionary<string, object?>
        {
            { "code", firstError.Code }
        };

        if (firstError.Metadata is { Count: > 0 } metadata)
        {
            extensions["metadata"] = metadata;
        }

        var localizedDescription = LocalizeError(firstError);

        return Problem(
            statusCode: statusCode,
            title: MapToTitle(firstError.Type),
            detail: localizedDescription,
            instance: HttpContext.Request.Path,
            extensions: extensions
        );
    }

    private string LocalizeError(Error error)
    {
        var localizer = HttpContext.RequestServices.GetRequiredService<IStringLocalizer<ApiErrorResources>>();
        var localizedTemplate = localizer[error.Code];

        if (localizedTemplate.ResourceNotFound)
        {
            return error.Description;
        }

        if (error.Metadata is not { Count: > 0 } metadata)
        {
            return localizedTemplate.Value;
        }

        return PlaceholderRegex.Replace(localizedTemplate.Value, match =>
        {
            var key = match.Groups["key"].Value;

            return metadata.TryGetValue(key, out var value)
                ? FormatMetadataValue(value)
                : match.Value;
        });
    }

    private static string FormatMetadataValue(object value) => value switch
    {
        string text => text,
        IEnumerable values => string.Join(", ", values.Cast<object?>().Select(FormatScalarValue)),
        _ => FormatScalarValue(value)
    };

    private static string FormatScalarValue(object? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        return value switch
        {
            IFormattable formattable => formattable.ToString(null, CultureInfo.CurrentUICulture),
            _ => value.ToString() ?? string.Empty
        };
    }

    private static int MapToStatusCode(Error error)
    {
        var normalTypes = Enum.GetValues<ErrorType>();

        // Error.Custom uses NumericType to carry the HTTP status code
        if (!normalTypes.Contains(error.Type) && error.NumericType is >= 400 and < 600)
        {
            return error.NumericType;
        }

        return error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Unexpected => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError
        };
    }

    private static string MapToTitle(ErrorType errorType) => errorType switch
    {
        ErrorType.Validation => "Validation failed",
        ErrorType.Conflict => "Conflict occurred",
        ErrorType.NotFound => "Resource not found",
        ErrorType.Unauthorized => "Unauthorized access",
        ErrorType.Forbidden => "Forbidden access",
        ErrorType.Unexpected => "An unexpected error occurred",
        _ => "Error"
    };
}
