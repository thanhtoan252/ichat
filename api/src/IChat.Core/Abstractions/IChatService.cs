namespace IChat.Core.Abstractions;

using IChat.Core.Contracts.Conversations;

public interface IChatService
{
    /// <summary>
    /// Sinh câu trả lời RAG và phát ra chuỗi SSE event: status, sources, delta, done.
    /// Huỷ cancellationToken giữa chừng vẫn lưu phần đã sinh kèm ghi chú [interrupted].
    /// </summary>
    IAsyncEnumerable<SseEvent> StreamAnswerAsync(SendMessageRequest request, CancellationToken cancellationToken);
}
