namespace IChat.Infrastructure.Ai;

using IChat.Infrastructure.Ai.Providers;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

public sealed class ChatClientFactory(
    IOptions<AiOptions> options,
    OpenAIChatClientFactory openAi) : IChatClientFactory
{
    private readonly AiOptions _options = options.Value;

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
        return openAi.Create(settings);
    }
}
