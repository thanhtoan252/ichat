namespace IChat.Core.Abstractions;

public interface ICurrentUser
{
    Guid? Id { get; }
}
