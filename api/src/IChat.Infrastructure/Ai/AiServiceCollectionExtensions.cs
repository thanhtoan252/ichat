namespace IChat.Infrastructure.Ai;

using IChat.Core.Abstractions;
using IChat.Core.Rag;
using IChat.Infrastructure.Ai.Providers;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

public static class AiServiceCollectionExtensions
{
    /// <summary>
    /// Where all of the AI wiring happens. Vendor names appear only under Ai/Providers — one file per vendor —
    /// so adding a vendor means one new file and one registration line here.
    /// Not a single line in Core or Api knows about OpenAI/Anthropic/Google.
    /// </summary>
    public static IServiceCollection AddIChatAi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AiOptions>()
            .BindConfiguration(AiOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<AiOptions>, AiOptionsValidator>();

        services.AddOptions<RagOptions>()
            .BindConfiguration(RagOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // One implementation per vendor; ChatClientFactory / EmbeddingGeneratorFactory only look the provider up
        // by enum. Forgetting to register a vendor fails ProviderFactoryRegistryTests.
        services.AddSingleton<IChatProviderClientFactory, OpenAIChatClientFactory>();
        services.AddSingleton<IChatProviderClientFactory, AzureOpenAIChatClientFactory>();
        services.AddSingleton<IChatProviderClientFactory, AnthropicChatClientFactory>();
        services.AddSingleton<IChatProviderClientFactory, GoogleChatClientFactory>();

        services.AddSingleton<IEmbeddingProviderClientFactory, OpenAIEmbeddingClientFactory>();
        services.AddSingleton<IEmbeddingProviderClientFactory, AzureOpenAIEmbeddingClientFactory>();
        services.AddSingleton<IEmbeddingProviderClientFactory, GoogleEmbeddingClientFactory>();

        services.AddSingleton<IChatClientFactory, ChatClientFactory>();
        services.AddSingleton<IEmbeddingGeneratorFactory, EmbeddingGeneratorFactory>();
        services.AddSingleton<IModelCatalog, ModelCatalog>();
        services.AddSingleton<ITokenEstimator, SimpleTokenEstimator>();

        // UseDistributedCache needs an IDistributedCache; in-memory is enough for a single node.
        services.AddDistributedMemoryCache();

        var pipeline = configuration.GetSection($"{AiOptions.SectionName}:Pipeline").Get<PipelineOptions>() ?? new PipelineOptions();

        var chatBuilder = services.AddChatClient(serviceProvider =>
            serviceProvider.GetRequiredService<IChatClientFactory>().Create());

        ApplyPipeline(chatBuilder, pipeline, "IChat.Chat");

        // The secondary client, on a cheap model, for internal work: query rewriting, conversation titles, reranking.
        var utilityBuilder = services.AddKeyedChatClient(
            AiServiceKeys.UtilityChat,
            serviceProvider => serviceProvider.GetRequiredService<IChatClientFactory>().CreateUtility());

        ApplyPipeline(utilityBuilder, pipeline, "IChat.UtilityChat");

        var embeddingBuilder = services.AddEmbeddingGenerator(serviceProvider =>
            serviceProvider.GetRequiredService<IEmbeddingGeneratorFactory>().Create());

        if (pipeline.EnableOpenTelemetry)
        {
            embeddingBuilder.UseOpenTelemetry(sourceName: "IChat.Embedding");
        }

        embeddingBuilder.UseLogging();

        services.AddScoped<BatchingEmbeddingService>();
        services.AddScoped<IQueryRewriter, LlmQueryRewriter>();
        services.AddScoped<IReranker>(ResolveReranker);

        services.AddHostedService<EmbeddingModelGuard>();

        return services;
    }

    /// <summary>
    /// The middleware is attached once here rather than inside each branch of the factory, so caching,
    /// telemetry and logging behave identically no matter which provider is running.
    /// </summary>
    private static void ApplyPipeline(ChatClientBuilder builder, PipelineOptions pipeline, string telemetrySourceName)
    {
        if (pipeline.EnableFunctionInvocation)
        {
            builder.UseFunctionInvocation();
        }

        if (pipeline.EnableCaching)
        {
            builder.UseDistributedCache();
        }

        if (pipeline.EnableOpenTelemetry)
        {
            builder.UseOpenTelemetry(sourceName: telemetrySourceName);
        }

        builder.UseLogging();
    }

    private static IReranker ResolveReranker(IServiceProvider serviceProvider)
    {
        var mode = serviceProvider.GetRequiredService<IOptions<RagOptions>>().Value.Reranking.Mode;

        return mode switch
        {
            RerankMode.Llm => ActivatorUtilities.CreateInstance<LlmReranker>(serviceProvider),

            // Cohere/Voyage would mean adding another vendor to the system; not enabled in this version,
            // so it degrades to a no-op instead of throwing at runtime.
            _ => new NoOpReranker()
        };
    }
}
