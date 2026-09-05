namespace IChat.Api.Endpoints.Conversations.V1.DTOs;

using IChat.Api.Endpoints.Common;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Không còn lọc theo userId: danh sách luôn là hội thoại của chính người gọi
/// (admin thì thấy tất cả), quyết định bởi token chứ không bởi query string.
/// </summary>
public sealed class GetConversationsQuery(
    [FromQuery(Name = "offset")] int? offset,
    [FromQuery(Name = "limit")] int? limit)
{
    public int Offset { get; } = PagingQuery.ResolveOffset(offset);

    public int Limit { get; } = PagingQuery.ResolveLimit(limit, defaultLimit: 20, maxLimit: 100);
}
