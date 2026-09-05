namespace IChat.Core.UnitTests.Auth;

using IChat.Core.Domain.Identity;
using FluentAssertions;
using NUnit.Framework;

[TestFixture]
public class RefreshTokenTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 4, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public void IsActiveAt_AFreshToken_IsActive()
    {
        // Arrange
        var token = Issue();

        // Act
        var active = token.IsActiveAt(Now);

        // Assert
        active.Should().BeTrue();
    }

    [Test]
    public void IsActiveAt_AnExpiredToken_IsNotActive()
    {
        // Arrange
        var token = Issue();

        // Act
        var active = token.IsActiveAt(Now.AddDays(15));

        // Assert
        active.Should().BeFalse();
    }

    [Test]
    public void Revoke_ARevokedToken_IsNotActive_EvenBeforeItExpires()
    {
        // Arrange
        var token = Issue();

        // Act
        token.Revoke(Now.AddMinutes(1), replacedByTokenHash: "next-hash");

        // Assert
        token.IsActiveAt(Now.AddMinutes(2)).Should().BeFalse();
        token.ReplacedByTokenHash.Should().Be("next-hash");
    }

    private static RefreshToken Issue()
    {
        return RefreshToken.Issue(Guid.CreateVersion7(), "hash", Now, Now.AddDays(14));
    }
}
