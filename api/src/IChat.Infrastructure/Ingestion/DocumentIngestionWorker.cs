namespace IChat.Infrastructure.Ingestion;

using System.Text.Json;
using IChat.Core.Abstractions;
using IChat.Core.Domain.Documents;
using IChat.Core.Domain.Documents.Parsing;
using IChat.Infrastructure.Ai;
using IChat.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public sealed class DocumentIngestionWorker(
    IDocumentIngestionQueue queue,
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<DocumentIngestionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var documentId in queue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                await ProcessAsync(documentId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Ingestion failed for document {DocumentId}.", documentId);
                await TryMarkFailedAsync(documentId, exception.Message, stoppingToken);
            }
        }
    }

    private async Task ProcessAsync(Guid documentId, CancellationToken cancellationToken)
    {
        // One DI scope per document; never capture a scoped service in the constructor.
        using var scope = scopeFactory.CreateScope();
        var services = IngestionServices.From(scope.ServiceProvider);

        var document = await services.DbContext.Documents
            .SingleOrDefaultAsync(item => item.Id == documentId, cancellationToken);

        if (document is null)
        {
            logger.LogWarning("Document {DocumentId} no longer exists, skipping.", documentId);

            return;
        }

        document.MarkProcessing();
        await services.DbContext.SaveChangesAsync(cancellationToken);

        var parsed = await ParseAsync(services, document, cancellationToken);

        if (parsed is null)
        {
            return;
        }

        var chunks = services.Chunker.Chunk(parsed.Blocks, document.Title);
        var now = timeProvider.GetUtcNow();

        if (chunks.Count == 0)
        {
            document.MarkIndexed(0, now);
            await services.DbContext.SaveChangesAsync(cancellationToken);
            logger.LogWarning("Document {DocumentId} produced no chunks.", documentId);

            return;
        }

        var entities = await BuildChunkEntitiesAsync(services, document, chunks, now, cancellationToken);
        await ReplaceChunksAsync(services.DbContext, document, entities, now, cancellationToken);

        logger.LogInformation(
            "Indexed document {DocumentId} into {ChunkCount} chunks using {EmbeddingModel}.",
            documentId,
            entities.Count,
            services.EmbeddingService.QualifiedModel);
    }

    /// <summary>
    /// Returns null when the format is unsupported — by then the Failed status has already been written
    /// and there is nothing left to do with this document.
    /// </summary>
    private static async Task<ParsedDocument?> ParseAsync(
        IngestionServices services,
        Document document,
        CancellationToken cancellationToken)
    {
        // Re-check the magic bytes here instead of trusting the stored ContentType: the file on disk is the truth.
        var header = new byte[IDocumentParserResolver.MagicHeaderLength];

        await using (var probeStream = await services.FileStorage.OpenReadAsync(document.StoragePath, cancellationToken))
        {
            _ = await probeStream.ReadAsync(header, cancellationToken);
        }

        var parserResult = services.ParserResolver.Resolve(document.FileName, document.ContentType, header);

        if (parserResult.IsFailure)
        {
            document.MarkFailed(parserResult.Error.Message);
            await services.DbContext.SaveChangesAsync(cancellationToken);

            return null;
        }

        await using var contentStream = await services.FileStorage.OpenReadAsync(document.StoragePath, cancellationToken);

        return await parserResult.Value.ParseAsync(contentStream, cancellationToken);
    }

    private static async Task<List<DocumentChunk>> BuildChunkEntitiesAsync(
        IngestionServices services,
        Document document,
        IReadOnlyList<TextChunk> chunks,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken)
    {
        var embeddings = await services.EmbeddingService.EmbedAsync(
            chunks.Select(chunk => chunk.EmbeddedText).ToList(),
            cancellationToken);

        return chunks
            .Select((chunk, index) => DocumentChunk.Create(
                document.Id,
                chunk.Index,
                chunk.Content,
                chunk.HeadingPath,
                chunk.EmbeddedText,
                chunk.TokenCount,
                embeddings[index],
                services.EmbeddingService.QualifiedModel,
                services.EmbeddingService.Dimensions,
                JsonSerializer.Serialize(chunk.Metadata),
                createdAt))
            .ToList();
    }

    /// <summary>
    /// Deletes the old chunks and writes the new ones in the SAME transaction: a half-finished reindex is worse
    /// than none, because the document would lose part of its content while still being marked Indexed.
    /// </summary>
    private static async Task ReplaceChunksAsync(
        IChatDbContext dbContext,
        Document document,
        List<DocumentChunk> entities,
        DateTimeOffset indexedAt,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        await dbContext.DocumentChunks
            .Where(chunk => chunk.DocumentId == document.Id)
            .ExecuteDeleteAsync(cancellationToken);

        dbContext.DocumentChunks.AddRange(entities);
        document.MarkIndexed(entities.Count, indexedAt);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task TryMarkFailedAsync(Guid documentId, string errorMessage, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IChatDbContext>();
            var document = await dbContext.Documents.SingleOrDefaultAsync(item => item.Id == documentId, cancellationToken);

            if (document is not null)
            {
                document.MarkFailed(errorMessage);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not persist the Failed status for document {DocumentId}.", documentId);
        }
    }

    /// <summary>
    /// The scoped services one document needs, resolved once at the top of the scope. They are grouped here so
    /// the rest of the worker never has to see an IServiceProvider again — each ingestion step's dependencies
    /// are readable straight off its method signature.
    /// </summary>
    private sealed record IngestionServices(
        IChatDbContext DbContext,
        IFileStorage FileStorage,
        IDocumentParserResolver ParserResolver,
        IStructuredChunker Chunker,
        BatchingEmbeddingService EmbeddingService)
    {
        public static IngestionServices From(IServiceProvider services)
        {
            return new IngestionServices(
                services.GetRequiredService<IChatDbContext>(),
                services.GetRequiredService<IFileStorage>(),
                services.GetRequiredService<IDocumentParserResolver>(),
                services.GetRequiredService<IStructuredChunker>(),
                services.GetRequiredService<BatchingEmbeddingService>());
        }
    }
}
