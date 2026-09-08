namespace IChat.Infrastructure.Ai;

using System.Text;
using System.Text.Json;
using IChat.Core.Abstractions;
using IChat.Core.Rag;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Rerank bằng LLM chấm điểm. Khác với RRF (fusion, chỉ dựa trên thứ hạng),
/// bước này thực sự đọc lại nội dung để chấm độ liên quan.
/// </summary>
public sealed class LlmReranker(
    [FromKeyedServices(AiServiceKeys.UtilityChat)] IChatClient utilityChatClient,
    IOptions<RagOptions> ragOptions,
    ILogger<LlmReranker> logger) : IReranker
{
    private readonly RerankingOptions _reranking = ragOptions.Value.Reranking;

    public async Task<IReadOnlyList<ScoredChunk>> RerankAsync(string query, IReadOnlyList<ScoredChunk> candidates, CancellationToken cancellationToken)
    {
        if (candidates.Count <= 1)
        {
            return candidates;
        }

        var input = candidates.Take(_reranking.InputTopK).ToList();

        try
        {
            var prompt = new StringBuilder();
            prompt.AppendLine($"Question: {query}").AppendLine().AppendLine("Passages:");

            for (var i = 0; i < input.Count; i++)
            {
                var snippet = input[i].Content.Length > 600 ? input[i].Content[..600] : input[i].Content;
                prompt.AppendLine($"[{i}] {snippet}").AppendLine();
            }

            prompt.AppendLine("""
                Score how relevant each passage is to the question, on a scale of 0 to 10.
                Return ONLY JSON shaped like [{"index": 0, "score": 8}], with no further explanation.
                """);

            var response = await utilityChatClient.GetResponseAsync(
                [new ChatMessage(ChatRole.User, prompt.ToString())],
                new ChatOptions { MaxOutputTokens = 512 },
                cancellationToken);

            var scores = ParseScores(response.Text, input.Count);

            if (scores.Count == 0)
            {
                logger.LogWarning("LlmReranker could not parse the scores, keeping the input order.");

                return candidates;
            }

            return input
                .Select((chunk, index) => (chunk, score: scores.GetValueOrDefault(index, 0d)))
                .OrderByDescending(item => item.score)
                .Take(_reranking.OutputTopK)
                .Select(item => item.chunk.WithScore(item.score / 10d))
                .ToList();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Rerank là bước phụ: hỏng thì degrade về thứ tự sẵn có.
            logger.LogWarning(exception, "LlmReranker failed, keeping the pre-rerank order.");

            return candidates;
        }
    }

    /// <summary>Parse phòng thủ: model hay bọc JSON trong markdown fence hoặc thêm lời dẫn.</summary>
    private static Dictionary<int, double> ParseScores(string? text, int candidateCount)
    {
        var result = new Dictionary<int, double>();

        if (string.IsNullOrWhiteSpace(text))
        {
            return result;
        }

        var start = text.IndexOf('[');
        var end = text.LastIndexOf(']');

        if (start < 0 || end <= start)
        {
            return result;
        }

        try
        {
            using var document = JsonDocument.Parse(text[start..(end + 1)]);

            foreach (var element in document.RootElement.EnumerateArray())
            {
                if (!element.TryGetProperty("index", out var indexProperty) ||
                    !element.TryGetProperty("score", out var scoreProperty))
                {
                    continue;
                }

                if (!indexProperty.TryGetInt32(out var index) || index < 0 || index >= candidateCount)
                {
                    continue;
                }

                if (scoreProperty.TryGetDouble(out var score))
                {
                    result[index] = score;
                }
            }
        }
        catch (JsonException)
        {
            return [];
        }

        return result;
    }
}
