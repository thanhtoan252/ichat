namespace IChat.Infrastructure.Ai;

using Microsoft.Extensions.AI;

/// <summary>
/// Returns the RAW client only. The middleware (caching, telemetry, logging, function invocation) is
/// attached once at the DI registration layer so every provider behaves identically.
/// </summary>
public interface IChatClientFactory
{
    IChatClient Create();

    IChatClient CreateUtility();

    ChatProviderCapabilities Capabilities { get; }

    ChatProviderCapabilities UtilityCapabilities { get; }
}
