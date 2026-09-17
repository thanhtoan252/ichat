namespace IChat.Infrastructure.Ai.Providers;

using Microsoft.Extensions.AI;

/// <summary>
/// One implementation per vendor. Unlike <see cref="IChatClientFactory"/>, which is THE ENTRY POINT:
/// it reads config, picks the vendor, then delegates down to here.
/// Returns the RAW client only: the middleware is still attached once at the DI registration layer.
/// </summary>
public interface IChatProviderClientFactory
{
    ChatProvider Provider { get; }

    IChatClient Create(ResolvedChatSettings settings);
}
