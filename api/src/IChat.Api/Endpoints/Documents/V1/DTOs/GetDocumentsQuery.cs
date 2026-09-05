namespace IChat.Api.Endpoints.Documents.V1.DTOs;

using Microsoft.AspNetCore.Mvc;

public sealed class GetDocumentsQuery(
    [FromQuery(Name = "status")] DocumentStatusFilter? status = null,
    [FromQuery(Name = "offset")] int offset = 0,
    [FromQuery(Name = "limit")] int limit = 20)
{
    public DocumentStatusFilter? Status { get; } = status;

    public int Offset { get; } = Math.Max(offset, 0);

    // Xin quá nhiều thì bị cắt về trần chứ không phải 400; phản hồi nói lại limit thật sự đã dùng.
    public int Limit { get; } = limit <= 0 ? 20 : Math.Min(limit, 100);
}
