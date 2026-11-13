namespace IChat.Core.Services.Chat;

using System.Diagnostics;
using System.Runtime.CompilerServices;
using IChat.Core.Abstractions;
using IChat.Core.Contracts.Conversations;
using IChat.Core.Rag;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

public sealed class AnswerGenerator(
    IChatClient chatClient,
    IModelCatalog modelCatalog,
    ILogger<AnswerGenerator> logger)
{

    public async IAsyncEnumerable<SseEvent> StreamAsync(
            IReadOnlyList<ChatMessage> prompt,
            string? requestedModel,
            AnswerBuffer buffer,
            [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var options = BuildChatOptions(requestedModel);

        using var generationActivity = StartGenerationActivity(options);

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

            Temperature = (float?)modelCatalog.ChatTemperature,

            MaxOutputTokens = modelCatalog.ChatMaxOutputTokens
        };
    }

    private Activity? StartGenerationActivity(ChatOptions options)
    {
        var activity = RetrievalPipeline.ActivitySource.StartActivity("rag.generate");

        activity?.SetTag("gen_ai.system", modelCatalog.GetSnapshot().Chat.Provider);
        activity?.SetTag("gen_ai.request.model", options.ModelId);
        activity?.SetTag("gen_ai.request.max_tokens", modelCatalog.ChatMaxOutputTokens);

        return activity;
    }

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

            return new StreamStep { Interrupted = true };
        }
        catch (Exception exception)
        {

            logger.LogError(exception, "OpenAI failed while streaming the answer.");

            return new StreamStep
            {
                Error = new ErrorPayload
                {
                    Code = SseErrorCode.External,
                    Message = "OpenAI failed while generating the answer."
                }
            };
        }
    }

    private readonly record struct StreamStep
    {
        public ChatResponseUpdate? Update { get; init; }

        public bool Interrupted { get; init; }

        public ErrorPayload? Error { get; init; }
    }
}
