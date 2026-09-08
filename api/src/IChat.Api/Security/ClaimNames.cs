namespace IChat.Api.Security;

/// <summary>
/// Tên claim ngắn theo chuẩn JWT. Trước đây token dùng URI dài của <c>ClaimTypes</c>
/// (~160 byte chỉ riêng ba cái tên) và chúng đi kèm MỌI request. Đặt ở một chỗ vì bên ký
/// (<see cref="JwtAccessTokenService"/>), bên kiểm tra (AddJwtBearer) và bên đọc
/// (<see cref="HttpContextCurrentUser"/>) lệch nhau một ký tự là 401/403 im lặng.
/// </summary>
public static class ClaimNames
{
    public const string Sub = "sub";

    public const string Name = "name";

    public const string Role = "role";
}
