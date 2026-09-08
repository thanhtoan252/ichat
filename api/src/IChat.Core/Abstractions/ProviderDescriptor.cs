namespace IChat.Core.Abstractions;

public sealed class ProviderDescriptor
{
    public required string Kind { get; init; }

    public required string Provider { get; init; }

    public required string Model { get; init; }

    public required bool Available { get; init; }

    public string? Reason { get; init; }
}
