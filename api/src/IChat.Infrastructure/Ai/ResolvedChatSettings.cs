namespace IChat.Infrastructure.Ai;

/// <summary>
/// One chat provider's configuration AFTER the UtilityChat -> Chat fallback has been applied.
/// Enough to build a client, describe a status or validate, without rereading AiOptions.
/// </summary>
public sealed record ResolvedChatSettings
{
    public required ChatProvider Provider { get; init; }

    public required string Model { get; init; }

    public required int MaxOutputTokens { get; init; }

    public required int TimeoutSeconds { get; init; }

    public string? Endpoint { get; init; }

    public string? ApiKey { get; init; }

    public ChatProviderCapabilities Capabilities => ChatProviderCapabilities.For(Provider);

    /// <summary>The name shown in error messages; it matches the config section's branch name.</summary>
    public string Label => Provider.ToString();

    /// <summary>
    /// AiOptionsValidator already rejects a missing key at startup; this is the safety net for the paths
    /// that do not go through ValidateOnStart (tests, tooling).
    /// </summary>
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
