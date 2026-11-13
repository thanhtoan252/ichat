namespace IChat.Api.Endpoints.Admin.V1.DTOs;

public sealed class ProviderResponse
{
    public required string Kind { get; init; }

    public required string Provider { get; init; }

    public required string Model { get; init; }

    public required bool Available { get; init; }

    public string? Reason { get; init; }
}
