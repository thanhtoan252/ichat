namespace IChat.Infrastructure;

using IChat.Core.Abstractions;
using IChat.Core.Rag;
using IChat.Infrastructure.Ai;
using IChat.Infrastructure.Ingestion;
using IChat.Infrastructure.Ingestion.Parsing;
using IChat.Infrastructure.Persistence;
using IChat.Infrastructure.Search;
using IChat.Infrastructure.Search.Branches;
using IChat.Infrastructure.Security;
using IChat.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddIChatInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:Default.");

        services.AddIChatPersistence(connectionString);
        services.AddIChatAi(configuration);

        services.AddOptions<StorageOptions>()
                    .BindConfiguration(StorageOptions.SectionName)
                    .ValidateDataAnnotations()
                    .ValidateOnStart();

        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();

        services.AddHostedService<IdentitySeeder>();

        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddScoped<ContextAssembler>();

        services.AddSingleton<IUnsupportedFormatDetector, LegacyDocFormatDetector>();

        services.AddSingleton<IDocumentParser, DocxDocumentParser>();
        services.AddSingleton<IDocumentParser, PdfDocumentParser>();
        services.AddSingleton<IDocumentParser, MarkdownDocumentParser>();
        services.AddSingleton<IDocumentParser, PlainTextDocumentParser>();
        services.AddSingleton<IDocumentParserResolver, DocumentParserResolver>();

        services.AddScoped<IStructuredChunker, HeadingAwareChunker>();

        services.AddScoped<PostgresChunkStore>();
        services.AddScoped<IChunkSearch>(provider => provider.GetRequiredService<PostgresChunkStore>());
        services.AddScoped<IChunkLoader>(provider => provider.GetRequiredService<PostgresChunkStore>());

        services.AddScoped<IRetrievalBranch, VectorSearchBranch>();
        services.AddScoped<IRetrievalBranch, FullTextSearchBranch>();
        services.AddScoped<IRetrievalBranch, TrigramSearchBranch>();
        services.AddSingleton<IDocumentIngestionQueue, DocumentIngestionQueue>();
        services.AddHostedService<DocumentIngestionWorker>();

        return services;
    }
}
