namespace IChat.Core.Contracts.Conversations;

/// <summary>
/// The values of <see cref="StatusPayload.Stage"/> — the client uses them to show progress while
/// waiting. A published contract.
/// </summary>
public static class AnswerStage
{
    public const string Rewriting = "rewriting";

    public const string Retrieving = "retrieving";

    public const string Generating = "generating";
}
