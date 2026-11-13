namespace IChat.Core.Common;

public sealed class Error(string code, string message, ErrorType type = ErrorType.Unexpected)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.None);

    public const string ValidationCode = "Validation";

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

    public static Error UnsupportedMediaType(string message) =>
            new(UnsupportedMediaTypeCode, message, ErrorType.Validation);

    public static Error External(string code, string message) =>
            new(code, message, ErrorType.External);
}
