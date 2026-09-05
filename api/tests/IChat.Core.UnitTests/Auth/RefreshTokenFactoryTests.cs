namespace IChat.Core.UnitTests.Auth;

using IChat.Core.Services.Auth;
using FluentAssertions;
using Xunit;

public class RefreshTokenFactoryTests
{
    [Fact]
    public void CreateRawToken_IsUrlSafeAndUnique()
    {
        var tokens = Enumerable.Range(0, 100).Select(_ => RefreshTokenFactory.CreateRawToken()).ToList();

        tokens.Should().OnlyHaveUniqueItems();
        // Token đi trong cookie nên không được chứa ký tự phải escape.
        tokens.Should().OnlyContain(token => token.All(character =>
            char.IsAsciiLetterOrDigit(character) || character == '-' || character == '_'));
    }

    [Fact]
    public void Hash_IsDeterministicAndHidesTheToken()
    {
        var token = RefreshTokenFactory.CreateRawToken();

        var hash = RefreshTokenFactory.Hash(token);

        hash.Should().Be(RefreshTokenFactory.Hash(token));
        hash.Should().HaveLength(64).And.NotContain(token);
        hash.Should().NotBe(RefreshTokenFactory.Hash(RefreshTokenFactory.CreateRawToken()));
    }
}
