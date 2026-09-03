namespace IChat.Infrastructure.Ingestion.Parsing;

using IChat.Core.Abstractions;
using IChat.Core.Common;

/// <summary>
/// Ca người dùng gõ nhầm phổ biến nhất. Phải trả thông điệp hướng dẫn lưu lại thành .docx
/// chứ không để thư viện OpenXml nổ thành 500 khi gặp file OLE2.
/// </summary>
public sealed class LegacyDocFormatDetector : IUnsupportedFormatDetector
{
    // OLE2 compound file: định dạng của Word 97-2003 (.doc), hoàn toàn khác docx.
    private static readonly byte[] Ole2Magic = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];

    public Error? Detect(string fileName, ReadOnlySpan<byte> header)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        if (extension != ".doc" && !StartsWith(header, Ole2Magic))
        {
            return null;
        }

        return Error.UnsupportedMediaType(
            "The .doc format (Word 97-2003) is not supported. Open the file in Word, save it as .docx and upload it again.");
    }

    private static bool StartsWith(ReadOnlySpan<byte> header, ReadOnlySpan<byte> magic)
    {
        return header.Length >= magic.Length && header[..magic.Length].SequenceEqual(magic);
    }
}
