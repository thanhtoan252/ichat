namespace IChat.Infrastructure.Ingestion;

using System.Text;
using IChat.Core.Abstractions;
using IChat.Core.Domain.Documents.Parsing;
using IChat.Core.Rag;
using Microsoft.Extensions.Options;

/// <summary>
/// Cắt mù theo dấu câu là nguyên nhân phổ biến nhất khiến retrieval trả về chunk vô nghĩa:
/// chunk thứ 47 đứng một mình thì không ai biết nó nói về cái gì. Chunker này cắt theo
/// cây heading trước, và không bao giờ cắt ngang qua ranh giới heading.
/// </summary>
public sealed class HeadingAwareChunker(IOptions<RagOptions> options, ITokenEstimator tokenEstimator) : IStructuredChunker
{
    private static readonly string[] Separators = ["\n\n", "\n", ". ", " "];

    private readonly ChunkingOptions _chunking = options.Value.Chunking;

    public IReadOnlyList<TextChunk> Chunk(IReadOnlyList<DocumentBlock> blocks, string documentTitle)
    {
        ArgumentNullException.ThrowIfNull(blocks);

        var sections = BuildSections(blocks);
        var chunks = new List<TextChunk>();
        var index = 0;

        foreach (var section in sections)
        {
            foreach (var piece in ChunkSection(section))
            {
                var embeddedText = BuildEmbeddedText(documentTitle, section.HeadingPath, piece.Content);

                chunks.Add(new TextChunk
                {
                    Index = index++,
                    Content = piece.Content,
                    HeadingPath = section.HeadingPath,
                    EmbeddedText = embeddedText,
                    TokenCount = tokenEstimator.Estimate(piece.Content),
                    Metadata = piece.Metadata
                });
            }
        }

        return chunks;
    }

    /// <summary>
    /// Thêm tiền tố "title > heading path" trước khi embed là thay đổi rẻ nhất và
    /// hiệu quả nhất trong toàn bộ pipeline này.
    /// </summary>
    private string BuildEmbeddedText(string documentTitle, string? headingPath, string content)
    {
        if (!_chunking.PrependHeadingPath)
        {
            return content;
        }

        var prefix = string.IsNullOrWhiteSpace(headingPath)
            ? documentTitle
            : $"{documentTitle} > {headingPath}";

        return $"{prefix}\n\n{content}";
    }

    private static List<Section> BuildSections(IReadOnlyList<DocumentBlock> blocks)
    {
        var sections = new List<Section>();
        var headingStack = new List<(int Level, string Text)>();
        var current = new Section { Blocks = [] };

        foreach (var block in blocks)
        {
            if (block.Kind == BlockKind.Heading && block.HeadingLevel is > 0)
            {
                if (current.Blocks.Count > 0)
                {
                    sections.Add(current);
                }

                var level = block.HeadingLevel.Value;
                headingStack.RemoveAll(entry => entry.Level >= level);
                headingStack.Add((level, block.Text));

                current = new Section
                {
                    HeadingPath = string.Join(" > ", headingStack.Select(entry => entry.Text)),
                    Blocks = []
                };

                continue;
            }

            current.Blocks.Add(block);
        }

        if (current.Blocks.Count > 0)
        {
            sections.Add(current);
        }

        return sections;
    }

    private List<Piece> ChunkSection(Section section)
    {
        var pieces = new List<Piece>();
        var buffer = new List<DocumentBlock>();
        var bufferTokens = 0;

        void Flush()
        {
            if (buffer.Count == 0)
            {
                return;
            }

            pieces.Add(BuildPiece(buffer));
            buffer.Clear();
            bufferTokens = 0;
        }

        foreach (var block in section.Blocks)
        {
            var blockTokens = tokenEstimator.Estimate(block.Text);

            // Bảng là đơn vị nguyên tử: không bao giờ split, kể cả khi vượt TargetTokens.
            if (block.Kind == BlockKind.Table)
            {
                Flush();
                pieces.Add(BuildPiece([block]));

                continue;
            }

            if (blockTokens > _chunking.TargetTokens)
            {
                Flush();

                foreach (var part in SplitLongText(block.Text))
                {
                    pieces.Add(BuildPiece([block], part));
                }

                continue;
            }

            if (bufferTokens + blockTokens > _chunking.TargetTokens && buffer.Count > 0)
            {
                Flush();
            }

            buffer.Add(block);
            bufferTokens += blockTokens;
        }

        Flush();

        return MergeUndersizedPieces(pieces);
    }

    /// <summary>Chunk nhỏ hơn MinTokens thì gộp vào chunk kề, trong cùng một section.</summary>
    private List<Piece> MergeUndersizedPieces(List<Piece> pieces)
    {
        if (pieces.Count <= 1)
        {
            return pieces;
        }

        var merged = new List<Piece>();

        foreach (var piece in pieces)
        {
            if (merged.Count > 0 &&
                tokenEstimator.Estimate(piece.Content) < _chunking.MinTokens &&
                !piece.ContainsTable &&
                !merged[^1].ContainsTable)
            {
                merged[^1] = merged[^1].MergeWith(piece);

                continue;
            }

            merged.Add(piece);
        }

        // Chunk đầu tiên vẫn có thể quá nhỏ nếu section chỉ có một mẩu ngắn.
        if (merged.Count > 1 &&
            tokenEstimator.Estimate(merged[0].Content) < _chunking.MinTokens &&
            !merged[0].ContainsTable &&
            !merged[1].ContainsTable)
        {
            merged[1] = merged[0].MergeWith(merged[1]);
            merged.RemoveAt(0);
        }

        return merged;
    }

    private List<string> SplitLongText(string text)
    {
        var atoms = SplitIntoAtoms(text, 0);
        var results = new List<string>();
        var buffer = new List<string>();
        var bufferTokens = 0;

        foreach (var atom in atoms)
        {
            var atomTokens = tokenEstimator.Estimate(atom);

            if (bufferTokens + atomTokens > _chunking.TargetTokens && buffer.Count > 0)
            {
                results.Add(string.Concat(buffer).Trim());

                // Giữ lại phần đuôi làm overlap để câu trả lời nằm vắt qua ranh giới
                // vẫn còn nguyên trong ít nhất một chunk.
                var overlap = new List<string>();
                var overlapTokens = 0;

                for (var i = buffer.Count - 1; i >= 0 && overlapTokens < _chunking.OverlapTokens; i--)
                {
                    overlap.Insert(0, buffer[i]);
                    overlapTokens += tokenEstimator.Estimate(buffer[i]);
                }

                buffer = overlap;
                bufferTokens = overlapTokens;
            }

            buffer.Add(atom);
            bufferTokens += atomTokens;
        }

        if (buffer.Count > 0)
        {
            var tail = string.Concat(buffer).Trim();

            if (tail.Length > 0)
            {
                results.Add(tail);
            }
        }

        return results.Count == 0 ? [text] : results;
    }

    private List<string> SplitIntoAtoms(string text, int separatorIndex)
    {
        if (separatorIndex >= Separators.Length)
        {
            return [text];
        }

        var separator = Separators[separatorIndex];
        var parts = text.Split(separator);

        if (parts.Length == 1)
        {
            return SplitIntoAtoms(text, separatorIndex + 1);
        }

        var atoms = new List<string>();

        for (var i = 0; i < parts.Length; i++)
        {
            var part = i < parts.Length - 1 ? parts[i] + separator : parts[i];

            if (part.Length == 0)
            {
                continue;
            }

            if (tokenEstimator.Estimate(part) > _chunking.TargetTokens)
            {
                atoms.AddRange(SplitIntoAtoms(part, separatorIndex + 1));
            }
            else
            {
                atoms.Add(part);
            }
        }

        return atoms;
    }

    private static Piece BuildPiece(IReadOnlyList<DocumentBlock> blocks, string? overrideContent = null)
    {
        var content = overrideContent ?? string.Join("\n\n", blocks.Select(block => block.Text)).Trim();
        var containsTable = blocks.Any(block => block.Kind == BlockKind.Table);

        var metadata = new Dictionary<string, object?>
        {
            ["blockKinds"] = blocks.Select(block => block.Kind.ToString()).Distinct().ToArray(),
            ["startBlockIndex"] = blocks[0].Order,
            ["endBlockIndex"] = blocks[^1].Order,
            ["containsTable"] = containsTable
        };

        // page chỉ có ý nghĩa với PDF. DOCX không có trang cố định khi chưa render,
        // nên tuyệt đối không bịa số trang cho docx — định vị bằng startBlockIndex.
        var page = blocks
            .Select(block => block.Metadata is not null && block.Metadata.TryGetValue("page", out var value) ? value : null)
            .FirstOrDefault(value => value is not null);

        metadata["page"] = page is not null && int.TryParse(page, out var pageNumber) ? pageNumber : null;

        return new Piece
        {
            Content = content,
            ContainsTable = containsTable,
            Metadata = metadata
        };
    }

    private sealed class Section
    {
        public string? HeadingPath { get; init; }

        public required List<DocumentBlock> Blocks { get; init; }
    }

    private sealed class Piece
    {
        public required string Content { get; init; }

        public required bool ContainsTable { get; init; }

        public required IReadOnlyDictionary<string, object?> Metadata { get; init; }

        public Piece MergeWith(Piece other)
        {
            var metadata = new Dictionary<string, object?>(Metadata)
            {
                ["endBlockIndex"] = other.Metadata.GetValueOrDefault("endBlockIndex"),
                ["containsTable"] = ContainsTable || other.ContainsTable
            };

            var kinds = new List<string>();
            if (Metadata.GetValueOrDefault("blockKinds") is string[] left)
            {
                kinds.AddRange(left);
            }

            if (other.Metadata.GetValueOrDefault("blockKinds") is string[] right)
            {
                kinds.AddRange(right);
            }

            metadata["blockKinds"] = kinds.Distinct().ToArray();

            return new Piece
            {
                Content = new StringBuilder(Content).Append("\n\n").Append(other.Content).ToString().Trim(),
                ContainsTable = ContainsTable || other.ContainsTable,
                Metadata = metadata
            };
        }
    }
}
