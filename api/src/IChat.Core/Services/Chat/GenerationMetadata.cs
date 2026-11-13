namespace IChat.Core.Services.Chat;

public sealed record GenerationMetadata
{
    public required string Provider { get; init; }

    public string? Model { get; init; }

    public required long LatencyMs { get; init; }

    public required long RetrievalMs { get; init; }

    public required bool Degraded { get; init; }
}
