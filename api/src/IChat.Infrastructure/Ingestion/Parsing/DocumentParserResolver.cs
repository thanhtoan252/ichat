namespace IChat.Infrastructure.Ingestion.Parsing;

using IChat.Core.Abstractions;
using IChat.Core.Common;

/// <summary>
/// Trình duyệt và HTTP client thỉnh thoảng gửi application/octet-stream hoặc content type
/// sai, nên ưu tiên phần mở rộng + magic bytes hơn header do client khai báo.
/// Resolver không biết format nào tồn tại: mọi kiến thức nằm ở <see cref="DocumentFormat"/>
/// của từng parser, nên thêm format mới không phải sửa file này.
/// </summary>
public sealed class DocumentParserResolver(
    IEnumerable<IDocumentParser> parsers,
    IEnumerable<IUnsupportedFormatDetector> detectors) : IDocumentParserResolver
{
    private readonly IReadOnlyList<IDocumentParser> _parsers = [.. parsers];

    private readonly IReadOnlyList<IUnsupportedFormatDetector> _detectors = [.. detectors];

    private readonly Dictionary<string, IDocumentParser> _parsersByContentType =
        parsers.ToDictionary(parser => parser.Format.ContentType, StringComparer.OrdinalIgnoreCase);

    public Result<IDocumentParser> Resolve(string fileName, string declaredContentType, ReadOnlySpan<byte> header)
    {
        var contentTypeResult = ResolveContentType(fileName, declaredContentType, header);

        if (contentTypeResult.IsFailure)
        {
            return Result.Failure<IDocumentParser>(contentTypeResult.Error);
        }

        if (!_parsersByContentType.TryGetValue(contentTypeResult.Value, out var parser))
        {
            return Result.Failure<IDocumentParser>(
                Error.UnsupportedMediaType($"No parser registered for content type '{contentTypeResult.Value}'."));
        }

        return Result.Success(parser);
    }

    public Result<string> ResolveContentType(string fileName, string declaredContentType, ReadOnlySpan<byte> header)
    {
        // Định dạng bị từ chối có chủ đích phải chặn TRƯỚC TIÊN, kèm hướng dẫn cho người dùng.
        foreach (var detector in _detectors)
        {
            var rejected = detector.Detect(fileName, header);

            if (rejected is not null)
            {
                return Result.Failure<string>(rejected);
            }
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var byExtension = _parsers.FirstOrDefault(parser => parser.Format.Extensions.Contains(extension));

        if (byExtension is not null)
        {
            var format = byExtension.Format;

            if (format.RejectOnMagicMismatch && HasReadableHeader(header, format) && !MatchesMagic(header, format))
            {
                return Result.Failure<string>(Error.UnsupportedMediaType(
                    $"The file has a {extension} extension but its content is not an Office (zip) container."));
            }

            return Result.Success(format.ContentType);
        }

        // Span không đi qua được lambda, nên dò magic bằng vòng lặp tường minh.
        foreach (var parser in _parsers)
        {
            if (MatchesMagic(header, parser.Format))
            {
                return Result.Success(parser.Format.ContentType);
            }
        }

        // Không suy được từ đuôi file lẫn magic bytes thì mới xét tới content type client khai báo.
        if (_parsersByContentType.ContainsKey(declaredContentType))
        {
            return Result.Success(declaredContentType);
        }

        return Result.Failure<string>(Error.UnsupportedMediaType(
            $"The '{extension}' format is not supported. Only {SupportedExtensions()} are accepted."));
    }

    private static bool HasReadableHeader(ReadOnlySpan<byte> header, DocumentFormat format)
    {
        // Header ngắn hơn chữ ký thì không kết luận được gì; đừng từ chối chỉ vì đọc thiếu byte.
        foreach (var magic in format.MagicBytes)
        {
            if (header.Length >= magic.Length)
            {
                return true;
            }
        }

        return false;
    }

    private static bool MatchesMagic(ReadOnlySpan<byte> header, DocumentFormat format)
    {
        foreach (var magic in format.MagicBytes)
        {
            if (header.Length >= magic.Length && header[..magic.Length].SequenceEqual(magic))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Danh sách trong thông điệp lỗi sinh từ đuôi CHÍNH của từng parser theo thứ tự đăng ký,
    /// nên thêm một parser là thông điệp tự đúng theo.
    /// </summary>
    private string SupportedExtensions()
    {
        var extensions = _parsers
            .Select(parser => parser.Format.Extensions[0])
            .ToList();

        if (extensions.Count <= 1)
        {
            return string.Join(string.Empty, extensions);
        }

        return $"{string.Join(", ", extensions.Take(extensions.Count - 1))} and {extensions[^1]}";
    }
}
