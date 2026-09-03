namespace IChat.Core.Contracts.Conversations;

/// <summary>
/// Giá trị của <see cref="ErrorPayload.Code"/>. Cố tình tách khỏi <c>Error.Code</c> của
/// tầng service: lỗi trong luồng SSE đi kèm HTTP 200 nên client phân nhánh trên bộ mã
/// riêng, ngắn hơn và không mang tên entity.
/// </summary>
public static class SseErrorCode
{
    public const string Validation = "Validation";

    public const string NotFound = "NotFound";

    public const string External = "External";
}
