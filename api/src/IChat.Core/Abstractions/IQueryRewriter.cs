namespace IChat.Core.Abstractions;

using Microsoft.Extensions.AI;

public interface IQueryRewriter
{
    /// <summary>Never throws: on failure it falls back to the original question.</summary>
    Task<string> RewriteAsync(string originalQuestion, IReadOnlyList<ChatMessage> history, CancellationToken cancellationToken);
}
