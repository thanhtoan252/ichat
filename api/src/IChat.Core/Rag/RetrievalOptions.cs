namespace IChat.Core.Rag;

using System.ComponentModel.DataAnnotations;

public sealed class RetrievalOptions
{
    [Range(0, 500)]
    public int VectorCandidates { get; set; } = 40;

    [Range(0, 500)]
    public int FullTextCandidates { get; set; } = 40;

    /// <summary>0 = tắt nhánh trigram.</summary>
    [Range(0, 500)]
    public int TrigramCandidates { get; set; }

    [Range(0d, 1d)]
    public double VectorMinSimilarity { get; set; } = 0.20;

    [Range(0d, 1d)]
    public double FullTextMinRank { get; set; } = 0.01;

    // RrfK = 60 là hằng số trong bài báo gốc về Reciprocal Rank Fusion (Cormack 2009).
    // Giá trị lớn làm phẳng ảnh hưởng của thứ hạng cao, giá trị nhỏ khuếch đại top-1.
    [Range(1, 1000)]
    public int RrfK { get; set; } = 60;

    [Range(1, 200)]
    public int FusedTopK { get; set; } = 20;
}
