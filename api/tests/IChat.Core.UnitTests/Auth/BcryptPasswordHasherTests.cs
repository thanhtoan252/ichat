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
        // Salt ngẫu nhiên: hai hash khác nhau nhưng cùng verify được. Nếu chúng bằng nhau
        // thì bảng users đã lộ ra ai đang dùng chung mật khẩu.
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
        // Hash hỏng trong database phải ra "sai mật khẩu", không phải 500.
        verified.Should().BeFalse();
    }
}
