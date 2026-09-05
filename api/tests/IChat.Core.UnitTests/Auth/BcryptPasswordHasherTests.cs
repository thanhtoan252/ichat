namespace IChat.Core.UnitTests.Auth;

using IChat.Infrastructure.Security;
using FluentAssertions;
using Xunit;

public class BcryptPasswordHasherTests
{
    private readonly BcryptPasswordHasher _hasher = new();

    [Fact]
    public void Hash_ProducesADifferentValueEveryTime()
    {
        var first = _hasher.Hash("admin");
        var second = _hasher.Hash("admin");

        // Salt ngẫu nhiên: hai hash khác nhau nhưng cùng verify được. Nếu chúng bằng nhau
        // thì bảng users đã lộ ra ai đang dùng chung mật khẩu.
        first.Should().NotBe(second);
        _hasher.Verify("admin", first).Should().BeTrue();
        _hasher.Verify("admin", second).Should().BeTrue();
    }

    [Fact]
    public void Verify_WithWrongPassword_ReturnsFalse()
    {
        _hasher.Verify("khong-phai", _hasher.Hash("admin")).Should().BeFalse();
    }

    [Fact]
    public void Verify_WithCorruptedHash_ReturnsFalseInsteadOfThrowing()
    {
        // Hash hỏng trong database phải ra "sai mật khẩu", không phải 500.
        _hasher.Verify("admin", "khong-phai-mot-bcrypt-hash").Should().BeFalse();
    }
}
