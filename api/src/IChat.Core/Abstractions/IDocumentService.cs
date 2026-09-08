namespace IChat.Core.Abstractions;

using IChat.Core.Common;
using IChat.Core.Contracts.Documents;

public interface IDocumentService
{
    Task<Result<UploadDocumentResult>> UploadAsync(UploadDocumentRequest request, CancellationToken cancellationToken);

    Task<Result<PaginatedList<DocumentSummary>>> GetListAsync(GetDocumentsRequest request, CancellationToken cancellationToken);

    Task<Result<DocumentSummary>> GetByIdAsync(Guid documentId, CancellationToken cancellationToken);

    Task<Result<PaginatedList<ChunkView>>> GetChunksAsync(GetDocumentChunksRequest request, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(Guid documentId, CancellationToken cancellationToken);
}
