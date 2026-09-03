namespace IChat.Infrastructure.Ai;

using System.ComponentModel.DataAnnotations;

public sealed class AiOptions
{
    public const string SectionName = "Ai";

    [Required]
    public ChatProviderOptions Chat { get; set; } = new();

    [Required]
    public UtilityChatOptions UtilityChat { get; set; } = new();

    [Required]
    public EmbeddingOptions Embedding { get; set; } = new();

    public PipelineOptions Pipeline { get; set; } = new();

    /// <summary>Allowlist cho field `model` tùy chọn trong body request.</summary>
    public IList<string> AllowedChatModels { get; set; } = [];

    public ResolvedChatSettings ResolveChat()
    {
        return new ResolvedChatSettings
        {
            Provider = Chat.Provider,
            Model = Chat.Model,
            MaxOutputTokens = Chat.MaxOutputTokens,
            TimeoutSeconds = Chat.TimeoutSeconds,
            Endpoint = Chat.Endpoint,
            ApiKey = Chat.ApiKey
        };
    }

    /// <summary>
    /// Mọi field bỏ trống của UtilityChat đều kế thừa từ Chat, nên cấu hình utility chỉ
    /// cần khai báo đúng phần khác biệt (thường là model rẻ hơn). Đây là nơi DUY NHẤT
    /// định nghĩa quy tắc kế thừa đó: client factory, catalog cho admin và validator lúc
    /// startup đều phải đi qua đây, nếu không ba nơi sẽ mô tả cùng một hệ thống theo ba
    /// kiểu khác nhau — và admin endpoint sẽ báo "thiếu key" cho provider đang chạy tốt.
    /// Model và MaxOutputTokens KHÔNG kế thừa: utility luôn phải tự khai báo model của nó.
    /// </summary>
    public ResolvedChatSettings ResolveUtilityChat()
    {
        return new ResolvedChatSettings
        {
            Provider = UtilityChat.Provider ?? Chat.Provider,
            Model = UtilityChat.Model,
            MaxOutputTokens = UtilityChat.MaxOutputTokens,
            TimeoutSeconds = UtilityChat.TimeoutSeconds,
            Endpoint = UtilityChat.Endpoint ?? Chat.Endpoint,
            ApiKey = UtilityChat.ApiKey ?? Chat.ApiKey
        };
    }
}
