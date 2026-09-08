namespace IChat.Infrastructure.Security;

using IChat.Core.Abstractions;

/// <summary>
/// BCrypt với work factor 12: đủ chậm để chống dò offline, vẫn dưới ~250ms trên
/// phần cứng thường nên không cảm nhận được khi đăng nhập.
/// </summary>
public sealed class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string passwordHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Hash hỏng trong database không được ném ra 500; coi như sai mật khẩu.
            return false;
        }
    }
}
