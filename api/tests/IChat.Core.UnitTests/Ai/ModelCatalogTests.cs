namespace IChat.Core.UnitTests.Ai;

using FluentAssertions;
using IChat.Infrastructure.Ai;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

public sealed class ModelCatalogTests
{
    // Các model dòng reasoning chỉ chấp nhận temperature mặc định và trả HTTP 400
    // nếu nhận bất kỳ giá trị nào khác. Không cấu hình Temperature phải nghĩa là
    // "không gửi tham số này đi", chứ không phải rơi về một giá trị đoán trước.
    [Fact]
    public void ChatTemperature_IsNull_WhenNotConfigured()
    {
        var catalog = CatalogFor(new ChatProviderOptions { Model = "gpt-5.6-luna" });

        catalog.ChatTemperature.Should().BeNull();
    }

    [Fact]
    public void ChatTemperature_IsTheConfiguredValue_WhenConfigured()
    {
        var catalog = CatalogFor(new ChatProviderOptions { Model = "gpt-4o-mini", Temperature = 0.2 });

        catalog.ChatTemperature.Should().Be(0.2);
    }

    // docker-compose truyền mọi biến `Ai__*` xuống container kể cả khi không set:
    // giá trị đến nơi là chuỗi rỗng chứ không phải vắng mặt (xem .env.gemini.example).
    // Chuỗi rỗng phải bind về null, nếu không thì escape hatch "không gửi temperature"
    // sẽ không dùng được qua docker-compose.
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Temperature_BindsToNull_WhenTheEnvironmentVariableIsEmpty(string? configured)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ai:Chat:Model"] = "gpt-5.6-luna",
                ["Ai:Chat:Temperature"] = configured
            })
            .Build();

        var options = new AiOptions();
        configuration.GetSection("Ai").Bind(options);

        options.Chat.Temperature.Should().BeNull();
    }

    [Fact]
    public void Temperature_Binds_WhenTheEnvironmentVariableHasAValue()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ai:Chat:Model"] = "gpt-4o-mini",
                ["Ai:Chat:Temperature"] = "0.2"
            })
            .Build();

        var options = new AiOptions();
        configuration.GetSection("Ai").Bind(options);

        options.Chat.Temperature.Should().Be(0.2);
    }

    private static ModelCatalog CatalogFor(ChatProviderOptions chat) =>
        new(Options.Create(new AiOptions { Chat = chat }));
}
