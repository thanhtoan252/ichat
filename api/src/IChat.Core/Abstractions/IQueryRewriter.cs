namespace IChat.Core.Abstractions;

using Microsoft.Extensions.AI;

public interface IQueryRewriter
{

Task<string> RewriteAsync(string originalQuestion, IReadOnlyList<ChatMessage> history, CancellationToken cancellationToken);
}
