namespace IChat.Core.Abstractions;

using Microsoft.Extensions.AI;

public interface IQueryRewriter
{
    /// <summary>Không bao giờ ném lỗi: hỏng thì fallback về câu hỏi gốc.</summary>
    Task<string> RewriteAsync(string originalQuestion, IReadOnlyList<ChatMessage> history, CancellationToken cancellationToken);
}
