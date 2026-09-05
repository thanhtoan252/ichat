namespace IChat.Core.UnitTests.Ai;

using FluentAssertions;
using IChat.Infrastructure.Ai;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NUnit.Framework;

[TestFixture]
public sealed class ModelCatalogTests
{
    // Các model dòng reasoning chỉ chấp nhận temperature mặc định và trả HTTP 400
    // nếu nhận bất kỳ giá trị nào khác. Không cấu hình Temperature phải nghĩa là
    // "không gửi tham số này đi", chứ không phải rơi về một giá trị đoán trước.
    [Test]
    public void ChatTemperature_IsNull_WhenNotConfigured()
    {
        // Arrange
        var catalog = CatalogFor(new ChatProviderOptions { Model = "gpt-5.6-luna" });

        // Act
        var temperature = catalog.ChatTemperature;

        // Assert
        temperature.Should().BeNull();
    }

    [Test]
    public void ChatTemperature_IsTheConfiguredValue_WhenConfigured()
    {
        // Arrange
        var catalog = CatalogFor(new ChatProviderOptions { Model = "gpt-4o-mini", Temperature = 0.2 });

        // Act
        var temperature = catalog.ChatTemperature;

        // Assert
        temperature.Should().Be(0.2);
    }

    // docker-compose truyền mọi biến `Ai__*` xuống container kể cả khi không set:
    // giá trị đến nơi là chuỗi rỗng chứ không phải vắng mặt (xem .env.gemini.example).
    // Chuỗi rỗng phải bind về null, nếu không thì escape hatch "không gửi temperature"
    // sẽ không dùng được qua docker-compose.
    [TestCase("")]
    [TestCase(null)]
    public void Temperature_BindsToNull_WhenTheEnvironmentVariableIsEmpty(string? configured)
    {
        // Arrange
        var configuration = ConfigurationFor(model: "gpt-5.6-luna", temperature: configured);
        var options = new AiOptions();

        // Act
        configuration.GetSection("Ai").Bind(options);

        // Assert
        options.Chat.Temperature.Should().BeNull();
    }

    [Test]
    public void Temperature_Binds_WhenTheEnvironmentVariableHasAValue()
    {
        // Arrange
        var configuration = ConfigurationFor(model: "gpt-4o-mini", temperature: "0.2");
        var options = new AiOptions();

        // Act
        configuration.GetSection("Ai").Bind(options);

        // Assert
        options.Chat.Temperature.Should().Be(0.2);
    }

    private static ModelCatalog CatalogFor(ChatProviderOptions chat)
    {
        return new ModelCatalog(Options.Create(new AiOptions { Chat = chat }));
    }

    private static IConfiguration ConfigurationFor(string model, string? temperature)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ai:Chat:Model"] = model,
                ["Ai:Chat:Temperature"] = temperature
            })
            .Build();
    }
}
