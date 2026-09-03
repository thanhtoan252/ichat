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
    /// Nơi wiring toàn bộ AI. Tên hãng chỉ xuất hiện trong thư mục Ai/Providers — mỗi hãng
    /// một file — nên thêm hãng mới là thêm một file và một dòng đăng ký ở đây.
    /// Core và Api không có một dòng nào biết tới OpenAI/Anthropic/Google.
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

        // Một hãng một implementation; ChatClientFactory / EmbeddingGeneratorFactory chỉ tra
        // bảng theo enum provider. Quên đăng ký một hãng sẽ fail ProviderFactoryRegistryTests.
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

        // UseDistributedCache cần một IDistributedCache; in-memory là đủ cho một node.
        services.AddDistributedMemoryCache();

        var pipeline = configuration.GetSection($"{AiOptions.SectionName}:Pipeline").Get<PipelineOptions>() ?? new PipelineOptions();

        var chatBuilder = services.AddChatClient(serviceProvider =>
            serviceProvider.GetRequiredService<IChatClientFactory>().Create());

        ApplyPipeline(chatBuilder, pipeline, "IChat.Chat");

        // Client phụ, model rẻ, cho tác vụ nội bộ: viết lại câu hỏi, đặt tên hội thoại, rerank.
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
    /// Middleware gắn một lần ở đây thay vì nhét vào từng nhánh của factory, nên caching,
    /// telemetry và logging hoạt động y hệt nhau bất kể đang chạy provider nào.
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

            // Cohere/Voyage cần thêm một nhà cung cấp vào hệ thống; chưa bật ở phiên bản này
            // nên degrade về no-op thay vì ném lỗi lúc chạy.
            _ => new NoOpReranker()
        };
    }
}
