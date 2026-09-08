namespace IChat.Core.Contracts.Conversations;

public sealed class SseEvent
{
    public required string EventType { get; init; }

    public required object Payload { get; init; }

    public static SseEvent Status(string stage)
    {
        return new SseEvent
        {
            EventType = SseEventType.Status,
            Payload = new StatusPayload { Stage = stage }
        };
    }

    public static SseEvent Sources(IReadOnlyList<SourceView> sources)
    {
        return new SseEvent
        {
            EventType = SseEventType.Sources,
            Payload = new SourcesPayload { Sources = sources }
        };
    }

    public static SseEvent Delta(string text)
    {
        return new SseEvent
        {
            EventType = SseEventType.Delta,
            Payload = new DeltaPayload { Text = text }
        };
    }

    public static SseEvent Done(DonePayload payload)
    {
        return new SseEvent
        {
            EventType = SseEventType.Done,
            Payload = payload
        };
    }

    public static SseEvent Failure(ErrorPayload error)
    {
        return new SseEvent
        {
            EventType = SseEventType.Error,
            Payload = error
        };
    }

    public static SseEvent Failure(string code, string message)
    {
        return Failure(new ErrorPayload { Code = code, Message = message });
    }
}
