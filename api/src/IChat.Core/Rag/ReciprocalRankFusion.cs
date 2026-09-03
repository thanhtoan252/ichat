namespace IChat.Core.Rag;

/// <summary>
/// RRF là FUSION, không phải rerank: nó trộn nhiều danh sách đã xếp hạng thành một,
/// chỉ dựa trên thứ hạng chứ không chấm lại độ liên quan.
/// </summary>
public static class ReciprocalRankFusion
{
    public static IReadOnlyList<(Guid Id, double Score)> Fuse(IReadOnlyList<IReadOnlyList<Guid>> rankedLists, int k, int topK)
    {
        ArgumentNullException.ThrowIfNull(rankedLists);
        ArgumentOutOfRangeException.ThrowIfLessThan(k, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(topK, 1);

        var scores = new Dictionary<Guid, double>();
        var firstSeen = new Dictionary<Guid, int>();
        var order = 0;

        foreach (var list in rankedLists)
        {
            if (list is null)
            {
                continue;
            }

            for (var rank = 0; rank < list.Count; rank++)
            {
                var id = list[rank];

                // Chỉ cộng qua các danh sách mà chunk có mặt — không phạt chunk vắng mặt.
                scores[id] = scores.GetValueOrDefault(id) + 1.0 / (k + rank + 1);

                if (!firstSeen.ContainsKey(id))
                {
                    firstSeen[id] = order++;
                }
            }
        }

        return scores
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => firstSeen[pair.Key])
            .Take(topK)
            .Select(pair => (pair.Key, pair.Value))
            .ToList();
    }
}
