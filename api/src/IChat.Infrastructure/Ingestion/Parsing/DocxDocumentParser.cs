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
/// DOCX là format tốt nhất trong bốn format được hỗ trợ: cấp heading là metadata thật
/// nằm sẵn trong file chứ không phải suy đoán từ font size như PDF.
/// </summary>
public sealed partial class DocxDocumentParser : IDocumentParser
{
    public const string DocxContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    public DocumentFormat Format { get; } = new()
    {
        ContentType = DocxContentType,

        // Mọi file Office hiện đại đều là zip; đuôi .docx mà không phải zip là file giả,
        // từ chối chứ không đưa cho OpenXml đoán.
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

        // Header/footer nằm ở part riêng nên duyệt body sẽ không chạm tới chúng —
        // đúng như mong muốn, vì chúng lặp trên mọi trang và chỉ tạo nhiễu cho index.
        foreach (var element in body.ChildElements)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AppendElement(element, blocks, ref order, styleOutlineLevels, numberingFormats, footnotes, insideTextBox: false);
        }

        // Text box nằm NGOÀI luồng paragraph chính; duyệt body theo cách thông thường
        // sẽ bỏ sót hoàn toàn, nên phải duyệt riêng.
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
                // Mục lục tự động là danh sách heading lặp lại; nó sẽ tạo ra một chunk
                // khớp với mọi truy vấn về tên mục và chiếm chỗ của chunk nội dung thật.
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

                // Ô gộp ngang: lặp lại độ rộng để số cột của markdown không lệch.
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
    /// Word hay cắt một câu thành nhiều w:r liền nhau (đổi màu, spell-check, lịch sử sửa).
    /// Ghép hết w:t trong cùng paragraph, nếu không sẽ ra những block dài vài ký tự.
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
            // Nội dung trong w:del là phần ĐÃ BỊ XÓA nhưng còn lưu vết của tracked changes.
            // Đưa nó vào index nghĩa là ichat sẽ trích dẫn một điều khoản đã bị gạch bỏ
            // như thể nó còn hiệu lực. Lỗi này im lặng hoàn toàn nên phải chặn tường minh,
            // không dựa vào việc SDK dùng w:delText cho văn bản đã xóa.
            if (HasAncestor<DeletedRun>(text) || HasAncestor<Deleted>(text))
            {
                continue;
            }

            // Text box được duyệt riêng ở một vòng khác, nên khi đang xử lý paragraph của
            // luồng chính thì bỏ qua để không nhân đôi. Nhưng lúc duyệt chính text box đó,
            // cờ này phải tắt, nếu không paragraph của nó sẽ ra rỗng và bị loại.
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

    /// <summary>Chỉ lấy alt text từ wp:docPr/@descr; phiên bản này không OCR.</summary>
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
    /// Ưu tiên tên style Heading1..Heading9; nếu không khớp thì tra w:outlineLvl.
    /// Fallback này là bắt buộc: template doanh nghiệp thường định nghĩa style riêng
    /// ("Muc1", "TieuDeChuong") mà vẫn gán outline level đúng.
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

    /// <summary>Map "numId:level" -> có phải bullet hay không, để flatten thành "- " hoặc "1. ".</summary>
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
