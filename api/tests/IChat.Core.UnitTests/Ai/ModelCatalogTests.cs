namespace IChat.Core.UnitTests.Ai;

using FluentAssertions;
using IChat.Infrastructure.Ai;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NUnit.Framework;

[TestFixture]
public sealed class ModelCatalogTests
{
    // The reasoning-family models only accept their default temperature and answer with HTTP 400 for any other
    // value. Leaving Temperature unconfigured has to mean "do not send this parameter", not falling back to
    // some guessed value.
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

    // docker-compose passes every `Ai__*` variable into the container even when it is not set: the value arrives
    // as an empty string rather than absent (see .env.gemini.example).
    // An empty string has to bind to null, otherwise the "do not send a temperature" escape hatch is unusable
    // through docker-compose.
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
