namespace IChat.Core.Contracts.Documents;

using IChat.Core.Domain.Documents;

public sealed class GetDocumentsRequest
{
    public DocumentStatus? Status { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}
