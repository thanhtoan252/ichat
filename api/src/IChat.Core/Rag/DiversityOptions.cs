namespace IChat.Core.Rag;

using System.ComponentModel.DataAnnotations;

public sealed class DiversityOptions
{
    [Range(0d, 1d)]
    public double MmrLambda { get; set; } = 0.7;

    [Range(1, 100)]
    public int MaxChunksPerDocument { get; set; } = 3;

    [Range(1, 100)]
    public int FinalTopK { get; set; } = 8;
}
