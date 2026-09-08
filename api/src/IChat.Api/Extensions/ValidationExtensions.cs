namespace IChat.Api.Extensions;

using IChat.Core.Common;
using FluentValidation.Results;

public static class ValidationExtensions
{
    /// <summary>
    /// Giữ nguyên extension "code": "Validation" như lỗi nghiệp vụ để client chỉ phải
    /// phân nhánh trên một trường duy nhất, dù thân response là ValidationProblemDetails.
    /// </summary>
    public static IResult ToValidationProblem(this ValidationResult validationResult)
    {
        var errors = validationResult.Errors
            .GroupBy(failure => string.IsNullOrWhiteSpace(failure.PropertyName) ? "request" : ToCamelCase(failure.PropertyName))
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).ToArray());

        return Results.ValidationProblem(
            errors,
            statusCode: StatusCodes.Status400BadRequest,
            title: Error.ValidationCode,
            detail: string.Join(" ", validationResult.Errors.Select(failure => failure.ErrorMessage)),
            extensions: new Dictionary<string, object?> { ["code"] = Error.ValidationCode });
    }

    private static string ToCamelCase(string propertyName) =>
        char.IsUpper(propertyName[0])
            ? string.Concat(char.ToLowerInvariant(propertyName[0]), propertyName[1..])
            : propertyName;
}
