namespace IChat.Core.Abstractions;

/// <summary>
/// Danh tính của request hiện tại, đọc từ access token đã được xác thực. Service layer
/// chỉ được lấy chủ sở hữu từ đây — không bao giờ từ body hay query.
///
/// Cố tình chỉ có Id: việc phân vai trò do policy ở tầng endpoint quyết định
/// (<c>RequireAuthorization(AuthPolicies.Admin)</c>), nên nghiệp vụ không cần hỏi
/// "người này có phải admin không" và cũng không nên có chỗ để hỏi.
/// </summary>
public interface ICurrentUser
{
    Guid? Id { get; }
}
