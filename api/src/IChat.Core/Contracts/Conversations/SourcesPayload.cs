namespace IChat.Core.Contracts.Conversations;

/// <summary>Everything that went into the context, so the UI can show "consulting N sources".</summary>
public sealed class SourcesPayload
{
    public required IReadOnlyList<SourceView> Sources { get; init; }
}
