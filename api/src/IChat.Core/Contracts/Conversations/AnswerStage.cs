namespace IChat.Core.Contracts.Conversations;

/// <summary>
/// Giá trị của <see cref="StatusPayload.Stage"/> — client dùng để hiển thị tiến trình
/// trong lúc chờ. Contract đã công bố.
/// </summary>
public static class AnswerStage
{
    public const string Rewriting = "rewriting";

    public const string Retrieving = "retrieving";

    public const string Generating = "generating";
}
