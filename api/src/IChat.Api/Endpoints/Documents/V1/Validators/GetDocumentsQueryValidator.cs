namespace IChat.Api.Endpoints.Documents.V1.Validators;

using IChat.Api.Endpoints.Common;
using IChat.Api.Endpoints.Documents.V1.DTOs;

public sealed class GetDocumentsQueryValidator : PagedQueryValidator<GetDocumentsQuery>
{
    public GetDocumentsQueryValidator()
        : base(maxPageSize: 100)
    {
    }
}
