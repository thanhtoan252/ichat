namespace IChat.Core.Abstractions;

/// <summary>
/// Estimates tokens locally without making an extra OpenAI API call.
/// </summary>
public interface ITokenEstimator
{
    int Estimate(string? text);
}
