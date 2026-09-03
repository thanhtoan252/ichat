namespace IChat.Infrastructure.Search;

using System.Globalization;
using System.Text;

/// <summary>
/// Đây là chỗ dễ sai nhất trong cả hệ thống.
///
/// plainto_tsquery và websearch_to_tsquery đều nối lexeme bằng toán tử &amp;. Câu hỏi thật
/// dài 10-15 từ, nghĩa là truy vấn đòi một chunk chứa ĐỦ CẢ 15 lexeme — xác suất gần bằng
/// không. Hậu quả: nhánh full-text trả rỗng gần như mọi lúc, hệ thống vẫn chạy, vẫn trả lời,
/// và người vận hành tưởng hybrid search đang hoạt động trong khi thực chất chỉ có vector.
///
/// Vì vậy phải tự dựng tsquery kiểu OR. Chuẩn hoá ở đây phải khớp với
/// to_tsvector('simple', immutable_unaccent(...)) dùng lúc index.
/// </summary>
public static class TsQueryBuilder
{
    /// <summary>
    /// Config 'simple' của PostgreSQL không loại stopword, nên phải tự lọc. Không lọc thì
    /// "của" và "là" khớp mọi chunk và ts_rank_cd mất hết ý nghĩa phân biệt.
    /// </summary>
    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
    {
        // Tiếng Việt (đã bỏ dấu). Danh sách này cố tình KHÔNG chứa các từ mà sau khi
        // unaccent bị trùng với từ nội dung phổ biến trong tài liệu kỹ thuật:
        //   moi  <- "môi trường",  tai <- "tài liệu",  nen <- "nền tảng",
        //   ai   <- "AI",          dau <- "đầu vào",   ban <- "bản ghi".
        // Giữ lại một lexeme yếu vẫn tốt hơn là đánh rơi một từ nội dung.
        "va", "la", "cua", "cac", "mot", "cho", "voi", "trong", "den", "tu", "khi", "nay",
        "khong", "duoc", "nhu", "ve", "thi", "ma", "neu", "hay", "hoac", "nhung", "cung",
        "se", "dang", "phai", "vi", "de", "theo", "tren", "duoi", "sau", "truoc", "giua",
        "nhieu", "lam", "the", "nao", "gi", "sao", "toi", "roi", "nhe", "a", "u",
        // Tiếng Anh
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

        // Chỉ giữ chữ và số: mọi ký tự đặc biệt (| & ! ( ) ' " <->) đều bị loại,
        // nên chuỗi kết quả không bao giờ làm hỏng cú pháp tsquery.
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

    /// <summary>Tương đương unaccent() của PostgreSQL, kể cả ánh xạ đ -> d của tiếng Việt.</summary>
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

            // đ/Đ không phải tổ hợp dấu nên FormD không tách được; unaccent map nó thành d.
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
