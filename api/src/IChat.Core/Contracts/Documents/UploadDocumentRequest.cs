namespace IChat.Core.Contracts.Documents;

public sealed class UploadDocumentRequest
{
    public required Stream Content { get; init; }

    public required string FileName { get; init; }

    public required string DeclaredContentType { get; init; }

    public required long SizeInBytes { get; init; }

    public string? Title { get; init; }
}
