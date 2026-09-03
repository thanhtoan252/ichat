namespace IChat.Infrastructure.Ai.Providers;

using Microsoft.Extensions.AI;

public sealed class GoogleChatClientFactory : IChatProviderClientFactory
{
    public ChatProvider Provider => ChatProvider.Google;

    public IChatClient Create(ResolvedChatSettings settings)
    {
        // CẢNH BÁO: docker-compose luôn truyền Ai__*__Endpoint xuống container dưới dạng
        // chuỗi RỖNG chứ không phải null, nên nhánh `?? default` dưới đây KHÔNG chạy khi
        // chạy bằng docker-compose — file env phải set Endpoint tường minh (.env.gemini.example).
        var endpoint = settings.Endpoint ?? OpenAICompatibleClientFactory.GoogleOpenAICompatibleEndpoint;

        return OpenAICompatibleClientFactory
            .Create(settings.RequireApiKey(), endpoint)
            .GetChatClient(settings.Model)
            .AsIChatClient();
    }
}
