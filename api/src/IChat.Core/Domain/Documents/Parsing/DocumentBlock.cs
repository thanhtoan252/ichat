namespace IChat.Core.Domain.Documents.Parsing;

/// <summary>
/// The common block model for EVERY format. The chunker only knows this model,
/// never whether the original document was docx, pdf, markdown or txt.
/// </summary>
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
