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

const string password = "admin";

var first = _hasher.Hash(password);
        var second = _hasher.Hash(password);

first.Should().NotBe(second);
        _hasher.Verify(password, first).Should().BeTrue();
        _hasher.Verify(password, second).Should().BeTrue();
    }

[Test]
    public void Verify_WithWrongPassword_ReturnsFalse()
    {

var hash = _hasher.Hash("admin");

var verified = _hasher.Verify("khong-phai", hash);

verified.Should().BeFalse();
    }

[Test]
    public void Verify_WithCorruptedHash_ReturnsFalseInsteadOfThrowing()
    {

const string corruptedHash = "khong-phai-mot-bcrypt-hash";

var verified = _hasher.Verify("admin", corruptedHash);

verified.Should().BeFalse();
    }
}
