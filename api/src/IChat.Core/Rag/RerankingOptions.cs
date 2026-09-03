namespace IChat.Core.Rag;

using System.ComponentModel.DataAnnotations;

public sealed class RerankingOptions
{
    public RerankMode Mode { get; set; } = RerankMode.None;

    [Range(1, 200)]
    public int InputTopK { get; set; } = 20;

    [Range(1, 200)]
    public int OutputTopK { get; set; } = 8;
}
