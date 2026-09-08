namespace IChat.Infrastructure.Ai;

using Microsoft.Extensions.AI;

/// <summary>
/// Chỉ trả về client THÔ. Middleware (caching, telemetry, logging, function invocation)
/// gắn một lần ở tầng đăng ký DI để mọi provider hành xử y hệt nhau.
/// </summary>
public interface IChatClientFactory
{
    IChatClient Create();

    IChatClient CreateUtility();

    ChatProviderCapabilities Capabilities { get; }

    ChatProviderCapabilities UtilityCapabilities { get; }
}
