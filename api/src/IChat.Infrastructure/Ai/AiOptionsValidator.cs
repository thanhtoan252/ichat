namespace IChat.Infrastructure.Ai;

using IChat.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

public sealed class AiOptionsValidator : IValidateOptions<AiOptions>
{
    public ValidateOptionsResult Validate(string? name, AiOptions options)
    {
        var failures = new List<string>();

        ValidateChat(options.ResolveChat(), "Ai:Chat", failures);
        ValidateChat(options.ResolveUtilityChat(), "Ai:UtilityChat", failures);

        if (string.IsNullOrWhiteSpace(options.Embedding.Model))
        {
            failures.Add("Ai:Embedding:Model is required. Never hardcode a model name in C#.");
        }

        if (string.IsNullOrWhiteSpace(options.Embedding.ApiKey))
        {
            failures.Add(
                "Missing OpenAI API key for embeddings: " +
                "set Ai:Embedding:ApiKey (env var Ai__Embedding__ApiKey, user secrets, or a secret store).");
        }

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
        if (string.IsNullOrWhiteSpace(settings.Model))
        {
            failures.Add($"{section}:Model is required. Never hardcode a model name in C#.");
        }

        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            failures.Add(
                $"Missing OpenAI API key for {section}: " +
                $"set {section}:ApiKey (env var {section.Replace(":", "__")}__ApiKey, user secrets, or a secret store).");
        }
    }
}
