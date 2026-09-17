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
        // Runs after the migrations in Program.cs, so the users table already exists.
        services.AddHostedService<IdentitySeeder>();

        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddScoped<ContextAssembler>();

        // Deliberately rejected formats are stopped at the very front, ahead of every parser.
        services.AddSingleton<IUnsupportedFormatDetector, LegacyDocFormatDetector>();

        // One IDocumentParser implementation per format; the resolver picks by extension plus magic bytes
        // rather than trusting the content type the client sent.
        // REGISTRATION ORDER decides the probing order and the list of extensions in the 415 message.
        services.AddSingleton<IDocumentParser, DocxDocumentParser>();
        services.AddSingleton<IDocumentParser, PdfDocumentParser>();
        services.AddSingleton<IDocumentParser, MarkdownDocumentParser>();
        services.AddSingleton<IDocumentParser, PlainTextDocumentParser>();
        services.AddSingleton<IDocumentParserResolver, DocumentParserResolver>();

        services.AddScoped<IStructuredChunker, HeadingAwareChunker>();

        // One instance behind both interfaces within a scope: two separate AddScoped calls would build two
        // different PostgresChunkStore instances for the same request.
        services.AddScoped<PostgresChunkStore>();
        services.AddScoped<IChunkSearch>(provider => provider.GetRequiredService<PostgresChunkStore>());
        services.AddScoped<IChunkLoader>(provider => provider.GetRequiredService<PostgresChunkStore>());

        // REGISTRATION ORDER IS A CONTRACT: .NET's IEnumerable<T> returns services in registration order, and
        // the Retrieval Lab reads the stages as vector, fulltext, trigram. Reordering the three lines below
        // reorders the stages in the /api/v1/search response.
        services.AddScoped<IRetrievalBranch, VectorSearchBranch>();
        services.AddScoped<IRetrievalBranch, FullTextSearchBranch>();
        services.AddScoped<IRetrievalBranch, TrigramSearchBranch>();
        services.AddSingleton<IDocumentIngestionQueue, DocumentIngestionQueue>();
        services.AddHostedService<DocumentIngestionWorker>();

        return services;
    }
}
