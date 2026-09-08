namespace IChat.Infrastructure.Ai.Providers;

using System.ClientModel;
using OpenAI;

/// <summary>
/// Đoạn dựng OpenAIClient kèm endpoint tuỳ chọn dùng chung cho chat OpenAI, chat Google và
/// cả hai embedding tương thích OpenAI — trước đây bị chép ba lần.
/// </summary>
public static class OpenAICompatibleClientFactory
{
    // Gemini đi qua endpoint OpenAI-compatible để không phải phụ thuộc một package beta;
    // đổi sang SDK Vertex chỉ cần sửa hai factory Google.
    public const string GoogleOpenAICompatibleEndpoint = "https://generativelanguage.googleapis.com/v1beta/openai/";

    public static OpenAIClient Create(string apiKey, string? endpoint)
    {
        var clientOptions = new OpenAIClientOptions();

        if (!string.IsNullOrWhiteSpace(endpoint))
        {
            clientOptions.Endpoint = new Uri(endpoint);
        }

        return new OpenAIClient(new ApiKeyCredential(apiKey), clientOptions);
    }
}
