namespace IChat.Infrastructure.Ai;

/// <summary>
/// Cấu hình của một chat provider SAU KHI đã áp dụng fallback UtilityChat -> Chat.
/// Đủ để dựng client, mô tả trạng thái hoặc validate mà không phải đọc lại AiOptions.
/// </summary>
public sealed record ResolvedChatSettings
{
    public required ChatProvider Provider { get; init; }

    public required string Model { get; init; }

    public required int MaxOutputTokens { get; init; }

    public required int TimeoutSeconds { get; init; }

    public string? Endpoint { get; init; }

    public string? ApiKey { get; init; }

    public ChatProviderCapabilities Capabilities => ChatProviderCapabilities.For(Provider);

    /// <summary>Tên hiển thị trong thông báo lỗi; trùng khớp tên nhánh trong config.</summary>
    public string Label => Provider.ToString();

    /// <summary>
    /// AiOptionsValidator đã chặn thiếu key từ lúc startup; đây là lưới an toàn cho những
    /// đường chạy không đi qua ValidateOnStart (test, tooling).
    /// </summary>
    public string RequireApiKey()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            throw new InvalidOperationException(
                $"Missing API key for {Label}. Set Ai__Chat__ApiKey (or Ai__UtilityChat__ApiKey) " +
                "as an environment variable, in user secrets, or in a secret store.");
        }

        return ApiKey;
    }
}
