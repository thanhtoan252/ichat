namespace IChat.Infrastructure.Search;

using System.Diagnostics;
using IChat.Core.Abstractions;
using IChat.Core.Rag;
using IChat.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using Pgvector;

/// <summary>
/// Toàn bộ SQL của tầng chunk: ba nhánh tìm kiếm (vector, full-text, trigram) và hai
/// đường nạp nội dung. Không tách thành hai class vì OpenConnectionAsync và
/// ReadCandidatesAsync dùng chung — tách chỉ đẻ ra trùng lặp mới.
/// </summary>
public sealed class PostgresChunkStore(IDbContextFactory<IChatDbContext> dbContextFactory) : IChunkSearch, IChunkLoader
{
    public async Task<SearchBranchResult> SearchVectorAsync(float[] queryEmbedding, int limit, double minSimilarity, CancellationToken cancellationToken)
    {
        if (limit <= 0)
        {
            return new SearchBranchResult { Candidates = [], ElapsedMs = 0 };
        }

        var stopwatch = Stopwatch.StartNew();

        // Mỗi nhánh một DbContext riêng vì DbContext không thread-safe và ba nhánh
        // chạy song song bằng Task.WhenAll.
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var connection = await OpenConnectionAsync(dbContext, cancellationToken);

        // Tăng recall của HNSW cho phiên truy vấn này.
        await using (var tuning = connection.CreateCommand())
        {
            tuning.CommandText = "SET hnsw.ef_search = 100;";
            await tuning.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT c.id, c.document_id, c.content, c.heading_path, c.chunk_index,
                   1 - (c.embedding <=> @queryEmbedding) AS score
            FROM document_chunks c
            WHERE 1 - (c.embedding <=> @queryEmbedding) >= @minSimilarity
            ORDER BY c.embedding <=> @queryEmbedding
            LIMIT @limit;
            """;

        command.Parameters.Add(new NpgsqlParameter("queryEmbedding", new Vector(queryEmbedding)));
        command.Parameters.AddWithValue("minSimilarity", minSimilarity);
        command.Parameters.AddWithValue("limit", limit);

        var candidates = await ReadCandidatesAsync(command, RetrievalSource.Vector, cancellationToken);

        return new SearchBranchResult { Candidates = candidates, ElapsedMs = stopwatch.ElapsedMilliseconds };
    }

    public async Task<SearchBranchResult> SearchFullTextAsync(string query, int limit, double minRank, CancellationToken cancellationToken)
    {
        var tsQuery = TsQueryBuilder.Build(query);

        if (limit <= 0 || tsQuery is null)
        {
            // Không còn lexeme nào sau khi lọc: trả rỗng chứ đừng ép ''::tsquery.
            return new SearchBranchResult { Candidates = [], ElapsedMs = 0, TsQuery = tsQuery };
        }

        var stopwatch = Stopwatch.StartNew();

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var connection = await OpenConnectionAsync(dbContext, cancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT c.id, c.document_id, c.content, c.heading_path, c.chunk_index,
                   ts_rank_cd(c.content_tsv, q.tsq) AS score
            FROM document_chunks c,
                 (SELECT CAST(@tsQuery AS tsquery) AS tsq) q
            WHERE c.content_tsv @@ q.tsq
              AND ts_rank_cd(c.content_tsv, q.tsq) >= @minRank
            ORDER BY score DESC
            LIMIT @limit;
            """;

        command.Parameters.AddWithValue("tsQuery", tsQuery);
        command.Parameters.AddWithValue("minRank", minRank);
        command.Parameters.AddWithValue("limit", limit);

        var candidates = await ReadCandidatesAsync(command, RetrievalSource.FullText, cancellationToken);

        return new SearchBranchResult
        {
            Candidates = candidates,
            ElapsedMs = stopwatch.ElapsedMilliseconds,
            TsQuery = tsQuery
        };
    }

    public async Task<SearchBranchResult> SearchTrigramAsync(string query, int limit, CancellationToken cancellationToken)
    {
        if (limit <= 0 || string.IsNullOrWhiteSpace(query))
        {
            return new SearchBranchResult { Candidates = [], ElapsedMs = 0 };
        }

        var stopwatch = Stopwatch.StartNew();

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var connection = await OpenConnectionAsync(dbContext, cancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT c.id, c.document_id, c.content, c.heading_path, c.chunk_index,
                   similarity(c.content, @query) AS score
            FROM document_chunks c
            WHERE c.content % @query
            ORDER BY score DESC
            LIMIT @limit;
            """;

        command.Parameters.AddWithValue("query", query);
        command.Parameters.AddWithValue("limit", limit);

        var candidates = await ReadCandidatesAsync(command, RetrievalSource.Trigram, cancellationToken);

        return new SearchBranchResult { Candidates = candidates, ElapsedMs = stopwatch.ElapsedMilliseconds };
    }

    public async Task<IReadOnlyList<ScoredChunk>> LoadChunksAsync(IReadOnlyList<Guid> chunkIds, CancellationToken cancellationToken)
    {
        if (chunkIds.Count == 0)
        {
            return [];
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var rows = await dbContext.DocumentChunks
            .AsNoTracking()
            .Where(chunk => chunkIds.Contains(chunk.Id))
            .Select(chunk => new
            {
                chunk.Id,
                chunk.DocumentId,
                DocumentTitle = chunk.Document!.Title,
                chunk.Content,
                chunk.HeadingPath,
                chunk.ChunkIndex,
                chunk.Embedding
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new ScoredChunk
            {
                ChunkId = row.Id,
                DocumentId = row.DocumentId,
                DocumentTitle = row.DocumentTitle,
                Content = row.Content,
                HeadingPath = row.HeadingPath,
                ChunkIndex = row.ChunkIndex,
                Score = 0,
                Embedding = row.Embedding
            })
            .ToList();
    }

    /// <summary>
    /// MỘT truy vấn gộp cho toàn bộ tập cặp (document_id, chunk_index).
    /// Lặp N+1 query ở đây là lỗi hiệu năng kinh điển của bước mở rộng lân cận.
    /// </summary>
    public async Task<IReadOnlyList<NeighborChunk>> LoadNeighborsAsync(
        IReadOnlyList<(Guid DocumentId, int ChunkIndex)> keys,
        CancellationToken cancellationToken)
    {
        if (keys.Count == 0)
        {
            return [];
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var connection = await OpenConnectionAsync(dbContext, cancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT c.document_id, c.chunk_index, c.content
            FROM document_chunks c
            JOIN unnest(@documentIds, @chunkIndexes) AS wanted(document_id, chunk_index)
              ON c.document_id = wanted.document_id AND c.chunk_index = wanted.chunk_index;
            """;

        command.Parameters.Add(new NpgsqlParameter("documentIds", NpgsqlDbType.Array | NpgsqlDbType.Uuid)
        {
            Value = keys.Select(key => key.DocumentId).ToArray()
        });

        command.Parameters.Add(new NpgsqlParameter("chunkIndexes", NpgsqlDbType.Array | NpgsqlDbType.Integer)
        {
            Value = keys.Select(key => key.ChunkIndex).ToArray()
        });

        var neighbors = new List<NeighborChunk>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            neighbors.Add(new NeighborChunk
            {
                DocumentId = reader.GetGuid(0),
                ChunkIndex = reader.GetInt32(1),
                Content = reader.GetString(2)
            });
        }

        return neighbors;
    }

    private static async Task<NpgsqlConnection> OpenConnectionAsync(IChatDbContext dbContext, CancellationToken cancellationToken)
    {
        var connection = (NpgsqlConnection)dbContext.Database.GetDbConnection();

        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        return connection;
    }

    private static async Task<List<RetrievalCandidate>> ReadCandidatesAsync(
        NpgsqlCommand command,
        RetrievalSource source,
        CancellationToken cancellationToken)
    {
        var candidates = new List<RetrievalCandidate>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            candidates.Add(new RetrievalCandidate
            {
                ChunkId = reader.GetGuid(0),
                DocumentId = reader.GetGuid(1),
                Content = reader.GetString(2),
                HeadingPath = reader.IsDBNull(3) ? null : reader.GetString(3),
                ChunkIndex = reader.GetInt32(4),
                Score = reader.GetDouble(5),
                Source = source
            });
        }

        return candidates;
    }
}
