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

        const int sampleSize = 100;

        var tokens = Enumerable.Range(0, sampleSize).Select(_ => RefreshTokenFactory.CreateRawToken()).ToList();

        tokens.Should().OnlyHaveUniqueItems();

        tokens.Should().OnlyContain(token => token.All(character =>
                    char.IsAsciiLetterOrDigit(character) || character == '-' || character == '_'));
    }

    [Test]
    public void Hash_IsDeterministicAndHidesTheToken()
    {

        var token = RefreshTokenFactory.CreateRawToken();
        var otherToken = RefreshTokenFactory.CreateRawToken();

        var hash = RefreshTokenFactory.Hash(token);

        hash.Should().Be(RefreshTokenFactory.Hash(token));
        hash.Should().HaveLength(64).And.NotContain(token);
        hash.Should().NotBe(RefreshTokenFactory.Hash(otherToken));
    }
}
