namespace IChat.Core.Abstractions;

/// <summary>
/// The identity of the current request, read from the validated access token. The service layer
/// may only take the owner from here — never from a body or a query string.
///
/// Deliberately only an Id: role decisions belong to the policy at the endpoint layer
/// (<c>RequireAuthorization(AuthPolicies.Admin)</c>), so business code never has to ask
/// "is this an admin?" and should not even have a place to ask it.
/// </summary>
public interface ICurrentUser
{
    Guid? Id { get; }
}
