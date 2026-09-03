namespace IChat.Api.Middleware;

using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            return true;
        }

        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        logger.LogError(
            exception,
            "Unhandled error while processing {RequestMethod} {RequestPath}. TraceId={TraceId}",
            httpContext.Request.Method,
            httpContext.Request.Path.Value,
            traceId);

        // Không bao giờ để message gốc của SDK lọt ra client: có thể lộ chi tiết cấu hình.
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Unexpected",
            Detail = "An unexpected error occurred.",
            Extensions = { ["traceId"] = traceId }
        };

        httpContext.Response.StatusCode = problemDetails.Status.Value;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
