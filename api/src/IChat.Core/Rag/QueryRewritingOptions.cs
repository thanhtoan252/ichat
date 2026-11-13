namespace IChat.Core.Rag;

using System.ComponentModel.DataAnnotations;

public sealed class QueryRewritingOptions
{
    public bool Enabled { get; set; } = true;

    [Range(0, 20)]
    public int HistoryTurns { get; set; } = 3;

    public bool SkipOnFirstTurn { get; set; } = true;

    [Range(16, 2048)]
    public int MaxOutputTokens { get; set; } = 128;
}
