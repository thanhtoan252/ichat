namespace IChat.Infrastructure.Ai;

using IChat.Core.Abstractions;

/// <summary>
/// Approximates ~4 characters per token for Latin text and ~2 for Vietnamese with diacritics.
/// It uses a conservative estimate instead of making another OpenAI API call for token counting.
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
