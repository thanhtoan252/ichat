namespace IChat.Core.Abstractions;

/// <summary>
/// Ước lượng token không phụ thuộc tokenizer của một hãng, vì hệ thống hỗ trợ nhiều hãng
/// và mỗi hãng tokenize khác nhau.
/// </summary>
public interface ITokenEstimator
{
    int Estimate(string? text);
}
