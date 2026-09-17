namespace IChat.Core.Domain.Identity;

/// <summary>
/// The system's only two roles. Stored in the database as strings (UserConfiguration),
/// so the declaration order is not part of the contract.
/// </summary>
public enum UserRole
{
    User = 0,
    Admin = 1
}
