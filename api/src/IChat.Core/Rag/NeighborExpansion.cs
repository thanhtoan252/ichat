namespace IChat.Core.Rag;

public static class NeighborExpansion
{
    /// <summary>Các cặp (documentId, chunkIndex) cần nạp thêm — dùng cho MỘT truy vấn gộp, không N+1.</summary>
    public static IReadOnlyList<(Guid DocumentId, int ChunkIndex)> PlanNeighborKeys(IReadOnlyList<ScoredChunk> selected, int before, int after)
    {
        ArgumentNullException.ThrowIfNull(selected);

        var wanted = new HashSet<(Guid, int)>();
        var anchors = selected.Select(chunk => (chunk.DocumentId, chunk.ChunkIndex)).ToHashSet();

        foreach (var chunk in selected)
        {
            for (var offset = -before; offset <= after; offset++)
            {
                var index = chunk.ChunkIndex + offset;
                if (offset == 0 || index < 0)
                {
                    continue;
                }

                var key = (chunk.DocumentId, index);
                if (!anchors.Contains(key))
                {
                    wanted.Add(key);
                }
            }
        }

        return wanted.OrderBy(key => key.Item1).ThenBy(key => key.Item2).ToList();
    }

    public static IReadOnlyList<ExpandedContext> Expand(
        IReadOnlyList<ScoredChunk> selected,
        IReadOnlyList<NeighborChunk> neighbors,
        int before,
        int after)
    {
        ArgumentNullException.ThrowIfNull(selected);
        ArgumentNullException.ThrowIfNull(neighbors);

        if (selected.Count == 0)
        {
            return [];
        }

        var contentByKey = new Dictionary<(Guid, int), string>();
        foreach (var neighbor in neighbors)
        {
            contentByKey[(neighbor.DocumentId, neighbor.ChunkIndex)] = neighbor.Content;
        }

        foreach (var chunk in selected)
        {
            contentByKey[(chunk.DocumentId, chunk.ChunkIndex)] = chunk.Content;
        }

        var groups = new List<Group>();

        foreach (var documentGroup in selected.GroupBy(chunk => chunk.DocumentId))
        {
            var spans = documentGroup
                .Select(chunk => new Span
                {
                    Start = Math.Max(0, chunk.ChunkIndex - before),
                    End = chunk.ChunkIndex + after,
                    Anchor = chunk
                })
                .OrderBy(span => span.Start)
                .ThenBy(span => span.End)
                .ToList();

            var current = new Group
            {
                DocumentId = documentGroup.Key,
                Start = spans[0].Start,
                End = spans[0].End,
                Anchors = [spans[0].Anchor]
            };

            for (var i = 1; i < spans.Count; i++)
            {
                var span = spans[i];

                // Gộp khi hai khoảng chạm hoặc chồng nhau, để không đưa cùng đoạn văn vào context hai lần.
                if (span.Start <= current.End + 1)
                {
                    current = new Group
                    {
                        DocumentId = current.DocumentId,
                        Start = current.Start,
                        End = Math.Max(current.End, span.End),
                        Anchors = [.. current.Anchors, span.Anchor]
                    };
                }
                else
                {
                    groups.Add(current);
                    current = new Group
                    {
                        DocumentId = documentGroup.Key,
                        Start = span.Start,
                        End = span.End,
                        Anchors = [span.Anchor]
                    };
                }
            }

            groups.Add(current);
        }

        var results = new List<ExpandedContext>(groups.Count);

        foreach (var group in groups)
        {
            var parts = new List<string>();
            int? actualStart = null;
            var actualEnd = group.Start;

            for (var index = group.Start; index <= group.End; index++)
            {
                if (!contentByKey.TryGetValue((group.DocumentId, index), out var content) || string.IsNullOrWhiteSpace(content))
                {
                    continue;
                }

                actualStart ??= index;
                actualEnd = index;
                parts.Add(content);
            }

            if (parts.Count == 0)
            {
                continue;
            }

            var best = group.Anchors.OrderByDescending(anchor => anchor.Score).First();

            results.Add(new ExpandedContext
            {
                AnchorChunkIds = group.Anchors.OrderByDescending(anchor => anchor.Score).Select(anchor => anchor.ChunkId).ToList(),
                DocumentId = group.DocumentId,
                DocumentTitle = best.DocumentTitle,
                HeadingPath = best.HeadingPath,
                Text = JoinTrimmingOverlap(parts),
                Score = group.Anchors.Max(anchor => anchor.Score),
                StartChunkIndex = actualStart ?? group.Start,
                EndChunkIndex = actualEnd
            });
        }

        return results.OrderByDescending(result => result.Score).ToList();
    }

    /// <summary>
    /// Chunk kề nhau chồng lấn OverlapTokens, nên phần đuôi của khối trước trùng phần đầu khối sau.
    /// Cắt phần trùng để không đưa cùng một đoạn văn vào context hai lần.
    /// </summary>
    public static string JoinTrimmingOverlap(IReadOnlyList<string> parts)
    {
        ArgumentNullException.ThrowIfNull(parts);

        if (parts.Count == 0)
        {
            return string.Empty;
        }

        var builder = new System.Text.StringBuilder(parts[0].Trim());

        for (var i = 1; i < parts.Count; i++)
        {
            var next = parts[i].Trim();
            if (next.Length == 0)
            {
                continue;
            }

            var previous = builder.ToString();
            var overlap = LongestSuffixPrefixOverlap(previous, next);
            builder.Append("\n\n").Append(next.AsSpan(overlap));
        }

        return builder.ToString().Trim();
    }

    private const int MaxOverlapScan = 4000;

    private static int LongestSuffixPrefixOverlap(string previous, string next)
    {
        var max = Math.Min(Math.Min(previous.Length, next.Length), MaxOverlapScan);

        for (var length = max; length >= 24; length--)
        {
            if (string.CompareOrdinal(previous, previous.Length - length, next, 0, length) == 0)
            {
                return length;
            }
        }

        return 0;
    }

    private readonly struct Span
    {
        public required int Start { get; init; }

        public required int End { get; init; }

        public required ScoredChunk Anchor { get; init; }
    }

    private sealed class Group
    {
        public required Guid DocumentId { get; init; }

        public required int Start { get; init; }

        public required int End { get; init; }

        public required IReadOnlyList<ScoredChunk> Anchors { get; init; }
    }
}
