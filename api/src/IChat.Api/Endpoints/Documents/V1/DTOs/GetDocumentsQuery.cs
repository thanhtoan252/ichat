namespace IChat.Api.Endpoints.Documents.V1.DTOs;

using Microsoft.AspNetCore.Mvc;

public sealed class GetDocumentsQuery(
    [FromQuery(Name = "status")] DocumentStatusFilter? status = null,
    [FromQuery(Name = "offset")] int offset = 0,
    [FromQuery(Name = "limit")] int limit = 20)
{
    public DocumentStatusFilter? Status { get; } = status;

    public int Offset { get; } = Math.Max(offset, 0);

    // Asking for too many is clamped to the ceiling rather than rejected with a 400; the response repeats the limit actually used.
    public int Limit { get; } = limit <= 0 ? 20 : Math.Min(limit, 100);
}
