namespace IChat.Core.Rag;

using System.Text.RegularExpressions;

public static partial class CitationExtractor
{
    [GeneratedRegex(@"\[(\d{1,3})\]", RegexOptions.CultureInvariant)]
    private static partial Regex MarkerRegex { get; }

[GeneratedRegex(@"```.*?```|~~~.*?~~~|`[^`\n]*`", RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex CodeSpanRegex { get; }

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

private static string MaskCodeSpans(string text)
    {
        return CodeSpanRegex.Replace(text, match => new string(' ', match.Length));
    }
}
