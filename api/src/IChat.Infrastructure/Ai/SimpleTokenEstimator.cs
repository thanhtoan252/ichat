namespace IChat.Infrastructure.Ai;

using IChat.Core.Abstractions;

/// <summary>
/// Xấp xỉ ~4 ký tự/token cho văn bản latin và ~2 cho tiếng Việt có dấu.
/// Cố tình KHÔNG dùng tokenizer riêng của một hãng: hệ thống hỗ trợ nhiều hãng
/// và mỗi hãng tokenize khác nhau, nên một ước lượng chung ổn định hơn.
/// </summary>
public sealed class SimpleTokenEstimator : ITokenEstimator
{
    public int Estimate(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        var diacritics = 0;
        foreach (var character in text)
        {
            if (character > 127)
            {
                diacritics++;
            }
        }

        var nonLatinRatio = (double)diacritics / text.Length;
        var charactersPerToken = nonLatinRatio > 0.05 ? 2.0 : 4.0;

        return Math.Max(1, (int)Math.Ceiling(text.Length / charactersPerToken));
    }
}
