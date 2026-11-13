namespace IChat.Core.UnitTests.Ai;

using FluentAssertions;
using IChat.Infrastructure.Ai;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NUnit.Framework;

[TestFixture]
public sealed class ModelCatalogTests
{

    [Test]
    public void ChatTemperature_IsNull_WhenNotConfigured()
    {

        var catalog = CatalogFor(new ChatModelOptions { Model = "gpt-5.6-luna" });

        var temperature = catalog.ChatTemperature;

        temperature.Should().BeNull();
    }

    [Test]
    public void ChatTemperature_IsTheConfiguredValue_WhenConfigured()
    {

        var catalog = CatalogFor(new ChatModelOptions { Model = "gpt-4o-mini", Temperature = 0.2 });

        var temperature = catalog.ChatTemperature;

        temperature.Should().Be(0.2);
    }

    [TestCase("")]
    [TestCase(null)]
    public void Temperature_BindsToNull_WhenTheEnvironmentVariableIsEmpty(string? configured)
    {

        var configuration = ConfigurationFor(model: "gpt-5.6-luna", temperature: configured);
        var options = new AiOptions();

        configuration.GetSection("Ai").Bind(options);

        options.Chat.Temperature.Should().BeNull();
    }

    [Test]
    public void Temperature_Binds_WhenTheEnvironmentVariableHasAValue()
    {

        var configuration = ConfigurationFor(model: "gpt-4o-mini", temperature: "0.2");
        var options = new AiOptions();

        configuration.GetSection("Ai").Bind(options);

        options.Chat.Temperature.Should().Be(0.2);
    }

    private static ModelCatalog CatalogFor(ChatModelOptions chat)
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
