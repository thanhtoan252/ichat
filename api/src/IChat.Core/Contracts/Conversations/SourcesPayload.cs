namespace IChat.Core.Contracts.Conversations;

/// <summary>Toàn bộ những gì đã đưa vào context, để UI hiện "đang tham khảo N nguồn".</summary>
public sealed class SourcesPayload
{
    public required IReadOnlyList<SourceView> Sources { get; init; }
}
