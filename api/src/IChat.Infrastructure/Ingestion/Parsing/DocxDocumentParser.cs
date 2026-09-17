namespace IChat.Infrastructure.Ingestion.Parsing;

using System.Text;
using System.Text.RegularExpressions;
using IChat.Core.Abstractions;
using IChat.Core.Domain.Documents.Parsing;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DrawingWp = DocumentFormat.OpenXml.Drawing.Wordprocessing;

/// <summary>
/// DOCX is the best of the four supported formats: heading levels are real metadata already present in the
/// file, rather than something inferred from font size as in PDF.
/// </summary>
public sealed partial class DocxDocumentParser : IDocumentParser
{
    public const string DocxContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    public DocumentFormat Format { get; } = new()
    {
        ContentType = DocxContentType,

        // Every modern Office file is a zip; a .docx that is not a zip is a fake,
        // so reject it instead of handing it to OpenXml to guess at.
        Extensions = [".docx"],
        MagicBytes = [[0x50, 0x4B, 0x03, 0x04]],
        RejectOnMagicMismatch = true
    };

    [GeneratedRegex(@"^Heading([1-9])$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex HeadingStyleRegex { get; }

    [GeneratedRegex(@"^TOC\d*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex TocStyleRegex { get; }

    public Task<ParsedDocument> ParseAsync(Stream content, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);

        using var buffer = new MemoryStream();
        content.CopyTo(buffer);
        buffer.Position = 0;

        using var wordDocument = WordprocessingDocument.Open(buffer, false);
        var mainPart = wordDocument.MainDocumentPart
            ?? throw new InvalidOperationException("The docx file has no MainDocumentPart.");

        var body = mainPart.Document?.Body
            ?? throw new InvalidOperationException("The docx file has no body.");

        var styleOutlineLevels = BuildStyleOutlineLevels(mainPart);
        var numberingFormats = BuildNumberingFormats(mainPart);
        var footnotes = BuildFootnotes(mainPart);

        var blocks = new List<DocumentBlock>();
        var order = 0;

        // Headers and footers live in their own parts, so walking the body never touches them —
        // which is what we want, since they repeat on every page and only add noise to the index.
        foreach (var element in body.ChildElements)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AppendElement(element, blocks, ref order, styleOutlineLevels, numberingFormats, footnotes, insideTextBox: false);
        }

        // Text boxes sit OUTSIDE the main paragraph flow; walking the body the usual way would miss them
        // entirely, so they get their own pass.
        foreach (var textBox in body.Descendants<TextBoxContent>())
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var element in textBox.ChildElements)
            {
                AppendElement(element, blocks, ref order, styleOutlineLevels, numberingFormats, footnotes, insideTextBox: true);
            }
        }

        var metadata = new Dictionary<string, string> { ["format"] = "docx" };

        return Task.FromResult(new ParsedDocument { Blocks = blocks, Metadata = metadata });
    }

    private static void AppendElement(
        OpenXmlElement element,
        List<DocumentBlock> blocks,
        ref int order,
        IReadOnlyDictionary<string, int> styleOutlineLevels,
        IReadOnlyDictionary<string, bool> numberingFormats,
        IReadOnlyDictionary<string, string> footnotes,
        bool insideTextBox)
    {
        switch (element)
        {
            case Paragraph paragraph:
            {
                var block = BuildParagraphBlock(paragraph, order, styleOutlineLevels, numberingFormats, footnotes, insideTextBox);

                if (block is not null)
                {
                    blocks.Add(block);
                    order++;
                }

                break;
            }

            case Table table:
            {
                var block = BuildTableBlock(table, order, insideTextBox);

                if (block is not null)
                {
                    blocks.Add(block);
                    order++;
                }

                break;
            }

            case SdtBlock sdtBlock:
            {
                // An automatic table of contents is just a repeated list of headings; it would produce a chunk
                // that matches every question about a section name and crowd out the real content chunk.
                if (IsTableOfContents(sdtBlock))
                {
                    break;
                }

                foreach (var child in sdtBlock.Descendants<Paragraph>())
                {
                    var block = BuildParagraphBlock(child, order, styleOutlineLevels, numberingFormats, footnotes, insideTextBox);

                    if (block is not null)
                    {
                        blocks.Add(block);
                        order++;
                    }
                }

                break;
            }
        }
    }

    private static DocumentBlock? BuildParagraphBlock(
        Paragraph paragraph,
        int order,
        IReadOnlyDictionary<string, int> styleOutlineLevels,
        IReadOnlyDictionary<string, bool> numberingFormats,
        IReadOnlyDictionary<string, string> footnotes,
        bool insideTextBox)
    {
        var styleId = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;

        if (styleId is not null && TocStyleRegex.IsMatch(styleId))
        {
            return null;
        }

        if (paragraph.Descendants<FieldCode>().Any(field => field.Text?.Contains("TOC", StringComparison.OrdinalIgnoreCase) == true))
        {
            return null;
        }

        var text = ExtractText(paragraph, excludeTextBoxDescendants: !insideTextBox);
        var altText = ExtractImageAltText(paragraph);

        if (!string.IsNullOrWhiteSpace(altText))
        {
            text = string.IsNullOrWhiteSpace(text) ? altText : $"{text}\n{altText}";
        }

        var metadata = new Dictionary<string, string>();

        var footnoteText = ExtractFootnotes(paragraph, footnotes);
        if (!string.IsNullOrWhiteSpace(footnoteText))
        {
            text = $"{text}\n{footnoteText}";
            metadata["hasFootnote"] = "true";
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (insideTextBox)
        {
            metadata["source"] = "textbox";
        }

        var headingLevel = ResolveHeadingLevel(paragraph, styleId, styleOutlineLevels);

        if (headingLevel is not null)
        {
            return new DocumentBlock
            {
                Kind = BlockKind.Heading,
                Text = text,
                HeadingLevel = headingLevel,
                Order = order,
                Metadata = metadata
            };
        }

        var numbering = paragraph.ParagraphProperties?.NumberingProperties;

        if (numbering is not null)
        {
            var listLevel = numbering.NumberingLevelReference?.Val?.Value ?? 0;
            var numberingId = numbering.NumberingId?.Val?.Value;
            var isBullet = numberingId is null
                || !numberingFormats.TryGetValue($"{numberingId}:{listLevel}", out var bullet)
                || bullet;

            var marker = isBullet ? "- " : "1. ";
            var indent = new string(' ', listLevel * 2);

            return new DocumentBlock
            {
                Kind = BlockKind.ListItem,
                Text = $"{indent}{marker}{text}",
                ListLevel = listLevel,
                Order = order,
                Metadata = metadata
            };
        }

        return new DocumentBlock
        {
            Kind = BlockKind.Paragraph,
            Text = text,
            Order = order,
            Metadata = metadata
        };
    }

    private static DocumentBlock? BuildTableBlock(Table table, int order, bool insideTextBox)
    {
        var rows = new List<IReadOnlyList<string>>();

        foreach (var row in table.Elements<TableRow>())
        {
            var cells = new List<string>();

            foreach (var cell in row.Elements<TableCell>())
            {
                var cellText = string.Join(
                    " ",
                    cell.Descendants<Paragraph>().Select(ExtractText).Where(value => !string.IsNullOrWhiteSpace(value)));

                cells.Add(cellText);

                // A horizontally merged cell: repeat the width so the markdown column count stays aligned.
                var span = cell.TableCellProperties?.GridSpan?.Val?.Value ?? 1;
                for (var i = 1; i < span; i++)
                {
                    cells.Add(string.Empty);
                }
            }

            if (cells.Count > 0)
            {
                rows.Add(cells);
            }
        }

        if (rows.Count == 0)
        {
            return null;
        }

        var metadata = new Dictionary<string, string> { ["containsTable"] = "true" };

        if (insideTextBox)
        {
            metadata["source"] = "textbox";
        }

        return new DocumentBlock
        {
            Kind = BlockKind.Table,
            Text = TableToMarkdown.Render(rows),
            Order = order,
            Metadata = metadata
        };
    }

    /// <summary>
    /// Word often splits one sentence across several adjacent w:r elements (a colour change, spell-check, an
    /// edit history). Join every w:t within the same paragraph, or the result is blocks a few characters long.
    /// </summary>
    private static string ExtractText(Paragraph paragraph)
    {
        return ExtractText(paragraph, excludeTextBoxDescendants: true);
    }

    private static string ExtractText(Paragraph paragraph, bool excludeTextBoxDescendants)
    {
        var builder = new StringBuilder();

        foreach (var text in paragraph.Descendants<Text>())
        {
            // Content inside w:del is text that HAS BEEN DELETED but is still kept as a tracked change.
            // Indexing it means ichat would cite a clause that was struck out as though it still applied.
            // That failure is completely silent, so it has to be blocked explicitly rather than relying on
            // the SDK using w:delText for deleted text.
            if (HasAncestor<DeletedRun>(text) || HasAncestor<Deleted>(text))
            {
                continue;
            }

            // Text boxes get their own pass, so while walking a paragraph of the main flow they are skipped to
            // avoid duplicates. But during that dedicated pass this flag must be off, otherwise the text box's
            // own paragraphs would come out empty and be dropped.
            if (excludeTextBoxDescendants && HasAncestor<TextBoxContent>(text))
            {
                continue;
            }

            builder.Append(text.Text);
        }

        return builder.ToString().Trim();
    }

    private static bool HasAncestor<TElement>(OpenXmlElement element) where TElement : OpenXmlElement
    {
        for (var parent = element.Parent; parent is not null; parent = parent.Parent)
        {
            if (parent is TElement)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Takes alt text from wp:docPr/@descr only; this version does no OCR.</summary>
    private static string ExtractImageAltText(Paragraph paragraph)
    {
        var descriptions = paragraph.Descendants<DrawingWp.DocProperties>()
            .Select(properties => properties.Description?.Value)
            .Where(description => !string.IsNullOrWhiteSpace(description))
            .ToList();

        return descriptions.Count == 0 ? string.Empty : string.Join(" ", descriptions);
    }

    private static string ExtractFootnotes(Paragraph paragraph, IReadOnlyDictionary<string, string> footnotes)
    {
        var parts = new List<string>();

        foreach (var reference in paragraph.Descendants<FootnoteReference>())
        {
            var id = reference.Id?.Value.ToString();

            if (id is not null && footnotes.TryGetValue(id, out var footnoteText) && !string.IsNullOrWhiteSpace(footnoteText))
            {
                parts.Add(footnoteText);
            }
        }

        return parts.Count == 0 ? string.Empty : string.Join(" ", parts);
    }

    /// <summary>
    /// Prefers the Heading1..Heading9 style names; falls back to w:outlineLvl when none matches.
    /// That fallback is essential: corporate templates routinely define their own styles
    /// ("Muc1", "TieuDeChuong") while still setting the correct outline level.
    /// </summary>
    private static int? ResolveHeadingLevel(Paragraph paragraph, string? styleId, IReadOnlyDictionary<string, int> styleOutlineLevels)
    {
        if (styleId is not null)
        {
            var match = HeadingStyleRegex.Match(styleId);

            if (match.Success)
            {
                return int.Parse(match.Groups[1].ValueSpan);
            }
        }

        var directOutlineLevel = paragraph.ParagraphProperties?.OutlineLevel?.Val?.Value;

        if (directOutlineLevel is >= 0 and <= 8)
        {
            return directOutlineLevel.Value + 1;
        }

        if (styleId is not null && styleOutlineLevels.TryGetValue(styleId, out var level))
        {
            return level;
        }

        return null;
    }

    private static Dictionary<string, int> BuildStyleOutlineLevels(MainDocumentPart mainPart)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var styles = mainPart.StyleDefinitionsPart?.Styles;

        if (styles is null)
        {
            return result;
        }

        foreach (var style in styles.Elements<Style>())
        {
            var styleId = style.StyleId?.Value;

            if (styleId is null)
            {
                continue;
            }

            var outlineLevel = style.StyleParagraphProperties?.OutlineLevel?.Val?.Value;

            if (outlineLevel is >= 0 and <= 8)
            {
                result[styleId] = outlineLevel.Value + 1;
            }
        }

        return result;
    }

    /// <summary>Maps "numId:level" -> whether it is a bullet, so it can be flattened into "- " or "1. ".</summary>
    private static Dictionary<string, bool> BuildNumberingFormats(MainDocumentPart mainPart)
    {
        var result = new Dictionary<string, bool>(StringComparer.Ordinal);
        var numbering = mainPart.NumberingDefinitionsPart?.Numbering;

        if (numbering is null)
        {
            return result;
        }

        var abstractFormats = new Dictionary<string, Dictionary<int, bool>>(StringComparer.Ordinal);

        foreach (var abstractNum in numbering.Elements<AbstractNum>())
        {
            var abstractId = abstractNum.AbstractNumberId?.Value.ToString();

            if (abstractId is null)
            {
                continue;
            }

            var levels = new Dictionary<int, bool>();

            foreach (var level in abstractNum.Elements<Level>())
            {
                var levelIndex = level.LevelIndex?.Value ?? 0;
                var format = level.NumberingFormat?.Val?.Value;
                levels[levelIndex] = format == NumberFormatValues.Bullet;
            }

            abstractFormats[abstractId] = levels;
        }

        foreach (var numberingInstance in numbering.Elements<NumberingInstance>())
        {
            var numberId = numberingInstance.NumberID?.Value.ToString();
            var abstractId = numberingInstance.AbstractNumId?.Val?.Value.ToString();

            if (numberId is null || abstractId is null || !abstractFormats.TryGetValue(abstractId, out var levels))
            {
                continue;
            }

            foreach (var (levelIndex, isBullet) in levels)
            {
                result[$"{numberId}:{levelIndex}"] = isBullet;
            }
        }

        return result;
    }

    private static Dictionary<string, string> BuildFootnotes(MainDocumentPart mainPart)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        var footnotes = mainPart.FootnotesPart?.Footnotes;

        if (footnotes is null)
        {
            return result;
        }

        foreach (var footnote in footnotes.Elements<Footnote>())
        {
            var id = footnote.Id?.Value.ToString();

            if (id is null)
            {
                continue;
            }

            var text = string.Join(" ", footnote.Descendants<Paragraph>().Select(ExtractText).Where(value => !string.IsNullOrWhiteSpace(value)));

            if (!string.IsNullOrWhiteSpace(text))
            {
                result[id] = text;
            }
        }

        return result;
    }

    private static bool IsTableOfContents(SdtBlock sdtBlock)
    {
        var gallery = sdtBlock.SdtProperties?.GetFirstChild<SdtContentDocPartObject>()?.DocPartGallery?.Val?.Value;

        if (gallery?.Contains("Table of Contents", StringComparison.OrdinalIgnoreCase) == true)
        {
            return true;
        }

        return sdtBlock.Descendants<FieldCode>().Any(field => field.Text?.Contains("TOC", StringComparison.OrdinalIgnoreCase) == true);
    }
}
