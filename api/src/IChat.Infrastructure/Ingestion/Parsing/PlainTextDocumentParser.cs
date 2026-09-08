namespace IChat.Infrastructure.Ingestion.Parsing;

using IChat.Core.Abstractions;
using IChat.Core.Domain.Documents.Parsing;

/// <summary>TXT không có cấu trúc: toàn bộ là Paragraph, không có heading.</summary>
public sealed class PlainTextDocumentParser : IDocumentParser
{
    public const string PlainTextContentType = "text/plain";

    public DocumentFormat Format { get; } = new()
    {
        ContentType = PlainTextContentType,
        Extensions = [".txt"]
    };

    public async Task<ParsedDocument> ParseAsync(Stream content, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(content, leaveOpen: true);
        var text = await reader.ReadToEndAsync(cancellationToken);

        var blocks = new List<DocumentBlock>();
        var order = 0;

        foreach (var paragraph in text.Split(["\r\n\r\n", "\n\n"], StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = paragraph.Trim();

            if (trimmed.Length == 0)
            {
                continue;
            }

            blocks.Add(new DocumentBlock
            {
                Kind = BlockKind.Paragraph,
                Text = trimmed,
                Order = order++
            });
        }

        return new ParsedDocument
        {
            Blocks = blocks,
            Metadata = new Dictionary<string, string> { ["format"] = "txt" }
        };
    }
}
