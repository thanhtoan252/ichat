namespace IChat.Core.Contracts.Conversations;

/// <summary>
/// The event names of the SSE channel. This is a published contract — clients branch on exactly
/// these strings, so changing one value here is a breaking change.
/// </summary>
public static class SseEventType
{
    public const string Status = "status";

    public const string Sources = "sources";

    public const string Delta = "delta";

    public const string Done = "done";

    public const string Error = "error";
}
