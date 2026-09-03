namespace IChat.Infrastructure.Ai;

using System.ComponentModel.DataAnnotations;

public sealed class ChatProviderOptions
{
    public ChatProvider Provider { get; set; } = ChatProvider.OpenAI;

    /// <summary>Không bao giờ có giá trị mặc định trong C# — model ID của mọi hãng đổi liên tục.</summary>
    [Required(AllowEmptyStrings = false)]
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Không có giá trị mặc định: các model dòng reasoning chỉ chấp nhận temperature
    /// mặc định của chính hãng và trả HTTP 400 nếu nhận giá trị khác. Bỏ trống nghĩa là
    /// không gửi tham số này đi, để hãng tự quyết định.
    /// </summary>
    [Range(0d, 2d)]
    public double? Temperature { get; set; }

    // Anthropic bắt buộc phải có MaxOutputTokens, OpenAI thì tùy chọn.
    // Luôn set cho mọi provider để không phải nhớ khác biệt này.
    [Range(1, 200_000)]
    public int MaxOutputTokens { get; set; } = 2048;

    [Range(1, 600)]
    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>Bắt buộc với AzureOpenAI.</summary>
    public string? Endpoint { get; set; }

    /// <summary>Bind thẳng qua IConfiguration: env var Ai__Chat__ApiKey, user secrets, hoặc secret store.</summary>
    public string? ApiKey { get; set; }
}
