namespace IChat.Core.Rag;

/// <summary>
/// mmr = λ · relevance − (1−λ) · max_cosine(candidate, đã_chọn).
/// Chống lại việc top-K toàn là các mảnh chồng lấn của cùng một trang.
/// </summary>
public static class MaximalMarginalRelevance
{
    public static IReadOnlyList<ScoredChunk> Select(IReadOnlyList<ScoredChunk> candidates, double lambda, int maxChunksPerDocument, int finalTopK)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxChunksPerDocument, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(finalTopK, 1);

        if (candidates.Count == 0)
        {
            return [];
        }

        var remaining = candidates.OrderByDescending(candidate => candidate.Score).ToList();
        var selected = new List<ScoredChunk>(Math.Min(finalTopK, remaining.Count));
        var perDocument = new Dictionary<Guid, int>();

        while (selected.Count < finalTopK && remaining.Count > 0)
        {
            var bestIndex = -1;
            var bestScore = double.NegativeInfinity;

            for (var i = 0; i < remaining.Count; i++)
            {
                var candidate = remaining[i];

                if (perDocument.GetValueOrDefault(candidate.DocumentId) >= maxChunksPerDocument)
                {
                    continue;
                }

                var redundancy = 0.0;
                foreach (var chosen in selected)
                {
                    redundancy = Math.Max(redundancy, CosineSimilarity(candidate.Embedding, chosen.Embedding));
                }

                var mmr = (lambda * candidate.Score) - ((1 - lambda) * redundancy);

                if (mmr > bestScore)
                {
                    bestScore = mmr;
                    bestIndex = i;
                }
            }

            if (bestIndex < 0)
            {
                break;
            }

            var picked = remaining[bestIndex];
            remaining.RemoveAt(bestIndex);
            selected.Add(picked);
            perDocument[picked.DocumentId] = perDocument.GetValueOrDefault(picked.DocumentId) + 1;
        }

        return selected;
    }

    public static double CosineSimilarity(float[]? left, float[]? right)
    {
        if (left is null || right is null || left.Length == 0 || left.Length != right.Length)
        {
            return 0.0;
        }

        double dot = 0, leftNorm = 0, rightNorm = 0;
        for (var i = 0; i < left.Length; i++)
        {
            dot += (double)left[i] * right[i];
            leftNorm += (double)left[i] * left[i];
            rightNorm += (double)right[i] * right[i];
        }

        if (leftNorm <= 0 || rightNorm <= 0)
        {
            return 0.0;
        }

        return dot / (Math.Sqrt(leftNorm) * Math.Sqrt(rightNorm));
    }
}
