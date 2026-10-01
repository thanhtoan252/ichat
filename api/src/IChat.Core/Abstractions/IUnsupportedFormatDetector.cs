namespace IChat.Core.Abstractions;

using IChat.Core.Common;

/// <summary>
/// Runs ahead of any format detection: a format that is rejected ON PURPOSE must come back with a
/// message telling the user what to do, instead of reaching a parser and blowing up as a 500.
/// </summary>
public interface IUnsupportedFormatDetector
{
    /// <summary>Returns an Error when the format is rejected on purpose, null when it has no opinion.</summary>
    Error? Detect(string fileName, ReadOnlySpan<byte> header);
}
