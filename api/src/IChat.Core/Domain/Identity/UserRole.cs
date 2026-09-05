namespace IChat.Core.Domain.Identity;

/// <summary>
/// Hai vai trò duy nhất của hệ thống. Lưu xuống database dưới dạng chuỗi
/// (UserConfiguration) nên thứ tự khai báo không phải là contract.
/// </summary>
public enum UserRole
{
    User = 0,
    Admin = 1
}
