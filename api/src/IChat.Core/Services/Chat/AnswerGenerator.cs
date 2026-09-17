namespace IChat.Core.Services.Chat;

using System.Diagnostics;
using System.Runtime.CompilerServices;
using IChat.Core.Abstractions;
using IChat.Core.Contracts.Conversations;
using IChat.Core.Rag;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

/// <summary>
/// Calls the model and emits each piece of text. Everything about "talking to a provider" lives here:
/// ChatOptions, timeout, the telemetry span, and how every kind of ending is funnelled into one path.
/// </summary>
public sealed class AnswerGenerator(
    IChatClient chatClient,
    IModelCatalog modelCatalog,
    ILogger<AnswerGenerator> logger)
{
    /// <summary>
    /// Emits each piece of text the provider returns, and at the end an "error" event if the provider failed.
    /// It is split out because C# does not allow `yield return` inside try/catch: the loop has to call
    /// MoveNextAsync and catch errors by hand, and that is the knottiest part of the whole chat flow.
    /// </summary>
    public async IAsyncEnumerable<SseEvent> StreamAsync(
        IReadOnlyList<ChatMessage> prompt,
        string? requestedModel,
        AnswerBuffer buffer,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var options = BuildChatOptions(requestedModel);

        using var generationActivity = StartGenerationActivity(options);

        // The timeout has to be enforced here: each vendor's SDK is constructed directly by a factory, so it
        // never passes through our HttpClient pipeline, and a hung provider would hold the SSE connection
        // open forever.
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

            // Not configured means not sent: the reasoning-family models reject any temperature other than
            // the vendor's default with an HTTP 400.
            Temperature = (float?)modelCatalog.ChatTemperature,

            // Anthropic requires max_tokens; always set it, for every provider.
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
    /// Reads one update and funnels every way of ending into the same return type: the stream ran out,
    /// the client disconnected midway, or the provider failed.
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
            // The client closed the tab midway: what was generated is still persisted, marked [interrupted].
            return new StreamStep { Interrupted = true };
        }
        catch (Exception exception)
        {
            // The SDK's own error message never reaches the client: it can leak configuration details.
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

    /// <summary>One read from the stream: a null Update means it ended, for one reason or another.</summary>
    private readonly record struct StreamStep
    {
        public ChatResponseUpdate? Update { get; init; }

        public bool Interrupted { get; init; }

        public ErrorPayload? Error { get; init; }
    }
}
