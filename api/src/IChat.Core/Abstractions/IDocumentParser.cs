namespace IChat.Core.Abstractions;

using IChat.Core.Domain.Documents.Parsing;

public interface IDocumentParser
{
    DocumentFormat Format { get; }

    Task<ParsedDocument> ParseAsync(Stream content, CancellationToken cancellationToken);
}
