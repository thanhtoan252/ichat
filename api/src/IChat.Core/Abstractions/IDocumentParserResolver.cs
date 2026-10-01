namespace IChat.Core.Abstractions;

using IChat.Core.Common;

public interface IDocumentParserResolver
{
    /// <summary>How many leading bytes to read for magic-byte detection: enough for OLE2, the longest signature.</summary>
    const int MagicHeaderLength = 8;

    /// <summary>
    /// Trusts the extension plus the magic bytes over the content type the client declared,
    /// because browsers often send application/octet-stream or an outright wrong content type.
    /// </summary>
    Result<IDocumentParser> Resolve(string fileName, string declaredContentType, ReadOnlySpan<byte> header);

    Result<string> ResolveContentType(string fileName, string declaredContentType, ReadOnlySpan<byte> header);
}
