namespace IChat.Core.Services;

using System.Linq.Expressions;
using IChat.Core.Abstractions;
using IChat.Core.Common;
using IChat.Core.Contracts.Documents;
using IChat.Core.Domain.Documents;
using Microsoft.EntityFrameworkCore;

public sealed class DocumentService(
    IApplicationDbContext dbContext,
    IFileStorage fileStorage,
    IDocumentParserResolver parserResolver,
    IDocumentIngestionQueue ingestionQueue,
    TimeProvider timeProvider) : IDocumentService
{
    /// <summary>
    /// A single definition of the DocumentSummary shape. It is an Expression rather than a method because EF
    /// has to translate it into a SELECT of only the needed columns — as an ordinary method the query would
    /// load whole entities and map them in memory.
    /// </summary>
    private static readonly Expression<Func<Document, DocumentSummary>> ToSummary = document => new DocumentSummary
    {
        Id = document.Id,
        Title = document.Title,
        FileName = document.FileName,
        ContentType = document.ContentType,
        SizeInBytes = document.SizeInBytes,
        Status = document.Status.ToString(),
        ErrorMessage = document.ErrorMessage,
        ChunkCount = document.ChunkCount,
        CreatedAt = document.CreatedAt,
        IndexedAt = document.IndexedAt
    };

    public async Task<Result<UploadDocumentResult>> UploadAsync(UploadDocumentRequest request, CancellationToken cancellationToken)
    {
        // Read the first few bytes to check the magic bytes: the content type the client declared is not trustworthy.
        var header = new byte[IDocumentParserResolver.MagicHeaderLength];
        var headerLength = await ReadHeaderAsync(request.Content, header, cancellationToken);

        var contentTypeResult = parserResolver.ResolveContentType(
            request.FileName,
            request.DeclaredContentType,
            header.AsSpan(0, headerLength));

        if (contentTypeResult.IsFailure)
        {
            return Result.Failure<UploadDocumentResult>(contentTypeResult.Error);
        }

        var storagePath = await fileStorage.SaveAsync(request.Content, request.FileName, cancellationToken);

        var title = string.IsNullOrWhiteSpace(request.Title)
            ? Path.GetFileNameWithoutExtension(request.FileName)
            : request.Title.Trim();

        var document = Document.Create(
            title,
            request.FileName,
            contentTypeResult.Value,
            request.SizeInBytes,
            storagePath,
            timeProvider.GetUtcNow());

        dbContext.Documents.Add(document);
        await dbContext.SaveChangesAsync(cancellationToken);

        await ingestionQueue.EnqueueAsync(document.Id, cancellationToken);

        return Result.Success(new UploadDocumentResult
        {
            Id = document.Id,
            Title = document.Title,
            Status = document.Status.ToString()
        });
    }

    public async Task<Result<PaginatedList<DocumentSummary>>> GetListAsync(GetDocumentsRequest request, CancellationToken cancellationToken)
    {
        var source = dbContext.Documents.AsNoTracking();

        if (request.Status is { } status)
        {
            source = source.Where(document => document.Status == status);
        }

        var totalCount = await source.CountAsync(cancellationToken);

        var items = await source
            .OrderByDescending(document => document.CreatedAt)
            .Skip(request.Offset)
            .Take(request.Limit)
            .Select(ToSummary)
            .ToListAsync(cancellationToken);

        return Result.Success(new PaginatedList<DocumentSummary>
        {
            Items = items,
            Offset = request.Offset,
            Limit = request.Limit,
            TotalCount = totalCount
        });
    }

    public async Task<Result<DocumentSummary>> GetByIdAsync(Guid documentId, CancellationToken cancellationToken)
    {
        var document = await dbContext.Documents
            .AsNoTracking()
            .Where(item => item.Id == documentId)
            .Select(ToSummary)
            .SingleOrDefaultAsync(cancellationToken);

        return document is null
            ? Result.Failure<DocumentSummary>(Error.NotFound("Document", documentId))
            : Result.Success(document);
    }

    public async Task<Result<PaginatedList<ChunkView>>> GetChunksAsync(GetDocumentChunksRequest request, CancellationToken cancellationToken)
    {
        var documentExists = await dbContext.Documents.AnyAsync(item => item.Id == request.DocumentId, cancellationToken);

        if (!documentExists)
        {
            return Result.Failure<PaginatedList<ChunkView>>(Error.NotFound("Document", request.DocumentId));
        }

        var source = dbContext.DocumentChunks.AsNoTracking().Where(chunk => chunk.DocumentId == request.DocumentId);
        var totalCount = await source.CountAsync(cancellationToken);

        var items = await source
            .OrderBy(chunk => chunk.ChunkIndex)
            .Skip(request.Offset)
            .Take(request.Limit)
            .Select(chunk => new ChunkView
            {
                Id = chunk.Id,
                ChunkIndex = chunk.ChunkIndex,
                Content = chunk.Content,
                HeadingPath = chunk.HeadingPath,
                EmbeddedText = chunk.EmbeddedText,
                TokenCount = chunk.TokenCount,
                EmbeddingModel = chunk.EmbeddingModel,
                EmbeddingDimensions = chunk.EmbeddingDimensions,
                Metadata = chunk.Metadata
            })
            .ToListAsync(cancellationToken);

        return Result.Success(new PaginatedList<ChunkView>
        {
            Items = items,
            Offset = request.Offset,
            Limit = request.Limit,
            TotalCount = totalCount
        });
    }

    public async Task<Result> DeleteAsync(Guid documentId, CancellationToken cancellationToken)
    {
        var document = await dbContext.Documents.SingleOrDefaultAsync(item => item.Id == documentId, cancellationToken);

        if (document is null)
        {
            return Result.Failure(Error.NotFound("Document", documentId));
        }

        var storagePath = document.StoragePath;

        // Chunks and citations cascade at the database level, per the FK configuration.
        dbContext.Documents.Remove(document);
        await dbContext.SaveChangesAsync(cancellationToken);
        await fileStorage.DeleteAsync(storagePath, cancellationToken);

        return Result.Success();
    }

    private static async Task<int> ReadHeaderAsync(Stream content, byte[] header, CancellationToken cancellationToken)
    {
        if (!content.CanSeek)
        {
            return 0;
        }

        var read = await content.ReadAsync(header, cancellationToken);
        content.Position = 0;

        return read;
    }
}
