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
/// Facade của một lượt chat: chỉ giữ mạch truyện SSE và thứ tự các event, còn việc nặng
/// giao cho ba collaborator (dựng ngữ cảnh, sinh câu trả lời, ghi xuống database).
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
            .FirstOrDefaultAsync(item => item.Id == request.ConversationId, cancellationToken);

        // Hội thoại của người khác trả về đúng thông điệp "không tồn tại" như khi id sai:
        // phân biệt hai trường hợp là để lộ rằng id đó có thật.
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

        // Gửi ngay để UI không đứng im trong lúc chờ round-trip viết lại câu hỏi.
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

        // CancellationToken.None là cố ý: client đóng tab thì token của request đã huỷ,
        // nhưng phần câu trả lời đã sinh vẫn phải được lưu.
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
        // Câu hỏi GỐC mới là thứ đưa vào prompt sinh câu trả lời; bản viết lại chỉ
        // phục vụ retrieval, dùng nhầm sẽ khiến câu trả lời lệch khỏi điều người dùng hỏi.
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
