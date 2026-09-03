namespace IChat.Infrastructure.Ai.Providers;

using Microsoft.Extensions.AI;

/// <summary>
/// Một hãng một implementation. Khác với <see cref="IChatClientFactory"/> — cái đó là CỬA VÀO:
/// đọc config, chọn ra hãng nào, rồi uỷ quyền xuống đây.
/// Chỉ trả client THÔ: middleware vẫn gắn một lần ở tầng đăng ký DI.
/// </summary>
public interface IChatProviderClientFactory
{
    ChatProvider Provider { get; }

    IChatClient Create(ResolvedChatSettings settings);
}
