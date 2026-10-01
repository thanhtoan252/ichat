namespace IChat.Core.Abstractions;

using IChat.Core.Domain.Documents.Parsing;

public interface IStructuredChunker
{
    IReadOnlyList<TextChunk> Chunk(IReadOnlyList<DocumentBlock> blocks, string documentTitle);
}
