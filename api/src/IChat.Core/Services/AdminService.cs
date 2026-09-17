namespace IChat.Core.Services;

using IChat.Core.Abstractions;
using IChat.Core.Common;
using IChat.Core.Contracts.Admin;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// The mandatory escape hatch when the embedding model changes: it puts every document back to Pending
/// and pushes them through the ingestion queue again. Without this step the old and the new vectors live
/// in different spaces and search goes wrong silently.
/// </summary>
public sealed class AdminService(IApplicationDbContext dbContext, IDocumentIngestionQueue ingestionQueue) : IAdminService
{
    public async Task<Result<ReindexResult>> ReindexAsync(CancellationToken cancellationToken)
    {
        var documents = await dbContext.Documents.ToListAsync(cancellationToken);

        foreach (var document in documents)
        {
            document.ResetForReindex();
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var document in documents)
        {
            await ingestionQueue.EnqueueAsync(document.Id, cancellationToken);
        }

        return Result.Success(new ReindexResult { DocumentCount = documents.Count });
    }
}
