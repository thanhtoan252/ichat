namespace IChat.Core.Contracts.Documents;

public sealed class GetDocumentChunksRequest
{
    public required Guid DocumentId { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 50;
}
