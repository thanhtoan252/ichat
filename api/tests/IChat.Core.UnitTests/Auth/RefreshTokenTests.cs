namespace IChat.Core.UnitTests.Auth;

using IChat.Core.Domain.Identity;
using FluentAssertions;
using Xunit;

public class RefreshTokenTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 4, 12, 0, 0, TimeSpan.Zero);

    private static RefreshToken Issue() =>
        RefreshToken.Issue(Guid.CreateVersion7(), "hash", Now, Now.AddDays(14));

    [Fact]
    public void AFreshToken_IsActive()
    {
        Issue().IsActiveAt(Now).Should().BeTrue();
    }

    [Fact]
    public void AnExpiredToken_IsNotActive()
    {
        Issue().IsActiveAt(Now.AddDays(15)).Should().BeFalse();
    }

    [Fact]
    public void ARevokedToken_IsNotActive_EvenBeforeItExpires()
    {
        var token = Issue();

        token.Revoke(Now.AddMinutes(1), replacedByTokenHash: "next-hash");

        token.IsActiveAt(Now.AddMinutes(2)).Should().BeFalse();
        token.ReplacedByTokenHash.Should().Be("next-hash");
    }
}
