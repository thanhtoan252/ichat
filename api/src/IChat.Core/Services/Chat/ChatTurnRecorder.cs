namespace IChat.Core.Services.Chat;

using IChat.Core.Abstractions;
using IChat.Core.Contracts.Conversations;
using IChat.Core.Domain.Conversations;
using IChat.Core.Rag;
using Microsoft.Extensions.Logging;

public sealed class ChatTurnRecorder(
    IApplicationDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<ChatTurnRecorder> logger)
{

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

        foreach (var citation in extracted)
        {
            var source = assembled.Sources[citation.SourceOrdinal];

            var chunkId = source.CitationChunkId;

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

        return citations;
    }
}
