namespace IChat.Core.Rag;

using System.ComponentModel.DataAnnotations;

public sealed class ContextOptions
{
    [Range(256, 200_000)]
    public int MaxTokens { get; set; } = 6000;

    [Range(0, 100)]
    public int HistoryTurns { get; set; } = 6;
}
