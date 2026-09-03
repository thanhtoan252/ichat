namespace IChat.Api.Endpoints.Search.V1.DTOs;

/// <summary>
/// Trả kết quả TỪNG CHẶNG và tsquery đã dựng là điểm mấu chốt: đa số thời gian debug RAG
/// là nhìn xem chunk rơi rụng ở chặng nào, chứ không phải sửa prompt.
/// </summary>
public sealed class SearchResponse
{
    public required string OriginalQuery { get; init; }

    public required string RewrittenQuery { get; init; }

    public required IReadOnlyDictionary<string, SearchStageResponse> Stages { get; init; }

    public required bool Degraded { get; init; }

    public required long ElapsedMs { get; init; }
}
