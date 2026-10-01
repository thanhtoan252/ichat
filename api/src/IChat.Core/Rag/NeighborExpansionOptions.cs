namespace IChat.Core.Rag;

using System.ComponentModel.DataAnnotations;

public sealed class NeighborExpansionOptions
{
    public bool Enabled { get; set; } = true;

    [Range(0, 10)]
    public int Before { get; set; } = 1;

    [Range(0, 10)]
    public int After { get; set; } = 1;
}
