namespace IChat.Infrastructure.Ai;

using Microsoft.Extensions.AI;

/// <summary>
/// Returns the RAW client only. The middleware (caching, telemetry, logging, function invocation) is
/// attached once at the DI registration layer.
/// </summary>
public interface IChatClientFactory
{
    IChatClient Create();

    IChatClient CreateUtility();

}
