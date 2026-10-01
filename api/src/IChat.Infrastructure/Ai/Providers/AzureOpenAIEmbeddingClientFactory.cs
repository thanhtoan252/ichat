namespace IChat.Infrastructure.Ai.Providers;

using System.ClientModel;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;

public sealed class AzureOpenAIEmbeddingClientFactory : IEmbeddingProviderClientFactory
{
    public EmbeddingProvider Provider => EmbeddingProvider.AzureOpenAI;

    public IEmbeddingGenerator<string, Embedding<float>> Create(EmbeddingOptions options, int? dimensions)
    {
        if (string.IsNullOrWhiteSpace(options.Endpoint))
        {
            throw new InvalidOperationException("Provider AzureOpenAI requires Ai:Embedding:Endpoint.");
        }

        var credential = new ApiKeyCredential(options.RequireApiKey());

        return new AzureOpenAIClient(new Uri(options.Endpoint), credential)
            .GetEmbeddingClient(options.Model)
            .AsIEmbeddingGenerator(dimensions);
    }
}
