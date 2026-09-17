namespace IChat.Infrastructure.Ingestion.Parsing;

using IChat.Core.Abstractions;
using IChat.Core.Common;

/// <summary>
/// Browsers and HTTP clients occasionally send application/octet-stream or an outright wrong content type,
/// so the extension plus the magic bytes win over the header the client declared.
/// The resolver does not know which formats exist: all of that knowledge lives in each parser's
/// <see cref="DocumentFormat"/>, so adding a format never means editing this file.
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
        // Deliberately rejected formats must be stopped FIRST, with guidance for the user.
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

        // A Span cannot cross a lambda, so the magic bytes are probed with an explicit loop.
        foreach (var parser in _parsers)
        {
            if (MatchesMagic(header, parser.Format))
            {
                return Result.Success(parser.Format.ContentType);
            }
        }

        // Only once neither the extension nor the magic bytes say anything does the declared content type matter.
        if (_parsersByContentType.ContainsKey(declaredContentType))
        {
            return Result.Success(declaredContentType);
        }

        return Result.Failure<string>(Error.UnsupportedMediaType(
            $"The '{extension}' format is not supported. Only {SupportedExtensions()} are accepted."));
    }

    private static bool HasReadableHeader(ReadOnlySpan<byte> header, DocumentFormat format)
    {
        // A header shorter than the signature proves nothing; do not reject just because too few bytes were read.
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
    /// The list in the error message is generated from each parser's PRIMARY extension in registration order,
    /// so adding a parser keeps the message correct by itself.
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
