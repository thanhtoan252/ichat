namespace IChat.Core.Contracts.Documents;

public sealed class UploadDocumentResult
{
    public required Guid Id { get; init; }

    public required string Title { get; init; }

    public required string Status { get; init; }
}
