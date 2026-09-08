namespace IChat.Core.Contracts.Auth;

public sealed class AccessToken
{
    public required string Value { get; init; }

    public required DateTimeOffset ExpiresAt { get; init; }
}
