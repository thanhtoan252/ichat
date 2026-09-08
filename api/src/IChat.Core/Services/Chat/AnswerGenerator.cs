namespace IChat.Core.Services.Chat;

using System.Diagnostics;
using System.Runtime.CompilerServices;
using IChat.Core.Abstractions;
using IChat.Core.Contracts.Conversations;
using IChat.Core.Rag;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

/// <summary>
/// Gọi model và phát ra từng mẩu text. Mọi thứ liên quan tới việc "nói chuyện với provider"
/// nằm ở đây: ChatOptions, timeout, span telemetry, và cách quy mọi kiểu kết thúc về một mối.
/// </summary>
public sealed class AnswerGenerator(
    IChatClient chatClient,
    IModelCatalog modelCatalog,
    ILogger<AnswerGenerator> logger)
{
    /// <summary>
    /// Phát ra từng mẩu text mà provider trả về, và ở cuối là event "error" nếu provider hỏng.
    /// Tách riêng vì C# không cho `yield return` bên trong try/catch: vòng lặp phải tự gọi
    /// MoveNextAsync và bắt lỗi thủ công, và đó là đoạn rối nhất của cả luồng chat.
    /// </summary>
    public async IAsyncEnumerable<SseEvent> StreamAsync(
        IReadOnlyList<ChatMessage> prompt,
        string? requestedModel,
        AnswerBuffer buffer,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var options = BuildChatOptions(requestedModel);

        using var generationActivity = StartGenerationActivity(options);

        // Timeout phải được thực thi ở đây: SDK của từng hãng được factory dựng trực tiếp
        // nên không đi qua HttpClient pipeline của chúng ta, và một provider treo sẽ
        // giữ kết nối SSE mở vô hạn.
        using var generationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        generationCts.CancelAfter(TimeSpan.FromSeconds(modelCatalog.ChatTimeoutSeconds));
        var generationToken = generationCts.Token;

        var stream = chatClient.GetStreamingResponseAsync(prompt, options, generationToken).GetAsyncEnumerator(generationToken);

        try
        {
            while (true)
            {
                var step = await ReadNextUpdateAsync(stream);

                if (step.Update is null)
                {
                    if (step.Error is not null)
                    {
                        buffer.MarkFailed(step.Error);
                    }
                    else if (step.Interrupted)
                    {
                        buffer.MarkInterrupted();
                    }

                    break;
                }

                buffer.AddUsage(step.Update);

                var text = step.Update.Text;

                if (!string.IsNullOrEmpty(text))
                {
                    buffer.AppendText(text);

                    yield return SseEvent.Delta(text);
                }
            }
        }
        finally
        {
            await stream.DisposeAsync();
        }

        if (buffer.Error is not null)
        {
            yield return SseEvent.Failure(buffer.Error);
        }
    }

    private ChatOptions BuildChatOptions(string? requestedModel)
    {
        return new ChatOptions
        {
            ModelId = requestedModel,

            // Không cấu hình thì không gửi: các model dòng reasoning từ chối mọi
            // temperature khác giá trị mặc định của hãng bằng HTTP 400.
            Temperature = (float?)modelCatalog.ChatTemperature,

            // Anthropic bắt buộc phải có max_tokens; luôn set cho mọi provider.
            MaxOutputTokens = modelCatalog.ChatMaxOutputTokens
        };
    }

    /// <summary>Theo OpenTelemetry semantic convention cho GenAI.</summary>
    private Activity? StartGenerationActivity(ChatOptions options)
    {
        var activity = RetrievalPipeline.ActivitySource.StartActivity("rag.generate");

        activity?.SetTag("gen_ai.system", modelCatalog.GetSnapshot().Chat.Provider);
        activity?.SetTag("gen_ai.request.model", options.ModelId);
        activity?.SetTag("gen_ai.request.max_tokens", modelCatalog.ChatMaxOutputTokens);

        return activity;
    }

    /// <summary>
    /// Đọc một update, quy mọi cách kết thúc về cùng một kiểu trả về: hết stream,
    /// client ngắt giữa chừng, hoặc provider hỏng.
    /// </summary>
    private async Task<StreamStep> ReadNextUpdateAsync(IAsyncEnumerator<ChatResponseUpdate> stream)
    {
        try
        {
            return await stream.MoveNextAsync()
                ? new StreamStep { Update = stream.Current }
                : new StreamStep();
        }
        catch (OperationCanceledException)
        {
            // Client đóng tab giữa chừng: vẫn lưu phần đã sinh với ghi chú [interrupted].
            return new StreamStep { Interrupted = true };
        }
        catch (Exception exception)
        {
            // Không để message lỗi gốc của SDK lọt ra client: có thể lộ chi tiết cấu hình.
            logger.LogError(exception, "The provider failed while streaming the answer.");

            return new StreamStep
            {
                Error = new ErrorPayload
                {
                    Code = SseErrorCode.External,
                    Message = "The AI provider failed while generating the answer."
                }
            };
        }
    }

    /// <summary>Một lần đọc stream: Update là null nghĩa là đã kết thúc, vì lý do nào đó.</summary>
    private readonly record struct StreamStep
    {
        public ChatResponseUpdate? Update { get; init; }

        public bool Interrupted { get; init; }

        public ErrorPayload? Error { get; init; }
    }
}
