namespace IChat.Infrastructure;

using IChat.Core.Abstractions;
using IChat.Core.Rag;
using IChat.Infrastructure.Ai;
using IChat.Infrastructure.Ingestion;
using IChat.Infrastructure.Ingestion.Parsing;
using IChat.Infrastructure.Persistence;
using IChat.Infrastructure.Search;
using IChat.Infrastructure.Search.Branches;
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

        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddScoped<ContextAssembler>();

        // Định dạng bị từ chối có chủ đích chặn ngay đầu chuỗi, trước mọi parser.
        services.AddSingleton<IUnsupportedFormatDetector, LegacyDocFormatDetector>();

        // Mỗi format một implementation IDocumentParser; resolver chọn theo
        // phần mở rộng + magic bytes chứ không tin content type client gửi.
        // THỨ TỰ ĐĂNG KÝ quyết định thứ tự dò và danh sách đuôi file trong thông điệp 415.
        services.AddSingleton<IDocumentParser, DocxDocumentParser>();
        services.AddSingleton<IDocumentParser, PdfDocumentParser>();
        services.AddSingleton<IDocumentParser, MarkdownDocumentParser>();
        services.AddSingleton<IDocumentParser, PlainTextDocumentParser>();
        services.AddSingleton<IDocumentParserResolver, DocumentParserResolver>();

        services.AddScoped<IStructuredChunker, HeadingAwareChunker>();

        // Một instance cho cả hai interface trong cùng scope: hai lần AddScoped riêng lẻ
        // sẽ dựng hai PostgresChunkStore khác nhau cho cùng một request.
        services.AddScoped<PostgresChunkStore>();
        services.AddScoped<IChunkSearch>(provider => provider.GetRequiredService<PostgresChunkStore>());
        services.AddScoped<IChunkLoader>(provider => provider.GetRequiredService<PostgresChunkStore>());

        // THỨ TỰ ĐĂNG KÝ LÀ CONTRACT: IEnumerable<T> của .NET trả về theo đúng thứ tự đăng ký,
        // và Retrieval Lab đọc các chặng theo thứ tự vector, fulltext, trigram. Đổi thứ tự ba
        // dòng dưới đây là đổi thứ tự chặng trong response của /api/v1/search.
        services.AddScoped<IRetrievalBranch, VectorSearchBranch>();
        services.AddScoped<IRetrievalBranch, FullTextSearchBranch>();
        services.AddScoped<IRetrievalBranch, TrigramSearchBranch>();
        services.AddSingleton<IDocumentIngestionQueue, DocumentIngestionQueue>();
        services.AddHostedService<DocumentIngestionWorker>();

        return services;
    }
}
