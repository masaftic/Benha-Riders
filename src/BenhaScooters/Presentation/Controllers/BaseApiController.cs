using ErrorOr;
using Microsoft.AspNetCore.Mvc;

namespace BenhaScooters.Presentation.Controllers;

[ApiController]
public class BaseApiController : ControllerBase
{
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

        return Problem(
            statusCode: statusCode,
            title: firstError.Type.ToString(),
            detail: firstError.Description,
            instance: HttpContext.Request.Path,
            extensions: extensions
        );
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
}
