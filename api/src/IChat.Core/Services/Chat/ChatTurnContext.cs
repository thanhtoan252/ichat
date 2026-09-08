namespace IChat.Core.Services.Chat;

using IChat.Core.Rag;
using Microsoft.Extensions.AI;

/// <summary>Mọi thứ cần để sinh câu trả lời, đã dựng xong trước khi gọi model.</summary>
public sealed record ChatTurnContext
{
    public required IReadOnlyList<ChatMessage> History { get; init; }

    public required string RewrittenQuery { get; init; }

    public required PipelineOutcome Retrieval { get; init; }

    public required AssembledContext Assembled { get; init; }
}
