namespace IChat.Infrastructure.Ai;

public sealed record ResolvedChatSettings
{
    public required string Model { get; init; }

public required int MaxOutputTokens { get; init; }

public required int TimeoutSeconds { get; init; }

public string? ApiKey { get; init; }

public string Label => "OpenAI";

public string RequireApiKey()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            throw new InvalidOperationException(
                $"Missing API key for {Label}. Set Ai__Chat__ApiKey (or Ai__UtilityChat__ApiKey) " +
                "as an environment variable, in user secrets, or in a secret store.");
        }

return ApiKey;
    }
}
