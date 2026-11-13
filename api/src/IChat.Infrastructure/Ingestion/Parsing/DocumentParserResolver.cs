namespace IChat.Infrastructure.Ingestion.Parsing;

using IChat.Core.Abstractions;
using IChat.Core.Common;

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

        foreach (var parser in _parsers)
        {
            if (MatchesMagic(header, parser.Format))
            {
                return Result.Success(parser.Format.ContentType);
            }
        }

        if (_parsersByContentType.ContainsKey(declaredContentType))
        {
            return Result.Success(declaredContentType);
        }

        return Result.Failure<string>(Error.UnsupportedMediaType(
                    $"The '{extension}' format is not supported. Only {SupportedExtensions()} are accepted."));
    }

    private static bool HasReadableHeader(ReadOnlySpan<byte> header, DocumentFormat format)
    {

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
