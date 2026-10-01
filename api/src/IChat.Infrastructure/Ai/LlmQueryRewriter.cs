namespace IChat.Infrastructure.Ai;

using IChat.Core.Abstractions;
using IChat.Core.Rag;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public sealed class LlmQueryRewriter(
    [FromKeyedServices(AiServiceKeys.UtilityChat)] IChatClient utilityChatClient,
    IOptions<AiOptions> aiOptions,
    IOptions<RagOptions> ragOptions,
    ILogger<LlmQueryRewriter> logger) : IQueryRewriter
{
    private readonly UtilityChatOptions _utility = aiOptions.Value.UtilityChat;
    private readonly QueryRewritingOptions _rewriting = ragOptions.Value.QueryRewriting;

    public async Task<string> RewriteAsync(string originalQuestion, IReadOnlyList<ChatMessage> history, CancellationToken cancellationToken)
    {
        if (!_rewriting.Enabled)
        {
            return originalQuestion;
        }

        // The first turn has no pronoun to resolve, so skip it and save a round-trip.
        if (history.Count == 0 && _rewriting.SkipOnFirstTurn)
        {
            return originalQuestion;
        }

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(_utility.TimeoutSeconds));

            var transcript = string.Join(
                "\n",
                history.TakeLast(ChatHistoryWindow.MessageCountFor(_rewriting.HistoryTurns)).Select(message => $"{message.Role}: {message.Text}"));

            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, PromptBuilder.QueryRewriteSystemPrompt),
                new(ChatRole.User, $"{transcript}\nLast question: {originalQuestion}")
            };

            var response = await utilityChatClient.GetResponseAsync(
                messages,
                new ChatOptions { MaxOutputTokens = _rewriting.MaxOutputTokens },
                timeout.Token);

            var rewritten = response.Text?.Trim().Trim('"');

            if (string.IsNullOrWhiteSpace(rewritten) || rewritten.Length > originalQuestion.Length * 3)
            {
                logger.LogWarning(
                    "Query rewriting returned an unusable result (empty, or more than 3x the original length); falling back to the original question.");

                return originalQuestion;
            }

            return rewritten;
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            // A secondary step: on failure it degrades to the original question rather than failing the request.
            logger.LogWarning(exception, "Query rewriting failed, falling back to the original question.");

            return originalQuestion;
        }
    }
}
