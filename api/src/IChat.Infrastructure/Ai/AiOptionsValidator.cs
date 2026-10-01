namespace IChat.Infrastructure.Ai;

using IChat.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

/// <summary>Fails fast at startup, so a configuration error never surfaces in a user's first request.</summary>
public sealed class AiOptionsValidator : IValidateOptions<AiOptions>
{
    public ValidateOptionsResult Validate(string? name, AiOptions options)
    {
        var failures = new List<string>();

        ValidateChat(options.ResolveChat(), "Ai:Chat", failures);
        ValidateChat(options.ResolveUtilityChat(), "Ai:UtilityChat", failures);

        var embeddingCapabilities = EmbeddingProviderCapabilities.For(options.Embedding.Provider);

        if (string.IsNullOrWhiteSpace(options.Embedding.Model))
        {
            failures.Add("Ai:Embedding:Model is required. Never hardcode a model name in C#.");
        }

        if (embeddingCapabilities.RequiresEndpoint && string.IsNullOrWhiteSpace(options.Embedding.Endpoint))
        {
            failures.Add($"Ai:Embedding:Endpoint is required for provider {options.Embedding.Provider}.");
        }

        if (embeddingCapabilities.RequiresApiKey && string.IsNullOrWhiteSpace(options.Embedding.ApiKey))
        {
            failures.Add(
                $"Missing API key for embedding provider {options.Embedding.Provider}: " +
                "set Ai:Embedding:ApiKey (env var Ai__Embedding__ApiKey, user secrets, or a secret store).");
        }

        // A vector(N) column fixes the dimension count at the schema level. A different dimension count is a
        // breaking change in the data layer and needs an EF migration changing the column type, not just a config edit.
        if (options.Embedding.Dimensions != EmbeddingDimensions.Default)
        {
            failures.Add(
                $"Ai:Embedding:Dimensions = {options.Embedding.Dimensions} but the schema is " +
                $"{EmbeddingDimensions.ColumnType}. Changing the dimension count requires an EF migration that " +
                "alters the column type plus a full reindex. See the README section 'Changing the embedding model'.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateChat(ResolvedChatSettings settings, string section, List<string> failures)
    {
        var capabilities = settings.Capabilities;

        if (string.IsNullOrWhiteSpace(settings.Model))
        {
            failures.Add($"{section}:Model is required. Never hardcode a model name in C#.");
        }

        if (capabilities.RequiresEndpoint && string.IsNullOrWhiteSpace(settings.Endpoint))
        {
            failures.Add($"{section}:Endpoint is required for provider {settings.Provider}.");
        }

        if (capabilities.RequiresApiKey && string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            failures.Add(
                $"Missing API key for {section} (provider {settings.Provider}): " +
                $"set {section}:ApiKey (env var {section.Replace(":", "__")}__ApiKey, user secrets, or a secret store).");
        }
    }
}
