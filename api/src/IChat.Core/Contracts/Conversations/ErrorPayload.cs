namespace IChat.Core.Contracts.Conversations;

public sealed class ErrorPayload
{
    public required string Code { get; init; }

    public required string Message { get; init; }
}
