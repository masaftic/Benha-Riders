using ErrorOr;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BenhaScooters.Presentation.Endpoints;

public static class ApiProblem
{
    public static IResult HandleProblems(List<Error> errors, HttpContext? ctx = null)
    {
        if (errors.Count == 0)
        {
            return Results.Problem();
        }

        if (ctx is not null)
        {
            ctx.Items["ErrorCodes"] = errors.Select(e => e.Code).ToList();
        }

        if (errors.All(e => e.Type == ErrorType.Validation))
        {
            bool areBusinessErrors = errors.All(e => IsErrorCode(e.Code));

            if (areBusinessErrors)
            {
                var businessError = errors[0];

                return Results.Problem(
                    title: businessError.Description,
                    detail: businessError.Metadata?.GetValueOrDefault("Detail")?.ToString(),
                    statusCode: 400);
            }
            else
            {
                var dictionary = errors
                    .GroupBy(e => ToCamelCase(e.Code))
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.Description).ToArray());

                return Results.ValidationProblem(
                    dictionary,
                    statusCode: 400,
                    title: "One or more validation errors occurred.");
            }
        }

        var firstError = errors[0];

        return Results.Problem(
            title: firstError.Description,
            detail: firstError.Metadata?.GetValueOrDefault("Detail")?.ToString(),
            statusCode: MapToStatusCode(firstError));
    }

    private static int MapToStatusCode(Error error)
    {
        var errorType = error.Type;

        return errorType switch
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

    private static string ToCamelCase(string str)
    {
        if (string.IsNullOrEmpty(str) || char.IsLower(str[0]))
        {
            return str;
        }

        return char.ToLowerInvariant(str[0]) + str[1..];
    }

    private static bool IsErrorCode(string code)
    {
        return code.Contains('_') && !code.Contains(' ') && code.All(c => char.IsUpper(c) || c == '_');
    }
}
