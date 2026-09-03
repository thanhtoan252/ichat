namespace IChat.Core.Services.Chat;

using IChat.Core.Abstractions;
using IChat.Core.Contracts.Conversations;
using IChat.Core.Domain.Conversations;
using IChat.Core.Rag;
using Microsoft.Extensions.Logging;

/// <summary>
/// Ghi một lượt chat xuống database: câu hỏi trước khi stream, câu trả lời và citation sau
/// khi stream xong.
/// </summary>
public sealed class ChatTurnRecorder(
    IApplicationDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<ChatTurnRecorder> logger)
{
    /// <summary>Lưu TRƯỚC khi stream, kèm rewritten_query và retrieval_ms.</summary>
    public async Task RecordQuestionAsync(
        Conversation conversation,
        string content,
        string rewrittenQuery,
        long retrievalMs,
        CancellationToken cancellationToken)
    {
        dbContext.Messages.Add(Message.CreateUser(
            conversation.Id,
            content,
            rewrittenQuery,
            (int)retrievalMs,
            timeProvider.GetUtcNow()));

        // Cuộc trò chuyện tạo từ nút "New chat" chưa có tên; câu hỏi đầu tiên đặt tên cho nó,
        // nếu không danh sách conversation sẽ toàn "New conversation".
        if (ConversationTitle.IsDefault(conversation.Title))
        {
            conversation.Rename(ConversationTitle.FromQuestion(content), timeProvider.GetUtcNow());
        }
        else
        {
            conversation.Touch(timeProvider.GetUtcNow());
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<DonePayload> RecordAnswerAsync(
        Conversation conversation,
        GeneratedAnswer answer,
        AssembledContext assembled,
        GenerationMetadata metadata,
        CancellationToken cancellationToken)
    {
        var assistantMessage = Message.CreateAssistant(
            conversation.Id,
            answer.Text,
            metadata.Provider,
            metadata.Model,
            answer.InputTokens,
            answer.OutputTokens,
            (int)metadata.LatencyMs,
            (int)metadata.RetrievalMs,
            timeProvider.GetUtcNow());

        dbContext.Messages.Add(assistantMessage);

        var citations = RecordCitations(assistantMessage, answer.Text, assembled);

        conversation.Touch(timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);

        return new DonePayload
        {
            MessageId = assistantMessage.Id,
            Citations = citations,
            Provider = metadata.Provider,
            Model = metadata.Model,
            InputTokens = answer.InputTokens,
            OutputTokens = answer.OutputTokens,
            LatencyMs = metadata.LatencyMs,
            RetrievalMs = metadata.RetrievalMs,
            Degraded = metadata.Degraded,
            Interrupted = answer.Interrupted
        };
    }

    /// <summary>
    /// Không được tin marker model sinh ra: chỉ ghi citation cho marker nằm đúng
    /// phạm vi context. Bước này vẫn chạy trên phần text đã có kể cả khi bị ngắt.
    /// </summary>
    private List<CitationPayload> RecordCitations(Message assistantMessage, string answerText, AssembledContext assembled)
    {
        var extracted = CitationExtractor.Extract(answerText, assembled.Sources.Count, out var invalidMarkers);

        if (invalidMarkers.Count > 0)
        {
            logger.LogWarning(
                "The answer for message {MessageId} contains out-of-range markers: {Markers}. This signals a problem with the prompt.",
                assistantMessage.Id,
                string.Join(", ", invalidMarkers));
        }

        var citations = new List<CitationPayload>();
        var seenChunks = new HashSet<Guid>();

        foreach (var citation in extracted)
        {
            var source = assembled.Sources[citation.SourceOrdinal];

            // Một khối context có thể gộp nhiều chunk gốc; citation trỏ về chunk gốc,
            // chunk lân cận chỉ làm giàu context chứ không trở thành citation độc lập.
            foreach (var chunkId in source.AnchorChunkIds)
            {
                if (!seenChunks.Add(chunkId))
                {
                    continue;
                }

                dbContext.MessageCitations.Add(
                    MessageCitation.Create(assistantMessage.Id, chunkId, citation.MarkerIndex, source.Score));

                citations.Add(new CitationPayload
                {
                    MarkerIndex = citation.MarkerIndex,
                    ChunkId = chunkId,
                    DocumentId = source.DocumentId,
                    DocumentTitle = source.DocumentTitle,
                    HeadingPath = source.HeadingPath,
                    Score = source.Score
                });
            }
        }

        return citations;
    }
}
