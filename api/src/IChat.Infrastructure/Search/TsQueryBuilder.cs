namespace IChat.Infrastructure.Search;

using System.Globalization;
using System.Text;

public static class TsQueryBuilder
{

    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
    {

"va", "la", "cua", "cac", "mot", "cho", "voi", "trong", "den", "tu", "khi", "nay",
        "khong", "duoc", "nhu", "ve", "thi", "ma", "neu", "hay", "hoac", "nhung", "cung",
        "se", "dang", "phai", "vi", "de", "theo", "tren", "duoi", "sau", "truoc", "giua",
        "nhieu", "lam", "the", "nao", "gi", "sao", "toi", "roi", "nhe", "a", "u",

"the", "is", "are", "was", "were", "be", "been", "being", "an", "of", "to",
        "in", "on", "at", "for", "with", "by", "from", "as", "that", "this", "these",
        "those", "its", "and", "or", "but", "if", "then", "than", "so", "such",
        "can", "could", "will", "would", "shall", "should", "may", "might", "must",
        "does", "did", "have", "has", "had", "what", "which", "who", "whom",
        "how", "when", "where", "why", "all", "any", "some", "not", "there", "here",
        "you", "he", "she", "we", "they", "me", "him", "her", "us", "them", "my",
        "your", "his", "our", "their", "about", "into", "over", "under", "again", "more"
    };

    public static string? Build(string? query)
    {
        var lexemes = ExtractLexemes(query);

        return lexemes.Count == 0 ? null : string.Join(" | ", lexemes);
    }

    public static IReadOnlyList<string> ExtractLexemes(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var normalized = RemoveDiacritics(query).ToLowerInvariant();
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            builder.Append(char.IsLetterOrDigit(character) ? character : ' ');
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var lexemes = new List<string>();

        foreach (var token in builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (token.Length <= 1 || StopWords.Contains(token))
            {
                continue;
            }

            if (seen.Add(token))
            {
                lexemes.Add(token);
            }
        }

        return lexemes;
    }

    private static string RemoveDiacritics(string text)
    {
        var decomposed = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(character switch
            {
                'đ' => 'd',
                'Đ' => 'D',
                _ => character
            });
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
