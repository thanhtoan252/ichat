namespace IChat.Core.Domain.Identity;

public sealed class User
{
    private User()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>Định danh đăng nhập, luôn được chuẩn hoá về chữ thường trước khi lưu.</summary>
    public string UserName { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public UserRole Role { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static User Create(string userName, string displayName, string passwordHash, UserRole role, DateTimeOffset createdAt)
    {
        return new User
        {
            Id = Guid.CreateVersion7(),
            UserName = Normalize(userName),
            DisplayName = displayName.Trim(),
            PasswordHash = passwordHash,
            Role = role,
            IsActive = true,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }

    /// <summary>Chuẩn hoá một lần ở đây để nơi tra cứu và nơi ghi không lệch nhau.</summary>
    public static string Normalize(string userName) => userName.Trim().ToLowerInvariant();

    public void ChangeRole(UserRole role, DateTimeOffset updatedAt)
    {
        Role = role;
        UpdatedAt = updatedAt;
    }

    public void SetActive(bool isActive, DateTimeOffset updatedAt)
    {
        IsActive = isActive;
        UpdatedAt = updatedAt;
    }

    public void ChangePasswordHash(string passwordHash, DateTimeOffset updatedAt)
    {
        PasswordHash = passwordHash;
        UpdatedAt = updatedAt;
    }
}
