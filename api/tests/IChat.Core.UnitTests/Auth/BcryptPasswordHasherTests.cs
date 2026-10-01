namespace IChat.Core.UnitTests.Auth;

using IChat.Infrastructure.Security;
using FluentAssertions;
using NUnit.Framework;

[TestFixture]
public class BcryptPasswordHasherTests
{
    private BcryptPasswordHasher _hasher = null!;

    [SetUp]
    public void SetUp()
    {
        _hasher = new BcryptPasswordHasher();
    }

    [Test]
    public void Hash_ProducesADifferentValueEveryTime()
    {
        // Arrange
        const string password = "admin";

        // Act
        var first = _hasher.Hash(password);
        var second = _hasher.Hash(password);

        // Assert
        // Random salt: two different hashes that both verify. If they were equal, the users table would reveal
        // who shares a password with whom.
        first.Should().NotBe(second);
        _hasher.Verify(password, first).Should().BeTrue();
        _hasher.Verify(password, second).Should().BeTrue();
    }

    [Test]
    public void Verify_WithWrongPassword_ReturnsFalse()
    {
        // Arrange
        var hash = _hasher.Hash("admin");

        // Act
        var verified = _hasher.Verify("khong-phai", hash);

        // Assert
        verified.Should().BeFalse();
    }

    [Test]
    public void Verify_WithCorruptedHash_ReturnsFalseInsteadOfThrowing()
    {
        // Arrange
        const string corruptedHash = "khong-phai-mot-bcrypt-hash";

        // Act
        var verified = _hasher.Verify("admin", corruptedHash);

        // Assert
        // A corrupt hash in the database has to come out as "wrong password", not as a 500.
        verified.Should().BeFalse();
    }
}
