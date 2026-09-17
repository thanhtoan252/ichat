namespace IChat.Core.Services.Chat;

using IChat.Core.Abstractions;
using IChat.Core.Domain.Conversations;
using IChat.Core.Rag;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

/// <summary>
/// Builds the whole context of one chat turn: history, rewritten question, retrieval, context.
/// It is split in two steps because the SSE flow has to emit status("retrieving") exactly between them,
/// and an iterator cannot yield from inside an await.
/// </summary>
public sealed class ChatTurnContextBuilder(
    IApplicationDbContext dbContext,
    IQueryRewriter queryRewriter,
    RetrievalPipeline pipeline,
    ContextAssembler contextAssembler,
    IOptions<RagOptions> ragOptions)
{
    private readonly RagOptions _rag = ragOptions.Value;

    /// <summary>Loads the history, then rewrites the question against that history.</summary>
    public async Task<ChatTurnQuery> PrepareQueryAsync(Guid conversationId, string question, CancellationToken cancellationToken)
    {
        var history = await LoadHistoryAsync(conversationId, cancellationToken);
        var rewrittenQuery = await RewriteQueryAsync(question, history, cancellationToken);

        return new ChatTurnQuery { History = history, RewrittenQuery = rewrittenQuery };
    }

    /// <summary>Runs retrieval on the rewritten question, then assembles the context within the token budget.</summary>
    public async Task<ChatTurnContext> BuildAsync(ChatTurnQuery query, CancellationToken cancellationToken)
    {
        var retrieval = await RetrieveAsync(query, cancellationToken);
        var assembled = contextAssembler.Assemble(retrieval.Contexts, _rag.Context.MaxTokens);

        return new ChatTurnContext
        {
            History = query.History,
            RewrittenQuery = query.RewrittenQuery,
            Retrieval = retrieval,
            Assembled = assembled
        };
    }

    private async Task<string> RewriteQueryAsync(string originalQuestion, IReadOnlyList<ChatMessage> history, CancellationToken cancellationToken)
    {
        if (!_rag.QueryRewriting.Enabled)
        {
            return originalQuestion;
        }

        return await queryRewriter.RewriteAsync(originalQuestion, history, cancellationToken);
    }

    private Task<PipelineOutcome> RetrieveAsync(ChatTurnQuery query, CancellationToken cancellationToken)
    {
        // Retrieval runs on the rewrite; Rewrite=false because it was already rewritten above.
        return pipeline.ExecuteAsync(
            new RetrievalRequest
            {
                Query = query.RewrittenQuery,
                Mode = SearchMode.Hybrid,
                Rewrite = false,
                History = query.History
            },
            cancellationToken);
    }

    private async Task<List<ChatMessage>> LoadHistoryAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        var messageCount = ChatHistoryWindow.MessageCountFor(_rag.Context.HistoryTurns);

        var recent = await dbContext.Messages
            .AsNoTracking()
            .Where(message => message.ConversationId == conversationId)
            .OrderByDescending(message => message.CreatedAt)
            .Take(messageCount)
            .Select(message => new { message.Role, message.Content })
            .ToListAsync(cancellationToken);

        recent.Reverse();

        return recent
            .Select(message => new ChatMessage(
                message.Role == MessageRole.Assistant ? ChatRole.Assistant : ChatRole.User,
                message.Content))
            .ToList();
    }
}
