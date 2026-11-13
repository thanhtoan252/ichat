namespace IChat.Api.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IChat.Core.Domain.Identity;
using FluentAssertions;
using Xunit;

[Collection(nameof(IChatApiCollection))]
public class AuthorizationTests(IChatApiFactory factory)
{
    private static async Task<Guid> CreateConversationAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/conversations", new { title = "Mine" });
        response.EnsureSuccessStatusCode();

        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<JsonElement> ListConversationsAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/v1/conversations");
        response.EnsureSuccessStatusCode();

        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
    }

    [Fact]
    public async Task ProtectedEndpoints_WithoutToken_ReturnUnauthorized()
    {
        await factory.ResetDatabaseAsync();
        var anonymous = factory.CreateClient();

        (await anonymous.GetAsync("/api/v1/conversations")).StatusCode
                    .Should().Be(HttpStatusCode.Unauthorized);
        (await anonymous.GetAsync("/api/v1/documents")).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);
        (await anonymous.GetAsync("/api/v1/providers")).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LogoutAndProfile_NeedABearerToken_NotJustTheCookie()
    {

        await factory.ResetDatabaseAsync();
        var anonymous = factory.CreateClient();

        (await anonymous.PostAsync("/api/v1/auth/logout", content: null)).StatusCode
                    .Should().Be(HttpStatusCode.Unauthorized);
        (await anonymous.GetAsync("/api/v1/auth/me")).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task HealthEndpoints_StayAnonymous()
    {
        var anonymous = factory.CreateClient();

        (await anonymous.GetAsync("/health/live")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TheProviderCatalog_IsForbiddenForAUser()
    {
        await factory.ResetDatabaseAsync();
        var user = await factory.CreateClientAsync(UserRole.User, "plain-user");

        (await user.GetAsync("/api/v1/providers")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminOnlyWrites_AreForbiddenForAUser()
    {
        await factory.ResetDatabaseAsync();
        var user = await factory.CreateClientAsync(UserRole.User, "plain-user");

        var upload = await user.PostAsync(
                    "/api/v1/documents",
                    TestHelpers.InlineContent("blocked.md", "text/markdown", "# Nope"));

        var search = await user.PostAsJsonAsync("/api/v1/search", new { query = "bat ky" });
        var reindex = await user.PostAsync("/api/v1/admin/reindex", content: null);

        upload.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        search.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        reindex.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Documents_AreReadableByAnySignedInUser()
    {
        await factory.ResetDatabaseAsync();
        var user = await factory.CreateClientAsync(UserRole.User, "plain-user");

        (await user.GetAsync("/api/v1/documents")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Conversations_AreScopedToTheirOwner()
    {
        await factory.ResetDatabaseAsync();

        var alice = await factory.CreateClientAsync(UserRole.User, "alice");
        var bob = await factory.CreateClientAsync(UserRole.User, "bob");

        var aliceConversation = await CreateConversationAsync(alice);
        await CreateConversationAsync(bob);

        var aliceList = await ListConversationsAsync(alice);
        aliceList.GetProperty("totalCount").GetInt32().Should().Be(1);
        aliceList.GetProperty("items")[0].GetProperty("id").GetGuid().Should().Be(aliceConversation);

        (await bob.GetAsync($"/api/v1/conversations/{aliceConversation}/messages")).StatusCode
                    .Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AnAdminDoesNotSeeSomeoneElsesConversations()
    {

        await factory.ResetDatabaseAsync();

        var alice = await factory.CreateClientAsync(UserRole.User, "alice");
        var admin = await factory.CreateClientAsync(UserRole.Admin, "root");

        var aliceConversation = await CreateConversationAsync(alice);

        (await ListConversationsAsync(admin)).GetProperty("totalCount").GetInt32().Should().Be(0);

        (await admin.GetAsync($"/api/v1/conversations/{aliceConversation}/messages")).StatusCode
                    .Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AnAdminStillSeesTheirOwnConversations()
    {
        await factory.ResetDatabaseAsync();

        var admin = await factory.CreateClientAsync(UserRole.Admin, "root");
        var own = await CreateConversationAsync(admin);

        var list = await ListConversationsAsync(admin);

        list.GetProperty("totalCount").GetInt32().Should().Be(1);
        list.GetProperty("items")[0].GetProperty("id").GetGuid().Should().Be(own);
    }

    [Theory]
    [InlineData(UserRole.User)]
    [InlineData(UserRole.Admin)]
    public async Task SendingAMessageToSomeoneElsesConversation_ReportsItAsMissing(UserRole role)
    {
        await factory.ResetDatabaseAsync();

        var alice = await factory.CreateClientAsync(UserRole.User, "alice");
        var bob = await factory.CreateClientAsync(role, "bob");
        var conversationId = await CreateConversationAsync(alice);

        var response = await bob.PostAsJsonAsync(
                    $"/api/v1/conversations/{conversationId}/messages",
                    new { content = "Cho toi xem cai nay" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("event: error").And.Contain("NotFound");
    }
}
