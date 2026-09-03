namespace IChat.Infrastructure.Ai.Providers;

using Microsoft.Extensions.AI;

public sealed class GoogleEmbeddingClientFactory : IEmbeddingProviderClientFactory
{
    public EmbeddingProvider Provider => EmbeddingProvider.Google;

    public IEmbeddingGenerator<string, Embedding<float>> Create(EmbeddingOptions options, int? dimensions)
    {
        // Cùng cảnh báo như GoogleChatClientFactory: docker-compose truyền endpoint dưới
        // dạng chuỗi RỖNG chứ không phải null, nên nhánh `??` không chạy khi chạy compose.
        var endpoint = options.Endpoint ?? OpenAICompatibleClientFactory.GoogleOpenAICompatibleEndpoint;

        return OpenAICompatibleClientFactory
            .Create(options.RequireApiKey(), endpoint)
            .GetEmbeddingClient(options.Model)
            .AsIEmbeddingGenerator(dimensions);
    }
}
