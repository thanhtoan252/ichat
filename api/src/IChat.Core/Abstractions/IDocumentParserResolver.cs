namespace IChat.Core.Abstractions;

using IChat.Core.Common;

public interface IDocumentParserResolver
{

    const int MagicHeaderLength = 8;

    Result<IDocumentParser> Resolve(string fileName, string declaredContentType, ReadOnlySpan<byte> header);

    Result<string> ResolveContentType(string fileName, string declaredContentType, ReadOnlySpan<byte> header);
}
