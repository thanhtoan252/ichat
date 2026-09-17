namespace IChat.Core.Abstractions;

/// <summary>
/// Estimates tokens without depending on any one vendor's tokenizer, because the system supports
/// several vendors and each of them tokenizes differently.
/// </summary>
public interface ITokenEstimator
{
    int Estimate(string? text);
}
