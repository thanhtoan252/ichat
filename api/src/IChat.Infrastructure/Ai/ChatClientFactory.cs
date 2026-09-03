namespace IChat.Infrastructure.Ai;

using IChat.Infrastructure.Ai.Providers;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

/// <summary>
/// CỬA VÀO: đọc config, chọn hãng, rồi uỷ quyền xuống đúng một
/// <see cref="IChatProviderClientFactory"/>. Bản thân nó không biết hãng nào tồn tại.
/// </summary>
public sealed class ChatClientFactory(
    IOptions<AiOptions> options,
    IEnumerable<IChatProviderClientFactory> providers) : IChatClientFactory
{
    private readonly AiOptions _options = options.Value;

    private readonly Dictionary<ChatProvider, IChatProviderClientFactory> _providers =
        providers.ToDictionary(factory => factory.Provider);

    public ChatProviderCapabilities Capabilities => _options.ResolveChat().Capabilities;

    public ChatProviderCapabilities UtilityCapabilities => _options.ResolveUtilityChat().Capabilities;

    public IChatClient Create()
    {
        return Build(_options.ResolveChat());
    }

    public IChatClient CreateUtility()
    {
        return Build(_options.ResolveUtilityChat());
    }

    private IChatClient Build(ResolvedChatSettings settings)
    {
        if (!_providers.TryGetValue(settings.Provider, out var factory))
        {
            throw new InvalidOperationException($"Unsupported chat provider: {settings.Provider}.");
        }

        return factory.Create(settings);
    }
}
