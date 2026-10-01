namespace IChat.Core.Services;

using System.Diagnostics;
using System.Runtime.CompilerServices;
using IChat.Core.Abstractions;
using IChat.Core.Contracts.Conversations;
using IChat.Core.Rag;
using IChat.Core.Services.Chat;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;

/// <summary>
/// The facade of one chat turn: it only holds the SSE narrative and the order of the events, while the
/// heavy lifting goes to three collaborators (build the context, generate the answer, write to the database).
/// </summary>
public sealed class ChatService(
    IApplicationDbContext dbContext,
    ChatTurnContextBuilder contextBuilder,
    AnswerGenerator answerGenerator,
    ChatTurnRecorder turnRecorder,
    IModelCatalog modelCatalog,
    ICurrentUser currentUser,
    IValidator<SendMessageRequest> validator) : IChatService
{
    public async IAsyncEnumerable<SseEvent> StreamAnswerAsync(
        SendMessageRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var validationError = await FindValidationErrorAsync(request, cancellationToken);

        if (validationError is not null)
        {
            yield return SseEvent.Failure(validationError);

            yield break;
        }

        var conversation = await dbContext.Conversations
            .SingleOrDefaultAsync(item => item.Id == request.ConversationId, cancellationToken);

        // Someone else's conversation gets the same "does not exist" message as a wrong id:
        // telling the two apart would reveal that the id is real.
        if (conversation is null || conversation.UserId != currentUser.Id)
        {
            yield return SseEvent.Failure(
                SseErrorCode.NotFound,
                $"Conversation '{request.ConversationId}' does not exist.");

            yield break;
        }

        var modelError = FindDisallowedModelError(request.Model);

        if (modelError is not null)
        {
            yield return SseEvent.Failure(modelError);

            yield break;
        }

        var totalStopwatch = Stopwatch.StartNew();

        // Sent immediately so the UI does not sit still during the rewrite round-trip.
        yield return SseEvent.Status(AnswerStage.Rewriting);

        var query = await contextBuilder.PrepareQueryAsync(request.ConversationId, request.Content, cancellationToken);

        yield return SseEvent.Status(AnswerStage.Retrieving);

        var context = await contextBuilder.BuildAsync(query, cancellationToken);

        await turnRecorder.RecordQuestionAsync(
            conversation,
            request.Content,
            context.RewrittenQuery,
            context.Retrieval.RetrievalMs,
            cancellationToken);

        yield return SseEvent.Sources(BuildSources(context.Assembled));
        yield return SseEvent.Status(AnswerStage.Generating);

        var snapshot = modelCatalog.GetSnapshot();
        var model = request.Model ?? snapshot.Chat.Model;
        var prompt = BuildAnswerPrompt(context, request.Content);
        var buffer = new AnswerBuffer();

        await foreach (var generationEvent in answerGenerator.StreamAsync(prompt, model, buffer, cancellationToken))
        {
            yield return generationEvent;
        }

        var metadata = new GenerationMetadata
        {
            Provider = snapshot.Chat.Provider,
            Model = model,
            LatencyMs = totalStopwatch.ElapsedMilliseconds,
            RetrievalMs = context.Retrieval.RetrievalMs,
            Degraded = context.Retrieval.Degraded
        };

        // CancellationToken.None is deliberate: when the client closes the tab the request's token is already
        // cancelled, but whatever answer was generated still has to be persisted.
        var done = await turnRecorder.RecordAnswerAsync(
            conversation,
            buffer.ToAnswer(),
            context.Assembled,
            metadata,
            CancellationToken.None);

        yield return SseEvent.Done(done);
    }

    private async Task<ErrorPayload?> FindValidationErrorAsync(SendMessageRequest request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);

        if (validation.IsValid)
        {
            return null;
        }

        return new ErrorPayload
        {
            Code = SseErrorCode.Validation,
            Message = string.Join(" ", validation.Errors.Select(failure => failure.ErrorMessage))
        };
    }

    private ErrorPayload? FindDisallowedModelError(string? model)
    {
        if (model is null || modelCatalog.IsChatModelAllowed(model))
        {
            return null;
        }

        return new ErrorPayload
        {
            Code = SseErrorCode.Validation,
            Message = $"Model '{model}' is not in the allowlist."
        };
    }

    private IReadOnlyList<ChatMessage> BuildAnswerPrompt(ChatTurnContext context, string originalUserQuestion)
    {
        // The ORIGINAL question is what goes into the answer prompt; the rewrite only serves retrieval,
        // and using it here would steer the answer away from what the user asked.
        return PromptBuilder.BuildAnswerPrompt(
            context.Assembled,
            ChatHistoryNormalizer.Normalize(context.History),
            originalUserQuestion,
            modelCatalog.SupportsMultipleSystemMessages);
    }

    private static IReadOnlyList<SourceView> BuildSources(AssembledContext assembled)
    {
        return assembled.Sources
            .Select(source => new SourceView
            {
                Index = source.Index,
                ChunkId = source.AnchorChunkIds.Count > 0 ? source.AnchorChunkIds[0] : Guid.Empty,
                DocumentId = source.DocumentId,
                DocumentTitle = source.DocumentTitle,
                HeadingPath = source.HeadingPath,
                Snippet = TextSnippet.From(source.Text),
                Score = source.Score
            })
            .ToList();
    }

}
