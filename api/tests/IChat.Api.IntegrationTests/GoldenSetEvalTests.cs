namespace IChat.Api.IntegrationTests;

using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

/// <summary>
/// Chạy golden set qua pipeline thật. Embedding ở đây là fake tất định nên con số
/// recall KHÔNG phải thước đo chất lượng ngữ nghĩa; nhưng tỷ lệ nhánh full-text trả rỗng
/// thì hoàn toàn thật, vì nhánh full-text không dùng embedding chút nào.
/// </summary>
[Collection(nameof(IChatApiCollection))]
public class GoldenSetEvalTests(IChatApiFactory factory, ITestOutputHelper output)
{
    private sealed class GoldenItem
    {
        public required string Tag { get; init; }

        public required string Question { get; init; }

        public required List<string> ExpectedAnswerContains { get; init; }

        public List<HistoryTurn>? History { get; init; }
    }

    private sealed class HistoryTurn
    {
        public required string Role { get; init; }

        public required string Content { get; init; }
    }

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private async Task SeedCorpusAsync(HttpClient client)
    {
        var corpusDirectory = Path.Combine(AppContext.BaseDirectory, "eval-corpus");
        var documentIds = new List<Guid>();

        foreach (var path in Directory.GetFiles(corpusDirectory, "*.md").OrderBy(path => path))
        {
            var response = await client.PostAsync(
                "/api/v1/documents",
                TestHelpers.InlineContent(Path.GetFileName(path), "text/markdown", await File.ReadAllTextAsync(path)));

            response.EnsureSuccessStatusCode();
            documentIds.Add(JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid());
        }

        foreach (var documentId in documentIds)
        {
            (await TestHelpers.WaitForStatusAsync(factory, documentId, TimeSpan.FromSeconds(60))).Should().Be("Indexed");
        }
    }

    [Fact]
    public async Task GoldenSet_FullTextBranchIsAlmostNeverEmpty()
    {
        var client = factory.CreateClient();
        await SeedCorpusAsync(client);

        var goldenSet = JsonSerializer.Deserialize<List<GoldenItem>>(
            await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "eval-golden-set.json")), Json)!;

        var emptyFullText = new List<string>();
        var foundInFullText = 0;
        var byTag = new Dictionary<string, (int Total, int FullTextHit)>();

        foreach (var item in goldenSet)
        {
            var response = await client.PostAsJsonAsync("/api/v1/search", new
            {
                query = item.Question,
                mode = "FullText",
                rewrite = false,
                topK = 8
            }, Json);

            response.EnsureSuccessStatusCode();

            var stages = JsonDocument.Parse(await response.Content.ReadAsStringAsync())
                .RootElement.GetProperty("stages");

            var fullText = stages.GetProperty("fulltext");
            var count = fullText.GetProperty("count").GetInt32();

            if (count == 0)
            {
                emptyFullText.Add(item.Question);
            }

            var hits = stages.GetProperty("final").GetProperty("top").EnumerateArray()
                .Select(hit => hit.GetProperty("snippet").GetString() ?? string.Empty)
                .ToList();

            var hit = item.ExpectedAnswerContains.Any(
                expected => hits.Any(snippet => snippet.Contains(expected, StringComparison.OrdinalIgnoreCase)));

            if (hit)
            {
                foundInFullText++;
            }

            var current = byTag.GetValueOrDefault(item.Tag);
            byTag[item.Tag] = (current.Total + 1, current.FullTextHit + (hit ? 1 : 0));
        }

        var emptyRate = emptyFullText.Count / (double)goldenSet.Count;

        output.WriteLine($"golden set: {goldenSet.Count} questions");
        output.WriteLine($"full-text empty     : {emptyFullText.Count}/{goldenSet.Count} = {emptyRate:P1}");
        output.WriteLine($"recall@8 (full-text branch only): {foundInFullText}/{goldenSet.Count} = {foundInFullText / (double)goldenSet.Count:P1}");

        foreach (var (tag, stats) in byTag.OrderBy(entry => entry.Key))
        {
            output.WriteLine($"  {tag,-12} recall@8 = {stats.FullTextHit}/{stats.Total}");
        }

        foreach (var question in emptyFullText)
        {
            output.WriteLine($"  [empty] {question}");
        }

        // Đây là điều kiện mà lỗi plainto_tsquery sẽ phá vỡ ngay lập tức:
        // với AND-query, gần như 100% câu hỏi dài sẽ trả rỗng.
        emptyRate.Should().BeLessThan(
            0.20,
            "an OR-style tsquery must keep the full-text branch alive; with plainto_tsquery this rate would be near 100%");
    }
}
