namespace IChat.Infrastructure.Ai;

using Microsoft.Extensions.AI;

public interface IChatClientFactory
{
    IChatClient Create();

IChatClient CreateUtility();

}
