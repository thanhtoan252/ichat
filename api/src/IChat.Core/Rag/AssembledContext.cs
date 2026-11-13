namespace IChat.Core.Rag;

public sealed class AssembledContext
{
    public required IReadOnlyList<ContextSource> Sources { get; init; }

    public required string RenderedContext { get; init; }

    public bool HasContext => Sources.Count > 0;

    public static AssembledContext Empty { get; } = new()
    {
        Sources = [],
        RenderedContext = string.Empty
    };
}
