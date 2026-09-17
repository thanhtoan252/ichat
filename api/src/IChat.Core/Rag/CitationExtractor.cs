namespace IChat.Core.Rag;

using System.Text.RegularExpressions;

/// <summary>
/// The markers a model produces cannot be trusted. Only a marker pointing inside the real context
/// range is written to message_citations.
/// </summary>
public static partial class CitationExtractor
{
    [GeneratedRegex(@"\[(\d{1,3})\]", RegexOptions.CultureInvariant)]
    private static partial Regex MarkerRegex { get; }

    [GeneratedRegex(@"```.*?```|~~~.*?~~~|`[^`\n]*`", RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex CodeSpanRegex { get; }

    /// <summary>
    /// Returns the valid markers in order of appearance, deduplicated.
    /// <paramref name="sourceCount"/> is how many sources the context actually holds.
    /// </summary>
    public static IReadOnlyList<ExtractedCitation> Extract(string? answerText, int sourceCount, out IReadOnlyList<int> invalidMarkers)
    {
        var invalid = new List<int>();
        invalidMarkers = invalid;

        if (string.IsNullOrWhiteSpace(answerText) || sourceCount <= 0)
        {
            return [];
        }

        var masked = MaskCodeSpans(answerText);
        var seen = new HashSet<int>();
        var results = new List<ExtractedCitation>();

        foreach (Match match in MarkerRegex.Matches(masked))
        {
            if (!int.TryParse(match.Groups[1].ValueSpan, out var marker))
            {
                continue;
            }

            if (marker < 1 || marker > sourceCount)
            {
                if (!invalid.Contains(marker))
                {
                    invalid.Add(marker);
                }

                continue;
            }

            if (seen.Add(marker))
            {
                results.Add(new ExtractedCitation { MarkerIndex = marker, SourceOrdinal = marker - 1 });
            }
        }

        return results;
    }

    /// <summary>Replaces code spans with spaces of the same length so markers inside code are not counted.</summary>
    private static string MaskCodeSpans(string text)
    {
        return CodeSpanRegex.Replace(text, match => new string(' ', match.Length));
    }
}
