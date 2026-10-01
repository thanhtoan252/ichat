namespace IChat.Infrastructure.Security;

using IChat.Core.Abstractions;

/// <summary>
/// BCrypt with work factor 12: slow enough to resist offline cracking, still under ~250ms on ordinary
/// hardware so a login does not feel sluggish.
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
            // A corrupt hash in the database must not throw a 500; treat it as a wrong password.
            return false;
        }
    }
}
