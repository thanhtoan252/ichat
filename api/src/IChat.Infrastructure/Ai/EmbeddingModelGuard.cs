namespace IChat.Infrastructure.Ai;

using IChat.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Đổi model embedding rồi restart mà không reindex là bug tệ nhất của một hệ RAG:
/// vector cũ và mới nằm ở hai không gian khác nhau, search vẫn trả kết quả nhưng sai
/// hoàn toàn, và không có lỗi nào báo. Guard này biến lỗi im lặng đó thành lỗi ồn ào.
/// </summary>
public sealed class EmbeddingModelGuard(
    IServiceScopeFactory scopeFactory,
    IOptions<AiOptions> options,
    ILogger<EmbeddingModelGuard> logger) : IHostedService
{
    private readonly EmbeddingOptions _embedding = options.Value.Embedding;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IChatDbContext>();

        if (!await dbContext.Database.CanConnectAsync(cancellationToken))
        {
            logger.LogWarning("EmbeddingModelGuard skipped its check: could not reach the database.");

            return;
        }

        var existing = await dbContext.DocumentChunks
            .AsNoTracking()
            .Select(chunk => new { chunk.EmbeddingModel, chunk.EmbeddingDimensions })
            .Distinct()
            .ToListAsync(cancellationToken);

        if (existing.Count == 0)
        {
            return;
        }

        var mismatchedDimensions = existing
            .Where(row => row.EmbeddingDimensions != _embedding.Dimensions)
            .ToList();

        if (mismatchedDimensions.Count > 0)
        {
            var detail = string.Join(", ", mismatchedDimensions.Select(row => $"{row.EmbeddingModel}={row.EmbeddingDimensions}"));

            // Lệch số chiều: dữ liệu không dùng được, chặn khởi động.
            throw new InvalidOperationException(
                $"Embedding dimension mismatch. Config says {_embedding.Dimensions} but the database holds: {detail}. " +
                "Run POST /api/v1/admin/reindex after migrating the column, or revert the config to the previous dimension count.");
        }

        var mismatchedModels = existing
            .Where(row => !string.Equals(row.EmbeddingModel, _embedding.Qualified, StringComparison.OrdinalIgnoreCase))
            .Select(row => row.EmbeddingModel)
            .ToList();

        if (mismatchedModels.Count > 0)
        {
            // Cùng số chiều nhưng khác model: vector vẫn insert được nhưng nằm khác
            // không gian ngữ nghĩa. Cảnh báo thật to thay vì chặn khởi động.
            logger.LogWarning(
                "Embedding model mismatch: config says '{Configured}' but the database still holds chunks from {Existing}. " +
                "Search results will be silently wrong until a reindex (POST /api/v1/admin/reindex).",
                _embedding.Qualified,
                string.Join(", ", mismatchedModels));
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
