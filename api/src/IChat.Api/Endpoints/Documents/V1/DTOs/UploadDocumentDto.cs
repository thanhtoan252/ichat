namespace IChat.Api.Endpoints.Documents.V1.DTOs;

public sealed class UploadDocumentDto
{
    public required IFormFile File { get; init; }

    public string? Title { get; init; }
}
