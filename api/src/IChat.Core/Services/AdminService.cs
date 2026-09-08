namespace IChat.Core.Services;

using IChat.Core.Abstractions;
using IChat.Core.Common;
using IChat.Core.Contracts.Admin;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Đường thoát bắt buộc khi đổi model embedding: đặt toàn bộ document về Pending
/// và đẩy lại vào ingestion queue. Không có bước này thì vector cũ và mới nằm ở hai
/// không gian khác nhau và search sai một cách im lặng.
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
