namespace IChat.Infrastructure.Ingestion.Parsing;

using IChat.Core.Abstractions;
using IChat.Core.Domain.Documents.Parsing;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

/// <summary>
/// Khác DOCX, PDF không có metadata heading: cấp heading phải suy từ font size và bold.
/// Suy không được thì HeadingLevel = null, chấp nhận — thà không có còn hơn đoán sai.
/// </summary>
public sealed class PdfDocumentParser : IDocumentParser
{
    public const string PdfContentType = "application/pdf";

    private const double HeadingSizeRatio = 1.15;

    public DocumentFormat Format { get; } = new()
    {
        ContentType = PdfContentType,
        Extensions = [".pdf"],
        MagicBytes = [[0x25, 0x50, 0x44, 0x46]]
    };

    public Task<ParsedDocument> ParseAsync(Stream content, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        content.CopyTo(buffer);
        buffer.Position = 0;

        using var pdfDocument = PdfDocument.Open(buffer);

        var lines = new List<PdfLine>();

        foreach (var page in pdfDocument.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();
            lines.AddRange(ExtractLines(page));
        }

        if (lines.Count == 0)
        {
            return Task.FromResult(new ParsedDocument
            {
                Blocks = [],
                Metadata = new Dictionary<string, string> { ["format"] = "pdf" }
            });
        }

        // Cỡ chữ thân bài = trung vị của mọi dòng; heading là dòng nổi bật hơn mức đó.
        var bodySize = Median(lines.Select(line => line.FontSize).ToList());
        var headingSizes = lines
            .Where(line => IsHeadingCandidate(line, bodySize))
            .Select(line => Math.Round(line.FontSize, 1))
            .Distinct()
            .OrderByDescending(size => size)
            .ToList();

        var blocks = new List<DocumentBlock>();
        var order = 0;
        var paragraphBuffer = new List<PdfLine>();

        void FlushParagraph()
        {
            if (paragraphBuffer.Count == 0)
            {
                return;
            }

            var text = string.Join(" ", paragraphBuffer.Select(line => line.Text)).Trim();

            if (text.Length > 0)
            {
                blocks.Add(new DocumentBlock
                {
                    Kind = BlockKind.Paragraph,
                    Text = text,
                    Order = order++,
                    Metadata = new Dictionary<string, string> { ["page"] = paragraphBuffer[0].PageNumber.ToString() }
                });
            }

            paragraphBuffer.Clear();
        }

        foreach (var line in lines)
        {
            if (IsHeadingCandidate(line, bodySize))
            {
                FlushParagraph();

                var rounded = Math.Round(line.FontSize, 1);
                var level = headingSizes.IndexOf(rounded);
                var headingLevel = level >= 0 ? Math.Min(level + 1, 6) : (int?)null;

                blocks.Add(new DocumentBlock
                {
                    Kind = BlockKind.Heading,
                    Text = line.Text,
                    HeadingLevel = headingLevel,
                    Order = order++,
                    Metadata = new Dictionary<string, string> { ["page"] = line.PageNumber.ToString() }
                });

                continue;
            }

            paragraphBuffer.Add(line);
        }

        FlushParagraph();

        return Task.FromResult(new ParsedDocument
        {
            Blocks = blocks,
            Metadata = new Dictionary<string, string> { ["format"] = "pdf" }
        });
    }

    private static bool IsHeadingCandidate(PdfLine line, double bodySize)
    {
        if (line.Text.Length > 200)
        {
            return false;
        }

        return line.FontSize >= bodySize * HeadingSizeRatio || (line.IsBold && line.FontSize >= bodySize);
    }

    private static IEnumerable<PdfLine> ExtractLines(Page page)
    {
        var words = page.GetWords().ToList();

        if (words.Count == 0)
        {
            yield break;
        }

        // Gom từ thành dòng theo toạ độ Y; ngưỡng nới theo chiều cao chữ.
        var groups = words
            .GroupBy(word => Math.Round(word.BoundingBox.Bottom / 3.0))
            .OrderByDescending(group => group.Key);

        foreach (var group in groups)
        {
            var ordered = group.OrderBy(word => word.BoundingBox.Left).ToList();
            var text = string.Join(" ", ordered.Select(word => word.Text)).Trim();

            if (text.Length == 0)
            {
                continue;
            }

            var letters = ordered.SelectMany(word => word.Letters).ToList();

            if (letters.Count == 0)
            {
                continue;
            }

            var fontSize = Median(letters.Select(letter => letter.PointSize).ToList());
            var boldCount = letters.Count(letter => letter.FontName?.Contains("Bold", StringComparison.OrdinalIgnoreCase) == true);

            yield return new PdfLine
            {
                Text = text,
                FontSize = fontSize,
                IsBold = boldCount > letters.Count / 2,
                PageNumber = page.Number
            };
        }
    }

    private static double Median(List<double> values)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        var sorted = values.OrderBy(value => value).ToList();

        return sorted[sorted.Count / 2];
    }

    private readonly struct PdfLine
    {
        public required string Text { get; init; }

        public required double FontSize { get; init; }

        public required bool IsBold { get; init; }

        public required int PageNumber { get; init; }
    }
}
