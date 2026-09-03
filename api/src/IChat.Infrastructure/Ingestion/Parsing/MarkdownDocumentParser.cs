namespace IChat.Infrastructure.Ingestion.Parsing;

using IChat.Core.Abstractions;
using IChat.Core.Domain.Documents.Parsing;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;

public sealed class MarkdownDocumentParser : IDocumentParser
{
    public const string MarkdownContentType = "text/markdown";

    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    public DocumentFormat Format { get; } = new()
    {
        ContentType = MarkdownContentType,
        Extensions = [".md", ".markdown"]
    };

    public async Task<ParsedDocument> ParseAsync(Stream content, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(content, leaveOpen: true);
        var source = await reader.ReadToEndAsync(cancellationToken);

        var document = Markdown.Parse(source, Pipeline);
        var blocks = new List<DocumentBlock>();
        var order = 0;

        foreach (var block in document)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AppendBlock(block, source, blocks, ref order);
        }

        return new ParsedDocument
        {
            Blocks = blocks,
            Metadata = new Dictionary<string, string> { ["format"] = "markdown" }
        };
    }

    private static void AppendBlock(Block block, string source, List<DocumentBlock> blocks, ref int order)
    {
        switch (block)
        {
            case HeadingBlock heading:
            {
                var text = Slice(source, heading).TrimStart('#').Trim();

                if (text.Length > 0)
                {
                    blocks.Add(new DocumentBlock
                    {
                        Kind = BlockKind.Heading,
                        Text = text,
                        HeadingLevel = heading.Level,
                        Order = order++
                    });
                }

                break;
            }

            // Bảng markdown giữ nguyên nguyên khối, không bao giờ tách.
            case Table table:
            {
                var text = Slice(source, table).Trim();

                if (text.Length > 0)
                {
                    blocks.Add(new DocumentBlock
                    {
                        Kind = BlockKind.Table,
                        Text = text,
                        Order = order++,
                        Metadata = new Dictionary<string, string> { ["containsTable"] = "true" }
                    });
                }

                break;
            }

            case ListBlock list:
            {
                foreach (var item in list)
                {
                    var text = Slice(source, item).Trim();

                    if (text.Length > 0)
                    {
                        blocks.Add(new DocumentBlock
                        {
                            Kind = BlockKind.ListItem,
                            Text = text,
                            ListLevel = 0,
                            Order = order++
                        });
                    }
                }

                break;
            }

            case QuoteBlock quote:
            {
                var text = Slice(source, quote).Trim();

                if (text.Length > 0)
                {
                    blocks.Add(new DocumentBlock
                    {
                        Kind = BlockKind.Quote,
                        Text = text,
                        Order = order++
                    });
                }

                break;
            }

            case CodeBlock code:
            {
                var text = Slice(source, code).Trim();

                if (text.Length > 0)
                {
                    blocks.Add(new DocumentBlock
                    {
                        Kind = BlockKind.Code,
                        Text = text,
                        Order = order++
                    });
                }

                break;
            }

            case ParagraphBlock paragraph:
            {
                var text = Slice(source, paragraph).Trim();

                if (text.Length > 0)
                {
                    blocks.Add(new DocumentBlock
                    {
                        Kind = BlockKind.Paragraph,
                        Text = text,
                        Order = order++
                    });
                }

                break;
            }
        }
    }

    /// <summary>Cắt theo span của AST để giữ nguyên markdown gốc (quan trọng với bảng và code).</summary>
    private static string Slice(string source, Block block)
    {
        var start = Math.Clamp(block.Span.Start, 0, source.Length);
        var end = Math.Clamp(block.Span.End + 1, start, source.Length);

        return source[start..end];
    }
}
