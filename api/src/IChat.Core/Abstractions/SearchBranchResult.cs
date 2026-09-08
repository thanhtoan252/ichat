namespace IChat.Core.Abstractions;

using IChat.Core.Rag;

public sealed class SearchBranchResult
{
    public required IReadOnlyList<RetrievalCandidate> Candidates { get; init; }

    public required long ElapsedMs { get; init; }

    public string? TsQuery { get; init; }

    /// <summary>
    /// Nhánh tự báo mình chạy trong trạng thái suy giảm (ví dụ embedding hỏng nên không
    /// tìm được gì), để pipeline không phải biết chi tiết bên trong từng nhánh.
    /// </summary>
    public bool Degraded { get; init; }
}
