namespace IChat.Api.Endpoints.Documents.V1.Validators;

using IChat.Api.Endpoints.Common;
using IChat.Api.Endpoints.Documents.V1.DTOs;

public sealed class GetDocumentChunksQueryValidator : PagedQueryValidator<GetDocumentChunksQuery>
{
    public GetDocumentChunksQueryValidator()
        : base(maxPageSize: 200)
    {
    }
}
