namespace IChat.Core.UnitTests.Auth;

using IChat.Core.Domain.Identity;
using FluentAssertions;
using NUnit.Framework;

[TestFixture]
public class UserTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 4, 12, 0, 0, TimeSpan.Zero);

    [TestCase("Admin", "admin")]
    [TestCase("  Alice  ", "alice")]
    [TestCase("BOB", "bob")]
    public void Create_NormalisesTheUserName(string input, string expected)
    {
        // Arrange & Act
        var user = User.Create(input, "Ai đó", "hash", UserRole.User, Now);
        var normalized = User.Normalize(input);

        // Assert
        // Chuẩn hoá một chỗ duy nhất: nếu nơi ghi và nơi tra cứu lệch nhau thì unique
        // index vẫn cho phép "Alice" và "alice" cùng tồn tại.
        user.UserName.Should().Be(expected);
        normalized.Should().Be(expected);
    }

    [Test]
    public void Create_StartsActive()
    {
        // Arrange & Act
        var user = User.Create("alice", "Alice", "hash", UserRole.User, Now);

        // Assert
        user.IsActive.Should().BeTrue();
    }

    [Test]
    public void ChangeRole_TouchesUpdatedAt()
    {
        // Arrange
        var user = User.Create("alice", "Alice", "hash", UserRole.User, Now);

        // Act
        user.ChangeRole(UserRole.Admin, Now.AddHours(1));

        // Assert
        user.Role.Should().Be(UserRole.Admin);
        user.UpdatedAt.Should().Be(Now.AddHours(1));
        user.CreatedAt.Should().Be(Now);
    }
}
