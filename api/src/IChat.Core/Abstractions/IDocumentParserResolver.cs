namespace IChat.Core.Abstractions;

using IChat.Core.Common;

public interface IDocumentParserResolver
{
    /// <summary>Số byte đầu file cần đọc để dò magic bytes: đủ cho OLE2, chữ ký dài nhất.</summary>
    const int MagicHeaderLength = 8;

    /// <summary>
    /// Ưu tiên phần mở rộng + magic bytes hơn content type do client khai báo,
    /// vì trình duyệt hay gửi application/octet-stream hoặc content type sai.
    /// </summary>
    Result<IDocumentParser> Resolve(string fileName, string declaredContentType, ReadOnlySpan<byte> header);

    Result<string> ResolveContentType(string fileName, string declaredContentType, ReadOnlySpan<byte> header);
}
