namespace IChat.Core.Rag;

using System.ComponentModel.DataAnnotations;

public sealed class ChunkingOptions
{
    [Range(50, 8000)]
    public int TargetTokens { get; set; } = 800;

    [Range(0, 2000)]
    public int OverlapTokens { get; set; } = 120;

    [Range(0, 2000)]
    public int MinTokens { get; set; } = 80;

    public bool PrependHeadingPath { get; set; } = true;
}
