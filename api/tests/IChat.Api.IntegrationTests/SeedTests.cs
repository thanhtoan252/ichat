namespace IChat.Api.IntegrationTests;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using IChat.Core.Domain.Identity;
using IChat.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

[Collection(nameof(IChatApiCollection))]
public class SeedTests(IChatApiFactory factory)
{
    private static async Task<HttpResponseMessage> LoginAsync(HttpClient client, string userName, string password) =>
        await client.PostAsJsonAsync("/api/v1/auth/login", new { userName, password });

[Theory]
    [InlineData("admin", "admin", nameof(UserRole.Admin))]
    [InlineData("user", "user", nameof(UserRole.User))]
    public async Task TheDefaultAccountsCanSignIn(string userName, string password, string role)
    {
        await SeedAsync();
        var client = factory.CreateClient();

var response = await LoginAsync(client, userName, password);

response.StatusCode.Should().Be(HttpStatusCode.OK);

var accessToken = JsonDocument.Parse(await response.Content.ReadAsStringAsync())
            .RootElement.GetProperty("accessToken").GetString();

using var profile = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        profile.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

var me = await client.SendAsync(profile);
        me.StatusCode.Should().Be(HttpStatusCode.OK);

JsonDocument.Parse(await me.Content.ReadAsStringAsync())
            .RootElement.GetProperty("role").GetString().Should().Be(role);
    }

[Fact]
    public async Task SeedingTwiceDoesNotDuplicateAnAccount()
    {
        await SeedAsync();
        await SeedAsync();

using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IChatDbContext>();

(await dbContext.Users.CountAsync(user => user.UserName == "admin")).Should().Be(1);
        (await dbContext.Users.CountAsync(user => user.UserName == "user")).Should().Be(1);
    }

private async Task SeedAsync()
    {
        using var scope = factory.Services.CreateScope();
        var seeder = factory.Services.GetServices<Microsoft.Extensions.Hosting.IHostedService>()
            .OfType<IChat.Infrastructure.Security.IdentitySeeder>()
            .Single();

await seeder.StartAsync(CancellationToken.None);
        await seeder.ExecuteTask!;
    }
}
