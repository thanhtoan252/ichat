namespace IChat.Core.Abstractions;

using IChat.Core.Rag;

/// <summary>Một nhánh tìm kiếm độc lập. Thêm nhánh mới = thêm một implementation + một dòng DI.</summary>
public interface IRetrievalBranch
{
    /// <summary>Khoá stage trả về cho Retrieval Lab; lấy từ RetrievalStageName.</summary>
    string StageName { get; }

    /// <summary>Sai khi mode không chọn nhánh này, hoặc khi config đã tắt nó (TrigramCandidates = 0).</summary>
    bool IsEnabledFor(SearchMode mode);

    Task<SearchBranchResult> SearchAsync(string query, CancellationToken cancellationToken);
}
