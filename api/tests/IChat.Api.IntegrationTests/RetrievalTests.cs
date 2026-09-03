namespace IChat.Api.IntegrationTests;

using System.Net.Http.Json;
using System.Text.Json;
using IChat.Api.IntegrationTests.Fakes;
using FluentAssertions;
using Xunit;

[Collection(nameof(IChatApiCollection))]
public class RetrievalTests(IChatApiFactory factory)
{
    private const string Corpus = """
        # Cai dat he thong

        ## Cau hinh bien moi truong

        De cau hinh bien moi truong cho ung dung, ban mo file appsettings.json va dat khoa
        ConnectionStrings. Gia tri timeout mac dinh la 120 giay cho moi ket noi.

        ## Trien khai

        He thong duoc trien khai bang docker compose voi hai service la postgres va api.
        """;

    private async Task<Guid> SeedAsync(HttpClient client)
    {
        var response = await client.PostAsync(
            "/api/v1/documents",
            TestHelpers.InlineContent("corpus.md", "text/markdown", Corpus));

        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var documentId = payload.RootElement.GetProperty("id").GetGuid();

        (await TestHelpers.WaitForStatusAsync(factory, documentId, TimeSpan.FromSeconds(30))).Should().Be("Indexed");

        return documentId;
    }

    private static async Task<JsonElement> SearchAsync(HttpClient client, object body)
    {
        var response = await client.PostAsJsonAsync("/api/v1/search", body);
        response.EnsureSuccessStatusCode();

        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
    }

    [Fact]
    public async Task FullTextBranch_WithLongQuestion_ReturnsNonEmpty()
    {
        // REGRESSION TEST cho lỗi plainto_tsquery: nó AND mọi lexeme nên câu hỏi dài
        // sẽ trả rỗng gần như luôn luôn, và hybrid search âm thầm chỉ còn nhánh vector.
        var client = factory.CreateClient();
        await SeedAsync(client);

        var result = await SearchAsync(client, new
        {
            query = "lam the nao de toi cau hinh bien moi truong cho ung dung trong file cau hinh vay",
            mode = "FullText",
            rewrite = false
        });

        var fulltext = result.GetProperty("stages").GetProperty("fulltext");

        fulltext.GetProperty("count").GetInt32().Should().BeGreaterThan(
            0,
            "a long question must still match thanks to the OR-style tsquery");

        fulltext.GetProperty("tsQuery").GetString().Should().Contain(" | ");
    }

    [Fact]
    public async Task Search_ExposesTsQueryForDebugging()
    {
        var client = factory.CreateClient();
        await SeedAsync(client);

        var result = await SearchAsync(client, new { query = "cau hinh bien moi truong", rewrite = false });

        result.GetProperty("stages").GetProperty("fulltext").GetProperty("tsQuery").GetString()
            .Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Search_ReturnsEveryPipelineStage()
    {
        var client = factory.CreateClient();
        await SeedAsync(client);

        var stages = await SearchAsync(client, new { query = "cau hinh timeout", rewrite = false });
        var names = stages.GetProperty("stages").EnumerateObject().Select(property => property.Name).ToList();

        names.Should().Contain(["vector", "fulltext", "trigram", "fused", "afterMmr", "final"]);
    }

    [Fact]
    public async Task HybridSearch_FindsExpectedChunk()
    {
        var client = factory.CreateClient();
        await SeedAsync(client);

        var result = await SearchAsync(client, new { query = "timeout mac dinh la bao nhieu giay", rewrite = false });
        var final = result.GetProperty("stages").GetProperty("final").GetProperty("top");

        final.GetArrayLength().Should().BeGreaterThan(0);
        final.EnumerateArray()
            .Select(hit => hit.GetProperty("snippet").GetString() ?? string.Empty)
            .Should().Contain(snippet => snippet.Contains("120 giay"));
    }

    [Fact]
    public async Task VectorBranchOnly_StillReturnsResults()
    {
        var client = factory.CreateClient();
        await SeedAsync(client);

        var result = await SearchAsync(client, new { query = "cau hinh bien moi truong", mode = "Vector", rewrite = false });

        result.GetProperty("stages").GetProperty("vector").GetProperty("count").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task EmbeddingProviderFailure_DegradesToFullText_AndFlagsDegraded()
    {
        var client = factory.CreateClient();
        await SeedAsync(client);

        FakeEmbeddingGenerator.ShouldFail = true;

        try
        {
            var result = await SearchAsync(client, new { query = "cau hinh bien moi truong", rewrite = false });

            result.GetProperty("degraded").GetBoolean().Should().BeTrue();
            result.GetProperty("stages").GetProperty("vector").GetProperty("count").GetInt32().Should().Be(0);
            result.GetProperty("stages").GetProperty("fulltext").GetProperty("count").GetInt32()
                .Should().BeGreaterThan(0, "only now is the full-text branch actually usable as a fallback");
        }
        finally
        {
            FakeEmbeddingGenerator.ShouldFail = false;
        }
    }

    [Fact]
    public async Task Search_QueryOfOnlyStopWords_DoesNotCrash()
    {
        var client = factory.CreateClient();
        await SeedAsync(client);

        var result = await SearchAsync(client, new { query = "của là và các một cho với", mode = "FullText", rewrite = false });

        result.GetProperty("stages").GetProperty("fulltext").GetProperty("count").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task Search_EmptyQuery_Returns400()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/search", new { query = "" });

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Search_MaxChunksPerDocumentIsRespected()
    {
        var client = factory.CreateClient();
        await SeedAsync(client);

        var result = await SearchAsync(client, new { query = "cau hinh trien khai he thong", rewrite = false, topK = 8 });
        var final = result.GetProperty("stages").GetProperty("final").GetProperty("top");

        final.EnumerateArray()
            .GroupBy(hit => hit.GetProperty("documentId").GetGuid())
            .Should().OnlyContain(group => group.Count() <= 3);
    }
}
