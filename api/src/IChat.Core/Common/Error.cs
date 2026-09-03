namespace IChat.Core.Common;

/// <summary>
/// Code là mã máy đọc được và cũng là title của ProblemDetails; Message dành cho người đọc.
/// Client dựa vào Code để phân nhánh nên đừng đổi chuỗi đã công bố.
/// </summary>
public sealed class Error(string code, string message, ErrorType type = ErrorType.Unexpected)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.None);

    /// <summary>Tầng API lặp lại mã này cho ValidationProblemDetails để client chỉ phân nhánh trên một trường.</summary>
    public const string ValidationCode = "Validation";

    /// <summary>Type vẫn là Validation, nên chỉ MÃ này phân biệt được 415 với 400.</summary>
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

    /// <summary>ResultExtensions map riêng code này về 415 thay vì 400.</summary>
    public static Error UnsupportedMediaType(string message) =>
        new(UnsupportedMediaTypeCode, message, ErrorType.Validation);

    public static Error External(string code, string message) =>
        new(code, message, ErrorType.External);
}
