namespace IChat.Core.Domain.Documents.Parsing;

public sealed class ParsedDocument
{
    public required IReadOnlyList<DocumentBlock> Blocks { get; init; }

    public required IReadOnlyDictionary<string, string> Metadata { get; init; }

    public static ParsedDocument Empty { get; } = new()
    {
        Blocks = [],
        Metadata = new Dictionary<string, string>()
    };
}
