namespace IChat.Core.Contracts.Documents;

using IChat.Core.Domain.Documents;

public sealed class GetDocumentsRequest
{
    public DocumentStatus? Status { get; init; }

    public required int Offset { get; init; }

    public required int Limit { get; init; }
}
