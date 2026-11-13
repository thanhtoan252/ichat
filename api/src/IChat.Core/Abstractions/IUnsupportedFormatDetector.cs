namespace IChat.Core.Abstractions;

using IChat.Core.Common;

public interface IUnsupportedFormatDetector
{

    Error? Detect(string fileName, ReadOnlySpan<byte> header);
}
