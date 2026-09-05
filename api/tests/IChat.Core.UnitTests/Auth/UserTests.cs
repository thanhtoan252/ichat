namespace IChat.Core.UnitTests.Auth;

using IChat.Core.Domain.Identity;
using FluentAssertions;
using Xunit;

public class UserTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 4, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("Admin", "admin")]
    [InlineData("  Alice  ", "alice")]
    [InlineData("BOB", "bob")]
    public void Create_NormalisesTheUserName(string input, string expected)
    {
        // Chuẩn hoá một chỗ duy nhất: nếu nơi ghi và nơi tra cứu lệch nhau thì unique
        // index vẫn cho phép "Alice" và "alice" cùng tồn tại.
        User.Create(input, "Ai đó", "hash", UserRole.User, Now).UserName.Should().Be(expected);
        User.Normalize(input).Should().Be(expected);
    }

    [Fact]
    public void Create_StartsActive()
    {
        User.Create("alice", "Alice", "hash", UserRole.User, Now).IsActive.Should().BeTrue();
    }

    [Fact]
    public void ChangeRole_TouchesUpdatedAt()
    {
        var user = User.Create("alice", "Alice", "hash", UserRole.User, Now);

        user.ChangeRole(UserRole.Admin, Now.AddHours(1));

        user.Role.Should().Be(UserRole.Admin);
        user.UpdatedAt.Should().Be(Now.AddHours(1));
        user.CreatedAt.Should().Be(Now);
    }
}
