namespace IChat.Infrastructure.Ai;

using IChat.Core.Abstractions;

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
