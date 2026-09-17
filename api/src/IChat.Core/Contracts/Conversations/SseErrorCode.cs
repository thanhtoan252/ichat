namespace IChat.Core.Contracts.Conversations;

/// <summary>
/// The values of <see cref="ErrorPayload.Code"/>. Deliberately separate from the service layer's
/// <c>Error.Code</c>: an error inside the SSE stream arrives with HTTP 200, so the client branches on
/// its own set of codes — shorter, and carrying no entity names.
/// </summary>
public static class SseErrorCode
{
    public const string Validation = "Validation";

    public const string NotFound = "NotFound";

    public const string External = "External";
}
