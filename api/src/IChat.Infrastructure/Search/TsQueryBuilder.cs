namespace IChat.Infrastructure.Search;

using System.Globalization;
using System.Text;

/// <summary>
/// This is the easiest place in the whole system to get wrong.
///
/// Both plainto_tsquery and websearch_to_tsquery join lexemes with the &amp; operator. A real question is
/// 10-15 words long, which means the query demands a chunk containing ALL 15 lexemes — a near-zero chance.
/// The consequence: the full-text branch returns empty almost every time, the system still runs, still
/// answers, and whoever operates it believes hybrid search is working when in fact only vector search is.
///
/// That is why the tsquery is built by hand as an OR query. The normalization here has to match the
/// to_tsvector('simple', immutable_unaccent(...)) used at index time.
/// </summary>
public static class TsQueryBuilder
{
    /// <summary>
    /// PostgreSQL's 'simple' configuration does not strip stop words, so they are filtered here. Without that,
    /// "của" and "là" match every chunk and ts_rank_cd loses all discriminating power.
    /// </summary>
    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
    {
        // Vietnamese (already unaccented). This list deliberately EXCLUDES words that, once unaccented, collide
        // with content words common in technical documents:
        //   moi  <- "môi trường",  tai <- "tài liệu",  nen <- "nền tảng",
        //   ai   <- "AI",          dau <- "đầu vào",   ban <- "bản ghi".
        // Keeping one weak lexeme is still better than dropping a content word.
        "va", "la", "cua", "cac", "mot", "cho", "voi", "trong", "den", "tu", "khi", "nay",
        "khong", "duoc", "nhu", "ve", "thi", "ma", "neu", "hay", "hoac", "nhung", "cung",
        "se", "dang", "phai", "vi", "de", "theo", "tren", "duoi", "sau", "truoc", "giua",
        "nhieu", "lam", "the", "nao", "gi", "sao", "toi", "roi", "nhe", "a", "u",
        // English
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

        // Keep letters and digits only: every special character (| & ! ( ) ' " <->) is stripped, so the
        // resulting string can never break the tsquery syntax.
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

    /// <summary>The equivalent of PostgreSQL's unaccent(), including the Vietnamese đ -> d mapping.</summary>
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

            // đ/Đ is not a combining sequence, so FormD cannot decompose it; unaccent maps it to d.
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
