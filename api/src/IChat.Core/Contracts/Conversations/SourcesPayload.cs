namespace IChat.Core.Contracts.Conversations;

public sealed class SourcesPayload
{
    public required IReadOnlyList<SourceView> Sources { get; init; }
}
