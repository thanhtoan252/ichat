namespace IChat.Api.Endpoints.Documents.V1.DTOs;

public sealed class UploadDocumentResponse
{
    public required Guid Id { get; init; }

    public required string Title { get; init; }

    public required string Status { get; init; }
}
