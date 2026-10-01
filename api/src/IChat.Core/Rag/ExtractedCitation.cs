namespace IChat.Core.Rag;

public sealed class ExtractedCitation
{
    public required int MarkerIndex { get; init; }

    public required int SourceOrdinal { get; init; }
}
