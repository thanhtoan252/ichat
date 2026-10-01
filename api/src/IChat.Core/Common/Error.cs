namespace IChat.Core.Common;

/// <summary>
/// Code is the machine-readable code and also the ProblemDetails title; Message is for humans.
/// Clients branch on Code, so a published string must not be changed.
/// </summary>
public sealed class Error(string code, string message, ErrorType type = ErrorType.Unexpected)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.None);

    /// <summary>The API layer repeats this code for ValidationProblemDetails so the client branches on a single field.</summary>
    public const string ValidationCode = "Validation";

    /// <summary>Type stays Validation, so only THIS code tells a 415 apart from a 400.</summary>
    public const string UnsupportedMediaTypeCode = "UnsupportedMediaType";

    public string Code { get; } = code;

    public string Message { get; } = message;

    public ErrorType Type { get; } = type;

    public static Error Failure(string code, string message) =>
        new(code, message);

    public static Error Validation(string message) =>
        new(ValidationCode, message, ErrorType.Validation);

    public static Error NotFound(string entity, object id) =>
        new($"{entity}.NotFound", $"No {entity} found with id '{id}'.", ErrorType.NotFound);

    public static Error Conflict(string code, string message) =>
        new(code, message, ErrorType.Conflict);

    public static Error Unauthorized(string message) =>
        new("Unauthorized", message, ErrorType.Unauthorized);

    /// <summary>ResultExtensions maps this code to 415 instead of 400.</summary>
    public static Error UnsupportedMediaType(string message) =>
        new(UnsupportedMediaTypeCode, message, ErrorType.Validation);

    public static Error External(string code, string message) =>
        new(code, message, ErrorType.External);
}
