namespace IChat.Core.UnitTests.Auth;

using IChat.Core.Services.Auth;
using FluentAssertions;
using NUnit.Framework;

[TestFixture]
public class RefreshTokenFactoryTests
{
    [Test]
    public void CreateRawToken_IsUrlSafeAndUnique()
    {
        // Arrange
        const int sampleSize = 100;

        // Act
        var tokens = Enumerable.Range(0, sampleSize).Select(_ => RefreshTokenFactory.CreateRawToken()).ToList();

        // Assert
        tokens.Should().OnlyHaveUniqueItems();
        // The token travels in a cookie, so it must not contain characters that need escaping.
        tokens.Should().OnlyContain(token => token.All(character =>
            char.IsAsciiLetterOrDigit(character) || character == '-' || character == '_'));
    }

    [Test]
    public void Hash_IsDeterministicAndHidesTheToken()
    {
        // Arrange
        var token = RefreshTokenFactory.CreateRawToken();
        var otherToken = RefreshTokenFactory.CreateRawToken();

        // Act
        var hash = RefreshTokenFactory.Hash(token);

        // Assert
        hash.Should().Be(RefreshTokenFactory.Hash(token));
        hash.Should().HaveLength(64).And.NotContain(token);
        hash.Should().NotBe(RefreshTokenFactory.Hash(otherToken));
    }
}
