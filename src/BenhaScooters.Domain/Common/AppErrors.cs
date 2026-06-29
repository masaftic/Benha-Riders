using ErrorOr;

namespace BenhaScooters.Domain.Common;

public static partial class AppErrors
{
    private static Dictionary<string, object> CreateMetadata(params (string Key, object? Value)[] metadata)
    {
        var dictionary = new Dictionary<string, object>(StringComparer.Ordinal);

        foreach (var (key, value) in metadata)
        {
            if (value is not null)
            {
                dictionary[key] = value;
            }
        }

        return dictionary;
    }

    private static AppError NewFailure(string code, string description, params (string Key, object? Value)[] metadata) =>
        metadata.Length == 0
            ? Error.Failure(code, description)
            : Error.Failure(code, description, CreateMetadata(metadata));

    private static AppError NewValidation(string code, string description, params (string Key, object? Value)[] metadata) =>
        metadata.Length == 0
            ? Error.Validation(code, description)
            : Error.Validation(code, description, CreateMetadata(metadata));

    private static AppError NewConflict(string code, string description, params (string Key, object? Value)[] metadata) =>
        metadata.Length == 0
            ? Error.Conflict(code, description)
            : Error.Conflict(code, description, CreateMetadata(metadata));

    private static AppError NewNotFound(string code, string description, params (string Key, object? Value)[] metadata) =>
        metadata.Length == 0
            ? Error.NotFound(code, description)
            : Error.NotFound(code, description, CreateMetadata(metadata));

    private static AppError NewUnauthorized(string code, string description, params (string Key, object? Value)[] metadata) =>
        metadata.Length == 0
            ? Error.Unauthorized(code, description)
            : Error.Unauthorized(code, description, CreateMetadata(metadata));

    private static AppError NewForbidden(string code, string description, params (string Key, object? Value)[] metadata) =>
        metadata.Length == 0
            ? Error.Forbidden(code, description)
            : Error.Forbidden(code, description, CreateMetadata(metadata));

    private static AppError NewUnexpected(string code, string description, params (string Key, object? Value)[] metadata) =>
        metadata.Length == 0
            ? Error.Unexpected(code, description)
            : Error.Unexpected(code, description, CreateMetadata(metadata));

    private static AppError NewCustom(int statusCode, string code, string description, params (string Key, object? Value)[] metadata) =>
        metadata.Length == 0
            ? Error.Custom(statusCode, code, description)
            : Error.Custom(statusCode, code, description, CreateMetadata(metadata));
}
