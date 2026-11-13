namespace IChat.Core.Abstractions;

public interface ITokenEstimator
{
    int Estimate(string? text);
}
