namespace IChat.Core.Domain.Documents.Parsing;

public sealed class DocumentBlock
{
    public required BlockKind Kind { get; init; }

    public required string Text { get; init; }

    public int? HeadingLevel { get; init; }

    public int? ListLevel { get; init; }

    public required int Order { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }

    public bool IsHeading => Kind == BlockKind.Heading && HeadingLevel is > 0;
}
