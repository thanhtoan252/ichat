namespace IChat.Core.Rag;

using System.Text.RegularExpressions;

/// <summary>
/// Không được tin marker mà model sinh ra. Chỉ marker trỏ vào đúng phạm vi context
/// mới được ghi vào message_citations.
/// </summary>
public static partial class CitationExtractor
{
    [GeneratedRegex(@"\[(\d{1,3})\]", RegexOptions.CultureInvariant)]
    private static partial Regex MarkerRegex { get; }

    [GeneratedRegex(@"```.*?```|~~~.*?~~~|`[^`\n]*`", RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex CodeSpanRegex { get; }

    /// <summary>
    /// Trả về các marker hợp lệ theo thứ tự xuất hiện, đã khử trùng lặp.
    /// <paramref name="sourceCount"/> là số nguồn thực sự có trong context.
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

    /// <summary>Thay code span bằng khoảng trắng cùng độ dài để marker bên trong code không bị tính.</summary>
    private static string MaskCodeSpans(string text)
    {
        return CodeSpanRegex.Replace(text, match => new string(' ', match.Length));
    }
}
