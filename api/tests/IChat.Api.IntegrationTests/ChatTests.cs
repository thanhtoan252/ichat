namespace IChat.Api.IntegrationTests;

using System.Net.Http.Json;
using System.Text.Json;
using IChat.Api.IntegrationTests.Fakes;
using IChat.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

[Collection(nameof(IChatApiCollection))]
public class ChatTests(IChatApiFactory factory)
{
    private const string Corpus = """
        # Cau hinh he thong

        Timeout mac dinh la 120 giay. Bien moi truong duoc dat trong file appsettings.json.
        """;

    private sealed class SseFrame
    {
        public required string EventType { get; init; }

        public required string Data { get; init; }
    }

    private async Task SeedAsync(HttpClient client)
    {
        var response = await client.PostAsync(
            "/api/v1/documents",
            TestHelpers.InlineContent("chat-corpus.md", "text/markdown", Corpus));

        var documentId = JsonDocument.Parse(await response.Content.ReadAsStringAsync())
            .RootElement.GetProperty("id").GetGuid();

        (await TestHelpers.WaitForStatusAsync(factory, documentId, TimeSpan.FromSeconds(30))).Should().Be("Indexed");
    }

    private static async Task<Guid> CreateConversationAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/conversations", new { title = "Test" });
        response.EnsureSuccessStatusCode();

        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CreateUntitledConversationAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/conversations", new { });
        response.EnsureSuccessStatusCode();

        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<string> ReadTitleAsync(HttpClient client, Guid conversationId)
    {
        var response = await client.GetAsync("/api/v1/conversations?page=1&pageSize=50");
        response.EnsureSuccessStatusCode();

        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement
            .GetProperty("items").EnumerateArray()
            .Single(item => item.GetProperty("id").GetGuid() == conversationId)
            .GetProperty("title").GetString()!;
    }

    private static async Task<List<SseFrame>> ReadSseAsync(HttpClient client, Guid conversationId, object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/conversations/{conversationId}/messages")
        {
            Content = JsonContent.Create(body)
        };

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/event-stream");

        var frames = new List<SseFrame>();
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        string? eventType = null;

        while (await reader.ReadLineAsync() is { } line)
        {
            if (line.StartsWith("event:", StringComparison.Ordinal))
            {
                eventType = line[6..].Trim();
            }
            else if (line.StartsWith("data:", StringComparison.Ordinal))
            {
                frames.Add(new SseFrame
                {
                    EventType = eventType ?? "message",
                    Data = line[5..].Trim()
                });
            }
        }

        return frames;
    }

    [Fact]
    public async Task SendMessage_EmitsAllFourEventTypesInOrder()
    {
        var client = await factory.CreateClientAsync();
        await SeedAsync(client);
        var conversationId = await CreateConversationAsync(client);

        factory.MainChat.ResponseText = "Timeout mac dinh la 120 giay [1].";

        var frames = await ReadSseAsync(client, conversationId, new { content = "timeout mac dinh la bao nhieu" });
        var types = frames.Select(frame => frame.EventType).ToList();

        types.Should().Contain("status");
        types.Should().Contain("sources");
        types.Should().Contain("delta");
        types.Should().Contain("done");

        types.IndexOf("sources").Should().BeLessThan(types.IndexOf("delta"), "sources must arrive before the first token");
        types.Last().Should().Be("done");
    }

    [Fact]
    public async Task SendMessage_StatusStagesAreReported()
    {
        var client = await factory.CreateClientAsync();
        await SeedAsync(client);
        var conversationId = await CreateConversationAsync(client);

        var frames = await ReadSseAsync(client, conversationId, new { content = "cau hinh o dau" });

        var stages = frames
            .Where(frame => frame.EventType == "status")
            .Select(frame => JsonDocument.Parse(frame.Data).RootElement.GetProperty("stage").GetString())
            .ToList();

        stages.Should().Contain(["rewriting", "retrieving", "generating"]);
    }

    [Fact]
    public async Task SendMessage_OnlyValidMarkersBecomeCitations()
    {
        var client = await factory.CreateClientAsync();
        await SeedAsync(client);
        var conversationId = await CreateConversationAsync(client);

        // [99] nằm ngoài phạm vi context nên phải bị loại; chỉ [1] được ghi vào DB.
        factory.MainChat.ResponseText = "Theo tai lieu [1] va cung theo [99] thi timeout la 120 giay.";

        var frames = await ReadSseAsync(client, conversationId, new { content = "timeout la bao nhieu" });
        var done = JsonDocument.Parse(frames.Last(frame => frame.EventType == "done").Data).RootElement;

        var markers = done.GetProperty("citations").EnumerateArray()
            .Select(citation => citation.GetProperty("markerIndex").GetInt32())
            .Distinct()
            .ToList();

        markers.Should().Contain(1);
        markers.Should().NotContain(99, "markers outside the context range must be dropped");

        var messageId = done.GetProperty("messageId").GetGuid();

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IChatDbContext>();
        var stored = await dbContext.MessageCitations.AsNoTracking()
            .Where(citation => citation.MessageId == messageId)
            .ToListAsync();

        stored.Should().NotBeEmpty();
        stored.Should().OnlyContain(citation => citation.MarkerIndex == 1);
    }

    [Fact]
    public async Task SendMessage_AnswerWithNoMarkers_WritesNoCitations()
    {
        var client = await factory.CreateClientAsync();
        await SeedAsync(client);
        var conversationId = await CreateConversationAsync(client);

        factory.MainChat.ResponseText = "Toi khong tim thay thong tin trong tai lieu.";

        var frames = await ReadSseAsync(client, conversationId, new { content = "mau sac cua mat trang" });
        var done = JsonDocument.Parse(frames.Last(frame => frame.EventType == "done").Data).RootElement;

        done.GetProperty("citations").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task SendMessage_PersistsRewrittenQueryAndProviderMetadata()
    {
        var client = await factory.CreateClientAsync();
        await SeedAsync(client);
        var conversationId = await CreateConversationAsync(client);

        factory.MainChat.ResponseText = "Cau tra loi [1].";
        await ReadSseAsync(client, conversationId, new { content = "cau hinh bien moi truong" });

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IChatDbContext>();
        var messages = await dbContext.Messages.AsNoTracking()
            .Where(message => message.ConversationId == conversationId)
            .OrderBy(message => message.CreatedAt)
            .ToListAsync();

        messages.Should().HaveCount(2);

        var userMessage = messages[0];
        userMessage.RewrittenQuery.Should().NotBeNullOrWhiteSpace("rewritten_query is the column looked at most when debugging retrieval");
        userMessage.RetrievalMs.Should().NotBeNull();

        var assistantMessage = messages[1];
        assistantMessage.Provider.Should().NotBeNullOrWhiteSpace();
        assistantMessage.Model.Should().NotBeNullOrWhiteSpace();
        assistantMessage.LatencyMs.Should().NotBeNull();
    }

    [Fact]
    public async Task SendMessage_QueryRewritingTimeout_FallsBackToOriginal_RequestStillSucceeds()
    {
        var client = await factory.CreateClientAsync();
        await SeedAsync(client);
        var conversationId = await CreateConversationAsync(client);

        // Lượt đầu để lịch sử không rỗng, nếu không rewriting sẽ bị bỏ qua.
        factory.MainChat.ResponseText = "Tra loi dau tien [1].";
        await ReadSseAsync(client, conversationId, new { content = "cau hinh he thong" });

        factory.UtilityChat.ShouldTimeout = true;

        try
        {
            var frames = await ReadSseAsync(client, conversationId, new { content = "vay no o dau" });

            frames.Should().Contain(frame => frame.EventType == "done", "a broken rewriting step may only degrade, never break the request");
        }
        finally
        {
            factory.UtilityChat.ShouldTimeout = false;
        }
    }

    [Fact]
    public async Task SendMessage_ModelOutsideAllowlist_EmitsErrorEvent()
    {
        var client = await factory.CreateClientAsync();
        var conversationId = await CreateConversationAsync(client);

        var frames = await ReadSseAsync(client, conversationId, new { content = "xin chao", model = "gpt-khong-ton-tai" });

        frames.Should().Contain(frame => frame.EventType == "error");
    }

    [Fact]
    public async Task SendMessage_UnknownConversation_EmitsErrorEvent()
    {
        var client = await factory.CreateClientAsync();

        var frames = await ReadSseAsync(client, Guid.NewGuid(), new { content = "xin chao" });

        frames.Should().Contain(frame => frame.EventType == "error");
    }

    [Fact]
    public async Task GetMessages_ReturnsPersistedConversation()
    {
        var client = await factory.CreateClientAsync();
        await SeedAsync(client);
        var conversationId = await CreateConversationAsync(client);

        factory.MainChat.ResponseText = "Cau tra loi [1].";
        await ReadSseAsync(client, conversationId, new { content = "timeout" });

        var response = await client.GetAsync($"/api/v1/conversations/{conversationId}/messages");
        response.EnsureSuccessStatusCode();

        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        payload.GetProperty("items").GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task SendMessage_FirstTurnOfUntitledConversation_NamesItAfterTheQuestion()
    {
        var client = await factory.CreateClientAsync();
        await SeedAsync(client);
        var conversationId = await CreateUntitledConversationAsync(client);

        factory.MainChat.ResponseText = "Timeout mac dinh la 120 giay [1].";
        await ReadSseAsync(client, conversationId, new { content = "timeout mac dinh la bao nhieu" });

        (await ReadTitleAsync(client, conversationId))
            .Should().Be("timeout mac dinh la bao nhieu", "an untitled thread is named after its first question");
    }

    [Fact]
    public async Task SendMessage_SecondTurn_KeepsTheTitleOfTheFirstQuestion()
    {
        var client = await factory.CreateClientAsync();
        await SeedAsync(client);
        var conversationId = await CreateUntitledConversationAsync(client);

        factory.MainChat.ResponseText = "Cau tra loi [1].";
        await ReadSseAsync(client, conversationId, new { content = "cau hinh he thong" });
        await ReadSseAsync(client, conversationId, new { content = "vay timeout la bao nhieu" });

        (await ReadTitleAsync(client, conversationId)).Should().Be("cau hinh he thong");
    }

    [Fact]
    public async Task SendMessage_ConversationCreatedWithATitle_KeepsIt()
    {
        var client = await factory.CreateClientAsync();
        await SeedAsync(client);
        var conversationId = await CreateConversationAsync(client);

        factory.MainChat.ResponseText = "Cau tra loi [1].";
        await ReadSseAsync(client, conversationId, new { content = "timeout mac dinh la bao nhieu" });

        (await ReadTitleAsync(client, conversationId)).Should().Be("Test", "a title chosen by the caller is never overwritten");
    }
}
