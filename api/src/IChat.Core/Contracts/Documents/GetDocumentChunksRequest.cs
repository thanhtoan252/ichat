namespace IChat.Core.Contracts.Documents;

public sealed class GetDocumentChunksRequest
{
    public required Guid DocumentId { get; init; }

    public required int Offset { get; init; }

    public required int Limit { get; init; }
}
