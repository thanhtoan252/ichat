namespace IChat.Api.Extensions;

using IChat.Core.Common;

public static class ProblemResultExtensions
{
    public static IResult ToProblemResult(this Error error) =>
        Results.Problem(
            detail: error.Message,
            statusCode: ToStatusCode(error),
            title: error.Code,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });

    private static int ToStatusCode(Error error)
    {
        if (error.Code == Error.UnsupportedMediaTypeCode)
        {
            return StatusCodes.Status415UnsupportedMediaType;
        }

        return error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,

            ErrorType.External => StatusCodes.Status502BadGateway,
            _ => StatusCodes.Status500InternalServerError
        };
    }
}
